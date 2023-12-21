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
            UKSQuery.QueryUKS(source, type, target, filter, out things, out relationships);

            if (things.Count > 0)
                OutputResults(things);
            else 
                OutputResults(relationships);

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
