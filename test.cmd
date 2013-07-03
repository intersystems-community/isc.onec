rem
set PDIR=%CD%

cd %GLOBALS_HOME%\bin
echo zn "ONECTEST"  for i=1:1:10 { job ##class(isc.onec.test.ContextDynamicTest).main() } | cache -s..\mgr
echo zn "ONECTEST"  d ##class(isc.onec.test.ContextDynamicTest).main() | cache -s..\mgr

cd %PDIR%
