import os
import hashlib

EXTENSION_ARCHIVE = ".zip"
EXTENSION_CHECKSUM = ".md5"

UPDATER = "updater"
ARCHIVENAME = "moriapack"
UPDATEINFO = "updateinfo.xml"



BUFFERSIZE=1024*1024

def get_checksum(filename):
	sumfilename = get_checksum_filename(filename)
	if is_newer_file(filename, sumfilename):
		sum = calculate_checksum(filename)
		f = open(sumfilename,"w")
		f.write(sum)
		f.close()
		return sum
	else:
		f = open(sumfilename,"r")
		sum = f.read()
		f.close()
		return sum

def get_checksum_filename(filename):
	return filename + EXTENSION_CHECKSUM

def calculate_checksum(filename):
	print "calculating checksum for ", filename
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


#source = a .mul file or something
#dependant = its compressed version, or .md5 file etc
#if the 'source' is newer than the dependant, the dependant will need to be updated
def is_newer_file(filename_source, filename_dependant):
	if not os.path.exists(filename_dependant):
		return True
	
	srcstat = os.stat(filename_source)
	srctime = max(srcstat.st_atime, srcstat.st_mtime, srcstat.st_ctime)
	
	depstat = os.stat(filename_dependant)	
	deptime = max(depstat.st_atime, depstat.st_mtime, depstat.st_ctime)
	
	return srctime > deptime

def is_helper_filename(filename):
	return filename.endswith(EXTENSION_ARCHIVE) or \
		filename.endswith(EXTENSION_CHECKSUM)
		
		
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
