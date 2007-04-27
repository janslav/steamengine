# rebuildIfNeeded.py - Runs distrib/build-common.bat and distrib/build-console.bat if needed.
# Copyright Richard Dillingham August 24, 2004
#
#	This program is free software; you can redistribute it and/or modify
#	it under the terms of the GNU General Public License as published by
#	the Free Software Foundation; either version 2 of the License, or
#	(at your option) any later version.
#
#	This program is distributed in the hope that it will be useful,
#	but WITHOUT ANY WARRANTY; without even the implied warranty of
#	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
#	GNU General Public License for more details.
#
#	You should have received a copy of the GNU General Public License
#	along with this program; if not, write to the Free Software
#	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
#

from __future__ import division
from datetime import datetime
import os, sys, os.path, string
import time

COPYRIGHT='Copyright Richard Dillingham August 24, 2004'

def main():
	if (recompileIsNeeded('Common',['.cs'],'bin',['Sane_Common.dll','Debug_Common.dll','Optimized_Common.dll'])):
		#os.system('pause')
		os.chdir('distrib')
		os.system('build-common.bat')
		os.chdir('..')
	if (recompileIsNeeded('WinConsole',['.cs'],'bin',['Sane_GUI.exe','Debug_GUI.exe','Optimized_GUI.exe'])):
		#os.system('pause')
		os.chdir('distrib')
		os.system('build-console.bat')
		os.chdir('..')

def recompileIsNeeded(folder, extensions, outfolder, outfiles):
	if (not os.path.exists(folder)):
		print "The folder '"+folder+"' does not exist. distrib/rebuildIfNeeded.py probably needs to be updated."
		os.system('pause')
		return 0
	if (not os.path.exists(outfolder)):
		print "The folder '"+folder+"' does not exist. distrib/rebuildIfNeeded.py probably needs to be updated."
		os.system('pause')
		return 0
	newestOutTime=None
	for outfile in outfiles:
		outpath = os.path.join(outfolder,outfile)
		if (os.path.exists(outpath)):
			timestamp=os.stat(outpath).st_mtime
			if (newestOutTime==None or timestamp>newestOutTime):
				newestOutTime=timestamp
		else:
			print "File '"+outpath+"' does not exist."
			return 1	#A recompile is definitely needed.
	while (len(folder)>0 and (folder[-1]=='/' or folder[-1]=='\\')):
		folder=folder[:-1]
	#print "newestOutTime", newestOutTime
	files=os.listdir(folder)
	#otherwise, check for newer source files
	for f in files:
		for extension in extensions:
			if ((f.lower().endswith(extension))):
				timestamp=os.stat(os.path.join(folder,f)).st_mtime
				#print "file timestamp", f, timestamp
				if (timestamp>newestOutTime):
					print "Compiling "+folder+" due to "+f+" having been updated."
					return 1
	return 0

	
if __name__ == '__main__': main()