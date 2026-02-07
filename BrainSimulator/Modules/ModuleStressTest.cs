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
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Threading.Tasks;
using System.Windows;
using Pluralize.NET;
using UKS;
using static BrainSimulator.Modules.ModuleOnlineInfo;

namespace BrainSimulator.Modules
{
    public class ModuleStressTest : ModuleBase
    {
        public static string Output = "";


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
            int maxCount = 100000;
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

            int times = 0;

            // Add each item to the UKS.

            //for (int i = 0; i < items.Count; i++)
            //{
            //    MainWindow.theUKS.GetOrAddThought(items[i]);
            //    if (i % (items.Count / 10) == 0)
            //    {
            //        Debug.WriteLine($"{times * 10}% complete.");
            //        times++;
            //    }
            //}


            // Alternate way to do this (with items in a hierarchy).
            // This way is also faster so it will probably be used in the future.
            int maxOuter = 100;

            for (int i = 0; i < maxOuter; i++)
            {
                Thought parent = MainWindow.theUKS.GetOrAddThought("A" + i.ToString());
                for (int j = 0; j < 100; j++)
                {
                    Thought parent0 = MainWindow.theUKS.GetOrAddThought("B" + i.ToString() + j.ToString(), parent);
                    for (int k = 0; k < 10; k++)
                    {
                        Thought parent1 = MainWindow.theUKS.GetOrAddThought("C" + i.ToString() + j.ToString() + k.ToString(), parent0);
                    }

                }

                Debug.WriteLine($"{i + 1}/{maxOuter} done.");

            }


            Debug.WriteLine("Done!");

            return "Items added successfully.";
   
        }

        public override void Fire()
        {
            Init();  //be sure to leave this here

            //if you want the dlg to update, use the following code whenever any parameter changes
            UpdateDialog();
        }

    }
}