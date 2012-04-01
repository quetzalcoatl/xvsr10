@echo off

rem if /i "%VSINSTALLDIR%"=="" (
rem   echo *** This file MUST be run from an admin VS Command prompt ***
rem   goto :eof
rem )

if /i "%VS100COMNTOOLS%"=="" (
   echo *** VS2010 shell variables are not set, please ensure VS100COMNTOOLS is set properly ***
   pause
   goto :eof
)

echo Deploying XUnitForVS TIP to VS Directory
rem echo - %VSINSTALLDIR%\Common7\IDE\PrivateAssemblies
rem copy /y "%~dp0\PrivateAssemblies\*.dll" "%VSINSTALLDIR%\Common7\IDE\PrivateAssemblies"
echo - %VS100COMNTOOLS%\..\IDE\PrivateAssemblies
copy /y "%~dp0\PrivateAssemblies\xunit.runner.visualstudio.vs2010.dll" "%VS100COMNTOOLS%\..\IDE\PrivateAssemblies"
copy /y "%~dp0\..\Imports\xunit.runner.utility.dll" "%VS100COMNTOOLS%\..\IDE\PrivateAssemblies"

set REGEDIT=regedit
if exist "%SystemRoot%\SysWOW64\regedit.exe" set REGEDIT=%SystemRoot%\SysWOW64\regedit.exe

echo Registering Package
"%~dp0\xunit.runner.visualstudio.vs2010.vsix"
"%REGEDIT%" /s "%~dp0\xunit.runner.visualstudio.vs2010.reg"

echo XUnitForVS plugin modules updated.
pause
