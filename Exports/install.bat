@echo off

if /i "%VSINSTALLDIR%"=="" (
   echo *** This file MUST be run from an admin VS Command prompt ***
   goto :eof
)

set REGEDIT=regedit
if exist "%SystemRoot%\SysWOW64\regedit.exe" set REGEDIT=%SystemRoot%\SysWOW64\regedit.exe


echo Deploying XUnitForVS TIP to VS Directory
echo - %VSINSTALLDIR%\Common7\IDE\PrivateAssemblies
copy /y "%~dp0\PrivateAssemblies\*.dll" "%VSINSTALLDIR%\Common7\IDE\PrivateAssemblies"


echo Deploying Project Item Template to VS Directory
echo - %VSINSTALLDIR%\Common7\IDE\ItemTemplates\CSharp\1033
xcopy /r /y /i "%~dp0\VSTemplates\*.zip" "%VSINSTALLDIR%\Common7\IDE\ItemTemplates\CSharp\1033"


echo Registering Package
call XUnitForVS.vsix
"%REGEDIT%" /s "%~dp0\XUnitForVS.reg"

echo Registering Visual Studio Templates
devenv /setup /installvstemplates


echo Visual Studio is now capable of opening and executing XUnit tests.
