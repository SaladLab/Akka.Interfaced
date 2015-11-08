@echo off

pushd %~dp0

tools\nuget\NuGet.exe update -self
tools\nuget\NuGet.exe install FAKE -ConfigFile tools\nuget\Nuget.Config -OutputDirectory packages -ExcludeVersion -Version 4.7.3
tools\nuget\NuGet.exe install xunit.runner.console -ConfigFile tools\nuget\Nuget.Config -OutputDirectory packages\FAKE -ExcludeVersion -Version 2.0.0

if not exist packages\SourceLink.Fake\tools\SourceLink.fsx (
  tools\nuget\nuget.exe install SourceLink.Fake -ConfigFile tools\nuget\Nuget.Config -OutputDirectory packages -ExcludeVersion
)
rem cls

set encoding=utf-8
packages\FAKE\tools\FAKE.exe build.fsx %*

popd
