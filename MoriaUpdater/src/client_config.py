from ConfigParser import ConfigParser, DEFAULTSECT


CONFIG_FILENAME = "MoriaUpdater.conf"




config = ConfigParser({
					'UseProxy':False,
					'Proxy':'http://yourproxy.com',
					'uoPath':'',					
					})

config.read(CONFIG_FILENAME)

use_proxy = config.getboolean(DEFAULTSECT, 'UseProxy')
proxy = config.get(DEFAULTSECT, 'Proxy')


if __name__ == '__main__': 	
	os.system("pause")