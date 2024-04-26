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

def Fire():
    uks.AddStatement("Fido","is-a","dog")
    # fidoThing = uks.Labeled("Fido")
    # for thing in  fidoThing.Parents:
    #     print (thing.ToString())

    # uks.AddStatement("dog","has","leg","","4")
    # uks.AddStatement("dog","has","fur")

    # dog = uks.Labeled("dog")
    # fido = uks.Labeled("Fido")
    # print (fido.Label)

    # listOfParams = List[Thing]()
    # listOfParams.Add(dog)

    # #results = dog.Relationships

    # results = uks.GetAllRelationships(listOfParams,False);

    # for relationship in results:
    #     print (relationship.ToString())

    # results = dog.Relationships

    # for relationship in results:
    #     print (relationship.ToString())





