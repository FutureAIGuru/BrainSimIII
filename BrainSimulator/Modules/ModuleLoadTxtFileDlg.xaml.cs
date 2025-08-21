//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System.IO;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BrainSimulator.Modules
{
    public partial class ModuleLoadTxtFileDlg : ModuleBaseDlg
    {
        public ModuleLoadTxtFileDlg()
        {
            InitializeComponent();
        }

        public override bool Draw(bool checkDrawTimer)
        {
            if (!base.Draw(checkDrawTimer)) return false;
            //this has a timer so that no matter how often you might call draw, the dialog
            //only updates 10x per second
            ModuleLoadTxtFile parent = (ModuleLoadTxtFile)base.ParentModule;
            return true;
        }

        private void TheGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }

        public string SelectedPath
        {
            get => PathText.Text.Trim();
            set => PathText.Text = value ?? string.Empty;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new System.Windows.Forms.OpenFileDialog
            {
                Title = "Select UKS .txt file",
                Filter = "UKS text (*.txt)|*.txt|All files (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false
            };
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                SelectedPath = dlg.FileName;
        }

        private async void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "";
            var path = SelectedPath;

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
                BrowseButton.IsEnabled = false;
                CancelButton.IsEnabled = false;

                // Run ingest off the UI thread to keep the window responsive
                ModuleLoadTxtFile parent = (ModuleLoadTxtFile)base.ParentModule;
              
                await Task.Run(() => parent.IngestTxt(parent.theUKS, path));

            }
            catch (Exception ex)
            {
                Mouse.OverrideCursor = null;
                ImportButton.IsEnabled = true;
                BrowseButton.IsEnabled = true;
                CancelButton.IsEnabled = true;

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
                BrowseButton.IsEnabled = true;
                CancelButton.IsEnabled = true;
            }
        }
    }
}

