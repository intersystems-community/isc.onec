@ECHO OFF

REM The following directory is for .NET 4.0
set DOTNETFX4=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319
set PATH=%PATH%;%DOTNETFX4%
set CONFIGURATION=Release

echo Installing WindowsService...
echo ---------------------------------------------------
cd bin\x86\%CONFIGURATION%
InstallUtil /i isc.onec.service.exe
echo ---------------------------------------------------
echo Done.
