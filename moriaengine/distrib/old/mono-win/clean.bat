@echo off
cd ..\..\bin
del *.exe
del *.pdb
del *.config
del Sane_Common.dll Debug_Common.dll Optimized_Common.dll

cd ..\distrib\mono-win