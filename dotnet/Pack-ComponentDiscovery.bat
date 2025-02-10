nuget pack ./ComponentDiscovery.nuspec -Build -Symbols -OutputDirectory ".\(Stage)\Packages" -InstallPackageToOutputPath
IF NOT EXIST "..\..\(NuGetRepo)" GOTO NOCOPYTOGLOBALREPO
xcopy ".\dist\Packages\*.nuspec" "..\..\(NuGetRepo)\" /d /r /y /s
xcopy ".\dist\Packages\*.nupkg*" "..\..\(NuGetRepo)\" /d /r /y /s
:NOCOPYTOGLOBALREPO
PAUSE