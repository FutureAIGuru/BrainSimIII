#get the correct library
import pythonnet
from pythonnet import load
pythonnet.load("coreclr");
#you might need the version info for debugging
#v = pythonnet.get_runtime_info()
#print(v)
import clr
from clr import *
from UKS import *
print ("UKS.dll loaded!")
clr.AddReference("C:\\Users\\c_sim\\source\\repos\\BrainSimIIINEW\\UKS\\bin\\Debug\\net8.0\\UKS")
uks = UKS()
from System.Collections.Generic import List

    
def Fire():
    print("Fire Called in SpotTest")
    print("Ancestors of SPot")

    fidoThing = uks.Labeled("Spot")
    for thing in  fidoThing.Ancestors:
        print (thing.ToString())




