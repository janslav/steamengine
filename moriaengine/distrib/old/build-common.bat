@echo off
rem mode con: cols=80 lines=50
echo Ignore any 'File not found' messages. We're deleting the compiled EXEs before recompiling so we know if compilation succeeded or failed.

cd..
cd bin
del Sane_Common.dll
del Debug_Common.dll
del Optimized_Common.dll
cd..
cd Common

%windir%\Microsoft.NET\Framework\v2.0.50727\csc /reference:..\bin\SteamDoc.dll /out:..\bin\Sane_Common.dll /lib:..\bin /t:library /recurse:*.cs /d:TRACE /d:MSWIN /d:SANE | more
if exist ..\bin\Sane_Common.dll goto optimized
goto close

:optimized
%windir%\Microsoft.NET\Framework\v2.0.50727\csc /reference:..\bin\SteamDoc.dll /out:..\bin\Optimized_Common.dll /lib:..\bin /t:library /recurse:*.cs /d:OPTIMIZED /d:MSWIN /o+ | more
if exist ..\bin\Optimized_Common.dll goto debug
goto close

:debug
%windir%\Microsoft.NET\Framework\v2.0.50727\csc /reference:..\bin\SteamDoc.dll /out:..\bin\Debug_Common.dll /lib:..\bin /t:library /recurse:*.cs /debug+ /d:MSWIN /d:DEBUG /d:TRACE | more
if exist ..\bin\Debug_Common.dll goto x2
goto close

:close
goto os_%os%

:os_Windows_NT
pause

goto x2
:os_
goto x2

:x2
cd..\distrib