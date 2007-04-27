rem @echo off

rem mode con: cols=80 lines=50
set path=%path%;%windir%\Microsoft.NET\Framework\v2.0.50727


echo WARNING: This file is unsupported.



echo Ignore any 'File not found' messages. We're deleting the compiled EXEs before recompiling so we know if compilation succeeded or failed.


cd..
cd bin
del Debug_SteamEngine_RUO.exe
del Sane_SteamEngine_RUO.exe
del Optimized_SteamEngine_RUO.exe
del Debug_GUI_RUO.exe
del Sane_GUI_RUO.exe
del Optimized_GUI_RUO.exe
cd..

rem Just change the following line to change the icon. seicon.ico should stay in the folder, and is loaded
rem by the WinConsole, too...
copy src\icon6.ico seicon.ico

:gui
cd..\src
cd WinConsole
csc /reference:..\bin\Sane_Common.dll;..\RunUO.exe /out:..\Sane_GUI_RUO.exe /lib:..\bin /t:winexe /recurse:*.cs /d:TESTRUNUO /win32icon:..\seicon.ico /d:TRACE /d:SANE /d:MSWIN | more
copy App.config ..\Sane_GUI_RUO.exe.config
if exist ..\Sane_GUI_RUO.exe goto optgui
goto close

:optgui
cd..
cd WinConsole
csc /reference:..\bin\Optimized_Common.dll;..\RunUO.exe /out:..\Optimized_GUI_RUO.exe /lib:..\bin /t:winexe /recurse:*.cs /d:SANE /d:TESTRUNUO /d:MSWIN /win32icon:..\seicon.ico /o+ | more
copy App.config ..\Optimized_GUI_RUO.exe.config
if exist ..\Optimized_GUI_RUO.exe goto dgui
goto close

:dgui
cd..
cd WinConsole
csc /reference:..\bin\Debug_Common.dll;..\RunUO.exe /d:TRACE /out:..\Debug_GUI_RUO.exe /lib:..\bin /debug+ /d:DEBUG /d:TRACE /t:winexe /recurse:*.cs /d:DEBUG /d:TESTRUNUO /d:MSWIN /win32icon:..\seicon.ico | more
copy App.config ..\Debug_GUI_RUO.exe.config
if exist ..\Debug_GUI_RUO.exe goto se
goto close

:se
cd..
set tempset=
IF EXIST ..\bin\fastdll.dll (
	set tempset=/resource:..\bin\fastdll.lib /d:USEFASTDLL /unsafe
)
csc /reference:Microsoft.JScript.dll;..\bin\Sane_Common.dll;..\bin\docGenerator.dll;..\RunUO.exe %tempset% /out:..\bin\Sane_SteamEngine_RUO.exe /t:exe /recurse:*.cs /d:TESTRUNUO /d:MSWIN /win32icon:..\seicon.ico /d:TRACE | more
if exist ..\bin\Sane_SteamEngine_RUO.exe goto optse
goto close

:optse
set tempset=
IF EXIST ..\bin\fastdll.dll (
	set tempset=/resource:..\bin\fastdll.lib /d:USEFASTDLL /unsafe
)
csc /reference:Microsoft.JScript.dll;..\bin\Optimized_Common.dll;..\bin\docGenerator.dll;..\RunUO.exe %tempset% /out:..\bin\Optimized_SteamEngine_RUO.exe /t:exe /recurse:*.cs /d:OPTIMIZED /d:TESTRUNUO /d:MSWIN /win32icon:..\seicon.ico /o+ | more
if exist ..\bin\SteamEngine_O_RUO.exe goto dse
goto close

:dse
set tempset=
IF EXIST ..\bin\fastdll.dll (
	set tempset=/resource:..\bin\Debug_fastdll.lib /d:USEFASTDLL /unsafe
)
csc /reference:Microsoft.JScript.dll;..\bin\Debug_Common.dll;..\bin\docGenerator.dll;..\RunUO.exe %tempset% /d:TRACE /debug+ /d:DEBUG /out:..\bin\Debug_SteamEngine_RUO.exe /t:exe /recurse:*.cs /d:TESTRUNUO /d:MSWIN /win32icon:..\seicon.ico | more
if exist ..\bin\Debug_SteamEngine_RUO.exe goto x2
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
