//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Catalyst;
using Mosaik.Core;
using Pluralize.NET;

namespace BrainSimulator.Modules
{
    public class ModuleSpeechParser : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XlmIgnore] 
        //public theStatus = 1;
        private dynamic nlp;
        private List<String> Verb;
        private List<String> Relation;
        private List<String> Punctuation = new List<String> { "?" };
        private ProcessStartInfo start;
        Process process = new ();
        List<Dictionary<string, string>> nlpResults = new();
        bool nlpComplete = false;

        List<Thing> phraseWords = new();


        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleSpeechParser()
        {
            minHeight = 1;
            maxHeight = 10;
            minWidth = 1;
            maxWidth = 10;
        }


        //fill this method in with code which will execute
        //once for each cycle of the engine

        public override void Fire()
        {
            Init();  //be sure to leave this here

            //if you want the dlg to update, use the following code whenever any parameter changes
            // UpdateDialog();
            GetUKS();
            if (UKS == null) return;
            Thing attn = UKS.GetOrAddThing("Attention", "Thing");
            if (attn == null) return;
            Thing currentWord = UKS.GetOrAddThing("CurrentWord", attn);

            // Remove reference to current phrase
            // Maybe this would be handled by whoever processes phrases in future.
            Thing attnPhrase = UKS.GetOrAddThing("CurrentPhrase", attn);
            //attnPhrase.RemoveReferenceAt(0);

            if (currentWord.Relationships.Count > 0 &&
                (currentWord.Relationships[0].T as Thing).V.ToString() != "endofphrase") // Continue building phrase while currentWord has reference
            {
                Thing newWord = (currentWord.Relationships[0].T as Thing);
                phraseWords.Add(newWord);
            }
            else // No new phrase or phrase is complete
            {
                if (phraseWords.Count == 0) return; // no new phrase

                Thing currentPhrase = new();

                this.phraseWords.ForEach(thingWord => currentPhrase.AddRelationship(thingWord));

                ParsePhrase(currentPhrase);
                // Clear phraseWords.
                phraseWords.Clear();
            }
        }

        // This parses a phrase, first part of this is hack for movement commands.
        // Afterwards turning the phrase, into a parameterized phrase.
        private void ParsePhrase(Thing currentPhrase)
        {
            Thing parsedPhrase = UKS.GetOrAddThing("ph*", "CurrentPhrase");
            int number;

            List<Thing> paramList = new();
            List<Thing> ReferenceThing = currentPhrase.RelationshipsAsThings.ToList();
            currentPhrase.RemoveRelationship(UKS.Labeled("w?"));

            // movement hack
            ModulePodInterface mpi = (ModulePodInterface)FindModule("PodInterface");
            if (ReferenceThing.Count == 2 && mpi != null && (
                ReferenceThing[0].V.ToString() == "move" ||
                ReferenceThing[0].V.ToString() == "moved" ||
                ReferenceThing[0].V.ToString() == "go"))
            {
                switch (ReferenceThing[1].V)
                {
                    case "ahead":
                    case "forward":
                    case "forwards":
                        {
                            mpi.CommandMove(1000, true);
                            break;
                        }
                    case "back":
                    case "backwards":
                        {
                            mpi.CommandMove(-1000, true);
                            break;
                        }
                }
                return;
            }

            if (ReferenceThing.Count == 2 && mpi != null && (
                ReferenceThing[0].V.ToString() == "look"))
            {
                switch (ReferenceThing[1].V)
                {
                    case "left":
                        {
                            mpi.CommandPan(Angle.FromDegrees(45), true);
                            break;
                        }
                    case "right": 
                        {
                            mpi.CommandPan(Angle.FromDegrees(-45), true);
                            break;
                        }
                    case "forward":
                        {
                            mpi.CommandPan(Angle.FromDegrees(0));
                            mpi.CommandTilt(Angle.FromDegrees(0));
                            break;
                        }
                    case "up":
                        {
                            mpi.CommandTilt(Angle.FromDegrees(10), true);
                            break;
                        }
                    case "down":
                        {
                            mpi.CommandTilt(Angle.FromDegrees(-10), true);
                            break;
                        }
                    case "around":
                        {
                            Thing command = UKS.GetOrAddThing("TurnAround", UKS.Labeled("Attention"));
                            break;
                        }
                }
                return;
            }

            if (ReferenceThing.Count >= 3 && mpi != null && (
                ReferenceThing[0].V.ToString() == "move" ||
                ReferenceThing[0].V.ToString() == "moved" ||
                ReferenceThing[0].V.ToString() == "turn" ||
                ReferenceThing[0].V.ToString() == "turned" ||
                (ReferenceThing[0].V.ToString() == "go" &&
                ReferenceThing[1].V.ToString() != "to")))
            {
                switch (ReferenceThing[1].V)
                {
                    case "ahead":
                    case "forward":
                    case "forwards":
                        {
                            if (float.TryParse(ReferenceThing[2].V.ToString(), out float value))
                            {
                                mpi.CommandMove(value);
                            }
                            break;
                        }
                    case "back":
                    case "backwards":
                        {
                            if (float.TryParse(ReferenceThing[2].V.ToString(), out float value))
                            {
                                mpi.CommandMove(-value);
                            }
                            break;
                        }
                    case "left":
                        {
                            if (float.TryParse(ReferenceThing[2].V.ToString(), out float value))
                            {
                                mpi.CommandTurn(Angle.FromDegrees(-value));
                            }
                            break;
                        }
                    case "right":
                        {
                            if (float.TryParse(ReferenceThing[2].V.ToString(), out float value))
                            {
                                mpi.CommandTurn(Angle.FromDegrees(value));
                            }
                            break;
                        }
                }
                return;
            }

            if (ReferenceThing.Count >= 3 && mpi != null && (
                ReferenceThing[0].V.ToString() == "look"))
            {
                Angle currentPan = mpi.GetCameraPan();
                switch (ReferenceThing[1].V)
                {
                    case "left":
                        {
                            if (float.TryParse(ReferenceThing[2].V.ToString(), out float value))
                            {
                                mpi.CommandPan(Angle.FromDegrees(value), true);
                            }
                            break;
                        }
                    case "right":
                        {
                            if (float.TryParse(ReferenceThing[2].V.ToString(), out float value))
                            {
                                mpi.CommandPan(Angle.FromDegrees(-value), true);
                            }
                            break;
                        }
                    case "up":
                        {
                            if (float.TryParse(ReferenceThing[2].V.ToString(), out float value))
                            {
                                mpi.CommandTilt(Angle.FromDegrees(value), true);
                            }
                            break;
                        }
                    case "down":
                        {
                            if (float.TryParse(ReferenceThing[2].V.ToString(), out float value))
                            {
                                mpi.CommandTilt(Angle.FromDegrees(-value), true);
                            }
                            break;
                        }
                }
                return;
            }
            // End of movement hack.

            // Getting NLP results. 
            string phrase = "";
            foreach (Thing t in ReferenceThing) phrase += t.V + " ";
            nlpResults.Clear();
            nlpComplete = false;
            process.StandardInput.WriteLine(phrase);
            while (!nlpComplete) 
            { 
                if ( process.HasExited )
                {
                    Debug.WriteLine("Something went wrong in Python Land. Unable to process text.");
                    return;
                }
            }

            //foreach ( Dictionary<string, string> strs in nlpResults )
            //{
            //    foreach ((string key, string value) in strs)
            //    {
            //        Debug.Write(key + " " + value + " \t ");
            //    }
            //    Debug.WriteLine("");
            //}

            for (int i = 0; i < ReferenceThing.Count; i++)
            {
                if (ReferenceThing[i].V.Equals("this"))
                {
                    //this is a basketball
                    Thing attentionObject = UKS.GetOrAddThing("ObjectReference*", "paramObjectReference");
                    attentionObject.V = "AttentionObject";
                    attentionObject.AddRelationship(ReferenceThing[i]);
                    parsedPhrase.AddRelationship(attentionObject);
                }
                else if (i == 0 && ReferenceThing[i].V.Equals("say"))
                {
                    //repeats back remainder of phrase
                    parsedPhrase.AddRelationship(ReferenceThing[i]);
                    Thing phraseThing = UKS.GetOrAddThing("phrase*", "paramPhrase");
                    for (i = 1; i < ReferenceThing.Count; i++)
                    {
                        phraseThing.AddRelationship(ReferenceThing[i]);
                    }
                    parsedPhrase.AddRelationship(phraseThing);
                }
                else if (ReferenceThing[i].V.Equals("replace"))
                {
                    //to build table of text replacement
                    parsedPhrase.AddRelationship(ReferenceThing[i]);
                    Thing replacementParam1 = UKS.GetOrAddThing("replacement*", "paramReplacement");
                    while (++i < ReferenceThing.Count && !ReferenceThing[i].V.Equals("with"))
                    {
                        replacementParam1.AddRelationship(ReferenceThing[i]);
                    }
                    parsedPhrase.AddRelationship(replacementParam1);
                    parsedPhrase.AddRelationship(ReferenceThing[i]);
                    Thing replacementParam2 = UKS.GetOrAddThing("replacement*", "paramReplacement");
                    while (++i < ReferenceThing.Count)
                    {
                        replacementParam2.AddRelationship(ReferenceThing[i]);
                    }
                    parsedPhrase.AddRelationship(replacementParam2);
                }
                else if (Relation.Contains(ReferenceThing[i].V))
                {
                    //builds relations (left/right etc.)
                    Thing direction = UKS.GetOrAddThing("rel*", "paramRelation");
                    direction.AddRelationship(ReferenceThing[i]);

                    parsedPhrase.AddRelationship(direction);
                }
                else if (UKS.Labeled("WakeWord").Children.FindFirst(t => t.Label.ToLower() == ReferenceThing[i].V.ToString()) != null)
                {
                    //change the wake word
                    Thing wakeword = UKS.GetOrAddThing("paramWakeword", "Parameter");
                    string name = ReferenceThing[i].V.ToString();
                    name = char.ToUpper(name[0]) + name.Substring(1);
                    Thing wakeWord = UKS.GetOrAddThing(name, wakeword);
                    parsedPhrase.AddRelationship(wakeWord);
                }
                else if (nlpResults[i]["POS"] == "AUX" || nlpResults[i]["POS"] == "VERB")
                {
                    //POS - part of speech
                    IPluralize pluralizer = new Pluralizer();
                    Thing verbObject = UKS.GetOrAddThing("Verb*", "paramVerb");
                    if (verbObject == null) continue;
                    Dictionary<string, string> nlpForCurrentWord = nlpResults[i];
                    
                    verbObject.V = pluralizer.Singularize(ReferenceThing[i].V.ToString());
                    ReferenceThing[i].Label = "w" + verbObject.V.ToString()[0..1].ToUpper() + verbObject.V.ToString()[1..];
                    ReferenceThing[i].V = verbObject.V;

                    verbObject.AddRelationship(ReferenceThing[i]);
                    parsedPhrase.AddRelationship(verbObject);
                }
                // This should mostly be handling noun phrases starting with a/an
                // Also includes compound nouns such as "Eiffel Tower"
                else if (ReferenceThing[i].V.Equals("a") || ReferenceThing[i].V.Equals("an") ||
                    nlpResults[i]["DEP"] == "compound")
                {
                    // This well help keep "is a" and "is" statements distinct
                    // Should only add an an following the word "is"
                    if ((ReferenceThing[i].V.Equals("a") || ReferenceThing[i].V.Equals("an")) &&
                        i-1 >= 0 &&
                        ReferenceThing[i-1].V.Equals("is"))
                    {
                        parsedPhrase.AddRelationship(UKS.Labeled("wA"));
                    }
                    Thing propertyList = UKS.GetOrAddThing("ObjectReference*", "paramObjectReference");
                    propertyList.V = "ObjectProperties";

                    int head = int.Parse(nlpResults[i]["HEAD"]);

                    // Adds the rest of the noun phrase.
                    while (++i <= head)
                    {
                        propertyList.AddRelationship(ReferenceThing[i]);
                    }
                    i--; // Went to far, want to step back.
                    if (propertyList.Relationships.Count == 0)
                        return;
                    parsedPhrase.AddRelationship(propertyList);
                }
                // Stand alone nouns, or adjectival compliments.
                else if (nlpResults[i]["POS"] == "NOUN" ||
                    nlpResults[i]["POS"] == "PROPN" ||
                    (nlpResults[i]["DEP"] == "acomp" &&
                    nlpResults[int.Parse(nlpResults[i]["HEAD"])]["DEP"] == "ROOT"))
                {
                    Thing propertyList = UKS.GetOrAddThing("ObjectReference*", "paramObjectReference");
                    propertyList.V = "ObjectProperties";
                    Dictionary<string, string> nlpForCurrentWord = nlpResults[i];
                    if (nlpForCurrentWord["TEXT"] != nlpForCurrentWord["LEMMA"])
                    {
                        if ((i-1 >= 0 &&
                            ReferenceThing[i-1].V.Equals("is")) &&
                            (i+1 < ReferenceThing.Count &&
                            (!ReferenceThing[i+1].V.Equals("with") &&
                            !ReferenceThing[i+1].V.Equals("than"))))
                        {
                            parsedPhrase.AddRelationship(UKS.Labeled("wA"));
                        }
                        ReferenceThing[i].Label = "w" + nlpForCurrentWord["LEMMA"][0..1].ToUpper() + nlpForCurrentWord["LEMMA"][1..];
                        ReferenceThing[i].V = nlpForCurrentWord["LEMMA"];
                    }
                    propertyList.AddRelationship(ReferenceThing[i]);
                    parsedPhrase.AddRelationship(propertyList);
                }
                else if (ReferenceThing[i].V.Equals("you"))
                {
                    Thing attentionObject = UKS.GetOrAddThing("ObjectReference*", "paramObjectReference");
                    attentionObject.V = "Self";
                    attentionObject.AddRelationship(ReferenceThing[i]);
                    parsedPhrase.AddRelationship(attentionObject);
                }
                else if (Punctuation.Contains(ReferenceThing[i].V))
                {
                    Thing punctuation = new();
                    punctuation.Label = "Punctuation";
                    punctuation.AddRelationship(ReferenceThing[i]);

                    //parsedPhrase.AddReference(punctuation);
                }                
                else if (ReferenceThing[i].V.Equals("at"))
                {
                    parsedPhrase.AddRelationship(ReferenceThing[i]);
                    Thing lmParam = UKS.GetOrAddThing("lmParam*", "paramLandmark");
                    while (++i < ReferenceThing.Count)
                    {
                        lmParam.AddRelationship(ReferenceThing[i]);
                    }
                    parsedPhrase.AddRelationship(lmParam);
                }
                else if (ReferenceThing[i].V.Equals("to"))
                {
                    parsedPhrase.AddRelationship(ReferenceThing[i]);
                    if (ReferenceThing.Count >= i + 1) continue;
                    if ( ReferenceThing[i+1].V.Equals("a")) continue;
                    Thing lmParam = UKS.GetOrAddThing("lmParam*", "paramLandmark");
                    while (++i < ReferenceThing.Count)
                    {
                        lmParam.AddRelationship(ReferenceThing[i]);
                    }
                    parsedPhrase.AddRelationship(lmParam);
                }
                else if (ReferenceThing[i].V.Equals("the") && ReferenceThing.Count > i + 2 && 
                    ReferenceThing[i+1].V.Equals("color") &&
                    ! ReferenceThing[i + 2].V.Equals("of"))
                {
                    i += 2;
                    Thing colorReference = UKS.GetOrAddThing("colorReference*", "paramColorReference");
                    for ( int j = i+2; j < ReferenceThing.Count; j++ )
                    {
                        colorReference.AddRelationship(ReferenceThing[j]);
                        i++;
                    }
                    parsedPhrase.AddRelationship(colorReference);
                }
                else if ( int.TryParse(ReferenceThing[i].V.ToString(), out number))
                {
                    Thing numberThing = UKS.GetOrAddThing("number*", "paramNumber");
                    numberThing.V = number;
                    parsedPhrase.AddRelationship(numberThing);
                }
                else
                {
                    
                    Thing type = WordType(ReferenceThing[i]);
                    if (type.Label != "Word")
                    {
                        Thing parameter = UKS.GetOrAddThing("param" + type.Label, "Parameter");
                        Thing t = UKS.GetOrAddThing(parameter.Label + "*", parameter);
                        t.Label = type.Label;
                        t.AddRelationship(ReferenceThing[i]);
                        parsedPhrase.AddRelationship(t);
                        Thing T = t.RelationshipsAsThings.First();
                        if (T.Relationships != null)
                        {
                            Thing obj = T.RelationshipsAsThings.First();
                            obj.SetFired();
                        }
                    }
                    else parsedPhrase.AddRelationship(ReferenceThing[i]);
                }
            }
        }

        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Debug.WriteLine("ModuleSpeechParser - NLP Process Error: " + e.Data);
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            //Debug.WriteLine(e.Data);
            if (e.Data == null) return;
            if (e.Data == "Process Complete")
            {
                nlpComplete = true;
            }
            else
            {
                // "TEXT", "LEMMA", "DEPENDENCY", "PART OF SPEECH", "TAG", "HEAD", "MORPH"
                string[] results = e.Data.Split("\t");
                Dictionary<string, string> keyValuePairs = new Dictionary<string, string>();
                keyValuePairs.Add("TEXT", results[0]);
                keyValuePairs.Add("LEMMA", results[1]);
                keyValuePairs.Add("DEP", results[2]);
                keyValuePairs.Add("POS", results[3]);
                keyValuePairs.Add("TAG", results[4]);
                keyValuePairs.Add("HEAD", results[5]);
                keyValuePairs.Add("MORPH", results[6]);
                nlpResults.Add(keyValuePairs);
            }
        }

        private Thing WordType(Thing word)
        {
            IList<Thing> references = word.RelationshipsAsThings;

            if (references.Count > 0)
            {
                Thing t = references[0];
                while (t.Parents[0].Label != "Thing")
                {
                    t = t.Parents[0];
                }
                return t;
            }

            return UKS.Labeled("Word");
        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
        }

        //the following can be used to massage public data to be different in the xml file
        //delete if not needed
        public override void SetUpBeforeSave()
        {
        }

        public override void SetUpAfterLoad()
        {
            UpdateVerbList();
            UpdateRelationList();


            string path = Utils.GetOrAddLocalSubFolder("python-3.10.8");
            string cmd = path + @"\nlp_processing.py";

            start = new ProcessStartInfo();
            start.FileName = path + @"\python.exe";   
            start.UseShellExecute = false;// Do not use OS shell
            start.CreateNoWindow = true; // We don't need new window
            start.RedirectStandardInput = true;
            start.RedirectStandardOutput = true;// Any output, generated by application will be redirected back
            start.RedirectStandardError = true; // Any error in standard output will be redirected back (for example exceptions)
            start.Arguments = string.Format("\"{0}\" \"{1}\"", cmd, "");
            process.OutputDataReceived+=Process_OutputDataReceived;
            process.ErrorDataReceived+=Process_ErrorDataReceived;
            process.StartInfo = start;
            process.Start();
            process.BeginOutputReadLine();
        }

        //called whenever the size of the module rectangle changes
        //for example, you may choose to reinitialize whenever size changes
        //delete if not needed
        public override void SizeChanged()
        {
            if (mv == null) return; //this is called the first time before the module actually exists
        }

        public override void UKSInitializedNotification()
        {
            MainWindow.SuspendEngine();
            GetUKS();

            UKS.GetOrAddThing("Verbs", "Audible");
            UKS.GetOrAddThing("vIs", UKS.Labeled("Verbs"), "is");
            UKS.GetOrAddThing("vIsA", UKS.Labeled("Verbs"), "is a");
            UKS.GetOrAddThing("vCan", UKS.Labeled("Verbs"), "can");
            UKS.GetOrAddThing("vHas", UKS.Labeled("Verbs"), "has");

            UKS.GetOrAddThing("Parameter", "Phrase");
            UKS.GetOrAddThing("paramRelation", "Parameter");
            UKS.GetOrAddThing("relFront", UKS.Labeled("paramRelation"), "front");
            UKS.GetOrAddThing("relBehind", UKS.Labeled("paramRelation"), "behind");
            UKS.GetOrAddThing("relLeft", UKS.Labeled("paramRelation"), "left");
            UKS.GetOrAddThing("relRight", UKS.Labeled("paramRelation"), "right");
            UKS.GetOrAddThing("relUp", UKS.Labeled("paramRelation"), "up");
            UKS.GetOrAddThing("relDown", UKS.Labeled("paramRelation"), "down");
            UKS.GetOrAddThing("relForward", UKS.Labeled("paramRelation"), "forward");
            UKS.GetOrAddThing("paramReplacement", "Parameter");

            UpdateVerbList();
            UpdateRelationList();

            MainWindow.ResumeEngine();
        }

        private void UpdateVerbList()
        {
            GetUKS();
            Verb = new();
            Thing t = UKS.Labeled("Verbs");
            IList<Thing> verbs = t.Children;
            foreach (Thing t1 in verbs)
            {
                Verb.Add(t1.V.ToString());
            }
        }

        private void UpdateRelationList()
        {
            GetUKS();
            Relation = new();
            Thing t1 = UKS.Labeled("paramRelation");
            IList<Thing> relations = t1.Children;
            foreach (Thing t in relations)
            {
                if (t.V != null) Relation.Add(t.V.ToString());
            }
        }
    }
}