using BrainSimulator.Modules;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BrainSimulator
{
    public partial class ModuleView : DependencyObject
    {
        public static readonly DependencyProperty AreaNumberProperty =
            DependencyProperty.Register("AreaNumber", typeof(int), typeof(MenuItem));

        public static void CreateContextMenu(int i, ModuleBase nr, FrameworkElement r, ContextMenu cm = null) //for a selection
        {
            cmCancelled = false;
            if (cm == null)
                cm = new ContextMenu();
            cm.SetValue(AreaNumberProperty, i);
            cm.PreviewKeyDown += Cm_PreviewKeyDown;

            StackPanel sp;
            MenuItem mi = new MenuItem();
            mi = new MenuItem();
            mi.Header = "Delete";
            mi.Click += Mi_Click;
            cm.Items.Add(mi);

            mi = new MenuItem();
            mi.Header = "Initialize";
            mi.Click += Mi_Click;
            cm.Items.Add(mi);

            mi = new MenuItem();
            mi.Header = "View Source";
            mi.Click += Mi_Click;
            cm.Items.Add(mi);

            mi = new MenuItem();
            mi.Header = "View Dialog Source";
            mi.Click += Mi_Click;
            cm.Items.Add(mi);

            mi = new MenuItem();
            mi.Header = "Info...";
            mi.Click += Mi_Click;
            cm.Items.Add(mi);

            mi = new MenuItem();
            //CheckBox cb1 = new CheckBox { Name = "Enabled", Content = "Enabled", IsChecked = nr.isEnabled, };
            //mi.Header = cb1;
            //cb1.Checked += Cb1_Checked;
            //cb1.Unchecked += Cb1_Checked;
            //cm.Items.Add(mi);

            if (MainWindow.BrainSim3Data.modules[i] != null)
            {
                var t = MainWindow.BrainSim3Data.modules[i].GetType();
                Type t1 = Type.GetType(t.ToString() + "Dlg");
                while (t1 == null && t.BaseType.Name != "ModuleBase")
                {
                    t = t.BaseType;
                    t1 = Type.GetType(t.ToString() + "Dlg");
                }
                if (t1 != null)
                {
                    cm.Items.Add(new MenuItem { Header = "Show Dialog" });
                    ((MenuItem)cm.Items[cm.Items.Count - 1]).Click += Mi_Click;
                    cm.Items.Add(new MenuItem { Header = "Hide Dialog" });
                    ((MenuItem)cm.Items[cm.Items.Count - 1]).Click += Mi_Click;
                }
            }

            //if (nr.CustomContextMenuItems() is MenuItem miCustom)
            //{
            //    cm.Items.Add(miCustom);
            //}

            sp = new StackPanel { Orientation = Orientation.Horizontal };
            Button b0 = new Button { Content = "OK", Width = 100, Height = 25, Margin = new Thickness(10) };
            b0.Click += B0_Click;
            sp.Children.Add(b0);
            b0 = new Button { Content = "Cancel", Width = 100, Height = 25, Margin = new Thickness(10) };
            b0.Click += B0_Click;
            sp.Children.Add(b0);

            cm.Items.Add(new MenuItem { Header = sp, StaysOpenOnClick = true });

            cm.Closed += Cm_Closed;
        }

        private static void Cb1_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb)
            {
                if (cb.Parent is MenuItem mi)
                    if (mi.Parent is ContextMenu cm)
                    {
                        Cm_Closed(cm, null);
                    }
            }
        }

        private static void Cm_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            ContextMenu cm = sender as ContextMenu;
            if (e.Key == Key.Enter)
            {
                Cm_Closed(sender, e);
            }
            if (e.Key == Key.Delete)
            {
                IInputElement focusedControl = Keyboard.FocusedElement;
                if (focusedControl.GetType() != typeof(TextBox))
                {
                    int i = (int)cm.GetValue(AreaNumberProperty);
                    DeleteModule(i);
                    MainWindow.Update();
                    deleted = true;
                    cm.IsOpen = false;
                }

            }
        }

        static bool cmCancelled = false;
        private static void B0_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b)
            {
                if (b.Parent is StackPanel sp)
                {
                    if (sp.Parent is MenuItem mi)
                    {
                        if (mi.Parent is ContextMenu cm)
                        {
                            if ((string)b.Content == "Cancel")
                                cmCancelled = true;
                            Cm_Closed(cm, e);
                        }
                    }
                }
            }
        }


        static bool deleted = false;
        private static void Cm_Closed(object sender, RoutedEventArgs e)
        {
            if ((Keyboard.GetKeyStates(Key.Escape) & KeyStates.Down) > 0)
            {
                MainWindow.Update();
                return;
            }
            if (deleted)
            {
                deleted = false;
            }
            else if (sender is ContextMenu cm)
            {
                if (!cm.IsOpen) return;
                cm.IsOpen = false;
                if (cmCancelled) return;

                int i = (int)cm.GetValue(AreaNumberProperty);
                string label = "";
                string theModuleTypeStr = "";
                Color color = Colors.Wheat;
                int width = 1, height = 1;

                Control cc = Utils.FindByName(cm, "AreaName");
                if (cc is TextBox tb)
                    label = tb.Text;
                cc = Utils.FindByName(cm, "Enabled");
                bool isEnabled = true;
                if (cc is CheckBox cb2)
                    isEnabled = (bool)cb2.IsChecked;

                cc = Utils.FindByName(cm, "AreaWidth");
                if (cc is TextBox tb1)
                    int.TryParse(tb1.Text, out width);
                cc = Utils.FindByName(cm, "AreaHeight");
                if (cc is TextBox tb2)
                    int.TryParse(tb2.Text, out height);
                cc = Utils.FindByName(cm, "AreaType");
                if (cc is ComboBox cb && cb.SelectedValue != null)
                {
                    theModuleTypeStr = "Module" + (string)cb.SelectedValue;
                    if (theModuleTypeStr == "") return;//something went wrong
                    label = (string)cb.SelectedValue;
                }

                cc = Utils.FindByName(cm, "AreaColor");
                if (cc is ComboBox cb1)
                    color = ((SolidColorBrush)((ComboBoxItem)cb1.SelectedValue).Background).Color;
                if (label == "" && theModuleTypeStr == "") return;

                ModuleBase theModule = MainWindow.BrainSim3Data.modules[i];

                //update the existing module
                theModule.Label = label;

                //did we change the module type?
                Type t1x = Type.GetType("BrainSimulator.Modules." + theModuleTypeStr);
                if (t1x != null && (MainWindow.BrainSim3Data.modules[i] == null || MainWindow.BrainSim3Data.modules[i].GetType() != t1x))
                {
                    MainWindow.BrainSim3Data.modules[i] = (ModuleBase)Activator.CreateInstance(t1x);
                    MainWindow.BrainSim3Data.modules[i].Label = theModuleTypeStr;
                }
            }
            MainWindow.Update();
            MainWindow.ReloadLoadedModules();
        }

        private static void Mi_Click(object sender, RoutedEventArgs e)
        {
            //Handle delete  & initialize commands
            if (sender is MenuItem mi)
            {
                if ((string)mi.Header == "View Source" || (string)mi.Header == "View Dialog Source")
                {
                    int i = (int)mi.Parent.GetValue(AreaNumberProperty);
                    ModuleBase m = MainWindow.BrainSim3Data.modules[i];
                    string theModuleType = m.GetType().Name.ToString();

                    if ((string)mi.Header == "View Dialog Source")
                        theModuleType += "Dlg.xaml";

                    string cwd = System.IO.Directory.GetCurrentDirectory();
                    cwd = cwd.ToLower().Replace("bin\\debug\\net6.0-windows", "");
                    string fileName = cwd + @"modules\" + theModuleType + ".cs";
                    if (File.Exists(fileName))
                        OpenSource(fileName);
                    else
                    {
                        fileName = cwd + @"BrainSim2modules\" + theModuleType + ".cs";
                        OpenSource(fileName);
                    }
                }
                if ((string)mi.Header == "Delete")
                {
                    int i = (int)mi.Parent.GetValue(AreaNumberProperty);
                    if (i >= 0)
                    {
                        DeleteModule(i);
                        deleted = true;
                    }
                }
                if ((string)mi.Header == "Initialize")
                {
                    int i = (int)mi.Parent.GetValue(AreaNumberProperty);
                    if (i < 0)
                    {
                    }
                    else
                    {
                        {
                            try
                            {
                                MainWindow.BrainSim3Data.modules[i].Initialize();
                            }
                            catch (Exception e1)
                            {
                                MessageBox.Show("Initialize failed on module " + MainWindow.BrainSim3Data.modules[i].Label + ".   Message: " + e1.Message);
                            }
                        }

                    }
                }
                if ((string)mi.Header == "Show Dialog")
                {
                    int i = (int)mi.Parent.GetValue(AreaNumberProperty);
                    if (i < 0)
                    {
                    }
                    else
                    {
                        MainWindow.BrainSim3Data.modules[i].ShowDialog();
                    }
                }
                if ((string)mi.Header == "Hide Dialog")
                {
                    int i = (int)mi.Parent.GetValue(AreaNumberProperty);
                    if (i < 0)
                    {
                    }
                    else
                    {
                        MainWindow.BrainSim3Data.modules[i].CloseDlg();
                    }
                }
                if ((string)mi.Header == "Info...")
                {
                    int i = (int)mi.Parent.GetValue(AreaNumberProperty);
                    if (i < 0)
                    {
                    }
                    else
                    {
                        ModuleBase m = MainWindow.BrainSim3Data.modules[i];
                        string theModuleType = m.GetType().Name.ToString();
                        ModuleDescriptionDlg md = new ModuleDescriptionDlg(theModuleType);
                        md.ShowDialog();
                    }
                }
            }
        }

        public static void OpenSource(string fileName)
        {
            Process process = new Process();
            string taskFile = @"C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\devenv.exe";
            ProcessStartInfo startInfo = new ProcessStartInfo(taskFile, "/edit " + fileName);
            process.StartInfo = startInfo;
            process.Start();
        }

        public static void DeleteModule(int i)
        {
            ModuleBase mb = MainWindow.BrainSim3Data.modules[i];
            mb.CloseDlg();
            mb.Closing();
            MainWindow.BrainSim3Data.modules.RemoveAt(i);

            MainWindow.ReloadLoadedModules();
        }
    }
}
