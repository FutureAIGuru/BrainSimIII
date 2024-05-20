//
// Copyright (c) FutureAI. All rights reserved.  
// Contains confidential and  proprietary information and programs which may not be distributed without a separate license
//  

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Linq;
using Pluralize.NET;
using System.Windows.Xps;
using static System.Net.Mime.MediaTypeNames;
using System.ComponentModel;
using System.Configuration;
using System.Windows.Documents;
using System.Threading.Tasks;
using UKS;

namespace BrainSimulator.Modules
{
    public class ModuleOnlineModification : ModuleBase
    {
        public string Output = "";

        public ModuleOnlineModification()
        {

        }

        public override void Initialize()
        {

        }

        public override void Fire()
        {
            Init();  //be sure to leave this here

            //if you want the dlg to update, use the following code whenever any parameter changes
            UpdateDialog();
        }

        public async Task GetChatGPTDataParents(string textIn)
        {
            try
            {
                if (textIn.StartsWith(".")) textIn = textIn.Substring(1);
                string queryText = $"Provide one or two-word answers completeing the phrase: {textIn} is a ...";
                string answerString = await GPT.GetGPTResult("Provide answers that are common sense seperated by commas.",queryText);

                if (!answerString.StartsWith("ERROR"))
                {

                    // Extract the generated text from the CompletionResult object
                    Output = answerString;
                    Debug.WriteLine(">>>" + queryText);
                    Debug.WriteLine(Output);
                    //some sort of error occurred
                    if (Output.Contains("language model")) return;

                    // Split by comma (,) to get individual values
                    string[] values = Output.Split(",");
                    // Get the UKS
                    GetUKS();
                    foreach (string s in values)
                    {
                        ModuleOnlineModificationDlg.relationshipCount += 1;
                        Debug.WriteLine("Individual Item: " + s);
                        theUKS.AddStatement("."+textIn, "is-a", "."+s.Trim());
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error with getting Chat GPT Fine tuning. Error is {ex}.");
            }
        }

        public IList<Thing> GetUnknownChildren()
        {
            GetUKS();

            Thing unknown = theUKS.GetOrAddThing("unknownObject");

            return unknown.Children;
        }

    }

}