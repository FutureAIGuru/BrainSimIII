//
// Copyright (c) FutureAI. All rights reserved.  
// Contains confidential and  proprietary information and programs which may not be distributed without a separate license
//  

using Pluralize.NET;
using System.Collections.Generic;
using System.Linq;

namespace BrainSimulator.Modules
{
    public partial class ModuleNLP : ModuleBase
    {
        //QUERY////////////////////////////////////////////////////////
        private void HandleQuery(List<NLPItem> nlpResultsIn, string desiredResult)
        {
            //HANDLE QUERIES HERE
            List<NLPItem> nlpResults = CloneNLPResults(nlpResultsIn);
            List<Relationship> relationships = new();
            List<ClauseType> clauses = new();
            string responseString = "";
            //does the question start with a verb?
            relPos = 0;
            if (nlpResults[0].lemma == "why")
            {
                responseString = HandleReasonQueries(nlpResults);
            }
            else if (nlpResults[0].dependency == "ROOT" || nlpResults[0].tag == "VBZ" || nlpResults[0].dependency == "aux")
            {
                responseString = HandleYesNoQueries(nlpResults);
            }
            //does the query start with a W word?
            else if (nlpResults[0].tag.StartsWith("W"))
            {
                HandleCommonQueries(nlpResults, ref relationships, clauses, ref responseString);
            }

            if (responseString == null || responseString == "")
                responseString = "I don't know about that.";

            OutputReponseString(responseString);

            CompareOutputToDesired(responseString, desiredResult);

            return;
        }

        private void HandleCommonQueries(List<NLPItem> nlpResults, ref List<Relationship> relationships, List<ClauseType> clauses, ref string responseString)
        {
            string queryWord = nlpResults[0].lemma;

            string searchTarget = "";
            switch (queryWord)
            {
                case "where": searchTarget = "place"; ResultsRemoveAt(nlpResults, 0); break;
                case "when": searchTarget = "time"; ResultsRemoveAt(nlpResults, 0); break;
                case "who": searchTarget = "person"; ResultsRemoveAt(nlpResults, 0); break;
                case "what": searchTarget = ""; ResultsRemoveAt(nlpResults, 0); break;
                case "how":
                    if (nlpResults[1].lemma == "many")
                    {
                        searchTarget = "number";
                        ResultsRemoveAt(nlpResults, 0);
                        ResultsRemoveAt(nlpResults, 0);
                    }
                    break;
            }

            NLPItem relType = HandleType(nlpResults, out List<string> typeProperties, clauses);
            NLPItem source = HandleSource(nlpResults, out List<string> sourcePoperties, clauses);
            NLPItem target = HandleTarget(nlpResults, out List<string> targetProperties, clauses, source?.index);

            if (queryWord == "what" || queryWord == "who")
            {
                //what is/are ...
                if (nlpResults[0].lemma == "be")
                {
                    //test of new GetPropertiesWithInheritance
                    Thing root = UKS.Labeled("Thing");
                    var ancestors = root.ExpandTransitiveRelationship("has-child", true).ToList();
                    //ancestors = root.ExpandTransitiveRelationship("has-child",false).ToList();
                    Thing test = UKS.Labeled(source?.lemma);
                    if (test != null)
                    {
                        ancestors = test.ExpandTransitiveRelationship("has-child", true).ToList();
                        ancestors = test.ExpandTransitiveRelationship("has-child", false).ToList();
                        var attributes = test.GetAttributesWithInheritance("has-child",true).ToList();
                        for (int i = 0; i < attributes.Count; i++)
                        {
                            Relationship attribute = attributes[i];
                            if (attribute.relType.HasAncestorLabeled("have"))
                                attributes.AddRange(attribute.target.GetAttributesWithInheritance("have",false));
                        }

                        UKS.DepthToFirstCommonAncestor(UKS.Labeled("suzie"), UKS.Labeled("mary"));
                    }
                    IList<Relationship> queryResults;
                    NLPItem s = null;
                    if (source != null)
                    {
                        s = SwapPronounPerson(source);
                        queryResults = UKS.Query(source?.lemma, "is-a", "");
                        foreach (Relationship r in queryResults)
                            if (r.source.HasAncestorLabeled("Object"))
                            {
                                responseString = AddArticleIfNeeded(s);
                                responseString += " " + ConjugateRootVerb("be", source);
                                string s1 = Pluralize(GetNonInstanceAncestor(r.source).Label, source.morph.Contains("Plur") ? 2 : 1);
                                responseString += " " + AddArticleIfNeeded(s1);
                                break;
                            }
                    }
                    else
                    {
                        queryResults = UKS.Query(null, "is", target?.lemma);
                        responseString = "";
                        foreach (Relationship r in queryResults)
                        {
                            if (r == queryResults.Last() && queryResults.Count > 2)
                                responseString += ", and ";
                            else if (r == queryResults.Last() && queryResults.Count == 2)
                                responseString += " and ";
                            else if (r != queryResults.First())
                                responseString += ", ";
                            responseString += GetStringFromThing(r.source);
                        }
                        if (queryResults.Count > 1)
                            responseString += " are ";
                        else if (queryResults.Count == 1)
                            responseString += " is ";
                        responseString += " " + target.lemma;
                    }
                    if (responseString == "")
                    {
                        queryResults = UKS.Query(s?.lemma, "is", "");
                        queryResults = EliminatedConflictingResults(queryResults);
                        CreateResponseFromQueryResult(responseString, source, queryResults);
                    }
                }
                //what x is ...
                else if (nlpResults[1].lemma == "be")
                {
                    responseString = "";
                    searchTarget = nlpResults[0].lemma;
                    List<NLPItem> remainder = CloneNLPResults(nlpResults);
                    ResultsRemoveAt(remainder, 0);
                    ResultsRemoveAt(remainder, 0);
                    relType = HandleType(remainder, out typeProperties, clauses);
                    source = HandleSource(remainder, out sourcePoperties, clauses);
                    target = HandleTarget(remainder, out targetProperties, clauses, source?.index);
                    IList<Thing> queryResults = UKS.ComplexQuery(source?.lemma, relType?.lemma, target?.lemma, "", searchTarget, out relationships);

                    if (queryResults.Count == 0)
                    {
                        (source, target) = (target, source);
                        queryResults = UKS.ComplexQuery(source?.lemma, relType?.lemma, target?.lemma, "", searchTarget, out relationships);
                    }
                    foreach (Thing t in queryResults)
                    {
                        if (t == queryResults.First())
                        {
                            responseString = SwapPronounPerson(source).text + " " + ConjugateRootVerb(relType?.lemma, source);
                            if (queryResults.Count == 1)
                                responseString += " a";
                        }
                        if (t == queryResults.Last() && queryResults.Count == 2) responseString += " and";
                        else if (t == queryResults.Last() && queryResults.Count > 2) responseString += ", and";
                        else if (t != queryResults[0]) responseString += ",";
                        responseString += " " + t.Label;
                    }
                    if (queryResults.Count != 0)
                        responseString += " " + Pluralize(target?.lemma, queryResults.Count);
                    else
                        responseString = "I don't know about that";
                }
                else
                {
                    var queryResults = UKS.Query(source?.lemma, relType?.lemma, target?.lemma, sourcePoperties, typeProperties, targetProperties);
                    if (queryResults.Count == 0)
                    {
                        (source, target) = (target, source);
                        (sourcePoperties, targetProperties) = (targetProperties, sourcePoperties);
                        queryResults = UKS.Query(null, relType?.lemma, target?.lemma, null, null, targetProperties);
                    }
                    if (queryResults.Count == 0)
                        queryResults = UKS.Query(source?.lemma, null, target?.lemma);
                    if (queryResults.Count == 0)
                    {
                        queryResults = UKS.Query(null, relType?.lemma, target?.lemma);
                        source = null;
                    }
                    queryResults = queryResults.Where(x => x.weight > 0.9).ToList();
                    if (searchTarget == "" && queryResults.Count > 0)
                    {
                        List<string> sources = new();
                        foreach (Relationship r in queryResults)
                            sources.Add(r.source.Label);
                        responseString += CreateAndedList(sources);
                        if (sources.Count == 1)
                            responseString += " " + queryResults[0].reltype.Label + " " + queryResults[0].target.Label;
                        else
                            responseString += " " + queryResults[0].reltype.Label + " " + Pluralize(queryResults[0].target.Label, sources.Count);
                    }
                    else
                    {
                        var v = UKS.ResultsOfType(queryResults, searchTarget);
                        if (v.Count > 0)
                        {
                            queryResults = EliminatedConflictingResults(queryResults);
                            foreach (var result in v)
                            {
                                if (result != v.First() && v.Count > 2)
                                    responseString += ", ";
                                if (result == v.Last() && v.Count > 1)
                                    responseString += " and ";

                                responseString += " " + result.Label;
                            }
                            //responseString = CreateResponseFromQueryResult(responseString, null, queryResults);

                        }
                    }
                }
            }

            else if (source?.lemma != "")
            {
                Relationship r = clauses.FindFirst(x => x.a == AppliesTo.source)?.clause;
                if (r != null)
                    source.lemma = r.target.Label;
                else if (target != null && relType.lemma == "be" && searchTarget == "")
                {
                    //for is queries, you need to turn the target into the source
                    source.lemma = target.lemma;
                    target.lemma = nlpResults.FindFirst(n => n.partOfSpeech == "NOUN")?.lemma;
                    if (target.lemma == source.lemma) target.lemma = "";
                }

                IList<Thing> queryResults = UKS.ComplexQuery(source?.lemma, "", target?.lemma, "with|and", searchTarget, out relationships);
                if (searchTarget == "number")
                {
                    responseString = "";
                    var queryResults1 = UKS.Query(source?.lemma, "", target?.lemma);
                    //                        if (queryResults1.Count == 1 && queryResults1[0].target.Label != source?.lemma)
                    if (queryResults1.Count == 0)
                    {
                        queryResults1 = UKS.Query(null, relType.lemma, target?.lemma);
                    }

                    string subj = source.lemma;
                    string targ = "";
                    if (target != null)
                    {
                        targ = target.lemma;
                        if (queryResults1.Count == 0)
                        {
                            queryResults1 = UKS.Query(target?.lemma, "", source?.lemma);
                            (subj, targ) = (targ, subj);
                        }
                    }
                    //if there are multiples, find the one with the highest weight
                    queryResults1 = queryResults1.OrderByDescending(x => x.weight).ToList();
                    if (queryResults1.Count > 0 && queryResults1[0].weight < 1)
                    {
                        responseString += " I think";
                    }
                    IList<Thing> qty = UKS.ResultsOfType(queryResults1, searchTarget);
                    if (qty.Count > 0)
                    {
                        if (target != null && subj == target.lemma && target.index > 0 && nlpResults[target.index - 1].partOfSpeech == "DET")
                            responseString += " " + nlpResults[target.index - 1].lemma;
                        if (subj == source.lemma && source.index > 0 && nlpResults[source.index - 1].partOfSpeech == "DET")
                            responseString += " " + nlpResults[source.index - 1].lemma;

                        int.TryParse(qty[0].Label, out int qtyInt);
                        if (qtyInt == 0 && qty[0].V != null) qtyInt = (int)qty[0].V; //if it's an alphabetic number ("two") the value is in the V

                        if (targ == "")
                        {
                            if (qtyInt == 1)
                                responseString = "there is 1 " + subj;
                            else
                                responseString = "there are " + qtyInt + " " + Pluralize(subj, qtyInt);
                        }
                        else
                        {
                            if (qty[0].Label == "many")
                            {
                                responseString += " " + subj + " has many";
                                qtyInt = 5;
                            }
                            else
                                responseString += " " + subj + " has " + qtyInt;
                            targ = Pluralize(targ, qtyInt);
                            responseString += " " + targ;
                        }
                    }
                }
                else if (queryResults.Count > 0 && searchTarget == "place" && relationships.Count != 1)
                {
                    responseString += RemovePunctuation(source?.lemma) + " is at the " + queryResults[0].Label;
                    responseString = CorrectOutputString(responseString);
                }
                else if (relationships.Count > 0)
                {
                    responseString = relationships[0].label;
                }
            }
        }

        private string HandleYesNoQueries(List<NLPItem> nlpResults)
        {
            string responseString = "";
            List<Relationship> relationships = new();
            List<ClauseType> clauses = new();
            NLPItem relType = HandleType(nlpResults, out List<string> typePoperties, clauses);
            NLPItem source = HandleSource(nlpResults, out List<string> sourcePoperties, clauses);
            if (source == null) return responseString;
            relPos = source.index;
            NLPItem target = HandleTarget(nlpResults, out List<string> targetProperties, clauses, source?.index);
            if (relType.lemma == "be")
            {
                relType.lemma = "is";
                if (relType.lemma == "is" && IsNoun(target) && target.dependency == "attr" && target.partOfSpeech != "PROPN") relType.lemma = "is-a";
                if (relType.lemma == "is" && relType.index < nlpResults.Count - 1 &&
                    (nlpResults[relType.index + 1]?.lemma == "a" || nlpResults[relType.index + 1]?.lemma == "an")) relType.lemma = "is-a";
                if (relType.lemma == "is" && target?.morph.Contains("Plur") == true) relType.lemma = "is-a";
                if (relType.lemma == "is" && target?.morph.Contains("Plur") == true) relType.lemma = "is-a";
                if (relType.lemma == "is-a")
                {
                    Thing targetThing = UKS.Labeled(target.lemma);
                    Thing sourceThing = UKS.Labeled(source.lemma);
                    if (sourceThing != null && targetThing != null)
                    {
                        if (sourceThing.HasAncestor(targetThing))
                        {
                            responseString = "Yes, " + AddArticleIfNeeded(SwapPronounPerson(source)) + " " + relType.text + " " + AddArticleIfNeeded(SwapPronounPerson(target));
                        }
                        else responseString = "No.";
                        return responseString;
                    }
                }
            }


            IList<Thing> queryResults;
            if (relType != null)
                queryResults = UKS.ComplexQuery(source?.lemma, relType?.lemma, target?.lemma, "", "", out relationships);
            if (relationships.Count == 0 || relType == null)
                queryResults = UKS.ComplexQuery(source?.lemma, "", target?.lemma, "", "", out relationships);

            if (relationships.Count == 0)
                responseString = "No";
            else
            {
                responseString = "Yes";
                //responseString = CreateResponseFromQueryResult(responseString, source, relationships);
            }
            return responseString;
        }
        private string HandleReasonQueries(List<NLPItem> nlpResults)
        {
            string responseString;
            if (nlpResults.Count > 1 && nlpResults[1].lemma == "not")
            {
                var reasons = UKS.WhyNot();
                if (reasons.Count == 0) responseString = "I don't know";
                else
                {
                    responseString = "because";
                    foreach (Relationship r in reasons)
                    {
                        if (r != reasons.First()) responseString += " and";
                        responseString += " " + AddArticleIfNeeded(r.source) + " " + r.reltype.Label + " not " + r.target.Label;
                    }
                }
            }
            else
            {
                var reasons = UKS.Why();
                if (reasons.Count == 0)
                    responseString = "I don't know";
                else
                {
                    responseString = "because";
                    foreach (Relationship r in reasons)
                    {
                        if (r != reasons.First()) responseString += " and";
                        responseString += " " + AddArticleIfNeeded(r.source) + " " + r.reltype.Label + " " + r.target.Label;
                    }
                }
            }

            return responseString;
        }

        string GetStringFromThing(Thing t)
        {
            string retVal = "";
            if (t != null)
            {
                IList<Thing> props = UKS.GetAttributes(t);
                retVal = "";
                foreach (Thing prop in props)
                {
                    if (prop != props.First() && props.Count > 1)
                        retVal += ",";
                    retVal += " " + prop.Label;
                }
                retVal += " " + Relationship.TrimDigits(t.Label);
            }
            return retVal;
        }

        private string CreateResponseFromQueryResult(string responseString, NLPItem source, IList<Relationship> queryResults)
        {
            string propertyString = "";
            bool isNumeric = false;
            Relationship r = null;
            for (int i = 0; i < queryResults.Count; i++)
            {
                r = queryResults[i];
                Thing invType = UKS.CheckForInverse(r.relType);
                if (r == queryResults.First())
                {
                    //if the property string is numeric, it follows the verb
                    foreach (Thing t in ThingProperties(r.relType))
                    {
                        propertyString += " " + t.Label;
                        if (t.HasAncestorLabeled("number")) isNumeric = true;
                    }
                    if (isNumeric && source != null)
                        responseString += AddArticleIfNeeded(SwapPronounPerson(source)) + " " + Relationship.TrimDigits(r.reltype.Label) + " " + propertyString;
                    else if (source != null && source.lemma == r.s.Label)
                        responseString += AddArticleIfNeeded(SwapPronounPerson(source)) + propertyString + " " + ConjugateRootVerb(Relationship.TrimDigits(r.reltype.Label));
                    else if (source != null && invType != null)
                        responseString += AddArticleIfNeeded(SwapPronounPerson(source)) + propertyString + " " +
                            ConjugateRootVerb(Relationship.TrimDigits(invType.Label));

                }
                else if (r == queryResults.Last() && queryResults.Count > 1)
                    responseString += ", and";
                else
                    responseString += ", ";
                if (source != null && source.lemma == r.s.Label)
                    responseString += " " + r.target?.Label;
                if (invType != null)
                    responseString += " " + AddArticleIfNeeded(r.source);
            }
            if (source == null && r != null)
                if (isNumeric)
                    responseString += " " + Relationship.TrimDigits(r.reltype.Label) + " " + propertyString + " " + ConjugateRootVerb(Relationship.TrimDigits(r.target?.Label));
                else
                    responseString += " " + propertyString + " " + ConjugateRootVerb(Relationship.TrimDigits(r.reltype.Label)) + " " + Relationship.TrimDigits(r.target?.Label);

            return responseString;
        }

        NLPItem HandleSource(List<NLPItem> nlpResults, out List<string> properties, List<ClauseType> clauses)
        {
            properties = new();
            if (relPos == -1) return null;

            NLPItem si = nlpResults.FindFirst(x => x.dependency == "nsubj");
            if (si == null)
                si = nlpResults.FindFirst(x => x.partOfSpeech == "NOUN" && x.dependency != "pobj" ||
                x.partOfSpeech == "PROPN" || x.partOfSpeech == "PRON");
            if (si == null)
                si = nlpResults.FindFirst(x => x.partOfSpeech == "PROPN");

            if (si != null)
            {
                for (int i = 0; i < si.index; i++)
                    if (IsAdjective(nlpResults[i]))
                        properties.Add(nlpResults[i].lemma);
                FindClauses(nlpResults, si.index, relPos, AppliesTo.source, clauses);
            }
            return si;
        }
        NLPItem HandleType(List<NLPItem> nlpResults, out List<string> properties, List<ClauseType> clauses)
        {
            properties = new();
            NLPItem si = nlpResults.FindFirst(x => x.partOfSpeech == "VERB");
            if (si == null)
                si = nlpResults.FindFirst(x => x.dependency == "ROOT");
            if (si != null)
                relPos = si.index;
            else return null;

            NLPItem siAux = nlpResults.FindFirst(x => x.partOfSpeech == "AUX");
            if (siAux != null && si != siAux && siAux.lemma != "do" && siAux.head == si.index)
                properties.Add(siAux.lemma);

            // Special case for "have" check for number words. 
            if (si.lemma == "have")
            {
                string count = "";
                int i = si.index + 1;
                while (i < nlpResults.Count &&
                    (nlpResults[i].partOfSpeech == "NUM" ||
                    (UKS.Labeled(nlpResults[i].text) != null && UKS.Labeled(nlpResults[i].text).HasAncestor(UKS.Labeled("number")))))
                {
                    count += nlpResults[i].text + " ";
                    i++;
                }
                if (count != "")
                    properties.Add(count.Trim());
            }

            return si;
        }
        NLPItem HandleTarget(List<NLPItem> nlpResults, out List<string> properties, List<ClauseType> clauses, int? sourceIndex)
        {
            properties = new();
            if (relPos == -1) return null;
            NLPItem si = nlpResults.FindFirst(x => x.dependency == "dobj" && x.index != sourceIndex);
            if (si == null)
                si = nlpResults.FindFirst(x => x.index != sourceIndex && IsAdjective(x));
            if (si == null)
                si = nlpResults.FindFirst(x => x.index != sourceIndex && (x.partOfSpeech == "NOUN" || IsNoun(x)));
            if (si == null)
                si = nlpResults.FindFirst(x => x.index != sourceIndex && (x.partOfSpeech == "NOUN" || IsNoun(x)));

            if (si != null)
            {
                for (int i = relPos + 1; i < si.index; i++)
                    if (IsAdjective(nlpResults[i]))
                        properties.Add(nlpResults[i].lemma);
                if (si.index < nlpResults.Count &&
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

        private void HandleTellMeAbout(List<NLPItem> nlpResults, string desiredResult)
        {
            string targetLabel = "";
            string responseString = "";
            List<string> properties = new();
            NLPItem theItem = nlpResults.FindFirst(x => x.dependency == "pobj");
            if (theItem == null)
                theItem = nlpResults.FindFirst(x => x.dependency == "dobj");
            if (theItem == null)
            {
                OutputReponseString("I don't know about that ");
                return;
            }

            targetLabel += " " + theItem.lemma;
            foreach (NLPItem item1 in nlpResults)
                if (item1.head == theItem.index && IsAdjective(item1))
                    properties.Add(item1.lemma);

            var queryResult = UKS.Query("", "", targetLabel);
            Thing targetThing = UKS.Labeled(theItem.lemma);
            if (queryResult.Count > 0) targetThing = queryResult[0].target;
            targetLabel = (string.Join(" ", properties) + " " + targetLabel).Trim();
            if (properties.Count > 0)
            {
                targetThing = targetThing.ChildWithProperties(properties);
            }
            if (targetThing != null)
            {
                responseString = ListProperties(targetThing, targetLabel);
                responseString = CorrectOutputString(responseString);
                OutputReponseString(responseString);
            }
            else
            {
                OutputReponseString("I don't know about that ");
            }
            CompareOutputToDesired(responseString, desiredResult);
        }
        private string ListChildren(Thing targetThing, int maxCount)
        {
            string responseString = "";
            int count = 0;
            if (targetThing == null) return "I don't know about that";
            List<string> entries = new List<string>();
            foreach (Thing t in targetThing.Children)
            {
                if (t.Children.Count > 0) continue;
                string propertyString = "";
                foreach (Thing t1 in ThingProperties(t))
                    propertyString += " " + t1.Label;

                entries.Add(propertyString.TrimEnd() + " " + Relationship.TrimDigits(t.Label));
                count++;
            }
            if (count == 0)
                return "I don't know of any " + Pluralize(targetThing.Label, 2);
            else if (targetThing.Children.Count == 1)
                responseString += CreateAndedList(entries) + " is a " + Pluralize(GetStringFromThing(targetThing), 1);
            else
                responseString += CreateAndedList(entries) + " are " + Pluralize(GetStringFromThing(targetThing), 2);
            return responseString;
        }

        string CreateAndedList(List<string> entries)
        {
            if (entries.Count == 0) return "";
            if (entries.Count == 1) return entries[0];
            string returnString = "";
            for (int i = 0; i < entries.Count; i++)
            {
                string entry = entries[i];
                if (i == entries.Count - 1 && entries.Count > 2)
                    returnString += ", and ";
                else if (i == entries.Count - 1)
                    returnString += " and ";
                else if (i != 0)
                    returnString += ", ";
                returnString += entry;
            }
            return returnString;
        }

        private string ListProperties(Thing targetThing, string targetLabel)
        {
            string responseString = "";
            if (targetThing == null) return "I don't know about " + targetLabel;

            List<Thing> sourceModifiers = new();
            List<Thing> typeModifiers = new();

            IList<Relationship> queryResults = UKS.Query(targetThing, "is-a", "");
            queryResults = EliminatedConflictingResults(queryResults);
            if (queryResults.Count > 0)
            {
                responseString = targetLabel;
                List<Thing> commonParents = new();
                for (int i = 1; i < queryResults.Count; i++)
                {
                    Relationship r = queryResults[i];
                    commonParents.AddRange(ModuleUKS.FindCommonParents(r.source, queryResults[i - 1].source));
                }
                if (commonParents.Count == 0 && targetThing.Parents.Count > 0)
                    commonParents.Add(targetThing.Parents[0]);

                foreach (Thing parent in commonParents)
                {
                    if (parent == commonParents.Last() && commonParents.Count > 1) responseString += " and";
                    responseString += " is a";

                    foreach (Relationship r in queryResults)
                    {
                        //if (!r.target.Parents.Contains(parent)) continue;
                        if (r != queryResults[0]) responseString += ",";
                        sourceModifiers = ThingProperties(r.source);
                        typeModifiers = ThingProperties(r.reltype);
                        if (typeModifiers.FindFirst(x => x.Label == "not") != null)
                            responseString += " not";

                        foreach (Thing t in sourceModifiers)
                            if (t.Parents.FindFirst(x => x.Label == "pronoun") == null)
                                responseString += " " + t.Label;
                    }
                    responseString += " " + Relationship.TrimDigits(parent.Label);
                }
            }

            queryResults = UKS.Query(targetThing, "", "");
            queryResults = queryResults.OrderBy(x => x.label).ToList();

            for (int i = queryResults.Count - 1; i >= 0; i--)
            {
                if (Relationship.TrimDigits(queryResults[i].reltype?.Label) == "has-child") queryResults.RemoveAt(i);
                else if (Relationship.TrimDigits(queryResults[i].reltype?.Label) == "is" &&
                    queryResults[i].source != targetThing) queryResults.RemoveAt(i);
                else if (queryResults[i]?.target?.HasAncestorLabeled("pronoun") == true) queryResults.RemoveAt(i);
                else if (queryResults[i]?.relType?.HasAncestorLabeled("go") == true || queryResults[i]?.relType?.Label == "go") queryResults.RemoveAt(i);
            }
            if (queryResults.Count != 0)
            {
                List<Relationship> culledResults = new List<Relationship>();
                List<Relationship> culledResultsNullTarget = new List<Relationship>();
                foreach (Relationship r in queryResults)
                {
                    if (sourceModifiers.Contains(r.target)) continue;
                    if (r.weight < 0.8f) continue;
                    if (r.target == null || r.reltype.Relationships.FindFirst(x => x.target.HasAncestorLabeled("Relationship")) != null)
                        culledResultsNullTarget.Add(r);
                    else
                        culledResults.Add(r);
                }

                string currentRelType = "";
                if (culledResults.Count > 0) currentRelType = culledResults[0].relType?.Label;

                foreach (Relationship r in culledResults)
                {
                    if (r == culledResults.First())
                    {
                        if (targetThing.HasAncestorLabeled("person"))
                            responseString += " " + "who";
                        else
                            responseString += " " + "that";
                    }
                    else if (r == culledResults.Last() && culledResults.Count > 2)
                        responseString += ", and";
                    else if (r == culledResults.Last())
                        responseString += " and";
                    else
                        responseString += ",";

                    typeModifiers = ThingProperties(r.reltype);
                    if (currentRelType != Relationship.TrimDigits(r.relType?.Label))
                    {
                        currentRelType = Relationship.TrimDigits(r.relType?.Label);
                        responseString += " " + SingularizeVerb(currentRelType);
                    }
                    if (typeModifiers.FindFirst(x => x.Label == "not") != null)
                        responseString += " not";

                    Thing numericModifier = null;
                    int qty = 0;
                    foreach (Thing modifier in typeModifiers)
                    {
                        int.TryParse(modifier.Label, out qty);
                        if (modifier.Label == "some" || modifier.Label == "many")
                            numericModifier = modifier;
                    }
                    string targetString = Relationship.TrimDigits(r.target?.Label);
                    if (qty > 1 || numericModifier != null)
                    {
                        IPluralize pluralizer = new Pluralizer();
                        targetString = pluralizer.Pluralize(targetString);
                        if (numericModifier != null) responseString += " " + numericModifier.Label;
                        else responseString += " " + qty.ToString();
                    }
                    else if (currentRelType != "is")
                    {
                        responseString += " a";
                    }
                    List<Thing> targetModifiers = ThingProperties(r.target);
                    foreach (Thing t in targetModifiers)
                        responseString += " " + t.Label;
                    responseString += " " + targetString;
                }
                if (culledResultsNullTarget.Count > 0 && culledResults.Count > 0) { responseString += " and"; }
                foreach (Relationship r in culledResultsNullTarget)
                {
                    if (r == culledResultsNullTarget.First())
                    {
                        if (targetThing.HasAncestorLabeled("person"))
                            responseString += " " + "who";
                        else
                            responseString += " " + "that";
                    }
                    else if (r == culledResultsNullTarget.Last() && culledResultsNullTarget.Count > 2)
                        responseString += ", and";
                    else if (r == culledResultsNullTarget.Last())
                        responseString += " and";
                    else
                        responseString += ",";
                    typeModifiers = ThingProperties(r.reltype);

                    List<Thing> modifiers = ThingProperties(r.reltype);
                    string newTypeLabel = "";
                    foreach (Thing t in modifiers)
                        newTypeLabel += " " + Relationship.TrimDigits(t.Label);
                    newTypeLabel += " " + Relationship.TrimDigits(r.reltype.Label);
                    if (modifiers.Count > 0 && newTypeLabel != currentRelType)
                    {
                        currentRelType = newTypeLabel;
                        responseString += " " + newTypeLabel;
                    }
                    if (r.target != null)
                        responseString += " " + Relationship.TrimDigits(r.target.Label);

                }
            }
            return responseString;
        }
        IList<Relationship> EliminatedConflictingResults(IList<Relationship> relationships)
        {
            List<Relationship> retVal = new();
            for (int i = 0; i < relationships.Count; i++)
            {
                retVal.Add(relationships[i]);
                for (int j = i + 1; j < relationships.Count; j++)
                {
                    if (UKS.RelationshipsAreExclusive(relationships[i], relationships[j]))
                    {
                        if (relationships[i].weight < relationships[j].weight)
                            retVal[i] = relationships[j];
                        relationships.RemoveAt(j);
                    }

                }
            }
            return retVal;
        }
    }
}