@echo off

bin\nant -D:debug=false -D:defineSymbols="OPTIMIZED,MSWIN" -D:optimize=true -buildfile:distrib/nant/default.build buildCore


bin\Optimized.SteamEngine.GameCore.exe