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

    // BtnAddLink_Click is called the AddLink button is clicked
    private void BtnAddLink_Click(object sender, RoutedEventArgs e)
    {
        string newThing = sourceText.Text;
        string targetThing = targetText.Text;
        string relationType = linkText.Text;
        string clauseLabel = clauseTypeText.Text.ToUpper(); ;

        if (!CheckAddLinkFieldsFilled()) return;

        ModuleUKSClause UKSClause = (ModuleUKSClause)ParentModule;

        Thought source = UKSClause.theUKS.CreateThingFromMultipleAttributes(newThing,false);
        Thought target = UKSClause.theUKS.CreateThingFromMultipleAttributes(targetThing,false);
        Thought relType = UKSClause.theUKS.CreateThingFromMultipleAttributes(relationType, true);
        Thought clauseType = UKSClause.theUKS.CreateThingFromMultipleAttributes(clauseLabel, false);

        Thought r1 = null;
        if (rBase is not null)
        {
            if (GetInstanceRoot(rBase.From) != source ||
                GetInstanceRoot(rBase.To) != target ||
                GetInstanceRoot(rBase.LinkType) != relType)
                rBase = null;
            if (rBase is not null && !rBase.From.LinksTo.Contains(rBase))
                rBase = null;
            if (rBase is not null)
                r1 = rBase;
        }
        if (r1 is null) //enable appending clause to existing Links
            r1 = UKSClause.theUKS.AddStatement (source,relType,target);

        Thought theClauseType = UKSClause.theUKS.GetOrAddThing(clauseLabel,"ClauseType");

        Thought source2 = UKSClause.theUKS.CreateThingFromMultipleAttributes(sourceText2.Text, false);
        Thought relType2 = UKSClause.theUKS.CreateThingFromMultipleAttributes(linkText2.Text, true);
        Thought target2 = UKSClause.theUKS.CreateThingFromMultipleAttributes(targetText2.Text, false);

        Thought rClause = UKSClause.theUKS.AddStatement(source2, relType2, target2);

        Thought rAdded = UKSClause.theUKS.AddStatement(r1, theClauseType, rClause);

        SetUpRelComboBox(GetInstanceRoot(r1.From),rAdded);
    }

    private Thought GetInstanceRoot(Thought t)
    {
        Thought t1 = t;
        while (t1.HasProperty("isInstance")) t1 = t1.Parents[0];
        return t1;
    }


    public static readonly DependencyProperty theLink=
DependencyProperty.Register("Thought", typeof(Thought), typeof(ComboBoxItem));


    // thingText_TextChanged is called when the thought textbox changes
    private void Text_TextChanged(object sender, TextChangedEventArgs e)
    {
        Thought sourceThing = CheckThingExistence(sender);
        if (sender is TextBox source && source.Name == "sourceText")
        {
            SetUpRelComboBox(sourceThing);
        }
    }

    private void SetUpRelComboBox(Thought sourceThing,Thought rSelected = null)
    {
        SourceDisambiguation.Items.Clear();
        SourceDisambiguation.Items.Add("<new>");
        SourceDisambiguation.SelectedIndex = 0;
        rBase = null;
        if (sourceThing is not null)
        {
            foreach (Thought t in sourceThing.Descendents)
            {
                if (t != sourceThing && !t.HasProperty("isInstance")) continue;
                foreach (Thought r in t.LinksTo)
                {
                    if (r.LinkType.Label == "has-child") continue;
                    if (r.LinkType.Label == "hasProperty") continue;
                    var cbi = new ComboBoxItem();
                    cbi.Content = r.To.Label;
                    cbi.ToolTip = r.ToString();
                    cbi.SetValue(theLink, r);
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

    Thought rBase = null;
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
                Thought r = (Thought)cbi.GetValue(theLink);
                linkText.Text = r.LinkType.Label;
                targetText.Text = r.To.Label;
                rBase = r;
                SetStatus(r.ToString(), Colors.Yellow);
            }
        }
    }

    //copied from UKSStatementDlg.cs
    private Thought CheckThingExistence(object sender)
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
            List<Thought> tl = ModuleUKSStatement.ThingListFromString(text);
            if (tl is null || tl.Count == 0)
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
    private bool CheckAddLinkFieldsFilled()
    {
        SetStatus("");
        ModuleUKSClause UKSEvent = (ModuleUKSClause)ParentModule;

        if (sourceText.Text == "")
        {
            SetStatus("Source not provided");
            return false;
        }
        if (linkText.Text == "")
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
        if (linkText2.Text == "")
        {
            SetStatus("Clause type not provided");
            return false;
        }
        return true;
    }
}
