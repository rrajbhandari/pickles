using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[CheckBuildProjectConfigurations]
[ShutdownDotNetAfterServerBuild]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode
    public static int Main()
    {
        if (IsLocalBuild)
        {
            config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddUserSecrets<Build>()
                .Build();
        }

        return Execute<Build>(x => x.Publish);
    }

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    static IConfiguration config;
    [Solution] readonly Solution Solution;
    [GitRepository] readonly GitRepository GitRepository;

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    AbsolutePath PublishDirectory => ArtifactsDirectory / "publish";
    AbsolutePath CommandLineDirectory => PublishDirectory / "exe";
    AbsolutePath MsBuildDirectory => PublishDirectory / "msbuild";
    AbsolutePath PowerShellDirectory => PublishDirectory / "powershell";
    AbsolutePath DeployDirectory => ArtifactsDirectory / "deploy";

    AbsolutePath OutputDirectory => RootDirectory / "Output";

    String AssemblyProduct = "Pickles";
    String AssemblyCompany = "Pickles";
    String Version = "3.0.0-alpha.2";
    String Copyright = "Copyright (c) Jeffrey Cameron 2010-2012, PicklesDoc 2012-present";
    String NuGetApiKey = "";

    Target Clean => _ => _
        .Before(Test)
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
            EnsureCleanDirectory(ArtifactsDirectory);
            EnsureCleanDirectory(DeployDirectory);
            EnsureCleanDirectory(DeployDirectory / "zip");
            EnsureCleanDirectory(DeployDirectory / "nuget");
            EnsureCleanDirectory(OutputDirectory);
        });

    Target Test => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetProjectFile(RootDirectory / "src" / "Pickles" / "Pickles.sln")
                .SetLoggers("trx;LogFileName=TestResults.xml")
            );
        });

    Target Publish => _ => _
        .DependsOn(Test)
        .Executes(() =>
        {
            var publishCombinations =
                from runtime in new[] {"win10-x86", "osx-x64", "linux-x64"}
                select new {runtime};

            DotNetPublish(p => p
                .SetProject(RootDirectory / "src" / "Pickles" / "Pickles.CommandLine")
                .SetConfiguration(Configuration)
                .SetVersion(Version)
                .CombineWith(publishCombinations, (s, v) => s
                    .SetRuntime(v.runtime)
                    .SetOutput(CommandLineDirectory / v.runtime)
                )
            );

            DotNetPublish(p => p
                .SetProject(RootDirectory / "src" / "Pickles" / "Pickles.MsBuild")
                .SetConfiguration(Configuration)
                .SetVersion(Version)
                .CombineWith(publishCombinations, (s, v) => s
                    .SetRuntime(v.runtime)
                    .SetOutput(MsBuildDirectory / v.runtime)
                )
            );

            DotNetPublish(p => p
                .SetProject(RootDirectory / "src" / "Pickles" / "Pickles.PowerShell")
                .SetConfiguration(Configuration)
                .SetVersion(Version)
                .CombineWith(publishCombinations, (s, v) => s
                    .SetRuntime(v.runtime)
                    .SetOutput(PowerShellDirectory / v.runtime)
                )
            );

            foreach (var p in publishCombinations)
            {
                ZipFile.CreateFromDirectory(CommandLineDirectory / p.runtime,
                    DeployDirectory / "zip" / "Pickles-exe-" + p.runtime + "-" + Version + ".zip");
                ZipFile.CreateFromDirectory(MsBuildDirectory / p.runtime,
                    DeployDirectory / "zip" / "Pickles-msbuild-" + p.runtime + "-" + Version + ".zip");
                ZipFile.CreateFromDirectory(PowerShellDirectory / p.runtime,
                    DeployDirectory / "zip" / "Pickles-powershell-" + p.runtime + "-" + Version + ".zip");
            }
        });

    Target GenerateSampleOutput => _ => _
        .DependsOn(Publish)
        .Executes(() =>
        {
            string runtime = String.Empty;
            var formats = new List<string> {"Html", "Dhtml", "Word", "Excel", "JSON", "Cucumber", "Markdown"};
            string exampleSource = SourceDirectory / "Pickles" / "Examples";
            string outputFolder = string.Empty;

            bool isMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

            if (isMac)
            {
                runtime = "osx-x64";
            }
            else if (isWindows)
            {
                runtime = "win10-x86";
            }
            else if (isLinux)
            {
                runtime = "linux-x64";
            }

            foreach (var format in formats)
            {
                outputFolder = OutputDirectory / format;

                ProcessStartInfo processStartInfo =
                    new ProcessStartInfo(PublishDirectory / "exe" / runtime / "Pickles",
                        $"-f={exampleSource} -o={outputFolder} -df={format} --sn=Pickles --sv={Version}");

                //TODO Repeat with experimental features

                processStartInfo.CreateNoWindow = false;
                processStartInfo.UseShellExecute = false;
                Process p = Process.Start(processStartInfo);
                p.WaitForExit();
                Console.WriteLine(p.ExitCode);

                // MSBuild(o => o
                //     .SetTargetPath(RootDirectory / "testOutput.proj")
                //
                //     .SetProperty("Version", Version)
                //     .SetProperty("ShouldIncludeExperimentalFeatures", false)
                // );
            }

            //Copy sample output to docs folder
            EnsureCleanDirectory(RootDirectory / "docs" / "Output");
            CopyFilesRecursively(new DirectoryInfo(RootDirectory / "Output"), new DirectoryInfo(RootDirectory / "docs" / "Output"));
            EnsureCleanDirectory(RootDirectory / "Output");

            //Update version in docs index
            var index = File.ReadAllText(RootDirectory / "docs" / "index_template.html");
            index = index.Replace("VERSION_PLACEHOLDER", Version);
            File.WriteAllText(RootDirectory / "docs" / "index.html", index);
        });

    public static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
    {
        foreach (DirectoryInfo dir in source.GetDirectories())
            CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
        foreach (FileInfo file in source.GetFiles())
            file.CopyTo(Path.Combine(target.FullName, file.Name));
    }

    Target Pack => _ => _
        .DependsOn(GenerateSampleOutput)
        .Executes(() =>
        {
            var commandLineProject = File.ReadAllText(RootDirectory / "src" / "Pickles" / "Pickles.CommandLine" /
                                                      "Pickles.CommandLine.csproj");

            //Setting PackageId via the dotnet pack command sets the id for all referenced projects and
            //throws the "ambiguous project name" error. Duplicating the project file is a temporary hack
            //till this gets fixed.
            var clPackage = commandLineProject.Replace("PACKAGE_ID", "Pickles.CommandLine");
            File.WriteAllText(
                RootDirectory / "src" / "Pickles" / "Pickles.CommandLine" / "Pickles.CommandLine.cl.csproj", clPackage);

            DotNetPack(s => s
                    .SetProject(RootDirectory / "src" / "Pickles" / "Pickles.CommandLine" /
                                "Pickles.CommandLine.cl.csproj")
                    .SetConfiguration(Configuration)
                    .SetProperty("Version", Version)
                    .SetRuntime("win10-x86")
                    .SetOutputDirectory(DeployDirectory / "nuget")

                //.SetCopyright(Copyright)
            );

            File.Delete(RootDirectory / "src" / "Pickles" / "Pickles.CommandLine" / "Pickles.CommandLine.cl.csproj");

            var winPackage = commandLineProject.Replace("PACKAGE_ID", "Pickles.CommandLine.win");
            File.WriteAllText(
                RootDirectory / "src" / "Pickles" / "Pickles.CommandLine" / "Pickles.CommandLine.win.csproj", winPackage);

            DotNetPack(s => s
                    .SetProject(RootDirectory / "src" / "Pickles" / "Pickles.CommandLine" /
                                "Pickles.CommandLine.win.csproj")
                    .SetConfiguration(Configuration)
                    .SetProperty("Version", Version)
                    //.SetProperty("NuspecFile", "")
                    .SetRuntime("win10-x86")
                    .SetOutputDirectory(DeployDirectory / "nuget")
                //.SetCopyright(Copyright)
            );

            File.Delete(RootDirectory / "src" / "Pickles" / "Pickles.CommandLine" / "Pickles.CommandLine.win.csproj");

            var macPackage = commandLineProject.Replace("PACKAGE_ID", "Pickles.CommandLine.mac");
            File.WriteAllText(
                RootDirectory / "src" / "Pickles" / "Pickles.CommandLine" / "Pickles.CommandLine.mac.csproj", macPackage);

            DotNetPack(s => s
                    .SetProject(RootDirectory / "src" / "Pickles" / "Pickles.CommandLine" /
                                "Pickles.CommandLine.mac.csproj")
                    .SetConfiguration(Configuration)
                    .SetProperty("Version", Version)
                    .SetRuntime("osx-x64")
                    .SetOutputDirectory(DeployDirectory / "nuget")

                //.SetCopyright(Copyright)
            );

            File.Delete(RootDirectory / "src" / "Pickles" / "Pickles.CommandLine" / "Pickles.CommandLine.mac.csproj");

            var linuxPackage = commandLineProject.Replace("PACKAGE_ID", "Pickles.CommandLine.linux");
            File.WriteAllText(
                RootDirectory / "src" / "Pickles" / "Pickles.CommandLine" / "Pickles.CommandLine.linux.csproj", linuxPackage);

            DotNetPack(s => s
                    .SetProject(RootDirectory / "src" / "Pickles" / "Pickles.CommandLine" /
                                "Pickles.CommandLine.linux.csproj")
                    .SetConfiguration(Configuration)
                    .SetProperty("Version", Version)
                    .SetRuntime("linux-x64")
                    .SetOutputDirectory(DeployDirectory / "nuget")

               //.SetCopyright(Copyright)
             );

            File.Delete(RootDirectory / "src" / "Pickles" / "Pickles.CommandLine" / "Pickles.CommandLine.linux.csproj");

            // DotNetPack(s => s
            //         .SetProject(RootDirectory / "src" / "Pickles" / "Pickles.MsBuild" / "Pickles.MsBuild.csproj")
            //         .SetConfiguration(Configuration)
            //         .SetRuntime("win10-x86")
            //         .SetProperty("Version", Version)
            //         .SetOutputDirectory(DeployDirectory / "nuget")
            //     //.SetCopyright(Copyright)
            // );

            DotNetPack(s => s
                    .SetProject(RootDirectory / "src" / "Pickles" / "Pickles.PowerShell" /
                                "Pickles.PowerShell.csproj")
                    .SetConfiguration(Configuration)
                    .SetRuntime("win10-x86")
                    .SetProperty("Version", Version)
                    .SetOutputDirectory(DeployDirectory / "nuget")
                //.SetCopyright(Copyright)
            );
        });
}