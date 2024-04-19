#pip install pythonnet
#doesn't work with .net 8.0
#does work with .net 6.0

#HOW to setup the dll
#import clr
#clr.AddReference("pathToDll\\dllName")
#from nameSpace import *
#obj = ClassName()
#xxx = obj.Method(params)


import clr
from System.Collections.Generic import List

print ("Hello World!")

#clr.AddReference("dlls\\UKS")
clr.AddReference("..\\UKS\\bin\\Debug\\net6.0\\UKS")
from UKS import *
print ("UKS.dll loaded!")

uks = UKS()

uks.AddStatement("Fido","is-a","dog")
uks.AddStatement("dog","has","leg","","4")
uks.AddStatement("dog","has","fur")

dog = uks.Labeled("dog")
fido = uks.Labeled("Fido")
print (fido.Label)

listOfParams = List[Thing]()
listOfParams.Add(dog)

#results = dog.Relationships

results = uks.GetAllRelationships(listOfParams,False);

for relationship in results:
    print (relationship.ToString())




