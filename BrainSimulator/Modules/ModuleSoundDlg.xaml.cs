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

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UKS;

namespace BrainSimulator.Modules;

public partial class ModuleSoundDlg : ModuleBaseDlg
{
    public ModuleSoundDlg()
    {
        InitializeComponent();
    }

    public override bool Draw(bool checkDrawTimer)
    {
        if (!base.Draw(checkDrawTimer)) return false;
        //this has a timer so that no matter how often you might call draw, the dialog
        //only updates 10x per second
        ModuleSound parent = (ModuleSound)base.ParentModule;
        return true;
    }

    private void TheGrid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        Draw(false);
    }

    private void PlaySound_Click(object sender, RoutedEventArgs e)
    {
        var module = ParentModule as ModuleSound;
        if (module != null)
        {
            module.PlayCMajorTriad(1000);
        }
    }


    private void PitchButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            string pitch = button.Content.ToString();
            PlayNote(pitch);
        }
    }

    private void PlayNote(string pitch)
    {
        var module = ParentModule as ModuleSound;
        if (module != null)
        {
            module.FireNote(pitch);
        }
    }

    private void cbCadence_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ParentModule is ModuleSound module && cbCadence.SelectedItem is ComboBoxItem item)
        {
            if (int.TryParse(item.Content.ToString(), out int value))
            {
                module.Cadence = value;
            }
        }
    }

    private void cbPitchOffset_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ParentModule is ModuleSound module && cbPitchOffset.SelectedItem is ComboBoxItem item)
        {
            if (int.TryParse(item.Content.ToString(), out int value))
            {
                module.PitchOffset = value;
            }
        }
    }
}

