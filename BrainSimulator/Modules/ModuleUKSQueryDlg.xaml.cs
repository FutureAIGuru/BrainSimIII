//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
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
            if (sender is Button b)
            {
                if (b.Content.ToString() == "Add")
                {
                    string newText = typeText1.Text + " " + targetText1.Text;
                    if (!queryText1.Text.Contains(newText))
                    {
                        if (!string.IsNullOrEmpty(queryText1.Text))
                            queryText1.Text += "\n";
                        queryText1.Text += newText;
                    }
                    QueryAttribs();
                }
                if (b.Content.ToString() == "Clear")
                {
                    queryText1.Text = "";
                    resultText1.Text = "";
                }
                if (b.Content.ToString() == "Query")
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
                        OutputResults(relationships, target == "", source == "");

                }
            }
        }
        private void QueryAttribs()
        {
            ModuleUKSQuery UKSQuery = (ModuleUKSQuery)ParentModule;
            Thing ancestor = UKSQuery.theUKS.Labeled(ancestorText1.Text);
            if (ancestor == null)
            {
                resultText1.Text = "<Ancestor must be specified>";
                return;
            }
            //build the query object
            Thing queryThing = new Thing();
            string[] rels = queryText1.Text.Split('\n');
            foreach (string s in rels)
            {
                string[] relParams = s.Split(' ');
                if (relParams.Length == 2)
                {
                    Thing relType = UKSQuery.theUKS.Labeled(relParams[0]);
                    Thing relTarget = UKSQuery.theUKS.Labeled(relParams[1]);
                    if (relType == null)
                    {
                        resultText1.Text = $"<{relParams[0]} not found>";
                        return;
                    }
                    if (relTarget == null)
                    {
                        resultText1.Text = $"<{relParams[1]} not found>";
                        return;
                    }

                    queryThing.AddRelationship(relTarget, relType);
                }
            }
            float confidence = 0;
            Thing result = UKSQuery.theUKS.SearchForClosestMatch(queryThing, ancestor, ref confidence);
            if (result == null)
            {
                resultText1.Text = "<No Results>";
                return;
            }
            resultText1.Text = result.Label + "   " + confidence;
            while (result != null)
            {
                result = UKSQuery.theUKS.GetNextClosestMatch(ref confidence);
                if (result != null)
                    resultText1.Text += "\n" + result.Label + "   " + confidence;

            }
        }

        private void OutputResults<T>(IList<T> r, bool noSource = false, bool noTarget = false)
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
                            resultString += r2.relType?.ToString() + " " + r2.target.ToString() + "\n";
                        else if (noTarget && r2.Clauses.Count == 0 && fullCB.IsChecked == false)
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
