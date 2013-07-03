REM
set PDIR=%CD%
set VS_VERSION=10.0
set CONFIGURATION=Release
cd "%ProgramFiles%\Microsoft Visual Studio %VS_VERSION%\VC\"
call "%ProgramFiles%\Microsoft Visual Studio %VS_VERSION%\VC\vcvarsall.bat" x86
cd %PDIR%
cd isc.gateway.net.service
msbuild isc.gateway.net.service.sln /t:Rebuild /p:Configuration=%CONFIGURATION% /p:Platform="x86"
set BDIR=%CD%
rem net stop "Caché One C Bridge"
rem net stop "Cache One C Bridge"
echo Uninstalling...
cd %PDIR%\bin
call uninstall


rm -rf %PDIR%\bin\*

cp %BDIR%\bin\x86\%CONFIGURATION%\* %PDIR%\bin

cp -r %PDIR%\dist\* .
echo Copying config
cp %PDIR%\isc.onec.adapter\bin\x86\%CONFIGURATION%\isc.onec.adapter.dll.config %PDIR%\bin
cp %PDIR%\isc.onec.tcp.async\bin\x86\%CONFIGURATION%\isc.onec.tcp.async.dll.config %PDIR%\bin

cd %PDIR%\bin
call install

echo Starting service ...
rem net start "Caché One C Bridge"
net start "Cache One C Bridge"
echo Done.
cd %PDIR%
rem call test.cmd

cd %PDIR%
