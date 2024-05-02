//
// Copyright (c) FutureAI. All rights reserved.  
// Contains confidential and  proprietary information and programs which may not be distributed without a separate license
//  

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BrainSimulator.Modules
{

    public partial class ModuleOnlineFileDlg : ModuleBaseDlg
    {
        // Word max set to 50 by default, modify at your own risk!
        int wordMax = 50;
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
                        using (StreamReader reader = new StreamReader(filePath))
                        {
                            string line;
                            while ((line = reader.ReadLine()) != null)
                            {
                                if (words.Count >= wordMax)
                                {
                                    break;
                                }
                                // Split the line into words, removing empty entries
                                string[] lineWords = line.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                                words.AddRange(lineWords);  // Add the words from this line to the list
                            }
                        }

                        // Optional: Display the words in the console (commented out by default)
                        /*
                        foreach (string word in words)
                        {
                            Console.WriteLine(word);
                        }
                        */

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

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                if (words.Count == 0)
                {
                    txtOutput.Text = "No file has been added! Cannot run.";
                }
                else
                {
                    ModuleOnlineFile mf = (ModuleOnlineFile)base.ParentModule;
                    txtOutput.Text = "Running through words... Word count is: " + words.Count.ToString() + ".";
                    Debug.WriteLine("Running through words... Word count is: " + words.Count.ToString() + ".");
                    foreach (string word in words)
                    {
                        mf.GetChatGPTDataFine(word);
                    }
                    txtOutput.Text = $"Done running! Error count out of total words is Not Implemented Yet / {words.Count.ToString()}.";
                    Debug.WriteLine($"Done running! Error count out of total words is Not Implemented Yet / {words.Count.ToString()}.");
                }
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            txtOutput.Text = "";
        }
    }
}