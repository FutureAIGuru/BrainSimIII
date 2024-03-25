//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;


using System.Windows.Shapes;
using System.Windows.Ink;

namespace BrainSimulator.Modules
{
    public partial class ModuleVisionDlg : ModuleBaseDlg
    {
        // Constructor of the ModuleUKSStatement dialog
        public ModuleVisionDlg()
        {
            InitializeComponent();
        }

        // Draw gets called to draw the dialog when it needs refreshing
        public override bool Draw(bool checkDrawTimer)
        {
            if (!base.Draw(checkDrawTimer)) return false;

            ModuleVision parent = (ModuleVision)base.ParentModule;

            imageDisplay.Source =  (ImageSource)parent.bitmap;

            theCanvas.Children.Clear();

            if (parent.boundaryArray != null) 
            {
                float scale = (float) (theCanvas.ActualWidth/ parent.boundaryArray.GetLength(0));
                for (int x = 0; x < parent.boundaryArray.GetLength(0); x++)
                    for (int y = 0; y < parent.boundaryArray.GetLength(1); y++)
                    {
                        if (parent.boundaryArray[x,y] != 0)
                        {
                            Ellipse e = new Ellipse() {
                                Height = 3, Width = 3,
                             Stroke = Brushes.Black,
                         };
                            Canvas.SetTop(e,y * scale);
                            Canvas.SetLeft(e,x * scale);
                            theCanvas.Children.Add(e);
                        }
                    }
            }

            return true;
        }
        string defaultDirectory = "";
        private void Button_Browse_Click(object sender, RoutedEventArgs e)
        {
            if (defaultDirectory == "")
            {
                defaultDirectory = System.IO.Path.GetDirectoryName(MainWindow.currentFileName);
            }
            OpenFileDialog openFileDialog1 = new OpenFileDialog
            {
                Filter = "Image Files| *.png;*.jpg",
                Title = "Select an image file",
                Multiselect = true,
                InitialDirectory = defaultDirectory,
            };
            // Show the Dialog.  
            // If the user clicked OK in the dialog  
            DialogResult result = openFileDialog1.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                defaultDirectory = System.IO.Path.GetDirectoryName(openFileDialog1.FileName);
                ModuleVision parent = (ModuleVision)base.ParentModule;

                textBoxPath.Text = openFileDialog1.FileName;
                List<string> fileList;
                string curPath;
                if (openFileDialog1.FileNames.Length > 1)
                {
                    fileList = new List<string>(openFileDialog1.FileNames);
                    curPath = fileList[0];
                }
                else
                {
                    fileList = GetFileList(openFileDialog1.FileName);
                    curPath = openFileDialog1.FileName;
                }

                parent.currentFilePath = curPath;
                //parent.SetParameters(fileList, curPath, (bool)cbAutoCycle.IsChecked, (bool)cbNameIsDescription.IsChecked);
            }
        }

        private List<string> GetFileList(string filePath)
        {
            SearchOption subFolder = SearchOption.AllDirectories;
            if (!(bool)cbSubFolders.IsChecked)
                subFolder = SearchOption.TopDirectoryOnly;
            string dir = filePath;
            FileAttributes attr = File.GetAttributes(filePath);
            if ((attr & FileAttributes.Directory) != FileAttributes.Directory)
                dir = System.IO.Path.GetDirectoryName(filePath);
            return new List<string>(Directory.EnumerateFiles(dir, "*.png", subFolder));
        }
    }
}
