#display a dialog with a treeview of the UKS content

#TODO:
#icon in upper left displays correctly if script is called direclty but not if from c#
#dialog title never displays correctly
#UKS load and save should be added
#dislog should not show in windows titlebar
#dialog should be a child of the calling window so it will show in front of the command prompt screen
#get rid of remaining globals
#only fill in children if they are displayed
#standardized way of handling height-width-position setup


#time needed for refresh
import time

#GUI library
import tkinter as tk
from tkinter import StringVar,Label, Entry, Button
from tkinter import ttk
from tkinter.ttk import Treeview

#for PythonNet
import pythonnet
from pythonnet import load
import clr
from clr import *
pythonnet.load("coreclr");

#import the UKS
clr.AddReference("UKS")
from UKS import *
from System.Collections.Generic import List
try:
    uks = UKS()
except Exception as e:
    print(f"UKS could not be found. {e.Message}  ")


#keep track of expanded items so refresh can preserve them
listOfOpenItems = []        

#pause the refresh if the mouse is inside the control
updatePaused = False


def Refhesh():
    #Clear the treeview list items
    for item in theTreeView.get_children():
       theTreeView.delete(item)    
    iid = theTreeView.insert('', 'end', 'Things', text='Thing')    
    #build the new tree content
    AddChildren(iid,'Thing')
    
def AddChildren(parentID,itemLabel):
    #recursively add children to the treeview
    theParentThing = uks.Labeled(itemLabel)
    if (theParentThing == None):  #safety
        return
    children = theParentThing.Children
    for child in children:
        iid = theTreeView.insert(parentID,'end',child.Label,text=child.ToString())
        if (parentID in listOfOpenItems):
            theTreeView.item(parentID,open=True)
        AddChildren(iid,child.Label)
 
def handleOpenEvent(event):
    #add the open item to the list
    item_id = theTreeView.focus();
    if (item_id not in listOfOpenItems):
        listOfOpenItems.append(item_id)
def handleCloseEvent(event):
    #remove the closed item from the list
    item_id = theTreeView.focus();
    if (item_id in listOfOpenItems):
        listOfOpenItems.remove(item_id)
def handleMouseEnter(event):
    #Set color bakcground light blue
    style.configure("Treeview", background="light blue", fieldbackground="light blue")
    #pause update
    updatePaused = True
def handleMouseLeave(event):
    #Set color bakcground white
    style.configure("Treeview", background="white", fieldbackground="white")
    #resume update
    updatePaused =False


import os
def main():
    # Create the main window
    global root
    root = tk.Tk()
    root.title("View the UKS tree")
    root.iconbitmap(os.getcwd()+"\\iconsmall.ico")    
    root.geometry("400x400+250+250")

    global style
    style = ttk.Style(root)
    style.theme_use("clam")

    global theTreeView
    theTreeView = Treeview(root,columns=1,show="tree")
    theTreeView.pack(expand=True,fill=tk.BOTH,padx=10,pady=10)
    
    # Inserted at the root:
    iid = theTreeView.insert('', 'end', 'Thing', text='Thing')  
    AddChildren(iid,'Thing')
    
    #TODO Add load and save command buttons here
    #submit_button = Button(root, text="Refresh", command=lambda:Refhesh())
    #submit_button.pack()

    #set up the Treeview events to handle
    theTreeView.bind('<<TreeviewOpen>>',handleOpenEvent)
    theTreeView.bind('<<TreeviewClose>>',handleCloseEvent)
    theTreeView.bind('<Enter>',handleMouseEnter)
    theTreeView.bind('<Leave>',handleMouseLeave)
    

main()


def Fire():
    if updatePaused:
        return
    global prevTime
    currTime = time.time() 
    try:
        if (currTime > prevTime + 1):
            Refhesh()
            prevTime = currTime
    except NameError:
        prevTime = currTime
    root.update()



