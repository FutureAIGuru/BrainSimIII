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
using UKS;

namespace BrainSimulator.Modules
{

    public partial class ModuleOnlineModificationDlg : ModuleBaseDlg
    {
        // Error count and relationship count, used for debugging (static for now, working on fix).
        public static int errorCount;
        public static int relationshipCount;

        public ModuleOnlineModificationDlg()
        {
            InitializeComponent();
        }

        private void TheCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }

        private async void AddParentsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                // Uncomment below if you want to run it.
                await ProcessChildrenAsync();
            }
        }

        private async void AddMoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                txtOutput.Text = "Not implemented yet!";
                Debug.WriteLine("Not implemented yet!");
            }
        }

        // Task to process the words.
        public async Task ProcessChildrenAsync()
        {
            ModuleOnlineModification mf = (ModuleOnlineModification)base.ParentModule;

            // Get the UKS
            IList<Thing> children = mf.GetUnknownChildren();

            if (children.Count == 0)
            {
                txtOutput.Text = "No unknown objects found!";
            }
            else
            {
                relationshipCount = 0;
                txtOutput.Text = "Adding parents to unknown words... Word count is: " + children.Count + ".";
                Debug.WriteLine("Adding parents to unknown words... Word count is: " + children.Count + ".");

                int childId = 1;

                foreach (Thing child in children)
                {
                    Debug.WriteLine(childId.ToString() + "/" + children.Count + ": " + child.ToString());
                    await mf.GetChatGPTDataParents(child.Label);
                    childId++;
                }

                int newChildCount = mf.GetUnknownChildren().Count;
                txtOutput.Text = $"Done running! Total child count: {children.Count}. Total relationship count: {relationshipCount}. New unknownObject count: {newChildCount}";
                Debug.WriteLine($"Done running! Total child count: {children.Count}. Total relationship count: {relationshipCount}. New unknownObject count: {newChildCount}");

            }

        }

        private void GetUnknownCountButton_Click(object sender, RoutedEventArgs e)
        {
            ModuleOnlineModification mf = (ModuleOnlineModification)base.ParentModule;
            int childCount = mf.GetUnknownChildren().Count;
            txtOutput.Text = $"Child count of unknownObjects is {childCount}.";
            Debug.WriteLine($"Child count of unknownObjects is {childCount}.");
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            txtOutput.Text = "";
        }
    }
}