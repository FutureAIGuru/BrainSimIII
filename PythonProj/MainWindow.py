import sys, os
from os.path import join, dirname
from multiprocessing.util import spawnv_passfds
from tkinter import *
from PIL import Image, ImageTk
import pythonnet
pythonnet.load("coreclr")
import clr
# clr.AddReference("UKS")  # this doesn't work on Yida's MAC
clr.AddReference("../UKS/bin/Debug/net8.0/UKS")  # only this works on Yida's MAC
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
        # self.image = Image.open("C:\\Users\\c_sim\\source\\repos\\BrainSimIIINEW\\BrainSimulator\\BrainSim3SplashScreen.jpg")
        self.image = Image.open(join(join(dirname(os.getcwd()), "BrainSimulator"),  # path/to/BrainSimulator/
                                     "BrainSim3SplashScreen.jpg"))  # path/to/BrainSim3SplashScreen.jpg
        self.img_copy= self.image.copy()
        self.background_image = ImageTk.PhotoImage(self.image)
        self.background = Label(self, image=self.background_image)
        self.background.pack(fill=BOTH, expand=YES)
        self.background.bind("<Configure>", self._resize_image)
        self.button0 = Button(self,text="Open", command=self.click)
        self.button0.place(x=80,y=50)
        self.moduleList = Listbox(master=self, 
                                  activestyle="dotbox", 
                                  bg="grey", fg="yellow", 
                                  font="Helvetica", 
                                  height=10, width = 15)
        for idx, module in enumerate(uks.Labeled("AvailableModule").Children):
            self.moduleList.insert(idx, module.Label)
        self.moduleList.place(x=0, y=0)
        #self.moduleList.pack(side=LEFT,expand=True)
        self.moduleList.bind("<<ListboxSelect>>", self.moduleClicked)

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
            else:
                data = data + "*"
            event.widget.delete(idx)
            event.widget.insert(idx, data)

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
        self.background.configure(image=self.background_image)
        self.moduleList.height = new_height


if __name__ == "__main__":
    root = Tk()
    root.title("Main Window")
    root.geometry("600x600")
    root.configure(background="black")
    window = MainWindow(root)
    window.pack(fill=BOTH, expand=YES)
    try:
        ## ??? Is this mandatory for Windows ???
        print(f"sys.argv[1:] == {sys.argv[1:]}")
        print(f"hello: {sys.argv[1]}")
        if sys.argv[1] == "StandAlone":
            root.mainloop()
    except Exception:
        ## This is sufficient for Mac.
        root.mainloop()
