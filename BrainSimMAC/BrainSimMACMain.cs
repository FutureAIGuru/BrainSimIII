using Python.Runtime;



BrainSimulator.ModuleHandler.CreateEmptyUKS();
BrainSimulator.ModuleHandler.ActivateModule("view_uks_tree.py");
BrainSimulator.ModuleHandler.ActivateModule("view_dialog_add_statement.py");

while (true) 
{
    foreach (var moduleName in BrainSimulator.ModuleHandler.pythonModules)
        BrainSimulator.ModuleHandler.RunScript(moduleName);
}

