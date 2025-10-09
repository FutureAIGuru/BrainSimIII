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

    // BtnAddRelationship_Click is called when the AddRelationship button is clicked
    private void BtnAddRelationship_Click(object sender, RoutedEventArgs e)
    {
        string newThing = sourceText.Text;
        string targetThing = targetText.Text;
        string relationType = relationshipText.Text;

        if (!CheckAddRelationshipFieldsFilled()) return;

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


        ModuleUKSStatement UKSStatement = (ModuleUKSStatement)ParentModule;
        Relationship r1 = UKSStatement.AddRelationship(newThing, targetThing, relationType);
        if (r1 != null && setConfCB.IsChecked == true)
        {
            r1.Weight = confidence;
            r1.TimeToLive = duration;
        }

        CheckThingExistence(targetText);
        CheckThingExistence(sourceText);
        CheckThingExistence(relationshipText);
    }

    // Check for thing existence and set background color of the textbox and the error message accordingly.
    private  bool CheckThingExistence(object sender)
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
            List<Thing> tl = ModuleUKSStatement.ThingListFromString(text);
            if (tl == null || tl.Count == 0)
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

    // thingText_TextChanged is called when the thing textbox changes
    private void Text_TextChanged(object sender, TextChangedEventArgs e)
    {
        CheckThingExistence(sender);
    }

    // Check for parent existence and set background color of the textbox and the error message accordingly.
    private bool CheckAddRelationshipFieldsFilled()
    {
        SetStatus("OK");
        ModuleUKSStatement UKSStatement = (ModuleUKSStatement)ParentModule;

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
        return true;
    }
}
