//
// Copyright (c) FutureAI. All rights reserved.  
// Code is proprietary and may not be distributed without a separate licence
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

namespace BrainSimulator.Modules
{
    public partial class ModuleAvoidanceScannerDlg : ModuleBaseDlg
    {
        public ModuleAvoidanceScannerDlg()
        {
            InitializeComponent();
        }

        public override bool Draw(bool checkDrawTimer)
        {
            if (!base.Draw(checkDrawTimer)) return false;
            ModuleAvoidanceScanner parent = (ModuleAvoidanceScanner)base.ParentModule;
            double distance = 100 * parent.Distance;
            double direction = 100 * parent.Direction;
            DistanceText.Content = distance.ToString("000") + "%";
            DirectionText.Content = direction.ToString("000") + "%";
            return true;
        }

        private void TheCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }

        private void SaveDebugImagesCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            ModuleAvoidanceScanner parent = (ModuleAvoidanceScanner)base.ParentModule;
            if (parent == null) return;
            parent.SaveDebugImages = true;
        }

        private void SaveDebugImagesCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            ModuleAvoidanceScanner parent = (ModuleAvoidanceScanner)base.ParentModule;
            if (parent == null) return;
            parent.SaveDebugImages = false;
        }
    }
}
