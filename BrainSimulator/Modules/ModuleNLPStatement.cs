//
// Copyright (c) FutureAI. All rights reserved.  
// Contains confidential and  proprietary information and programs which may not be distributed without a separate license
//  

using Emgu.CV.Text;
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
    public partial class ModuleNLP : ModuleBase
    {
        //DECLARATION/////////////////////////////////////////////
        private Relationship HandleDeclaration(List<NLPItem> nlpResults, string phrase="", string desiredResult="")
        {
            if (nlpResults.FindFirst(x=>x.lemma == "hasproperty")!= null)
            {
                Relationship r = null;
                UKS.GetOrAddThing("transitive", "Relationship");
                Thing theSource = UKS.Labeled(nlpResults[0].lemma);
                Thing theTarget = UKS.Labeled(nlpResults[2].lemma);
                Thing theRelationship = UKS.Labeled("hasProperty");
                if (theSource != null && theTarget != null && theRelationship != null)
                    r = UKS.AddStatement(theSource, theRelationship,theTarget, null,null,null);
                return r;
            }

            if (!HandleIfClauses(nlpResults, phrase)) return null;

            if (!HandleAndLists(nlpResults, phrase)) return null;

            List<ClauseType> clauses = new();
            //is this a negative statement?
            bool negative = false;
            foreach (NLPItem item in nlpResults)
            {
                if (item.dependency == "neg" ||
                    (item.dependency == "det" && item.lemma == "no"))
                {
                    negative = true;
                    ResultsRemoveAt(nlpResults, item.index);
                    break;
                }
            }

            relPos = -1;
            NLPItem relType = HandleTypeD(nlpResults, out List<string> typeProperties, clauses);
            NLPItem source = HandleSourceD(nlpResults, out List<string> sourcePoperties, clauses);
            NLPItem target = HandleTargetD(nlpResults, out List<string> targetProperties, clauses);

            if (relPos == -1)
            {
                Debug.WriteLine("Declaration has no type");
                return null; //I don't understand
            }
            //special cases for is and is-a compatibility
            if (relType.lemma == "be") relType.lemma = "is";
            if (relType.lemma == "is" && IsNoun(target) && target.dependency == "attr" && target.partOfSpeech != "PROPN") relType.lemma = "is-a";
            if (relType.lemma == "is" && relType.index < nlpResults.Count - 1 && 
                (nlpResults[relType.index + 1]?.lemma == "a" || nlpResults[relType.index + 1]?.lemma == "an")) relType.lemma = "is-a";
            if (relType.lemma == "is" && target?.morph.Contains("Plur") == true) relType.lemma = "is-a";
            if (relType.lemma == "is" && target?.morph.Contains("Plur") == true) relType.lemma = "is-a";
            if (relType.lemma == "is-a")
            {
                //for is-a phrases, if the target is not already known, fire off a web search for it
                Thing targetThing = UKS.Labeled(target.lemma);
                if (targetThing is null)// || targetThing.Parents.FindFirst(x=>x.Label == "Object")!= null)
                {
                    ModuleWords mw = (ModuleWords)FindModule("Words");
                    if (mw != null)
                    {
                        //Words I don't know
                        //TODO: don't look up words you've already tried
                        //TODO: add subjects too if it is not an is-a relationship
                        //Thing words = UKS.GetOrAddThing("WordsIWantToKnow", "Attention");
                        //string word = "w" + char.ToUpper(target.lemma[0]) + target.lemma.Substring(1);
                        //UKS.GetOrAddThing(word, words);
                        ////mw.GetOnlineData(target.lemma);
                    }
                }
            }

            RemoveDeterminants(nlpResults);

            if (negative)
            {
                typeProperties.RemoveAll(x => x == "1");
                typeProperties.Add("not");
                typeProperties.Add("0");
            }
            //add the relationship
            ModuleObject mo = (ModuleObject)FindModule("Object");
            mo.DoPendingRelationship(false);
            r = UKS.AddStatement(source?.lemma, relType.lemma, target?.lemma, sourcePoperties, typeProperties, targetProperties);
            if (r == null)
            {
                OutputReponseString("No information added");
                return r;
            }
            if (r.source != null) r.source.SetFired();
            if (r.target != null) r.target.SetFired();
            if (r.relType != null) r.relType.SetFired();
            //add any dependent clauses
            if (UKS.CheckForInverse(relType.lemma))
            {
                //if the relationship has been inverted, we need to invert the clauses too
                foreach (ClauseType clause in clauses)
                {
                    if (clause.a == AppliesTo.source)
                        clause.a = AppliesTo.target;
                    else if (clause.a == AppliesTo.target)
                        clause.a = AppliesTo.source;
                }
            }
            r.label = phrase;
            foreach (ClauseType clause in clauses)
                r.AddClause(clause.a, clause.clause);

            Relationship pending = mo.GetPendingRelationship();
            if (pending != null)
            {
                string responseString;
                if (pending.source != null)
                {
                    responseString = "Is " + AddArticleIfNeeded(pending.source) + " a " + pending.target.Label + "?";
                }
                else
                {
                    responseString = "What is " + pending.target.Label + "?";
                }
                OutputReponseString(responseString);
                CompareOutputToDesired(responseString, desiredResult);
            }
            return r;
        }
        NLPItem HandleTypeD(List<NLPItem> nlpResults, out List<string> properties, List<ClauseType> clauses)
        {
            properties = new();
            NLPItem si = nlpResults.FindFirst(x => /*x.partOfSpeech == "VERB" || */x.dependency == "ROOT");
            if (si == null)
                //if verbs are in an AND list, only the first is the root
                si = nlpResults.FindFirst(x => x.tag.StartsWith("VB"));

            if (si != null)
                relPos = si.index;
            else return null;

            NLPItem siAux = nlpResults.FindFirst(x => x.partOfSpeech == "AUX");
            if (siAux != null && si != siAux && siAux.lemma != "do" && siAux.head == si.index)
                properties.Add(siAux.lemma);
            List<NLPItem> adverbs = nlpResults.FindAll(x => x.partOfSpeech == "ADV" && x.head==si.index);
            foreach (NLPItem adverb in adverbs)
                properties.Add(adverb.lemma);


            // Special case for "have" check for number words. 
            if (si.lemma == "have")
            {
                string count = "";
                int i = si.index + 1;
                while (i < nlpResults.Count &&
                    (nlpResults[i].partOfSpeech == "NUM" ||
                    (UKS.Labeled(nlpResults[i].text) != null && UKS.Labeled(nlpResults[i].text).HasAncestor(UKS.Labeled("number")))))
                {
                    count += nlpResults[i].lemma + " ";
                    i++;
                }
                if (count == "") //check for plural object
                {
                    NLPItem item = nlpResults.FindFirst(x =>/* x.dependency == "dobj" && */ x.morph != null && x.morph.Contains("Plur"));
                    if (item != null)
                    {
                        count = "some";
                    }
                }
                if (count != "")
                    properties.Add(count.Trim());
                else
                    properties.Add("1");
            }

            return si;
        }
        NLPItem HandleSourceD(List<NLPItem> nlpResults, out List<string> properties, List<ClauseType> clauses)
        {
            properties = new();
            if (relPos == -1) return null;

            NLPItem si = nlpResults.FindFirst(x => x.dependency == "nsubj");
            if (si == null)
                si = nlpResults.FindFirst(x => x.partOfSpeech == "PROPN" && x.index != relPos);
            if (si == null)
                si = nlpResults.FindFirst(x => x.partOfSpeech == "NOUN");
            if (si != null && si.index > relPos && relPos > 0)
                si = nlpResults[relPos - 1];

            if (si != null)
            {
                srcPos = si.index;
                for (int i = 0; i < si.index; i++)
                    if (IsAdjective(nlpResults[i]))
                        properties.Add(nlpResults[i].lemma);
                FindClauses(nlpResults, si.index, relPos, AppliesTo.source, clauses);
            }
            return si;
        }
        NLPItem HandleTargetD(List<NLPItem> nlpResults, out List<string> properties, List<ClauseType> clauses)
        {
            properties = new();
            if (relPos == -1) return null;
            NLPItem si = nlpResults.FindFirst(x => x.index > relPos && IsNoun(x) && x.index != srcPos);
            if (si == null)
                si = nlpResults.FindFirst(x => x.index > relPos && x.dependency == "dobj");
            if (si == null)
                si = nlpResults.FindFirst(x => x.index > relPos && IsAdjective(x));
            if (si == null)
                si = nlpResults.FindFirst(x => x.index > relPos && (x.partOfSpeech.StartsWith("INT") || x.partOfSpeech == "VERB"));
            if (si != null)
            {
                targPos = si.index;
                for (int i = relPos + 1; i < si.index; i++)
                    if (IsAdjective(nlpResults[i]) && nlpResults[i].lemma != "many") //numbers are attached to the reltype, not the target
                        properties.Add(nlpResults[i].lemma);
                if (si.index < nlpResults.Count - 1 &&
                    nlpResults[si.index + 1].lemma == "be")
                {
                    for (int i = si.index + 1; i < nlpResults.Count; i++)
                        if (IsAdjective(nlpResults[i]))
                            properties.Add(nlpResults[i].lemma);
                }
                FindClauses(nlpResults, si.index, nlpResults.Count, AppliesTo.target, clauses);
            }
            return si;
        }

        private bool HandleIfClauses(List<NLPItem> nlpResults, string phrase)
        {
            NLPItem itemIf = nlpResults.FindFirst(x => x.lemma == "if" || x.lemma == "when");
            if (itemIf == null) return true; //there is no if clause
            List<NLPItem> subjects = nlpResults.FindAll(x => x.dependency == "nsubj");
            if (subjects.Count != 2) return true;
            int start1 = subjects[0].index;
            int start2 = subjects[1].index;
            NLPItem temp = nlpResults.FindFirst(x => x.head == start1);
            if (temp != null) start1 = temp.index;
            temp = nlpResults.FindFirst(x => x.head == start2);
            if (temp != null) start2 = temp.index;
            //make a local copy
            List<NLPItem> clause1 = new();
            foreach (NLPItem item in nlpResults)
                clause1.Add(new NLPItem(item));
            for (int i = start2; i < nlpResults.Count; i++)
                ResultsRemoveAt(clause1, start2);

            List<NLPItem> clause2 = new();
            foreach (NLPItem item in nlpResults)
                clause2.Add(new NLPItem(item));
            for (int i = 0; i < start2; i++)
                ResultsRemoveAt(clause2, start1);
            ResultsRemoveAt(clause1, itemIf.index);


            if (itemIf.index < start1) //is the if clause first?
            {
                Relationship ifClause = HandleDeclaration(clause1, "", "");
                Relationship mainClause = HandleDeclaration(clause2, "", "");
                mainClause?.clauses.Add(new ClauseType(AppliesTo.condition, ifClause));
                if (mainClause == null) Debug.WriteLine("Conditional main clause cannot contain and clause");
            }
            else //the if clause comes 2nd
            {
                Relationship ifClause = HandleDeclaration(clause2, "", "");
                Relationship mainClause = HandleDeclaration(clause1, "", "");
                mainClause?.clauses.Add(new ClauseType(AppliesTo.condition, ifClause));
                if (mainClause == null) Debug.WriteLine("Conditional main clause cannot contain and clause");
            }
            return false;
        }

        private void SwapCommasForAndLists(List<NLPItem> list)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                NLPItem item = list[i];
                if (item.lemma == ",")
                {
                    if (i < list.Count - 1)
                        if (list[i + 1].lemma == "and") ResultsRemoveAt(list, i);
                        else item.lemma = "and";
                }
            }
        }
        private bool HandleAndLists(List<NLPItem> nlpResults, string phrase)
        {
            SwapCommasForAndLists(nlpResults);
            //handle AND with just pairs of one-word items
            List<NLPItem> ands = nlpResults.FindAll(x => x.lemma == "and" || x.lemma == "or");
            if (ands.Count == 0) return true;
            List<(int, int)> rangeList = new();
            int prevIndex = ands[0].head;
            int prevIndex1 = nlpResults.FindFirst(x => x.head == prevIndex).index;
            //hack to make "mary can sing and dance work:
            NLPItem can = nlpResults.FindFirst(x => x.lemma == "can");
            if (can != null) { if (can.index > prevIndex1) prevIndex1 = can.index + 1; }

            if (prevIndex1 < prevIndex && nlpResults[prevIndex1].partOfSpeech != "NOUN") prevIndex = prevIndex1;

            foreach (NLPItem itemAnd in ands)
            {
                rangeList.Add((prevIndex, itemAnd.index));
                prevIndex = itemAnd.index + 1;
            }
            //this hack compensates for AND  lists being of different types
            NLPItem final = nlpResults.FindFirst(x => x.index > ands.Last().index && (
                    (x.tag.Length > 1 && x.tag.Substring(0, 2) == nlpResults[ands[0].head].tag.Substring(0, 2)) ||
                    x.partOfSpeech == nlpResults[ands[0].head].partOfSpeech ||
                    x.dependency[0] == nlpResults[ands[0].head].dependency[0]
                    ));
            //final = nlpResults[nlpResults.Count-2];
            if (final != null)
                rangeList.Add((prevIndex, final.index));

            foreach (var keepRange in rangeList)
            {
                List<NLPItem> remainder = BuildResultsWithSingleCase(nlpResults, rangeList, keepRange);
                for (int i = remainder.Count - 1; i >= 0; i--)
                    if (remainder[i].lemma == "and" || remainder[i].lemma == "or")
                        ResultsRemoveAt(remainder, i);
                //HandleDeclaration(remainder, phrase, "");
                pendingInput.Enqueue(new() { desiredResult = "", items = remainder, phrase = phrase });

            }
            return false;
        }

        private List<NLPItem> BuildResultsWithSingleCase(List<NLPItem> nlpResults, List<(int, int)> rangeList, (int, int) keepRange)
        {
            //make a local copy
            List<NLPItem> remainder = CloneNLPResults(nlpResults);
            for (int j = rangeList.Count - 1; j >= 0; j--)
            {
                (int, int) rangeToDelete = rangeList[j];
                if (rangeToDelete != keepRange)
                {
                    RemoveRange(remainder, rangeToDelete);
                }
            }

            return remainder;
        }

        private void RemoveRange(List<NLPItem> remainder, (int, int) rangeToDelete)
        {
            for (int i = rangeToDelete.Item2; i >= rangeToDelete.Item1; i--)
                ResultsRemoveAt(remainder, i);
        }
    }

}