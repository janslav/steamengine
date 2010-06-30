import os
import sys
from zipfile import ZipFile, ZIP_DEFLATED


import utils

def diff(oldfilename, newfilename, patchfilename):
	commandline('bsdiff ' + oldfilename + ' ' + newfilename + ' ' + patchfilename)

def commandline(command):
	#print "commandline:", command
	print command
	inpipe, outpipe = os.popen4(command)
	for line in outpipe.readlines():
		sys.stdout.write(line)
	inpipe.close()
	code = outpipe.close()
	if (code):
		raise RuntimeError('Command failed. Return code = '+code)
	

def compress_and_checksum(filename):
	archivename = filename+utils.EXTENSION_ARCHIVE
	
	try:
		archive = ZipFile(archivename, mode="w", compression=ZIP_DEFLATED)
		archive.write(filename, os.path.basename(filename))	
		utils.get_checksum(archivename)
		
	finally:
		archive.close()
	

