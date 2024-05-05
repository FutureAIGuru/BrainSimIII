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
using System.Windows.Threading;

namespace BrainSimulator.Modules
{
    abstract public class ModuleBase
    {
        //this is public so it will be included in the saved xml file.  That way
        //initialized data content can be preserved from run to run and only reinitialized when requested.
        public bool initialized = false;

        public bool isEnabled = true;
        public string Label = "";

        protected ModuleBaseDlg dlg = null;
        public Point dlgPos;
        public Point dlgSize;
        public bool dlgIsOpen = false;
        protected bool allowMultipleDialogs = false;
        private List<ModuleBaseDlg> dlgList = null;

        [XmlIgnore]
        //public static ModuleUKS UKS = null;
        public UKS.UKS UKS = null;

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
            UKS = MainWindow.theUKS;
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

        protected void Init(bool forceInit = false)
        {
            // SetModuleView();

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

        public void CloseDlg()
        {
            if (dlgList != null)
                for (int i = dlgList.Count - 1; i >= 0; i--)
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

            // we need to set the dialog owner so it will display properly
            // this hack is here because a file might load and create dialogs prior to the mainwindow opening
            // so the same functionality is called from within FileLoad
            Window mainWindow = Application.Current.MainWindow;
            if (mainWindow.GetType() == typeof(MainWindow))
                dlg.Owner = Application.Current.MainWindow;
            // else
            //     Utils.Noop();

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
    }
}
