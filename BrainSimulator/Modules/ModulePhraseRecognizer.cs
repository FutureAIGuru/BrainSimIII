//
// Copyright (c) FutureAI. All rights reserved.  
// Code is proprietary and may not be distributed without a separate licence
//  

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using static System.Math;

namespace BrainSimulator.Modules
{
    public class ModulePhraseRecognizer : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XmlIgnore] 
        //public theStatus = 1;

        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModulePhraseRecognizer()
        {
            minHeight = 1;
            maxHeight = 1;
            minWidth = 1;
            maxWidth = 1;
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
            Thing currentPhraseParent = UKS.GetOrAddThing("CurrentPhrase", attn);
            if (currentPhraseParent == null || currentPhraseParent.Children.Count == 0) return;

            Thing currentPhrase = currentPhraseParent.Children[0];
            currentPhrase.RemoveParent(currentPhraseParent);
            if (currentPhrase.Relationships.Count == 0) return;
            List<Thing> cpParameters = currentPhrase.RelationshipsAsThings.Where(t => t.HasAncestor(UKS.Labeled("Parameter"))).ToList();

            Thing match = FindClosestMatchingPhrase(currentPhrase);
            Thing action;
            if ((float)match.V == 0)
            {
                action = new();
                action.V = "Speak";
                Thing noMatchResponse = new Thing();
                foreach (Thing t in UKS.Labeled("resIDoNotUnderstand").RelationshipsAsThings)
                {
                    noMatchResponse.AddRelationship(t);
                }
                noMatchResponse.AddRelationship(UKS.Labeled("w.."));
                noMatchResponse.AddRelationship(UKS.Labeled("wI"));
                noMatchResponse.AddRelationship(UKS.Labeled("wHeard"));
                foreach (Thing t in currentPhrase.RelationshipsAsThings)
                {
                    noMatchResponse.AddRelationship(t);
                }
                action.AddRelationship(noMatchResponse);
                UKS.Labeled("CurrentQueryResult").AddRelationship(action);
                return;
            }

            action = FindAction(match.RelationshipsAsThings[0]);
            if (action == null)
            {
                action = new();
                action.V = "Speak";
                action.AddRelationship(UKS.Labeled("resIDoNotHaveAnActionForThat"));
                UKS.Labeled("CurrentQueryResult").AddRelationship(action);
                return;
            }
            else if (action.V.ToString() == "WordToParams")
            {
                cpParameters.Clear();
                List<Thing> cpThings = currentPhrase.RelationshipsAsThings;
                List<Thing> matchThings = match.RelationshipsAsThings[0].RelationshipsAsThings;
                int cpCount = cpThings.Count;
                int matchCount = matchThings.Count;
                int shortestCount = cpCount < matchCount ? cpCount : matchCount;
                for (int i = 0; i < shortestCount; i++)
                {
                    if (cpThings[i].HasAncestorLabeled("Parameter") &&
                        ! cpThings[i].Label.StartsWith("Verb") )
                    {
                        cpParameters.Add(cpThings[i]);
                    }
                    else if (matchThings[i].Label == "Word")
                    {
                        Thing newParam = new();
                        newParam.AddRelationship(cpThings[i]);
                        cpParameters.Add(newParam);
                    }
                }
                action = FindAction(action.RelationshipsAsThings[0]);
            }

            Thing queryResult = UKS.Labeled("CurrentQueryResult");
            queryResult.AddRelationship(action);
            foreach (Thing t in cpParameters) UKS.Labeled("CurrentQueryResult").AddRelationship(t);
            UKS.DeleteThing(currentPhrase);
        }

        // Goes through situations linked to a thing to find an
        // action that leads to positive results.
        private Thing FindAction(Thing match)
        {
            ModuleEvent Event = (ModuleEvent)FindModule(typeof(ModuleEvent));
            if (Event == null) return null;
            Thing action = null;
            List<Relationship> situations = match.GetRelationshipByWithAncestor(UKS.Labeled("Situation"));
            foreach (Relationship l in situations)
            {
                string step = "";
                if (l.source is Thing lsource)
                {
                    Thing eventResult = Event.FindTowardGoal(UKS.Labeled("Positive"), lsource, out step);
                    if (eventResult == null) continue;
                    action = eventResult.RelationshipsAsThings[0];
                }
                break;
            }

            return action;
        }

        // Finds match of currentPhrase to any heard phrases.
        // If no match currently returns empty thing. 
        private Thing FindClosestMatchingPhrase(Thing currentPhrase)
        {
            Thing closestMatch = new();
            closestMatch.V = 0f;
            List<Thing> currentPhraseWords = currentPhrase.RelationshipsAsThings;
            int cpCount = currentPhraseWords.Count;
            IList<Thing> heardPhrases = UKS.Labeled("HeardPhrase").Children;
            foreach (Thing heardPhrase in heardPhrases)
            {
                if ( heardPhrase.Label == "qyPropertiesOfObjectReference" )
                {

               }
                List<Thing> heardPhraseWords = heardPhrase.RelationshipsAsThings;
                float match = 0;
                int hpCount = heardPhraseWords.Count;
                int shortestPhraseCount = hpCount <= cpCount ? hpCount : cpCount;
                int longestPhraseCount = hpCount <= cpCount ? cpCount : hpCount;
                for (int i = 0; i < shortestPhraseCount; i++)
                {
                    Thing cpW = currentPhrase.RelationshipsAsThings[i];
                    Thing hpW = heardPhrase.RelationshipsAsThings[i];
                    if ( cpW.Relationships.Count == 1 &&
                         cpW.RelationshipsAsThings[0].V.Equals(hpW.V) )
                    {
                        match++;
                    }
                    else if ( cpW.Label.StartsWith("Verb") && cpW.V == hpW.V )
                    {
                        match++;
                    }
                    else if (cpW == hpW)
                    {
                        match++; // exact match
                    }
                    else if (hpW.Label.StartsWith("param") && cpW.Label.StartsWith(hpW.Label.Substring(5)))
                    {
                        match++;
                    }
                    else if (cpW.HasAncestor(hpW)) 
                    { 
                        match += 0.5f; 
                    } // matches parent
                    else break;
                }

                float score = match / longestPhraseCount;
                if (score > (float)closestMatch.V)
                {
                    closestMatch.RemoveRelationshipAt(0);
                    closestMatch.AddRelationship(heardPhrase);
                    closestMatch.V = score;
                }
            }
            return closestMatch;
        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
            InsertPhrasesIntoUKS();
        }

        private void InsertPhrasesIntoUKS()
        {
            GetUKS();
            if (UKS == null) return;

            // This routine assumes that all required UKS roots have already been initilaized by 
            // UKS.CreateInitialStructure(). 

            // Remove current directives, queries, declarations and responses to prevent duplicates.
            Thing directive = UKS.Labeled("Directive");
            if (directive != null) UKS.DeleteAllChildren(directive);
            Thing query = UKS.Labeled("Query");
            if (query != null) UKS.DeleteAllChildren(query);
            Thing declaration = UKS.Labeled("Declaration");
            if (declaration != null) UKS.DeleteAllChildren(declaration);
            Thing response = UKS.Labeled("Response");
            if (response != null) UKS.DeleteAllChildren(response);

            Thing t = null;
            #pragma warning disable format

            // Pre-seeded Words needed for known phrases.
            UKS.GetOrAddThing("wGo", UKS.Labeled("Word"), "go");
            UKS.GetOrAddThing("wTo", UKS.Labeled("Word"), "to");
            UKS.GetOrAddThing("wLook", UKS.Labeled("Word"), "look");
            UKS.GetOrAddThing("wAt", UKS.Labeled("Word"), "at");
            UKS.GetOrAddThing("wAm", UKS.Labeled("Word"), "am");
            UKS.GetOrAddThing("wPoint", UKS.Labeled("Word"), "point");
            UKS.GetOrAddThing("wTake", UKS.Labeled("Word"), "take");
            UKS.GetOrAddThing("wPicture", UKS.Labeled("Word"), "picture");
            UKS.GetOrAddThing("wGood", UKS.Labeled("Word"), "good");
            UKS.GetOrAddThing("wTodays", UKS.Labeled("Word"), "todays");
            UKS.GetOrAddThing("wToday", UKS.Labeled("Word"), "today");
            UKS.GetOrAddThing("wYesterdays", UKS.Labeled("Word"), "yesterdays");
            UKS.GetOrAddThing("wTomorrows", UKS.Labeled("Word"), "tomorrows");
            UKS.GetOrAddThing("wYesterday", UKS.Labeled("Word"), "yesterday");
            UKS.GetOrAddThing("wTomorrow", UKS.Labeled("Word"), "tomorrow");
            UKS.GetOrAddThing("wDraw", UKS.Labeled("Word"), "draw");
            UKS.GetOrAddThing("wWhen", UKS.Labeled("Word"), "when");
            UKS.GetOrAddThing("wWork", UKS.Labeled("Word"), "work");
            UKS.GetOrAddThing("wWrite", UKS.Labeled("Word"), "write");
            UKS.GetOrAddThing("wOver", UKS.Labeled("Word"), "over");
            UKS.GetOrAddThing("wEnds", UKS.Labeled("Word"), "ends");
            UKS.GetOrAddThing("wWas", UKS.Labeled("Word"), "was");
            UKS.GetOrAddThing("wDate", UKS.Labeled("Word"), "date");
            UKS.GetOrAddThing("wTime", UKS.Labeled("Word"), "time");
            UKS.GetOrAddThing("wNo", UKS.Labeled("Word"), "no");
            UKS.GetOrAddThing("wThis", UKS.Labeled("Word"), "this");
            UKS.GetOrAddThing("wIs", UKS.Labeled("Word"), "is");
            UKS.GetOrAddThing("wIt", UKS.Labeled("Word"), "it");
            UKS.GetOrAddThing("wA", UKS.Labeled("Word"), "a");
            UKS.GetOrAddThing("wCan", UKS.Labeled("Word"), "can");
            UKS.GetOrAddThing("wWhat", UKS.Labeled("Word"), "what");
            UKS.GetOrAddThing("wBehind", UKS.Labeled("Word"), "behind");
            UKS.GetOrAddThing("wYou", UKS.Labeled("Word"), "you");
            UKS.GetOrAddThing("wYour", UKS.Labeled("Word"), "your");
            UKS.GetOrAddThing("wDo", UKS.Labeled("Word"), "do");
            UKS.GetOrAddThing("wFeel", UKS.Labeled("Word"), "feel");
            UKS.GetOrAddThing("wHow", UKS.Labeled("Word"), "how");
            UKS.GetOrAddThing("wMany", UKS.Labeled("Word"), "many");
            UKS.GetOrAddThing("wI", UKS.Labeled("Word"), "i");
            UKS.GetOrAddThing("wNot", UKS.Labeled("Word"), "not");
            UKS.GetOrAddThing("wUnderstand", UKS.Labeled("Word"), "understand");
            UKS.GetOrAddThing("wThere", UKS.Labeled("Word"), "there");
            UKS.GetOrAddThing("wMe", UKS.Labeled("Word"), "me");
            UKS.GetOrAddThing("wNothing", UKS.Labeled("Word"), "nothing");
            UKS.GetOrAddThing("wKnow", UKS.Labeled("Word"), "know");
            UKS.GetOrAddThing("wAbout", UKS.Labeled("Word"), "about");
            UKS.GetOrAddThing("wOK", UKS.Labeled("Word"), "ok");
            UKS.GetOrAddThing("wLeft", UKS.Labeled("Word"), "left");
            UKS.GetOrAddThing("wRight", UKS.Labeled("Word"), "right");
            UKS.GetOrAddThing("wAbove", UKS.Labeled("Word"), "above");
            UKS.GetOrAddThing("wBelow", UKS.Labeled("Word"), "below");
            UKS.GetOrAddThing("wOf", UKS.Labeled("Word"), "of");
            UKS.GetOrAddThing("wSee", UKS.Labeled("Word"), "see");
            UKS.GetOrAddThing("wAnything", UKS.Labeled("Word"), "anything");
            UKS.GetOrAddThing("wStop", UKS.Labeled("Word"), "stop");
            UKS.GetOrAddThing("wLate", UKS.Labeled("Word"), "late");
            UKS.GetOrAddThing("wDont", UKS.Labeled("Word"), "dont");
            UKS.GetOrAddThing("wThat", UKS.Labeled("Word"), "that");
            UKS.GetOrAddThing("wNone", UKS.Labeled("Word"), "none");
            UKS.GetOrAddThing("wAre", UKS.Labeled("Word"), "are");
            UKS.GetOrAddThing("wIn", UKS.Labeled("Word"), "in");
            UKS.GetOrAddThing("wCount", UKS.Labeled("Word"), "count");
            UKS.GetOrAddThing("wExplore", UKS.Labeled("Word"), "explore");
            UKS.GetOrAddThing("wName", UKS.Labeled("Word"), "name");
            UKS.GetOrAddThing("wValid", UKS.Labeled("Word"), "valid");
            UKS.GetOrAddThing("wWake", UKS.Labeled("Word"), "wake");
            UKS.GetOrAddThing("wWhere", UKS.Labeled("Word"), "where");
            UKS.GetOrAddThing("wWord", UKS.Labeled("Word"), "word");
            UKS.GetOrAddThing("wList", UKS.Labeled("Word"), "list");
            UKS.GetOrAddThing("wSome", UKS.Labeled("Word"), "some");
            UKS.GetOrAddThing("wObjects", UKS.Labeled("Word"), "objects");
            UKS.GetOrAddThing("wFind", UKS.Labeled("Word"), "find");
            UKS.GetOrAddThing("wTurn", UKS.Labeled("Word"), "turn");
            UKS.GetOrAddThing("wAround", UKS.Labeled("Word"), "around");
            UKS.GetOrAddThing("wLandmarks", UKS.Labeled("Word"), "landmarks");
            UKS.GetOrAddThing("wSure", UKS.Labeled("Word"), "sure");
            UKS.GetOrAddThing("wCube", UKS.Labeled("Word"), "cube");
            UKS.GetOrAddThing("wSmall", UKS.Labeled("Word"), "small");
            UKS.GetOrAddThing("wSphere", UKS.Labeled("Word"), "sphere");
            UKS.GetOrAddThing("wYellow", UKS.Labeled("Word"), "yellow");
            UKS.GetOrAddThing("wMedium", UKS.Labeled("Word"), "medium");
            UKS.GetOrAddThing("wHeard", UKS.Labeled("Word"), "heard");
            UKS.GetOrAddThing("wHave", UKS.Labeled("Word"), "have");
            UKS.GetOrAddThing("wAn", UKS.Labeled("Word"), "an");
            UKS.GetOrAddThing("wAction", UKS.Labeled("Word"), "action");
            UKS.GetOrAddThing("wFor", UKS.Labeled("Word"), "for");
            UKS.GetOrAddThing("wSomething", UKS.Labeled("Word"), "something");
            UKS.GetOrAddThing("wWent", UKS.Labeled("Word"), "went");
            UKS.GetOrAddThing("wWrong", UKS.Labeled("Word"), "wrong");
            UKS.GetOrAddThing("wReplace", UKS.Labeled("Word"), "replace");
            UKS.GetOrAddThing("wWith", UKS.Labeled("Word"), "with");
            UKS.GetOrAddThing("wTell", UKS.Labeled("Word"), "tell");
            UKS.GetOrAddThing("wThe", UKS.Labeled("Word"), "the");
            UKS.GetOrAddThing("wColor", UKS.Labeled("Word"), "color");
            UKS.GetOrAddThing("wShape", UKS.Labeled("Word"), "shape");
            UKS.GetOrAddThing("wSize", UKS.Labeled("Word"), "size");
            UKS.GetOrAddThing("wThings", UKS.Labeled("Word"), "thing");
            UKS.GetOrAddThing("wCould", UKS.Labeled("Word"), "could");
            UKS.GetOrAddThing("wThink", UKS.Labeled("Word"), "think");
            UKS.GetOrAddThing("wDance", UKS.Labeled("Word"), "dance");
            UKS.GetOrAddThing("wUp", UKS.Labeled("Word"), "up");
            UKS.GetOrAddThing("wDown", UKS.Labeled("Word"), "down");
            UKS.GetOrAddThing("wForward", UKS.Labeled("Word"), "forward");
            UKS.GetOrAddThing("wSay", UKS.Labeled("Word"), "say");
            UKS.GetOrAddThing("wHas", UKS.Labeled("Word"), "has");
            UKS.GetOrAddThing("wOn", UKS.Labeled("Word"), "on");
            UKS.GetOrAddThing("wThan", UKS.Labeled("Word"), "than");
            UKS.GetOrAddThing("wProperties", UKS.Labeled("Word"), "properties");
            UKS.GetOrAddThing("wAnd", UKS.Labeled("Word"), "and");
            UKS.GetOrAddThing("wYes", UKS.Labeled("Word"), "yes");
            UKS.GetOrAddThing("wNo", UKS.Labeled("Word"), "no");

            // This is puncuation for outgoing text, speech.
            UKS.GetOrAddThing("puncComma", UKS.Labeled("Word"), ",");
            UKS.GetOrAddThing("w..", UKS.Labeled("Word"), ".."); // This is used for adding delays to Speech to text.

            // These are things thate are parmaterized. Variable part of sentences.
            UKS.GetOrAddThing("Parameter", UKS.Labeled("Phrase"));
            UKS.GetOrAddThing("paramObjectReference", "Parameter");
            UKS.GetOrAddThing("paramObject", "Parameter");
            UKS.GetOrAddThing("paramDate", "Parameter");
            UKS.GetOrAddThing("paramPrevDate", "Parameter");
            UKS.GetOrAddThing("paramNextDate", "Parameter");
            UKS.GetOrAddThing("paramTime", "Parameter");
            UKS.GetOrAddThing("paramName", "Parameter");
            UKS.GetOrAddThing("paramEnds", "Parameter");
            UKS.GetOrAddThing("paramStarts", "Parameter");
            UKS.GetOrAddThing("paramProperty", "Parameter");
            UKS.GetOrAddThing("paramPropertyName", "Parameter");
            UKS.GetOrAddThing("paramRelation", "Parameter");
            UKS.GetOrAddThing("paramFeel", "Parameter");
            UKS.GetOrAddThing("paramCount", "Parameter");
            UKS.GetOrAddThing("paramWakeword", "Parameter");
            UKS.GetOrAddThing("paramLandmark", "Parameter");
            UKS.GetOrAddThing("paramColorReference", "Parameter");
            UKS.GetOrAddThing("paramColorDescription", "Parameter");
            UKS.GetOrAddThing("paramPhrase", "Parameter");
            UKS.GetOrAddThing("paramPicture", "Parameter");
            UKS.GetOrAddThing("paramNumber", "Parameter");
            UKS.GetOrAddThing("paramProperties", "Parameter");
            UKS.GetOrAddThing("paramVerb", "Parameter");


            // Directives, commands giving to Sallie
            UKS.GetOrAddThing("HeardPhrase", "Phrase");
                t = UKS.GetOrAddThing("dirGoTo", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("wGo"));
                    t.AddRelationship(UKS.Labeled("wTo"));
                    t.AddRelationship(UKS.Labeled("ObjectReference"));
                AddPhraseEvent(UKS.Labeled("dirGoTo"), "Action", UKS.Labeled("dirGoTo"), "Positive");

                t = UKS.GetOrAddThing("dirExplore", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("wExplore"));
                AddPhraseEvent(UKS.Labeled("dirExplore"), "Action", UKS.Labeled("dirExplore"), "Positive");
                
                t = UKS.GetOrAddThing("dirGood", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("wGood"));
                t = UKS.GetOrAddThing("dirNo", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("wNo"));
                t = UKS.GetOrAddThing("dirStop", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("wStop"));
                t = UKS.GetOrAddThing("dirDontDoThat", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("wDont"));
                    t.AddRelationship(UKS.Labeled("wDo"));
                    t.AddRelationship(UKS.Labeled("wThat"));
                t = UKS.GetOrAddThing("dirTurnAround", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("wTurn"));
                    t.AddRelationship(UKS.Labeled("wAround"));
                AddPhraseEvent(UKS.Labeled("dirTurnAround"), "Action", UKS.Labeled("dirTurnAround"), "Positive");

                t = UKS.GetOrAddThing("dirLookAround", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("wLook"));
                    t.AddRelationship(UKS.Labeled("wAround"));
                t = UKS.GetOrAddThing("dirFindLandmarks", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("wFind"));
                    t.AddRelationship(UKS.Labeled("wLandmarks"));
                AddPhraseEvent(UKS.Labeled("dirFindLandmarks"), "Action", UKS.Labeled("dirFindLandmarks"), "Positive");

                t = UKS.GetOrAddThing("dirFindObject", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("wFind"));
                    t.AddRelationship(UKS.Labeled("paramObjectReference"));
                AddPhraseEvent(UKS.Labeled("dirFindObject"), "Action", UKS.Labeled("dirFindObject"), "Positive");

                t = UKS.GetOrAddThing("dirGoToLandmark", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("wGo"));
                    t.AddRelationship(UKS.Labeled("wTo"));
                    t.AddRelationship(UKS.Labeled("paramLandmark"));
                AddPhraseEvent(UKS.Labeled("dirGoToLandmark"), "Action", UKS.Labeled("dirGoToLandmark"), "Positive");

                t = UKS.GetOrAddThing("dirReplaceXWithY", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("wReplace"));
                    t.AddRelationship(UKS.Labeled("paramReplacement"));
                    t.AddRelationship(UKS.Labeled("wWith"));
                    t.AddRelationship(UKS.Labeled("paramReplacement"));
                t = UKS.GetOrAddThing("dirDance", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("wDance"));
                t = UKS.GetOrAddThing("dirDrawAShape", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("wDraw"));
                    t.AddRelationship(UKS.Labeled("wA"));
                    t.AddRelationship(UKS.Labeled("paramShape"));
                t = UKS.GetOrAddThing("dirWriteYourName", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("wWrite"));
                    t.AddRelationship(UKS.Labeled("wYour"));
                    t.AddRelationship(UKS.Labeled("wName"));
                    t.AddRelationship(UKS.Labeled("paramName"));
                t = UKS.GetOrAddThing("dirTakeAPicture", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("wTake"));
                    t.AddRelationship(UKS.Labeled("wA"));
                    t.AddRelationship(UKS.Labeled("wPicture"));
                    t.AddRelationship(UKS.Labeled("paramPicture"));
                t = UKS.GetOrAddThing("dirSayPhrase", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("wSay"));
                    t.AddRelationship(UKS.Labeled("paramPhrase"));


            // Declarations are statements inputs. 
            UKS.GetOrAddThing("Declaration", "Phrase");

                // Is phrase
                t = UKS.GetOrAddThing("decObjectReferenceIsObjectReference", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("paramObjectReference"));
                    t.AddRelationship(UKS.Labeled("wIs"));
                    t.AddRelationship(UKS.Labeled("paramObjectReference"));
                AddPhraseEvent(UKS.Labeled("decObjectReferenceIsObjectReference"), "Store", UKS.Labeled("vIs"), "Positive");

                // Is A phrase
                t = UKS.GetOrAddThing("decObjectReferenceIsAObjectReference", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("paramObjectReference"));
                    t.AddRelationship(UKS.Labeled("wIs"));
                    t.AddRelationship(UKS.Labeled("wA"));
                    t.AddRelationship(UKS.Labeled("paramObjectReference"));
                AddPhraseEvent(UKS.Labeled("decObjectReferenceIsAObjectReference"), "Store", UKS.Labeled("vIsA"), "Positive");

            // has phrase
            t = UKS.GetOrAddThing("decObjectReferenceHasObjectReference", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("paramObjectReference"));
                    t.AddRelationship(UKS.Labeled("wHas"));
                    t.AddRelationship(UKS.Labeled("paramObjectReference"));
                AddPhraseEvent(UKS.Labeled("decObjectReferenceHasObjectReference"), "Store", UKS.Labeled("vHas"), "Positive");

            // has count phrase
            t = UKS.GetOrAddThing("decObjectReferenceHasNumberObjectReference", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("paramObjectReference"));
                    t.AddRelationship(UKS.Labeled("wHas"));
                    t.AddRelationship(UKS.Labeled("paramNumber"));
                    t.AddRelationship(UKS.Labeled("paramObjectReference"));
                AddPhraseEvent(UKS.Labeled("decObjectReferenceHasNumberObjectReference"), "Store", UKS.GetOrAddThing("paramHasCount", "Parameter"), "Positive");

            t = UKS.GetOrAddThing("decObjectHasNumberObjectReference", "HeardPhrase");
                t.AddRelationship(UKS.Labeled("paramObject"));
                t.AddRelationship(UKS.Labeled("wHas"));
                t.AddRelationship(UKS.Labeled("paramNumber"));
                t.AddRelationship(UKS.Labeled("paramObjectReference"));
            AddPhraseEvent(UKS.Labeled("decObjectHasNumberObjectReference"), "Store", UKS.GetOrAddThing("paramHasCount", "Parameter"), "Positive");

            // ObjectReference Is Relation Than ObjectReference
            t = UKS.GetOrAddThing("decObjectReferenceIsObjectReferenceThanObjectReference", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("paramObjectReference"));
                    t.AddRelationship(UKS.Labeled("wIs"));
                    t.AddRelationship(UKS.Labeled("paramObjectReference"));
                    t.AddRelationship(UKS.Labeled("wThan"));
                    t.AddRelationship(UKS.Labeled("paramObjectReference"));
                AddPhraseEvent(UKS.Labeled("decObjectReferenceIsObjectReferenceThanObjectReference"), "Store", UKS.Labeled("paramRelation"), "Positive");

            // ObjectReference Is Relation With ObjectReference
            t = UKS.GetOrAddThing("decObjectReferenceIsObjectReferenceWithObjectReference", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("paramObjectReference"));
                    t.AddRelationship(UKS.Labeled("wIs"));
                    t.AddRelationship(UKS.Labeled("paramObjectReference"));
                    t.AddRelationship(UKS.Labeled("wWith"));
                    t.AddRelationship(UKS.Labeled("paramObjectReference"));
                AddPhraseEvent(UKS.Labeled("decObjectReferenceIsObjectReferenceWithObjectReference"), "Store", UKS.Labeled("paramRelation"), "Positive");

            // ObjectReference Is Relation With ObjectReference
            t = UKS.GetOrAddThing("decObjectReferenceIsVERBWithObjectReference", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("paramObjectReference"));
                    t.AddRelationship(UKS.Labeled("wIs"));
                    t.AddRelationship(UKS.Labeled("paramVerb"));
                    t.AddRelationship(UKS.Labeled("wWith"));
                    t.AddRelationship(UKS.Labeled("paramObjectReference"));
                AddPhraseEvent(UKS.Labeled("decObjectReferenceIsVERBWithObjectReference"), "Store", UKS.Labeled("paramRelation"), "Positive");

            t = UKS.GetOrAddThing("decObjectReferenceCanVERB", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("paramObjectReference"));
                    t.AddRelationship(UKS.Labeled("wCan"));
                    t.AddRelationship(UKS.Labeled("paramVerb"));

                t = UKS.GetOrAddThing("decYourNameIsWakeword", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("wYour"));
                    t.AddRelationship(UKS.Labeled("wName"));
                    t.AddRelationship(UKS.Labeled("wIs"));
                    t.AddRelationship(UKS.Labeled("paramWakeword"));
                t = UKS.GetOrAddThing("decObjectIsObjectReference", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("paramObject"));
                    t.AddRelationship(UKS.Labeled("wIs"));
                    t.AddRelationship(UKS.Labeled("paramObjectReference"));
                t = UKS.GetOrAddThing("decObjectIsNotObjectReference", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("paramObject"));
                    t.AddRelationship(UKS.Labeled("wIs"));
                    t.AddRelationship(UKS.Labeled("wNot"));
                    t.AddRelationship(UKS.Labeled("paramObjectReference"));
                t = UKS.GetOrAddThing("decObjectReferenceIsNotObjectReference", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("paramObjectReference"));
                    t.AddRelationship(UKS.Labeled("wIs"));
                    t.AddRelationship(UKS.Labeled("wNot"));
                    t.AddRelationship(UKS.Labeled("paramObjectReference"));
                t = UKS.GetOrAddThing("decYouAreAtLandmark", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("paramObjectReference"));
                    t.AddRelationship(UKS.Labeled("wIs"));
                    t.AddRelationship(UKS.Labeled("wAt"));
                    t.AddRelationship(UKS.Labeled("paramLandmark"));

                // Properties of Object -- Lists references of giving object.
                t = UKS.GetOrAddThing("qyPropertiesOfObjectReference", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("wProperties"));
                    t.AddRelationship(UKS.Labeled("wOf"));
                    t.AddRelationship(UKS.Labeled("paramObjectReference"));

                // Query what has a Relation
                t = UKS.GetOrAddThing("qyWhatVERBObject", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("wWhat"));
                    t.AddRelationship(UKS.Labeled("Verb"));
                    t.AddRelationship(UKS.Labeled("paramObject"));
                AddPhraseEvent(UKS.Labeled("qyWhatVERBObject"), "ListObjectsWithRelation", UKS.Labeled("qyWhatVERBObject"), "Positive");
            
                // Query what has relation with count
                t = UKS.GetOrAddThing("qyWhatHasNumberObject", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("wWhat"));
                    t.AddRelationship(UKS.Labeled("wHas"));
                    t.AddRelationship(UKS.Labeled("paramNumber"));
                    t.AddRelationship(UKS.Labeled("paramObject"));
                AddPhraseEvent(UKS.Labeled("qyWhatHasNumberObject"), "ListObjectsWithRelationCount", UKS.Labeled("qyWhatHasNumberObject"), "Positive");


                t = UKS.GetOrAddThing("qyWhatIsObjectReference", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("wWhat"));
                    t.AddRelationship(UKS.Labeled("wIs"));
                    t.AddRelationship(UKS.Labeled("paramObjectReference"));
                t = UKS.GetOrAddThing("qyWhatIsObject", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("wWhat"));
                    t.AddRelationship(UKS.Labeled("wIs"));
                    t.AddRelationship(UKS.Labeled("paramObject"));
                t = UKS.GetOrAddThing("qyWhatIsProperty", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("wWhat"));
                    t.AddRelationship(UKS.Labeled("wIs"));
                    t.AddRelationship(UKS.Labeled("paramProperty"));
                t = UKS.GetOrAddThing("qyWhatIsRelationOfObjectReference", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("wWhat"));
                    t.AddRelationship(UKS.Labeled("wIs"));
                    t.AddRelationship(UKS.Labeled("paramRelation"));
                    t.AddRelationship(UKS.Labeled("wOf"));
                    t.AddRelationship(UKS.Labeled("paramObjectReference"));
                t = UKS.GetOrAddThing("qyWhatIsBehindObjectReference", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("wWhat"));
                    t.AddRelationship(UKS.Labeled("wIs"));
                    t.AddRelationship(UKS.Labeled("paramRelation"));
                    t.AddRelationship(UKS.Labeled("paramObjectReference"));
                t = UKS.GetOrAddThing("qyWhatIsInFrontOfObjectReference", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("wWhat"));
                    t.AddRelationship(UKS.Labeled("wIs"));
                    t.AddRelationship(UKS.Labeled("wIn"));
                    t.AddRelationship(UKS.Labeled("paramRelation"));
                    t.AddRelationship(UKS.Labeled("wOf"));
                    t.AddRelationship(UKS.Labeled("paramObjectReference"));
                t = UKS.GetOrAddThing("qyHowManyObjectsLikeObjectReference", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("wHow"));
                    t.AddRelationship(UKS.Labeled("wMany"));
                    t.AddRelationship(UKS.Labeled("wObjects"));
                    t.AddRelationship(UKS.Labeled("wLike"));
                    t.AddRelationship(UKS.Labeled("paramObjectReference"));
                t = UKS.GetOrAddThing("qyHowManyPropertyObjects", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("wHow"));
                    t.AddRelationship(UKS.Labeled("wMany"));
                    t.AddRelationship(UKS.Labeled("paramProperty"));
                    t.AddRelationship(UKS.Labeled("wObjects"));
                t = UKS.GetOrAddThing("qyWhatIsTodaysDate", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("wWhat"));
                    t.AddRelationship(UKS.Labeled("wIs"));
                    t.AddRelationship(UKS.Labeled("wTodays"));
                    t.AddRelationship(UKS.Labeled("wDate"));
                t = UKS.GetOrAddThing("qyWhatWasYesterdaysDate", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("wWhat"));
                    t.AddRelationship(UKS.Labeled("wWas"));
                    t.AddRelationship(UKS.Labeled("wYesterdays"));
                    t.AddRelationship(UKS.Labeled("wDate"));
                t = UKS.GetOrAddThing("qyWhatIsTomorrowsDate", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("wWhat"));
                    t.AddRelationship(UKS.Labeled("wIs"));
                    t.AddRelationship(UKS.Labeled("wThe"));
                    t.AddRelationship(UKS.Labeled("wDate"));
                    t.AddRelationship(UKS.Labeled("wTomorrow"));
                t = UKS.GetOrAddThing("qyWhatTimeIsIt", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("wWhat"));
                    t.AddRelationship(UKS.Labeled("wTime"));
                    t.AddRelationship(UKS.Labeled("wIs"));
                    t.AddRelationship(UKS.Labeled("wIt"));
                t = UKS.GetOrAddThing("qyHowDoYouFeel", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("wHow"));
                    t.AddRelationship(UKS.Labeled("wDo"));
                    t.AddRelationship(UKS.Labeled("ObjectReference"));
                    t.AddRelationship(UKS.Labeled("wFeel"));
                t = UKS.GetOrAddThing("qyAreYouOK", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("wAre"));
                    t.AddRelationship(UKS.Labeled("wYou"));
                    t.AddRelationship(UKS.Labeled("wOk"));
                t = UKS.GetOrAddThing("qyWhatIsTheTime", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("wWhat"));
                    t.AddRelationship(UKS.Labeled("wIs"));
                    t.AddRelationship(UKS.Labeled("wThe"));
                    t.AddRelationship(UKS.Labeled("wTime"));
                t = UKS.GetOrAddThing("qyWhenIsWorkOver", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("wWhen"));
                    t.AddRelationship(UKS.Labeled("wIs"));
                    t.AddRelationship(UKS.Labeled("wWork"));
                    t.AddRelationship(UKS.Labeled("wOver"));
                t = UKS.GetOrAddThing("qyHowDoYouFeel", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("wHow"));
                    t.AddRelationship(UKS.Labeled("wDo"));
                    t.AddRelationship(UKS.Labeled("wYou"));
                    t.AddRelationship(UKS.Labeled("wFeel"));
                t = UKS.GetOrAddThing("qyNameSomeObjectsThatAreObjectReference", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("wName"));
                    t.AddRelationship(UKS.Labeled("wSome"));
                    t.AddRelationship(UKS.Labeled("wObjects"));
                    t.AddRelationship(UKS.Labeled("wThat"));
                    t.AddRelationship(UKS.Labeled("wAre"));
                    t.AddRelationship(UKS.Labeled("paramObjectReference"));
                t = UKS.GetOrAddThing("qyWhereAreYou", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("wWhere"));
                    t.AddRelationship(UKS.Labeled("wAre"));
                    t.AddRelationship(UKS.Labeled("paramObjectReference"));
                t = UKS.GetOrAddThing("qyTellMeAboutTheColor", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("wTell"));
                    t.AddRelationship(UKS.Labeled("wMe"));
                    t.AddRelationship(UKS.Labeled("wAbout"));
                    t.AddRelationship(UKS.Labeled("wThe"));
                    t.AddRelationship(UKS.Labeled("wColor"));
                t = UKS.GetOrAddThing("qyTellMeSomeThingsThatAreTheColor", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("wTell"));
                    t.AddRelationship(UKS.Labeled("wMe"));
                    t.AddRelationship(UKS.Labeled("wSome"));
                    t.AddRelationship(UKS.Labeled("wThings"));
                    t.AddRelationship(UKS.Labeled("wThat"));
                    t.AddRelationship(UKS.Labeled("wAre"));
                    t.AddRelationship(UKS.Labeled("wThe"));
                    t.AddRelationship(UKS.Labeled("wColor"));
                t = UKS.GetOrAddThing("qyWhatIsThisColor", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("wWhat"));
                    t.AddRelationship(UKS.Labeled("wIs"));
                    t.AddRelationship(UKS.Labeled("paramObjectReference"));
                    t.AddRelationship(UKS.Labeled("wColor"));
                t = UKS.GetOrAddThing("qyTellMeTheColorOfObjectReference", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("wTell"));
                    t.AddRelationship(UKS.Labeled("wMe"));
                    t.AddRelationship(UKS.Labeled("wThe"));
                    t.AddRelationship(UKS.Labeled("wColor"));
                    t.AddRelationship(UKS.Labeled("wOf"));
                    t.AddRelationship(UKS.Labeled("paramObjectReference"));
                t = UKS.GetOrAddThing("qyTellMeTheShapeOfObjectReference", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("wTell"));
                    t.AddRelationship(UKS.Labeled("wMe"));
                    t.AddRelationship(UKS.Labeled("wThe"));
                    t.AddRelationship(UKS.Labeled("wShape"));
                    t.AddRelationship(UKS.Labeled("wOf"));
                    t.AddRelationship(UKS.Labeled("paramObjectReference"));
                t = UKS.GetOrAddThing("qyTellMeTheSizeOfObjectReference", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("wTell"));
                    t.AddRelationship(UKS.Labeled("wMe"));
                    t.AddRelationship(UKS.Labeled("wThe"));
                    t.AddRelationship(UKS.Labeled("wSize"));
                    t.AddRelationship(UKS.Labeled("wOf"));
                    t.AddRelationship(UKS.Labeled("paramObjectReference"));
                t = UKS.GetOrAddThing("qyTellMeTheObjectOfObjectReference", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("wTell"));
                    t.AddRelationship(UKS.Labeled("wMe"));
                    t.AddRelationship(UKS.Labeled("wThe"));
                    t.AddRelationship(UKS.Labeled("paramObject"));
                    t.AddRelationship(UKS.Labeled("wOf"));
                    t.AddRelationship(UKS.Labeled("paramObjectReference"));
                t = UKS.GetOrAddThing("qyHowManyObjectOnObjectReference", "HeardPhrase");
                    t.AddRelationship(UKS.Labeled("wHow"));
                    t.AddRelationship(UKS.Labeled("wMany"));
                    t.AddRelationship(UKS.Labeled("paramObject"));
                    t.AddRelationship(UKS.Labeled("wOn"));
                    t.AddRelationship(UKS.Labeled("paramObjectReference"));

            // These are things Sallie will say.
            t = UKS.GetOrAddThing("Response", "Phrase");
                t = UKS.GetOrAddThing("resIDoNotUnderstand", "Response");
                    t.AddRelationship(UKS.Labeled("wI"));
                    t.AddRelationship(UKS.Labeled("wDo"));
                    t.AddRelationship(UKS.Labeled("wNot"));
                    t.AddRelationship(UKS.Labeled("wUnderstand"));
                t = UKS.GetOrAddThing("resThatIsAObjectReference", "Response");
                    t.AddRelationship(UKS.Labeled("wThat"));
                    t.AddRelationship(UKS.Labeled("wIs"));
                    t.AddRelationship(UKS.Labeled("wA"));
                    t.AddRelationship(UKS.Labeled("paramObjectReference"));
                t = UKS.GetOrAddThing("resThatIsAProperty", "Response");
                    t.AddRelationship(UKS.Labeled("wThat"));
                    t.AddRelationship(UKS.Labeled("wIs"));
                    t.AddRelationship(UKS.Labeled("wA"));
                    t.AddRelationship(UKS.Labeled("paramProperty"));
            t = UKS.GetOrAddThing("resThereIs", "Response");
                    t.AddRelationship(UKS.Labeled("wThere"));
                    t.AddRelationship(UKS.Labeled("wIs"));
                t = UKS.GetOrAddThing("resNothingIsBehindMe", "Response");
                    t.AddRelationship(UKS.Labeled("wNothing"));
                    t.AddRelationship(UKS.Labeled("wIs"));
                    t.AddRelationship(UKS.Labeled("wBehind"));
                    t.AddRelationship(UKS.Labeled("wMe"));
                t = UKS.GetOrAddThing("resBehindMeIs", "Response");
                    t.AddRelationship(UKS.Labeled("wBehind"));
                    t.AddRelationship(UKS.Labeled("wMe"));
                    t.AddRelationship(UKS.Labeled("wIs"));
                    t.AddRelationship(UKS.Labeled("wA"));
                t = UKS.GetOrAddThing("resIFeel", "Response");
                    t.AddRelationship(UKS.Labeled("wI"));
                    t.AddRelationship(UKS.Labeled("wFeel"));
                    t.AddRelationship(UKS.Labeled("paramFeel"));
                t = UKS.GetOrAddThing("resOK", "Response");
                    t.AddRelationship(UKS.Labeled("wOK"));
                t = UKS.GetOrAddThing("resIDoNotKnowAbout", "Response");
                    t.AddRelationship(UKS.Labeled("wI"));
                    t.AddRelationship(UKS.Labeled("wDo"));
                    t.AddRelationship(UKS.Labeled("wNot"));
                    t.AddRelationship(UKS.Labeled("wKnow"));
                    t.AddRelationship(UKS.Labeled("wAbout"));
                    t = UKS.GetOrAddThing("resRelation", "Response");
                t = UKS.GetOrAddThing("resIDoNotSeeA", "Response");
                    t.AddRelationship(UKS.Labeled("wI"));
                    t.AddRelationship(UKS.Labeled("wDo"));
                    t.AddRelationship(UKS.Labeled("wNot"));
                    t.AddRelationship(UKS.Labeled("wSee"));
                    t.AddRelationship(UKS.Labeled("wA"));
                t = UKS.GetOrAddThing("resIDontSeeAnything", "Response");
                    t.AddRelationship(UKS.Labeled("wI"));
                    t.AddRelationship(UKS.Labeled("wDo"));
                    t.AddRelationship(UKS.Labeled("wNot"));
                    t.AddRelationship(UKS.Labeled("wSee"));
                    t.AddRelationship(UKS.Labeled("wAnything"));
                t = UKS.GetOrAddThing("resICount", "Response");
                    t.AddRelationship(UKS.Labeled("wI"));
                    t.AddRelationship(UKS.Labeled("wCount"));
                    t.AddRelationship(UKS.Labeled("paramCount"));
                t = UKS.GetOrAddThing("resIAmAt", "Response");
                    t.AddRelationship(UKS.Labeled("wI"));
                    t.AddRelationship(UKS.Labeled("wAm"));
                    t.AddRelationship(UKS.Labeled("wAt"));
                    t.AddRelationship(UKS.Labeled("paramLandmark"));
                t = UKS.GetOrAddThing("resIAmNotSure", "Response");
                    t.AddRelationship(UKS.Labeled("wI"));
                    t.AddRelationship(UKS.Labeled("wAm"));
                    t.AddRelationship(UKS.Labeled("wNot"));
                    t.AddRelationship(UKS.Labeled("wSure"));

                t = UKS.GetOrAddThing("resWhatIsThis", "Response");
                    t.AddRelationship(UKS.Labeled("wWhat"));
                    t.AddRelationship(UKS.Labeled("wIs"));
                    t.AddRelationship(UKS.Labeled("wThis"));
                t = UKS.GetOrAddThing("resThatIsNotAValidWakeWord", "Response");
                    t.AddRelationship(UKS.Labeled("wThat"));
                    t.AddRelationship(UKS.Labeled("wIs"));
                    t.AddRelationship(UKS.Labeled("wNot"));
                    t.AddRelationship(UKS.Labeled("wA"));
                    t.AddRelationship(UKS.Labeled("wValid"));
                    t.AddRelationship(UKS.Labeled("wWake"));
                    t.AddRelationship(UKS.Labeled("wWord"));
                t = UKS.GetOrAddThing("resIDoNotHaveAnActionForThat", "Response");
                    t.AddRelationship(UKS.Labeled("wI"));
                    t.AddRelationship(UKS.Labeled("wDo"));
                    t.AddRelationship(UKS.Labeled("wNot"));
                    t.AddRelationship(UKS.Labeled("wHave"));
                    t.AddRelationship(UKS.Labeled("wAn"));
                    t.AddRelationship(UKS.Labeled("wAction"));
                    t.AddRelationship(UKS.Labeled("wFor"));
                    t.AddRelationship(UKS.Labeled("wThat"));
                t = UKS.GetOrAddThing("resSomethingWentWrong", "Response");
                    t.AddRelationship(UKS.Labeled("wSomething"));
                    t.AddRelationship(UKS.Labeled("wWent"));
                    t.AddRelationship(UKS.Labeled("wWrong"));
                t = UKS.GetOrAddThing("resColorReferenceIsColorDescription", "Response");
                    t.AddRelationship(UKS.Labeled("paramColorReference"));
                    t.AddRelationship(UKS.Labeled("wIs"));
                    t.AddRelationship(UKS.Labeled("wA"));
                    t.AddRelationship(UKS.Labeled("paramColorDescription"));
                t = UKS.GetOrAddThing("resICouldNotFindAny", "Response");
                    t.AddRelationship(UKS.Labeled("wI"));
                    t.AddRelationship(UKS.Labeled("wCould"));
                    t.AddRelationship(UKS.Labeled("wNot"));
                    t.AddRelationship(UKS.Labeled("wFind"));
                    t.AddRelationship(UKS.Labeled("wAny"));
                t = UKS.GetOrAddThing("resIThinkThisColorIs", "Response");
                    t.AddRelationship(UKS.Labeled("wI"));
                    t.AddRelationship(UKS.Labeled("wThink"));
                    t.AddRelationship(UKS.Labeled("wThis"));
                    t.AddRelationship(UKS.Labeled("wColor"));
                    t.AddRelationship(UKS.Labeled("wIs"));
                t = UKS.GetOrAddThing("resIDoNotKnowThisColor", "Response");
                    t.AddRelationship(UKS.Labeled("wI"));
                    t.AddRelationship(UKS.Labeled("wDo"));
                    t.AddRelationship(UKS.Labeled("wNot"));
                    t.AddRelationship(UKS.Labeled("wKnow"));
                    t.AddRelationship(UKS.Labeled("wThis"));            
                    t.AddRelationship(UKS.Labeled("wColor"));
                t = UKS.GetOrAddThing("resColorOfObjectReferenceIs", "Response");
                    t.AddRelationship(UKS.Labeled("wThe"));
                    t.AddRelationship(UKS.Labeled("wColor"));
                    t.AddRelationship(UKS.Labeled("wOf"));
                    t.AddRelationship(UKS.Labeled("paramObjectReference"));
                    t.AddRelationship(UKS.Labeled("wIs"));                
                t = UKS.GetOrAddThing("resShapeOfObjectReferenceIs", "Response");
                    t.AddRelationship(UKS.Labeled("wThe"));
                    t.AddRelationship(UKS.Labeled("wShape"));
                    t.AddRelationship(UKS.Labeled("wOf"));
                    t.AddRelationship(UKS.Labeled("paramObjectReference"));
                    t.AddRelationship(UKS.Labeled("wIs"));                
                t = UKS.GetOrAddThing("resSizeOfObjectReferenceIs", "Response");
                    t.AddRelationship(UKS.Labeled("wThe"));
                    t.AddRelationship(UKS.Labeled("wSize"));
                    t.AddRelationship(UKS.Labeled("wOf"));
                    t.AddRelationship(UKS.Labeled("paramObjectReference"));
                    t.AddRelationship(UKS.Labeled("wIs"));
                t = UKS.GetOrAddThing("resICouldNotFindPropertyName", "Response");
                    t.AddRelationship(UKS.Labeled("wI"));
                    t.AddRelationship(UKS.Labeled("wCould"));
                    t.AddRelationship(UKS.Labeled("wNot"));
                    t.AddRelationship(UKS.Labeled("wFind"));
                t = UKS.GetOrAddThing("resTodayIsDate", "Response");
                    t.AddRelationship(UKS.Labeled("wToday"));
                    t.AddRelationship(UKS.Labeled("wIs"));
                    t.AddRelationship(UKS.Labeled("paramDate"));
                t = UKS.GetOrAddThing("resYesterdayWasDate", "Response");
                    t.AddRelationship(UKS.Labeled("wYesterday"));
                    t.AddRelationship(UKS.Labeled("wWas"));
                    t.AddRelationship(UKS.Labeled("paramPrevDate"));
                t = UKS.GetOrAddThing("resTomorrowIsDate", "Response");
                    t.AddRelationship(UKS.Labeled("wTomorrow"));
                    t.AddRelationship(UKS.Labeled("wIs"));
                    t.AddRelationship(UKS.Labeled("paramNextDate"));
                t = UKS.GetOrAddThing("resItIsTime", "Response");
                    t.AddRelationship(UKS.Labeled("wIt"));
                    t.AddRelationship(UKS.Labeled("wIs"));
                    t.AddRelationship(UKS.Labeled("paramTime"));
                t = UKS.GetOrAddThing("resWorkEndsInTime", "Response");
                    t.AddRelationship(UKS.Labeled("wWork"));
                    t.AddRelationship(UKS.Labeled("wEnds"));
                    t.AddRelationship(UKS.Labeled("wIn"));
                    t.AddRelationship(UKS.Labeled("paramEnds"));
                t = UKS.GetOrAddThing("resIAmEmotion", "Response");
                    t.AddRelationship(UKS.Labeled("wI"));
                    t.AddRelationship(UKS.Labeled("wAm"));
                    t.AddRelationship(UKS.Labeled("paramFeel"));
                t = UKS.GetOrAddThing("resProperties", "Response");
                    t.AddRelationship(UKS.Labeled("paramProperties"));
                t = UKS.GetOrAddThing("resSayPhrase", "Response");
                    t.AddRelationship(UKS.Labeled("paramPhrase"));
                t = UKS.GetOrAddThing("resThereAre", "Response");
                    t.AddRelationship(UKS.Labeled("wThere"));
                    t.AddRelationship(UKS.Labeled("wAre"));
                    t.AddRelationship(UKS.Labeled("paramCount"));


            AddPhraseEvent(UKS.Labeled("qyWhatIsProperty"), "Speak", UKS.Labeled("resThatIsAProperty"), "Positive");
            AddPhraseEvent(UKS.Labeled("qyWhatIsObjectReference"), "Speak", UKS.Labeled("resThatIsAObjectReference"), "Positive");
            AddPhraseEvent(UKS.Labeled("qyWhatIsObject"), "Speak", UKS.Labeled("resThatIsAObjectReference"), "Positive");
            AddPhraseEvent(UKS.Labeled("qyWhatIsRelationOfObjectReference"), "Speak", UKS.Labeled("resThatIsAObjectReference"), "Positive");
            AddPhraseEvent(UKS.Labeled("qyWhatIsBehindObjectReference"), "Speak", UKS.Labeled("resThatIsAObjectReference"), "Positive");
            AddPhraseEvent(UKS.Labeled("qyWhatIsInFrontOfObjectReference"), "Speak", UKS.Labeled("resThatIsAObjectReference"), "Positive");
            AddPhraseEvent(UKS.Labeled("qyHowDoYouFeel"), "Speak", UKS.Labeled("resIFeel"), "Positive");
            AddPhraseEvent(UKS.Labeled("qyHowManyObjectsLikeObjectReference"), "Speak", UKS.Labeled("resICount"), "Positive");
            AddPhraseEvent(UKS.Labeled("qyHowManyPropertyObjects"), "Speak", UKS.Labeled("resICount"), "Positive");
            AddPhraseEvent(UKS.Labeled("qyWhereAreYou"), "Speak", UKS.Labeled("resIAmAt"), "Positive");
            AddPhraseEvent(UKS.Labeled("qyTellMeAboutTheColor"), "Speak", UKS.Labeled("resColorReferenceIsColorDescription"), "Positive");
            AddPhraseEvent(UKS.Labeled("qyWhatIsTodaysDate"), "Speak", UKS.Labeled("resTodayIsDate"), "Positive");
            AddPhraseEvent(UKS.Labeled("qyWhatWasYesterdaysDate"), "Speak", UKS.Labeled("resYesterdayWasDate"), "Positive");
            AddPhraseEvent(UKS.Labeled("qyWhatIsTomorrowsDate"), "Speak", UKS.Labeled("resTomorrowIsDate"), "Positive");
            AddPhraseEvent(UKS.Labeled("qyWhatTimeIsIt"), "Speak", UKS.Labeled("resItIsTime"), "Positive");
            AddPhraseEvent(UKS.Labeled("qyWhatIsTheTime"), "Speak", UKS.Labeled("resItIsTime"), "Positive");
            AddPhraseEvent(UKS.Labeled("qyWhenIsWorkOver"), "Speak", UKS.Labeled("resWorkEndsInTime"), "Positive");
            AddPhraseEvent(UKS.Labeled("qyAreYouOK"), "Speak", UKS.Labeled("resIAmEmotion"), "Positive");
            AddPhraseEvent(UKS.Labeled("qyHowDoYouFeel"), "Speak", UKS.Labeled("resIAmEmotion"), "Positive");
            AddPhraseEvent(UKS.Labeled("qyTellMeTheColorOfObjectReference"), "WikiDataColor", UKS.Labeled("resTheColorOfObjectReferenceIs"), "Positive");
            AddPhraseEvent(UKS.Labeled("qyTellMeTheShapeOfObjectReference"), "WikiDataShape", UKS.Labeled("resTheShapeOfObjectReferenceIs"), "Positive");
            AddPhraseEvent(UKS.Labeled("qyTellMeTheSizeOfObjectReference"), "WikiDataSize", UKS.Labeled("resTheSizeOfObjectReferenceIs"), "Positive");
            AddPhraseEvent(UKS.Labeled("qyHowManyObjectOnObjectReference"), "Speak", UKS.Labeled("resThereAre"), "Positive");

            AddPhraseEvent(UKS.Labeled("qyPropertiesOfWord"), "WordToParams", UKS.Labeled("qyPropertiesOfObjectReference"), "Positive");
            AddPhraseEvent(UKS.Labeled("qyPropertiesOfObject"), "Speak", UKS.Labeled("resProperties"), "Positive");
            AddPhraseEvent(UKS.Labeled("qyPropertiesOfObjectReference"), "Speak", UKS.Labeled("resProperties"), "Positive");

            


            AddPhraseEvent(UKS.Labeled("dirLookAround"), "Action", UKS.Labeled("dirLookAround"), "Positive");
            AddPhraseEvent(UKS.Labeled("dirReplaceXWithY"), "Store", UKS.Labeled("dirReplaceXWithY"), "Positive");

            AddPhraseEvent(UKS.Labeled("dirDance"), "Action", UKS.Labeled("dirDance"), "Positive");
            AddPhraseEvent(UKS.Labeled("dirDrawAShape"), "Action", UKS.Labeled("dirDrawAShape"), "Positive");
            AddPhraseEvent(UKS.Labeled("dirWriteYourName"), "Action", UKS.Labeled("dirWriteYourName"), "Positive");
            AddPhraseEvent(UKS.Labeled("dirTakeAPicture"), "Action", UKS.Labeled("dirTakeAPicture"), "Positive");

            AddPhraseEvent(UKS.Labeled("dirStop"), "Action", UKS.Labeled("dirStop"), "Positive");
            AddPhraseEvent(UKS.Labeled("dirNo"), "Action", UKS.Labeled("dirStop"), "Positive"); 
            AddPhraseEvent(UKS.Labeled("dirDontDoThat"), "Action", UKS.Labeled("dirStop"), "Positive");

            AddPhraseEvent(UKS.Labeled("dirGood"), "Action", UKS.Labeled("dirGood"), "Positive");

            AddPhraseEvent(UKS.Labeled("dirSayPhrase"), "Speak", UKS.Labeled("resSayPhrase"), "Positive");

            AddPhraseEvent(UKS.Labeled("decYourNameIsWakeword"), "Action", UKS.Labeled("decYourNameIsWakeword"), "Positive");

            

            AddPhraseEvent(UKS.Labeled("decObjectReferenceCanVERB"), "Store", UKS.Labeled("vCan"), "Positive");

            AddPhraseEvent(UKS.Labeled("qyNameSomeObjectsThatAreObjectReference"), "ListChildren", UKS.Labeled("qyNameSomeObjectsThatAreObjectReference"), "Positive");
            AddPhraseEvent(UKS.Labeled("decObjectReferenceIsNotObjectReference"), "Store", UKS.Labeled("wNot"), "Positive");
            AddPhraseEvent(UKS.Labeled("decObjectIsNotObjectReference"), "Store", UKS.Labeled("wNot"), "Positive");
            AddPhraseEvent(UKS.Labeled("decYouAreAtLandmark"), "Store", UKS.Labeled("decYouAreAtLandmark"), "Positive");
            AddPhraseEvent(UKS.Labeled("dirReplaceXWithY"), "Store", UKS.Labeled("dirReplaceXWithY"), "Positive");
            AddPhraseEvent(UKS.Labeled("qyTellMeSomeThingsThatAreTheColor"), "ListObjects", UKS.Labeled("qyTellMeSomeThingsThatAreTheColor"), "Positive");
            AddPhraseEvent(UKS.Labeled("qyWhatIsThisColor"), "QueryWikiColor", UKS.Labeled("qyWhatIsThisColor"), "Positive");
#pragma warning restore format
        }

        void AddPhraseEvent(Thing parsePhrase, String actionType, Thing action, String outcome)
        {
            if (parsePhrase == null)
            {

            }
            //what is this -> This is [object]
            Thing e = UKS.GetOrAddThing("E*", "Event");
            Thing s = UKS.GetOrAddThing("s*", "Situation");
            e.AddChild(s);
            Thing a = UKS.GetOrAddThing(actionType + "*", "Action");
            a.V = actionType;
            Thing o = UKS.GetOrAddThing(outcome, "Outcome");
            Thing r = UKS.GetOrAddThing(e.Label + "_ER0", e.Label);
            r.AddRelationship(a);
            r.AddRelationship(o);
            s.AddRelationship(parsePhrase);
            a.AddRelationship(action);
        }

        //the following can be used to massage public data to be different in the xml file
        //delete if not needed
        public override void SetUpBeforeSave()
        {
        }
        public override void SetUpAfterLoad()
        {
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
            InsertPhrasesIntoUKS();
            MainWindow.ResumeEngine();
        }
    }
}
