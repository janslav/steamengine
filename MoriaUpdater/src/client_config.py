import os
from ConfigParser import ConfigParser, DEFAULTSECT

from client_utils import get_uodir_from_registry 


CONFIG_FILENAME = "MoriaUpdater.conf"




config = ConfigParser({
					'UseProxy': 'False',
					'Proxy':'http://yourproxy.com',
					'UoPath':get_uodir_from_registry(),
					'DeleteVerdata':'True'					
					})

config.read(CONFIG_FILENAME)

use_proxy = config.getboolean(DEFAULTSECT, 'UseProxy')
proxy = config.get(DEFAULTSECT, 'Proxy')
uo_path = os.path.abspath(os.path.normpath(config.get(DEFAULTSECT, 'UoPath')))
delete_verdata = config.getboolean(DEFAULTSECT, 'DeleteVerdata')


f = file(CONFIG_FILENAME, mode="w")
try:
	config.write(f)
finally:
	f.close()



if __name__ == '__main__':
	print "uo_path:", uo_path
	
	
	os.system("pause")