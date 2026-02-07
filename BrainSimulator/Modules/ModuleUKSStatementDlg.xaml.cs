/*
 * Brain Simulator Thought
 *
 * Copyright (c) 2026 Charles Simon
 *
 * This file is part of Brain Simulator Thought and is licensed under
 * the MIT License. You may use, copy, modify, merge, publish, distribute,
 * sublicense, and/or sell copies of this software under the terms of
 * the MIT License.
 *
 * See the LICENSE file in the project root for full license information.
 */
//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using Pluralize.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using UKS;
using static BrainSimulator.Modules.ModuleOnlineInfo;


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


    //these get the data back from the combobox selection 
    Thought tSource = null;

    // BtnAddLink_Click is called when the AddLink button is clicked or ENTER is pressed in one of the textboxes
    private void BtnAddLink_Click(object sender, RoutedEventArgs e)
    {
        ModuleUKSStatement UKSStatement = (ModuleUKSStatement)ParentModule;
        string fromString = sourceText.Text;
        string toString = targetText.Text;
        string linkTypeString = linkText.Text;

        //Special case for [This,is-a,dog]
        if (fromString.ToLower() == "this")
        {
            if (linkTypeString.ToLower().Contains("called"))
            {
                Thought mostRecent = UKSStatement.theUKS.Labeled("mostRecent");
                if (mostRecent is null)
                {
                    SetStatus("'This' is not defined at this time");
                    return;
                }
                Thought mostRecentTarget = mostRecent.LinksTo.FindFirst(x => x.LinkType.Label == "is").To;
                mostRecentTarget.Label = toString;
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

        //hack for "dogs are animals"
        IPluralize pluralizer = new Pluralizer();
        if (pluralizer.IsPlural(fromString) && pluralizer.IsPlural(toString) && linkTypeString == "are")
            linkTypeString = "is-a";


        //hand source though which is itself a link
        var sourceParts = UKSStatement.Singular(fromString.Split(" ", StringSplitOptions.RemoveEmptyEntries));
        if (sourceParts.Length == 3)
        {
            Thought r2 = new()
            {
                From = UKSStatement.theUKS.GetOrAddThought(sourceParts[0]),
                LinkType = UKSStatement.theUKS.GetOrAddThought(sourceParts[1]),
                To = UKSStatement.theUKS.GetOrAddThought(sourceParts[2])
            };
            var existing = UKSStatement.theUKS.GetLinks(r2);
            if (existing.Count == 0)
                tSource = UKSStatement.theUKS.AddStatement(sourceParts[0], sourceParts[1], sourceParts[2]);
            else
            {
                // multiple matches, create a dropdown in the UI to select which one?
                sourceCombo.Visibility = Visibility.Visible;
                sourceCombo.Items.Clear();
                ComboBoxItem cbi = new ComboBoxItem { Content = "<New>", ToolTip = "Create a new Link" };
                cbi.PreviewMouseLeftButtonUp += ComboItem_Clicked;
                sourceCombo.Items.Add(cbi);
                sourceCombo.SelectedIndex = 0;
                //sourceCombo.IsDropDownOpen = true;
                Dispatcher.BeginInvoke(DispatcherPriority.Input, () => sourceCombo.IsDropDownOpen = true);
                foreach (var t in existing)
                {
                    string toolTipText = "";
                    foreach (var r in t.LinksTo.Where(x=>x.LinkType.Label != "is-a"))
                        toolTipText += r.ToString() +  "\n";
                    if (!string.IsNullOrEmpty(toolTipText))
                        toolTipText = toolTipText[..^1];

                    cbi = new()
                    {
                        Content = t,
                        ToolTip = toolTipText,
                    };
                    cbi.PreviewMouseLeftButtonUp += ComboItem_Clicked;
                    sourceCombo.Items.Add(cbi);
                }
                return;
            }
        }

        if (tSource is null)
        {
            tSource = UKSStatement.theUKS.CreateThoughtFromMultipleAttributes(fromString, false);
        }


        Thought r1 = UKSStatement.AddLink(tSource, linkTypeString, toString);

        if (r1 is not null && setConfCB.IsChecked == true)
        {
            r1.Weight = confidence;
            r1.TimeToLive = duration;
        }
        if (r1 is not null && eventCB.IsChecked == true)
        {
            r1.Fire();
            Thought subject = r1.From;
            UKSStatement.theUKS.GetOrAddThought("events", "LinkType");
            Thought previousEvents = subject.LinksTo.FindFirst(x => x.LinkType.Label == "events");
            Thought theSequence = subject.LinksTo.FindFirst(x => x.LinkType.Label == "events")?.To;
            if (theSequence is null)
            {
                Thought t1 = UKSStatement.theUKS.CreateFirstElement(subject, r1);
                subject.RemoveLinks("events");
                subject.AddLink(t1, "events");
            }
            else
            {
                Thought t1 = UKSStatement.theUKS.InsertElement(theSequence, r1);
            }
        }

        CheckThoughtExistence(targetText);
        CheckThoughtExistence(sourceText);
        CheckThoughtExistence(linkText);

        tSource = null;
    }

    private void ComboItem_Clicked(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not ComboBoxItem cbi) return;
        ModuleUKSStatement UKSStatement = (ModuleUKSStatement)ParentModule;
        sourceCombo.Visibility = Visibility.Hidden;
        if (cbi.Content.ToString() == "<New>")
        {
            var sourceParts = UKSStatement.Singular(sourceText.Text.Split(" ", StringSplitOptions.RemoveEmptyEntries));
            if (sourceParts.Length == 3)
            {
                tSource = UKSStatement.theUKS.AddStatement(sourceParts[0], sourceParts[1], sourceParts[2]);
                sourceText.Text = tSource.ToString();
            }
        }
        else
        {
            sourceText.Text = cbi.Content.ToString();
            tSource = (Thought)cbi.Content;
        }
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

    //private void sourceCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    //{
    //    ModuleUKSStatement UKSStatement = (ModuleUKSStatement)ParentModule;
    //    sourceCombo.Visibility = Visibility.Hidden;
    //    if (sourceCombo.SelectedValue.ToString() == "<New>")
    //    {
    //        var sourceParts = UKSStatement.Singular(sourceText.Text.Split(" ", StringSplitOptions.RemoveEmptyEntries));
    //        if (sourceParts.Length == 3)
    //        {
    //            Thought r1 = UKSStatement.AddLink(sourceParts[0], sourceParts[1], sourceParts[2]);
    //            sourceText.Text = r1.ToString();
    //        }
    //    }
    //    else
    //    {
    //        if (sourceCombo.SelectedItem is ComboBoxItem cbi)
    //        {
    //            sourceText.Text = cbi.Content.ToString();
    //            tSource = (Thought)cbi.Content;
    //        }
    //    }
    //}
}
