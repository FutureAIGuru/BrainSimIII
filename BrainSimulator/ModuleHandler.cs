using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using UKS;

#if !CONSOLE_APP
using System.Windows.Interop;
#endif

namespace BrainSimulator;

public class ModuleHandler
{
    public List<string> pythonModules = new();


    public List<(string, dynamic)> activePythonModules = new();
    public UKS.UKS theUKS = new();

    string pythonPath = "";
    public string PythonPath { get => pythonPath; set => pythonPath = value; }

    //Runtime.PythonDLL = @"/opt/anaconda3/envs/brainsim/bin/python";  // Yida's MAC
    // Runtime.PythonDLL = PythonDll;//  @"python310";  // Charles's Windows


    public string ActivateModule(string moduleType)
    {
        Thing t = theUKS.GetOrAddThing(moduleType, "AvailableModule");
        t = theUKS.CreateInstanceOf(theUKS.Labeled(moduleType));
        t.AddParent(theUKS.Labeled("ActiveModule"));

#if !CONSOLE_APP
        if (!moduleType.Contains(".py"))
        {
            BrainSimulator.Modules.ModuleBase newModule = MainWindow.theWindow.CreateNewModule(moduleType);
            newModule.Label = t.Label;
            MainWindow.theWindow.activeModules.Add(newModule);
        }
        else
#endif
        {
            pythonModules.Add(t.Label);
        }

        return t.Label;
    }
    public void DeactivateModule(string moduleLabel)
    {
        Thing t = theUKS.Labeled(moduleLabel);
        if (t == null) return;
        for (int i = 0; i < t.Relationships.Count; i++)
        {
            Relationship r = t.Relationships[i];
            theUKS.DeleteThing(r.target);
        }
        theUKS.DeleteThing(t);

        return;
    }


    public List<string> GetPythonModules()
    {
        //this is a buffer of python modules so they can be imported once and run many times.
        List<String> pythonFiles = new();
        if (pythonPath == "no") return pythonFiles;
        try
        {
            var filesInDir = Directory.GetFiles(@".", "m*.py").ToList();
            foreach (var file in filesInDir)
            {
                if (file.StartsWith("utils")) continue;
                pythonFiles.Add(Path.GetFileName(file));
            }
            filesInDir = Directory.GetFiles(@".\pythonModules", "m*.py").ToList();
            foreach (var file in filesInDir)
            {
                if (file.StartsWith("utils")) continue;
                pythonFiles.Add(Path.GetFileName(file));
            }
        }
        catch
        {

        }
        return pythonFiles;
    }

    public bool InitPythonEngine()
    {
        try
        {
            //Runtime.PythonDLL = @"/opt/anaconda3/envs/brainsim/bin/python";  // Yida's MAC
            Runtime.PythonDLL = PythonPath;//  @"python310";  // Charles's Windows
            if (!PythonEngine.IsInitialized)
                PythonEngine.Initialize();
            dynamic sys = Py.Import("sys");
            dynamic os = Py.Import("os");
            string desiredPath = os.path.join(os.getcwd(), "./bin/Debug/net8.0/");
            sys.path.append(desiredPath);  // enables finding scriptName module
            sys.path.append(os.getcwd() + "\\pythonModules");
            Console.WriteLine("PythonEngine init succeeded\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Python engine initialization failed because: "+ex.Message);
            return false;
        }
        return true;
    }
    public void RunScript(string moduleLabel)
    {
        if (PythonPath == "no") return;
        bool firstTime = false;
        //get the ModuleType
        Thing tModule = theUKS.Labeled(moduleLabel);
        if (tModule == null) { return; }
        Thing tModuleType = tModule.Parents.FindFirst(x => x.HasAncestorLabeled("AvailableModule"));
        if (tModuleType == null) return;
        string moduleType = tModuleType.Label;
        moduleType = moduleType.Replace(".py", "");

        //if this is the very first call, initialize the python engine
        if (Runtime.PythonDLL == null)
        {
            try
            {
                Runtime.PythonDLL = PythonPath;//  @"python310";  // Charles's Windows
                PythonEngine.Initialize();
                dynamic sys = Py.Import("sys");
                dynamic os = Py.Import("os");
                string desiredPath = os.path.join(os.getcwd(), "./bin/Debug/net8.0/");
                sys.path.append(desiredPath);  // enables finding scriptName module
                sys.path.append(os.getcwd() + "\\pythonModules");
                Console.WriteLine("PythonEngine init succeeded\n");
            }
            catch
            {
                Console.WriteLine("Python engine initialization failed");
                return;
            }
        }
        using (Py.GIL())
        {
            var theModuleEntry = activePythonModules.FirstOrDefault(x => x.Item1.ToLower() == moduleLabel.ToLower());
            if (string.IsNullOrEmpty(theModuleEntry.Item1))
            {
                //if this is the first time this modulw has been used
                try
                {
                    Console.WriteLine("Loading " + moduleLabel);
                    dynamic theModule = Py.Import(moduleType);
                    theModule.Init();
                    theModuleEntry = (moduleLabel, theModule);
                    activePythonModules.Add(theModuleEntry);
                    firstTime = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Load/initialize failed for module: " + moduleLabel + "   Reason: " + ex.Message);
                    theModuleEntry = (moduleLabel, null);
                    activePythonModules.Add(theModuleEntry);
                }
            }
            if (theModuleEntry.Item2 != null)
            {
                try
                {
                    theModuleEntry.Item2.Fire();
#if !CONSOLE_APP
//This sets the owner of any target window so that the system will work propertly
                    if (firstTime)
                    {
                        var HWND = theModuleEntry.Item2.GetHWND();
                        var ss = HWND.ToString();
                        // this works, and returns 1322173
                        int intValue = Convert.ToInt32(ss, 16);
                        firstTime = false;
                        SetOwner(intValue);
                    }
#endif
                }
                catch (Exception ex)
                {
                    activePythonModules.Remove(theModuleEntry);
                    DeactivateModule(moduleLabel);
#if !CONSOLE_APP
                    MainWindow.theWindow.ReloadActiveModulesSP();
#endif
                    Console.WriteLine("Fire method call failed for module: " + moduleLabel + "   Reason: " + ex.Message);
                }
            }
        }
    }

    public void CreateEmptyUKS()
    {
        theUKS = new UKS.UKS();
        theUKS.AddThing("BrainSim", null);
        theUKS.GetOrAddThing("AvailableModule", "BrainSim");
        theUKS.GetOrAddThing("ActiveModule", "BrainSim");

        InsertMandatoryModules();
    }

    public void InsertMandatoryModules()
    {

        Debug.WriteLine("InsertMandatoryModules entered");
#if !CONSOLE_APP
        ActivateModule("UKS");
        ActivateModule("UKSStatement");
#endif
    }



#if !CONSOLE_APP
    private const int GWL_HWNDPARENT = -8;

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    static void SetOwner(int HWND)
    {
        // Example usage
        IntPtr childHwnd = new IntPtr(HWND); // Child window handle
                                             //IntPtr ownerHwnd = new IntPtr(654321); // New owner window handle
        Window window = Window.GetWindow(MainWindow.theWindow);
        var wih = new WindowInteropHelper(window);
        IntPtr ownerHwnd = wih.Handle;

        ChangeWindowOwner(childHwnd, ownerHwnd);
    }

    static void ChangeWindowOwner(IntPtr childHwnd, IntPtr ownerHwnd)
    {
        IntPtr result = 0;
        if (IntPtr.Size == 8)
            SetWindowLongPtr(childHwnd, GWL_HWNDPARENT, ownerHwnd);
        else
            SetWindowLong(childHwnd, GWL_HWNDPARENT, ownerHwnd);

        if (result == IntPtr.Zero)
        {
            int errorCode = Marshal.GetLastWin32Error();
            Console.WriteLine($"Failed to change window owner. Error code: {errorCode}");
        }
        else
        {
            Console.WriteLine("Window owner changed successfully.");
        }
    }
#endif
}
