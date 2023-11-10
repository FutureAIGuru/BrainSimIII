//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System.Windows;
using System;

namespace BrainSimulator.Modules
{
    public partial class ModulePodDlg : ModuleBaseDlg
    {
        public ModulePodDlg()
        {
            InitializeComponent();

            this.DataContext = this;


        }

        string theConfigString;
        ModulePod parent = null;
        public string TheConfigString
        {
            get
            {
                if (parent == null)
                {
                    parent = (ModulePod)base.ParentModule;
                    theConfigString = parent.configString;
                }
                return theConfigString;
            }
            set
            {
                theConfigString = value;
            }
        }


        public override bool Draw(bool checkDrawTimer)
        {
            //not used
            if (!base.Draw(checkDrawTimer)) return false;
            return true;
        }

        private void TheCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            ModulePod parent = (ModulePod)base.ParentModule;
            parent.configString = theConfigString;
            Close();
        }

        private void RebootClick(object sender, RoutedEventArgs e)
        {
            ModulePod parent = (ModulePod)base.ParentModule;
            parent.Reboot_ESP();
        }

        private void RecalibrateClick(object sender, RoutedEventArgs e)
        {
            ModulePod parent = (ModulePod)base.ParentModule;
            parent.Recalibrate_Mpu();
        }

        private void ResetYaw_Click(object sender, RoutedEventArgs e)
        {
            ModulePod parent = (ModulePod)base.ParentModule;
            parent.ResetYaw();
        }

        private void ConfirmRatio_Click(object sender, RoutedEventArgs e)
        {
            ModulePod parent = (ModulePod)base.ParentModule;
            if (msRatiocb.SelectedItem != null)
                parent.RealMoveRatio = (float)msRatiocb.SelectedItem;
        }

        private void RefreshRatio_Click(object sender, RoutedEventArgs e)
        {
            msRatiocb.Items.Clear();
            ModulePod parent = (ModulePod)base.ParentModule;
            foreach (float item in parent.RealMoveRatios)
            {
                msRatiocb.Items.Add(item);
            }
        }
    }
}
