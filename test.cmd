set PDIR=%CD%

cd C:\usr\isc\ens\e1010400x32u\bin
echo zn "ONECTEST"  d ##class(isc.onec.test.ContextDynamicTest).main() | cache -s..\mgr

cd %PDIR%