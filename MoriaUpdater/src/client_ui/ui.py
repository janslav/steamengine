#! /usr/bin/env python
# -*- python -*-

import sys

py2 = py30 = py31 = False
version = sys.hexversion
if version >= 0x020600F0 and version < 0x03000000 :
    py2 = True    # Python 2.6 or 2.7
    from Tkinter import *
    import ttk
elif version >= 0x03000000 and version < 0x03010000 :
    py30 = True
    from tkinter import *
    import ttk
elif version >= 0x03010000:
    py31 = True
    from tkinter import *
    import tkinter.ttk as ttk
else:
    print ("""
    You do not have a version of python supporting ttk widgets..
    You need a version >= 2.6 to execute PAGE modules.
    """)
    sys.exit()



from PIL import ImageTk 
PhotoImage = ImageTk.PhotoImage

'''

    If you use the following functions, change the names 'w' and
    'w_win'.  Use as a template for creating a new Top-level window.

w = None
def create_Moria_Updater ()
    global w
    global w_win
    if w: # So we have only one instance of window.
        return
    w = Toplevel (root)
    w.title('Moria Updater')
    w.geometry('465x300+576+197')
    w_win = Moria_Updater (w)

   Template for routine to destroy a top level window.

def destroy():
    global w
    w.destroy()
    w = None
'''

def vp_start_gui():
    global val, w, root
    root = Tk()
    root.title('Moria_Updater')
    root.geometry('465x300+576+197')
    w = Moria_Updater (root)
    init()
    root.mainloop()



def init():
    pass

def xxx() :
    pass





class Moria_Updater:
    def __init__(self, master=None):
        # Set background of toplevel window to match
        # current style
        style = ttk.Style()
        theme = style.theme_use()
        default = style.lookup(theme, 'background')
        master.configure(background=default)



        self.tLa35 = ttk.Label (master)
        self.tLa35.place(relx=-0.01,rely=0.0)
        self._img1 = PhotoImage(file="D:/SE/MoriaUpdater/src/client_ui/background.png")
        self.tLa35.configure(image=self._img1)

        self.scr33 = ScrolledText (master)
        self.scr33.place(relx=0.02,rely=0.27,relheight=0.6,relwidth=0.78)
        self.scr33.configure(background="white")
        self.scr33.configure(height="3")
        self.scr33.configure(width="10")

        self.tPr36 = ttk.Progressbar (master)
        self.tPr36.place(relx=0.02,rely=0.9,relheight=0.07,relwidth=0.77)






# The following code is added to facilitate the Scrolled widgets you specified.
class AutoScroll(object):
    '''Configure the scrollbars for a widget.'''

    def __init__(self, master):
        vsb = ttk.Scrollbar(master, orient='vertical', command=self.yview)
        hsb = ttk.Scrollbar(master, orient='horizontal', command=self.xview)

        self.configure(yscrollcommand=self._autoscroll(vsb),
            xscrollcommand=self._autoscroll(hsb))
        self.grid(column=0, row=0, sticky='nsew')
        vsb.grid(column=1, row=0, sticky='ns')
        hsb.grid(column=0, row=1, sticky='ew')

        master.grid_columnconfigure(0, weight=1)
        master.grid_rowconfigure(0, weight=1)

        # Copy geometry methods of master  (took from ScrolledText.py)
        methods = Pack.__dict__.keys() + Grid.__dict__.keys() \
                  + Place.__dict__.keys()

        for meth in methods:
            if meth[0] != '_' and meth not in ('config', 'configure'):
                setattr(self, meth, getattr(master, meth))

    @staticmethod
    def _autoscroll(sbar):
        '''Hide and show scrollbar as needed.'''
        def wrapped(first, last):
            first, last = float(first), float(last)
            if first <= 0 and last >= 1:
                sbar.grid_remove()
            else:
                sbar.grid()
            sbar.set(first, last)
        return wrapped

    def __str__(self):
        return str(self.master)

def _create_container(func):
    '''Creates a ttk Frame with a given master, and use this new frame to
    place the scrollbars and the widget.'''
    def wrapped(cls, master, **kw):
        container = ttk.Frame(master)
        return func(cls, container, **kw)
    return wrapped

class ScrolledText(AutoScroll, Text):
    '''A standard Tkinter Text widget with scrollbars that will
    automatically show/hide as needed.'''
    @_create_container
    def __init__(self, master, **kw):
        Text.__init__(self, master, **kw)
        AutoScroll.__init__(self, master)

if __name__ == '__main__':
    vp_start_gui()



