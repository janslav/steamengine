@echo off

bin\nant -D:debug=true -D:defineSymbols="TRACE,DEBUG,MSWIN" -buildfile:distrib/nant/default.build buildCore

bin\Debug.SteamEngine.GameCore.exe

