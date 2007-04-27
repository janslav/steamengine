@echo off
rem mode con: cols=80 lines=50
echo Ignore any 'File not found' messages. We're deleting the compiled EXEs before recompiling so we know if compilation succeeded or failed.

cd..
cd bin
del Converter.exe
cd..
cd SphereShardConverter

%windir%\Microsoft.NET\Framework\v1.1.4322\csc /reference:..\bin\SteamDoc.dll;..\bin\Debug_Common.dll;..\bin\Debug_SteamEngine.exe /out:..\bin\Converter.exe /lib:..\bin /t:exe /recurse:*.cs /debug+ /d:MSWIN /d:DEBUG /d:TRACE | more
copy ..\WinConsole\App.config ..\bin\Converter.exe.config
if exist ..\bin\Converter.exe goto x2
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