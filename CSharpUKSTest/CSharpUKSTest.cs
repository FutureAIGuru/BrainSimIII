
using Python.Runtime;
using UKS;

//this is a buffer of python modules so they can be imported once and run many times.
List<(string,dynamic)> modules = new List<(string,dynamic)>();

Console.WriteLine("Hello, World!");

UKS.UKS theUKS = new();
theUKS.AddStatement("Spot", "is-a", "dog");

//these are the module scripts to be run...they will soon be in a loop
RunScript("SpotTest", modules);
RunScript("PythonUKSTest",modules);
RunScript("SpotTest", modules);
RunScript("PythonUKSTest", modules);


//here is mix/and/match between c# and python using the UKS
theUKS.AddStatement("Fido", "is-a", "dog");
theUKS.AddStatement("dog", "has", "leg", "", "4");
theUKS.AddStatement("dog", "has", "fur");
Thing fido = theUKS.Labeled("Fido");
if (fido != null)
{
    var results = theUKS.GetAllRelationships(new List<Thing>() { fido }, false);
    foreach (var result in results)
        Console.WriteLine(result);
}

static void RunScript(string scriptName, List<(string,dynamic)> modules)
{
    if (Runtime.PythonDLL == null)
    {
        Runtime.PythonDLL = @"Python310.dll";
        PythonEngine.Initialize();
    }
    using (Py.GIL())
    {
        var theModuleEntry = modules.FirstOrDefault(x=>x.Item1.ToLower() == scriptName.ToLower());
        if (string.IsNullOrEmpty(theModuleEntry.Item1))
        {
            Console.WriteLine("Loading " + scriptName);
            dynamic theModule = Py.Import(scriptName);
            theModuleEntry = (scriptName, theModule);
            modules.Add(theModuleEntry);
        }
        Console.WriteLine("Fire " + scriptName);
        theModuleEntry.Item2.Fire();
    }
}
