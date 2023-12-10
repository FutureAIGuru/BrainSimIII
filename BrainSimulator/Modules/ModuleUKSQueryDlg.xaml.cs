//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BrainSimulator.Modules
{
    public partial class ModuleUKSQueryDlg : ModuleBaseDlg
    {
        // Constructor of the ModuleUKSStatement dialog
        public ModuleUKSQueryDlg()
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
            string source = sourceText.Text;
            string filter = filterText.Text;

            if (!AddRelationshipChecks()) return;

            ModuleUKSQuery UKSQuery = (ModuleUKSQuery)ParentModule;

            var queryResult = UKSQuery.QueryUKS (source, filter);
            string resultString = "";
            if (queryResult.Count == 0) { resultString = "No Results"; }
            foreach (Relationship r in queryResult)
            {
                if (r.weight < 0.95) resultString += "<" + r.weight.ToString("f2") + ">";
                resultString += r.ToString() + "\n";
            }
            resultText.Text = resultString;
        }

        // Check for thing existence and set background color of the textbox and the error message accordingly.
        private bool CheckThingExistence(object sender)
        {
            if (sender is TextBox tb)
            {
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


        // referenceText_TextChanged is called when the reference textbox changes
        private void targetText_TextChanged(object sender, TextChangedEventArgs e)
        {
            CheckThingExistence(sender);
        }


        // Check for parent existence and set background color of the textbox and the error message accordingly.
        private bool CheckAddRelationshipFieldsFilled()
        {
            SetError("");
            ModuleUKSQuery UKSQuery = (ModuleUKSQuery)ParentModule;

            if (sourceText.Text == "")
            {
                SetError("Source not provided");
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

    }
}
