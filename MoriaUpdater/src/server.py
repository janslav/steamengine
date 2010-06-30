import os 
import sys
from zipfile import ZipFile, ZIP_DEFLATED

import gnosis.xml.pickle

import update_info as ui
import utils
import server_utils

DIRNAME_FTPROOT = "storage"
FILENAME_NOFORCE = "noforce"
FILENAME_TODELETE = "todelete"

FILENAME_PACK = "moriapack"

_localdir = os.path.dirname(sys.argv[0])
_ftproot = os.path.join(_localdir, DIRNAME_FTPROOT)

_releasesdir = os.path.join(_ftproot, utils.DIRNAME_RELEASES)
_patchesdir = os.path.join(_ftproot, utils.DIRNAME_PATCHES)

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
	
	print "compressing latest files for direct downloading"
	compress_latest(uiobj)
	
	print "creating moriapack"
	create_pack(uiobj) #basically compresses the same files as compress_latest, 
		#could be somehow optimized, if anyone cared (I don't)
	
	print "writing "+utils.FILENAME_UPDATEINFO
	uifilename = os.path.join(_ftproot, utils.FILENAME_UPDATEINFO)
	f = file(uifilename, "w")
	try:		
		gnosis.xml.pickle.dump(uiobj, f)
	finally:
		f.close()
	server_utils.compress_and_checksum(uifilename)

	print "done"
	
def list_releases(uiobj):
	dirs_releases = os.listdir(_releasesdir)
	uiobj.lastversion = dirs_releases[0]
	for directory in dirs_releases:
		release_path = os.path.join(_releasesdir, directory)
		release_name = os.path.join(utils.DIRNAME_RELEASES, directory)
		for filename in server_utils.listfiles_recursive(release_path, "", []):
			if not server_utils.is_helper_filename(filename): #we are skipping the zipped ones and md5s
				filepath = os.path.join(release_path, filename)
				checksum = server_utils.get_checksum(filepath)
				version = ui.FileVersionInfo(release_name, checksum)
				
				fi = ui.ui_getfilebyname(uiobj, filename)
				if (fi == None):
					fi = ui.FileInfo(filename)
					ui.ui_addfile(uiobj, fi)
				ui.fi_addversion(fi, version)
	
def list_originals(uiobj):
	originalsdir = os.path.join(_ftproot, utils.DIRNAME_ORIGINALS)
	for directory in os.listdir(originalsdir):
		original_path = os.path.join(originalsdir, directory)
		original_name = os.path.join(utils.DIRNAME_ORIGINALS,directory)
		for fi in uiobj.files.values(): #walking the list of files from releases, looking for them in originals
			filepath = os.path.join(original_path, fi.filename)
			if os.path.exists(filepath):
				checksum = server_utils.get_checksum(filepath)
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
					patchfilename = server_utils.create_patch_if_needed(_ftproot, _patchesdir, \
						fi.filename, oldver, newver)				
					oldver.patchchecksum = server_utils.get_checksum(patchfilename)
					
def create_patches_fromoriginals(uiobj):	
	for fi in uiobj.files.values():
		if not fi.todelete:
			latestver = fi.versions_sorted[-1]
			for origver in fi.versions_byname.values():
				if origver.isoriginal:
					
					#delete possible old patches first
					if len(fi.versions_sorted) > 1: #at least 2 release versions = there could be old patch
						for i in range(0, len(fi.versions_sorted) - 1): #all but the last one
							server_utils.delete_patch(fi.filename, origver, fi.versions_sorted[i])
							
					#create patch to latest release
					patchfilename = server_utils.create_patch_if_needed(fi.filename, origver, latestver)				
					origver.patchchecksum = server_utils.get_checksum(patchfilename)

def compress_latest(uiobj):
	for fi in uiobj.files.values():
		if not fi.todelete:
			#delete possible unneeded archives first
			if len(fi.versions_sorted) > 1: #at least 2 release versions = there could be old archive
				for i in range(0, len(fi.versions_sorted) - 1): #all but the last one
					filename = os.path.join(_releasesdir, fi.versions_sorted[i].name, fi.filename) 
					server_utils.delete_archive_of(filename)
			
			latestver = fi.versions_sorted[-1]
			filename = os.path.join(_ftproot, latestver.name, fi.filename)
			latestver.archivechecksum = server_utils.compress_and_checksum(filename)

			
def create_pack(uiobj):
	packname = os.path.join(_ftproot, FILENAME_PACK+utils.EXTENSION_ARCHIVE)
	
	archive = ZipFile(packname, mode="w", compression=ZIP_DEFLATED)
	try:				
		for fi in uiobj.files.values():
			if not fi.todelete:
				latestver = fi.versions_sorted[-1]
				print "	appending:",  os.path.join(latestver.name, fi.filename)
				completefilename = os.path.join(_ftproot, latestver.name, fi.filename)						
				archive.write(completefilename, arcname=fi.filename)
	finally:
		archive.close()

def read_additional_config(uiobj):
	noforcefile = os.path.join(_ftproot, FILENAME_NOFORCE)
	
	f = open(noforcefile)
	try:
		for line in f:
			fi = ui.ui_getfilebyname(uiobj, line.strip().lower())
			if fi != None:
				fi.forced = False
	finally:
		f.close()
	
	
	todelfile = os.path.join(_ftproot, FILENAME_TODELETE)
	f = open(todelfile)
	try:		
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