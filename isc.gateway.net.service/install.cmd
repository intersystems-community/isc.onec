@echo off

set DOTNETFX4=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319
set PATH=%PATH%;%DOTNETFX4%

set DEFAULT_PORT=9101
set /p PORT="Please enter the target TCP port number [%DEFAULT_PORT%]: "
if not defined PORT set PORT=%DEFAULT_PORT%
InstallUtil /port=%PORT% isc.onec.service.exe
