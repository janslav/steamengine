@echo off
set mono_path=C:\Program Files\Mono\bin\
set path=%path%;%mono_path%
echo Expectation: path to MONO executables is %mono_path%
echo Ignore any 'File not found' messages. We're deleting the compiled EXEs before recompiling so we know if compilation succeeded or failed.

cd ..\..\bin
del Sane_SteamEngine.exe
del Optimized_SteamEngine.exe
del Debug_SteamEngine.exe
cd..

rem Just change the following line to change the icon. seicon.ico should stay in the folder, and is loaded
rem by the WinConsole, too...
copy src\icon6.ico bin\seicon.ico

cd src
echo.
echo Sane build...
mcs -r:../bin/Sane_Common.dll;../bin/SteamDoc.dll;System.Drawing -out:../bin/Sane_SteamEngine.exe -t:exe -recurse:*.cs -d:TRACE -d:OPTIMIZED -d:MONO -win32icon:../seicon.ico | more
if exist ..\bin\Sane_SteamEngine.exe goto compoptimized
goto close

:compoptimized
echo Optimized build...
mcs -r:System.Drawing;../bin/Optimized_Common.dll;../bin/SteamDoc.dll -out:../bin/Optimized_SteamEngine.exe -d:OPTIMIZED -d:MONO -t:exe -recurse:*.cs -win32icon:../seicon.ico | more
if exist ..\bin\Optimized_SteamEngine.exe goto compdebug
goto close

:compdebug
echo Debug build...
mcs -r:System.Drawing;../bin/Debug_Common.dll;../bin/SteamDoc.dll -out:../bin/Debug_SteamEngine.exe -t:exe -recurse:*.cs -win32icon:../seicon.ico -debug+ -d:DEBUG -d:TRACE -d:MONO | more
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
cd ..\distrib\mono-win
