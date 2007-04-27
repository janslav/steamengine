@echo off
set mono_path=C:\Program Files\Mono\bin\
set path=%path%;%mono_path%
echo Expectation: path to MONO executables is %mono_path%
echo Ignore any 'File not found' messages. We're deleting the compiled EXEs before recompiling so we know if compilation succeeded or failed.

cd ..\..\bin
del Sane_Common.dll
del Debug_Common.dll
del Optimized_Common.dll
cd ..\Common

echo.
echo Sane build...
mcs -out:../bin/Sane_Common.dll -lib:../bin -t:library -recurse:*.cs -d:TRACE -d:OPTIMIZED -d:MONO -d:MSWIN -r:System.Drawing;..\bin\SteamDoc.dll | more
if exist ..\bin\Sane_Common.dll goto optimized
goto close

:optimized
echo Optimized build...
mcs -out:../bin/Optimized_Common.dll -lib:../bin -t:library -recurse:*.cs -d:OPTIMIZED -d:MONO -d:MSWIN -r:System.Drawing;..\bin\SteamDoc.dll | more
if exist ..\bin\Optimized_Common.dll goto debug
goto close

:debug
echo Debug build...
mcs -out:../bin/Debug_Common.dll -lib:../bin -t:library -recurse:*.cs -debug+ -d:DEBUG -d:TRACE -d:MONO -d:MSWIN -r:System.Drawing;..\bin\SteamDoc.dll | more
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
cd ..\distrib\mono-win