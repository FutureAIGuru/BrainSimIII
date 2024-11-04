//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System;
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
            if (parent.newStoredShape == null) tbNewName.IsEnabled = false;
            else tbNewName.IsEnabled = true;

            if (parent.theUKS.Labeled("currentShape") == null) return false;
            tbFound.Text = "";
            foreach (Thing currentShape in parent.theUKS.Labeled("currentShape").Children)
            {
                Thing shapeType = currentShape.GetAttribute("storedShape");
                if (shapeType == null) { continue; }
                Relationship confidence = parent.theUKS.GetRelationship(currentShape.Label,"hasAttribute", shapeType.Label);
                tbFound.Text += $"{currentShape.Label}   {shapeType.Label}  Conf.: {(int)(confidence.Weight * 100)}%  {currentShape.GetAttribute("size")}\n";
                tbFound.Text += $"                  {currentShape.GetAttribute("Color")?.Label} Orientation:{currentShape.GetAttribute("Rotation")?.Label[6..]}\n";
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
            parent.newStoredShape = parent.AddShapeToLibrary();
            tbNewName.IsEnabled = true;
        }

        private void tbNewName_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                ModuleShape parent = (ModuleShape)ParentModule;
                if (parent.newStoredShape == null) return;
                string newName = tbNewName.Text;
                parent.newStoredShape.Label = newName;
                tbNewName.Text = "New Name";
                parent.newStoredShape.RelationshipsFrom.FindFirst(x => x.reltype.Label == "has-child").TimeToLive = TimeSpan.MaxValue;
                Draw(false);
            }
        }

        private void tbNewName_GotFocus(object sender, RoutedEventArgs e)
        {
            ModuleShape parent = (ModuleShape)ParentModule;
            if (parent.newStoredShape == null) tbNewName.IsEnabled = false;
            else
            {
                tbNewName.IsEnabled = true;
                if (tbNewName.Text == "New Name")
                    tbNewName.Text = "";
            }
        }
    }
}
