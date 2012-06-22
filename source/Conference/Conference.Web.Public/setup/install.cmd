@REM skip processing when running in the dev fabric
if "%INSTRUMENTATIONENABLED%"=="false" goto :EOF

@REM cd to the location of the script
cd "%~dp0"

@REM allow unsigned script execution
powershell -command Set-ExecutionPolicy RemoteSigned

@REM execute the script
powershell -command ./install.ps1 2>&1 > install.log