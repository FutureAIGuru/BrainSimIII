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
        // Error count and relationship count, used for debugging (static for now, working on fix).
        public static int errorCount;
        public static int relationshipCount;

        // Word max set to 10 by default. *Modify/Increase Value at your own risk!*
        int wordMax = 100;
        List<string> words = new List<string>();  // List to hold all words

        public ModuleGPTInfoDlg()
        {
            InitializeComponent();
        }

        private void TheCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }

        private void SetOutputText(string theText)
        {
            txtOutput.Text = theText;
            ModuleGPTInfo mf = (ModuleGPTInfo)base.ParentModule;
            StatusLabel.Content = $"{GPT.totalTokensUsed} tokens used.  {mf.theUKS.Labeled("unknownObject")?.Children.Count} unknown Things.  ";
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                // Reset to 0 each time for error and relationship count
                errorCount = 0;
                relationshipCount = 0;
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
                            while ((line = reader.ReadLine()) != null)
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

                        // Optional: Display the words in the console (commented out by default)
                        /*
                        for (int i = 0; i < 5; i++)
                        {
                            Debug.WriteLine(words[i]);
                        }
                        */

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
        }

        private async void RunButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                if (words.Count == 0)
                {
                    SetOutputText("No file has been added! Cannot run.");
                }
                else
                {
                    await ProcessWordsAsync(words);
                }
            }
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

            txtOutput.Text = $"Done running! Total word count: {words.Count}. Total relationship count: {relationshipCount}. Total error count (not accepted): {errorCount}.";
            Debug.WriteLine($"Done running! Total word count: {words.Count}. Total relationship count: {relationshipCount}. Total error count (not accepted): {errorCount}.");
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

            SetOutputText($"Done running! Total word count: {words.Count}. Total relationship count: {relationshipCount}. Total error count (not accepted): {errorCount}.");
            Debug.WriteLine($"Done running! Total word count: {words.Count}. Total relationship count: {relationshipCount}. Total error count (not accepted): {errorCount}.");
        }

        public async Task VerifyAsync(string label)
        {
            ModuleGPTInfo mf = (ModuleGPTInfo)base.ParentModule;
            if (!label.StartsWith(".")) label = "." + label;
            UKS.Thing child = mf.theUKS.Labeled(label);
            if (child == null) return;
            foreach (Thing parent in child.Parents)
            {
                ModuleGPTInfo.GetChatGPTVerifyParentChild(child.Label, parent.Label);
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
            }
            if (e.Key == Key.Up)
            {
                SetOutputText("Working...");
                string txt = textInput.Text;
                await ModuleGPTInfo.GetChatGPTParents(txt);
                SetOutputText(ModuleGPTInfo.Output);

            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b)
            {
                ModuleGPTInfo mf = (ModuleGPTInfo)base.ParentModule;
                if (b.Content.ToString() == "Re-Parse")
                {
                    ModuleGPTInfo.ParseGPTOutput(textInput.Text, txtOutput.Text);
                }
                else if (b.Content.ToString() == "Verify")
                {
                    VerifyAsync(textInput.Text);
                }
                else if (b.Content.ToString() == "Verify All")
                {
                    SetOutputText("Verifying all is-a relationships");
                    foreach (Thing t in mf.theUKS.UKSList)
                    {
                        if (t.Parents.FindFirst(x => x.Label == "unknownObject") != null) continue;
                        if (!t.Label.StartsWith('.')) continue;
                        if (t.Label == ".") continue;
                        VerifyAsync(t.Label);
                    }
                    SetOutputText("Done");
                }
                else
                {
                    List<string> words = new List<string>();
                    var thingList = mf.theUKS.Labeled("unknownObject").Children;

                    SetOutputText($"Getting parents for {thingList.Count} Things");
                    foreach (var thing in thingList)
                    {
                        if (words.Count >= wordMax) break;
                        words.Add(thing.Label);
                    }

                    ProcessParentsAsync(words);
                }
            }
        }
    }
}