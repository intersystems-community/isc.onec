@echo off

set DOTNETFX4=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319
set PATH=%PATH%;%DOTNETFX4%
set CONFIGURATION=Debug

cd bin\x86\%CONFIGURATION%

:loop
set /p PORT="Please enter the target TCP port number: "
if not defined PORT goto loop
InstallUtil /port=%PORT% isc.onec.service.exe
