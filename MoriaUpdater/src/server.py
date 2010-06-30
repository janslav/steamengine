import os 
import sys
from zipfile import ZipFile, ZIP_DEFLATED

import gnosis.xml.pickle

import update_info as ui
import utils

from server_utils import diff, compress_and_checksum

DIRNAME_FTPROOT = "storage"
FILENAME_NOFORCE = "noforce"
FILENAME_TODELETE = "todelete"



_localdir = os.path.dirname(sys.argv[0])
_ftproot = os.path.join(_localdir, DIRNAME_FTPROOT)

_releasesdir = os.path.join(_ftproot, ui.DIRNAME_RELEASES)
_patchesdir = os.path.join(_ftproot, ui.DIRNAME_PATCHES)

#_updaterfile = os.path.join(FTPROOT, UPDATER, "library.zip")

def main(arg=None):
	#first, make a list of the files to patch
	uiobj = ui.UpdateInfo("moria update files")
	
	print "creating list of files"
	list_releases(uiobj)
	list_originals(uiobj)
	
	print "reading noforce and todelete config files"
	read_additional_config(uiobj)
	
	print "creating patches"
	create_patches_successive(uiobj) #patches between releases
	create_patches_fromoriginals(uiobj) #patches from originals to latest
	
	print "creating moriapack"
	create_pack(uiobj)
	
	
	print "writing "+utils.UPDATEINFO
	uifilename = os.path.join(_ftproot, ui.FILENAME_UPDATEINFO)	
	gnosis.xml.pickle.dump(uiobj, file(uifilename, "w"))
	compress_and_checksum(uifilename)

	print "done"
	
def list_releases(uiobj):
	dirs_releases = os.listdir(_releasesdir)
	uiobj.lastversion = dirs_releases[0]
	for directory in dirs_releases:
		release_path = os.path.join(_releasesdir, directory)
		release_name = os.path.join(ui.DIRNAME_RELEASES, directory)
		for filename in makefilelist(release_path, "", []):
			if not utils.is_helper_filename(filename): #we are skipping the zipped ones and md5s
				filepath = os.path.join(release_path, filename)
				checksum = utils.get_checksum(filepath)
				version = ui.FileVersionInfo(release_name, checksum)
				
				fi = ui.ui_getfilebyname(uiobj, filename)
				if (fi == None):
					fi = ui.FileInfo(filename)
					ui.ui_addfile(uiobj, fi)
				ui.fi_addversion(fi, version)

def makefilelist(basedir, currentdir, filelist): #makes a recursive file list, with paths starting from currentdir
	for name in os.listdir(basedir):
		filepath = os.path.join(basedir, name)
		relativepath = os.path.join(currentdir, name)
		if os.path.isdir(filepath):
			makefilelist(filepath, relativepath, filelist)
		elif os.path.isfile(filepath):
			#print "appending", filepath, "to", filelist
			filelist.append(relativepath)
	return filelist
	
def list_originals(uiobj):
	originalsdir = os.path.join(_ftproot, ui.DIRNAME_ORIGINALS)
	for directory in os.listdir(originalsdir):
		original_path = os.path.join(originalsdir, directory)
		original_name = os.path.join(ui.DIRNAME_ORIGINALS,directory)
		for fi in uiobj.files.values(): #walking the list of files from releases, looking for them in originals
			filepath = os.path.join(original_path, fi.filename)
			if os.path.exists(filepath):
				checksum = utils.get_checksum(filepath)
				version = ui.FileVersionInfo(original_name, checksum)
				version.isoriginal = True
				fi.addversion(version)

def create_patches_successive(uiobj):	
	for fi in uiobj.files.values():
		if not fi.todelete:
			if len(fi.versions_sorted) > 1: #at least 2 versions = there's something to patch
				for i in range(0, len(fi.versions_sorted) - 1):
					oldver = fi.versions_sorted[i]
					newver = fi.versions_sorted[i+1]
					patchfilename = create_patch_if_needed(fi.filename, oldver, newver)				
					oldver.patchchecksum = utils.get_checksum(patchfilename)
					
def create_patches_fromoriginals(uiobj):	
	for fi in uiobj.files.values():
		if not fi.todelete:
			latestver = fi.versions_sorted[-1]
			for origver in fi.versions_byname.values():
				if origver.isoriginal:
					
					#delete possible old patches first
					if len(fi.versions_sorted) > 1: #at least 2 release versions = there could be old patch
						for i in range(0, len(fi.versions_sorted) - 1): #all but the last one
							delete_patch(fi.filename, origver, fi.versions_sorted[i])
							
					#create patch to latest release
					patchfilename = create_patch_if_needed(fi.filename, origver, latestver)				
					origver.patchchecksum = utils.get_checksum(patchfilename)
				
def delete_patch(filename, origversion, newversion):
	patchpath = os.path.join(_patchesdir, utils.get_patchname(filename, origversion.name, newversion.name))
	if os.path.exists(patchpath):
		os.remove(patchpath)
	patchchecksumfile = utils.get_checksum_filename(patchpath)
	if os.path.exists(patchchecksumfile):
		os.remove(patchchecksumfile)
			
def create_patch_if_needed(filename, origversion, newversion): #returns name of the patch file
	patchpath = os.path.join(_patchesdir, utils.get_patchname(filename, origversion.name, newversion.name))
	patchdir = os.path.dirname(patchpath)
	if not os.path.exists(patchdir):
		os.makedirs(patchdir)
	if os.path.exists(patchpath):
		pass #the patch already exists, we do nothing
	else:
		origpath = os.path.join(_ftproot, origversion.name, filename)
		newpath = os.path.join(_ftproot, newversion.name, filename)
		diff(origpath, newpath, patchpath)
	return patchpath
			
def create_pack(uiobj):
	packname = os.path.join(_ftproot, utils.ARCHIVENAME+utils.EXTENSION_ARCHIVE)
	try:		
		archive = ZipFile(packname, mode="w", compression=ZIP_DEFLATED)
		for fi in uiobj.files.values():
			if not fi.todelete:
				latestver = fi.versions_sorted[-1]
				completefilename = os.path.join(_ftproot, latestver.name, fi.filename)		
				archive.write(completefilename, arcname=fi.filename)
	finally:
		archive.close()

def read_additional_config(uiobj):
	noforcefile = os.path.join(_ftproot, FILENAME_NOFORCE)
	try:
		f = open(noforcefile)
		for line in f:
			fi = ui.ui_getfilebyname(uiobj, line.strip().lower())
			if fi != None:
				fi.forced = False
	finally:
		f.close()
	
	
	todelfile = os.path.join(_ftproot, FILENAME_TODELETE)
	try:
		f = open(todelfile)
		for line in f:
			filename = line.strip()
			fi = ui.ui_getfilebyname(uiobj, filename.lower())
			if fi != None:
				fi.todelete = True
			else:
				fi = ui.FileInfo(filename)
				fi.todelete = True
				ui.ui_addfile(uiobj, fi)
	finally:
		f.close()

	
	
	
	
if __name__ == '__main__': 
	main()