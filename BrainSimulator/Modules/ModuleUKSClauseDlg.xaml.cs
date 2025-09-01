//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UKS;

namespace BrainSimulator.Modules
{
    public partial class ModuleUKSClauseDlg : ModuleBaseDlg
    {
        // Constructor of the ModuleUKSStatement dialog
        public ModuleUKSClauseDlg()
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
            string clauseType = clauseTypeText.Text.ToUpper(); ;

            if (!CheckAddRelationshipFieldsFilled()) return;

            ModuleUKSClause UKSClause = (ModuleUKSClause)ParentModule;

            Relationship r1 = rBase;
            if (rBase == null) 
                r1 = UKSClause.AddRelationship(newThing, targetThing, relationType);

            Thing theClauseType = UKSClause.GetClauseType(clauseType);

            Thing source2 = UKSClause.theUKS.GetOrAddThing(sourceText2.Text);
            Thing type2 = UKSClause.theUKS.GetOrAddThing(relationshipText2.Text);
            Thing target2 = UKSClause.theUKS.GetOrAddThing(targetText2.Text);
            UKSClause.theUKS.AddClause(r1, theClauseType, source2, type2, target2);

            SetUpRelComboBox(GetInstanceRoot(r1.source));
        }
        private Thing GetInstanceRoot(Thing t)
        {
            Thing t1 = t;
            while (t1.HasProperty("isInstance")) t1 = t1.Parents[0];
            return t1;
        }


        public static readonly DependencyProperty theRelationship=
DependencyProperty.Register("Relationship", typeof(Relationship), typeof(ComboBoxItem));


        // thingText_TextChanged is called when the thing textbox changes
        private void Text_TextChanged(object sender, TextChangedEventArgs e)
        {
            Thing sourceThing = CheckThingExistence(sender);
            if (sender is TextBox source && source.Name == "sourceText")
            {
                SetUpRelComboBox(sourceThing);
            }
        }

        private void SetUpRelComboBox(Thing sourceThing)
        {
            SourceDisambuation.Items.Clear();
            SourceDisambuation.Items.Add("<new>");
            rBase = null;
            if (sourceThing != null)
            {
                foreach (Thing t in sourceThing.Descendents)
                {
                    if (t != sourceThing && !t.HasProperty("isInstance")) continue;
                    foreach (Relationship r in t.Relationships)
                    {
                        if (r.reltype.Label == "has-child") continue;
                        if (r.reltype.Label == "hasProperty") continue;
                        var cbi = new ComboBoxItem();
                        cbi.Content = r.target.Label;
                        cbi.ToolTip = r.ToString();
                        cbi.SetValue(theRelationship, r);
                        SourceDisambuation.Items.Add(cbi);
                    }
                }
                SourceDisambuation.SelectedIndex = 0;
                if (SourceDisambuation.Items.Count > 1)
                    SourceDisambuation.Visibility = Visibility.Visible;
            }
            else
            {
                SourceDisambuation.Visibility = Visibility.Hidden;
            }
        }

        Relationship rBase = null;
        private void SourceDisambuation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cb)
            {
                if (cb.SelectedIndex < 1)
                {
                    relationshipText.Text = "";
                    targetText.Text = "";
                    rBase = null;
                    errorText.Content = "";
                    errorText.Foreground = Brushes.Red;
                }
                else
                {
                    ComboBoxItem cbi = (ComboBoxItem)cb.SelectedItem;
                    Relationship r = (Relationship)cbi.GetValue(theRelationship);
                    relationshipText.Text = r.relType.Label;
                    targetText.Text = r.target.Label;
                    rBase = r;
                    errorText.Content = r.ToString();
                    errorText.Foreground = Brushes.Yellow;
                }
            }
        }

        //copied from UKSStatementDlg.cs
        private Thing CheckThingExistence(object sender)
        {
            if (sender is TextBox tb)
            {
                string text = tb.Text.Trim();

                if (text == "" && !tb.Name.Contains("arget"))
                {
                    tb.Background = new SolidColorBrush(Colors.Pink);
                    SetError("Source and type cannot be empty");
                    return null;
                }
                List<Thing> tl = ModuleUKSStatement.ThingListFromString(text);
                if (tl == null || tl.Count == 0)
                {
                    tb.Background = new SolidColorBrush(Colors.LemonChiffon);
                    SetError("");
                    return null;
                }
                tb.Background = new SolidColorBrush(Colors.White);
                SetError("");
                return tl[0];
            }
            return null;
        }



        // Check for parent existence and set background color of the textbox and the error message accordingly.
        private bool CheckAddRelationshipFieldsFilled()
        {
            SetError("");
            ModuleUKSClause UKSEvent = (ModuleUKSClause)ParentModule;

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
            if (clauseTypeText.Text == "")
            {
                SetError("Clause type not provided");
                return false;
            }
            if (sourceText2.Text == "")
            {
                SetError("Clause source not provided");
                return false;
            }
            if (relationshipText2.Text == "")
            {
                SetError("Clause type not provided");
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
    }
}
