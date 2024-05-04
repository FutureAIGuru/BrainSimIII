//
// Copyright (c) FutureAI. All rights reserved.  
// Contains confidential and  proprietary information and programs which may not be distributed without a separate license
//  

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Pluralize.NET;

namespace BrainSimulator.Modules
{

    public partial class ModuleOnlineFileDlg : ModuleBaseDlg
    {
        // Error count and relationship count, used for debugging (static for now, working on fix).
        public static int errorCount;
        public static int relationshipCount;

        // Word max set to 10 by default. *Modify/Increase Value at your own risk!*
        int wordMax = 10;
        List<string> words = new List<string>();  // List to hold all words

        public ModuleOnlineFileDlg()
        {
            InitializeComponent();
        }

        private void TheCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }

        //this handles both the load and the merge buttons
        [STAThread]
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
                                string[] lineWords = line.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                                // Singularize the words.
                                for (int i = 0; i < lineWords.Length; i++)
                                {
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
                        txtOutput.Text = "An error occurred while reading the file:\n" + error.Message;
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
                    txtOutput.Text = "No file has been added! Cannot run.";
                }
                else
                {
                    await ProcessWordsAsync(words);
                    /*
                    ModuleOnlineFile mf = (ModuleOnlineFile)base.ParentModule;
                    txtOutput.Text = "Running through words... Word count is: " + words.Count.ToString() + ".";
                    Debug.WriteLine("Running through words... Word count is: " + words.Count.ToString() + ".");
                    foreach (string word in words)
                    {
                       mf.GetChatGPTDataFine(word);
                    }
                    //txtOutput.Text = $"Done running! Error count out of total words is {errorCount} / {words.Count.ToString()}.";
                    //Debug.WriteLine($"Done running! Error count out of total words is {errorCount} / {words.Count.ToString()}.");
                    */
                }
            }
        }

        // Task to process the words.
        public async Task ProcessWordsAsync(List<string> words)
        {
            ModuleOnlineFile mf = (ModuleOnlineFile)base.ParentModule;

            txtOutput.Text = "Running through words... Word count is: " + words.Count + ".";
            Debug.WriteLine("Running through words... Word count is: " + words.Count + ".");

            foreach (string word in words)
            {
                await mf.GetChatGPTDataFine(word);
            }

            txtOutput.Text = $"Done running! Total word count: {words.Count}. Total relationship count: {relationshipCount}. Total error count (not accepted): {errorCount}.";
            Debug.WriteLine($"Done running! Total word count: {words.Count}. Total relationship count: {relationshipCount}. Total error count (not accepted): {errorCount}.");
        }


        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            txtOutput.Text = "";
        }
    }
}