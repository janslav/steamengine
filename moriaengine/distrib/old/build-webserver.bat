@echo off
rem mode con: cols=80 lines=50
echo Ignore any 'File not found' messages. We're deleting the compiled EXEs before recompiling so we know if compilation succeeded or failed.

cd..
cd bin
del SteamWeb.exe
del Debug_SteamWeb.exe

cd..
cd SteamWeb

%windir%\Microsoft.NET\Framework\v2.0.50727\csc /reference:..\bin\SteamDoc.dll;..\bin\Sane_Common.dll /out:..\bin\SteamWeb.exe /lib:..\bin /t:exe /recurse:*.cs /d:MSWIN /d:OPTIMIZED /o+ /win32icon:..\seicon.ico | more
if exist ..\bin\SteamWeb.exe goto debug
goto close

:debug
%windir%\Microsoft.NET\Framework\v2.0.50727\csc /reference:..\bin\SteamDoc.dll;..\bin\Sane_Common.dll /out:..\bin\Debug_SteamWeb.exe /lib:..\bin /t:exe /recurse:*.cs /debug+ /d:MSWIN /d:DEBUG /d:TRACE /win32icon:..\seicon.ico | more
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