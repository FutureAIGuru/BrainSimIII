//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using System.Collections.Generic;
using System.Windows;
using UKS;

namespace BrainSimulator.Modules
{
    public partial class ModuleUKSQueryDlg : ModuleBaseDlg
    {
        // Constructor of the ModuleUKSStatement dialog
        //lists of nouns from https://www.momswhothink.com/list-of-nouns/
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
            string filter = filterText.Text;

            List<Thing> things;
            List<Relationship> relationships;
            ModuleUKSQuery UKSQuery = (ModuleUKSQuery)ParentModule;
            UKSQuery.QueryUKS(source, type, target, filter, out things, out relationships);

            if (things.Count > 0)
                OutputResults(things);
            else
                OutputResults(relationships,target=="",source=="");

        }

        private void OutputResults<T>(IList<T> r, bool noSource = false,bool noTarget = false)
        {
            string resultString = "";
            if (r == null || r.Count == 0)
                resultString = "No Results";
            else
                foreach (var r1 in r)
                {
                    if (r1 is Relationship r2)
                    {
                        if (noSource && r2.Clauses.Count == 0 && fullCB.IsChecked == false)
                            resultString += r2.relType.ToString() + " " + r2.target.ToString() + "\n";
                        else if (noTarget&& r2.Clauses.Count == 0 && fullCB.IsChecked == false)
                            resultString += r2.source.ToString() + " " + r2.relType.ToString() + "\n";
                        else
                            resultString += r2.ToString() + "\n";
                    }
                    else
                        resultString += r1.ToString() + "\n";
                }
            resultText.Text = resultString;
        }
    }
}
