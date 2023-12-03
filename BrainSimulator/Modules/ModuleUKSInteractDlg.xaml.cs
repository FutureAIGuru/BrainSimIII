//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BrainSimulator.Modules
{
    public partial class ModuleUKSInteractDlg : ModuleBaseDlg
    {
        public ModuleUKSInteractDlg()
        {
            InitializeComponent();
        }

        public override bool Draw(bool checkDrawTimer)
        {
            if (!base.Draw(checkDrawTimer)) return false;

            return true;
        }

        private void TheGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }

        /*
        private void UpdateUKS()
        {
            ModuleUKSInteract uksInteract = (ModuleUKSInteract)ParentModule;
            SetError("");

            if (addThingRadio.IsChecked == true)
            {
                string newThing = thingText.Text;
                string parent = parentText.Text;

                if (newThing == "" || parent == "")
                {
                    SetError("Fill all entry fields.");
                    return;
                }

                if (CheckThingExistance() == false)
                {
                    SetError("Parent does not exist.");
                    return;
                }

                if (uksInteract.AddChildButton(newThing, parent) == false)
                {
                    SetError("Thing already exists.");
                    return;
                }
            }
            else if (addReferenceRadio.IsChecked == true)
            {
                string thing = thingText.Text;
                string parent = parentComboBox.Text;
                string reference = referenceText.Text;
                string relType = relationshipTypeTextBox.Text;
                string referenceParent = referenceParentComboBox.Text;
                string[] modifiersText = ModifiersTextBox.Text.Split("\r\n");

                if (thing == "" || parent == "" || reference == "" || referenceParent == "")
                {
                    SetError("Fill all entry fields.");
                    return;
                }

                uksInteract.AddReference(thing, parent, reference, relType,referenceParent,modifiersText);

                UpdateParentBox();
                UpdateReferenceParentBox();
            }
            else //delete thing
            {
                string thing = thingText.Text;
                string parent = parentComboBox.Text;

                if (thing == "" || parent == "")
                {
                    SetError("Fill all entry fields.");
                    return;
                }

                if (uksInteract.DeleteThing(thing, parent) == false)
                {
                    SetError("Cannot delete a thing with children.");
                    return;
                }

                UpdateParentBox();
            }
        }
        */

        private void UpdateReferenceParentBox()
        {
            //referenceParentComboBox.Items.Clear();

            ModuleUKSInteract uksInteract = (ModuleUKSInteract)ParentModule;

            //repopulate selections

            List<Thing> referenceParents = uksInteract.ParentsOfLabel(referenceText.Text);

            if (referenceParents == null) return;
            foreach (var parent in referenceParents)
            {
              //  referenceParentComboBox.Items.Add(parent.Label);
            }

        //    if (referenceParentComboBox.Items.Count == 1)
          //      referenceParentComboBox.SelectedIndex = 0;
        }

        private void updateReferenceBox()
        {
            ModuleUKSInteract uksInteract = (ModuleUKSInteract)ParentModule;

            if (uksInteract.SearchLabelUKS(referenceText.Text) == null || uksInteract.isRoot(referenceText.Text))
                referenceText.Background = new SolidColorBrush(Colors.Pink);
            else
                referenceText.ClearValue(Control.BackgroundProperty);
        }

        private void parentText_TextChanged(object sender, TextChangedEventArgs e)
        {
            CheckThingExistence();
            parentText.ClearValue(Control.BackgroundProperty);
        }

        private void referenceText_TextChanged(object sender, TextChangedEventArgs e)
        {
            ModuleUKSInteract uksInteract = (ModuleUKSInteract)ParentModule;

            updateReferenceBox();

            UpdateReferenceParentBox();
        }

        private void BtnAddThing_Click(object sender, RoutedEventArgs e)
        {
            string newThing = thingText.Text;
            string parent = GetParentName();

            if (newThing == "" || GetParentName() == "")
            {
                SetError("Fill thing name and parent name (defaults unknownObject).");
                return;
            }

            if (CheckThingExistence())
            {
                SetError("Thing exists already.");
                return;
            }

            if (!CheckParentExistence())
            {
                SetError("Parent does not exist.");
                return;
            }

            ModuleUKSInteract uksInteract = (ModuleUKSInteract)ParentModule;
            if (uksInteract.AddChildButton(newThing, parent) == false)
            SetError("");
        }

        private void BtnAddRelationship_Click(object sender, RoutedEventArgs e)
        {

        }
        
        private void thingText_TextChanged(object sender, TextChangedEventArgs e)
        {
            CheckThingExistence();
        }

        private void parentText_TextChanged_1(object sender, TextChangedEventArgs e)
        {
            CheckParentExistence();
        }

        // Check for thing existence and set background color of the textbox and the error message accordingly.
        private bool CheckThingExistence()
        {
            ModuleUKSInteract uksInteract = (ModuleUKSInteract)ParentModule;

            if (uksInteract.SearchLabelUKS(thingText.Text) == null)
            {
                thingText.ClearValue(Control.BackgroundProperty);
                SetError("");
                return false;
            }

            thingText.Background = new SolidColorBrush(Colors.Pink);
            SetError("Thing exists already.");
            return true;
        }

        // Check for parent existence and set background color of the textbox and the error message accordingly.
        private bool CheckParentExistence()
        {
            ModuleUKSInteract uksInteract = (ModuleUKSInteract)ParentModule;

            if (uksInteract.SearchLabelUKS(GetParentName()) == null)
            {
                parentText.Background = new SolidColorBrush(Colors.Pink);
                SetError("Parent does not exist.");
                return false;
            }
            parentText.ClearValue(Control.BackgroundProperty);
            SetError("");
            return true;
        }

        // GetParentName returns either the content of the parentName textbox, or the default "unknownObject" value.
        private string GetParentName()
        {
            if (parentText.Text.Length == 0)
            {
                return "unknownObject";
            }
            return parentText.Text;
        }

        // SetError turns the error text yellow and sets the message, or clears the color and the text.
        private void SetError(string message)
        {
            if (string.IsNullOrEmpty(message)) 
            {
                errorText.Background = new SolidColorBrush(Colors.Gray);
            }
            else
            {
                errorText.Background = new SolidColorBrush(Colors.Yellow);
            }

            errorText.Content = message;
        }
    }
}
