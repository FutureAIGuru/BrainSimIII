#pip install pythonnet
#doesn't work with .net 8.0
#does work with .net 6.0

#OUTLINE: HOW to setup the dll for a Python Module
#import pythonnet
#from pythonnet import load
#pythonnet.load("coreclr");
#import clr
#clr.AddReference("pathToDll\\dllName")
#from nameSpace import *
#obj = ClassName()
#xxx = obj.Method(params)


#get the correct library
from tkinter.ttk import Treeview
from turtle import width
import pythonnet
from pythonnet import load
pythonnet.load("coreclr");
#you might need the version info for debugging
#v = pythonnet.get_runtime_info()
#print(v)


#load the clr
import clr
from clr import *

clr.AddReference("UKS")
from UKS import *
from System.Collections.Generic import List

#here's how you can put the UKS call into a try/catch block
try:
    uks = UKS()
except Exception as e:
    print(f"Something Went wrong. {e.Message}  ")


#from tkinter import a*
import tkinter as tk
from tkinter import StringVar,Label, Entry, Button
print("Hello from UKSTreeView")

def Refresh_click():
    #Clear the treeview list items
    for item in theTreeView.get_children():
       theTreeView.delete(item)    
    iid = theTreeView.insert('', 'end', 'Things', text='Thing')    
    AddChildren(iid,'Thing')
    
def AddChildren(parentID,itemLabel):
    theParentThing = uks.Labeled(itemLabel)
    if (theParentThing == None):
        return
    children = theParentThing.Children
    for child in children:
        iid = theTreeView.insert(parentID,'end',child.Label,text=child.ToString())
        theTreeView.item(parentID,open=True)
        AddChildren(iid,child.Label)
    
def handleOpenEvent(event):
    item_id = theTreeView.focus();
    print(item_id)    
def handleMotionEvent(event):
    print(event)    

def __init__(self,parent):
        Tkinter.Frame.__init__(self, parent, relief=Tkinter.SUNKEN, bd=2)
        self.parent = parent        

        self.menubar = Tkinter.Menu(self)
        self.parent.winfo_toplevel().configure(menu=self.menubar)

        self.tree = ttk.Treeview(self)

        self.yscrollbar = ttk.Scrollbar(self, orient='vertical', command=self.tree.yview)
        self.tree.configure(yscrollcommand=self.yscrollbar.set)

        self.tree.grid(row=0, column=0, sticky="nsew")
        self.yscrollbar.grid(row=0, column=1, sticky='nse')
        self.yscrollbar.configure(command=self.tree.yview)

        self.grid_rowconfigure(0, weight=1)
        self.grid_columnconfigure(0, weight=1)
def main():
    # Create the main window
    global root
    root = tk.Tk()
    root.title = "View the UKS tree"

    global theTreeView
    theTreeView = Treeview(root,columns=1)
    theTreeView.grid(row=0,column=0,padx=20,pady=20,sticky="nsew")
    # Inserted at the root, program chooses id:
    iid = theTreeView.insert('', 'end', 'Thing', text='Thing')  
    theTreeView.item(iid,open=True)
    AddChildren(iid,'Thing')
    
    submit_button = Button(root, text="Refresh", command=lambda:Refresh_click()).grid(row=3,column=0,sticky="ws",pady=20)
    root.bind('<Return>',Refresh_click())
    #to grab mouse motion
    #theTreeView.bind('<Motion>',handleMotionEvent)
    theTreeView.bind('<<TreeviewOpen>>',handleOpenEvent)
    

main()

def Fire():
     root.update()
   

