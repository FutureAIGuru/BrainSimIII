using UKS;

BrainSimulator.ModuleHandler.CreateEmptyUKS();

if (args.Length > 0)
{
    string fileName = args[0];
    BrainSimulator.ModuleHandler.theUKS.LoadUKSfromXMLFile(fileName);
}
//initialize active module list
Thing activeModulesRoot = BrainSimulator.ModuleHandler.theUKS.Labeled("ActiveModule");
if (activeModulesRoot != null)
{
    foreach (Thing module in activeModulesRoot.Children)
        if (module.Label.Contains(".py"))
            BrainSimulator.ModuleHandler.pythonModules.Add(module.Label);
}
//force the MainWindow to always be activated
BrainSimulator.ModuleHandler.ActivateModule("MainWindow.py");


while (true)
{
    foreach (var module in activeModulesRoot.Children)
    {
        if (module.Label.Contains(".py"))
            BrainSimulator.ModuleHandler.RunScript(module.Label);
    }

    for (int i = 0; i < BrainSimulator.ModuleHandler.activePythonModules.Count; i++)
    {
        (string, dynamic) module = BrainSimulator.ModuleHandler.activePythonModules[i];
        if (activeModulesRoot.Children.FindFirst(x => x.Label == module.Item1) == null)
        {
            try
            {
                module.Item2.Close();
                BrainSimulator.ModuleHandler.activePythonModules.RemoveAt(i);
                i--;
            }
            catch { }
        }
    }
}
