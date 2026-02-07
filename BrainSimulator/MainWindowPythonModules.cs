/*
 * Brain Simulator Through
 *
 * Copyright (c) 2026 Charles Simon
 *
 * This file is part of Brain Simulator Through and is licensed under
 * the MIT License. You may use, copy, modify, merge, publish, distribute,
 * sublicense, and/or sell copies of this software under the terms of
 * the MIT License.
 *
 * See the LICENSE file in the project root for full license information.
 */

using Python.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using UKS;
using System.Windows.Interop;


namespace BrainSimulator;

public partial class MainWindow : Window
{
    public static List<(string, dynamic)> activePythonModules = new();
    public static List<string> GetPythonModules()
    {
        //this is a buffer of python modules so they can be imported once and run many times.
        List<(string, dynamic)> modules = new List<(string, dynamic)>();
        List<String> pythonFiles = new();
        try
        {
            pythonFiles = Directory.GetFiles(@".\pythonModules", "*.py").ToList();
            string destDir = Directory.GetCurrentDirectory();
            string sourceDir = destDir + "\\PythonModules";
            for (int i = 0; i < pythonFiles.Count; i++)
            {
                if (pythonFiles[i].StartsWith("utils")) continue;
                pythonFiles[i] = Path.GetFileName(pythonFiles[i]);
            }
        }
        catch
        {

        }
        return pythonFiles;
    }
    static void RunScript(string moduleLabel)
    {
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
                //TODO use variable
                Runtime.PythonDLL = @"Python310";

                PythonEngine.Initialize();
                dynamic sys = Py.Import("sys");
                dynamic os = Py.Import("os");
                sys.path.append(os.getcwd());  // enables finding scriptName module
                sys.path.append(os.getcwd() + "\\pythonModules");
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
                    theModuleEntry = (moduleLabel, theModule);
                    activePythonModules.Add(theModuleEntry);
                    theModule.Init();
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
                    if (!theModuleEntry.Item2.Fire())
                    { } //this doesn't seem to work because it thows an exception instead
                    if (firstTime)
                    {
                        var HWND = theModuleEntry.Item2.GetHWND();
                        var ss = HWND.ToString();
                        // this works, and returns 1322173
                        int intValue = Convert.ToInt32(ss, 16);
                        firstTime = false;
                        SetOwner(intValue);
                    }
                }
                catch (Exception ex)
                {
                    activePythonModules.Remove(theModuleEntry);
                    MainWindow.theWindow.DeactivateModule(moduleLabel);
                    Console.WriteLine("Fire method call failed for module: " + moduleLabel + "   Reason: " + ex.Message);
                }
            }
        }
    }


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
}



