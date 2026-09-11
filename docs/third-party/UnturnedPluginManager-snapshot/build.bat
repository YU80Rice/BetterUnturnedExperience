@echo off
REM ============================================================
REM  PluginManager build script
REM  Output: ..\BepInEx\Plugins\PluginManagerMod.dll
REM ============================================================
setlocal
set GAME=%~dp0..
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe

if not exist "%CSC%" (
    echo [ERROR] csc.exe not found. Please install .NET Framework 4.x
    exit /b 1
)

"%CSC%" /nologo /t:library /codepage:65001 ^
 /out:"%GAME%\BepInEx\Plugins\PluginManagerMod.dll" ^
 /r:"%GAME%\BepInEx\core\BepInEx.dll" ^
 /r:"%GAME%\BepInEx\core\0Harmony.dll" ^
 /r:"%GAME%\Unturned_Data\Managed\UnityEngine.dll" ^
 /r:"%GAME%\Unturned_Data\Managed\UnityEngine.CoreModule.dll" ^
 /r:"%GAME%\Unturned_Data\Managed\UnityEngine.InputLegacyModule.dll" ^
 /r:"%GAME%\Unturned_Data\Managed\UnityEngine.TextRenderingModule.dll" ^
 /r:"%GAME%\Unturned_Data\Managed\netstandard.dll" ^
 /r:"%GAME%\Unturned_Data\Managed\Assembly-CSharp.dll" ^
 /r:"%GAME%\Unturned_Data\Managed\SDG.Glazier.Runtime.dll" ^
 /r:"%GAME%\Unturned_Data\Managed\UnturnedDat.dll" ^
 "%~dp0PluginManagerMod.cs"

if errorlevel 1 (
    echo [ERROR] Compilation failed. See messages above.
    exit /b 1
)

echo [OK] Built: %GAME%\BepInEx\Plugins\PluginManagerMod.dll
endlocal
