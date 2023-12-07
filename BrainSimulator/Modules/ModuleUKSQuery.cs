using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;

namespace BrainSimulator.Modules
{
    public partial class ModuleUKS
    {
        public List<Thing> ComplexQuery(string source, string verb, string target, string prepositionString, string resultType, out List<Relationship> rawResult)
        {
            failedConditions.Clear();
            succeededConditions.Clear();
            //basic query
            rawResult = Query(source, verb, target).ToList();
            if (rawResult.Count == 0 && verb == "")
                rawResult = Query(target, verb, source).ToList();
            List<Relationship> clauses = new();
            if (!string.IsNullOrEmpty(prepositionString))
            {
                //add prepositionsl clauses
                string[] prepositions = prepositionString.Split("|");
                foreach (string s in prepositions)
                {
                    if (source != "")
                        clauses.AddRange(UKS.Query("", s, source));
                    if (target != "")
                        clauses.AddRange(UKS.Query("", s, target));
                }
            }

            //merge phrases containing clauses
            foreach (Relationship clause in clauses)
                rawResult.AddRange(clause.clausesFrom);

            //delete results which do not contain an element with the resultTipe
            if (resultType != "")
            {
                for (int i = rawResult.Count - 1; i >= 0; i--)
                {
                    Relationship r = rawResult[i];
                    if (r.source != null && ThingInTree(r.source, resultType)) continue;
                    if (r.target != null && ThingInTree(r.target, resultType)) continue;
                    if (r.relType != null && ThingInTree(r.relType, resultType)) continue;
                    rawResult.RemoveAt(i);
                }
            }
            //get the correct things from the result and eliminate source from result
            List<Thing> resultList = GetThingsWithAncestor(rawResult, resultType);
            resultList = resultList.FindAll(x => Relationship.TrimDigits(x.Label) != source);
            return resultList;
        }
        public List<Thing> GetThingsWithAncestor(IList<Relationship> queryResult, string ancestorLabel)
        {
            bool HasAncestor(Thing t) { return t?.HasAncestorLabeled(ancestorLabel) == true; }
            Thing HasProperty(Thing t) { return t?.HasRelationshipWithAncestorLabeled(ancestorLabel); }
            List<Thing> resultList = new();
            foreach (Relationship rv in queryResult)
            {
                if (HasAncestor(rv.source))
                    resultList.Add(rv.source);
                else if (HasAncestor(rv.target))
                    resultList.Add(rv.target);
                else if (HasProperty(rv.source) is Thing t)
                    resultList.Add(t);
                else if (HasProperty(rv.target) is Thing t1)
                    resultList.Add(t1);
                else if (HasProperty(rv.reltype) is Thing t2)
                    resultList.Add(t2);
                foreach (ClauseType c in rv.clauses)
                    resultList.AddRange(GetThingsWithAncestor(new List<Relationship> { c.clause }, ancestorLabel));
            }
            resultList = resultList.Distinct().ToList();
            return resultList;
        }


        public IList<Relationship> Query(
                object source, object relationshipType = null, object target = null,
                object sourceModifiers = null, object relationshipModifiers = null, object targetModifiers = null
                )
        {
            Thing Source = ThingFromObject(source);
            Thing RelationshipType = ThingFromObject(relationshipType);
            Thing Target = ThingFromObject(target);
            List<Thing> SourceModifiers = ThingListFromObject(sourceModifiers);
            List<Thing> RelationshipTypeModifiers = ThingListFromObject(relationshipModifiers, "action");
            List<Thing> TargetModifiers = ThingListFromObject(targetModifiers);

            QueryRelationship q = new QueryRelationship
            {
                source = Source,
                relType = RelationshipType,
                target = Target,
                sourceProperties = SourceModifiers,
                typeProperties = RelationshipTypeModifiers,
                targetProperties = TargetModifiers,
            };
            IList<Relationship> queryResult = SearchRelationships(q);
            return queryResult;
        }

        private bool HandlePronouns(QueryRelationship query)
        {
            bool handled = false;
            //currently handles "this" and "that" as targets only
            Thing prounounReplacement = GetOrAddThing("pronounReplacements", "Object");
            if (query.source != null && query.source.HasAncestorLabeled("pronoun"))
            {
                Thing person = query.source.Relationships.FindFirst(x => x.target != null && x.target.HasAncestorLabeled("personPro"))?.target;
                switch (person?.Label)
                {
                    case "1st":
                        Thing currentSpeaker = GetOrAddThing("currentSpeaker", "pronounReplacements");
                        if (query.reltype?.Label == "is")
                        {
                            DeleteAllChildren(currentSpeaker);
                            query.source = currentSpeaker;
                            query.reltype = UKS.GetOrAddThing("has-child", "RelationshipType");
                            if (!query.target.HasAncestorLabeled("person") && !query.target.HasAncestorLabeled("pronounType"))
                                AddStatement(query.target, "is-a", "person");
                            handled = true;
                        }
                        else
                        {
                            if (currentSpeaker.Children.Count == 1)
                                query.source = currentSpeaker.Children[0];
                            else
                                return true;  //error, reference to I with no antecedant
                        }
                        break;
                    case "2nd":
                        Thing self = GetOrAddThing("self", "pronounReplacements");
                        if (query.reltype?.Label == "is")
                        {
                            DeleteAllChildren(self);
                            query.source = self;
                            query.reltype = UKS.GetOrAddThing("has-child", "RelationshipType");
                            if (!query.target.HasAncestorLabeled("person") && !query.target.HasAncestorLabeled("pronounType"))
                                AddStatement(query.target, "is-a", "person");
                            handled = true;
                        }
                        else
                        {
                            if (self.Children.Count == 1)
                                query.source = self.Children[0];
                            else
                                return true;  //error, reference to you with no antecedant
                        }
                        break;
                }
            }
            if (query.target != null && query.target.HasAncestorLabeled("pronoun"))
            {
                Thing person = query.target.Relationships.FindFirst(x => x.target != null && x.target.HasAncestorLabeled("personPro"))?.target;
                switch (person?.Label)
                {
                    case "1st":
                        Thing currentSpeaker = GetOrAddThing("currentSpeaker", "pronounReplacements");
                        if (currentSpeaker.Children.Count == 1)
                            query.target = currentSpeaker.Children[0];
                        else
                            return true;  //error, reference to I with no antecedant
                        break;
                    case "2nd":
                        Thing self = GetOrAddThing("self", "pronounReplacements");
                        if (self.Children.Count == 1)
                            query.target = self.Children[0];
                        else
                            return true;  //error, reference to I with no antecedant
                        break;
                }
                if (query.target.Label == "this" || query.target.Label == "that")
                {
                    Thing physicalObject = null;
                    /*
                    if (FindModule("MentalModel") is ModuleMentalModel mm)
                    {
                        Angle a = (Angle)(Labeled("CameraPan").V);
                        IList<Thing> objects = UKS.SearchByAngle(UKS.Labeled("MentalModel").Children, a);
                        if (objects.Count > 0) physicalObject = objects[0];
                        if (physicalObject == null)
                            return true;
                        //                        throw new Exception("Nothing found for 'this'");
                        query.target = physicalObject;
                        mm.MentalModelChanged = true;
                    }
                    */
                }
            }
            return handled;
        }

        IList<Relationship> SearchRelationships(QueryRelationship searchMask, bool doInheritance = true)
        {
            List<Relationship> queryResult = new();
            HandlePronouns(searchMask);
            Thing invType = CheckForInverse(searchMask.relType);
            //if this relationship has an inverse, switcheroo so we are storing consistently in one direction
            if (invType != null)
            {
                (searchMask.source, searchMask.target) = (searchMask.target, searchMask.source);
                (searchMask.sourceProperties, searchMask.targetProperties) = (searchMask.targetProperties, searchMask.sourceProperties);
                (searchMask.relType, invType) = (invType, searchMask.relType);
            }

            //handle pronouns in statements
            if (HandlePronouns(searchMask)) return queryResult;

            //first get all the relationships from the given source or target (whichever is specified)
            if (searchMask.source != null)
            {
                if (doInheritance)
                    queryResult = RelationshipWithInheritance(searchMask.source); //searches parents
                else
                    queryResult = (List<Relationship>)searchMask.source.Relationships;
                //List<Relationship> hasProperties = new List<Relationship>();
                //foreach (Relationship q in queryResult)
                //{
                //    hasProperties.AddRange(RelationshipByWithInheritance(q.target));
                //}
                //for (int i = 0; i < hasProperties.Count; i++)
                //{
                //    Relationship r = hasProperties[i];
                //    if (r.target != searchMask.target)
                //    {
                //        hasProperties.RemoveAt(i);
                //        i--;
                //    }
                //}
                //if (hasProperties.Count > 0)
                //    queryResult.AddRange(hasProperties);
            }
            else if (searchMask.target != null)
            {
                if (doInheritance)
                    queryResult = RelationshipByWithInheritance(searchMask.target); //searches children
                else
                    queryResult = (List<Relationship>)searchMask.target.RelationshipsFrom;
            }
            else if (searchMask.relType != null) //both source and target are null
            {
                queryResult = RelationshipByWithInheritance(searchMask.relType, null, 4, true);
            }
            else
            {
                return null;
            }
            //handle conditionals
            var listOfFalseConditions = queryResult.FindAll(r => !ConditionsAreMet(r.clauses, searchMask));
            if (listOfFalseConditions.Count > 0)
            {
                foreach (Relationship condition in listOfFalseConditions)
                {
                    queryResult.RemoveAll(r => r.reltype == condition.relType && r.target.HasAncestor(condition.target));
                }
            }
            queryResult.RemoveAll(r => r.clauses.FindFirst(c => c.a == AppliesTo.condition) != null);

            //then remove the ones which don't match the search criteria
            for (int i = queryResult.Count - 1; i >= 0; i--)
            {
                Relationship r = queryResult[i];
                //if (searchMask.relType != null && HasProperty(searchMask.relType, "transitive") &&
                //    r.relType != null && HasProperty(r.relType, "transitive")) continue;
                if (r.weight < 0.5)
                {
                    queryResult.RemoveAt(i);
                }
                else if (searchMask.source != null && r.source != null && !ThingInTree(r.source, searchMask.source))
                {
                    queryResult.RemoveAt(i);
                }
                else if (searchMask.source == null && searchMask.target != null && searchMask.target != r.target &&
                    Relationship.TrimDigits(r.target?.Label) != searchMask.target?.Label)
                {
                    queryResult.RemoveAt(i);
                }
                else if (searchMask.sourceProperties.Count > 0 && r.source != null && !ModifiersMatch(searchMask.sourceProperties, r.source))
                {
                    queryResult.RemoveAt(i);
                }
                else if (searchMask.target != null && !ThingInTree(r.target, searchMask.target))
                {
                    queryResult.RemoveAt(i);
                }
                else if (searchMask.targetProperties.Count > 0 && r.target != null && !ModifiersMatch(searchMask.targetProperties, r.target))
                {
                    queryResult.RemoveAt(i);
                }
                else if (searchMask.reltype != null && r.reltype != null && !ThingInTree(r.reltype, searchMask.reltype))
                {
                    queryResult.RemoveAt(i);
                }
                else if (searchMask.typeProperties.Count > 0 && r.reltype != null && !ModifiersMatch(searchMask.typeProperties, r.reltype))
                {
                    queryResult.RemoveAt(i);
                }
                else if (!doInheritance && searchMask.target != r.target)
                {
                    queryResult.RemoveAt(i);
                }
            }

            //handle transitive relationships
            int paramCount = 0;
            if (searchMask.relType != null) paramCount++;
            if (searchMask.source != null) paramCount++;
            if (searchMask.target != null) paramCount++;
            if (paramCount > 1 && (searchMask.relType == null || (searchMask.relType != null && HasProperty(searchMask.relType, "transitive"))))
                queryResult = HandleTransitivieRelatinships(searchMask, invType, queryResult);

            queryResult = queryResult.OrderByDescending(x => x.lastUsed).ToList();
            return queryResult;
        }
        List<Relationship> failedConditions = new();
        List<Relationship> succeededConditions = new();
        bool ConditionsAreMet(List<ClauseType> clauses, Relationship query)
        {
            if (clauses.Count == 0) return true;
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
                var qResult = SearchRelationships(q);
                for (int i = qResult.Count - 1; i >= 0; i--)
                    if (qResult[i].weight < 0.8)
                        qResult.RemoveAt(i);
                if (qResult.Count == 0)
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
        private List<Relationship> RelationshipByWithInheritance(Thing t, List<Relationship> relationshipList = null, int depthLimit = 4, bool sourceIsNull = false)
        {
            depthLimit--;
            if (depthLimit == 0)
                return relationshipList;

            bool firstTime = false;
            if (relationshipList == null)
            {
                firstTime = true;
                relationshipList = new List<Relationship>();
            }

            bool ignoreSourceInternal = (depthLimit < 2); //do not add inherited relationships which differ only in source
            if (sourceIsNull) ignoreSourceInternal = false;
            foreach (Relationship r in t.RelationshipsFrom)
            {
                if (firstTime)
                    relationshipList.Add(r);
                else
                {
                    if (r.relType?.Label != "has-child")
                        AddRelationshipToList(r, relationshipList, false, ignoreSourceInternal);
                }
            }
            foreach (Thing t2 in t.Children)  //search the instances of
            {
                RelationshipByWithInheritance(t2, relationshipList, depthLimit, sourceIsNull);
            }

            return relationshipList;
        }



        private List<Relationship> RelationshipWithInheritance(Thing t, List<Relationship> relationshipList = null, int depthLimit = 8)
        {
            depthLimit--;
            if (depthLimit == 0)
                return relationshipList;
            bool firstTime = false;
            if (relationshipList == null)
            {
                firstTime = true;
                relationshipList = new List<Relationship>();
            }
            foreach (Relationship r in t.Relationships)
            {
                if (firstTime)
                    relationshipList.Add(r);
                else
                {
                    bool ignoreSource = depthLimit < 7;
                    if (r.relType?.Label != "has-child")
                        AddRelationshipToList(r, relationshipList, true, ignoreSource);
                }
            }
            foreach (Thing t2 in t.Parents)
            {
                RelationshipWithInheritance(t2, relationshipList, depthLimit);
            }
            return relationshipList;
        }
        public List<Thing> GeneralQuery(List<Thing> properties)
        {
            List<Thing> resultList = new();
            List<Thing> firingList = new();
            foreach (Thing t in properties) if (t != null) firingList.Add(t);

            for (int loopCount = 0; loopCount < 10 && resultList.Count == 0; loopCount++)
            {
                //check for result
                //is there a thing which has all the properties?
                foreach (Thing t in firingList)
                {
                    int count = MatchingPropertyCount(t, properties);
                    if (count == properties.Count)
                        if (!resultList.Contains(t)) resultList.Add(t);
                }
                if (resultList.Count == 0)
                {
                    //expand the firing list
                    //TODO, only expand with transitive properties
                    //startingCount keeps new things from being processed on this pass
                    int startingCount = firingList.Count;
                    for (int i = 0; i < startingCount; i++)
                    {
                        Thing t = firingList[i];
                        foreach (Relationship r in t.Relationships)
                        {
                            //HasProperty(r.relType, "transitive");
                            if (!firingList.Contains(r.target)) firingList.Add(r.target);
                        }
                        foreach (Relationship r in t.RelationshipsFrom)
                            if (!firingList.Contains(r.source)) firingList.Add(r.source);
                    }
                }
            }
            return resultList;
        }
        int MatchingPropertyCount(Thing t, List<Thing> properties)
        {
            //TODO expand this to handle transitive relationships
            int retVal = 0;
            foreach (Relationship r in t.Relationships)
                if (properties.Contains(r.target)) retVal++;
            foreach (Relationship r in t.RelationshipsFrom)
                if (properties.Contains(r.source)) retVal++;
            return retVal;
        }
        void AddRelationshipToList(Relationship r, List<Relationship> relations, bool checkExclusion = true, bool ignoreSource = false)
        {
            //only add the new property to the list if it does not conflict with one already in the list
            //don't add if it already exists
            Relationship existing = relations.FindFirst(x => RelationshipsAreEqual(x, r, ignoreSource));
            if (existing == null)
            {
                if (checkExclusion)
                {
                    foreach (Relationship r1 in relations)
                    {
                        if (RelationshipsAreExclusive(r, r1))
                            goto cantAdd1;
                    }
                }
                relations.Add(r);
            cantAdd1:
                { }
            }
        }

    }
}
