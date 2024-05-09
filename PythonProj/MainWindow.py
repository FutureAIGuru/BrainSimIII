## Global imports
import sys, os
from typing import List, Union
import time  # time needed for refresh()
import tkinter as tk
import tkinter.ttk as ttk
## Local imports
from utils import ViewBase




class MainWindow(ViewBase):
    def __init__(self, level: Union[tk.Tk, tk.Toplevel]) -> None:
        title: str = "The Brain Simulator III"
        super(MainWindow, self).__init__(
            title=title, level=level, module_type=os.path.basename(__file__))
        #put in a little test data if none exists
        if self.uks.Labeled("BrainSim") == None:
            self.uks.GetOrAddThing("BrainSim",None)
            self.uks.GetOrAddThing("AvailableModule","BrainSim")
            self.uks.GetOrAddThing("UKSTreeView","AvailableModule")
            self.uks.GetOrAddThing("AddStatement","AvailableModule")
        self.build()

   
    def build(self):
        self.moduleList = tk.Listbox(master=self.level, 
                                  activestyle="dotbox", 
                                  bg="grey", fg="yellow", 
                                  font="Helvetica", 
                                  height=10, width = 45)
        activeModules = self.uks.Labeled("ActiveModule").Children
        for idx, module in enumerate(self.uks.Labeled("AvailableModule").Children):
            labelToAdd = module.Label
            active = False
            for m1 in activeModules:
                if labelToAdd in m1.Label:
                    active = True
            if active:
                labelToAdd += "*"
            if ".py" in labelToAdd:
                self.moduleList.insert(idx, labelToAdd)
        self.moduleList.pack(side='left',expand=False,fill='y')
        self.moduleList.bind("<<ListboxSelect>>", self.moduleClicked)
        if sys.argv[0] != "":
            self.level.mainloop()
            

    # dummy to create a new window when clicked
    def click(self):
        new_window = Toplevel(self)
        new_window.transient(self)
        
    def moduleClicked(self,event):
        selection = event.widget.curselection()
        if selection:
            idx = selection[0]
            data = event.widget.get(idx)
            if data[-1] == "*":
                data = data[:-1]
                thingToDelete = self.uks.Labeled(data+'0')
                if thingToDelete != None:
                    self.uks.DeleteAllChildren(thingToDelete)
                    self.uks.DeleteThing(thingToDelete)
                    print ("deactivating ",data)

            else:
                self.uks.GetOrAddThing(data+'0',"ActiveModule")
                self.uks.AddStatement(data+'0',"is-a",data)
                print ("activating ",data)
                data = data + "*"
            event.widget.delete(idx)
            event.widget.insert(idx, data)
            
    def fire(self):
        self.level.update()
        return True


######################
##  Expose Methods  ##
######################

def Init():
    global view
    view = MainWindow(level=tk.Tk())

def Fire() -> bool:    
    return view.fire()
    
def GetHWND() -> int:
    hwnd = view.level.frame()
    return hwnd

def SetLabel(label):
    view.setLabel(label)

if sys.argv[0]  != "":
    Init()

