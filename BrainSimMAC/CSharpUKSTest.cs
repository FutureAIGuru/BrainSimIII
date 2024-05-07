using Python.Runtime;

//this is a buffer of python modules so they can be imported once and run many times.
List<(string, dynamic)> modules = new List<(string, dynamic)>();

// var pythonFiles = Directory.EnumerateFiles(".", "*.py").ToList();
// for (int i = 0; i < pythonFiles.Count; i++)
//     pythonFiles[i] = Path.GetFileNameWithoutExtension(pythonFiles[i]);
// 
// while (true)
//     foreach (var pythonFile in pythonFiles)
//         RunScript(pythonFile, modules);

while (true) 
{
    RunScript("view_uks_tree", modules);  // must launch root-window first
    RunScript("view_dialog_add_statement", modules);
}


static void RunScript(string scriptName, List<(string, dynamic)> modules)
{
    //if this is the very first call, initialize the python engine
    if (Runtime.PythonDLL == null)
    {
        try
        {
            Console.WriteLine("\nLoading Python DLL...");
            Runtime.PythonDLL = @"/opt/anaconda3/envs/brainsim/bin/python";
            PythonEngine.Initialize();
            dynamic sys = Py.Import("sys");
            dynamic os = Py.Import("os");
            sys.path.append(os.getcwd());  // enables finding scriptName module
            Console.WriteLine("PythonEngine init succeeded\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine("PythonEngine init failed");
            Console.WriteLine(ex.Message + "\n");
            return;
        }
    }
    using (Py.GIL())
    {
        var theModuleEntry = modules.FirstOrDefault(x => x.Item1.ToLower() == scriptName.ToLower());
        if (string.IsNullOrEmpty(theModuleEntry.Item1))
        {
            //if this is the first time this module has been used
            try
            {
                Console.WriteLine("Loading " + scriptName);
                dynamic theModule = Py.Import(scriptName);
                theModuleEntry = (scriptName, theModule);
                modules.Add(theModuleEntry);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Load/initialize failed for module: " + scriptName);
                Console.WriteLine("Reason: " + ex.Message);
                theModuleEntry = (scriptName, null);
                modules.Add(theModuleEntry);
            }
        }
        if (theModuleEntry.Item2 != null)
        {
            try
            {
                theModuleEntry.Item2.Fire();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fire method call failed for module: " + scriptName);
                Console.WriteLine("Reason: " + ex.Message);
            }
        }
    }
}

