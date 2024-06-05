import sys, os
## Global imports
from typing import Union
import tkinter as tk
## Local imports
from utils import ViewBase


class ViewDialogAddStatement(ViewBase):
    def __init__(self, level: Union[tk.Tk, tk.Toplevel]) -> None:
        title: str = "UKS Add Statement"
        super(ViewDialogAddStatement, self).__init__(
            title=title, level=level, module_type=os.path.basename(__file__))
        ## Set up a callback
        sv0, sv1, sv2 = tk.StringVar(), tk.StringVar(), tk.StringVar()
        self.input_src = tk.Entry(master=self.level, width=40, textvariable=sv0)
        self.input_rel = tk.Entry(master=self.level, width=40, textvariable=sv1)
        self.input_tgt = tk.Entry(master=self.level, width=40, textvariable=sv2)
        self.input_src.grid(row=0, column=1, pady=10, padx=10)
        self.input_rel.grid(row=1, column=1, pady=10, padx=10)
        self.input_tgt.grid(row=2, column=1, pady=10, padx=10)
        sv0.trace_add(mode="write", 
                      callback=(lambda a,b,c: self.text_changed(sv0, self.input_src)))
        sv1.trace_add(mode="write", 
                      callback=(lambda a,b,c: self.text_changed(sv1, self.input_rel)))
        sv2.trace_add(mode="write", 
                      callback=(lambda a,b,c: self.text_changed(sv2, self.input_tgt)))
        self.build()
    
    def text_changed(self, sv: tk.StringVar, e: tk.Entry) -> None:
        """ Change colors of input fields """
        s: str = sv.get()
        if self.uks.Labeled(s) is not None:
            e.configure(bg="lightyellow")
        else:
            e.configure(bg="pink")
    
    def submit_input(self):
        """ Process the input from the entry widgets """
        src: str = self.input_src.get()
        rel: str = self.input_rel.get()
        tgt: str = self.input_tgt.get()
        self.uks.AddStatement(src, rel, tgt)
        print(f"User Input: [{src}, {rel}, {tgt}]")
    
    ################
    ##  Handlers  ##
    ################

    def handle_return(self, event: str):
        """ !!! NOTE: DO NOT delete the `event` arg !!! """
        self.submit_input()

    #############
    ##  Build  ##
    #############

    def build(self):
        ## Create the window
        self.level.geometry("+100+100")
        ## Add widgets 
        src_widget = tk.Label(master=self.level, text="Source:")
        rel_widget = tk.Label(master=self.level, text="Relationship:")
        tgt_widget = tk.Label(master=self.level, text="Target:")
        src_widget.grid(row=0, sticky="E")  # E = east
        rel_widget.grid(row=1, sticky="E")  # E = east
        tgt_widget.grid(row=2, sticky="E")  # E = east
        ## Add SUBMIT button
        submit_button = tk.Button(master=self.level, text="Submit", 
                                  command=lambda:self.submit_input())
        submit_button.grid(row=3, column=1, pady=20, sticky="W")  # W = west
        self.level.bind("<Return>", self.handle_return)
       
        if sys.argv[0]  != "":
            self.level.mainloop()
    
    ############
    ##  Fire  ##
    ############

    def fire(self) ->bool:
        self.level.update()
        return self.level.winfo_exists()


######################
##  Expose Methods  ##
######################

def Init():
    global view
    view = ViewDialogAddStatement(level=tk.Tk())

def Fire() -> bool:
    return view.fire()
    
def GetHWND() -> int:
    hwnd = view.level.frame()
    return hwnd

def SetLabel(label: str):
    view.setLabel(label)
    
def Close():
    view.close()

if sys.argv[0]  != "":
    Init()
