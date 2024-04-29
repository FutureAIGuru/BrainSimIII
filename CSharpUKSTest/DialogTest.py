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
print("Hello from dialogtest")


#change colors of input fields if they are 
def textChanged0(sv):
    s = sv.get()
    if (uks.Labeled(s) != None):
        input_source.configure(bg='lightyellow')
    else:
        input_source.configure(bg='pink')
def textChanged1(sv):
    s = sv.get()
    if (uks.Labeled(s) != None):
        input_type.configure(bg='lightyellow')
    else:
        input_type.configure(bg='pink')
def textChanged2(sv):
    s = sv.get()
    if (uks.Labeled(s) != None):
        input_target.configure(bg='lightyellow')
    else:
        input_target.configure(bg='pink')
    

def handleReturn(event):
    submit_input()   
def submit_input():
    # Process the input from the entry widgets
    source = input_source.get()
    relType = input_type.get()
    target = input_target.get()
    uks.AddStatement(source,relType,target)
    print("User Input:", source,relType,target)

def main():
    # Create the main window
    global root
    root = tk.Tk()
    root.title = "Add Relationship to UKS"
    root.geometry("+100+100")
    #root.overrideredirect(True)
    #root.deiconify()

    #set up a callback
    sv0 = StringVar()
    sv0.trace("w",lambda name, index, mode, sv0=sv0: textChanged0(sv0))
    sv1 = StringVar()
    sv1.trace("w",lambda name, index, mode, sv1=sv1: textChanged1(sv1))
    sv2 = StringVar()
    sv2.trace("w",lambda name, index, mode, sv2=sv2: textChanged2(sv2))

    # Add widgets 
    Label(root,text="Source:").grid(row=0,sticky="E")
    global input_source
    input_source = Entry(root, width=40, textvariable=sv0)
    input_source.grid(row=0,column=1,pady=10,padx=10)
    Label(root,text="Relationship:").grid(row=1,sticky="E")
    global input_type
    input_type = Entry(root, width=40, textvariable=sv1)
    input_type.grid(row=1,column=1,pady=10,padx=10)
    Label(root,text="Target:").grid(row=2,sticky="E")
    global input_target
    input_target = Entry(root, width=40, textvariable=sv2)
    input_target.grid(row=2,column=1,pady=10,padx=10)
    
    submit_button = Button(root, text="Submit", command=lambda:submit_input()).grid(row=3,column=1,sticky="W",pady=20)
    
    root.bind('<Return>',handleReturn)


main()

def Fire():
     root.update()
   

