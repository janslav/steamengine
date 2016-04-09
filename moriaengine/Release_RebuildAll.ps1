

Import-Module -Name ".\lib\Invoke-MsBuild.psm1"

Invoke-MsBuild -Path ".\SteamEngine.sln" -ShowBuildWindow -MsBuildParameters "/target:Clean;Build /property:Configuration=Release" 

#bin\nant -D:debug=false -D:cmdLineParams=/debug+ -D:defineSymbols="TRACE,SANE,MSWIN" -buildfile:distrib/nant/default.build buildAuxiliaryServer


#bin\Sane.SteamEngine.AuxiliaryServer.exe