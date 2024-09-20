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

    public partial class ModuleChatbotDlg : ModuleBaseDlg
    {

        // UKS call
        // Needed when accessing all items in the UKS.
        public static ModuleHandler moduleHandler = new();

        public ModuleChatbotDlg()
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                String input = textInput.Text;
                if (input.Length > 0)
                {
                    StatusLabel.Content = "";
                    textPrevious.Text += "\n\nUser: " + input;
                    String output = ModuleChatbot.ParseInput(input);
                    textPrevious.Text += "\n\nBrain Sim III: " + output;
                    textInput.Text = "";
                }
                else
                {
                    StatusLabel.Content = "Error, message cannot be empty.";
                }
            }
        }

        private void txtOutput_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void TheGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }

    }
}