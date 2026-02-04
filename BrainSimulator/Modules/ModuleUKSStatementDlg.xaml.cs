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
using UKS;


namespace BrainSimulator.Modules;

public partial class ModuleUKSStatementDlg : ModuleBaseDlg
{
    // Constructor of the ModuleUKSStatement dialog
    public ModuleUKSStatementDlg()
    {
        InitializeComponent();
    }

    // Draw gets called to draw the dialog when it needs refreshing
    public override bool Draw(bool checkDrawTimer)
    {
        if (!base.Draw(checkDrawTimer)) return false;

        return true;
    }

    // BtnAddLink_Click is called when the AddLink button is clicked
    private void BtnAddLink_Click(object sender, RoutedEventArgs e)
    {
        ModuleUKSStatement UKSStatement = (ModuleUKSStatement)ParentModule;
        string newThought = sourceText.Text;
        string targetThought = targetText.Text;
        string relationType = linkText.Text;

        //Special case for [This,is-a,dog]
        if (newThought.ToLower() == "this")
        {
            if (relationType.ToLower().Contains("called"))
            {
                Thought mostRecent = UKSStatement.theUKS.Labeled("mostRecent");
                if (mostRecent is null)
                {
                    SetStatus("'This' is not defined at this time");
                    return;
                }
                Thought mostRecentTarget = mostRecent.LinksTo.FindFirst(x => x.LinkType.Label == "is").To;
                mostRecentTarget.Label = targetThought;
            }
            return;
        }

        if (!CheckAddLinkFieldsFilled()) return;

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

        Thought r1 = UKSStatement.AddLink(newThought, targetThought, relationType);
        if (r1 is not null && setConfCB.IsChecked == true)
        {
            r1.Weight = confidence;
            r1.TimeToLive = duration;
        }

        CheckThoughtExistence(targetText);
        CheckThoughtExistence(sourceText);
        CheckThoughtExistence(linkText);
    }

    // Check for thought existence and set background color of the textbox and the error message accordingly.
    private bool CheckThoughtExistence(object sender)
    {
        if (sender is TextBox tb)
        {
            string text = tb.Text.Trim();

            if (text == "" && !tb.Name.Contains("arget"))
            {
                tb.Background = new SolidColorBrush(Colors.Pink);
                SetStatus("Source and type cannot be empty");
                return false;
            }
            List<Thought> tl = ModuleUKSStatement.ThoughtListFromString(text);
            if (tl is null || tl.Count == 0)
            {
                tb.Background = new SolidColorBrush(Colors.LemonChiffon);
                SetStatus("OK");
                return false;
            }
            tb.Background = new SolidColorBrush(Colors.White);
            SetStatus("OK");
            return true;
        }
        return false;
    }


    // TheGrid_SizeChanged is called when the dialog is sized
    private void TheGrid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        Draw(false);
    }

    // thoughtText_TextChanged is called when the thought textbox changes
    private void Text_TextChanged(object sender, TextChangedEventArgs e)
    {
        CheckThoughtExistence(sender);
    }

    // Check for parent existence and set background color of the textbox and the error message accordingly.
    private bool CheckAddLinkFieldsFilled()
    {
        SetStatus("OK");
        ModuleUKSStatement UKSStatement = (ModuleUKSStatement)ParentModule;

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
        return true;
    }
}
