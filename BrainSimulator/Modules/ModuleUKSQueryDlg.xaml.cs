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
        List<Relationship> result = new();


        // Draw gets called to draw the dialog when it needs refreshing
        public override bool Draw(bool checkDrawTimer)
        {
            if (!base.Draw(checkDrawTimer)) return false;
            return true;
        }

        private void BtnRelationships_Click(object sender, RoutedEventArgs e)
        {
            string source = sourceText.Text;
            string type = typeText.Text;
            string target = targetText.Text;
            string filter= filterText.Text;

            List<Thing> things;
            List<Relationship> relationships;
            ModuleUKSQuery UKSQuery = (ModuleUKSQuery)ParentModule;
            var queryResult = UKSQuery.QueryUKS(source, type, target, filter, out things, out relationships);

            if (things.Count > 0)
                OutputResults(things);
            else if (relationships.Count > 0)
                OutputResults(relationships);
            else
               OutputResults(queryResult);
        }

        // thingText_TextChanged is called when the thing textbox changes
        private void thingText_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void relationshipText_TextChanged(object sender, TextChangedEventArgs e)
        {
        }


        // referenceText_TextChanged is called when the reference textbox changes
        private void targetText_TextChanged(object sender, TextChangedEventArgs e)
        {
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

        private void BtnSequence_Click(object sender, RoutedEventArgs e)
        {
            string source = sourceText.Text;
            ModuleUKSQuery UKSQuery = (ModuleUKSQuery)ParentModule;
            var result = UKSQuery.QuerySequence(source);
            OutputResults(result);
        }

        private void OutputResults<T>(IList<T> r)
        {
            string resultString = "";
            if (r == null || r.Count == 0)
                resultString = "No Results";
            else
                foreach (var r1 in r)
                    resultString += r1.ToString() + "\n";
            resultText.Text = resultString;
        }
    }
}
