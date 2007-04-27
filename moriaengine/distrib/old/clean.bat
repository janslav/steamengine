@echo off
cd ..
cd bin
del *.exe
del *.pdb
del *.config
del Sane_Common.dll Debug_Common.dll Optimized_Common.dll MSVS_Common.dll
del *.xml
del SEScripts*

cd ..\distrib