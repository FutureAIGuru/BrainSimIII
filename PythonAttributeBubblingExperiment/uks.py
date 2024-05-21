import pythonnet
pythonnet.load(runtime="coreclr")

import clr
clr.AddReference("../UKS/bin/Debug/net8.0/UKS")

UKS, Thing, Clause, Relationship = None, None, None, None
try:
    from UKS import UKS, Thing, Clause, Relationship
    print("successful UKS import")
except Exception as e:
    print("failed UKS import")
    print(e)
