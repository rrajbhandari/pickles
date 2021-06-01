@echo off
set "picklesVersion=2.3.3"

cls

rem "packages\nuget\NuGet.exe" "Install" "FAKE" "-OutputDirectory" "packages" "-ExcludeVersion"
"packages\nuget\NuGet.exe" "Install" "Chocolatey" "-OutputDirectory" "packages" "-ExcludeVersion"
rem "packages\nuget\NuGet.exe" "Install" "NUnit.ConsoleRunner" "-OutputDirectory" "packages" "-ExcludeVersion"
rem "packages\nuget\NuGet.exe" "Restore" "src\Pickles\Pickles.sln"

rem "packages\FAKE\tools\Fake.exe" build.fsx --envvar version %picklesVersion%
rem "packages\FAKE\tools\Fake.exe" test.fsx --envvar version %picklesVersion%
rem if errorlevel 1 goto handleerror1orhigher
rem "packages\FAKE\tools\Fake.exe" nuget.fsx --envvar version %picklesVersion%
rem "packages\FAKE\tools\Fake.exe" chocolatey.fsx --envvar version %picklesVersion%

rem call InstallPackages.cmd

rem FOR %%A IN (testRunnerCmd testRunnerMsBuild testRunnerPowerShell) DO (
rem  call %%A.cmd %picklesVersion%
rem   if errorlevel 1 goto handleerror1orhigher
rem )

@ECHO all fine
goto end

:handleerror1orhigher

@ECHO Something went wrong!
goto end

:end
