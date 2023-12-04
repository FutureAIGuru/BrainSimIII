//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
//using System.Windows.Forms;
using System.Windows.Media;

namespace BrainSimulator.Modules
{
    public partial class ModuleUKSInteractDlg : ModuleBaseDlg
    {
        // Constructor of the ModuleUKSInteract dialog
        public ModuleUKSInteractDlg()
        {
            InitializeComponent();
        }

        // Draw gets called to draw the dialog when it needs refreshing
        public override bool Draw(bool checkDrawTimer)
        {
            if (!base.Draw(checkDrawTimer)) return false;

            return true;
        }

        // BtnAddThing_Click is called the AddThing button is clicked
        private void BtnAddThing_Click(object sender, RoutedEventArgs e)
        {
            string newThing = GetThingName();
            string parent = GetParentName();

            AddThingChecks();

            ModuleUKSInteract uksInteract = (ModuleUKSInteract)ParentModule;
            uksInteract.AddChildButton(newThing, parent);

            AddThingChecks();
            SetError("Thing created in UKS.");
            if (parent.ToLower() == "relationship")
            {
                FillRelationshipsComboBox();
            }
        }

        // BtnAddRelationship_Click is called the AddRelationship button is clicked
        private void BtnAddRelationship_Click(object sender, RoutedEventArgs e)
        {
            string newThing = GetThingName();
            string targetThing = GetReferenceName();
            string relationType = relationshipComboBox.SelectedItem.ToString();

            AddRelationshipChecks();

            ModuleUKSInteract uksInteract = (ModuleUKSInteract)ParentModule;
            uksInteract.AddReference(newThing, targetThing, relationType);

            AddRelationshipChecks();
        }

        // Check for thing existence and set background color of the textbox and the error message accordingly.
        private bool CheckThingExistence()
        {
            ModuleUKSInteract uksInteract = (ModuleUKSInteract)ParentModule;

            if (uksInteract.SearchLabelUKS(GetThingName()) == null)
            {
                thingText.ClearValue(Control.BackgroundProperty);
                SetError("");
                return false;
            }

            thingText.Background = new SolidColorBrush(Colors.Pink);
            SetError("Thing exists in UKS.");
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

        // Check for target existence and set background color of the textbox and the error message accordingly.
        private bool CheckReferenceExistence()
        {
            ModuleUKSInteract uksInteract = (ModuleUKSInteract)ParentModule;

            if (uksInteract.SearchLabelUKS(GetReferenceName()) == null)
            {
                referenceText.Background = new SolidColorBrush(Colors.Pink);
                SetError("Target does not exist.");
                return false;
            }
            referenceText.ClearValue(Control.BackgroundProperty);
            SetError("");
            return true;
        }

        // GetThingName returns either the content of the parentName textbox, or the default "" value.
        private string GetThingName()
        {
            if (thingText.Text.Length == 0)
                return "";
            return thingText.Text;
        }

        // GetParentName returns either the content of the parentName textbox, or the default "unknownObject" value.
        private string GetParentName()
        {
            if (parentText.Text.Length == 0)
                return "unknownObject";
            return parentText.Text;
        }

        // GetReferenceName returns either the content of the referenceName textbox, or the default "" value.
        private string GetReferenceName()
        {
            if (referenceText.Text.Length == 0)
                return "";
            return referenceText.Text;
        }

        // TheGrid_SizeChanged is called when the dialog is sized
        private void TheGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
            FillRelationshipsComboBox();
        }

        // FillRelationshipsComboBox can be called to refresh the contents of the relationshiptype combobox
        private void FillRelationshipsComboBox()
        {
            relationshipComboBox.Items.Clear();
            ModuleUKSInteract uksInteract = (ModuleUKSInteract)ParentModule;
            List<string> relTypes = uksInteract.RelationshipTypes();
            foreach (string item in relTypes)
            {
                relationshipComboBox.Items.Add(item);
            }
        }

        // thingText_TextChanged is called when the thing textbox changes
        private void thingText_TextChanged(object sender, TextChangedEventArgs e)
        {
            CheckThingExistence();
        }

        // parentText_TextChanged is called when the parent textbox changes
        private void parentText_TextChanged(object sender, TextChangedEventArgs e)
        {
            CheckThingExistence();
            parentText.ClearValue(Control.BackgroundProperty);
        }

        // referenceText_TextChanged is called when the reference textbox changes
        private void referenceText_TextChanged(object sender, TextChangedEventArgs e)
        {
            CheckReferenceExistence();
        }

        // Check for parent existence and set background color of the textbox and the error message accordingly.
        private bool CheckAddThingFieldsFilled()
        {
            SetError("");
            ModuleUKSInteract uksInteract = (ModuleUKSInteract)ParentModule;
            if (GetThingName().Length == 0 || GetParentName().Length == 0)
            {
                SetError("Fill thing name and parent name (defaults unknownObject).");
                return false;
            }
            parentText.ClearValue(Control.BackgroundProperty);
            return true;
        }

        // Check for parent existence and set background color of the textbox and the error message accordingly.
        private bool CheckAddRelationshipFieldsFilled()
        {
            SetError("");
            ModuleUKSInteract uksInteract = (ModuleUKSInteract)ParentModule;

            parentText.ClearValue(Control.BackgroundProperty);
            return true;
        }


        // SetError turns the error text yellow and sets the message, or clears the color and the text.
        private void SetError(string message)
        {
            if (string.IsNullOrEmpty(message))  
                errorText.Background = new SolidColorBrush(Colors.Gray);
            else 
                errorText.Background = new SolidColorBrush(Colors.Yellow);

            errorText.Content = message;
        }

        // Checks the relevant fields for AddThing, 
        private bool AddThingChecks()
        {
            if (CheckAddThingFieldsFilled()) return false;
            if (CheckThingExistence())       return false;
            if (!CheckParentExistence())     return false;
            return true;
        }

        // Checks the relevant fields for AddRelationship, 
        private void AddRelationshipChecks()
        {
            if (!AddThingChecks())                  return;
            if (CheckAddRelationshipFieldsFilled()) return;
        }

        private void relationshipComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // CheckRelationshipsType(relationshipComboBox.Text);
        }

    }
}
