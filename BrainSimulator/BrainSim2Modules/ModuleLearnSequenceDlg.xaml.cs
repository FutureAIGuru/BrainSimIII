//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System.Windows;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System;

namespace BrainSimulator.Modules;
public partial class ModuleLearnSequenceDlg : ModuleBaseDlg
{
    public ModuleLearnSequenceDlg()
    {
        InitializeComponent();
    }

    private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
    {
        Regex regex = new("[^0-9]+");
        e.Handled = regex.IsMatch(e.Text);
    }

    public override bool Draw(bool checkDrawTimer)
    {
        if (!base.Draw(checkDrawTimer)) return false;
        //this has a timer so that no matter how often you might call draw, the dialog
        //only updates 10x per second

        //use a line like this to gain access to the parent's public variables
        //ModuleEmpty parent = (ModuleEmpty)base.ParentModule;

        //here are some other possibly-useful items
        //theCanvas.Children.Clear();
        //Point windowSize = new Point(theCanvas.ActualWidth, theCanvas.ActualHeight);
        //Point windowCenter = new Point(windowSize.X / 2, windowSize.Y / 2);
        //float scale = (float)Math.Min(windowSize.X, windowSize.Y) / 12;
        //if (scale == 0) return false;

        ModuleLearnSequence parent = (ModuleLearnSequence)base.ParentModule;

        learnedLabel.Content = parent.NeuronListToString();

        return true;
    }

    private void TheCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        Draw(false);
    }

    private void ButtonUpdate_Click(object sender, RoutedEventArgs e)
    {
        ModuleLearnSequence parent = (ModuleLearnSequence)base.ParentModule;
        // values before update
        int sequences = parent.GetMaxNumberOfSequences();
        int length = parent.GetMaxSequenceLength();
        int pause = parent.GetPauseTimeInSeconds();

        // update values
        try
        {
            sequences = int.Parse(TextBoxMaxLeanredSequences.Text);
            length = int.Parse(TextBoxMaxSequenceLength.Text);
            pause = int.Parse(TextBoxPauseLimit.Text);
        }
        catch (Exception)
        {
        }

        // set values
        parent.SetMaxNumberOfSequences(sequences);
        parent.SetMaxSequenceLength(length);
        parent.SetPauseTime(pause);

        // display updated valyes - in case they entered invalid value, reset to valid value
        TextBoxMaxLeanredSequences.Text = parent.GetMaxNumberOfSequences().ToString();
        TextBoxMaxSequenceLength.Text = parent.GetMaxSequenceLength().ToString();
        TextBoxPauseLimit.Text = parent.GetPauseTimeInSeconds().ToString();
    }

    private void theCanvas_Loaded(object sender, RoutedEventArgs e)
    {
        ModuleLearnSequence parent = (ModuleLearnSequence)base.ParentModule;
        TextBoxMaxSequenceLength.Text = parent.GetMaxSequenceLength().ToString();
        TextBoxMaxLeanredSequences.Text = parent.GetMaxNumberOfSequences().ToString();
    }
}

