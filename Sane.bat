@echo off

bin\nant -D:debug=false -D:defineSymbols="TRACE,SANE,MSWIN" -buildfile:distrib/nant/default.build
