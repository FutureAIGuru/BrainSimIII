## Global imports
import sys, os
from typing import List, Union
import time  # time needed for refresh()
import tkinter as tk
import tkinter.ttk as ttk
## Local imports
from utils import ViewBase
from tkinter.filedialog import askopenfile,asksaveasfile

titleBase = "The Brain Simulator III"
TIMEDELAY = 1

class MainWindow(ViewBase):
    def __init__(self, level: Union[tk.Tk, tk.Toplevel]) -> None:
        title: str = titleBase
        super(MainWindow, self).__init__(
            title=title, level=level, module_type=os.path.basename(__file__))
        self.setupUKS()
        self.build()

   
    def build(self):
        self.level.protocol("WM_DELETE_WINDOW",self.onClosing) #trap the "X" in the window upper-right
        self.moduleList = tk.Listbox(master=self.level, 
                                  activestyle="dotbox", 
                                  bg="grey", fg="yellow", 
                                  font="Helvetica", 
                                  height=10, width = 45)
        self.moduleList.pack(side='top',expand=1,fill='x')
        self.moduleList.bind("<<ListboxSelect>>", self.moduleClicked)
        self.openButton = tk.Button(master=self.level,text="Open",command=self.openFile,width=10)        
        self.saveButton = tk.Button(master=self.level,text="Save",command=self.saveFile,width=10)        
        self.saveAsButton = tk.Button(master=self.level,text="SaveAs",command=self.saveAsFile,width=10)        
        self.openButton.pack(side='left',padx=50,pady=20)
        self.saveAsButton.pack(side='right',padx=50)
        self.saveButton.pack(side='top',pady=20)
        self.setupcontent()

        if sys.argv[0] != "":
            self.level.mainloop()
        
    def setupcontent(self):
        self.moduleList.delete(0,'end')
        activeModules = self.uks.Labeled("ActiveModule").Children
        for idx, module in enumerate(self.uks.Labeled("AvailableModule").Children):
            labelToAdd = module.Label
            if "main" in labelToAdd.lower():
                continue
            if "template" in labelToAdd.lower():
                continue
            active = False
            for m1 in activeModules:
                if labelToAdd in m1.Label:
                    active = True
            if active:
                labelToAdd += "*"
            if ".py" in labelToAdd:
                self.moduleList.insert(idx, labelToAdd)

    ###########################
    ##   FILE Methods        ##
    ###########################
            
    def openFile(self):
        file = askopenfile(mode='r', 
                           title='Load UKS Content File',
                           parent=self.level,
                           filetypes=[("XML files","*.xml")])
        if file is not None:
            fileName = file.name        
            file.close()
            self.uks.LoadUKSfromXMLFile(fileName)
            self.setupUKS()
            if self.uks.Labeled("MainWindow.py") == None:
                self.uks.AddThing("MainWindow.py", self.uks.Labeled("AvailableModule"));
            self.activateModule("MainWindow.py")
            self.level.title(titleBase +'  --  ' +os.path.basename(fileName))
            self.setupcontent()
            print ("File Loaded: ",fileName)

    #Add necessary status info to older UKS if needed
    def setupUKS(self):
        if self.uks.Labeled("BrainSim") == None:
            self.uks.AddThing("BrainSim",None)
        self.uks.GetOrAddThing("AvailableModule","BrainSim")
        self.uks.GetOrAddThing("ActiveeModule","BrainSim")
        if self.uks.Labeled("AvailableModule").Children.Count == 0:
            python_modules = os.listdir(".")
            for module in python_modules:
                if module.startswith("m") and module.endswith(".py"):
                    self.uks.GetOrAddThing(module,"AvailableModule")

        
        
            
    def saveFile(self):
        if self.uks.FileName == "":
            self.saveAsFile()
        else:            
            self.uks.SaveUKStoXMLFile(self.uks.FileName)
            print ("File Saved: ",self.uks.FileName)

    def saveAsFile(self):
        file = asksaveasfile(mode='w', 
                             title='Save UKS Content to File',
                             parent=self.level,
                             filetypes=[("XML files","*.xml")],
                             defaultextension="*.*")
        if file is not None:
            fileName = file.name        
            file.close()
            self.uks.SaveUKStoXMLFile(fileName)
            print ("File Saved As: ",fileName)
            self.level.title(titleBase +'  --  ' + os.path.basename(fileName))
        
        
    ###########################
    ##   event handlers      ##
    ###########################
    def moduleClicked(self,event):
        selection = event.widget.curselection()
        if selection:
            idx = selection[0]
            data = event.widget.get(idx)
            if data[-1] == "*":
                data = data[:-1] #strip off the asterisk
                self.deactivateModule(data+'0')
            else:
                self.activateModule(data)
            self.setupcontent()
            
    def deactivateModule(self,moduleLabel):
        print ("deactivating ",moduleLabel)
        thingToDeactivate= self.uks.Labeled(moduleLabel)
        if thingToDeactivate != None:
            self.uks.DeleteAllChildren(thingToDeactivate)
            self.uks.DeleteThing(thingToDeactivate)
    def activateModule(self,moduleTypeLabel):
        print ("activating ",moduleTypeLabel)
        thingToActivate= self.uks.Labeled(moduleTypeLabel)
        if thingToActivate.Children.Count > 0:
            return
        if thingToActivate != None:
            newModule = self.uks.CreateInstanceOf(thingToActivate)
            newModule.AddParent(self.uks.Labeled("ActiveModule"))

            
    def onClosing(self):
        print ("MainWindow closing")    
        os._exit(0)
            
    def fire(self):
        #Put your functional code HERE
        #This function is called repeateldly so you may wish to do things only on a timer like this:
        curr_time: float = time.time()
        try:
            if curr_time > (self.prev_time + TIMEDELAY): 
                #do your stuff HERE
                self.prev_time = curr_time
        except Exception:
            self.prev_time = curr_time
        #you always nee this:
        self.setupcontent()
        self.level.update()
        #don't ever close this module while the program is running
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

