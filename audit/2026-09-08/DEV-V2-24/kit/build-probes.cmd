@echo off
rem DEV-V2-24 T7 probe build: A variant (GUID sorts BEFORE BUE) + Z variant (AFTER BUE).
setlocal
set MB=C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\MSBuild.exe
set PROJ=BueSameAsmProbe\BueSameAsmProbe.csproj
cd /d "%~dp0"

"%MB%" %PROJ% -t:Rebuild -p:Configuration=Release -v:minimal -nologo -flp:"logfile=build-probe-a.log;verbosity=normal"
if errorlevel 1 goto :fail

"%MB%" %PROJ% -t:Rebuild -p:Configuration=Release -p:ProbeVariant=Z -v:minimal -nologo -flp:"logfile=build-probe-z.log;verbosity=normal"
if errorlevel 1 goto :fail

copy /y BueSameAsmProbe\bin\a\BetterUnturnedExperience.dll out\BueSameAsmProbe-ABefore.dll >nul
copy /y BueSameAsmProbe\bin\z\BetterUnturnedExperience.dll out\BueSameAsmProbe-ZAfter.dll >nul
echo PROBES-OK
exit /b 0

:fail
echo PROBES-FAILED
exit /b 1
