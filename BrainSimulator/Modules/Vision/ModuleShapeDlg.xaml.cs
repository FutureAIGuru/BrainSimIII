//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System.Windows;
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
            if (parent.theUKS.Labeled("currentShape") == null) return false;
            tbFound.Text = "";
            foreach (Thing currentShape in parent.theUKS.Labeled("currentShape").Children)
            {
                Thing shapeType = currentShape.GetAttribute("storedShape");
                if (shapeType == null) { continue; }
                Relationship confidence = parent.theUKS.GetRelationship(currentShape.Label,"hasAttribute", shapeType.Label);
                tbFound.Text += $"{currentShape.Label}   {shapeType.Label}  conf:{confidence.Weight.ToString("0.00")}  {currentShape.GetAttribute("size")}\n";
                tbFound.Text += $"                  {currentShape.GetAttribute("Color")?.Label} orientation:{currentShape.GetAttribute("Rotation")?.Label[6..]}\n";
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
