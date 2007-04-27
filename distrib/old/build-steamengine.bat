@echo off
rem mode con: cols=80 lines=50
echo Ignore any 'File not found' messages. We're deleting the compiled EXEs before recompiling so we know if compilation succeeded or failed.


cd..
cd bin
del Sane_SteamEngine.exe
del Optimized_SteamEngine.exe
del Debug_SteamEngine.exe
cd..

rem Just change the following line to change the icon. seicon.ico should stay in the folder, and is loaded
rem by the WinConsole, too...
copy src\icon6.ico bin\seicon.ico

cd src
set tempset=
IF EXIST ..\bin\fastdll.dll (
	set tempset=/resource:..\bin\fastdll.lib /d:USEFASTDLL /unsafe
)
%windir%\Microsoft.NET\Framework\v2.0.50727\csc /reference:Microsoft.JScript.dll;..\bin\Sane_Common.dll;..\bin\SteamDoc.dll %tempset% /out:..\bin\Sane_SteamEngine.exe /t:exe /recurse:*.cs /d:TRACE /d:MSWIN /d:SANE /win32icon:..\seicon.ico | more
if exist ..\bin\Sane_SteamEngine.exe goto compoptimized
goto close

:compoptimized
set tempset=
IF EXIST ..\bin\fastdll.dll (
	set tempset=/resource:..\bin\fastdll.lib /d:USEFASTDLL /unsafe
)
%windir%\Microsoft.NET\Framework\v2.0.50727\csc /warn:4 /warnaserror /reference:Microsoft.JScript.dll;..\bin\Optimized_Common.dll;..\bin\SteamDoc.dll /out:..\bin\Optimized_SteamEngine.exe %tempset% /d:OPTIMIZED /d:MSWIN /t:exe /recurse:*.cs /win32icon:..\seicon.ico /o+ | more
if exist ..\bin\Optimized_SteamEngine.exe goto compdebug
goto close

:compdebug
set tempset=
IF EXIST ..\bin\Debug_fastdll.dll (
	set tempset=/resource:..\bin\Debug_fastdll.lib /d:USEFASTDLL /unsafe
)
%windir%\Microsoft.NET\Framework\v2.0.50727\csc /reference:Microsoft.JScript.dll;..\bin\Debug_Common.dll;..\bin\SteamDoc.dll /out:..\bin\Debug_SteamEngine.exe %tempset% /t:exe /recurse:*.cs /win32icon:..\seicon.ico /debug+ /d:DEBUG /d:TRACE /d:MSWIN | more
if exist ..\bin\Debug_SteamEngine.exe goto x2
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
