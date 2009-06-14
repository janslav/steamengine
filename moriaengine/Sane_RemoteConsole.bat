@echo off

bin\nant -D:debug=false -D:cmdLineParams=/debug+ -D:defineSymbols="TRACE,SANE,MSWIN" -buildfile:distrib/nant/default.build buildRemoteConsole

start bin\Sane.SteamEngine.RemoteConsole.exe