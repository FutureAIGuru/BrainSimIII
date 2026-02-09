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
using BrainSimulator.Modules;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BrainSimulator
{
    public partial class MainWindow : Window
    {
        public static readonly DependencyProperty moduleNameProperty =
            DependencyProperty.Register("moduleName", typeof(string), typeof(MenuItem));

        public void CreateContextMenu(ModuleBase nr, FrameworkElement r, ContextMenu cm = null) //for a selection
        {
            //cmCancelled = false;
            if (cm is null)
                cm = new ContextMenu();
            cm.SetValue(moduleNameProperty, nr.Label);

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

            ModuleBase m = activeModules.FindFirst(x => x.Label == nr.Label);
            int i = activeModules.IndexOf(m);

            if (activeModules[i] is not null)
            {
                var t = activeModules[i].GetType();
                Type t1 = Type.GetType(t.ToString() + "Dlg");
                while (t1 is null && t.BaseType.Name != "ModuleBase")
                {
                    t = t.BaseType;
                    t1 = Type.GetType(t.ToString() + "Dlg");
                }
                if (t1 is not null)
                {
                    cm.Items.Add(new MenuItem { Header = "Show Dialog" });
                    ((MenuItem)cm.Items[cm.Items.Count - 1]).Click += Mi_Click;
                    cm.Items.Add(new MenuItem { Header = "Hide Dialog" });
                    ((MenuItem)cm.Items[cm.Items.Count - 1]).Click += Mi_Click;
                }
            }
        }


        private void Mi_Click(object sender, RoutedEventArgs e)
        {
            //Handle delete  & initialize commands
            if (sender is MenuItem mi)
            {
                string moduleName = (string)mi.Parent.GetValue(moduleNameProperty);
                ModuleBase m = activeModules.FindFirst(x => x.Label == moduleName);
                int i = activeModules.IndexOf(m);
                if ((string)mi.Header == "View Source" || (string)mi.Header == "View Dialog Source")
                {
                    string theModuleType = m.GetType().Name.ToString();

                    if ((string)mi.Header == "View Dialog Source")
                        theModuleType += "Dlg.xaml";

                    string cwd = System.IO.Directory.GetCurrentDirectory();
                    if (cwd.Contains("bin\\"))
                        cwd = cwd.ToLower().Substring(0, cwd.IndexOf("bin\\"));
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
                    if (i >= 0)
                    {
                        DeleteModule(moduleName);
//                        deleted = true;
                    }
                }
                if ((string)mi.Header == "Initialize")
                {
                    if (i < 0)
                    {
                    }
                    else
                    {
                        {
                            try
                            {
                                activeModules[i].Initialize();
                            }
                            catch (Exception e1)
                            {
                                MessageBox.Show("Initialize failed on module " + activeModules[i].Label + ".   Message: " + e1.Message);
                            }
                        }

                    }
                }
                if ((string)mi.Header == "Show Dialog")
                {
                    activeModules[i].OpenDlg();
                }
                if ((string)mi.Header == "Hide Dialog")
                {
                    activeModules[i].CloseDlg();
                }
                if ((string)mi.Header == "Info...")
                {
                    string theModuleType = m.GetType().Name.ToString();
                    ModuleDescriptionDlg md = new ModuleDescriptionDlg(theModuleType);
                    md.ShowDialog();
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

        public void DeleteModule(string moduleName)
        {
            ModuleBase mb = activeModules.FindFirst(x => x.Label == moduleName);
            mb.CloseDlg();
            mb.Closing();
            activeModules.Remove(mb);
            theUKS.DeleteThought(theUKS.Labeled(mb.Label));

            ReloadActiveModulesSP();
        }
    }
}
