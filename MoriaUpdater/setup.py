from distutils.core import setup
import py2exe


setup(
#    version = "0.5.0",
#    description = "py2exe user-access-control sample script",
#    name = "py2exe samples",
    
    options = {"py2exe": {"compressed": 1,
                          "optimize": 2,
                          #"ascii": 1,
                          "bundle_files": 1}},
    zipfile = None,
    windows = [dict(script="moriaUpdaterClient.py",
          dest_base="MoriaUpdater",
          uac_info="requireAdministrator")]
    )



