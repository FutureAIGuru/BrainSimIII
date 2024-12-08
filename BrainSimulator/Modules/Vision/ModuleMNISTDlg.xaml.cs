//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using System;
using System.Windows;
using System.Windows.Controls;


namespace BrainSimulator.Modules
{
    public partial class ModuleMNISTDlg : ModuleBaseDlg
    {
        // Constructor of the ModuleUKSStatement dialog
        public ModuleMNISTDlg()
        {
            InitializeComponent();
        }

        // Draw gets called to draw the dialog when it needs refreshing
        public override bool Draw(bool checkDrawTimer)
        {
            if (!base.Draw(checkDrawTimer)) return false;

            ModuleMNIST parent = (ModuleMNIST)base.ParentModule;
            if (parent == null) return false;
            digitLabel.Text = "Target Digit: " + parent.theDigit.ToString();
            ModuleShape ms = (ModuleShape)MainWindow.theWindow.GetModule("ModuleShape0");
            if (ms == null) return true;
            string mnistMessage = "\nHits: " + ms.mnistHitCount +
                "\nAsks: " + ms.mnistAskCount +
                "\nErrs: " + ms.mnistMissCount +
                "\n" + ms.mnistMessage;
            digitLabel.Text = mnistMessage;

            return true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b)
            {
                ModuleMNIST parent = (ModuleMNIST)base.ParentModule;
                if (b.Content.ToString() == "Train")
                {
                    parent.LoadFileList("C:\\Users\\c_sim\\source\\repos\\WORKING\\BrainSimulator\\TestImages\\OtherImages\\Digits");
                }
                else if (b.Content.ToString() == "Run/Stop")
                {
                    parent.running = !parent.running;
                }
                else if (b.Content.ToString() == "Step")
                {
                    parent.running = false;
                    parent.step = true;
                }
                else if (b.Content.ToString() == "Folder for test data")
                {
                    //open the folder dialog
                    System.Windows.Forms.FolderBrowserDialog dlg = new System.Windows.Forms.FolderBrowserDialog();
                    dlg.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\brainsim\\MNIST\\mnist_png\\training";
                    System.Windows.Forms.DialogResult result = dlg.ShowDialog();
                    if (result == System.Windows.Forms.DialogResult.OK)
                    {
                        parent.LoadFileList(dlg.SelectedPath);
                    }
                }
            }
        }
    }
}
