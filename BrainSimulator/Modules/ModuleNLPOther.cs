//
// Copyright (c) FutureAI. All rights reserved.  
// Contains confidential and  proprietary information and programs which may not be distributed without a separate license
//  

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using static System.Math;

namespace BrainSimulator.Modules
{
    public partial class ModuleNLP : ModuleBase
    {

        bool HandleHardCodedPhrases(string phrase, string desiredOutput)
        {
            string phrase1 = RemovePunctuation(phrase);
            string[] words = phrase1.Split(" ");

            ModuleObject mo = (ModuleObject)FindModule("Object");
            if (words.Length == 1 && words[0] == "yes")
            {
                Relationship r2 = mo.DoPendingRelationship();
                if (r2 != null)
                {
                    r2.weight = 1;
                    r2.inferred = false;
                }
                return true;
            }
            if (words.Length == 1 && words[0] == "no")
            {
                mo.DoPendingRelationship(false);
                ResultsRemoveAt(nlpResults, 0);
                return true;
            }
            Relationship pending = mo.GetPendingRelationship();
            if (pending != null && pending.source == null)
            {
                HandleKnownParseErrors(nlpResults);
                List<ClauseType> clauses = new();
                NLPItem relType = HandleTypeD(nlpResults, out List<string> typeProperties, clauses);
                //if this is just a declaration of a single noun...
                if (relType.dependency == "ROOT" && relType.partOfSpeech == "NOUN" && nlpResults[relType.index + 1].lemma == ".")
                {
                    UKS.AddStatement(relType.lemma, pending.relType, pending.target, typeProperties);
                    mo.DoPendingRelationship(false); //cancel the relationship
                    return true;
                }
            }

            if (words[0] == "say")
            {
                OutputReponseString(phrase.Substring(4)); //use phrase here to let puncutation help the speech system
                return true;
            }

            if (words[0] == "name" || words[0] == "list")
            {
                List<ClauseType> clauses = new();
                relPos= 0;
                NLPItem target = HandleSource(nlpResults, out List<string> targetProperties, clauses);
                List<Thing> propertyList = new();
                propertyList.Add(UKS.Labeled(target?.lemma));
                Thing targetThing = UKS.Labeled(target.lemma);
                foreach (string s in targetProperties)
                    propertyList.Add(UKS.Labeled(s));
                if (propertyList.Count > 1)
                {
                    var resultsThings = UKS.GeneralQuery(propertyList);
                    if (resultsThings.Count > 0)
                        targetThing = resultsThings[0];
                }
                string responseString = ListChildren(targetThing, 10);
                OutputReponseString(responseString);
                CompareOutputToDesired(responseString, desiredOutput);
                return true;
            }

            // movement 
            ModulePodInterface mpi = (ModulePodInterface)FindModule("PodInterface");
            if (mpi == null) return false;
            if (words.Length == 2 && mpi != null && (
                words[0] == "look"))
            {
                switch (words[1])
                {
                    case "left": mpi.CommandPan(Angle.FromDegrees(45), true); break;
                    case "right": mpi.CommandPan(Angle.FromDegrees(-45), true); break;
                    case "forward": mpi.CommandPan(Angle.FromDegrees(0)); mpi.CommandTilt(Angle.FromDegrees(0)); break;
                    case "up": mpi.CommandTilt(Angle.FromDegrees(10), true); break;
                    case "down": mpi.CommandTilt(Angle.FromDegrees(-10), true); break;
                    case "around": Thing command = UKS.GetOrAddThing("TurnAround", UKS.Labeled("Attention")); break;
                }
                return true;
            }

            if (words.Length > 2 &&
                (words[0] == "moved" ||
                words[0] == "move" ||
                words[0] == "turn" ||
                words[0] == "turned" ||
                words[0] == "go"))
            {
                switch (words[1])
                {
                    case "ahead":
                    case "forward":
                    case "forwards":
                        if (float.TryParse(words[2], out float value0))
                            mpi.CommandMove(value0);
                        break;
                    case "back":
                    case "backwards":
                        if (float.TryParse(words[2], out float value1))
                            mpi.CommandMove(-value1);
                        break;
                    case "left":
                        if (float.TryParse(words[2], out float value2))
                        {
                            mpi.CommandTurn(Angle.FromDegrees(-value2));
                        }
                        break;
                    case "right":
                        if (float.TryParse(words[2], out float value3))
                        {
                            mpi.CommandTurn(Angle.FromDegrees(value3));
                        }
                        break;
                }
                return true;
            }
            if (words[0] == "stop")
            {
                mpi.CommandStop();
                ModuleHappy happy = (ModuleHappy)FindModule(typeof(ModuleHappy));
                if (happy != null)
                    happy.decreaseHappiness();
            }
            if (words[0] == "what" && words[1] == "is")
            {
                ModuleQuery mq = (ModuleQuery)FindModule("Query");
                if (mq != null)
                {
                    Thing relativeTo = new Thing { V = "Self" };
                    Thing relation = new Thing();

                    if (phrase1.Contains("behind you"))
                        relation.AddRelationship(new Thing { V = "behind" });
                    else if (phrase1.Contains("to your left"))
                        relation.AddRelationship(new Thing { V = "left" });
                    else if (phrase1.Contains("to your right"))
                        relation.AddRelationship(new Thing { V = "right" });
                    else if (phrase1.Contains("in front of you"))
                        relation.AddRelationship(new Thing { V = "front" });
                    //TODO: the underlying spatial-relationship creaation needed for this feature doesn't seem to exist
                    //else if (phrase.Contains("to the left of"))
                    //{
                    //    relativeTo.Label = "Object";
                    //    relativeTo.V = null;
                    //    Thing targetObject = UKS.Labeled(words.Last());
                    //    if (targetObject != null)
                    //    {
                    //        //get the physical object
                    //        Thing targetPysicalObject = targetObject.Children.FindFirst(x => x.HasAncestorLabeled("MentalModel"));
                    //        if (targetPysicalObject != null)
                    //            relativeTo.AddRelationship(new Thing { V = targetPysicalObject.Label });
                    //        relation.AddRelationship(new Thing { V = "left" });
                    //    }
                    //}
                    else
                        goto noPhraseMatch;
                    Thing t = mq.findObjectByRelation(relation, relativeTo);
                    if (t.V.ToString() == "NoRelation")
                        OutputReponseString("I don't know what is behind me");
                    else
                        OutputReponseString("That is a " + t.V.ToString());
                    return true;
                noPhraseMatch:;
                }
            }
            if (words[0] == "good")
            {
                ModuleHappy happy = (ModuleHappy)FindModule(typeof(ModuleHappy));
                ModulePodAudio modulePodAudio = (ModulePodAudio)FindModule(typeof(ModulePodAudio));
                if (happy != null)
                {
                    happy.increaseHappiness();
                    if (modulePodAudio != null)
                        modulePodAudio.PlaySoundEffect("SalliePositive1.wav");
                }
                return true;
            }
            if (words[0] == "wrong")
            {
                ModuleHappy happy = (ModuleHappy)FindModule(typeof(ModuleHappy));
                ModulePodAudio modulePodAudio = (ModulePodAudio)FindModule(typeof(ModulePodAudio));
                if (happy != null)
                {
                    happy.decreaseHappiness();
                    if (modulePodAudio != null)
                        modulePodAudio.PlaySoundEffect("SallieNegative1.wav");
                }
                return true;
            }
            if (phrase1.Contains("how do you feel"))
            {
                ModuleHappy happy = (ModuleHappy)FindModule(typeof(ModuleHappy));
                if (happy != null)
                {
                    Thing mood = new Thing();
                    mood.V = happy.getMood();
                    OutputReponseString("I feel " + mood.V.ToString());
                }
                return true;
            }
            if (words[0] == "imagine" || words[0] == "visualize")
            {
                HandleKnownParseErrors(nlpResults);
                HandleCompounds(nlpResults);
                List<ClauseType> clauses = new();
                NLPItem relType = HandleTypeD(nlpResults, out List<string> typeProperties, clauses);
                NLPItem source = HandleSourceD(nlpResults, out List<string> sourcePoperties, clauses);
                NLPItem target = HandleTargetD(nlpResults, out List<string> targetProperties, clauses);
                ModuleGraphics mg = (ModuleGraphics)FindModule("Graphics");
                string thingName = "";
                mg.SetStartingColor("");
                if (phrase.Contains("upside down")) mg.SetUpsideDown();
                if (phrase.Contains("backward")) mg.SetBackwards();
                if (phrase.Contains("above")) SetRelativeStartPosition(new Point3DPlus(0, 0, 10f));
                if (phrase.Contains("below")) SetRelativeStartPosition(new Point3DPlus(0, 0, -10f));
                if (phrase.Contains("right") || phrase.Contains("beside") || phrase.Contains("next to")) SetRelativeStartPosition(new Point3DPlus(0, -10, 0f));
                if (phrase.Contains("left")) SetRelativeStartPosition(new Point3DPlus(0, 10, 0f));
                if (phrase.Contains("beyond") || phrase.Contains("behind")) SetRelativeStartPosition(new Point3DPlus(10, 0, 0f));
                if (phrase.Contains("front")) SetRelativeStartPosition(new Point3DPlus(-10, 0, 0f));
                foreach (string s in sourcePoperties)
                {
                    //color
                    if (Utils.ColorFromName(s) != Utils.ColorFromName(null))
                        mg.SetStartingColor(s);
                    //size
                    if (s == "big") mg.SetStartingSize(new Point3DPlus(2, 2, 2f));
                    if (s == "little") mg.SetStartingSize(new Point3DPlus(.5f, .5f, .5f));
                }
                thingName = RemovePunctuation(source.lemma);
                if (!mg.ImagineThing(thingName))
                    OutputReponseString("I don't know what a " + thingName + " looks like.");
                return true;
            }


            //what is behind you (to the left,right,infrontof) also of any visible object
            //how many of a visible objects
            //add names to visible properties
            //tell me some things that are
            //items from queryResolution HandleAction

            return false;
        }

        private void SetRelativeStartPosition(Point3DPlus offset)
        {
            ModuleGraphics mg = (ModuleGraphics)FindModule("Graphics");
            NLPItem item = nlpResults.FindLast(x => x.dependency == "pobj" || x.partOfSpeech == "NOUN");
            if (item != null)
            {

                Thing target = UKS.Labeled(item.lemma);
                Thing physicalObject = target?.Children.FindFirst(x => x.Label.StartsWith("po"));
                ModuleMentalModel mm = (ModuleMentalModel)FindModule("MentalModel");
                if (physicalObject != null)
                {
                    Dictionary<string, object> properties = physicalObject.GetRelationshipsAsDictionary();
                    Point3DPlus center = (Point3DPlus)properties["cen"];
                    center += offset;
                    Point3DPlus center1 = new Point3DPlus(center.X, center.Y, center.Z);
                    mg.SetStartingPosition(center1);
                }
                else
                    mg.SetStartingPosition(offset);
            }
            else
                mg.SetStartingPosition(offset);
        }

        public void ChangeNumbersToDigits(List<NLPItem> nlpResults)
        {
            foreach(NLPItem item in nlpResults)
            {
                string s = ConvertSpelledOutNumberToInteger(item.lemma);
                if (s != "-1")
                {
                    item.lemma = s;
                }
            }
        }
        public  string ConvertSpelledOutNumberToInteger(string spelledOutNumber)
        {
            // Define a dictionary that maps spelled-out numbers to digits
            Dictionary<string, int> numberDictionary = new Dictionary<string, int>()
    {
        {"zero", 0},
        {"one", 1},
        {"two", 2},
        {"three", 3},
        {"four", 4},
        {"five", 5},
        {"six", 6},
        {"seven", 7},
        {"eight", 8},
        {"nine", 9},
        {"ten", 10},
        {"eleven", 11},
        {"twelve", 12},
        {"thirteen", 13},
        {"fourteen", 14},
        {"fifteen", 15},
        {"sixteen", 16},
        {"seventeen", 17},
        {"eighteen", 18},
        {"nineteen", 19},
        {"twenty", 20},
        {"thirty", 30},
        {"forty", 40},
        {"fifty", 50},
        {"sixty", 60},
        {"seventy", 70},
        {"eighty", 80},
        {"ninety", 90},
        {"hundred", 100},
        {"thousand", 1000},
    };

            // Split the spelled-out number into its individual parts
            string[] parts = spelledOutNumber.Split(' ');

            // Combine the digits from each part
            if (parts.Length == 1)
            {
                // Single-digit number
                if (numberDictionary.TryGetValue(parts[0], out int digit))
                {
                    return digit.ToString();
                }
                else
                {
                    return "-1";
                }
            }
            else if (parts.Length == 2)
            {
                // Two-digit number
                int tens, ones;
                if (numberDictionary.TryGetValue(parts[0], out tens) && numberDictionary.TryGetValue(parts[1], out ones))
                {
                    return (tens + ones).ToString();
                }
                else
                {
                    return "-1";
                }
            }
            else if (parts.Length == 3)
            {
                // Three-digit number
                int hundreds, tens, ones;
                if (numberDictionary.TryGetValue(parts[0], out hundreds) && hundreds == 100 && numberDictionary.TryGetValue(parts[1], out tens) && numberDictionary.TryGetValue(parts[2], out ones))
                {
                    return (hundreds * tens + ones).ToString();
                }
                else
                {
                    return "-1";
                }
            }
            else
            {
                return "-1";
            }
        }



    }

}