set PDIR=%CD%
cd "C:\Program Files\Microsoft Visual Studio 9.0\VC\"
REM %comspec% /k "" 
call "C:\Program Files\Microsoft Visual Studio 9.0\VC\vcvarsall.bat" x86

cd %PDIR%
cd isc.gateway.net.service
msbuild isc.gateway.net.service.sln /t:Rebuild /p:Configuration=Release /p:Platform="x86"

set BDIR=%CD%

rem net stop "Caché One C Bridge"
rem net stop "Cache One C Bridge"

echo Uninstalling...
cd %PDIR%\bin
call uninstall
echo Done.

rm %PDIR%\bin\*

cp %BDIR%\bin\x86\Release\* %PDIR%\bin

cp %PDIR%\dist\* .


cd %PDIR%\bin
call install

echo Starting service ...
rem net start "Caché One C Bridge"
net start "Cache One C Bridge"
echo Done.

cd C:\usr\isc\ens\e1010400x32u\bin
echo zn "ONECTEST"  d ##class(isc.onec.test.ContextDynamicTest).main() | cache -s..\mgr

cd %PDIR%