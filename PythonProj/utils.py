## Global imports
import os
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
                 uks=uks) -> None:
        self.uks = uks
        self.level = level
        self.level.title(title)
        self.level.iconbitmap(os.path.join(os.getcwd(), "iconsmall.ico"))

    @abstractmethod
    def build(self):
        ...
    
    @abstractmethod
    def fire(self):
        ...
