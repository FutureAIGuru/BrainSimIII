//
// Copyright (c) FutureAI. All rights reserved.  
// Code is proprietary and may not be distributed without a separate licence
//  

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using static System.Math;
using Pluralize;
using Pluralize.NET;

namespace BrainSimulator.Modules
{
    public class ModuleQueryResolution : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XmlIgnore] 
        //public theStatus = 1;


        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleQueryResolution()
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
            ModuleQuery moduleQuery = (ModuleQuery)FindModule(typeof(ModuleQuery));
            if (moduleQuery == null) return;

            Thing queryResponse = UKS.GetOrAddThing("CurrentQueryResult", "Attention");
            if (queryResponse == null || queryResponse.Relationships.Count == 0) return; // No query response.

            Thing action = queryResponse.RelationshipsAsThings[0];

            List<Thing> parameters = new();
            for (int i = 1; i < queryResponse.Relationships.Count; i++) parameters.Add(queryResponse.RelationshipsAsThings[i]);

            queryResponse.RemoveAllRelationships();
            string actionString = action.V.ToString();

            if (action.Relationships.Count > 0 && actionString == "Speak")
            {
                HandleResponse(action.RelationshipsAsThings[0], parameters, moduleQuery);
            }
            else if (action.Relationships.Count > 0 && actionString == "Action")
            {
                HandleAction(action.RelationshipsAsThings[0], parameters, moduleQuery);
            }
            else if (action.Relationships.Count > 0 && actionString == "Store")
            {
                HandleStore(action.RelationshipsAsThings[0], parameters, moduleQuery);
            }
            else if (action.Relationships.Count > 0 && actionString.Substring(0, 4) == "List")
            {
                HandleListing(actionString.Substring(4), action.RelationshipsAsThings[0], parameters, moduleQuery);
            }
            else if (action.Relationships.Count > 0 && actionString.Substring(0, 5) == "Query")
            {
                HandleQuery(actionString.Substring(5), action.RelationshipsAsThings[0], parameters, moduleQuery);
            }
            else if (parameters.Count > 0 && actionString.Substring(0, 4) == "Wiki")
            {
                WikiDataQuery(actionString, parameters);
            }

        }

        private void HandleQuery(string v, Thing thing, List<Thing> parameters, ModuleQuery moduleQuery)
        {
            if (v == "WikiColor") QueryWikiColor(thing, parameters, moduleQuery);
        }

        private async void QueryWikiColor(Thing thing, List<Thing> parameters, ModuleQuery moduleQuery)
        {
            Thing target = moduleQuery.FindObjectMostCenter();
            if (target == null)
            {
                return; // Should say "I don't see anything"
            }

            Thing color = (target.GetRelationshipsWithAncestor(UKS.GetOrAddThing("col", "Property"))[0].T as Thing);
            List<string> colorValue = new();

            HSLColor hsl = new();
            hsl.hue = ((HSLColor)color.Children[0].V).hue - 10;
            hsl.saturation = 1f;
            hsl.luminance = 0.5f;
            for (int i = 0; i < 5; i++)
            {
                Debug.WriteLine(hsl.ToColor().ToString().Substring(3));
                colorValue.Add(hsl.ToColor().ToString().Substring(3));
                hsl.hue += 5;
            }
            Task<string> result = ModuleWikidata.GetColorFromHex(colorValue);
            while (!result.IsCompleted) { }
            string colorString = await result;
            Thing currentVerbalResponse = UKS.GetOrAddThing("CurrentVerbalResponse", "Attention");
            currentVerbalResponse.RemoveAllRelationships();
            if (colorString == "")
            {
                foreach (Thing word in UKS.Labeled("resIDoNotKnowThisColor").RelationshipsAsThings)
                {
                    currentVerbalResponse.AddRelationship(word);
                }
            }
            else
            {
                foreach (Thing word in UKS.Labeled("resIThinkThisColorIs").RelationshipsAsThings)
                {
                    currentVerbalResponse.AddRelationship(word);
                }
                Thing wordThing = new();
                wordThing.V = colorString;
                currentVerbalResponse.AddRelationship(wordThing);
            }
        }

        private void HandleListing(string actionString, Thing thing, List<Thing> parameters, ModuleQuery moduleQuery)
        {
            if (actionString == "Children") ListChildren(thing, parameters, moduleQuery);
            if (actionString == "Objects") ListObjects(thing, parameters);
            if (actionString.StartsWith("ObjectsWithRelation")) ListObjectsWithRelation(parameters, moduleQuery);
        }

        private void ListObjectsWithRelation(List<Thing> parameters, ModuleQuery moduleQuery)
        {
            Thing relation = new();
            int count = -1;
            Thing target;
            if (parameters[1].Label.StartsWith("number"))
            {
                relation = UKS.Labeled("Has");
                count = (int)parameters[1].V;
                target = UKS.Labeled(parameters[2].RelationshipsAsThings[0].V.ToString());
            }
            else
            {
                string relString = parameters[0].RelationshipsAsThings[0].V.ToString();
                relation = UKS.Labeled(char.ToUpper(relString[0]) + relString[1..]);
                target = UKS.Labeled(parameters[1].RelationshipsAsThings[0].V.ToString());
            }


            Thing currentVerbalResponse = UKS.GetOrAddThing("CurrentVerbalResponse", "Attention");
            currentVerbalResponse.RemoveAllRelationships();

            if (relation == null)
            {
                foreach (Thing word in UKS.Labeled("resIDoNotKnowAbout").RelationshipsAsThings)
                {
                    currentVerbalResponse.AddRelationship(word);
                }
                currentVerbalResponse.AddRelationship(parameters[0].RelationshipsAsThings[0]);
            }
            else
            {
                List<Thing> objects = moduleQuery.QueryObjectsWithRelation(relation, count, target);
                foreach (Thing thing in objects)
                {
                    if (thing == objects.Last() && thing != objects.First())
                        currentVerbalResponse.AddRelationship(UKS.Labeled("wAnd"));
                    Thing objectLabel = new();
                    objectLabel.V = thing.Label;
                    currentVerbalResponse.AddRelationship(objectLabel);
                    if (thing != objects.Last()) currentVerbalResponse.AddRelationship(UKS.Labeled("puncComma"));
                }
                if (objects.Count == 0) currentVerbalResponse.AddRelationship(UKS.Labeled("wNothing"));
                Thing relationTargetWord = new();
                relationTargetWord.V = relation.Label.ToLower();
                if (count != -1) relationTargetWord.V += " " + count;
                if (target == null)
                {
                    if (count == -1) relationTargetWord.V += " " + parameters[1].RelationshipsAsThings[0].V.ToString();
                    else relationTargetWord.V += " " + parameters[2].RelationshipsAsThings[0].V.ToString();
                }
                else relationTargetWord.V += " " + target.Label.ToLower();
                currentVerbalResponse.AddRelationship(relationTargetWord);
            }
        }

        private async void ListObjects(Thing thing, List<Thing> parameters)
        {
            string color = "";
            Thing currentVerbalResponse = UKS.GetOrAddThing("CurrentVerbalResponse", "Attention");
            currentVerbalResponse.RemoveAllRelationships();
            if (parameters.Count == 0)
            {
                foreach (Thing wordThing in UKS.Labeled("resSomethingWentWrong").RelationshipsAsThings)
                {
                    currentVerbalResponse.AddRelationship(wordThing);
                }
                return;
            }
            foreach (Thing colorThing in parameters[0].RelationshipsAsThings) color += colorThing.V + " ";
            Task<List<String>> result = ModuleWikidata.GetLabelsByColorName(color.Trim());
            while (!result.IsCompleted) { }
            List<string> labels = await result;

            if (labels.Count == 0)
            {
                foreach (Thing word in UKS.GetOrAddThing("resICouldNotFindAny", "Response").RelationshipsAsThings) currentVerbalResponse.AddRelationship(word);
                foreach (Thing colorThing in parameters[0].RelationshipsAsThings)
                {
                    currentVerbalResponse.AddRelationship(colorThing);
                }
                currentVerbalResponse.AddRelationship(UKS.GetOrAddThing("wThings", "Word"));
                return;
            }
            for (int i = 0; i < labels.Count; i++)
            {
                Thing wordThing = new();
                string word = labels[i];
                if (i != labels.Count - 1) word += ',';
                wordThing.V = word;
                currentVerbalResponse.AddRelationship(wordThing);
            }
            if (labels.Count == 1) currentVerbalResponse.AddRelationship(UKS.Labeled("wIs"));
            else currentVerbalResponse.AddRelationship(UKS.Labeled("wAre"));
            foreach (Thing colorThing in parameters[0].RelationshipsAsThings)
            {
                currentVerbalResponse.AddRelationship(colorThing);
            }
        }

        private void WikiDataQuery(string property, List<Thing> parameters)
        {
            property = property.Substring(8);
            ModuleWikidata parent = (ModuleWikidata)FindModule("Wikidata");
            string itemName = (parameters[2].Relationships[0].T as Thing).V.ToString();
            Thing item = parent.SetItemInUKS(itemName, property);
            parent.GetItemAndPropsFromURL(item, property);
            Thing response = UKS.GetOrAddThing("CurrentVerbalResponse", "Attention");
            string theWord = "w" + char.ToUpper(itemName[0]) + itemName.Substring(1);

            response.AddRelationship(UKS.Labeled("wA"));
            response.AddRelationship(UKS.GetOrAddThing(theWord, UKS.Labeled("Word")));
            response.AddRelationship(UKS.Labeled("wIs"));
        }

        private void CenterView()
        {
            ModulePodInterface pi = (ModulePodInterface)FindModule("PodInterface");

        }

        private void ListChildren(Thing thing, List<Thing> parameters, ModuleQuery moduleQuery)
        {
            int count = 0;
            int numItems = 3;
            Thing currentVerbalResponse = UKS.GetOrAddThing("CurrentVerbalResponse", "Attention");
            currentVerbalResponse.RemoveAllRelationships();
            if (parameters.Count == 0) return;
            Thing objectThing = moduleQuery.FindObjectFromReference(parameters[1]);
            if (objectThing == null)
            {
                foreach (Thing w in UKS.Labeled("resIDoNotKnowAbout").RelationshipsAsThings) currentVerbalResponse.AddRelationship(w);
                if (parameters.Count > 3)
                    foreach (Thing r in parameters[4].RelationshipsAsThings) currentVerbalResponse.AddRelationship(r);
                return;
            }

            numItems = numItems < objectThing.Children.Count ? numItems : objectThing.Children.Count;
            foreach (Thing t in objectThing.Children)
            {
                if (count >= numItems) break;
                Thing childLabel = new();
                childLabel.V = t.Label.Replace("-", " ", StringComparison.Ordinal);
                if (count < numItems - 2) childLabel.V += ",";
                if (count == numItems - 2) childLabel.V += " and";
                count++;
                currentVerbalResponse.AddRelationship(childLabel);
            }
        }

        private void HandleStore(Thing action, List<Thing> parameters, ModuleQuery moduleQuery)
        {
            if (parameters.Count < 3) return;
            if (action.Label == "decYouAreAtLandmark")
            {
                NameLandmark(parameters[2]);
                return;
            }
            if (action.Label == "dirReplaceXWithY")
            {
                StoreReplacement(parameters);
                return;
            }
            if (action.Label == "paramRelation")
            {
                ModuleObject obj = (ModuleObject)FindModule(typeof(ModuleObject));
                if (obj == null) return;
                StoreRelation(parameters, moduleQuery);
                obj.ConnectedRelationshipCheck();
                return;
            }
            if (action.Label == "paramHasCount")
            {
                ModuleObject obj = (ModuleObject)FindModule(typeof(ModuleObject));
                if (obj == null) return;
                StoreHasCount(parameters, moduleQuery);
                return;
            }

            Thing objectReference = parameters[0];
            Thing parentReference = parameters[2];
            Thing sourceThing = moduleQuery.FindObjectFromReference(objectReference);
            Thing parentThing = moduleQuery.FindObjectFromReference(parentReference);
            ModuleObject x = (ModuleObject)FindModule(typeof(ModuleObject));
            if (sourceThing == null)
            {
                if (objectReference.V?.ToString() == "AttentionObject")
                {
                    Thing currentVerbalResponse = UKS.GetOrAddThing("CurrentVerbalResponse", "Attention");
                    currentVerbalResponse.Children.Clear();
                    foreach (Thing wordThing in UKS.Labeled("resIDontSeeAnything").RelationshipsAsThings)
                    {
                        currentVerbalResponse.AddRelationship(wordThing);
                    }
                    return;
                }
                string objectLabel = "";
                foreach (Thing t in objectReference.RelationshipsAsThings) objectLabel += t.V.ToString() + "-";
                objectLabel = objectLabel.Substring(0, objectLabel.Length - 1);
                sourceThing = UKS.GetOrAddThing(objectLabel, "Object");
                foreach (Thing t in objectReference.RelationshipsAsThings) t.AddRelationship(sourceThing);
            }

            if (action.Label == "wNot")
            {
                ForgetStore(moduleQuery, sourceThing, parentThing, x);
            }
            else if (action.Label == "vIsA" && parameters[2].Label.StartsWith("ObjectReference"))
            {
                string parentLabel = ExtractLabelObjectReference(parentReference);
                if (sourceThing.HasAncestor(UKS.Labeled("MentalModel")))
                {
                    moduleQuery.CreateChildParentRelationship(sourceThing, parentLabel);
                    if (parentThing != null)
                        if (parentThing.Parents.Count > 0)
                        {
                            foreach (Thing P in parentThing.Parents)
                            {
                                if (P != UKS.Labeled("Object"))
                                    x.AddNewParentRelationship(parentThing.Parents[0], parentThing);
                            }
                        }
                }
                else
                {
                    //String objectLabel = sourceThing.Label;
                    //moduleQuery.AddParentToObject(parentLabel, objectLabel);
                    var z = UKS.Query(parameters[2]);
                    List<string> descriptors = new();
                    if (UKS.Labeled(parentLabel) == null)
                    {
                        parentLabel = GetLabelFromWord(z.Last().T.Label);
                    for (int i = 0; i < z.Count - 1; i++) descriptors.Add(GetLabelFromWord(z[i].T.Label.ToString()));
                    }
                    string GetLabelFromWord(string word)
                    {
                        string retVal = word.Substring(1).ToLower();
                        if (retVal == "object") retVal = "Object";
                        return retVal;
                    }
                   
                    Relationship r = UKS.AddStatement(sourceThing, "is-a", parentLabel, null, null, descriptors);
                    ModuleObject mObject = (ModuleObject)FindModule(typeof(ModuleObject));
                    mObject.UnBubbleProperties(r.source);
                    mObject.BubbleProperties(r.source);
                }
                foreach (Thing reference in parentReference.RelationshipsAsThings) reference.AddRelationWithoutDuplicate(UKS.Labeled(parentLabel));
            }
            else
            {
                // Relationship
                string parentLabel = "";
                if (parameters.Count == 4)
                {
                    Thing currentVerbalResponse = UKS.GetOrAddThing("CurrentVerbalResponse", "Attention");
                    currentVerbalResponse.Children.Clear();
                    foreach (Thing wordThing in UKS.Labeled("resSomethingWentWrong").RelationshipsAsThings)
                    {
                        currentVerbalResponse.AddRelationship(wordThing);
                    }
                    return;
                }

                //parentLabel = ExtractLabelObjectReference(parameters[2]);
                //Thing relationshipType = UKS.GetOrAddThing(action.Label.Substring(1), "Relationship");
                //if (parentThing == null)
                //{
                //    parentThing = UKS.GetOrAddThing(parentLabel, "Object");
                //    foreach (Thing t in parentReference.RelationshipsAsThings) t.AddRelationship(parentThing);
                //}
                Thing target = parameters[2];
                List<string> targetProperties = new();
                for (int i = 0; i < target.Relationships.Count - 1; i++)
                    targetProperties.Add(target.Relationships[i].T.Label.Substring(1).ToLower());
                string targetString = target.Relationships.Last().T.Label.Substring(1).ToLower();
                Relationship rel = UKS.AddStatement(sourceThing, action.Label.Substring(1), targetString, null, null, targetProperties);

                //if (parentThing.Relationships.Count == 0)
                //{
                //moduleQuery.ExtractProperties(sourceThing, parentThing);
                //}
                //else
                //{
                //    moduleQuery.PairProperties(sourceThing, parentLabel);
                //}
            }
            ModulePodAudio mpa = (ModulePodAudio)FindModule("PodAudio");
            if (mpa != null) mpa.PlaySoundEffect("ConfirmationChirpDownsampled.wav");
        }

        private void StoreHasCount(List<Thing> parameters, ModuleQuery moduleQuery)
        {
            if (parameters.Count != 4 || parameters[2].V == null)
            {
                Thing currentVerbalResponse = UKS.GetOrAddThing("CurrentVerbalResponse", "Attention");
                currentVerbalResponse.Children.Clear();
                foreach (Thing wordThing in UKS.Labeled("resSomethingWentWrong").RelationshipsAsThings)
                {
                    currentVerbalResponse.AddRelationship(wordThing);
                }
                return;
            }
            Thing source = moduleQuery.FindObjectFromReference(parameters[0]);
            int count = (int)parameters[2].V;
            Thing relationshipType = UKS.GetOrAddThing("Has", "Relationship");
            Thing target = moduleQuery.FindObjectFromReference(parameters[3]);

            if (source == null)
            {
                string sourceLabel = ExtractLabelObjectReference(parameters[0]);
                source = UKS.GetOrAddThing(sourceLabel, "Object");
                foreach (Thing t in parameters[0].RelationshipsAsThings) t.AddRelationship(source);
            }

            if (target == null)
            {
                string targetLabel = ExtractLabelObjectReference(parameters[3]);
                target = UKS.GetOrAddThing(targetLabel, "Object");
                foreach (Thing t in parameters[3].RelationshipsAsThings) t.AddRelationship(target);
            }
            if (UKS.Labeled("number") == null) UKS.SetupNumbers();
            Thing countThing = UKS.GetOrAddThing(count.ToString(), "number", count);

            Relationship r = UKS.AddStatement(source, relationshipType, target, null, new List<Thing> { countThing });

            ModulePodAudio mpa = (ModulePodAudio)FindModule("PodAudio");
            if (mpa != null) mpa.PlaySoundEffect("ConfirmationChirpDownsampled.wav");
        }

        private void StoreRelation(List<Thing> parameters, ModuleQuery moduleQuery)
        {
            if (parameters.Count != 4)
            {
                Thing currentVerbalResponse = UKS.GetOrAddThing("CurrentVerbalResponse", "Attention");
                currentVerbalResponse.Children.Clear();
                foreach (Thing wordThing in UKS.Labeled("resSomethingWentWrong").RelationshipsAsThings)
                {
                    currentVerbalResponse.AddRelationship(wordThing);
                }
                return;
            }
            Thing source = moduleQuery.FindObjectFromReference(parameters[0]);
            Thing relationshipType = UKS.GetOrAddThing(parameters[2].RelationshipsAsThings[0].V.ToString(), "Relationship");
            Thing target = moduleQuery.FindObjectFromReference(parameters[3]);

            if (source == null)
            {
                string sourceLabel = ExtractLabelObjectReference(parameters[0]);
                source = UKS.GetOrAddThing(sourceLabel, "Object");
                foreach (Thing t in parameters[0].RelationshipsAsThings) t.AddRelationship(source);
            }

            if (target == null)
            {
                string targetLabel = ExtractLabelObjectReference(parameters[3]);
                target = UKS.GetOrAddThing(targetLabel, "Object");
                foreach (Thing t in parameters[2].RelationshipsAsThings) t.AddRelationship(target);
            }

            UKS.AddStatement(source, relationshipType, target);

            ModulePodAudio mpa = (ModulePodAudio)FindModule("PodAudio");
            if (mpa != null) mpa.PlaySoundEffect("ConfirmationChirpDownsampled.wav");
        }

        private static string ExtractLabelObjectReference(Thing parentReference)
        {
            if (parentReference == null) return null;
            string parentLabel = "";
            foreach (Thing t in parentReference.RelationshipsAsThings)
                if (t.Children.Count > 0)
                {
                    if (t.Children[0].V != null)
                        parentLabel += t.Children[0].V.ToString() + "-";
                    else
                        parentLabel += t.Children[0].Label + "-";
                }
                else
                {
                    if (t.V != null)
                        parentLabel += t.V.ToString() + "-";
                    else
                        parentLabel += t.Label + "-";
                }
            if (parentLabel.Length > 0) parentLabel = parentLabel.Substring(0, parentLabel.Length - 1);
            return parentLabel;
        }

        private void ForgetStore(ModuleQuery moduleQuery, Thing objectThing, Thing parentThing, ModuleObject x)
        {
            // parentLabel = ExtractLabelObjectReference(parentThing);
            if (objectThing.RelationshipsAsThings.Contains(parentThing))
            {
                objectThing.RemoveRelationship(parentThing);
                //x.UnBubbleProperty(parentThing);
                return;
            }
            if (UKS.Labeled(parentThing.Label) != null && objectThing.Parents.Contains(UKS.Labeled(parentThing.Label)))
            {
                moduleQuery.RemoveFromHierarchy(objectThing, UKS.Labeled(parentThing.Label));
                //x.UnBubbleProperty(parentThing);
                return;
            }
            else//assume it's a property
            {
                Thing par = UKS.Labeled(parentThing.Label);
                if (par != null)
                {
                    if (par.Relationships.Count == 1)
                    {
                        moduleQuery.NotProperty(objectThing, par.RelationshipsAsThings[0]);
                        x.AddAllMatches(objectThing);
                        return;
                    }
                }
            }
        }

        private void StoreReplacement(List<Thing> parameters)
        {
            Thing replacement = UKS.GetOrAddThing("replacement*", "ReplacementPhrase");
            string rep0 = "";
            parameters[0].RelationshipsAsThings.ForEach(word => rep0 += " " + word.V);
            rep0 = rep0.Trim();
            string rep1 = "";
            parameters[1].RelationshipsAsThings.ForEach(word => rep1 += " " + word.V);
            rep1 = rep1.Trim();
            replacement.V = (rep0, rep1);
        }

        private void NameLandmark(Thing landmarkName)
        {
            ModuleSituation ms = (ModuleSituation)FindModule("Situation");
            Thing landmark = ms.curClosestLM;
            Thing currentVerbalResponse = UKS.GetOrAddThing("CurrentVerbalResponse", "Attention");
            currentVerbalResponse.RemoveAllRelationships();
            if (landmark == null)
            {
                foreach (Thing t in UKS.Labeled("resIDontSeeAnything").RelationshipsAsThings)
                {
                    currentVerbalResponse.AddRelationship(t);
                }
            }
            else
            {
                string name = "";
                foreach (Thing n in landmarkName.RelationshipsAsThings) name += "_" + n.V;
                landmark.Label = "LM" + name;
                currentVerbalResponse.AddRelationship(UKS.Labeled("wOK"));
            }
        }

        private void HandleAction(Thing action, List<Thing> parameters, ModuleQuery moduleQuery)
        {
            GetUKS();
            String commandString = action.Label.Substring(3);
            if (commandString == "Stop")
            {
                ModulePodInterface podInterface = (ModulePodInterface)FindModule(typeof(ModulePodInterface));
                if (podInterface == null) return;
                podInterface.CommandStop();
                ModuleHappy happy = (ModuleHappy)FindModule(typeof(ModuleHappy));
                if (happy == null) return;
                happy.decreaseHappiness();
                return;
            }
            else if (commandString == "DrawAShape")
            {
                ModuleCommandQueue CQ = (ModuleCommandQueue)FindModule(typeof(ModuleCommandQueue));
                if (CQ == null || parameters.Count == 1) return;
                string word = parameters[1].ToString();
                string[] x = word.Split('w');
                string[] w = x[1].Split(',');
                //word = "{wShape,}" need to trim off the extra stuff
                //string[] w = word.Split('w');
                if (w[0] == "Shape")
                {
                    Random r = new();
                    int i = r.Next(0, 2);
                    if (i == 0)
                        CQ.executeCommandQueue("Triangle");
                    else if (i == 1)
                        CQ.executeCommandQueue("Square");
                    else if (i == 2)
                        CQ.executeCommandQueue("Circle");
                    return;
                }
                CQ.executeCommandQueue(w[0]);
            }
            else if (commandString == "WriteYourName")
            {
                ModuleCommandQueue CQ = (ModuleCommandQueue)FindModule(typeof(ModuleCommandQueue));
                if (CQ == null) return;
                CQ.executeCommandQueue("SallieName");
            }
            else if (commandString == "TakeAPicture")
            {
                ModuleUserInterface ui = (ModuleUserInterface)FindModule(typeof(ModuleUserInterface));
                if (ui == null) return;
                ui.SetSaveImage(true);
            }
            else if (commandString == "FindObject")
            {
                if (parameters.Count > 1) FindObject(parameters[1]);
            }
            else if (commandString == "Good")
            {
                ModuleHappy happy = (ModuleHappy)FindModule(typeof(ModuleHappy));
                if (happy == null) return;
                happy.increaseHappiness();
            }
            else if (commandString == "YourNameIsWakeword")
            {
                if (parameters.Count == 2)
                {
                    Thing currentVerbalResponse = UKS.GetOrAddThing("CurrentVerbalResponse", "Attention");
                    currentVerbalResponse.RemoveAllRelationships();
                    foreach (Thing word in UKS.Labeled("resThatIsNotAValidWakeWord").RelationshipsAsThings)
                    {
                        currentVerbalResponse.AddRelationship(word);
                    }
                    return;
                }
                Thing wakeWord = UKS.GetOrAddThing("WakeWord", "Self");
                string oldWakeword = wakeWord.V.ToString();
                wakeWord.V = parameters[2].Label;
                ModuleSpeechInPlus speechInPlus = (ModuleSpeechInPlus)FindModule(typeof(ModuleSpeechInPlus));
                if (speechInPlus != null) speechInPlus.Initialize();
                else wakeWord.V = oldWakeword;
            }
            else if (commandString == "GoToLandmark")
            {
                ModuleSituationNavigation msn = (ModuleSituationNavigation)FindModule("SituationNavigation");
                ModuleSituation moduleSituation = (ModuleSituation)FindModule(typeof(ModuleSituation));
                if (msn == null) return;

                Thing landmark = parameters[0];
                string landmarkName = "LM";
                foreach (Thing w in landmark.RelationshipsAsThings) landmarkName += "_" + w.V;

                moduleSituation.speechResponseNeeded = true;
                msn.EnterNavigationMode(landmarkName);
            }
            else if (commandString == "Dance")
            {
                ModuleCommandQueue mcq = (ModuleCommandQueue)FindModule(typeof(ModuleCommandQueue));
                ModuleHappy mh = (ModuleHappy)FindModule(typeof(ModuleHappy));
                if (mh != null) mh.increaseHappiness();
                if (mcq == null) return;
                mcq.executeCommandQueue("Dance");
            }
            else if (commandString == "TurnAround")
            {
                ModulePodInterface mpi = (ModulePodInterface)FindModule(typeof(ModulePodInterface));
                mpi.CommandTurn(Angle.FromDegrees(180));
            }
            else if (commandString == "LookparamRelation")
            {
                ModulePodInterface mpi = (ModulePodInterface)FindModule(typeof(ModulePodInterface));
                ModulePodInterface podInt = (ModulePodInterface)FindModule("PodInterface");
                if (parameters.Count == 0) return;
                String dir = parameters[0].RelationshipsAsThings[0].V.ToString();
                if (mpi == null) return;
                if (dir == "right")
                {
                    mpi.CommandPan(Angle.FromDegrees(-45), true);
                }
                else if (dir == "left")
                {
                    mpi.CommandPan(Angle.FromDegrees(45), true);
                }
                else if (dir == "up")
                {
                    mpi.CommandTilt(Angle.FromDegrees(20), true);
                }
                else if (dir == "down")
                {
                    mpi.CommandTilt(Angle.FromDegrees(-20), true);
                }
                else if (dir == "forward")
                {
                    podInt.IsPodActive();
                    podInt?.ResetCamera();
                    podInt?.UpdateSallieSelf();
                }
            }
            else if (commandString == "LookForward")
            {
                ModulePod pod = (ModulePod)FindModule("Pod");
                pod.centerCam();
            }
            else
            {
                Thing command = UKS.GetOrAddThing(commandString, UKS.Labeled("Attention"));
                if (command.Label == "GoTo")
                {
                    if (parameters.Count == 0)
                    {
                        UKS.DeleteThing(command);
                        return;
                    }
                    command.AddRelationship(moduleQuery.FindObjectFromReference(parameters[1]));
                }
            }
        }

        private void FindObject(Thing objectReference)
        {
            string objectLabel = "";
            foreach (Thing t in objectReference.RelationshipsAsThings) objectLabel += t.V.ToString() + "-";
            objectLabel = objectLabel.Substring(0, objectLabel.Length - 1);
            Thing objectThing = UKS.Labeled(objectLabel);
            if (objectThing == null)
            {
                Thing currentVerbalResponse = UKS.GetOrAddThing("CurrentVerbalResponse", "Attention");
                currentVerbalResponse.RemoveAllRelationships();
                foreach (Thing t in UKS.Labeled("resIDoNotKnowAbout").RelationshipsAsThings) currentVerbalResponse.AddRelationship(t);
                Thing thing = new Thing();
                thing.V = objectLabel;
                currentVerbalResponse.AddRelationship(thing);
                return;
            }

            IList<Thing> physicalObjects = objectThing.GetRelationshipByWithAncestor(UKS.Labeled("MentalModel")).ConvertAll(l => l.source as Thing);
            if (physicalObjects.Count == 0)
            {
                Thing currentVerbalResponse = UKS.GetOrAddThing("CurrentVerbalResponse", "Attention");
                currentVerbalResponse.RemoveAllRelationships();
                foreach (Thing t in UKS.Labeled("resIDoNotSeeA").RelationshipsAsThings) currentVerbalResponse.AddRelationship(t);
                Thing thing = new Thing();
                thing.V = objectLabel;
                currentVerbalResponse.AddRelationship(thing);
                return;
            }
            physicalObjects = UKS.OrderByDistance(physicalObjects);
            Thing command = UKS.GetOrAddThing("GoTo", UKS.Labeled("Attention"));
            command.AddRelationship(physicalObjects[0]);
        }

        private async void HandleResponse(Thing response, List<Thing> parameters, ModuleQuery moduleQuery)
        {
            Thing currentVerbalResponse = UKS.GetOrAddThing("CurrentVerbalResponse", "Attention");
            currentVerbalResponse.RemoveAllRelationships();
            bool isRelation = false;
            try
            {
                foreach (Thing t in response.RelationshipsAsThings)
                {
                    if (t.Label.StartsWith("Verb"))
                    {
                        currentVerbalResponse.AddRelationship(t.RelationshipsAsThings[0]);
                    }
                    else if (t.HasAncestor(UKS.Labeled("Word")))
                    {
                        currentVerbalResponse.AddRelationship(t);
                    }
                    else if (t.Label == "paramObjectReference" || t.Label == "paramProperty")
                    {
                        Thing parameter = parameters[1];
                        parameters.RemoveAt(0);
                        Thing parentOrDescription;
                        if (parameter.HasAncestorLabeled("paramRelation"))
                        {
                            Thing relation = parameter;
                            isRelation = true;

                            Thing objectReference = parameters[1];
                            parameters.RemoveAt(0);
                            parentOrDescription = moduleQuery.findObjectByRelation(relation, objectReference);
                            parameter = objectReference;
                        }
                        else parentOrDescription = moduleQuery.DescribeObjectReference(parameter);
                        if (parentOrDescription == null)
                        {
                            currentVerbalResponse.RemoveAllRelationships();
                            if (parameter.V.ToString() == "AttentionObject")
                            {
                                foreach (Thing word in UKS.Labeled("resIDontSeeAnything").RelationshipsAsThings)
                                {
                                    currentVerbalResponse.AddRelationship(word);
                                }
                            }
                            else
                            {
                                foreach (Thing word in UKS.Labeled("resIDoNotKnowAbout").RelationshipsAsThings)
                                {
                                    currentVerbalResponse.AddRelationship(word);
                                }
                                currentVerbalResponse.AddRelationship(UKS.Labeled("wA"));
                                foreach (Thing property in parameter.RelationshipsAsThings)
                                {
                                    currentVerbalResponse.AddRelationship(property);
                                }
                            }
                        }
                        else if (parentOrDescription.V.ToString() == "NoRelation")
                        {
                            currentVerbalResponse.RemoveAllRelationships();
                            foreach (Thing word in UKS.Labeled("resIDontSeeAnything").RelationshipsAsThings)
                            {
                                currentVerbalResponse.AddRelationship(word);
                            }
                        }
                        else if (parentOrDescription.V.ToString() == "No Description")
                        {
                            currentVerbalResponse.RemoveAllRelationships();
                            foreach (Thing word in UKS.Labeled("resIDoNotKnowAbout").RelationshipsAsThings)
                            {
                                currentVerbalResponse.AddRelationship(word);
                            }
                            if (isRelation)
                            {
                                currentVerbalResponse.AddRelationship(UKS.Labeled("wThat"));
                            }
                            else
                            {
                                foreach (Thing property in parameter.RelationshipsAsThings)
                                {
                                    currentVerbalResponse.AddRelationship(property);
                                }
                            }
                        }
                        else currentVerbalResponse.AddRelationship(parentOrDescription);
                    }
                    else if (t.Label == "paramFeel")
                    {
                        ModuleHappy happy = (ModuleHappy)FindModule(typeof(ModuleHappy));
                        if (happy == null) return;

                        Thing mood = new Thing();
                        mood.V = happy.getMood();
                        currentVerbalResponse.AddRelationship(mood);
                    }
                    else if (t.Label == "paramCount")
                    {
                        if (parameters.Count == 1)
                        {
                            Thing reference = parameters[0];
                            parameters.RemoveAt(0);
                            Thing count = moduleQuery.CountObjects(reference);
                            currentVerbalResponse.AddRelationship(count);
                        }
                        else
                        {
                            Thing source = moduleQuery.FindObjectFromReference(parameters[1]);
                            Thing target = moduleQuery.FindObjectFromReference(parameters[0]);
                            Thing count = moduleQuery.HasHowMany(source, target);
                            currentVerbalResponse.AddRelationship(count);
                        }

                    }
                    else if (t.Label == "paramLandmark")
                    {
                        ModuleSituation ms = (ModuleSituation)FindModule("Situation");
                        Thing landmark = ms.curClosestLM;
                        if (landmark == null)
                        {
                            currentVerbalResponse.RemoveAllRelationships();
                            foreach (Thing w in UKS.Labeled("resIAmNotSure").RelationshipsAsThings)
                            {
                                currentVerbalResponse.AddRelationship(w);
                            }
                        }
                        else
                        {
                            List<string> words = landmark.Label.Split("_").ToList();
                            foreach (string word in words)
                            {
                                if (word == "LM") continue;
                                Thing wordThing = new();
                                wordThing.V = word;
                                currentVerbalResponse.AddRelationship(wordThing);
                            }
                        }
                    }
                    else if (t.Label == "paramEnds")
                    {
                        Thing Time = new Thing();
                        DateTime t1 = DateTime.Now;
                        //now
                        DateTime t2 = new DateTime(t1.Year, t1.Month, t1.Day, 17, 0, 0);
                        //today at 5 pm
                        TimeSpan diff = t2.Subtract(t1);
                        float hours = (float)diff.Hours;
                        float minutes = (float)diff.Minutes;
                        if (hours <= 0 && minutes <= 0)
                        {
                            currentVerbalResponse.RemoveAllRelationships();
                            Time.V = "now, go home already";
                            currentVerbalResponse.AddRelationship(Time);
                            return;
                        }
                        if (hours == 0)
                        {
                            Time.V = minutes.ToString();
                            Time.V += " minutes";
                            currentVerbalResponse.AddRelationship(Time);
                            return;
                        }
                        Time.V = hours.ToString();
                        Time.V += " hours and";
                        Time.V += minutes.ToString();
                        Time.V += " minutes";
                        currentVerbalResponse.AddRelationship(Time);
                    }
                    else if (t.Label == "paramStarts")
                    {
                        Thing Time = new Thing();
                        DateTime t1 = DateTime.Now;
                        //now
                        DateTime t2 = new DateTime(t1.Year, t1.Month, t1.Day, 8, 0, 0);
                        //today at 5 pm
                        TimeSpan diff = t1.Subtract(t2);
                        float hours = (float)diff.Hours;
                        float minutes = (float)diff.Minutes;
                        if (hours <= 0 && minutes <= 0)
                        {
                            Time.V = minutes.ToString();
                            Time.V += " minutes early";
                            currentVerbalResponse.AddRelationship(Time);
                            return;
                        }
                        if (hours == 0)
                        {
                            Time.V = minutes.ToString();
                            Time.V += " minutes late";
                            currentVerbalResponse.AddRelationship(Time);
                            return;
                        }
                        Time.V = hours.ToString();
                        Time.V += " hours and ";
                        Time.V += minutes.ToString();
                        Time.V += " minutes late";
                        currentVerbalResponse.AddRelationship(Time);
                    }
                    else if (t.Label == "paramDate")
                    {
                        Thing Date = new Thing();
                        Date.V = DateTime.Now.ToString("D");
                        currentVerbalResponse.AddRelationship(Date);
                    }
                    else if (t.Label == "paramPrevDate")
                    {
                        Thing Date = new Thing();

                        Date.V = DateTime.Now.AddDays(-1).ToString("D");
                        currentVerbalResponse.AddRelationship(Date);
                    }
                    else if (t.Label == "paramNextDate")
                    {
                        Thing Date = new Thing();
                        Date.V = DateTime.Now.AddDays(1).ToString("D");
                        currentVerbalResponse.AddRelationship(Date);
                    }
                    else if (t.Label == "paramTime")
                    {
                        Thing Time = new Thing();
                        Time.V = DateTime.Now.ToString("t");
                        currentVerbalResponse.AddRelationship(Time);
                    }
                    else if (t.Label == "paramColorReference")
                    {
                        Thing colorReference = parameters[2];
                        foreach (Thing wordThing in colorReference.RelationshipsAsThings)
                            currentVerbalResponse.AddRelationship(wordThing);
                    }
                    else if (t.Label == "paramColorDescription")
                    {
                        string color = "";
                        foreach (Thing colorThing in parameters[2].RelationshipsAsThings) color += colorThing.V + " ";
                        Task<string> result = ModuleWikidata.GetColorDescription(color.Trim());
                        while (!result.IsCompleted) { }
                        string description = await result;
                        if (description.Equals(""))
                        {
                            currentVerbalResponse.RemoveAllRelationships();
                            foreach (Thing word in UKS.Labeled("resIDoNotKnowAbout").RelationshipsAsThings)
                            {
                                currentVerbalResponse.AddRelationship(word);
                            }
                            foreach (Thing wordThing in parameters[2].RelationshipsAsThings)
                            {
                                currentVerbalResponse.AddRelationship(wordThing);
                            }
                        }
                        foreach (string word in description.Split(" "))
                        {
                            Thing wordThing = new();
                            wordThing.V = word;
                            currentVerbalResponse.AddRelationship(wordThing);
                        }
                    }
                    else if (t.Label == "paramPhrase")
                    {
                        foreach (Thing wordThing in parameters[0].RelationshipsAsThings)
                        {
                            currentVerbalResponse.AddRelationship(wordThing);
                        }
                    }
                    else if (t.Label.StartsWith("ObjectReference"))
                    {
                        currentVerbalResponse.AddRelationship(UKS.Labeled("wA"));
                        foreach (Thing wordThing in t.RelationshipsAsThings)
                        {
                            currentVerbalResponse.AddRelationship(wordThing);
                        }
                    }
                    else if (t.Label.StartsWith("paramProperties"))
                    {
                        Thing objectThing = null;
                        if (parameters.Count == 0)
                        {
                            currentVerbalResponse.RemoveAllRelationships();
                            foreach (Thing wordThing in UKS.Labeled("resSomethingWentWrong").RelationshipsAsThings)
                            {
                                currentVerbalResponse.AddRelationship(wordThing);
                            }
                            return;
                        }
                        if (parameters[1].Label == "Object")
                        {
                            string label = parameters[1].RelationshipsAsThings[0].V.ToString();
                            objectThing = UKS.Labeled(label);
                        }
                        else if (parameters[1].Label.StartsWith("ObjectReference")) objectThing = moduleQuery.FindObjectFromReference(parameters[1]);

                        if (objectThing == null)
                        {
                            currentVerbalResponse.RemoveAllRelationships();
                            foreach (Thing word in UKS.Labeled("resIDoNotKnowAbout").RelationshipsAsThings)
                            {
                                currentVerbalResponse.AddRelationship(word);
                            }
                            foreach (Thing wordThing in parameters[1].RelationshipsAsThings)
                            {
                                currentVerbalResponse.AddRelationship(wordThing);
                            }
                        }
                        else
                        {
                            Thing objectWords = new Thing();
                            objectWords.V = objectThing.Label;
                            currentVerbalResponse.AddRelationship(objectWords);
                            //List<Relationship> relations = moduleQuery.GetRelationships(objectThing);
                            IList<Relationship> relations = UKS.Query(objectThing);
                            string prevRelation = "";

                            if (relations.Count == 0)
                                currentVerbalResponse.AddRelationship(new Thing() { V = "has no properties." });
                            foreach (Relationship l in relations)
                            {
                                Thing referenceWords = new Thing();
                                if (l == relations.Last() && relations.Count != 1)
                                {
                                    referenceWords.V = "and ";
                                }
                                string relType = l.relType.Label;
                                for (int i = relType.Length - 1; i > 0; i--)
                                {
                                    if (Char.IsDigit(relType[i])) relType = relType.Substring(0, relType.Length - 1);
                                    else break;
                                }
                                if (prevRelation != relType)
                                    referenceWords.V += relType;
                                prevRelation = relType;
                                var z1 = UKS.ResultsOfType(new List<Relationship>() { l }, "number");
                                int count = 0;
                                if (z1.Count > 0 && z1[0].V != null)
                                    count = (int)z1[0].V;
                                if (count > 1)
                                {
                                    referenceWords.V += " " + count;
                                    // Add plural form of word.
                                    IPluralize pluralizer = new Pluralizer();
                                    string plural = pluralizer.Pluralize((l.T as Thing).Label);
                                    referenceWords.V += " " + plural;
                                }
                                else
                                {
                                    referenceWords.V += " " + (l.T as Thing).Label;
                                }

                                if (l != relations.Last())
                                {
                                    referenceWords.V += ",";
                                }

                                currentVerbalResponse.AddRelationship(referenceWords);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                currentVerbalResponse.RemoveAllRelationships();
                foreach (Thing word in UKS.Labeled("resSomethingWentWrong").RelationshipsAsThings)
                {
                    currentVerbalResponse.AddRelationship(word);
                }
            }
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

            MainWindow.ResumeEngine();
        }
    }
}
