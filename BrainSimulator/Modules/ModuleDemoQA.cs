using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UKS;

namespace BrainSimulator.Modules
{

    public class ModuleDemoQA : ModuleBase
    {
        public ModuleDemoQA()
        {

        }

        public override void Fire()
        {
            Init();  //be sure to leave this here

            //if you want the dlg to update, use the following code whenever any parameter changes
            UpdateDialog();
        }

        public override void Initialize()
        {

        }

        /// <summary>
        /// Function to turn natural language sentences into UKS statements.
        /// Copied from ModuleGPTInfo, TODO: Place both functions in a single location.
        /// </summary>
        public static async Task NaturalToUKS(string userInput)
        {
            if (userInput == null || userInput.Length == 0)
                Debug.WriteLine("# UKS: (no input)\n");

            string apiKey = ConfigurationManager.AppSettings["APIKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
                Debug.WriteLine("# UKS: OPENAI_API_KEY not set; cannot call ChatGPT.\n");

            var model = ConfigurationManager.AppSettings["OPENAI_MODEL"];
            if (string.IsNullOrWhiteSpace(model)) model = "gpt-4o-mini";

            // Build request
            var systemText = @"You extract knowledge from user-provided sentences.

Examples:
Input:
Fido is a dog that talks.
Return:
Fido is-a dog,Fido can talk

Input:
Brazil is a country. Countries have capitals.
Return: 
Brazil is-a country,country has capital

Input:
Jenny can walk. She enjoys walking.
Return:
Jenny can walk,Jenny enjoy walks

Input:
Fido can play outside if the weather is sunny. Fido likes to play. Dinosuars are extinct animals.
Return:
Fido can play IF weather is sunny,Fido likes playing,dinosaurs are extinct,dinosaurs is-a animal

Input:
Mary is at the store with George. They are buying biscuits.
Return:
Mary at store WITH George at store,Mary buys biscuit,George buy biscuit



Rules:
- OUTPUT ONLY DECLARATIVE STATEMENTS that assert a fact or relationship.
- There are two types of statements, regular ones and clauses that connect two relationships with a relationship.
- All regular statements have 3 items in them, the format is as follows: origin relationship target
- All clause statements have 7 items in them, the format is as follows: origin_1 relationship_1 target_1 CLAUSE origin_2 relationship_2 target_2
- IGNORE questions, commands, requests, opinions, jokes, or hypotheticals.
- Canonicalize: strip leading articles, remove trailing punctuation, title-case common nouns (keep ALLCAPS and numbers).
- Be faithful to the text; don't infer unspecified facts.
- If no valid statements exist, return nothing";


            string gptResults = await GPT.RunTextAsync(userInput, systemText);

            Debug.WriteLine("RESULTS: " + gptResults);

            string[] seperatedResults = gptResults.Split(',');

            foreach (string seperator in seperatedResults)
            {
                string[] items = seperator.Split(" ");

                // Basic Statements
                if (items.Length == 3)
                {
                    Debug.WriteLine("Statement: " + seperator);
                    Relationship r = MainWindow.theUKS.AddStatement(items[0], items[1], items[2]);
                    // Debug.WriteLine("Rel is " + r);
                    if (r == null)
                    {
                        Debug.WriteLine($"Relationship: {r} in natural to UKS is null!");
                    }
                }
                // Clauses
                else if (items.Length == 7)
                {
                    Debug.WriteLine("Clause: " + seperator);

                    // Setting up the values from GPT
                    string newThing = items[0].Trim();
                    string relationType = items[1].Trim();
                    string targetThing = items[2].Trim();

                    string clauseType = items[3].Trim();

                    string newThing2 = items[4].Trim();
                    string relationType2 = items[5].Trim();
                    string targetThing2 = items[6].Trim();

                    Relationship r = MainWindow.theUKS.AddStatement(newThing, relationType, targetThing, clauseType, newThing2, targetThing2);

                    if (r == null)
                    {
                        Debug.WriteLine($"Relationship: {r} in natural to UKS is null!");
                    }
                }
                else
                {
                    Debug.WriteLine($"Error! Statement '{seperator}' does not have 3 nor 7 items! (Neither statement nor clause).");
                }
            }

        }
    }
}
