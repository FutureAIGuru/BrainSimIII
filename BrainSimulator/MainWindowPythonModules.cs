
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;


namespace BrainSimulator;

public partial class MainWindow : Window
{
    public static List<(string, dynamic)> activePythonModules = new();
    public static List<string> GetPythonModules()
    {
        //this is a buffer of python modules so they can be imported once and run many times.
        List<(string, dynamic)> modules = new List<(string, dynamic)>();

        //TODO: check to see if files are updated and reload
        var pythonFiles = Directory.GetFiles(@".\pythonModules", "*.py").ToList();
        string destDir = Directory.GetCurrentDirectory();
        string sourceDir = destDir + "\\PythonModules";
        for (int i = 0; i < pythonFiles.Count; i++)
        {
            pythonFiles[i] = Path.GetFileName(pythonFiles[i]);
            File.Copy(Path.Combine(sourceDir,pythonFiles[i]), Path.Combine(destDir,pythonFiles[i]), true);
        }
        return pythonFiles;
    }

    static void RunScript(string scriptName)
    {
        //if this is the very first call, initialize the python engine
        if (Runtime.PythonDLL == null)
        {
            try
            {
                Runtime.PythonDLL = @"Python310.dll";
                PythonEngine.Initialize();
            }
            catch
            {
                Console.WriteLine("Python engine initialization failed");
                return;
            }
        }
        using (Py.GIL())
        {
            var theModuleEntry = activePythonModules.FirstOrDefault(x => x.Item1.ToLower() == scriptName.ToLower());
            if (string.IsNullOrEmpty(theModuleEntry.Item1))
            {
                //if this is the first time this modulw has been used
                try
                {
                    Console.WriteLine("Loading " + scriptName);
                    dynamic theModule = Py.Import(scriptName);
                    theModuleEntry = (scriptName, theModule);
                    activePythonModules.Add(theModuleEntry);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Load/initialize failed for module: " + scriptName + "   Reason: " + ex.Message);
                    theModuleEntry = (scriptName, null);
                    activePythonModules.Add(theModuleEntry);
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
                    Console.WriteLine("Fire method call failed for module: " + scriptName + "   Reason: " + ex.Message);
                }
            }
        }
    }
}

