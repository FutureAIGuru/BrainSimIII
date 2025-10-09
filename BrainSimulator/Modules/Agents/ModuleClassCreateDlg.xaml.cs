//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System.Windows;
using System.Windows.Controls;

namespace BrainSimulator.Modules
{
    public partial class ModuleClassCreateDlg : ModuleBaseDlg
    {
        public ModuleClassCreateDlg()
        {
            InitializeComponent();
        }

        public override bool Draw(bool checkDrawTimer)
        {
            if (!base.Draw(checkDrawTimer)) return false;
            //this has a timer so that no matter how often you might call draw, the dialog
            //only updates 10x per second
            ModuleClassCreate parent = (ModuleClassCreate)base.ParentModule;
            tbMessages.Text = parent.debugString;
            tbMessages.ScrollToEnd();
            return true;
        }

        private void TheGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ModuleClassCreate parent = (ModuleClassCreate)base.ParentModule;
            parent.DoTheWork();
        }

        private void Enable_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb)
            {
                ModuleClassCreate parent = (ModuleClassCreate)base.ParentModule;
                if (parent != null)
                    parent.isEnabled = cb.IsChecked == true;
            }
        }
    }
}
