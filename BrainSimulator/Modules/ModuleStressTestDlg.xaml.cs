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

    public partial class ModuleStressTestDlg : ModuleBaseDlg
    {

        // UKS call
        // Needed when accessing all items in the UKS.
        public static ModuleHandler moduleHandler = new();
        public static UKS.UKS theUKS = moduleHandler.theUKS;


        public ModuleStressTestDlg()
        {
            InitializeComponent();
        }

        public override bool Draw(bool checkDrawTimer)
        {
            return base.Draw(checkDrawTimer);
        }

        private void TheCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }

        private void SetOutputText(string theText)
        {
            txtOutput.Text = theText;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                txtOutput.Text = "";
                int count;
                bool isValid = int.TryParse(textInput.Text, out count);
                if (isValid)
                {
                    String message = ModuleStressTest.AddManyTestItems(count);
                    txtOutput.Text = message;
                }
                else
                {
                    txtOutput.Text = "Error! You must provide an integer to run.";
                }
            }
        }
    }
}