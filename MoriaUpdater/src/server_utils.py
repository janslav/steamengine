import os
import sys
from zipfile import ZipFile, ZIP_DEFLATED


import utils


def get_checksum(filename):
	sumfilename = get_checksum_filename(filename)
	if is_newer_file(filename, sumfilename):
		sum = utils.calculate_checksum(filename)
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
	return filename + utils.EXTENSION_CHECKSUM

#source = a .mul file or something
#dependant = its compressed version, or .md5 file etc
#if the 'source' is newer than the dependant, the dependant will need to be updated
def is_newer_file(filename_source, filename_dependant):
	if not os.path.exists(filename_dependant):
		return True
	
	srcstat = os.stat(filename_source)
	srctime = max(srcstat.st_mtime, srcstat.st_ctime)
	
	depstat = os.stat(filename_dependant)	
	deptime = max(depstat.st_mtime, depstat.st_ctime)
	
	return srctime > deptime

def is_helper_filename(filename):
	return filename.endswith(utils.EXTENSION_ARCHIVE) or \
		filename.endswith(utils.EXTENSION_CHECKSUM)
					
def create_patch_if_needed(ftproot, patchesdir, filename, origversion, newversion): #returns name of the patch file
	patchpath = os.path.join(patchesdir, utils.get_patchname(filename, origversion.name, newversion.name))
	patchdir = os.path.dirname(patchpath)
	if not os.path.exists(patchdir):
		os.makedirs(patchdir)
	if os.path.exists(patchpath):
		pass #the patch already exists, we do nothing
	else:
		origpath = os.path.join(ftproot, origversion.name, filename)
		newpath = os.path.join(ftproot, newversion.name, filename)
		diff(origpath, newpath, patchpath)
	return patchpath
		
def delete_patch(patchesdir, filename, origversion, newversion):
	patchpath = os.path.join(patchesdir, utils.get_patchname(filename, origversion.name, newversion.name))
	if os.path.exists(patchpath):
		os.remove(patchpath)
	checksumfile = get_checksum_filename(patchpath)
	if os.path.exists(checksumfile):
		os.remove(checksumfile)



def diff(oldfilename, newfilename, patchfilename):
	_commandline('bsdiff ' + oldfilename + ' ' + newfilename + ' ' + patchfilename)

def _commandline(command):
	print "	cmd:", command
	inpipe, outpipe = os.popen4(command)
	for line in outpipe.readlines():
		sys.stdout.write(line)
	inpipe.close()
	code = outpipe.close()
	if (code):
		raise RuntimeError('Command failed. Return code = '+str(code))
	
def delete_archive_of(filename):
	archivename = filename+utils.EXTENSION_ARCHIVE
	if os.path.exists(archivename):
		os.remove(archivename)
		
	checksumfile = get_checksum_filename(archivename)
	if os.path.exists(checksumfile):
		os.remove(checksumfile)

def compress_and_checksum(filename, arcname=None):
	archivename = filename+utils.EXTENSION_ARCHIVE
	
	if is_newer_file(filename, archivename):	
		print "	zipping:", filename
		
		if not arcname:
			arcname = os.path.basename(filename)
		archive = ZipFile(archivename, mode="w", compression=ZIP_DEFLATED)
		try:
			archive.write(filename, arcname=arcname)	
			return get_checksum(archivename)		
		finally:
			archive.close()
	

def listfiles_recursive(basedir, currentdir, filelist): #makes a recursive file list, with paths starting from currentdir
	for name in os.listdir(basedir):
		filepath = os.path.join(basedir, name)
		relativepath = os.path.join(currentdir, name)
		if os.path.isdir(filepath):
			listfiles_recursive(filepath, relativepath, filelist)
		elif os.path.isfile(filepath):
			#print "appending", filepath, "to", filelist
			filelist.append(relativepath)
	return filelist
