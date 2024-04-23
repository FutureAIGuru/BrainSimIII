
using Python.Runtime;
using UKS;


Runtime.PythonDLL = @"C:\Users\c_sim\AppData\Local\Programs\Python\Python310\python310.dll";
PythonEngine.Initialize();
using (Py.GIL())
{
    var module = PyModule.Import(@"C:\Users\c_sim\source\repos\BrainSimIIINEW\PythonUKSTest\PythonUKSTest.py");
    var result = module.InvokeMethod("Fire");
}

Console.WriteLine("Hello, World!");

UKS.UKS theUKS = new();
theUKS.AddStatement("Fido", "is-a", "dog");
theUKS.AddStatement("dog", "has", "leg", "", "4");
theUKS.AddStatement("dog", "has", "fur");
Thing fido = theUKS.Labeled("Fido");

var results = theUKS.GetAllRelationships(new List<Thing>() { fido },false);


foreach (var result in results)
    Console.WriteLine(result);


