@echo off

Import-Module -Name ".\lib\Invoke-MsBuild.psm1"

Invoke-MsBuild -Path ".\SteamEngine.sln" -ShowBuildWindow -MsBuildParameters "/target:Clean;Build /property:Configuration=Debug" 

Invoke-MsBuild -Path ".\SteamEngine.sln" -ShowBuildWindow -MsBuildParameters "/target:Clean;Build /property:Configuration=Release" 