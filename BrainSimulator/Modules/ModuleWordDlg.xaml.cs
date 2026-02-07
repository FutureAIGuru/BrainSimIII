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
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using UKS;

namespace BrainSimulator.Modules;

public partial class ModuleWordDlg : ModuleBaseDlg
{
    public ModuleWordDlg()
    {
        InitializeComponent();
    }

    private void btnAdd_Click(object sender, RoutedEventArgs e)
    {
        AddCurrentWord();
    }

    bool _isTextChangingInternally = false;
    private void txtWord_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Back || e.Key == Key.Delete)
        {
            _isTextChangingInternally = true;
            int caretIndex = txtWord.CaretIndex;
            if (e.Key == Key.Back) caretIndex--;
            if (caretIndex < 0) caretIndex = 0;
            txtWord.Text = txtWord.Text.Substring(0, caretIndex);
            txtWord.CaretIndex = caretIndex;
            e.Handled = true;
            _isTextChangingInternally = false;
            //get a new suggestion
            if (e.Key == Key.Back)
                txtWord_TextChanged(null, null);
        }
        if (e.Key == Key.Enter)
        {
            AddCurrentWord();
            txtWord.SelectionLength = 0;
        }
    }
    private void txtWord_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isTextChangingInternally)
            return;

        string searchText = txtWord.Text;
        if (!string.IsNullOrEmpty(searchText))
        {
            //get the first suggestion
            var module = ParentModule as ModuleWord;
            if (module is null) return;
            string suggestion = module.GetWordSuggestion(txtWord.Text);
            //get the real label to get the capitalization right
            if (suggestion is not null) suggestion = ThoughtLabels.GetThought(suggestion)?.Label;

            if (suggestion is not null && !suggestion.Equals(searchText, StringComparison.OrdinalIgnoreCase))
            {
                int caretIndex = txtWord.CaretIndex;
                _isTextChangingInternally = true;
                txtWord.Text = suggestion;
                txtWord.CaretIndex = caretIndex;
                txtWord.SelectionStart = caretIndex;
                int newCaretPosition = suggestion.Length - caretIndex;
                if (newCaretPosition < 0) newCaretPosition = 0;
                txtWord.SelectionLength = newCaretPosition;
                txtWord.SelectionOpacity = .4;
                _isTextChangingInternally = false;
            }
        }
    }

    private void AddCurrentWord()
    {
        string word = txtWord.Text?.Trim();
        
        if (string.IsNullOrWhiteSpace(word))
        {
            SetStatus("Please enter a word.");
            return;
        }

        var module = ParentModule as ModuleWord;
        if (module != null)
        {
            ModuleWord.AddWordSpelling(word);
            SetStatus($"Spelling added: {word}", Colors.Black);
            txtWord.Clear();
            txtWord.Focus();
        }
        else
        {
            SetStatus("Error: Module not found.", Colors.Red);
        }
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

        var module = ParentModule as ModuleWord;
        if (module != null)
        {
            SetStatus("Loading words...");
            
            int count = module.LoadWordsFromFile(filePath);
            
            SetStatus($"Successfully loaded {count} word(s) from file.");
        }
        else
        {
            SetStatus("Error: Module not found.");
        }
    }
}