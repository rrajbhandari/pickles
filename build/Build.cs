using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;

[CheckBuildProjectConfigurations]
class Build : NukeBuild
{
    public static int Main () {
        if(IsLocalBuild) {
            config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddUserSecrets<Build>()
                .Build();
        }

        return Execute<Build>(x => x.Compile);
    }

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution] readonly Solution Solution;

    static IConfiguration config;
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
        .Before(Restore)
        .Executes(() =>
        {
        });

    Target Restore => _ => _
        .Executes(() =>
        {
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
        });

}
