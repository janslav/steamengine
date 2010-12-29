import os 
import sys
from zipfile import ZipFile, ZIP_DEFLATED

import gnosis.xml.pickle

import update_info as ui
import utils
from client_gui import facade
import logging

SERVER_URL="file://D:/SE/storage"


def main(arg=None):
	logging.basicConfig(level=logging.INFO)
	
	facade.start_gui(work)
	
	
def work():
	facade.set_progress_current(50)
	facade.set_progress_overall(20)
	
	import time
	time.sleep(1)

	logging.info("info line")
	
	time.sleep(1)
	raise Exception("exception text")
	

if __name__ == '__main__': 
	main()