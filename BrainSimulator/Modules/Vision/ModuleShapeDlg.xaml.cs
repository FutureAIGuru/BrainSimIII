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
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using UKS;

namespace BrainSimulator.Modules
{
    public partial class ModuleShapeDlg : ModuleBaseDlg
    {
        public ModuleShapeDlg()
        {
            InitializeComponent();
        }

        public override bool Draw(bool checkDrawTimer)
        {
            if (!base.Draw(checkDrawTimer)) return false;

            ModuleShape parent = (ModuleShape)ParentModule;
            if (parent.foundShape != null)
            {
                tbFound.Text = $"{parent.foundShape.Label}   {parent.confidence}   {parent.scale}" ;
            }

            return true;
        }

        private void TheGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ModuleShape parent = (ModuleShape)ParentModule;
            parent.AddShapeToLibrary();
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            ModuleShape parent = (ModuleShape)ParentModule;
            float confidence = -1;
            Thing t = parent.theUKS.GetNextClosestMatch(ref confidence);
            if (t != null) 
                tbFound.Text += "\n"+t.Label + "   " + confidence;
        }
    }
}
