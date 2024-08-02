﻿//
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
    public class ModuleGPTInfo : ModuleBase
    {
        public static string Output = "";


        public ModuleGPTInfo()
        {

        }

        public override void Initialize()
        {

        }

        public override void SetUpAfterLoad()
        {
            string apiKey = ConfigurationManager.AppSettings["APIKey"];
            if (!apiKey.StartsWith("sk"))
            {
                MessageBox.Show(@"OpenAI GPT API Key was not found in the app.config file. GPT Info requests will be ignored. To set up: after getting an API key from OpenAI, put it in the app.config file in the form:
    <appSettings>
        <add key=""APIKey"" value=""<YOUR_API_KEY_HERE>"" />
    </appSettings>
", "API Key Not found", MessageBoxButton.OK);
            }
        }

        public override void Fire()
        {
            Init();  //be sure to leave this here

            //if you want the dlg to update, use the following code whenever any parameter changes
            UpdateDialog();
        }

        //these are static so they can be called from the UKS dialog context menu
        //This can verify parent-child releationships.
        public static async Task GetChatGPTVerifyParentChild(string child,string parent)
        {
            //these turns dotted names back into more english-language strings
            string child1 = GetStringFromThingLabel(child);
            string parent1 = GetStringFromThingLabel(parent);

            try
            {

                string answerString = "";
                string systemText = $"Provide commonsense facts about the following: ";
                string userText = $"Is the following true: a(n) {child1} is a(n) {parent1}? (yes or no, no explanation)";

                answerString = await GPT.GetGPTResult(userText, systemText);
                if (!answerString.StartsWith("ERROR") && answerString != "")
                {
                    Output = answerString;
                    Debug.WriteLine(">>>" + userText);
                    Debug.WriteLine(Output);
                    //some sort of error occurred
                    if (Output.Contains("language model")) return;
                    if (!answerString.Contains("yes"))
                    {
                        Thing tParent = MainWindow.theUKS.Labeled(parent);
                        if (tParent == null) tParent = MainWindow.theUKS.Labeled("." + parent);
                        if (tParent == null) return;
                        Thing tChild = MainWindow.theUKS.Labeled(child);
                        if (tChild == null) tChild = MainWindow.theUKS.Labeled("." + child);
                        if (tChild == null) return;
                        tChild.RemoveParent(tParent);
                        if (tChild.Parents.Count == 0)
                            tChild.AddParent(MainWindow.theUKS.Labeled("unknownObject"));
                        Debug.WriteLine($"Relationship: {child} is-a {parent} has been removed. ");
                    }
                    else
                    {
                        //tag item as verified so we don't try it again
                        Relationship r = MainWindow.theUKS.GetRelationship(parent, "has-child", child);
                        if (r != null)
                        {
                            r.GPTVerified = true;
                            Debug.WriteLine($"Relationship: {child} is-a {parent} has verified. ");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting data from GPT. Error is {ex}.");
            }

        }

        //this turns dotted names back into more english-language strings
        private static string GetStringFromThingLabel(string thingLabel)
        {
            string theString = thingLabel.ToLower();
            if (theString[0] == '.') theString = theString.Substring(1);
            string[] s = theString.Split('.');
            theString = "";
            for (int i = 1; i < s.Length; i++)
                theString += ((theString.Length == 0) ? "" : " ") + s[i];
            theString += ((theString.Length == 0) ? "" : " ") + s[0];
            return theString;
        }

        //this is used to add parents to unknownObjects
        public static async Task GetChatGPTParents(string textIn)
        {
            try
            {
                textIn = textIn.ToLower();
                if (textIn.StartsWith("."))
                    textIn = textIn.Substring(1);

                string answerString = "";
                string userText = $"Provide commonsense clasification answer the request which is appropriate for a 5 year old: What is-a {textIn}";
                string systemText = 
$@"This is a classification request. Examples: horse is-a | animal, mammal \n\r chimpanzee is-a | primate, mammal
Answer is formatted: is-a | VALUE, VALUE, VALUE with no more than 3 values and VAIUES are 1 or 2 words.
Answer should ONLY contain VALUEs where it is reasonable to say: '{textIn} is-a VALUE' and exclude: 'VALUE is-a {textIn}' 
Never include {textIn} in the result.";

                answerString = await GPT.GetGPTResult(userText, systemText);
                if (!answerString.StartsWith("ERROR") && answerString != "")
                {
                    Output = answerString;
                    Debug.WriteLine(">>>" + userText);
                    Debug.WriteLine(Output);
                    //some sort of error occurred
                    if (Output.Contains("language model")) return;

                    ParseGPTOutput(textIn, Output);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error with getting Chat GPT Fine tuning. Error is {ex}.");
            }
        }

        //this is a general factual information request from GPT
        public static async Task GetChatGPTData(string textIn)
        {
            try
            {
                textIn = textIn.ToLower();
                textIn = textIn.Replace(".", "");

                string answerString = "";
                string userText = $"Provide commonsense facts to answer the request: what is a {textIn}";
                string systemText = 
@"Provide answers that are common sense to a 10 year old. 
Each Answer in the formatted: VALUE-NAME | VALUE, VALUE, VALUE
**Individual ANSWERS should contain no more than 3 values**
**Individual VALUE should be ONE or TWO words.**
Example for dog: is-a | animal, pet
Example for numerical VALUE: contains (with counts) always contains parts | 2 eyes, 4 legs, 1 tail
Use any VALUE only once. 
Use the following VALUE-NAMEs if appropriate: 
is-a (where each value is a physical thing), 
can, 
always contains parts (with counts),
usually contains parts (with counts)
has unique characteristics,
is-part-of-speech, ";
                //the following have been tried but are not very consistent/useful
                //$"list examples of {textIn} (up to 5) do not include things which have a property of {textIn} , " +
                //"needs, " +
                //"is-part-of, " +
                //"is-bigger-than, " +
                //"is-smaller-than, " +
                //"is-similar-to, " +

                answerString = await GPT.GetGPTResult(userText, systemText);
                if (!answerString.StartsWith("ERROR") && answerString != "")
                {
                    Output = answerString;
                    Debug.WriteLine(">>>" + userText);
                    Debug.WriteLine(Output);
                    //some sort of error occurred
                    if (Output.Contains("language model")) return;

                    ParseGPTOutput(textIn, Output);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error with getting Chat GPT Fine tuning. Error is {ex}.");
            }
        }

        //this is a general attempt to add a single clause to every item that is a descedent of "Object".
        public static async Task GetChatGPTClauses()
        {
            try
            {
                UKS.UKS theUKS = MainWindow.theUKS;
                foreach (Thing t in theUKS.Labeled("Object").Descendents) {
                    // Get the label and sanitize the input.
                    String textIn = t.Label;
                    textIn = textIn.ToLower();
                    textIn = textIn.Replace(".", "");

                    // ChatGPT request made.
                    string answerString = "";
                    string userText = $"Provide a commonsense clause to answer the request about the following: {textIn}";
                    string systemText =
                        @"Provide answers that are common sense to a 10 year old. 
                    Format: Source1, Verb1, Target1, IF, Source2, Verb2, Target2
                    Example 1: car, can, move, IF, car, has, fuel
                    Example 2: dog, will, bark, IF, loud noise, is, heard
                    Make sure to have 7 items seperated by 6 commas.";

                    answerString = await GPT.GetGPTResult(userText, systemText);
                    if (!answerString.StartsWith("ERROR") && answerString != "")
                    {
                        Output = answerString;
                        Debug.WriteLine(">>>" + userText);
                        Debug.WriteLine(Output);
                        //some sort of error occurred
                        if (Output.Contains("language model")) return;

                        ParseGPTOutputClause(textIn, Output);
                    }
                }
                
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error with getting Chat GPT Clauses. Error is {ex}.");
            }
        }

        //this is a general attempt to disambiguate all words that are descendents of "Object".
        public static async Task DisambiguateTerms()
        {
            try
            {
                UKS.UKS theUKS = MainWindow.theUKS;
                int limit = 20;
                foreach (Thing t in theUKS.Labeled("Object").Descendents)
                {
                    limit--;
                    if (limit <= 0) break;
                    // Get the label and sanitize the input.
                    String textIn = t.Label;
                    textIn = textIn.ToLower();
                    textIn = textIn.Replace(".", "");

                    // ChatGPT request made.
                    string answerString = "";
                    string userText = $"Provide commonsense unambiguous item(s) to answer the request about the following: {textIn}";
                    string systemText =
                        @"Provide answers that are common sense to a 10 year old. 
                    Format: Item, UnambiguousThing1 | Item, UnambiguousThing2 | <More Here>
                    Example 1: can, action | can, container
                    Example 2: bow, gesture | bow, weapon | bow, knot
                    Example 3: oxygen, chemical
                    Remember the goal is to disambiguiate the words by providing multiple parents if the word is ambiguous.";

                    answerString = await GPT.GetGPTResult(userText, systemText);
                    if (!answerString.StartsWith("ERROR") && answerString != "")
                    {
                        Output = answerString;
                        Debug.WriteLine(">>>" + userText);
                        Debug.WriteLine(Output);
                        //some sort of error occurred
                        if (Output.Contains("language model")) return;

                        ParseGPTOutputAmbiguity(textIn, Output);
                    }
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error with getting Chat GPT Clauses. Error is {ex}.");
            }
        }

        //this is a general attempt to disambiguate all words that are from a file.
        public static async Task DisambiguateTermsFile(string textIn)
        {
            try
            {
                UKS.UKS theUKS = MainWindow.theUKS;
                // Get the label and sanitize the input.
                textIn = textIn.ToLower();
                textIn = textIn.Replace(".", "");

                // ChatGPT request made.
                string answerString = "";
                string userText = $"Provide commonsense unambiguous parent(s) to answer the request about the following: {textIn}";
                string systemText =
                        @"Provide answers that are common sense to a 10 year old. 
                    Format: Item, UnambiguousThing1 | Item, UnambiguousThing2 | <More Here>
                    Example 1: can, action | can, container
                    Example 2: bow, gesture | bow, weapon | bow, knot
                    Example 3: oxygen, chemical
                    Remember the goal is to disambiguiate the words by providing multiple parents if the word is ambiguous.";

                answerString = await GPT.GetGPTResult(userText, systemText);
                if (!answerString.StartsWith("ERROR") && answerString != "")
                {
                    Output = answerString;
                    Debug.WriteLine(">>>" + userText);
                    Debug.WriteLine(Output);
                    //some sort of error occurred
                    if (Output.Contains("language model")) return;
                    ParseGPTOutputAmbiguity(textIn, Output);
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error with getting Chat GPT Clauses. Error is {ex}.");
            }
        }

        // Given information from caluses, parse it into the UKS.
        // The general format is all commas (,).
        // It only creates exactly (1) clause per descendant of object.
        public static void ParseGPTOutputAmbiguity(String textIn, string GPTOutput)
        {
            // Get the UKS
            UKS.UKS theUKS = MainWindow.theUKS;
            //GetUKS();
            // Split by pipe (|) to get the individual parents
            string[] parents = GPTOutput.Split('|');
            // First we make the label a word.
            String englishWord = "." + textIn;
            theUKS.AddStatement(englishWord, "is-a", "Word");
            // Count to capture the amount of values being calculated.
            int count = 0;
            // Then we run through each individual parent.
            foreach (String parent in parents)
            {
                // Split by comma (,) to get individual pairs
                string[] valuePairs = parent.Split(',');
                // Error checks
                if (valuePairs.Length == 0)
                {
                    Debug.WriteLine($"Error, length of value '{parent}' is 0.");
                    ModuleGPTInfoDlg.errorCount += 1;
                }
                else if (valuePairs.Length != 2)
                {
                    Debug.WriteLine($"Error, length of value '{parent}' is not what is expected, 2.");
                    ModuleGPTInfoDlg.errorCount += 1;
                }
                else
                {
                    // Singularize the word
                    textIn = GPT.Singularize(textIn);

                    // The second value of the pair is the new parent.
                    String newParent = valuePairs[1];

                    // Make the english word "means" the abstract item.
                    Relationship r = theUKS.AddStatement(englishWord, "means", textIn + "*");

                    // Make the disambiguated term a child of the parent.
                    theUKS.AddStatement(r.target, "is-a", newParent);

                    // Increment count to see how many disambiguous items there are.
                    count++;

                    // Incrememnt successful relationships
                    ModuleGPTInfoDlg.relationshipCount += 1;
                }
            }
        }

        // Given information from caluses, parse it into the UKS.
        // The general format is all commas (,).
        // It only creates exactly (1) clause per descendant of object.
        public static void ParseGPTOutputClause(String textIn, string GPTOutput)
        {
            // Get the UKS
            UKS.UKS theUKS = MainWindow.theUKS;
            //GetUKS();
            // Split by comma (,) to get individual pairs
            string[] valuePairs = GPTOutput.Split(',');
            // Error checks
            if (valuePairs.Length == 0)
            {
                Debug.WriteLine($"Error, length of value '{Output}' is 0.");
                ModuleGPTInfoDlg.errorCount += 1;
            }
            else if (valuePairs.Length != 7)
            {
                Debug.WriteLine($"Error, length of value '{Output}' is not 7.");
                ModuleGPTInfoDlg.errorCount += 1;
            }
            // Make sure the clause is IF, used for testing purposes.
            else if (valuePairs[3].Trim() != "if")
            {
                Debug.WriteLine($"Error, middle clause isn't IF, but is '{valuePairs[3]}'");
                ModuleGPTInfoDlg.errorCount += 1;
            }
            else
            {
                textIn = GPT.Singularize(textIn);

                // Setting up the values from GPT
                string newThing = valuePairs[0].Trim();
                string relationType = valuePairs[1].Trim();
                string targetThing = valuePairs[2].Trim();

                // Hard code clase to IF...
                // Or to valuePairs[3].Trim() otherwise
                string clauseType = "IF";

                string newThing2 = valuePairs[4].Trim();
                string relationType2 = valuePairs[5].Trim();
                string targetThing2 = valuePairs[6].Trim();


                // Add relationships and clause
                Relationship r1 = AddRelationshipClause(newThing, targetThing, relationType);

                Relationship r2 = AddRelationshipClause(newThing2, targetThing2, relationType2);

                Thing theClauseType = GetClauseType(clauseType);

                r1.AddClause(theClauseType, r2);

                ModuleGPTInfoDlg.relationshipCount += 1;
            }
        }

        // Add Relationship Clause.
        // NOTES: Copied from ModuleUKSClause exactly, working on a fix.
        public static Relationship AddRelationshipClause(string source, string target, string relationshipType)
        {
            UKS.UKS theUKS = MainWindow.theUKS;
            if (theUKS == null) return null;
            IPluralize pluralizer = new Pluralizer();


            source = source.Trim();
            target = target.Trim();
            relationshipType = relationshipType.Trim();

            string[] tempStringArray = source.Split(' ');
            List<string> sourceModifiers = new();
            source = pluralizer.Singularize(tempStringArray[tempStringArray.Length - 1]);
            for (int i = 0; i < tempStringArray.Length - 1; i++) sourceModifiers.Add(pluralizer.Singularize(tempStringArray[i]));

            tempStringArray = target.Split(' ');
            List<string> targetModifiers = new();
            target = pluralizer.Singularize(tempStringArray[tempStringArray.Length - 1]);
            for (int i = 0; i < tempStringArray.Length - 1; i++) targetModifiers.Add(pluralizer.Singularize(tempStringArray[i]));

            tempStringArray = relationshipType.Split(' ');
            List<string> typeModifiers = new();
            relationshipType = pluralizer.Singularize(tempStringArray[0]);
            for (int i = 1; i < tempStringArray.Length; i++) typeModifiers.Add(pluralizer.Singularize(tempStringArray[i]));

            Relationship r = theUKS.AddStatement(source, relationshipType, target, sourceModifiers, typeModifiers, targetModifiers);

            return r;
        }

        // Get clause type.
        // NOTES: Copied directly from ModuleUKSClause like AddRelationshipClause, looking for a fix.
        public static Thing GetClauseType(string newThing)
        {
            UKS.UKS theUKS = MainWindow.theUKS;
            if (theUKS == null) return null;

            return theUKS.GetOrAddThing(newThing, "ClauseType");
        }

        //given general information output from GPT, parse it into UKS
        public static void ParseGPTOutput(string textIn, string GPTOutput)
        {
            // Get the UKS
            UKS.UKS theUKS = MainWindow.theUKS;
            //GetUKS();
            // Split by comma (,) to get individual pairs
            string[] valuePairs = GPTOutput.Split(';', '\n');
            // Error check
            if (valuePairs.Length == 0)
            {
                Debug.WriteLine($"Error, length of value '{Output}' is 0.");
                ModuleGPTInfoDlg.errorCount += 1;
            }

            textIn = GPT.Singularize(textIn);

            foreach (string s in valuePairs)
            {
                string[] nameValuePair = s.Split("|");
                if (nameValuePair.Length == 2)
                {
                    string valueType = nameValuePair[0].Trim();
                    string valueString = nameValuePair[1].Trim();
                    List<string> values = valueString.Split(',').ToList();
                    for (int i = 0; i < values.Count; i++) values[i] = GPT.Singularize(values[i]);

                    for (int j = 0; j < values.Count; j++)
                    {
                        valueType = nameValuePair[0].Trim();
                        List<string> valueTypeAttributes = new();
                        List<string> valueProperties = new();
                        string value = values[j].Trim();
                        if (value.Contains("-")) continue; //gets rid of hyphenated numbers
                        if (value.Contains('(') || value.Contains(')') || value.Contains(".") || value.Contains("\""))
                        {
                            continue;
                        }
                        string count = "";

                        //capture compound verbs based on "can"
                        List<string> subValues = value.Split(" ").ToList();
                        if (subValues.Count > 2) continue;
                        if (valueType == "can" && subValues.Count > 1)
                        {
                            valueTypeAttributes.Add("."+subValues[0]);
                            subValues.RemoveAt(0);
                        }

                        //parse out multi-word values
                        for (int i = 0; i < subValues.Count; i++)
                        {
                            string subValue = subValues[i].Trim();

                            //ignore extraneous articles
                            if (subValue == "a" || subValue == "an")
                            {
                                subValues.RemoveAt(i);
                                i--;
                            }

                            //trap numbers...sometimes GPT writes them out in text
                            List<string> digitWords = new() { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten" };
                            int index = digitWords.IndexOf(subValue);
                            if (index != -1)
                            {
                                count = index.ToString();
                                subValues.RemoveAt(i);
                                i--;
                            }
                            else if (int.TryParse(subValues[0], out int iCount))
                            {
                                count = iCount.ToString();
                                subValues.RemoveAt(i);
                                i--;
                            }
                            if (new List<string> { "some", "many", "lots", "few" }.Contains(subValue))
                            {
                                count = subValue;
                                subValues.RemoveAt(i);
                                i--;
                            }
                            //turn all extra values into properties
                            while (subValues.Count > 1)
                            {
                                valueProperties.Add("."+subValues[0]);
                                subValues.RemoveAt(0);
                            }
                        }

                        //write the valueType|value  to the UKS
                        if (subValues.Count > 0)
                            value = string.Join(" ", subValues);
                        else
                            continue;
                        if (value == textIn) continue;
                        if (valueType.StartsWith("examples"))
                        {
                            theUKS.AddStatement("." + value, "is-a", "." + textIn); //note reversal
                            ModuleGPTInfoDlg.relationshipCount += 1;
                        }
                        else if (valueType.StartsWith("is-part-of-speech"))
                        {
                            theUKS.AddStatement("." + textIn, "is-a", "." + value);
                            ModuleGPTInfoDlg.relationshipCount += 1;
                        }
                        else if (valueType.StartsWith("has-properties"))
                        {
                            theUKS.AddStatement("." + textIn, "is", "." + value, null, null, valueProperties);
                        }
                        else if (valueType.Contains("contains"))
                        {
                            Relationship r;
                            if (count == "" || count == "1")
                                r = theUKS.AddStatement("." + textIn, "has", "." + value, null, null, valueProperties);
                            else
                                r = theUKS.AddStatement("." + textIn, "has", "." + value, null, count, valueProperties);
                            ModuleGPTInfoDlg.relationshipCount += 1;
                            if (valueType.Contains("usually"))
                                r.Weight = .75f;
                        }
                        else
                        {
                            theUKS.AddStatement("." + textIn, valueType, "." + value, null, valueTypeAttributes, valueProperties);
                            ModuleGPTInfoDlg.relationshipCount += 1;
                        }
                        ///////   null reltypes? This was a safety check
                        ///
                        foreach (Thing t in theUKS.UKSList)
                            foreach (Relationship r in t.Relationships)
                                if (r.reltype == null)
                                {
                                    t.RemoveRelationship(r);
                                }

                    }
                }
            }
        }

        public static async Task SolveDuplicates()
        {
            // Get the UKS.
            UKS.UKS theUKS = MainWindow.theUKS;
            List<Thing> thingsToRemove = new List<Thing>();
            // Get all the children of Word and remove duplicates.
            foreach (Thing word in theUKS.GetOrAddThing("Word").Children)
            {
                // Find unique parents to remove duplicates
                List<Thing> uniqueParents = new List<Thing>();
                foreach (Relationship meaning in word.Relationships)
                {
                    // Find the parents of each target in the realtionship.
                    foreach (Thing parent in meaning.target.Parents)
                    {
                        // Continue if the parent is the word itself, (.rock and rock), instead of an actual abstract parent.
                        if ("." + parent.Label == meaning.source.Label)
                        {
                            continue;
                        }
                        // If the parent already exists, remove it.
                        if (uniqueParents.Contains(parent))
                        {
                            thingsToRemove.Add(meaning.target);
                        }
                        // Else if the parent does not exist, add it.
                        else
                        {
                            uniqueParents.Add(parent);
                        }
                    }
                }

            }

            // Remove the duplicate things at the end.
            foreach (Thing t in thingsToRemove)
            {
                theUKS.DeleteThing(t);
                ModuleGPTInfoDlg.relationshipCount++;
            }

        }

    }
}