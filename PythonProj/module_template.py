import sys, os
from time import time_ns
## Global imports
from typing import Union
import tkinter as tk
## Local imports
from utils import ViewBase

#Brain Simulator III Python Module
#Do a global search/replace for "ViewTemplate" with your class name
#Fill in the areas where functional code is needed

#the title which shows in the dialog titlebar
TITLE = "Your Window TitleBar Entry HERE"
#the minimum delay (in seconds) between successive calls to the self.fire method
TIME_DELAY = 0

class ViewTemplate(ViewBase):
    def __init__(self, level: Union[tk.Tk, tk.Toplevel]) -> None:
        title: str = TITLE
        super(ViewTemplate, self).__init__(
            title=title, level=level, module_type=os.path.basename(__file__))
        ## Set up any callbacks
        self.build()
    
    def build(self):
        ## Create the window
        self.level.geometry("300x250+100+100")
        
        #Put the widget-creation for the dialog HERE

        #needed for stand-alone debugging
        if sys.argv[0]  != "":
            self.level.mainloop()

    ############
    ##  Fire  ##
    ############

    def fire(self) ->bool:
        #Put your functional code HERE
        #This function is called repeateldly so you may wish to do things only on a timer like this:
        if self.update_paused:
            return True
        curr_time: float = time.time()
        try:
            if curr_time > (self.prev_time + TIMEDELAY): 
                #do your stuff HERE
                self.prev_time = curr_time
        except Exception:
            self.prev_time = curr_time
        #you always nee this:
        self.level.update()
        return self.level.winfo_exists()


######################
##  Exposed Methods  ##
######################

def Init():
    global view
    view = ViewTemplate(level=tk.Tk())

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





