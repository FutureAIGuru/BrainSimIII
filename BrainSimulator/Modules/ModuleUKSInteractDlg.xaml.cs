//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2024 FutureAI, Inc., all rights reserved
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

        public ModuleUKSInteractDlg()
        {
            InitializeComponent();
            SourceText.ToolTip = "Thing should not exist when in Add Thing mode, \nbut should exist when in Add Relationship mode.";
            TargetText.ToolTip = "Names the Target Thing in both modes. \nCan be left empty in Add Thing mode, \nbut then defaults to unknownObject.";
            TargetText.Text = DefaultSourceText;
            relationshipComboBox.ToolTip = "This relationship adds a new Source Thing.";
        }

        public override bool Draw(bool checkDrawTimer)
        {
            if (!base.Draw(checkDrawTimer)) return false;

            return true;
        }

        private void BtnAddThis_Click(object sender, RoutedEventArgs e)
        {
            string source = GetSourceName();
            string target = GetTargetName();

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


        private void TheGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
            FillRelationshipsComboBox();
        }

        private void FillRelationshipsComboBox()
        {
            relationshipComboBox.Items.Clear();
            ModuleUKSInteract uksInteract = (ModuleUKSInteract)ParentModule;
            List<string> relTypes = uksInteract.RelationshipTypes();
            foreach (string item in relTypes)
            {
                relationshipComboBox.Items.Add(item);
            }
            relationshipComboBox.SelectedValue = (object)"is-a";
        }


        private void EnableButtonIfDoable()
        {
            ModuleUKSInteract uksInteract = (ModuleUKSInteract)ParentModule;
            bool sourceOK = CheckSourceOK();
            bool targetOK = CheckTargetOK();
            if (uksInteract == null || !sourceOK || !targetOK)
            {
                updateMessage.Content = "Operation cannot proceed, check inputs.";
                BtnAddThis.IsEnabled = false;
            }
            else
            {
                updateMessage.Content = "";
                BtnAddThis.IsEnabled = true;
            }
        }

        private void relationshipComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            updateMessage.Content = "";
            if (relationshipComboBox.SelectedIndex == -1 || relationshipComboBox.SelectedItem.ToString().ToLower() == "is-a")
            {
                AddThingMode = true;
                BtnAddThis.Content = "Add Thing";
                relationshipComboBox.ToolTip = "This relationship adds a new Source Thing.";
            }
            else
            {
                AddThingMode = false;
                BtnAddThis.Content = "Add Relationship";
                relationshipComboBox.ToolTip = "This relationship adds a new Relationship between a Source and a Target Thing.";
            }
            EnableButtonIfDoable();
        }

        private void SourceText_TextChanged(object sender, TextChangedEventArgs e)
        {
            updateMessage.Content = "";
            EnableButtonIfDoable();
        }

        private void TargetText_TextChanged(object sender, TextChangedEventArgs e)
        {
            updateMessage.Content = "";
            EnableButtonIfDoable();
        }

        private bool CheckSourceOK()
        {
            ModuleUKSInteract uksInteract = (ModuleUKSInteract)ParentModule;
            if (uksInteract == null) return false;
            string sourceName = GetSourceName();
            bool noSource = (sourceName.Length == 0);
            bool shouldNotExist = AddThingMode;
            bool doesExist = (uksInteract.SearchLabelUKS(GetSourceName()) != null);

            if (noSource || (shouldNotExist && doesExist))
            {
                SourceText.Background = new SolidColorBrush(Colors.Pink);
                return false;
            }
            SourceText.ClearValue(Control.BackgroundProperty);
            return true;
        }

        private bool CheckTargetOK()
        {
            ModuleUKSInteract uksInteract = (ModuleUKSInteract)ParentModule;
            if (uksInteract == null) return true;
            bool noTarget = (GetTargetName() == "");
            bool doesNotExist = (uksInteract.SearchLabelUKS(GetTargetName()) == null);

            if (noTarget || doesNotExist)
            {
                TargetText.Background = new SolidColorBrush(Colors.Pink);
                return false;
            }
            TargetText.ClearValue(Control.BackgroundProperty);
            return true;
        }

        private string GetSourceName()
        {
            if (SourceText.Text.Length == 0)
            {
                return "";
            }
            return SourceText.Text;
        }

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
