
def __load():
    import imp, os, sys
    dirname = sys.prefix
    path = os.path.join(dirname, '_zope_interface_coptimizations.pyd')
    #print "py2exe extension module", __name__, "->", path
    mod = imp.load_dynamic(__name__, path)
##    mod.frozen = 1
__load()
del __load
