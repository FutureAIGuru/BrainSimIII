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
using static BrainSimulator.Modules.ModuleAttributeBubble;


namespace BrainSimulator.Modules
{
    public class ModuleStressTest : ModuleBase
    {
        public static string Output = "";
        public static int times = 1;
        public static int progress = 1;


        public ModuleStressTest()
        {

        }

        public override void Initialize()
        {

        }

        public override void SetUpAfterLoad()
        {

        }

        public static String AddManyTestItems(int count)
        {
            int maxCount = 1000000;
            List<string> items = new List<string>();

            // Cases where we do not want to go forward.
            if (count <= 0)
            {
                return "Count less or equal to 0, cannot commence.";
            }
            else if (count > maxCount)
            {
                return $"Count greater than maxCount {maxCount.ToString()}, cannot commence.";
            }


            // Add the items as strings.
            for (int i = 0; i < count; i++)
            {
                items.Add(i.ToString());
            }


            int maxChildren = 12;

            // Calculate the number of levels based on the logarithm.
            int levels = (int)Math.Ceiling(Math.Log(count) / Math.Log(maxChildren));

            Debug.WriteLine($"LEVELS IS {levels}");

            times = 1;
            progress = 1;

            DateTime startDate = new DateTime(2024, 1, 1);
            DateTime endDate = DateTime.Now;
            TimeSpan difference = endDate - startDate;
            int totalSeconds = (int)difference.TotalSeconds;

            Debug.WriteLine($"Seconds since 2024/1/1: {totalSeconds}.");

            // Start the recursive hierarchy building
            BuildHierarchy("unknownObject", 0, levels, maxChildren, count, totalSeconds.ToString());

            /*
            // Alternate way to do this (with items in a hierarchy).
            // This way is also faster so it will probably be used in the future.
            int maxOuter = 12;

            for (int i = 0; i < maxOuter; i++)
            {
                Thing parent = MainWindow.theUKS.GetOrAddThing("A" + i.ToString());
                for (int j = 0; j < 12; j++)
                {
                    Thing parent0 = MainWindow.theUKS.GetOrAddThing("B" + i.ToString() + j.ToString(), parent);
                    for (int k = 0; k < 12; k++)
                    {
                        Thing parent1 = MainWindow.theUKS.GetOrAddThing("C" + i.ToString() + j.ToString() + k.ToString(), parent0);
                    }

                }

                Debug.WriteLine($"{i+1}/{maxOuter} done.");

            }
            */

            Debug.WriteLine($"Done! Times {times}");

            return "Items added successfully.";
   
        }


        static void BuildHierarchy(string prefix, int currentLevel, int maxLevel, int maxChildren, int count, String totalSeconds)
        {
            // Base case: stop if the desired count is reached
            if (times >= count || currentLevel >= maxLevel)
                return;

            int remainingNodes = count - times;

            // Calculate how many children to create at this level, keeping the tree balanced
            int childrenAtThisLevel = Math.Min(maxChildren, (int)Math.Ceiling((double)remainingNodes / Math.Pow(maxChildren, maxLevel - currentLevel - 1)));

            for (int i = 0; i < childrenAtThisLevel; i++)
            {
                if (times >= count)
                {
                    break;
                }

                if (times % (count / 10) == 0 && times > 0)
                {
                    Debug.WriteLine($"{progress * 10}% complete.");
                    progress++;
                }

                string nodeName = prefix + "_i_" + i.ToString() + "_" + currentLevel.ToString();

                Thing toAdd = MainWindow.theUKS.GetOrAddThing(nodeName, prefix);
                times++;

                // Recurse to the next level
                BuildHierarchy(nodeName, currentLevel + 1, maxLevel, maxChildren, count, "");
            }
        }

        public override void Fire()
        {
            Init();  //be sure to leave this here

            //if you want the dlg to update, use the following code whenever any parameter changes
            UpdateDialog();
        }

    }
}