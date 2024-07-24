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
            int maxCount = 10000;
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

            // Add each item to the UKS.
            foreach (string item in items)
            {
                MainWindow.theUKS.GetOrAddThing(item);
            }

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