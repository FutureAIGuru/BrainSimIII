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
using System.Windows.Forms;
using System.Windows.Input;
using UKS;

namespace BrainSimulator.Modules;

public partial class ModuleTextDlg : ModuleBaseDlg
{
    public ModuleTextDlg()
    {
        InitializeComponent();
    }

    public override bool Draw(bool checkDrawTimer)
    {
        if (!base.Draw(checkDrawTimer)) return false;
        //this has a timer so that no matter how often you might call draw, the dialog
        //only updates 10x per second
        ModuleText parent = (ModuleText)base.ParentModule;
        return true;
    }

    private void TheGrid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        Draw(false);
    }

    private void BtnIngest_Click(object sender, RoutedEventArgs e)
    {
            string phrase = tbPhrase.Text ?? string.Empty;

        //string message = ModuleText.AddPhrase(phrase);
        string message = ModuleText.AddText(phrase);
        SetStatus(message);
    }

    private void btnBrowse_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Word List File",
            Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
            CheckFileExists = true
        };

        if (openFileDialog.ShowDialog() == true)
        {
            txtFilePath.Text = openFileDialog.FileName;
        }
    }

    private void btnLoad_Click(object sender, RoutedEventArgs e)
    {
        string filePath = txtFilePath.Text?.Trim();

        if (string.IsNullOrWhiteSpace(filePath))
        {
            SetStatus("Please select a file first.");
            return;
        }

        if (!File.Exists(filePath))
        {
            SetStatus("File not found.");
            return;
        }

        var module = ParentModule as ModuleText;
        if (module != null)
        {
            SetStatus("Loading words...");

            int count = module.LoadTextFromFile(filePath);

            SetStatus($"Successfully loaded {count} word(s) from file.");
        }
        else
        {
            SetStatus("Error: Module not found.");
        }
    }

    private void btnTrigram_Click(object sender, RoutedEventArgs e)
    {
        var module = ParentModule as ModuleText;
        if (module != null)
        {
            SetStatus("Generating trigrams...");
            int count = ModuleText.CreateTrigrams();
            SetStatus($"Successfully generated {count} trigrams.");
        }
        else
        {
            SetStatus("Error: Module not found.");
        }
    }
}