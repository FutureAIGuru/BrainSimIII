//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using UKS;

namespace BrainSimulator.Modules
{
    public partial class ModuleTextFileDlg : ModuleBaseDlg
    {
        public ModuleTextFileDlg()
        {
            InitializeComponent();
        }

        public override bool Draw(bool checkDrawTimer)
        {
            if (!base.Draw(checkDrawTimer)) return false;
            //this has a timer so that no matter how often you might call draw, the dialog
            //only updates 10x per second
            ModuleTextFile parent = (ModuleTextFile)base.ParentModule;
            return true;
        }

        private void TheGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }

        private string  Browse(bool open)
        {
            string path = "";
            FileDialog dlg;
            if (open)
            dlg = new OpenFileDialog
            {
                Title = "Select UKS .txt file",
                Filter = "UKS text (*.txt)|*.txt|All files (*.*)|*.*",
                CheckFileExists = false,
                Multiselect = false
            };
            else
                dlg = new SaveFileDialog
                {
                    Title = "Select UKS .txt file",
                    Filter = "UKS text (*.txt)|*.txt|All files (*.*)|*.*",
                    CheckFileExists = false,
                };
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                path = dlg.FileName;
            return path;
        }

        private async void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "";
            var path = Browse(true); ;
            if (string.IsNullOrEmpty(path)) return;
            
            if (string.IsNullOrWhiteSpace(path))
            {
                StatusText.Text = "Choose a file first.";
                return;
            }
            if (!File.Exists(path))
            {
                StatusText.Text = "File not found.";
                return;
            }

            try
            {
                ImportButton.IsEnabled = false;
                ExportButton.IsEnabled = false;

                // Run ingest off the UI thread to keep the window responsive
                ModuleTextFile parent = (ModuleTextFile)base.ParentModule;
              
                await Task.Run(() => parent.theUKS.ImportTextFile(path));

                StatusText.Text = "Success";
            }
            catch (Exception ex)
            {
                Mouse.OverrideCursor = null;
                ImportButton.IsEnabled = true;
                ExportButton.IsEnabled = true;

                // Show a friendly error, but include details for debugging.
                System.Windows.MessageBox.Show(this,
                    "Import failed.\n\n" + ex.Message,
                    "UKS Import",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
                ImportButton.IsEnabled = true;
                ExportButton.IsEnabled = true;
            }
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var path = Browse(true); ;
            if (string.IsNullOrEmpty(path)) return;

            if (string.IsNullOrWhiteSpace(path))
            {
                StatusText.Text = "Choose a file first.";
                return;
            }
            try {
                ImportButton.IsEnabled = false;
                ExportButton.IsEnabled = false;

                ModuleTextFile parent = (ModuleTextFile)base.ParentModule;

                //get the root to save the contents of from the UKS dialog root
                string root = "Object";
                Thing uksDlg = parent.theUKS.Labeled("ModuleUKS0");
                if (uksDlg != null) 
                foreach (var r in uksDlg.Relationships)
                {
                    if (r.reltype.Label == "hasAttribute" && r.target.Label.StartsWith("Root"))
                    {
                        root = (string)r.target.V;
                    }
                }
                await Task.Run(() => parent.theUKS.ExportTextFile(root, path));
                StatusText.Text = "Success";
            }
            catch (Exception ex)
            {
                Mouse.OverrideCursor = null;
                ImportButton.IsEnabled = true;
                ExportButton.IsEnabled = true;

                // Show a friendly error, but include details for debugging.
                System.Windows.MessageBox.Show(this,
                    "Import failed.\n\n" + ex.Message,
                    "UKS Import",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
                ImportButton.IsEnabled = true;
                ExportButton.IsEnabled = true;
            }
        }
    }
}

