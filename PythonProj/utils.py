## Global imports
import os
import sys
from typing import Union
from abc import abstractmethod
import tkinter as tk
## Import UKS.dll from C# modules
import pythonnet
pythonnet.load("coreclr")
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
        self.level.iconbitmap(os.path.join(os.getcwd(), "iconsmall.ico"))
        
        self.window_width = None
        self.window_height = None
        self.window_x = None
        self.window_y = None
        self.module_type = module_type
        self.label = ""
        #self.level.bind("<Configure>",self.resize)
        
    def setLabel(self,newlabel):
        self.label=newlabel

    def resize(self,event):
        #this actually receives ALL the configuration events...so we have to sort out resize/move and the event source
        if hasattr(event.widget, 'widgetName'):
            pass
        else: #if it doesn't have a name, it must be top level
            if (self.window_width != event.width) or (self.window_height != event.height):
                #if height/width changed, it must be resize
                self.window_width, self.window_height = event.width,event.height
            if (self.window_x != event.x ) or (self.window_y != event.y):
                self.window_x, self.window_y = event.x,event.y
            print(self.module_type, self.label)
            print(self.level.winfo_geometry())

    def setGeometryString():
        pass    
    


    @abstractmethod
    def build(self):
        ...
    
    @abstractmethod
    def fire(self):
        ...
