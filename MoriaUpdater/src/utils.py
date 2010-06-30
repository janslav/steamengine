import os
import hashlib

EXTENSION_ARCHIVE = ".zip"
EXTENSION_CHECKSUM = ".md5"

DIRNAME_ORIGINALS = "originals"
DIRNAME_RELEASES = "releases"
DIRNAME_PATCHES = "patches"

FILENAME_UPDATEINFO = "updateinfo.xml"


BUFFERSIZE=1024*1024

def calculate_checksum(filename):
	print "	checksumming:", filename
	m = hashlib.md5()
	fp = open(filename, 'rb')
	try:
		while 1:
			data = fp.read(BUFFERSIZE)
			if not data:
				break
			m.update(data)
	finally:
		fp.close()
	
	return m.hexdigest()

	
def get_patchname(filename, origversion, newversion):
	origversion = os.path.basename(origversion) #the version names include directory, which we dont need
	newversion = os.path.basename(newversion)
	#filename = filename.replace("\\", ".-.")
	#filename = filename.replace("/", ".-.")
	return filename+"."+origversion+"."+newversion+".patch"


		
		
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
