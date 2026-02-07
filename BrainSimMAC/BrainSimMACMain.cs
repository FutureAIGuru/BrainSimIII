/*
 * Brain Simulator Thought
 *
 * Copyright (c) 2026 Charles Simon
 *
 * This file is part of Brain Simulator Thought and is licensed under
 * the MIT License. You may use, copy, modify, merge, publish, distribute,
 * sublicense, and/or sell copies of this software under the terms of
 * the MIT License.
 *
 * See the LICENSE file in the project root for full license information.
 */
using BrainSimulator;
using UKS;


ModuleHandler moduleHandler = new ModuleHandler();
moduleHandler.CreateEmptyUKS();

//setup the python path
string? pythonPath = (string?)Environment.GetEnvironmentVariable("PythonPath", EnvironmentVariableTarget.User);
if (string.IsNullOrEmpty(pythonPath))
{
    Console.Write("Path to Python .dll must be set: ");
    pythonPath = Console.ReadLine();
    moduleHandler.PythonPath = pythonPath;
    if (moduleHandler.InitPythonEngine())
    {
        Environment.SetEnvironmentVariable("PythonPath", pythonPath, EnvironmentVariableTarget.User);
    }
    else
    {
        Console.Write("Path not set, program must restart.  Press any key... ");
        Console.ReadKey();
        Environment.Exit(1);
    }
}
else
{
    moduleHandler.PythonPath = pythonPath;
}


if (args.Length > 0)
{
    string fileName = args[0];
    moduleHandler.theUKS.LoadUKSfromXMLFile(fileName);
}
else
{
    var pythonFiles = moduleHandler.GetListOfExistingPythonModuleTypes();
    Thought availableModuleRoot = moduleHandler.theUKS.Labeled("AvailableModule");
    foreach (var moduleName in pythonFiles)
        moduleHandler.theUKS.AddThought(moduleName, availableModuleRoot);
}

//initialize active module list
Thought activeModulesRoot = moduleHandler.theUKS.Labeled("ActiveModule");
if (activeModulesRoot is not null)
{
    foreach (Thought module in activeModulesRoot.Children)
        if (module.Label.Contains(".py"))
            moduleHandler.pythonModules.Add(module.Label);
}
//force the MainWindow to always be activated
moduleHandler.ActivateModule("MainWindow.py");


while (true)
{
    foreach (var module in activeModulesRoot.Children)
    {
        if (module.Label.Contains(".py"))
            moduleHandler.RunScript(module.Label);
    }
    activeModulesRoot = moduleHandler.theUKS.Labeled("ActiveModule");

    for (int i = 0; i < moduleHandler.activePythonModules.Count; i++)
    {
        (string, dynamic) module = moduleHandler.activePythonModules[i];
        if (activeModulesRoot.Children.FindFirst(x => x.Label == module.Item1) is null)
        {
            try
            {
                module.Item2.Close();
                moduleHandler.activePythonModules.RemoveAt(i);
                i--;
            }
            catch { }
        }
    }
}
