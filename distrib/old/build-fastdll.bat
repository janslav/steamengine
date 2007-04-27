@echo off
rem mode con: cols=80 lines=50

rem You need the Microsoft Platform SDK to compile this.
rem You also need Microsoft Visual C++ Toolkit 2003 (or VS.NET).
rem http://www.google.com/search?hl=en&ie=UTF-8&oe=UTF-8&q=microsoft%20platform%20sdk&btnI=1
rem http://www.google.com/search?hl=en&ie=UTF-8&oe=UTF-8&q=visual%20c++%20toolkit&btnI=1

rem Your PATH, INCLUDE, and LIB environment vars should include the folders like this (Yours may be different):
rem If you set these in your environment variables, then DMC breaks (Digital Mars' c++ compiler).
rem If you put quotes around "c:\blah blah", then DMC works but VC++ breaks.
rem So we just set them here in this file, since this file only uses VC++. And in the compile scripts which use DMC, they set the appropriate
rem vars there too.

set PATH=C:\Program Files\Microsoft Visual C++ Toolkit 2003\bin;C:\Program Files\Microsoft Platform SDK for Windows XP SP2\Bin;%PATH%
set INCLUDE=C:\Program Files\Microsoft Visual C++ Toolkit 2003\include;C:\Program Files\Microsoft Platform SDK for Windows XP SP2\include;%INCLUDE%
set LIB=C:\Program Files\Microsoft Visual C++ Toolkit 2003\lib;C:\Program Files\Microsoft Platform SDK for Windows XP SP2\Lib;%LIB%

echo Ignore any 'File not found' messages. We're deleting the compiled EXEs before recompiling so we know if compilation succeeded or failed.

rem libs that aren't needed:  odbc32.lib odbccp32.lib 

cd ..\fastdll

mkdir Debug
mkdir Release

del ..\bin\fastdll.dll
del ..\bin\Debug_fastdll.dll

rem Debug first
cl /nologo /D "DOTNET" /MLd /W3 /RTC1 /GX /GS /ZI /Od /D "WIN32" /D "_DEBUG" /D "_WINDOWS" /D "_MBCS" /D "_USRDLL" /D "FASTDLL_EXPORTS" /Fp"Debug\fastdll.pch" /YX /Fo"Debug/" /Fd"Debug/" /FD /GZ /c fastdll.cpp
IF EXIST Debug\fastdll.obj (
    goto link  
)
goto nolink
:link

link Debug\fastdll.obj shfolder.lib kernel32.lib user32.lib gdi32.lib comdlg32.lib advapi32.lib shell32.lib ole32.lib oleaut32.lib uuid.lib /nologo /dll /incremental:yes /pdb:"Debug\fastdll.pdb" /debug /machine:I386 /def:".\fastdll.def" /out:"..\bin\Debug_fastdll.dll" /implib:"..\bin\Debug_fastdll.lib"
:nolink

if exist ..\bin\Debug_fastdll.dll goto comprelease
goto close

:comprelease
cl /nologo /D "DOTNET" /ML /W3 /GX /Ox /D "WIN32" /D "NDEBUG" /D "_WINDOWS" /D "_MBCS" /D "_USRDLL" /D "FASTDLL_EXPORTS" /Fp"Release/fastdll.pch" /YX /Fo"Release/" /Fd"Release/" /FD /c fastdll.cpp

IF EXIST Release\fastdll.obj (
    goto Rlink
)
goto Rnolink
:Rlink
link Release/fastdll.obj shfolder.lib kernel32.lib user32.lib gdi32.lib comdlg32.lib advapi32.lib shell32.lib ole32.lib oleaut32.lib uuid.lib /nologo /dll /incremental:no /pdb:"Release/art.pdb" /machine:I386 /def:".\fastdll.def" /out:"..\bin\fastdll.dll" /implib:"..\bin\fastdll.lib" 
:Rnolink

if exist ..\bin\fastdll.dll goto done
goto close

:done
goto close

:close
goto os_%os%

:os_Windows_NT
pause
goto x2
:os_
goto x2
:x2
cd..
