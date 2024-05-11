from argparse import FileType
from fileinput import filename
import sys, os
## Global imports
from typing import Union
import tkinter as tk
## Local imports
from utils import ViewBase
from tkinter.filedialog import askopenfile

#Brain Simulator III Python Module
#Do a global search/replace for "ViewOpenFileDlg" with your class name
#Fill in the areas where functional code is needed

class ViewOpenFileDlg(ViewBase):
    def __init__(self, level: Union[tk.Tk, tk.Toplevel]) -> None:
        title: str = "Your Window TitleBar Entry HERE"
        super(ViewOpenFileDlg, self).__init__(
            title=title, level=level, module_type=os.path.basename(__file__))
        ## Set up any callbacks
        self.build()
    
    def build(self):
        ## Create the window
        self.level.geometry("300x250+100+100")
        
        #Put the widget-creation for the dialog box HERE
        file = askopenfile(mode='r', title='Load UKS Content File',parent=self.level,filetypes=[("XML files","*.xml")])
        fileName = file.name        
        file.close()
        if file is not None:
            self.uks.LoadUKSfromXMLFile(fileName)
        #needed for stand-alone debugging
        self.level.destroy()
        if sys.argv[0]  != "":
            self.level.mainloop()
            

    ############
    ##  Fire  ##
    ############

    def fire(self) ->bool:
        #Put your functional code HERE
        self.level.update()
        return self.level.winfo_exists()


######################
##  Exposed Methods  ##
######################

def Init():
    global view
    view = ViewOpenFileDlg(level=tk.Tk())

def Fire() -> bool:
    return view.fire()
    
def GetHWND() -> int:
    hwnd = view.level.frame()
    return hwnd

def SetLabel(label: str):
    view.setLabel(label)
    
def Close():
    view.close()

if sys.argv[0]  != "":
    Init()





