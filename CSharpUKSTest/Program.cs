
using UKS;

Console.WriteLine("Hello, World!");

UKS.UKS theUKS = new();
theUKS.AddStatement("Fido", "is-a", "dog");
theUKS.AddStatement("dog", "has", "leg", "", "4");
theUKS.AddStatement("dog", "has", "fur");
Thing fido = theUKS.Labeled("Fido");

var results = theUKS.GetAllRelationships(new List<Thing>() { fido },false);


foreach (var result in results)
    Console.WriteLine(result);


