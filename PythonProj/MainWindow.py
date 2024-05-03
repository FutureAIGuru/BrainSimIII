
from multiprocessing.util import spawnv_passfds
from tkinter import *
from PIL import Image, ImageTk


root = Tk()
root.title("Main Window")
root.geometry("600x600")
root.configure(background="black")

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

uks.GetOrAddThing("BrainSim",None)
uks.GetOrAddThing("AvailableModule","BrainSim")
uks.GetOrAddThing("UKSTreeView","AvailableModule")
uks.GetOrAddThing("AddStatement","AvailableModule")
uks.GetOrAddThing("AddClause","AvailableModule")
uks.GetOrAddThing("Query","AvailableModule")
uks.GetOrAddThing("Vision","AvailableModule")
uks.GetOrAddThing("Hearing","AvailableModule")



class MainWindow(Frame):
    def __init__(self, master, *pargs):
        Frame.__init__(self, master, *pargs)

        self.build()
   
    def build(self):
        self.image = Image.open("C:\\Users\\c_sim\\source\\repos\\BrainSimIIINEW\\BrainSimulator\\BrainSim3SplashScreen.jpg")
        self.img_copy= self.image.copy()

        self.background_image = ImageTk.PhotoImage(self.image)

        self.background = Label(self, image=self.background_image)
        self.background.pack(fill=BOTH, expand=YES)
        self.background.bind('<Configure>', self._resize_image)

        self.button0 = Button(self,text="Open",command=self.click)
        self.button0.place(x=80,y=50)

        self.moduleList = Listbox(self, height = 10, 
                  width = 15, 
                  bg = "grey",
                  activestyle = 'dotbox', 
                  font = "Helvetica",
                  fg = "yellow")
        index =0 
        for module in uks.Labeled("AvailableModule").Children:
            self.moduleList.insert(index,module.Label)
            index = index+1
        self.moduleList.place(x=0,y=0)
        #self.moduleList.pack(side=LEFT,expand=True)
        self.moduleList.bind("<<ListboxSelect>>", self.moduleClicked)

    # dummy to create a new window when clicked
    def click(self):
        new_window = Toplevel(self)
        new_window.transient(self)
        
    def moduleClicked(self,event):
        selection = event.widget.curselection()
        if selection:
            index = selection[0]
            data = event.widget.get(index)
            if data[-1] == "*":
                data = data[:-1]
            else:
                data = data + "*"
            event.widget.delete(index)
            event.widget.insert(index,data)

    def _resize_image(self,event):

        new_width = event.width
        new_height = event.height
        image_width = self.background_image.width() 
        image_height = self.background_image.height()
        scaleX = new_width / image_width
        scaleY = new_height / image_height
        
        if scaleX < scaleY:
            new_width = image_width * scaleY
        else:
            new_height = image_height * scaleX
        self.image = self.img_copy.resize((int(new_width), int(new_height)))

        self.background_image = ImageTk.PhotoImage(self.image)
        self.background.configure(image =  self.background_image)
        self.moduleList.height = new_height



e = MainWindow(root)
e.pack(fill=BOTH, expand=YES)


root.mainloop()


"""

import tkinter as tk
from tkinter import ttk



class mainWIndow:
    def __init__ (self) -> None:
        self.build()
    def build(self):
        root = tk.Tk()
        root.title("Main Winwdow")
        root.mainloop()
        pass

        
mw = mainWIndow()

"""