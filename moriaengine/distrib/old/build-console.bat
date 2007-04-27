@echo off
rem mode con: cols=80 lines=50
echo Ignore any 'File not found' messages. We're deleting the compiled EXEs before recompiling so we know if compilation succeeded or failed.

cd ..\bin
del Sane_GUI.exe
del Debug_GUI.exe
del Optimized_GUI.exe
cd ..

rem Just change the following line to change the icon. seicon.ico should stay in the folder, and is loaded
rem by the WinConsole, too...
copy src\icon6.ico bin\seicon.ico

:compconsole
cd WinConsole
%windir%\Microsoft.NET\Framework\v2.0.50727\csc /reference:..\bin\Sane_Common.dll /out:..\bin\Sane_GUI.exe /lib:..\bin /t:winexe /recurse:*.cs /d:TRACE /d:SANE /d:MSWIN /win32icon:..\bin\seicon.ico | more
copy App.config ..\bin\Sane_GUI.exe.config
if exist ..\bin\Sane_GUI.exe goto optimizedconsole
goto close

:optimizedconsole
%windir%\Microsoft.NET\Framework\v2.0.50727\csc /reference:..\bin\Optimized_Common.dll /out:..\bin\Optimized_GUI.exe /d:MSWIN /d:OPTIMIZED /lib:..\bin /t:winexe /recurse:*.cs /win32icon:..\bin\seicon.ico /o+ | more
copy App.config ..\bin\Optimized_GUI.exe.config
if exist ..\bin\Optimized_GUI.exe goto debugconsole
goto close

:debugconsole
%windir%\Microsoft.NET\Framework\v2.0.50727\csc /reference:..\bin\Debug_Common.dll /out:..\bin\Debug_GUI.exe /lib:..\bin /t:exe /recurse:*.cs /win32icon:..\bin\seicon.ico /debug+ /d:DEBUG /d:TRACE /d:MSWIN | more
copy App.config ..\bin\Debug_GUI.exe.config
if exist ..\bin\Debug_GUI.exe goto x2
goto close

:close
goto os_%os%

:os_Windows_NT
pause

goto x2
:os_
goto x2

:x2
cd ..\distrib