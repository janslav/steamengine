import os
import hashlib
import logging

EXTENSION_ARCHIVE = ".zip"
EXTENSION_CHECKSUM = ".md5"

DIRNAME_ORIGINALS = "originals"
DIRNAME_RELEASES = "releases"
DIRNAME_PATCHES = "patches"

FILENAME_UPDATEINFO = "updateinfo.xml"


BUFFERSIZE=1024*1024

def calculate_checksum(filename, progress_callback = None):
	m = hashlib.md5()
	
	filesize = os.path.getsize(filename)
	logging.info("computing checksum of file " + filename + " - " + str(int(round((filesize / 1024))))+" kB")
	
	fp = open(filename, 'rb')
	done = 0
	try:
		while 1:
			data = fp.read(BUFFERSIZE)
			if not data:
				break
			m.update(data)
			if (progress_callback):
				done += len(data)			
				progress_callback(done)
	finally:
		fp.close()
	
	return m.hexdigest()
	
def get_patchname(filename, origversion, newversion):
	origversion = os.path.basename(origversion) #the version names include directory, which we dont need
	newversion = os.path.basename(newversion)
	#filename = filename.replace("\\", ".-.")
	#filename = filename.replace("/", ".-.")
	return filename.lower()+"."+origversion+"."+newversion+".patch"


		
		
#	
#def uncompressandremove(filename):
#	archivename = filename+ARCHIVEEXTENSION
#	print "rozbaluji "+archivename
#	archive = ZipFile(archivename)
#	unzipped = open(filename, "wb")
#	unzipped.write(archive.read(os.path.basename(filename)))
#	unzipped.close()
#	archive.close()
#	os.remove(archivename)
