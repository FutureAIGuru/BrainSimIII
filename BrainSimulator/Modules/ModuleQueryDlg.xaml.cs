//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace BrainSimulator.Modules
{
    public partial class ModuleQueryDlg : ModuleBaseDlg
    {
        public ModuleQueryDlg()
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
            //theCanvas.Children.Clear();
            //Point windowSize = new Point(theCanvas.ActualWidth, theCanvas.ActualHeight);
            //Point windowCenter = new Point(windowSize.X / 2, windowSize.Y / 2);
            //float scale = (float)Math.Min(windowSize.X, windowSize.Y) / 12;
            //if (scale == 0) return false;

            return true;
        }

        private void TheCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }

        private void Source_DropDownOpened(object sender, System.EventArgs e)
        {
            ModuleQuery parent = (ModuleQuery)base.ParentModule;
            parent.GetUKS();
            Thing ObjectRoot = parent.UKS.Labeled("Object");
            string selectedValue = (string)ObjectSelectionComboBox.SelectedItem;
            ObjectSelectionComboBox.Items.Clear();
            ObjectSelectionComboBox.Items.Add("No Selection");
            ObjectSelectionComboBox.SelectedIndex = 0;
            foreach (Thing t in ObjectRoot.Descendents)
            {
                int index = ObjectSelectionComboBox.Items.Add(t.Label);
                if (t.Label == selectedValue) ObjectSelectionComboBox.SelectedIndex = index;
            }
        }

        private void RelationshipType_DropDownOpened(object sender, System.EventArgs e)
        {
            ModuleQuery parent = (ModuleQuery)base.ParentModule;
            parent.GetUKS();
            Thing ObjectRoot = parent.UKS.Labeled("Relationship");
            string selectedValue = (string)RelationshipComboBox.SelectedItem;
            RelationshipComboBox.Items.Clear();
            RelationshipComboBox.Items.Add("No Selection");
            RelationshipComboBox.SelectedIndex = 0;
            foreach (Thing t in ObjectRoot.Children)
            {
                int index = RelationshipComboBox.Items.Add(t.Label);
                if (t.Label == selectedValue) RelationshipComboBox.SelectedIndex = index;
            }
        }

        private void Target_DropDownOpened(object sender, System.EventArgs e)
        {
            ModuleQuery parent = (ModuleQuery)base.ParentModule;
            parent.GetUKS();
            Thing ObjectRoot = parent.UKS.Labeled("Object");
            string selectedValue = (string)TargetComboBox.SelectedItem;
            TargetComboBox.Items.Clear();
            TargetComboBox.Items.Add("No Selection");
            TargetComboBox.SelectedIndex = 0;
            List<Thing> descendents = ObjectRoot.Descendents.ToList();
            //descendents.Sort();
            foreach (Thing t in ObjectRoot.Descendents)
            {
                int index = TargetComboBox.Items.Add(t.Label);
                if (t.Label == selectedValue) TargetComboBox.SelectedIndex = index;
            }
        }

        private void AddRelation_Button_Click(object sender, RoutedEventArgs e)
        {
            ModuleQuery parent = (ModuleQuery)base.ParentModule;
            parent.GetUKS();
            string relationshipType = (string)RelationshipComboBox.SelectedItem;
            string target = (string)TargetComboBox.SelectedItem;
            int count = int.TryParse(CountTextBox.Text, out int parsedCount) ? parsedCount : -1;
            string action = (bool)NegateCheckbox.IsChecked ? "exclude" : "include";
            RelationshipListBox.Items.Add((relationshipType, count, target, action));
            RelationshipComboBox.SelectedIndex = 0;
            TargetComboBox.SelectedIndex = 0;
            CountTextBox.Text = "";
        }

        private void DeleteRelation_Button_Click(object sender, RoutedEventArgs e)
        {
            if (RelationshipListBox.SelectedIndex != -1)
            {
                RelationshipListBox.Items.RemoveAt(RelationshipListBox.SelectedIndex);
            }
        }

        private void ClearRelations_Button_Click(object sender, RoutedEventArgs e)
        {
            RelationshipListBox.Items.Clear();
        }

        private void Search_Button_Click(object sender, RoutedEventArgs e)
        {
            ModuleQuery parent = (ModuleQuery)base.ParentModule;
            parent.GetUKS();
            List<(Thing, int, Thing, string)> relationshipList = new();
            foreach ( ValueTuple<string, int, string, string> item in RelationshipListBox.Items)
            {
                Thing relationshipType = parent.UKS.Labeled(item.Item1);
                int count = item.Item2;
                Thing target = parent.UKS.Labeled(item.Item3);
                string action = item.Item4;
                relationshipList.Add((relationshipType, count, target, action));
            }

            Thing currentVerbalResponse = parent.UKS.GetOrAddThing("CurrentVerbalResponse", "Attention");
            currentVerbalResponse.RemoveAllRelationships();
            if ( ObjectSelectionComboBox.SelectedIndex == -1 || ObjectSelectionComboBox.SelectedIndex == 0 )
            {
                List<Thing> objects = parent.QueryObjectsWithAllRelations(relationshipList);
                
                if ( objects.Count == 0 )
                {
                    currentVerbalResponse.AddRelationship(parent.UKS.Labeled("wNothing"));
                    return;
                }

                foreach (Thing thing in objects)
                {
                    if (thing == objects.Last() && thing != objects.First())
                        currentVerbalResponse.AddRelationship(parent.UKS.Labeled("wAnd"));
                    Thing objectLabel = new();
                    objectLabel.V = thing.Label;
                    currentVerbalResponse.AddRelationship(objectLabel);
                    if (thing != objects.Last()) currentVerbalResponse.AddRelationship(parent.UKS.Labeled("puncComma"));
                }
            }
            else
            {
                Thing source = parent.UKS.Labeled((string)ObjectSelectionComboBox.SelectedItem);
                bool result = parent.doesThingHaveAllRelations(source, relationshipList);

                if (result) currentVerbalResponse.AddRelationship(parent.UKS.Labeled("wYes"));
                else currentVerbalResponse.AddRelationship(parent.UKS.Labeled("wNo"));
            }
            
        }
    }
}
