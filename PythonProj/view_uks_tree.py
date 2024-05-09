## Global imports
import sys, os
from typing import List, Union
import time  # time needed for refresh()
import tkinter as tk
import tkinter.ttk as ttk
## Local imports
from utils import ViewBase


class ViewUKSTree(ViewBase):
    def __init__(self, level: Union[tk.Tk, tk.Toplevel]) -> None:
        title: str = "The Universal Knowledge Store (UKS)"
        super(ViewUKSTree, self).__init__(
            title=title, level=level, module_type=os.path.basename(__file__))
        ## Keep track of expanded items so refresh can preserve them
        self.open_items: List[str] = []
        ## Pause the refresh if the mouse is inside the control
        self.update_paused: bool = False
        self.style = ttk.Style(self.level)
        self.tree_view = ttk.Treeview(master=self.level, columns=1, show="tree")
        self.prev_time: float = None
        self.build()

    def refresh(self) -> None:
        ## Clear the treeview list items
        for item in self.tree_view.get_children():
            self.tree_view.delete(item)    
        iid: str = self.tree_view.insert(parent="", 
                                         index="end", 
                                         iid="Things", 
                                         text="Thing")
        ## Build the new tree content
        self.add_children(iid, "Thing")
        
    def add_children(self, parent_id: str, item_label: str) -> None:
        ## Recursively add children to the treeview
        parent_thing = self.uks.Labeled(item_label)
        if parent_thing is None:  # safety
            return
        children = parent_thing.Children
        for child in children:
            iid: str = self.tree_view.insert(parent=parent_id, 
                                             index="end", 
                                             iid=child.Label, 
                                             text=child.ToString())
            if parent_id in self.open_items:
                self.tree_view.item(parent_id, open=True)
            self.add_children(iid, child.Label)
    
    ################
    ##  Handlers  ##
    ################

    def handle_open_event(self, event: str) -> None:
        """ 
        !!! NOTE: DO NOT delete the `event` arg !!!
        Add the open item to the list 
        """
        item_id: str = self.tree_view.focus()
        if item_id not in self.open_items:
            self.open_items.append(item_id)
    
    def handle_close_event(self, event: str) -> None:
        """ 
        !!! NOTE: DO NOT delete the `event` arg !!!
        Remove the closed item from the list 
        """
        item_id: str = self.tree_view.focus()
        if item_id in self.open_items:
            self.open_items.remove(item_id)
    
    def handle_mouse_enter(self, event: str) -> None:
        """ !!! NOTE: DO NOT delete the `event` arg !!! """
        ## Set color background light blue 
        self.style.configure(style="Treeview", 
                             background="light blue", 
                             fieldbackground="light blue")
        ## Pause update
        self.update_paused: bool = True
    
    def handle_mouse_leave(self, event: str) -> None:
        """ !!! NOTE: DO NOT delete the `event` arg !!! """
        ## Set color background white 
        self.style.configure(style="Treeview", 
                             background="white", 
                             fieldbackground="white")
        ## Resume update
        self.update_paused: bool = False
    
    #############
    ##  Build  ##
    #############

    def build(self) -> None:
        ## Create the window
        self.level.transient()
        self.level.geometry("400x400+250+250")
        self.style.theme_use("clam")
        self.tree_view.pack(expand=True, fill=tk.BOTH, padx=10, pady=10)
        ## Inserted at the root:
        iid: str = self.tree_view.insert("", "end", "Thing", text="Thing")
        self.add_children(iid, "Thing")
        # TODO: Add load and save command buttons here
        ## Add REFRESH button
        refresh_button = tk.Button(master=self.level, 
                                   text="Refresh", 
                                   command=lambda:self.refresh())
        refresh_button.pack()
        ## Set up the Treeview events to handle
        self.tree_view.bind("<<TreeviewOpen>>", self.handle_open_event)
        self.tree_view.bind("<<TreeviewClose>>", self.handle_close_event)
        self.tree_view.bind("<Enter>", self.handle_mouse_enter)
        self.tree_view.bind("<Leave>", self.handle_mouse_leave)
        
        if sys.argv[0]  != "":
            self.level.mainloop()
            
        
    
    ############
    ##  Fire  ##
    ############

    def fire(self) -> bool:
        if self.update_paused:
            return True
        curr_time: float = time.time()
        try:
            if curr_time > (self.prev_time + 1.0):
                self.refresh()
                self.prev_time = curr_time
        except Exception:
            self.prev_time = curr_time
        self.level.update()
        return self.level.winfo_exists()


######################
##  Expose Methods  ##
######################

def Init():
    global view
    view = ViewUKSTree(level=tk.Tk())

def Fire() -> bool:    
    return view.fire()
    
def GetHWND() -> int:
    hwnd = view.level.frame()
    return hwnd

def SetLabel(label):
    view.setLabel(label)
    
def Close():
    view.close()

if sys.argv[0]  != "":
    Init()
