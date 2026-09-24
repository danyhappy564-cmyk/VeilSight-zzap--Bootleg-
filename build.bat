@echo off
rem Restore + Release build + copy to BepInEx\plugins\VeilSight + release zip
cd /d "%~dp0"
dotnet build VeilSight.sln -c Release
pause
