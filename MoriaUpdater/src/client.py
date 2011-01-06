import os 
import sys
import urllib
from urlparse import urlparse, urljoin
from tempfile import mkdtemp
import logging
from shutil import rmtree
from zipfile import ZipFile, ZIP_DEFLATED

import gnosis.xml.pickle

import update_info as ui
import utils
from client_gui import facade
import client_config as config


if (config.use_proxy):	
	schema = urlparse(config.proxy)
	proxies = {schema: config.proxy}
	urllib._urlopener = urllib.FancyURLopener(proxies)
	

def main(arg=None):	
	logging.basicConfig(level=logging.INFO)	
	facade.start_gui(work)	
	
#fractions of download progress
PERCENTAGE_DOWNLOAD_WITHOUT_CHECKSUM=0.9
PERCENTAGE_UNPACK_WITHOUT_CHECKSUM=0.8

#first phase - getting updateinfo file
TOTALPERCENTAGE_UI = 0.01 * facade.PBMAX
TOTALPERCENTAGE_UICHECKSUM_DOWNLOAD = 0.1 * TOTALPERCENTAGE_UI
TOTALPERCENTAGE_UIFILE_DOWNLOAD = 0.5 * TOTALPERCENTAGE_UI
TOTALPERCENTAGE_UIFILE_UNPACK = 0.3	* TOTALPERCENTAGE_UI
TOTALPERCENTAGE_UIFILE_READ = 0.1 * TOTALPERCENTAGE_UI

TOTALPERCENTAGE_TODOLIST = 1.0

TOTALPERCENTAGE_CHECKSUMALL = 5.0

TIMEPERCENTAGE_DOWNLOADING = 5
TIMEPERCENTAGE_UNPACKING = 3
TIMEPERCENTAGE_PATCHING = 4

	
def work():
	tempdir = mkdtemp()
	
	try:
		totaldone = 0 
		
		logging.info("Stazeni metainfo souboru...")
		#download ui archive checksum file
		uiarchivechecksum_filename = utils.FILENAME_UPDATEINFO + utils.EXTENSION_ARCHIVE + utils.EXTENSION_CHECKSUM
		totaldone = download(tempdir, uiarchivechecksum_filename, TOTALPERCENTAGE_UICHECKSUM_DOWNLOAD, totaldone)
		with open(os.path.join(tempdir, uiarchivechecksum_filename), "r") as f:
			uiarchivechecksum = f.read()
		
		totaldone = download(tempdir, utils.FILENAME_UPDATEINFO + utils.EXTENSION_ARCHIVE, TOTALPERCENTAGE_UIFILE_DOWNLOAD, totaldone, uiarchivechecksum)
		totaldone = unpack(tempdir, utils.FILENAME_UPDATEINFO, TOTALPERCENTAGE_UIFILE_UNPACK, totaldone)
		
		with open(os.path.join(tempdir, utils.FILENAME_UPDATEINFO), "r") as f:
			uiobj = gnosis.xml.pickle.load(f)
		totaldone += TOTALPERCENTAGE_UIFILE_READ
		facade.set_progress_overall(totaldone)
		
		logging.info("Vytvoreni seznamu souboru ke zpracovani...")
		#list files that need to be processed
		todo = [] #list of (filename, checksum, fileinfo)
		totalsize = 0
		for filename in utils.listfiles_recursive(config.uo_path):
			fi = ui.ui_popfilebyname(uiobj, filename)
			if not (fi is None): 
				path_in_uo = os.path.join(config.uo_path, filename)
				size = os.path.getsize(path_in_uo)		
				totalsize += size
				todo.append((path_in_uo, float(size), fi))
		
		for fi in uiobj.files.values():
			if fi.forced:
				todo.append((None, None, fi))
								
		totaldone += TOTALPERCENTAGE_TODOLIST
		facade.set_progress_overall(totaldone)
		
		logging.info("Souboru ke zpracovani: "+str(len(todo)) + ", celk. velikost: " + str(totalsize / (1024*1024)) + "MB")
		
		logging.info("Kalkulace kontrolnich souctu souboru...")
		
		todownload = [] #(path_in_uo, filesize, fi)
		downloadsize = 0
		topatch = [] #(path_in_uo, filesize, fi, version)
		patchfilessize = 0
		todelete = [] #path_in_uo
		for (path_in_uo, filesize, fi) in todo:
			if fi.todelete:
				todelete.append(path_in_uo)
			else:
				if path_in_uo:
					sizefraction = filesize/totalsize
					TOTALPERCENTAGE_CHECKSUMALL
					def progress_callback(done):
						fraction = (done/filesize)
						facade.set_progress_current(facade.PBMAX * fraction)
						facade.set_progress_overall(totaldone + (sizefraction * fraction * TOTALPERCENTAGE_CHECKSUMALL))
					
					checksum = utils.calculate_checksum(path_in_uo, progress_callback) 
					totaldone += sizefraction * TOTALPERCENTAGE_CHECKSUMALL
				
				latestversion = fi.versions_sorted[-1]
				if (checksum != latestversion.checksum): #nemame spravnou verzi, musime resit
					version = ui.fi_getversionbychecksum(fi, checksum)
					if not (version is None):
						topatch.append((path_in_uo, filesize, fi, version))
						patchfilessize += version.patchsize
					else:
						todownload.append((path_in_uo, filesize, fi))
						downloadsize += latestversion.archivesize
		
		if todownload or topatch or todelete:
			logging.info("Souboru ke stazeni:"+str(len(todownload))+", k patchnuti:"+str(len(topatch))+", ke smazani:"+str(len(todelete)))
		else:
			logging.info("Vsechny soubory v aktualni verzi.")
			facade.set_progress_overall(facade.PBMAX)
		
		
		totalwork = TIMEPERCENTAGE_DOWNLOADING * (downloadsize + patchfilessize) + \
			TIMEPERCENTAGE_UNPACKING * downloadsize + TIMEPERCENTAGE_PATCHING * patchfilessize
		fraction_per_byte = totaldone / totalwork
		
		
		tomove = [] #(path_downloaded, path_in_uo, filesize)
			
		for (path_in_uo, filesize, fi) in todownload:
			latestversion = fi.versions_sorted[-1]
			
			#download archive
			remote_path = latestversion.versionname + '/' + latestversion.filename + utils.EXTENSION_ARCHIVE
			fraction = latestversion.archivesize * fraction_per_byte * TIMEPERCENTAGE_DOWNLOADING
			totaldone = download(tempdir, remote_path, fraction, totaldone, latestversion.archivechecksum)
			
			#archive downloaded, now unpack
			path_downloaded = os.path.join(latestversion.versionname, latestversion.filename)
			fraction = latestversion.archivesize * fraction_per_byte * TIMEPERCENTAGE_UNPACKING
			totaldone = unpack(tempdir, path_downloaded, fraction, totaldone, latestversion.checksum)
			
			tomove.append((os.path.join(tempdir, path_downloaded), path_in_uo, filesize))
			
			
	finally:
		pass
		#rmtree(tempdir)
		
	
#	facade.set_progress_current(50)
#	facade.set_progress_overall(20)

def download(localroot, remote_filepath, totalfraction, totaldone, checksum = None):
	logging.info("downloading file '" + remote_filepath + "'")

	if (checksum):
		percentage_download = PERCENTAGE_DOWNLOAD_WITHOUT_CHECKSUM
	else:
		percentage_download = 1.0
		
	def reporthook(blocknum, blocksize, totalsize):
		#print "Block number: %d, Block size: %d, Total size: %d" % (blocknum, blocksize, totalsize)
		totalsize = float(totalsize)
		fraction = ((blocknum * blocksize)/totalsize)
		if (fraction > 1):
			fraction = 1.0

		fraction *= percentage_download		
		facade.set_progress_current(facade.PBMAX * fraction)
		facade.set_progress_overall(totaldone + (totalfraction * fraction))
	
	remote_filepath.replace('\\', '/')
	url = urljoin(config.server_url, remote_filepath)
	local_filepath = os.path.join(localroot, remote_filepath)
	
	dir = os.path.dirname(local_filepath)
	if not os.path.exists(dir):
		os.makedirs(dir)
	
	(downloaded_filepath, _) = urllib.urlretrieve(url, local_filepath, reporthook)
	if downloaded_filepath != local_filepath:
		raise Exception("File downloaded to unexpected location")
		
	if not (checksum is None):
		filesize = float(os.path.getsize(local_filepath))
		totaldone_sofar = totaldone + (totalfraction * percentage_download)
		currentdone = facade.PBMAX * percentage_download
		
		def progress_callback(done):
			fraction = (done/filesize) * (1 - percentage_download)
			facade.set_progress_current(currentdone + (facade.PBMAX * fraction))
			facade.set_progress_overall(totaldone_sofar + (totalfraction * fraction))
			
		calculated = utils.calculate_checksum(local_filepath, progress_callback)
		raise_if_incorect_checksum(checksum, calculated)
	
	return totalfraction + totaldone


def unpack(localroot, filepath, totalfraction, totaldone, checksum = None):
	if (checksum):
		percentage_unpack = PERCENTAGE_UNPACK_WITHOUT_CHECKSUM
	else:
		percentage_unpack = 1.0
	
	targetpath = os.path.join(localroot, filepath)
	targetfilename = os.path.basename(targetpath)
	archivepath = targetpath + utils.EXTENSION_ARCHIVE

	block_size = 1024 * 1024

	z = ZipFile(archivepath)
	try:
		entry_info = z.getinfo(targetfilename)
		size = float(entry_info.file_size)
	
		i = z.open(targetfilename)
		with open(targetpath, 'wb') as o:
			try:
				offset = 0
				while True:
					b = i.read(block_size)
					l = len(b)
					if l == 0:
						break
					offset += l 
					fraction = (offset/size)
					facade.set_progress_current(fraction * facade.PBMAX)
					facade.set_progress_overall(totaldone + (fraction * totalfraction))
					o.write(b)
			finally:	
				i.close()
	finally:
		z.close()
			
	if not (checksum is None):
		filesize = float(os.path.getsize(targetpath))
		totaldone_sofar = totaldone + (totalfraction * percentage_unpack)
		currentdone = facade.PBMAX * percentage_unpack
		
		def progress_callback(done):
			fraction = (done/filesize) * (1 - percentage_unpack)
			facade.set_progress_current(currentdone + (facade.PBMAX * fraction))
			facade.set_progress_overall(totaldone_sofar + (totalfraction * fraction))
			
		calculated = utils.calculate_checksum(targetpath, progress_callback)
		
		raise_if_incorect_checksum(checksum, calculated)
			
	return totalfraction + totaldone

def raise_if_incorect_checksum(checksum, calculated):
	if not checksum == calculated:
		raise Exception("Incorrect checksum. Run the updater again.")
	
if __name__ == '__main__': 
	main()