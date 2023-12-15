//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using static BrainSimulator.Modules.ModuleOnlineInfo;

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

        // BtnQuery_Click is called the Query button is clicked
        private void BtnTransitive_Click(object sender, RoutedEventArgs e)
        {

            string source = sourceText.Text;
            string filter = filterText.Text;

            if (!AddRelationshipChecks()) return;

            ModuleUKSQuery UKSQuery = (ModuleUKSQuery)ParentModule;
            var queryResult = UKSQuery.QueryUKS(source);
            string resultString = "";
            if (queryResult.Count == 0) { resultString = "No Results"; }
            foreach (var r in queryResult)
                resultString += r.ToString() + "\n";

            resultText.Text = resultString;
        }
        // BtnQuery_Click is called the Query button is clicked
        private void BtnAncestors_Click(object sender, RoutedEventArgs e)
        {

            string source = sourceText.Text;

            if (!AddRelationshipChecks()) return;

            ModuleUKSQuery UKSQuery = (ModuleUKSQuery)ParentModule;
            var queryResult = UKSQuery.QueryAncestors(source);
            string resultString = "";
            if (queryResult.Count == 0) { resultString = "No Results"; }
            foreach (Thing t in queryResult)
                resultString += t.ToString() + "\n";

            resultText.Text = resultString;
        }

        private void BtnRelationships_Click(object sender, RoutedEventArgs e)
        {
            string source = sourceText.Text;
            string type = typeText.Text;
            string target = targetText.Text;
            string filter = filterText.Text;

            ModuleUKSQuery UKSQuery = (ModuleUKSQuery)ParentModule;
            List<ModuleUKS.ThingWithQueryParams> queryResult = null;
            Relationship.Part p = 0;
            if (source != "")
            { queryResult = UKSQuery.QueryUKS(source); p = Relationship.Part.source; }
            else if (target != "")
            { queryResult = UKSQuery.QueryUKS(target); p = Relationship.Part.target; }
            else if (type != "")
            { queryResult = UKSQuery.QueryUKS(type); p = Relationship.Part.type; }

            List<Relationship> result = UKSQuery.QueryRelationships(queryResult, p);

            string resultString = "";
            if (result == null || result.Count == 0)
                resultString = "No Results";
            else
                foreach (var r in result)
                    resultString += r.ToString() + "\n";
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

        private void BtnSequence_Click(object sender, RoutedEventArgs e)
        {
            string filter = filterText.Text;
            ModuleUKSQuery UKSQuery = (ModuleUKSQuery)ParentModule;
            var result = UKSQuery.QuerySequence(filter);
            string resultString = "";
            if (result == null || result.Count == 0)
                resultString = "No Results";
            else
                foreach (var r in result)
                    resultString += r.ToString() + "\n";
            resultText.Text = resultString;
        }

        private void BtnChildren_Click(object sender, RoutedEventArgs e)
        {
            string source = sourceText.Text;
            ModuleUKSQuery UKSQuery = (ModuleUKSQuery)ParentModule;
            var result = UKSQuery.QueryChildren(source);
            string resultString = "";
            if (result == null || result.Count == 0)
                resultString = "No Results";
            else
                foreach (var r in result)
                    resultString += r.ToString() + "\n";
            resultText.Text = resultString;
        }
    }
}
