using System;
using System.IO.Compression;
using System.Linq;
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
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
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
                 from runtime in new[] { "win10-x86", "osx-x64", "linux-x64" }
                 select new { runtime };

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
                 ZipFile.CreateFromDirectory(CommandLineDirectory / p.runtime, DeployDirectory / "zip" / "Pickles-exe-" + p.runtime + "-" + Version + ".zip");
                 ZipFile.CreateFromDirectory(MsBuildDirectory / p.runtime, DeployDirectory / "zip" / "Pickles-msbuild-" + p.runtime + "-" + Version + ".zip");
                 ZipFile.CreateFromDirectory(PowerShellDirectory / p.runtime, DeployDirectory / "zip" / "Pickles-powershell-" + p.runtime + "-" + Version + ".zip");
             }
         });
}