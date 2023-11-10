//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System.Windows;

namespace BrainSimulator.Modules
{
    public partial class ModulePodActionDlg : ModuleBaseDlg
    {
        public ModulePodActionDlg()
        {
            InitializeComponent();

            this.DataContext = this;
        }

        string theConfigString;
        ModulePodAction parent = null;
        public string TheConfigString
        {
            get
            {
                if (parent == null)
                {
                    parent = (ModulePodAction)base.ParentModule;
                    theConfigString = parent.ActionsString;
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

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            ModulePodAction parent = (ModulePodAction)base.ParentModule;
            parent.ActionsString = theConfigString;
            Close();
        }
    }
}
