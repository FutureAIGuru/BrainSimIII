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

namespace BrainSimulator.Modules;

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
        string clauseLabel = clauseTypeText.Text.ToUpper(); ;

        if (!CheckAddRelationshipFieldsFilled()) return;

        ModuleUKSClause UKSClause = (ModuleUKSClause)ParentModule;

        Thing source = UKSClause.theUKS.CreateThingFromMultipleAttributes(newThing,false);
        Thing target = UKSClause.theUKS.CreateThingFromMultipleAttributes(targetThing,false);
        Thing relType = UKSClause.theUKS.CreateThingFromMultipleAttributes(relationType, true);
        Thing clauseType = UKSClause.theUKS.CreateThingFromMultipleAttributes(clauseLabel, false);

        Relationship r1 = null;
        if (rBase != null)
        {
            if (GetInstanceRoot(rBase.Source) != source ||
                GetInstanceRoot(rBase.Target) != target ||
                GetInstanceRoot(rBase.RelType) != relType)
                rBase = null;
            if (rBase != null && !rBase.Source.Relationships.Contains(rBase))
                rBase = null;
            if (rBase != null)
                r1 = rBase;
        }
        if (r1 == null) //enable appending clause to existing Relationships
            r1 = UKSClause.theUKS.AddStatement (source,relType,target);

        Thing theClauseType = UKSClause.theUKS.GetOrAddThing(clauseLabel,"ClauseType");

        Thing source2 = UKSClause.theUKS.CreateThingFromMultipleAttributes(sourceText2.Text, false);
        Thing relType2 = UKSClause.theUKS.CreateThingFromMultipleAttributes(relationshipText2.Text, true);
        Thing target2 = UKSClause.theUKS.CreateThingFromMultipleAttributes(targetText2.Text, false);

        Relationship rClause = UKSClause.theUKS.AddStatement(source2, relType2, target2);

        Relationship rAdded = UKSClause.theUKS.AddClause(r1, theClauseType, rClause);

        SetUpRelComboBox(GetInstanceRoot(r1.Source),rAdded);
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

    private void SetUpRelComboBox(Thing sourceThing,Relationship rSelected = null)
    {
        SourceDisambiguation.Items.Clear();
        SourceDisambiguation.Items.Add("<new>");
        SourceDisambiguation.SelectedIndex = 0;
        rBase = null;
        if (sourceThing != null)
        {
            foreach (Thing t in sourceThing.Descendents)
            {
                if (t != sourceThing && !t.HasProperty("isInstance")) continue;
                foreach (Relationship r in t.Relationships)
                {
                    if (r.RelType.Label == "has-child") continue;
                    if (r.RelType.Label == "hasProperty") continue;
                    var cbi = new ComboBoxItem();
                    cbi.Content = r.Target.Label;
                    cbi.ToolTip = r.ToString();
                    cbi.SetValue(theRelationship, r);
                    SourceDisambiguation.Items.Add(cbi);
                    if (r == rSelected)
                        SourceDisambiguation.SelectedItem = cbi;
                }
            }
            if (SourceDisambiguation.Items.Count > 1)
                SourceDisambiguation.Visibility = Visibility.Visible;
        }
        else
        {
            SourceDisambiguation.Visibility = Visibility.Hidden;
        }
    }

    Relationship rBase = null;
    private void SourceDisambuation_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox cb)
        {
            if (cb.SelectedIndex < 1)
            {
                SetStatus("OK");
                rBase = null;
            }
            else
            {
                ComboBoxItem cbi = (ComboBoxItem)cb.SelectedItem;
                Relationship r = (Relationship)cbi.GetValue(theRelationship);
                relationshipText.Text = r.RelType.Label;
                targetText.Text = r.Target.Label;
                rBase = r;
                SetStatus(r.ToString(), Colors.Yellow);
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
                SetStatus("Source and type cannot be empty");
                return null;
            }
            List<Thing> tl = ModuleUKSStatement.ThingListFromString(text);
            if (tl == null || tl.Count == 0)
            {
                tb.Background = new SolidColorBrush(Colors.LemonChiffon);
                return null;
            }
            tb.Background = new SolidColorBrush(Colors.White);
            SetStatus("");
            return tl[0];
        }
        return null;
    }



    // Check for parent existence and set background color of the textbox and the error message accordingly.
    private bool CheckAddRelationshipFieldsFilled()
    {
        SetStatus("");
        ModuleUKSClause UKSEvent = (ModuleUKSClause)ParentModule;

        if (sourceText.Text == "")
        {
            SetStatus("Source not provided");
            return false;
        }
        if (relationshipText.Text == "")
        {
            SetStatus("Type not provided");
            return false;
        }
        if (clauseTypeText.Text == "")
        {
            SetStatus("Clause type not provided");
            return false;
        }
        if (sourceText2.Text == "")
        {
            SetStatus("Clause source not provided");
            return false;
        }
        if (relationshipText2.Text == "")
        {
            SetStatus("Clause type not provided");
            return false;
        }
        return true;
    }
}
