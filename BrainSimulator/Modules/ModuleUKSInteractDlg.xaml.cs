//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using System;
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

        // BtnAddRelationship_Click is called the AddRelationship button is clicked
        private void BtnAddRelationship_Click(object sender, RoutedEventArgs e)
        {
            string newThing = sourceText.Text;
            string targetThing = targetText.Text;
            string relationType = relationshipText.Text;

            if (!AddRelationshipChecks()) return;
            TimeSpan duration = TimeSpan.MaxValue;
            string durationText = ((ComboBoxItem)durationCombo.SelectedItem).Content.ToString();
            switch (durationText)
            {
                case "Eternal": duration = TimeSpan.MaxValue; break;
                case "1 hr": duration = TimeSpan.FromHours(1); break;
                case "5 min": duration = TimeSpan.FromMinutes(5); break;
                case "1 min": duration = TimeSpan.FromMinutes(1); break;
                case "30 sec": duration = TimeSpan.FromSeconds(30); break;
                case "10 sec": duration = TimeSpan.FromSeconds(10); break;
            }
            float confidence = (float)confidenceSlider.Value;


            ModuleUKSInteract uksInteract = (ModuleUKSInteract)ParentModule;
            Relationship r1 = uksInteract.AddRelationship(newThing, targetThing, relationType, confidence, duration);

            if (connectorCombo.SelectedValue is ComboBoxItem c)
                if (c.Content.ToString() != "")
                {
                    string newThing2 = sourceText2.Text;
                    string targetThing2 = targetText2.Text;
                    string relationType2 = relationshipText2.Text;
                    Relationship r2 = uksInteract.AddRelationship(newThing2, targetThing2, relationType2, confidence, duration);

                    r1.AddClause(c.Content.ToString(), r2);
                }
        }

        // Check for thing existence and set background color of the textbox and the error message accordingly.
        private bool CheckThingExistence(object sender)
        {
            if (sender is TextBox tb)
            {
                ModuleUKSInteract uksInteract = (ModuleUKSInteract)ParentModule;

                if (tb.Text == "" && !tb.Name.Contains("arget"))
                {
                    tb.Background = new SolidColorBrush(Colors.Pink);
                    SetError("Source and type cannot be empty");
                    return false;
                }
                if (uksInteract.SearchLabelUKS(tb.Text) == null)
                {
                    tb.Background = new SolidColorBrush(Colors.Yellow);
                    SetError("");
                    return false;
                }
                tb.Background = new SolidColorBrush(Colors.White);
                SetError("Thing exists in UKS.");
                return true;
            }
            return false;
        }


        // TheGrid_SizeChanged is called when the dialog is sized
        private void TheGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
            //FillRelationshipsComboBox();
        }

        // thingText_TextChanged is called when the thing textbox changes
        private void thingText_TextChanged(object sender, TextChangedEventArgs e)
        {
            CheckThingExistence(sender);
        }

        private void relationshipText_TextChanged(object sender, TextChangedEventArgs e)
        {
            CheckThingExistence(sender);
        }

        // parentText_TextChanged is called when the parent textbox changes
        private void parentText_TextChanged(object sender, TextChangedEventArgs e)
        {
            CheckThingExistence(sender);
        }

        // referenceText_TextChanged is called when the reference textbox changes
        private void targetText_TextChanged(object sender, TextChangedEventArgs e)
        {
            CheckThingExistence(sender);
        }


        // Check for parent existence and set background color of the textbox and the error message accordingly.
        private bool CheckAddRelationshipFieldsFilled()
        {
            SetError("");
            ModuleUKSInteract uksInteract = (ModuleUKSInteract)ParentModule;

            if (sourceText.Text == "")
            {
                SetError("Source not provided");
                return false;
            }
            if (relationshipText.Text == "")
            {
                SetError("Type not provided");
                return false;
            }
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

        // Checks the relevant fields for AddRelationship, 
        private bool AddRelationshipChecks()
        {
            return CheckAddRelationshipFieldsFilled();
        }

        private void connectorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox c)
            {
                string selector = ((ComboBoxItem)c.SelectedItem).Content as string;
                if (selector != "")
                    this.Height = 450;
                else
                    this.Height = 270;
            }
        }
    }
}
