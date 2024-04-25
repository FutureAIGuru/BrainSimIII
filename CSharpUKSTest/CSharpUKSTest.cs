
using Python.Runtime;
using UKS;

//this is a buffer of python modules so they can be imported once and run many times.
List<(string, dynamic)> modules = new List<(string, dynamic)>();

Console.WriteLine("Hello, World!");

UKS.UKS theUKS = new();
theUKS.AddStatement("Spot", "is-a", "dog");

var pythonFiles = Directory.EnumerateFiles(".", "*.py").ToList();
for (int i = 0; i < pythonFiles.Count; i++)
    pythonFiles[i] = Path.GetFileNameWithoutExtension(pythonFiles[i]);

for (int i = 0; i < 100; i++) //this is the main "Engine" loop 
    foreach (var pythonFile in pythonFiles)
        RunScript(pythonFile, modules);



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

static void RunScript(string scriptName, List<(string, dynamic)> modules)
{
    if (Runtime.PythonDLL == null)
    {
        Runtime.PythonDLL = @"Python310.dll";
        PythonEngine.Initialize();
    }
    using (Py.GIL())
    {
        var theModuleEntry = modules.FirstOrDefault(x => x.Item1.ToLower() == scriptName.ToLower());
        if (string.IsNullOrEmpty(theModuleEntry.Item1))
        {
            Console.WriteLine("Loading " + scriptName);
            dynamic theModule = Py.Import(scriptName);
            theModuleEntry = (scriptName, theModule);
            modules.Add(theModuleEntry);
        }
        theModuleEntry.Item2.Fire();
    }
}
