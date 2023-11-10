//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Forms;

namespace BrainSimulator.Modules
{
    public partial class ModuleUI_SettingsDlg : ModuleBaseDlg
    {
        public ModuleUI_SettingsDlg()
        {
            InitializeComponent();
        }

        public override bool Draw(bool checkDrawTimer)
        {
            if (!base.Draw(checkDrawTimer)) return false;
            //this has a timer so that no matter how often you might call draw, the dialog
            //only updates 10x per second

            //use a line like this to gain access to the parent's public variables
            //ModuleEmpty parent = (ModuleEmpty)base.ParentModule;

            //here are some other possibly-useful items
            //theGrid.Children.Clear();
            //Point windowSize = new Point(theGrid.ActualWidth, theGrid.ActualHeight);
            //Point windowCenter = new Point(windowSize.X / 2, windowSize.Y / 2);
            //float scale = (float)Math.Min(windowSize.X, windowSize.Y) / 12;
            //if (scale == 0) return false;

            return true;
        }

        private void TheGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }

        private void ModuleBaseDlg_LostFocus(object sender, RoutedEventArgs e)
        {

        }

        public void UpdateComboBox(List<string> wakeWords)
        {
            WakeWordComboBox.Items.Clear();
            foreach(string name in wakeWords) WakeWordComboBox.Items.Add(name);

            ModuleUserInterfaceDlg UI = (ModuleUserInterfaceDlg)Owner;
            WakeWordComboBox.SelectedItem = UI.PodName;
        }

        private void WakeWordComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ModuleUserInterfaceDlg UI = (ModuleUserInterfaceDlg)Owner;
            if(WakeWordComboBox.SelectedItem != null)
                UI.SaveName(WakeWordComboBox.SelectedItem.ToString());
        }

        private void RecalibrateButton_Click(object sender, RoutedEventArgs e)
        {
            ModuleUserInterfaceDlg UI = (ModuleUserInterfaceDlg)Owner;
            if (UI != null) UI.CalibratePod();
        }

        private void SnapshotDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            ModuleUserInterfaceDlg UI = (ModuleUserInterfaceDlg)Owner;
            if (UI == null) return;

            FolderBrowserDialog folderDialog = new()
            {
                Description = "Select location where pictures will be saved",
                InitialDirectory = Utils.GetOrAddDocumentsSubFolder(Utils.FolderUISavedImages)
            };

            DialogResult result = folderDialog.ShowDialog();

            if(result == System.Windows.Forms.DialogResult.OK)
            {
                  UI.imageSaveLocation = folderDialog.SelectedPath;
            }

            folderDialog.Dispose();
        }
    }
}