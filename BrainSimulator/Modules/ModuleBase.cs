//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Xml.Serialization;
using Emgu.CV;
using System.Windows.Threading;
using System.Diagnostics;

namespace BrainSimulator.Modules
{
    abstract public class ModuleBase
    {
        private NeuronArray theArray;
        protected ModuleView mv = null;

        //this is public so it will be included in the saved xml file.  That way
        //initialized data content can be preserved from run to run and only reinitialized when requested.
        public bool initialized = false;

        protected int minWidth = 2;
        protected int maxWidth = 100;
        protected int minHeight = 2;
        protected int maxHeight = 100;
        public int MinWidth => minWidth;
        public int MinHeight => minHeight;
        public int MaxWidth => maxWidth;
        public int MaxHeight => maxHeight;
        public bool isEnabled = true;

        protected ModuleBaseDlg dlg = null;
        public Point dlgPos;
        public Point dlgSize;
        public bool dlgIsOpen = false;
        protected bool allowMultipleDialogs = false;
        private List<ModuleBaseDlg> dlgList = null;

        public ModuleBase() 
        {
        }

        public void SetNeuronArray(NeuronArray theArray)
        {
            this.theArray = theArray;
        }

        public NeuronArray GetNeuronArray()
        {
            if (this.theArray == null)
            {
                throw new Exception("No NeuronArray set on module");
            }
            return this.theArray;
        }

        abstract public void Fire();

        abstract public void Initialize();
        public virtual void SetInputImage(Mat inputImage, string inputFilename)
        {
        }
        
        public virtual void UKSInitializedNotification()
        {
        }

        public void UKSInitialized()
        {
            foreach (ModuleView na1 in GetNeuronArray().modules)
            {
                if (na1.TheModule.isEnabled)
                    na1.TheModule.UKSInitializedNotification();
            }
        }

        public virtual void UKSReloadedNotification()
        {
        }

        public void UKSReloaded()
        {
            foreach (ModuleView na1 in GetNeuronArray().modules)
            {
                if (na1.TheModule.isEnabled)
                    na1.TheModule.UKSReloadedNotification();
            }
        }

        [XmlIgnore]
        public  ModuleUKS UKS = null;
        public void GetUKS()
        {
            if (UKS is null)
            {
                UKS = (ModuleUKS)FindModule(typeof(ModuleUKS));
            }
        }

        private List<string> notFoundModules = new();

        public void MarkModuleTypeAsNotLoaded(string typeName)
        {
            string moduleName = typeName;
            if (typeName.StartsWith("BrainSimulator.Modules."))
            {
                moduleName = moduleName[23..];
            }
            if (!notFoundModules.Contains(moduleName))
            {
                notFoundModules.Add(moduleName);
                MessageBox.Show(" Module of type " + moduleName + " does not exist in this network.", "Module Not Found", MessageBoxButton.OK,
                    MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
            }
        }

        public void MarkModuleNameAsNotLoaded(string moduleName)
        {
            if (!notFoundModules.Contains(moduleName))
            {
                notFoundModules.Add(moduleName);
                MessageBox.Show("Module named " + moduleName + " does not exist in this network.", "Module Not Found", MessageBoxButton.OK,
                    MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
            }
        }

        public void ClearMissingModules()
        {
            notFoundModules.Clear();
        }
        
        public bool IsEnabled(bool silent=true)
        {
            if (!silent && !isEnabled)
            {
                MessageBox.Show(this.GetType().Name + " is not enabled", "Module Not Enabled", MessageBoxButton.OK,
                    MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
            }
            return isEnabled;
        }
        protected void Init(bool forceInit = false)
        {
            SetModuleView();

            if (initialized && !forceInit) return;
            initialized = true;

            Initialize();

            UpdateDialog();

            if (dlg == null && dlgIsOpen)
            {
                ShowDialog();
                dlgIsOpen = true;
            }
        }

        public void SetModuleView()
        {
            if (mv == null)
            {
                // this fails for ModuleTester because theNeuronArray is null!
                foreach (ModuleView na1 in GetNeuronArray().modules)
                {
                    if (na1.TheModule == this)
                    {
                        mv = na1;
                        break;
                    }
                }
            }
        }

        public void CloseDlg()
        {
            if (dlgList != null)
            for (int i = dlgList.Count-1 ; i >= 0; i--)
            {
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    dlgList[i].Close();
                });
            }
            if (dlg != null)
            {
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    dlg.Close();
                });
            }
        }

        //used by mainwindow to determine whether or not activation is needed
        public void Activate()
        {
            if (dlg == null) return;
            dlg.Activate();
        }
        public bool IsActive()
        {
            if (dlg == null) return false;
            return dlg.IsActive;
        }

        public virtual void ShowDialog()
        {
            ApartmentState aps = Thread.CurrentThread.GetApartmentState();
            if (aps != ApartmentState.STA) return;
            Type t = this.GetType();
            Type t1 = Type.GetType(t.ToString() + "Dlg");
            while (t1 == null && t.BaseType.Name != "ModuleBase")
            {
                t = t.BaseType;
                t1 = Type.GetType(t.ToString() + "Dlg");
            }
            if (t1 == null) return;
            //if (dlg != null) dlg.Close();
            if (!allowMultipleDialogs && dlg != null) dlg.Close();
            if (allowMultipleDialogs && dlg != null)
            {
                if (dlgList == null) dlgList = new List<ModuleBaseDlg>();
                dlgList.Add(dlg);
                dlgPos.X += 10;
                dlgPos.Y += 10;
            }
            dlg = (ModuleBaseDlg)Activator.CreateInstance(t1);
            if (dlg == null) return;
            dlg.ParentModule = (ModuleBase)this;
            dlg.Closed += Dlg_Closed;
            dlg.Closing += Dlg_Closing;
            dlg.LocationChanged += Dlg_LocationChanged;
            dlg.SizeChanged += Dlg_SizeChanged;

            //we need to set the dialog owner so it will display properly
            //this hack is here because a file might load and create dialogs prior to the mainwindow opening
            //so the same functionality is called from within FileLoad
            Window mainWindow = Application.Current.MainWindow;
            if (mainWindow.GetType() == typeof(MainWindow))
                dlg.Owner = Application.Current.MainWindow;
            else
                Utils.Noop();

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

            if (mainWindow.ActualWidth > 800) //try to keep dialogs on the screen
            {
                if (dlg.Width + dlg.Left > mainWindow.ActualWidth)
                    dlg.Left = mainWindow.ActualWidth - dlg.Width;
                if (dlg.Height + dlg.Top > mainWindow.ActualHeight)
                    dlg.Top = mainWindow.ActualHeight - dlg.Height;
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

        //this hack is here because a file can load and create dialogs prior to the mainwindow opening
        public void SetDlgOwner(Window MainWindow)
        {
            if (dlg != null)
                dlg.Owner = MainWindow;
        }

        public void MoveDialog(double x, double y)
        {
            if (dlg != null)
            {
                dlg.Top += y;
                dlg.Left += x;
            }
            if (dlgList != null)
            {
                foreach (var d in dlgList)
                {
                    d.Top += y;
                    d.Left += x;
                }
            }
        }
        private void Dlg_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            dlgSize = new Point()
            { Y = dlg.Height, X = dlg.Width };
        }

        private void Dlg_LocationChanged(object sender, EventArgs e)
        {
            dlgPos = new Point()
            { Y = dlg.Top, X = dlg.Left };
        }

        private void Dlg_Closed(object sender, EventArgs e)
        {
           if (dlg == null) 
                dlgIsOpen = false;
        }

        private void Dlg_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (dlgList != null && dlgList.Count > 0)
            {
                if (dlgList.Contains((ModuleBaseDlg)sender))
                {
                    dlgList.Remove((ModuleBaseDlg)sender);
                }
                else
                {
                    dlg = dlgList[0];
                    dlgList.RemoveAt(0);
                }
            }
            else
                dlg = null;
        }

        private DispatcherTimer timer;
        private DateTime dt;
        [XmlIgnore]
        public TimeSpan DialogLockSpan = new TimeSpan(0, 0, 0, 0, 500);
        public void UpdateDialog()
        {
            // only actually update the screen every 500ms
            TimeSpan ts = DateTime.Now - dt;
            if (ts < DialogLockSpan)
            {
                //if we're not drawing this time, start a timer which will do a final draw
                if (timer == null)
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
            if (timer != null) timer.Stop();

            if (dlg != null)
                Application.Current.Dispatcher.InvokeAsync(new Action(() =>
                {
                    dlg?.Draw(true);
                }));
        }
        public void Timer_Tick(object sender, EventArgs e)
        {
            timer.Stop();
            if (Application.Current == null) return;
            if (dlg != null)
                dlg.Draw(false);
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

        public ModuleBase FindModule(Type t, bool suppressWarning = true)
        {
            if (GetNeuronArray() == null) return null;
            foreach (ModuleView na1 in GetNeuronArray().modules)
            {
                if (na1.TheModule != null && na1.TheModule.GetType() == t)
                {
                    return na1.TheModule;
                }
            }
            if (!suppressWarning)
                MarkModuleTypeAsNotLoaded(t.ToString());
            return null;
        }

        public ModuleBase FindModule(string name, bool suppressWarning = true)
        {
            foreach (ModuleView na1 in GetNeuronArray().modules)
            {
                if (na1.Label == name)
                {
                    return na1.TheModule;
                }
            }
            if (!suppressWarning)
                MarkModuleNameAsNotLoaded("Module" + name);
            return null;
        }
    }
}
