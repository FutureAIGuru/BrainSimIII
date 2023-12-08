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
        private bool AddThingMode = true; // Adding a Thing if true, or a Relationship if false
        private const string DefaultSourceText = "unknownObject";

        // Constructor of the ModuleUKSInteract dialog
        public ModuleUKSInteractDlg()
        {
            InitializeComponent();
            SourceText.ToolTip = "Thing should not exist when in Add Thing mode, \nbut should exist when in Add Relationship mode.";
            TargetText.ToolTip = "Names the Target Thing in both modes. \nCan be left empty in Add Thing mode, \nbut then defaults to unknownObject.";
            TargetText.Text = DefaultSourceText;
            relationshipComboBox.ToolTip = "This relationship adds a new Source Thing.";
        }

        // Draw gets called to draw the dialog when it needs refreshing
        public override bool Draw(bool checkDrawTimer)
        {
            if (!base.Draw(checkDrawTimer)) return false;

            return true;
        }

        // BtnAddThis_Click is called the AddThing button is clicked
        private void BtnAddThis_Click(object sender, RoutedEventArgs e)
        {
            string source = GetSourceName();
            string target = GetTargetName();

            if (CheckSourceExistence() || CheckTargetExistence())
            {
                updateMessage.Content = "Operation cannot proceed, check inputs.";
                return;
            }

            if (AddThingMode)
            {
                AddThing(source, target);
            }
            else
            {
                AddRelationship(source, target);
            }
        }

        private void AddThing(string source, string target)
        {
            ModuleUKSInteract uksInteract = (ModuleUKSInteract)ParentModule;
            uksInteract.AddChildButton(source, target);
            if (AddThingMode && GetTargetName().ToLower() == "relationship")
            {
                FillRelationshipsComboBox();
            }

            updateMessage.Content = "Thing " + source + " was added to " + target;
        }

        private void AddRelationship(string source, string target)
        {
            string relationType = relationshipComboBox.SelectedItem.ToString();

            ModuleUKSInteract uksInteract = (ModuleUKSInteract)ParentModule;
            uksInteract.AddRelationship(source, target, relationType);

            updateMessage.Content = "Relationship " + source + " " + relationType + " " + target + " was added.";
        }


        // TheGrid_SizeChanged is called when the dialog is sized
        private void TheGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
            FillRelationshipsComboBox();
        }

        // Check for parent existence and set background color of the textbox and the error message accordingly.
        private bool CheckAddThingFieldsFilled()
        {
            SetError("");
            ModuleUKSInteract uksInteract = (ModuleUKSInteract)ParentModule;
            if (GetSourceName().Length == 0 /* || GetParentName().Length == 0 */)
            {
                SetError("Fill thing name and parent name (defaults unknownObject).");
                return false;
            }
            // parentText.ClearValue(Control.BackgroundProperty);
            return true;
        }

        // Check for parent existence and set background color of the textbox and the error message accordingly.
        private bool CheckAddRelationshipFieldsFilled()
        {
            SetError("");
            ModuleUKSInteract uksInteract = (ModuleUKSInteract)ParentModule;

            // parentText.ClearValue(Control.BackgroundProperty);
            return true;
        }


        // SetError turns the error text yellow and sets the message, or clears the color and the text.
        private void SetError(string message)
        {
            // if (string.IsNullOrEmpty(message))  
            //     errorText.Background = new SolidColorBrush(Colors.Gray);
            // else 
            //     errorText.Background = new SolidColorBrush(Colors.Yellow);
            // errorText.Content = message;
        }

        // Checks the relevant fields for AddThing, 
        private bool AddThingChecks()
        {
            if (CheckAddThingFieldsFilled()) return false;
            if (CheckSourceExistence())      return false;
            return true;
        }

        // Checks the relevant fields for AddRelationship, 
        private void AddRelationshipChecks()
        {
            if (!AddThingChecks())                  return;
            if (CheckAddRelationshipFieldsFilled()) return;
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

        // relationshipComboBox_SelectionChanged set the AddThingMode correctly, depending on the relationshipType
        private void relationshipComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            updateMessage.Content = "";
            if (relationshipComboBox.SelectedIndex == -1 || relationshipComboBox.SelectedItem.ToString().ToLower() == "is-a")
            {
                AddThingMode = true;
                BtnAddThing.Content = "Add Thing";
                relationshipComboBox.ToolTip = "This relationship adds a new Source Thing.";
            }
            else
            {
                AddThingMode = false;
                BtnAddThing.Content = "Add Relationship";
                relationshipComboBox.ToolTip = "This relationship adds a new Relationship between a Source and a Target Thing.";
            }
            CheckSourceExistence();
            CheckTargetExistence();
        }

        // thingText_TextChanged is called when the thing textbox changes
        private void SourceText_TextChanged(object sender, TextChangedEventArgs e)
        {
            updateMessage.Content = "";
            CheckSourceExistence();
        }

        // TargetText_TextChanged is called when the Target textbox changes
        private void TargetText_TextChanged(object sender, TextChangedEventArgs e)
        {
            updateMessage.Content = "";
            CheckTargetExistence();
        }

        // Check for thing existence and set background color of the textbox and the error message accordingly.
        private bool CheckSourceExistence()
        {
            ModuleUKSInteract uksInteract = (ModuleUKSInteract)ParentModule;
            if (uksInteract == null) return false; 
            bool noSource = (GetSourceName() == "");
            bool shouldExist = !AddThingMode ^ (uksInteract.SearchLabelUKS(GetSourceName()) != null);

            if (noSource || shouldExist)
            {
                SourceText.Background = new SolidColorBrush(Colors.Pink);
            }
            else
            {
                SourceText.ClearValue(Control.BackgroundProperty);
            }
            return shouldExist;
        }

        // Check for target existence and set background color of the textbox and the error message accordingly.
        private bool CheckTargetExistence()
        {
            ModuleUKSInteract uksInteract = (ModuleUKSInteract)ParentModule;
            if (uksInteract == null) return false;
            bool noTarget = (GetTargetName() == "");
            bool shouldExist = (uksInteract.SearchLabelUKS(GetTargetName()) == null);

            if (noTarget || shouldExist)
            {
                TargetText.Background = new SolidColorBrush(Colors.Pink);
            }
            else
            {
                TargetText.ClearValue(Control.BackgroundProperty);
            }
            return shouldExist;
        }

        // GetSourceName returns either the content of the parentName textbox, or the default "" value.
        private string GetSourceName()
        {
            if (SourceText.Text.Length == 0)
            {
                return "";
            }
            return SourceText.Text;
        }

        // GetTargetName returns either the content of the TargetName textbox, or the default "" value.
        private string GetTargetName()
        {
            if (TargetText.Text.Length == 0)
            {
                TargetText.Text = DefaultSourceText;
            }
            return TargetText.Text;
        }
    }
}
