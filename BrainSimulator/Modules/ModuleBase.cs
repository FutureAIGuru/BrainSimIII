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
//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using UKS;

namespace BrainSimulator.Modules;

abstract public class ModuleBase
{
    public bool initialized = false;

    public bool isEnabled = true;
    public string Label = "";

    protected ModuleBaseDlg dlg = null;
    public Point dlgPos;
    public Point dlgSize;
    public bool dlgIsOpen = false;

    //public static ModuleUKS UKS = null;
    public UKS.UKS theUKS = null;

    public ModuleBase()
    {
        string moduleName = this.GetType().Name;
        if (moduleName.StartsWith("Module"))
        {
            Label = moduleName[6..];
        }
    }

    abstract public void Fire();

    abstract public void Initialize();

    public virtual void UKSInitializedNotification()
    {
    }

    public void UKSInitialized()
    {
        foreach (ModuleBase module in MainWindow.theWindow.activeModules)
        {
            if (module.isEnabled)
                module.UKSInitializedNotification();
        }
    }

    public virtual void UKSReloadedNotification()
    {
    }

    public void UKSReloaded()
    {
        foreach (ModuleBase module in MainWindow.theWindow.activeModules)
        {
            if (module.isEnabled)
                module.UKSReloadedNotification();
        }
    }

    public void GetUKS()
    {
        theUKS = MainWindow.theUKS;
    }

    protected void Init(bool forceInit = false)
    {
        if (initialized && !forceInit) return;
        initialized = true;

        Initialize();

        UpdateDialog();

        if (dlg is null && dlgIsOpen)
        {
            ShowDialog();
            dlgIsOpen = true;
        }
    }
    public void OpenDlg()
    {
        SetSavedDlgAttribute("Open", "True");
        ShowDialog();
    }
    public void CloseDlg()
    {
        if (dlg is not null)
        {
            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                dlg.Close();
            });
        }
        SetSavedDlgAttribute("Open", "");
    }

    public virtual void ShowDialog()
    {
        if (GetSavedDlgAttribute("Open") != "True") return;
        string infoString = GetDlgWindow();
        if ( infoString is not null)
        {
            if (string.IsNullOrEmpty(infoString)) return;
            string[] info = infoString.Split('+', 'x');
            if (info.Length == 4)
            {
                dlgSize.X = float.Parse(info[0]);
                dlgSize.Y = float.Parse(info[1]);
                dlgPos.X = float.Parse(info[2]);
                dlgPos.Y = float.Parse(info[3]);
            }
        }

        ApartmentState aps = Thread.CurrentThread.GetApartmentState();
        if (aps != ApartmentState.STA) return;
        Type t = this.GetType();
        Type t1 = Type.GetType(t.ToString() + "Dlg");
        while (t1 is null && t.BaseType.Name != "ModuleBase")
        {
            t = t.BaseType;
            t1 = Type.GetType(t.ToString() + "Dlg");
        }
        if (t1 is null) return;
        //if (dlg is not null) dlg.Close();
        if (dlg is not null) dlg.Close();

        dlg = (ModuleBaseDlg)Activator.CreateInstance(t1);
        if (dlg is null) return;
        dlg.ParentModule = (ModuleBase)this;
        dlg.Closed += Dlg_Closed;
        dlg.Closing += Dlg_Closing;
        dlg.LocationChanged += Dlg_LocationChanged;
        dlg.SizeChanged += Dlg_SizeChanged;

        // we need to set the dialog owner so it will display properly
        // this hack is here because a file might load and create dialogs prior to the mainwindow opening
        // so the same functionality is called from within FileLoad
        Window mainWindow = Application.Current.MainWindow;
        if (mainWindow.GetType() == typeof(MainWindow))
            dlg.Owner = Application.Current.MainWindow;

        //restore the size and position
        if (dlgPos != new Point(0, 0))
        {
            dlg.Top = dlgPos.Y;
            dlg.Left = dlgPos.X;
        }
        else
        {
            dlg.Top = 250;
            dlg.Left = 250;
        }
        if (dlgSize != new Point(0, 0))
        {
            dlg.Width = dlgSize.X;
            dlg.Height = dlgSize.Y;
        }



#if !DEBUG
        if (GetType().ToString() != "BrainSimulator.Modules.ModuleUserInterface" && !GetType().ToString().StartsWith("BrainSimulator.Modules.ModuleUI_"))
            dlg.WindowState = WindowState.Minimized;
#endif

        dlg.Show();
        dlgIsOpen = true;

#if !DEBUG
        if (GetType().ToString() != "BrainSimulator.Modules.ModuleUserInterface" && !GetType().ToString().StartsWith("BrainSimulator.Modules.ModuleUI_"))
        dlg.Hide();
#endif
    }

    public  string GetSavedDlgAttribute(string attribName)
    {
        Thought thisDlg = theUKS.Labeled(Label);
        if (thisDlg is null) return null;   
        foreach (var r in thisDlg.LinksTo)
        {
            if (r.LinkType.Label == "hasAttribute" && r.To.Label.StartsWith(attribName))
            {
                string retVal = (string)r.To.V;
                return retVal;
            }
        }
        return null;
    }
    public void SetSavedDlgAttribute(string attribName, string attribValue)
    {
        if (string.IsNullOrEmpty(attribName)) { return; }
        Thought thisDlg = theUKS.Labeled(Label);
        if (thisDlg is null) { return; }
        foreach (var r in thisDlg.LinksTo)
        {
            if (r.LinkType.Label == "hasAttribute" && r.To.Label.StartsWith(attribName))
            {
                if (attribValue is null)
                {
                    theUKS.DeleteThought(r.To);
                    return;
                }
                r.To.V = attribValue;
                return;
            }
        }
        if (attribName is null) return;
        Thought dlgAttribParent = theUKS.GetOrAddThought("DlgAttrib", "BrainSim");
        Thought dlgInfo = theUKS.AddThought(attribName, dlgAttribParent);
        Thought hasAttribute = theUKS.GetOrAddThought("hasAttribute", "LinkType");
        thisDlg.AddLink(dlgInfo,hasAttribute);
        dlgInfo.V = attribValue;
        dlgInfo.Fire();
    }
    string GetDlgWindow()
    {
        return GetSavedDlgAttribute("DlgWindow");
    }
    void SetDlgWindow()
    {
        string infoString = "";
        if (dlg is not null)
            infoString = dlg.Width.ToString("F0") + "x" + dlg.Height.ToString("F0") + "+" + dlg.Left.ToString("F0") + "+" + dlg.Top.ToString("F0");
        SetSavedDlgAttribute("DlgWindow", infoString);

    }
    private void Dlg_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        dlgSize = new Point()
        { Y = dlg.Height, X = dlg.Width };
        SetDlgWindow();
    }

    private void Dlg_LocationChanged(object sender, EventArgs e)
    {
        dlgPos = new Point() { Y = dlg.Top, X = dlg.Left };
        SetDlgWindow();
    }

    private void Dlg_Closed(object sender, EventArgs e)
    {
        if (dlg is null)
            dlgIsOpen = false;
        SetSavedDlgAttribute("Open", "True");
    }

    private void Dlg_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        dlg = null;
    }

    private DispatcherTimer timer;
    private DateTime dt;
    public TimeSpan DialogLockSpan = new TimeSpan(0, 0, 0, 0, 500);
    public void UpdateDialog()
    {
        // only actually update the screen every 500ms
        TimeSpan ts = DateTime.Now - dt;
        if (ts < DialogLockSpan)
        {
            //if we're not drawing this time, start a timer which will do a final draw
            if (timer is null)
            {
                timer = new DispatcherTimer();
                timer.Interval = new TimeSpan(0, 0, 0, 0, 100);
                timer.Tick += Timer_Tick;
            }
            timer.Stop();
            timer.Start();
            return;
        }
        dt = DateTime.Now;
        if (timer is not null) timer.Stop();

        if (dlg is not null)
            Application.Current.Dispatcher.InvokeAsync(new Action(() =>
            {
                dlg?.Draw(true);
            }));
    }
    public void Timer_Tick(object sender, EventArgs e)
    {
        timer.Stop();
        if (Application.Current is null) return;
        if (dlg is not null)
            dlg.Draw(true);
    }

    //this is called to allow for any data massaging needed before saving the file
    public virtual void SetUpBeforeSave()
    { }
    //this is called to allow for any data massaging needed after loading the file
    public virtual void SetUpAfterLoad()
    { }
    public virtual void Closing()
    { }
    public virtual void SizeChanged()
    { }

    public virtual MenuItem CustomContextMenuItems()
    {
        return null;
    }
}
