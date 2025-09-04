## Global imports
import os
from typing import Union
from abc import abstractmethod
import tkinter as tk
## Import UKS.dll from C# modules
import clr
clr.AddReference("UKS")
from UKS import *
uks = None
try:
    uks = UKS()
except Exception as e:
    print(e)


class ViewBase(object):
    def __init__(self, 
                 title: str, 
                 level: Union[tk.Tk, tk.Toplevel],
                 module_type: str,
                 uks=uks) -> None:
        self.uks = uks
        self.level = level
        self.level.title(title)
        #self.level.transient()
        self.level.iconbitmap(os.path.join(os.getcwd(), "iconsmall.ico"))
        ## Set UI params
        self.module_type = module_type
        self.label = ""
        #for future resize event capture
        self.window_width = None
        self.window_height = None
        self.window_x = None
        self.window_y = None
        #BUG...if you enable the following line, the WINDOWS program will crash if you move/resize a window
        #self.level.bind("<Configure>",self.resize)
        
    def setLabel(self, new_label: str):
        self.label = new_label

    def resize(self,event):
        #breakpoint()
        #this actually receives ALL the configuration events...so we have to sort out resize/move and the event source
        if hasattr(event.widget, "widgetName"):
            pass
        else: #if it doesn't have a name, it must be top level
            if (self.window_width != event.width) or (self.window_height != event.height):
                #if height/width changed, it must be resize
                self.window_width, self.window_height = event.width,event.height
            if (self.window_x != event.x ) or (self.window_y != event.y):
                self.window_x, self.window_y = event.x,event.y
            print(self.module_type, self.label)
            print(self.level.winfo_geometry())
            #TODO Add code to update values in UKS

    def close(self):
        self.level.destroy()
        

    


    @abstractmethod
    def build(self):
        ...
    
    @abstractmethod
    def fire(self):
        ...
