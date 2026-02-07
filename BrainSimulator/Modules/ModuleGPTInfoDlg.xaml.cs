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
// Copyright (c) FutureAI. All rights reserved.  
// Contains confidential and  proprietary information and programs which may not be distributed without a separate license
//  

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Pluralize.NET;
using UKS;

namespace BrainSimulator.Modules
{

    public partial class ModuleGPTInfoDlg : ModuleBaseDlg
    {
        // Error count and link count, used for debugging (static for now, working on fix).
        public static int errorCount;
        public static int linkCount;

        // Word max set to 10 by default. *Modify/Increase Value at your own risk!*
        int wordMax = 100;
        List<string> words = new List<string>();  // List to hold all words

        // UKS call
        // Needed when accessing all items in the UKS.
        public static ModuleHandler moduleHandler = new();
        public static UKS.UKS theUKS = moduleHandler.theUKS;


        public ModuleGPTInfoDlg()
        {
            InitializeComponent();
        }

        public override bool Draw(bool checkDrawTimer)
        {
            ModuleGPTInfo mf = (ModuleGPTInfo)base.ParentModule;
            StatusLabel.Content = $"{GPT.totalTokensUsed} tokens used.  {mf.theUKS.Labeled("Unknown")?.Children.Count} unknown Thoughts.  ";
            return base.Draw(checkDrawTimer);
        }

        private void TheCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }

        private void SetOutputText(string theText)
        {
            txtOutput.Text = theText;
            ModuleGPTInfo mf = (ModuleGPTInfo)base.ParentModule;
            StatusLabel.Content = $"{GPT.totalTokensUsed} tokens used.  {mf.theUKS.Labeled("Unknown")?.Children.Count} unknown Thoughts.  ";
        }

        private async void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                // Reset to 0 each time for error and link count
                errorCount = 0;
                linkCount = 0;
                MainWindow.SuspendEngine();
                words.Clear();
                System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
                openFileDialog.Title = "Select a file";
                openFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"; // Filter files by extension

                if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string filePath = openFileDialog.FileName;

                    try
                    {
                        var pluralizer = new Pluralizer();

                        using (StreamReader reader = new StreamReader(filePath))
                        {
                            string line;
                            while ((line = reader.ReadLine()) is not null)
                            {
                                // Break if we are over word max (set to 50 by default) for testing.
                                if (words.Count >= wordMax)
                                {
                                    break;
                                }
                                // Split the line into words, removing empty entries
                                List<string> lineWords = line.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                                // Singularize the words.
                                for (int i = 0; i < lineWords.Count; i++)
                                {
                                    if (lineWords[i].Trim() == "")
                                    {
                                        lineWords.RemoveAt(i);
                                        i--;
                                    }
                                    lineWords[i] = pluralizer.Singularize(lineWords[i]);
                                }
                                // Add the words to the list from the line
                                words.AddRange(lineWords);
                            }
                        }
                        txtOutput.Text = "File Successfully Read!";
                    }
                    catch (IOException error)
                    {
                        Debug.WriteLine("An error occurred while reading the file:");
                        SetOutputText("An error occurred while reading the file:\n" + error.Message);
                        Debug.WriteLine(error.Message);
                    }
                }
                else
                {
                    txtOutput.Text = "No file selected.";
                    Debug.WriteLine("No file selected.");
                }
            }
            await ProcessWordsAsync(words);
        }

        private async void LoadAmbiguity_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                // Reset to 0 each time for error and link count
                errorCount = 0;
                linkCount = 0;
                MainWindow.SuspendEngine();
                words.Clear();
                System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
                openFileDialog.Title = "Select a file";
                openFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"; // Filter files by extension

                if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string filePath = openFileDialog.FileName;

                    try
                    {
                        var pluralizer = new Pluralizer();

                        using (StreamReader reader = new StreamReader(filePath))
                        {
                            string line;
                            while ((line = reader.ReadLine()) is not null)
                            {
                                // Break if we are over word max (set to 50 by default) for testing.
                                if (words.Count >= wordMax)
                                {
                                    break;
                                }
                                // Split the line into words, removing empty entries
                                List<string> lineWords = line.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                                // Singularize the words.
                                for (int i = 0; i < lineWords.Count; i++)
                                {
                                    if (lineWords[i].Trim() == "")
                                    {
                                        lineWords.RemoveAt(i);
                                        i--;
                                    }
                                    lineWords[i] = pluralizer.Singularize(lineWords[i]);
                                }
                                // Add the words to the list from the line
                                words.AddRange(lineWords);
                            }
                        }
                        txtOutput.Text = "File Successfully Read!";
                    }
                    catch (IOException error)
                    {
                        Debug.WriteLine("An error occurred while reading the file:");
                        SetOutputText("An error occurred while reading the file:\n" + error.Message);
                        Debug.WriteLine(error.Message);
                    }
                }
                else
                {
                    txtOutput.Text = "No file selected.";
                    Debug.WriteLine("No file selected.");
                }
            }
            await ProcessAmbiguityAsync(words);

        }


        // Task to process the words.
        public async Task ProcessWordsAsync(List<string> words)
        {
            ModuleGPTInfo mf = (ModuleGPTInfo)base.ParentModule;

            SetOutputText("Running through words... Word count is: " + words.Count + ".");
            Debug.WriteLine("Running through words... Word count is: " + words.Count + ".");

            foreach (string word in words)
            {
                if (word == words.Last())
                    await ModuleGPTInfo.GetChatGPTData(word.Trim());
                if (word.Trim() != "")
                    ModuleGPTInfo.GetChatGPTData(word.Trim());
            }

            txtOutput.Text = $"Done running! Total word count: {words.Count}. Total link count: {linkCount}. Total error count (not accepted): {errorCount}.";
            Debug.WriteLine($"Done running! Total word count: {words.Count}. Total link count: {linkCount}. Total error count (not accepted): {errorCount}.");
        }

        // Task to process the words AND remove ambiguity.
        public async Task ProcessAmbiguityAsync(List<string> words)
        {
            ModuleGPTInfo mf = (ModuleGPTInfo)base.ParentModule;

            SetOutputText("Running through words... Word count is: " + words.Count + ".");
            Debug.WriteLine("Running through words... Word count is: " + words.Count + ".");

            foreach (string word in words)
            {
                if (word == words.Last())
                    await ModuleGPTInfo.DisambiguateTermsFile(word.Trim());
                if (word.Trim() != "")
                    ModuleGPTInfo.DisambiguateTermsFile(word.Trim());
            }

            txtOutput.Text = $"Done running! Total word count: {words.Count}. Total link count: {linkCount}. Total error count (not accepted): {errorCount}.";
            Debug.WriteLine($"Done running! Total word count: {words.Count}. Total link count: {linkCount}. Total error count (not accepted): {errorCount}.");
        }


        // Task to process the words.
        public async Task ProcessParentsAsync(List<string> words)
        {
            txtOutput.Text = "Getting Parents: Running through words... Word count is: " + words.Count + ".";
            Debug.WriteLine("Getting Parents: Running through words... Word count is: " + words.Count + ".");

            foreach (string word in words)
            {
                if (word == words.Last())
                    await ModuleGPTInfo.GetChatGPTParents(word.Trim());
                if (word.Trim() != "")
                    ModuleGPTInfo.GetChatGPTParents(word.Trim());
            }

            SetOutputText($"Done processing unknowns! Total word count: {words.Count}. Total link count: {linkCount}. Total error count (not accepted): {errorCount}.");
            Debug.WriteLine($"Done running! Total word count: {words.Count}. Total link count: {linkCount}. Total error count (not accepted): {errorCount}.");
        }

        static int count = 0;
        public async Task verifyAllAsync()
        {
            count = 0;
            ModuleGPTInfo mf = (ModuleGPTInfo)base.ParentModule;
            SetOutputText("Verifying all is-a links");
            foreach (Thought t in mf.theUKS.AllThoughts)
            {
                if (t.Parents.FindFirst(x => x.Label == "Unknown") is not null) continue;
                if (!t.Label.StartsWith('.')) continue;
                if (t.Label == ".") continue;
                if (t == mf.theUKS.AllThoughts.Last())
                    await VerifyAsync(t.Label);
                else
                    VerifyAsync(t.Label);
            }
            SetOutputText($"Done verifying is-a links for reasonableness. Checked {count} links.");

        }
        public async Task VerifyAsync(string label)
        {
            ModuleGPTInfo mf = (ModuleGPTInfo)base.ParentModule;
            if (!label.StartsWith(".")) label = "." + label;
            UKS.Thought t = mf.theUKS.Labeled(label);
            if (t is null) return;
            foreach (Thought r in t.LinksTo)
            {
                //if (r.GPTVerified) continue;
                if (r.LinkType.Label != "has-child") continue;

                count++;
                ModuleGPTInfo.GetChatGPTVerifyParentChild(r.To.Label, t.Label);
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            SetOutputText(txtOutput.Text = "");
        }

        private async void textInput_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SetOutputText("Working...");
                string txt = textInput.Text;
                await ModuleGPTInfo.GetChatGPTData(txt);
                SetOutputText(ModuleGPTInfo.Output);
                txtOutput.Text += $"\n\rTotal link count: {linkCount}. Total error count (not accepted): {errorCount}.";
            }
            if (e.Key == Key.Up)
            {
                SetOutputText("Working...");
                string txt = textInput.Text;
                await ModuleGPTInfo.GetChatGPTParents(txt);
                SetOutputText(ModuleGPTInfo.Output);

            }
        }

        private async void GetGPTClausesAsync()
        {
            SetOutputText("Working...");
            await ModuleGPTInfo.GetChatGPTClauses();
            SetOutputText($"\n\rTotal clause count: {linkCount}. Total error count (not accepted): {errorCount}.");
        }

        private async void SolveAmbiguityGPTAsync()
        {
            SetOutputText("Working...");
            await ModuleGPTInfo.DisambiguateTerms();
            SetOutputText($"\n\rTotal disambiguity success count: {linkCount}. Total error count (not accepted): {errorCount}.");
        }

        private async void SolveDuplicatesAsync()
        {
            SetOutputText("Working...");
            await ModuleGPTInfo.SolveDuplicates();
            SetOutputText($"\n\rDone! Duplicates resolved: {linkCount}.");
        }

        

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b)
            {
                ModuleGPTInfo mf = (ModuleGPTInfo)base.ParentModule;
                // Reset to 0 each time for error and link count
                errorCount = 0;
                linkCount = 0;
                if (b.Content.ToString().StartsWith("Re-Parse"))
                {
                    ModuleGPTInfo.ParseGPTOutput(textInput.Text, txtOutput.Text);
                }
                else if (b.Content.ToString().StartsWith("Verify All"))
                {
                    verifyAllAsync();
                }
                else if (b.Content.ToString().StartsWith("Add Clauses To All"))
                {
                    GetGPTClausesAsync();
                }
                else if (b.Content.ToString().StartsWith("Solve Ambiguity"))
                {
                    SolveAmbiguityGPTAsync();
                }
                else if (b.Content.ToString().StartsWith("Remove Duplicates"))
                {
                    SolveDuplicatesAsync();
                }
                else //process unknowns
                {
                    List<string> words = new List<string>();
                    var thoughtList = mf.theUKS.Labeled("Unknown").Children;

                    SetOutputText($"Getting parents for {thoughtList.Count} Thoughts");
                    foreach (var thought in thoughtList)
                    {
                        if (words.Count >= wordMax) break;
                        words.Add(thought.Label);
                    }

                    ProcessParentsAsync(words);
                }
            }
        }
    }
}