ui.tcl je ve skutecnosti neco jako projektovej soubor 
pro "visual tcl" resp. "page", kterej se otevre a na zaklade toho 
se vygeneruje python, kterej se pak uklada do __init__.py


pouze se tam musi pridat tenhle kousek kodu, bo samotnej TK neumi png



from PIL import ImageTk 
PhotoImage = ImageTk.PhotoImage