using Catalyst.Models;
using OpenAI.GPT3.ObjectModels.ResponseModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Windows.Forms.VisualStyles;

namespace BrainSimulator.Modules
{
    public partial class ModuleUKS
    {

        public List<ThingWithQueryParams> Query(
                object source, object relationshipType = null, object target = null,
                object sourceModifiers = null, object relationshipModifiers = null, object targetModifiers = null
                )
        {
            //Thing Source = ThingFromObject(source);
            //Thing RelationshipType = ThingFromObject(relationshipType);
            //Thing Target = ThingFromObject(target);
            //List<Thing> SourceModifiers = ThingListFromObject(sourceModifiers);
            //List<Thing> RelationshipTypeModifiers = ThingListFromObject(relationshipModifiers, "action");
            //List<Thing> TargetModifiers = ThingListFromObject(targetModifiers);

            //QueryRelationship q = new QueryRelationship
            //{
            //    source = Source,
            //    relType = RelationshipType,
            //    target = Target,
            //    sourceProperties = SourceModifiers,
            //    typeProperties = RelationshipTypeModifiers,
            //    targetProperties = TargetModifiers,
            //};

            if (source is string s)
            {
                string[] sources = s.Split(' ');
                List<Thing> q = new();
                foreach (string s1 in sources)
                {
                    Thing t = ThingLabels.GetThing(s1);
                    if (t != null)
                        q.Add(t);
                }
                var queryResult = BuildSearchList(q);
                return queryResult;
            }
            return null;
        }

        //This is used to store temporary content during queries
        public class ThingWithQueryParams
        {
            public Thing thing;
            public int hopCount;
            public int haveCount = 1;
            public int hitCount = 1;
            public float weight;
            public ThingWithQueryParams(Thing t, int h, float weight)
            {
                thing = t;
                hopCount = h;
                this.weight = weight;
            }
            public string ToString()
            {
                return (thing.Label + "  : " + hopCount + " : " + weight + "  Count: " + haveCount + " Hits: " + hitCount);
            }
        }

        private List<ThingWithQueryParams> BuildSearchList(List<Thing> q)
        {
            List<ThingWithQueryParams> thingsToExamine = new();
            int maxHops = 20;
            int hopCount = 0;
            foreach (Thing t in q)
                thingsToExamine.Add(new ThingWithQueryParams(t, hopCount, 1));
            hopCount++;
            int currentEnd = thingsToExamine.Count;
            for (int i = 0; i < thingsToExamine.Count; i++)
            {
                Thing t = thingsToExamine[i].thing;
                //weights are multipled
                float curWeight = thingsToExamine[i].weight;
                int curCount = thingsToExamine[i].haveCount;

                foreach (Relationship r in t.RelationshipsFrom)  //has-child et al
                    if (r.relType == Thing.HasChild)
                    {
                        if (thingsToExamine.FindFirst(x => x.thing == r.source) is ThingWithQueryParams twgp)
                        {
                            twgp.hitCount++;
                        }
                        else
                        {
                            ThingWithQueryParams thingToAdd = new ThingWithQueryParams(r.source, hopCount, curWeight * r.weight);
                            thingsToExamine.Add(thingToAdd);
                            //if things have counts, they are multiplied
                            int val = GetCount(r.reltype);
                            thingToAdd.haveCount = curCount * val;
                        }
                    }

                foreach (Relationship r in t.Relationships) //has-a et al
                    if (r.relType.HasAncestorLabeled("has-a"))
                    {
                        if (thingsToExamine.FindFirst(x => x.thing == r.target) is ThingWithQueryParams twqp)
                        {
                            twqp.hitCount++;
                        }
                        else
                        {
                            ThingWithQueryParams thingToAdd = new ThingWithQueryParams(r.target, hopCount, curWeight * r.weight);
                            thingsToExamine.Add(thingToAdd);
                            //if things have counts, they are multiplied
                            int val = GetCount(r.reltype);
                            thingToAdd.haveCount = curCount * val;
                        }
                    }
                if (i == currentEnd - 1)
                {
                    hopCount++;
                    currentEnd = thingsToExamine.Count;
                    if (hopCount > maxHops) break;
                }
            }
            return thingsToExamine;
        }

        public List<Relationship> GetAllRelationships(List<ThingWithQueryParams> thingsToExamine,Relationship.Part p)
        {
            List<Relationship> result = new();
            for (int i = 0; i < thingsToExamine.Count; i++)
            {
                Thing t = thingsToExamine[i].thing;
                int haveCount = thingsToExamine[i].haveCount;
                IList<Relationship> relationshipsToAdd = null;
                switch (p)
                {
                    case Relationship.Part.source: relationshipsToAdd = t.Relationships; break;
                    case Relationship.Part.target: relationshipsToAdd = t.RelationshipsFrom; break;
                    case Relationship.Part.type: relationshipsToAdd = t.RelationshipsAsTypeWriteable; break;
                }
                foreach (Relationship r in relationshipsToAdd) 
                {
                    if (r.reltype == Thing.HasChild) continue;

                    //only add the new relatinoship to the list if it is not already in the list
                    bool ignoreSource = thingsToExamine[i].hopCount > 1;
                    Relationship existing = result.FindFirst(x => RelationshipsAreEqual(x, r, ignoreSource));
                    if (existing != null) continue;

                    if (haveCount > 1)
                    {
                        //this creates a temporary relationship so suzie has 2 arm, arm has 5 fingers, return suzie has 10 fingers
                        //this (transient) relationshiop doesn't exist in the UKS
                        Relationship r1 = new Relationship(r);
                        Thing newCountType = GetOrAddThing((GetCount(r.reltype) * haveCount).ToString(), "number");
                        Thing newRelType = CreateInstanceOf(r1.reltype, new List<Thing> { newCountType });
                        r1.reltype = newRelType;
                        result.Add(r1);
                    }
                    else
                        result.Add(r);
                }
            }

            RemoveConflictingResults(result);
            RemoveFalseConditionals(result);

            return result;
        }

        private void RemoveConflictingResults(List<Relationship> result)
        {
            for (int i = 0; i < result.Count; i++)
            {
                Relationship r1 = result[i];
                for (int j = i + 1; j < result.Count; j++)
                {
                    Relationship r2 = result[j];
                    if (RelationshipsAreExclusive(r1, r2))
                    {
                        result.RemoveAt(j);
                        if (j > 0) j--;
                    }

                }
            }
        }
        private void RemoveFalseConditionals(List<Relationship> result)
        {
            for (int i = 0; i < result.Count; i++)
            {
                Relationship r1 = result[i];
                if (!ConditionsAreMet(r1.clauses, r1))
                {
                    failedConditions.Add(r1);
                    result.RemoveAt(i);
                    if (i > 0) i--;
                }
                else
                {
                    succeededConditions.Add(r1);
                }
            }
        }

        int GetCount(Thing t)
        {
            int retVal = 1;
            foreach (Relationship r in t.Relationships)
                if (r.relType.Label == "is")
                    if (int.TryParse(r.target.Label, out int val))
                        return val;
            return retVal;
        }

        public bool HasSequence (IList<Relationship> relationships, List<Thing> targetAttributes)
        {
            //TODO modify to find closest match
            IList<Relationship> sortedReferences = relationships.OrderBy(x => x.relType.Label).ToList();
            for (int i = 0; i < sortedReferences.Count - targetAttributes.Count + 1; i++)
            {
                for (int j = 0; j < targetAttributes.Count; j++)
                {
                    if (sortedReferences[i+j].target != targetAttributes[j]) goto misMatch;
                }
                return true;
            misMatch: continue;
            }
            return false;
        }
        public List<Thing> HasSequence(List<Thing> targetAttributes)
        {
            List<Thing> retVal = new();
            foreach (Thing t in targetAttributes)
                foreach (Relationship r in t.RelationshipsFrom)
                    if (r.relType.HasAncestorLabeled("has"))
                        if (!retVal.Contains(r.source))
                            retVal.Add(r.source);

            for (int i = 0; i <retVal.Count;i++)
            {
                if (!HasSequence(retVal[i].Relationships,targetAttributes))
                {
                    retVal.RemoveAt(i);
                    if (i != 0)
                        i--;
                }
            }
            return retVal;
        }

        public List<Thing> GetAllAttributes(Thing t)
        {
            List<Thing> retVal = new();

            var temp1 = BuildSearchList(new List<Thing>() { t });
            var temp2 = GetAllRelationships(temp1, Relationship.Part.source);
            foreach (Relationship r in temp2)
                if (r.relType.Label == "is")
                    retVal.Add(r.target);
            return retVal;
        }

        //TODO add parameter to allow some number of misses
        public bool HasAllAttributes(Thing t, List<Thing> targetAttributes)
        {
            List<Thing> thingAttributes = GetAllAttributes(t);
            foreach (Thing t2 in targetAttributes)
                if (!thingAttributes.Contains(t2) && !t.AncestorList().Contains(t2)) return false;
            return true;
        }
        public List<Thing> FindThingsWithAttributes(List<Thing> attributes)
        {
            List<Thing> retVal = new();
            List<Thing> attribOwners = new List<Thing>();
            foreach (Thing t in attributes)
            {
                foreach (Relationship r in t.RelationshipsFrom)
                    if (r.reltype.Label == "is")
                    {
                        attribOwners.Add(r.source);
                    }
            }
            List<ThingWithQueryParams> results = BuildSearchList(attribOwners);
            foreach (var t in results)
            {
                if (HasAllAttributes(t.thing,attributes))
                    retVal.Add(t.thing);
            }
            return retVal;
        }


        //IList<Relationship> SearchRelationships(QueryRelationship searchMask, bool doInheritance = true, bool checkConditions = true)
        //{
        //    List<Relationship> queryResult = new();
        //    //HandlePronouns(searchMask);
        //    Thing invType = CheckForInverse(searchMask.relType);
        //    //if this relationship has an inverse, switcheroo so we are storing consistently in one direction
        //    if (invType != null)
        //    {
        //        (searchMask.source, searchMask.target) = (searchMask.target, searchMask.source);
        //        (searchMask.sourceProperties, searchMask.targetProperties) = (searchMask.targetProperties, searchMask.sourceProperties);
        //        (searchMask.relType, invType) = (invType, searchMask.relType);
        //    }

        //    //handle pronouns in statements
        //    //if (HandlePronouns(searchMask)) return queryResult;

        //    //first get all the relationships from the given source or target (whichever is specified)
        //    if (searchMask.source != null)
        //    {
        //        if (doInheritance)
        //            queryResult = RelationshipWithInheritance(searchMask.source); //searches parents
        //        else
        //            queryResult = (List<Relationship>)searchMask.source.Relationships;
        //        //List<Relationship> hasProperties = new List<Relationship>();
        //        //foreach (Relationship q in queryResult)
        //        //{
        //        //    hasProperties.AddRange(RelationshipByWithInheritance(q.target));
        //        //}
        //        //for (int i = 0; i < hasProperties.Count; i++)
        //        //{
        //        //    Relationship r = hasProperties[i];
        //        //    if (r.target != searchMask.target)
        //        //    {
        //        //        hasProperties.RemoveAt(i);
        //        //        i--;
        //        //    }
        //        //}
        //        //if (hasProperties.Count > 0)
        //        //    queryResult.AddRange(hasProperties);
        //    }
        //    else if (searchMask.target != null)
        //    {
        //        if (doInheritance)
        //            queryResult = RelationshipByWithInheritance(searchMask.target); //searches children
        //        else
        //            queryResult = (List<Relationship>)searchMask.target.RelationshipsFrom;
        //    }
        //    else if (searchMask.relType != null) //both source and target are null
        //    {
        //        queryResult = RelationshipByWithInheritance(searchMask.relType, null, 4, true);
        //    }
        //    else
        //    {
        //        return null;
        //    }
        //    if (checkConditions)
        //    {
        //        //handle conditionals
        //        var listOfFalseConditions = queryResult.FindAll(r => !ConditionsAreMet(r.clauses, searchMask));
        //        if (listOfFalseConditions.Count > 0)
        //        {
        //            foreach (Relationship condition in listOfFalseConditions)
        //            {
        //                queryResult.RemoveAll(r => r == condition);
        //            }
        //        }
        //        //queryResult.RemoveAll(r => r.clauses.FindFirst(c => c.a == AppliesTo.condition) != null);
        //    }

        //    //then remove the ones which don't match the search criteria
        //    for (int i = queryResult.Count - 1; i >= 0; i--)
        //    {
        //        Relationship r = queryResult[i];
        //        //if (searchMask.relType != null && HasProperty(searchMask.relType, "transitive") &&
        //        //    r.relType != null && HasProperty(r.relType, "transitive")) continue;
        //        if (r.weight < 0.5)
        //        {
        //            queryResult.RemoveAt(i);
        //        }
        //        else if (searchMask.source != null && r.source != null && !ThingInTree(r.source, searchMask.source))
        //        {
        //            queryResult.RemoveAt(i);
        //        }
        //        else if (searchMask.source == null && searchMask.target != null && searchMask.target != r.target &&
        //            Relationship.TrimDigits(r.target?.Label) != searchMask.target?.Label)
        //        {
        //            queryResult.RemoveAt(i);
        //        }
        //        else if (searchMask.sourceProperties.Count > 0 && r.source != null && !ModifiersMatch(searchMask.sourceProperties, r.source))
        //        {
        //            queryResult.RemoveAt(i);
        //        }
        //        else if (searchMask.target != null && !ThingInTree(r.target, searchMask.target))
        //        {
        //            queryResult.RemoveAt(i);
        //        }
        //        else if (searchMask.targetProperties.Count > 0 && r.target != null && !ModifiersMatch(searchMask.targetProperties, r.target))
        //        {
        //            queryResult.RemoveAt(i);
        //        }
        //        else if (searchMask.reltype != null && r.reltype != null && !ThingInTree(r.reltype, searchMask.reltype))
        //        {
        //            queryResult.RemoveAt(i);
        //        }
        //        else if (searchMask.typeProperties.Count > 0 && r.reltype != null && !ModifiersMatch(searchMask.typeProperties, r.reltype))
        //        {
        //            queryResult.RemoveAt(i);
        //        }
        //        else if (!doInheritance && searchMask.target != r.target)
        //        {
        //            queryResult.RemoveAt(i);
        //        }
        //    }

        //    //handle transitive relationships
        //    int paramCount = 0;
        //    if (searchMask.relType != null) paramCount++;
        //    if (searchMask.source != null) paramCount++;
        //    if (searchMask.target != null) paramCount++;
        //    if (paramCount > 1 && (searchMask.relType == null || (searchMask.relType != null && HasProperty(searchMask.relType, "transitive"))))
        //        queryResult = HandleTransitivieRelatinships(searchMask, invType, queryResult);

        //    queryResult = queryResult.OrderByDescending(x => x.lastUsed).ToList();
        //    return queryResult;
        //}
        List<Relationship> failedConditions = new();
        List<Relationship> succeededConditions = new();

        public static bool StackContains(string target, int count = 0)
        {
            StackTrace stackTrace = new StackTrace();           // get call stack
            StackFrame[] stackFrames = stackTrace.GetFrames();  // get method calls (frames)

            // do not bubble if called from Initialization
            foreach (StackFrame stackFrame in stackFrames)
            {
                string name = stackFrame.GetMethod().Name;
                if (name.Contains(target))
                {
                    if (count == 0)
                        return true;
                    count--;
                }
            }
            return false;
        }

        bool ConditionsAreMet(List<ClauseType> clauses, Relationship query)
        {
            if (clauses.Count == 0) return true;
            //if (StackContains("ConditionsAreMet",1)) return true;
            foreach (ClauseType c in clauses)
            {
                if (c.a != AppliesTo.condition) continue;
                Relationship r = c.clause;
                QueryRelationship q = new(r);
                if (query.source != null && query.source.AncestorList().Contains(q.source))
                    q.source = query.source;
                if (query.source != null && query.source.AncestorList().Contains(q.target))
                    q.target = query.target;
                if (query.target != null && query.target.AncestorList().Contains(q.source))
                    q.source = query.target;
                var qResult = Relationship.GetRelationship(q);
                if (qResult != null && qResult.weight < 0.8)
                    return false; ;
                if (qResult == null)
                {
                    failedConditions.Add(q);
                    return false;
                }
                else
                    succeededConditions.Add(q);
            }
            return true;
        }
        public List<Relationship> WhyNot()
        {
            return failedConditions;
        }
        public List<Relationship> Why()
        {
            return succeededConditions;
        }
        //private List<Relationship> RelationshipByWithInheritance(Thing t, List<Relationship> relationshipList = null, int depthLimit = 4, bool sourceIsNull = false)
        //{
        //    depthLimit--;
        //    if (depthLimit == 0)
        //        return relationshipList;

        //    bool firstTime = false;
        //    if (relationshipList == null)
        //    {
        //        firstTime = true;
        //        relationshipList = new List<Relationship>();
        //    }

        //    bool ignoreSourceInternal = (depthLimit < 2); //do not add inherited relationships which differ only in source
        //    if (sourceIsNull) ignoreSourceInternal = false;
        //    foreach (Relationship r in t.RelationshipsFrom)
        //    {
        //        if (firstTime)
        //            relationshipList.Add(r);
        //        else
        //        {
        //            if (r.relType?.Label != "has-child")
        //                AddRelationshipToList(r, relationshipList, false, ignoreSourceInternal);
        //        }
        //    }
        //    foreach (Thing t2 in t.Children)  //search the instances of
        //    {
        //        RelationshipByWithInheritance(t2, relationshipList, depthLimit, sourceIsNull);
        //    }

        //    return relationshipList;
        //}



        //private List<Relationship> RelationshipWithInheritance(Thing t, List<Relationship> relationshipList = null, int depthLimit = 8)
        //{
        //    depthLimit--;
        //    if (depthLimit == 0)
        //        return relationshipList;
        //    bool firstTime = false;
        //    if (relationshipList == null)
        //    {
        //        firstTime = true;
        //        relationshipList = new List<Relationship>();
        //    }
        //    foreach (Relationship r in t.Relationships)
        //    {
        //        if (firstTime)
        //            relationshipList.Add(r);
        //        else
        //        {
        //            bool ignoreSource = depthLimit < 7;
        //            if (r.relType?.Label != "has-child")
        //                AddRelationshipToList(r, relationshipList, true, ignoreSource);
        //        }
        //    }
        //    foreach (Thing t2 in t.Parents)
        //    {
        //        RelationshipWithInheritance(t2, relationshipList, depthLimit);
        //    }
        //    return relationshipList;
        //}
        //void AddRelationshipToList(Relationship r, List<Relationship> relations, bool checkExclusion = true, bool ignoreSource = false)
        //{
        //    //only add the new property to the list if it does not conflict with one already in the list
        //    //don't add if it already exists
        //    Relationship existing = relations.FindFirst(x => RelationshipsAreEqual(x, r, ignoreSource));
        //    if (existing == null)
        //    {
        //        if (checkExclusion)
        //        {
        //            foreach (Relationship r1 in relations)
        //            {
        //                if (RelationshipsAreExclusive(r, r1))
        //                    goto cantAdd1;
        //            }
        //        }
        //        relations.Add(r);
        //    cantAdd1:
        //        { }
        //    }
        //}

    }
}
