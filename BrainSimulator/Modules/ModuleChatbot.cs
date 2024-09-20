//
// Copyright (c) FutureAI. All rights reserved.  
// Contains confidential and  proprietary information and programs which may not be distributed without a separate license
//  

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Threading.Tasks;
using System.Windows;
using Pluralize.NET;
using UKS;

namespace BrainSimulator.Modules
{
    public class ModuleChatbot : ModuleBase
    {


        public ModuleChatbot()
        {

        }

        public override void Initialize()
        {

        }

        public override void SetUpAfterLoad()
        {

        }

        public static String ParseInput(String input)
        {
            input = input.Trim();
            input = input.ToLower();

            // Deals with edge cases (turn "is a" to "is-a")
            input = input.Replace("is a", "is-a");

            // Get only letters, digits, and white space.
            char[] arr = input.Where(c => (char.IsLetterOrDigit(c) ||
                             char.IsWhiteSpace(c) ||
                             c == '-')).ToArray();

            input = new string(arr);

            // Split by space
            String[] elements = input.Split(' ');

            String changesMade = "";

            // Find statements based on existing relationships
            // (Note: This is somewhat inefficient as it is a nested for loop)
            for (int i = 0; i < elements.Length; i++)
            {
                foreach (Thing relType in MainWindow.theUKS.GetOrAddThing("RelationshipType").Children)
                {
                    if (relType.Label == elements[i])
                    {
                        if (i > 0 && i < elements.Length-1)
                        {
                            Thing source = MainWindow.theUKS.GetOrAddThing(elements[i - 1]);
                            Thing rel = MainWindow.theUKS.GetOrAddThing(elements[i]);
                            Thing target = MainWindow.theUKS.GetOrAddThing(elements[i + 1]);
                            MainWindow.theUKS.AddStatement(source, rel, target);
                            changesMade += "Added Statment -> " + source + " " + rel + " " + target + " ";
                        }
                    }
                }
            }

            if (changesMade == "")
            {
                changesMade = "I don't understand (no changes made).";
            }

            return changesMade;
        }

        public override void Fire()
        {
            Init();  //be sure to leave this here

            //if you want the dlg to update, use the following code whenever any parameter changes
            UpdateDialog();
        }

    }
}