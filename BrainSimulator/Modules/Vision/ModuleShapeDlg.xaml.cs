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
            tbFound.Text = "";
            foreach (Thing shape in parent.theUKS.Labeled("currentShape").Children)
            {
                Thing shapeType = shape.AttributeOfType("hasShape");
                if (shapeType == null) { continue; }
                Thing size = shape.AttributeOfType("hasSize");
                Relationship confidence = parent.theUKS.GetRelationship(shape.Label,"hasShape", shapeType.Label);
                tbFound.Text += $"{shape.Label}   {shapeType.Label}  conf:{confidence.Weight.ToString("0.00")}  {shape.AttributeOfType("hasSize")}\n" ;
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
