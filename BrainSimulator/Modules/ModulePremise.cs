//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System;
using System.Collections.Generic;
using System.Linq;
using static BrainSimulator.Modules.ModulePremise;
using static System.Math;

namespace BrainSimulator.Modules
{
    public class ModulePremise : ModuleBase
    {
        // Any public variable you create here will automatically be saved and restored  
        // with the network unless you precede it with the [XmlIgnore] directive
        // [XmlIgnore] 
        // public theStatus = 1;
        public static float evidential_horizon = 1.0f;


        // Set size parameters as needed in the constructor
        // Set max to be -1 if unlimited
        public ModulePremise()
        {
            minHeight = 1;
            maxHeight = 500;
            minWidth = 1;
            maxWidth = 500;
        }

        // Fill this method in with code which will execute
        // once for each cycle of the engine
        public override void Fire()
        {
            Init();  //be sure to leave this here
            UpdateDialog();
        }

        public static Relationship R1 { get; set; }
        public static Relationship R2 { get; set; }
        public List<List<List<List<(Relationship, string)>>>> CycleStatements = new List<List<List<List<(Relationship, string)>>>>();
        public List<(Relationship, string)> GenStatements = new List<(Relationship, string)>();


        public ModulePremise(Relationship r1, int cycles)
        {
            R1 = r1;
            //
            CycleStatements = Cycle(r1, cycles);
        }

        public ModulePremise(Relationship r1, Relationship r2)
        {
            R1 = r1;
            R2 = r2;
            GetUKS();
            Thing root = UKS.GetOrAddThing("Object", "Thing");
            Thing relrel = UKS.GetOrAddThing("==>", "Relationship");//implication
            //
            GenStatements = GeneratePremise(R1, R2);
            //Variable v = new Variable();
            //Relationship test = v.GenerateGrammarTable(R1, R2, relrel);
        }


        //This function checks if a relationship r1 already exists in the UKS.  If it does, then it changes r1 to that relationship.
        public bool IsRelationship(Relationship r1)
        {
            GetUKS();
            Thing relationships = UKS.GetOrAddThing("Object", "Thing");
            foreach (Thing t in relationships.Descendents)
            {
                foreach (Relationship r in t.Relationships)
                {
                    if (r1.relType == r.relType && r1.source == r.source && r1.T == r.T)
                    {
                        //r1 = r;
                        return true;
                    }
                }
            }
            return false;
        }
        //This funciton checks if two relationships, r1 and r2, are in the UKS and if they are equal.  If so, they are set to eachother and are set to the relationship in the UKS.
        public bool IsRelationship(Relationship r1, Relationship r2)
        {
            GetUKS();
            Thing relationships = UKS.GetOrAddThing("Object", "Thing");
            foreach (Thing t in relationships.Children)
            {
                foreach (Relationship r in t.Relationships)
                {
                    if ((r1.relType == r.relType && r1.source == r.source && r1.T == r.T) &&
                        (r2.relType == r.relType && r2.source == r.source && r2.T == r.T))
                    {
                        r1 = r2 = r;
                        return true;
                    }
                }
            }
            return false;
        }
        //This funciton changes the UKS relationship to the input relationship r1 if they are the same type, source, and T=target
        public void ChangeSourceRelationship(Relationship r1)
        {
            GetUKS();
            Thing relationships = UKS.GetOrAddThing("Object", "Thing");

            int childint = 0;
            int relationshipint = 0;
            int relint = 0;
            bool breakout = false;
            foreach (Thing t in relationships.Children)
            {
                int retint = 0;
                foreach (Relationship r in t.Relationships)
                {
                    if (r1.relType == r.relType && r1.source == r.source && r1.T == r.T)
                    {
                        childint = relint;
                        relationshipint = retint;
                        breakout = true;
                        break;
                    }
                    retint++;
                }
                if (breakout)
                    break;
                relint++;
            }
            if (relationships.Children[childint].Relationships.Count != 0 && breakout == true)
            {
                relationships.Children[childint].Relationships[relationshipint].UpdateRelationship(r1);
            }
            else
            {
                if (r1.source is Thing r1source && r1.T is Thing r1T)
                    r1source.AddRelationship(r1T, r1.relType);//, r1.sentencetype);
            }

        }

        public void GetHighestTruthValue(List<(Relationship, string)> relationships)
        {
            foreach ((Relationship, string) r1 in relationships)
            {
                foreach ((Relationship, string) r2 in relationships)
                {
                    if (r2.Item1.source == r1.Item1.source && r2.Item1.T == r1.Item1.T && r2.Item1.relType == r1.Item1.relType)
                    {
                        if (r2.Item1.sentencetype.belief.TRUTH.EXPECT > r1.Item1.sentencetype.belief.TRUTH.EXPECT)
                            relationships.Remove(r1);
                        else if (r2.Item1.sentencetype.belief.TRUTH.EXPECT < r1.Item1.sentencetype.belief.TRUTH.EXPECT)
                            relationships.Remove(r2);
                        else if (r2.Item1.source == null && r2.Item1.T == null)
                        {
                            relationships.Remove(r2);
                            relationships.Remove(r1);
                        }
                    }
                }
            }
        }

        public void DeconstructTerm(Relationship statement)
        {
            //Do this later.  Take a term and deconstruct it into its individual components like other relationships, terms, copulas, etc.

        }

        public void Priority()
        {
            //Do this later.
        }

        private double ConvertToMiliseconds(DateTime? d)
        {
            double miliseconds = (double)d?.Millisecond / 1000;
            double seconds = (double)d?.Second;
            double minutes = (double)d?.Minute * 60;
            double hours = (double)d?.Hour * 3600;
            double days = (double)d?.Day * 24 * 3600;
            double total = miliseconds + seconds + minutes + hours + days;
            return total;
        }

        public List<List<List<List<(Relationship, string)>>>> Cycle(Relationship r1, int numCycles)
        {
            GetUKS();
            Tense tense = new Tense();
            List<List<List<List<(Relationship, string)>>>> listlistlistlist = new List<List<List<List<(Relationship, string)>>>>();
            Thing obj = UKS.GetOrAddThing("Object", "Thing");
            for (int i = 0; i < numCycles; i++)
            {
                List<List<List<(Relationship, string)>>> listlistlist = new List<List<List<(Relationship, string)>>>();
                lock (obj.Children)
                {
                    foreach (Thing t in obj.Descendents)
                    {

                        //if (t.RelationshipsBy.Count != 0)
                        if(1 == 1)
                        {
                            List<List<(Relationship, string)>> listlist = new List<List<(Relationship, string)>>();
                            lock (t.Relationships)
                            {
                                foreach (Relationship r2 in t.Relationships)
                                {
                                    double test1 = ConvertToMiliseconds(r1.lastUsed);
                                    double test2 = ConvertToMiliseconds(r2.lastUsed);
                                //Make sure the entry does not perform a revision with itself
                                    if (r1 == r2 && ((r1.sentencetype.belief.Tense?.ToString() == r2.sentencetype.belief.Tense?.ToString()  && 
                                        Abs(test1 - test2) > 1 ) || 
                                        r1.sentencetype.belief.Tense?.ToString() != r2.sentencetype.belief.Tense?.ToString() && 
                                        r1.sentencetype.belief.Tense != null && r2.sentencetype.belief.Tense != null))
                                    {
                                        //if (GetNewTense(r1,r2,tense))
                                        //{
                                            List<(Relationship, string)> list = new List<(Relationship, string)>();
                                            if (r2.source != null || r2.T != null)
                                            {
                                                list = GeneratePremise(r1, r2);
                                            }
                                            if (list.Count != 0)
                                                listlist.Add(list);


                                            Relationship newRelationship;
                                            if (IsRelationship(r1))
                                                if (r1.source is Thing r1source && r1.T is Thing r1T)
                                                {
                                                    newRelationship = r1source.AddRelationship(r1T, r1.relType/*, r1.sentencetype new SentenceType(type, tense, TruthValue, desire)*/);
                                                    if (r1.sentencetype.belief != null)
                                                    {
                                                        newRelationship.sentencetype.belief.Tense = tense;
                                                        //newRelationship.sentencetype.belief.TRUTH = list;
                                                    }
                                                }
                                        //}
                                    }
                                }
                            }
                            if (listlist.Count != 0)
                                listlistlist.Add(listlist);
                        }
                    }
                }

                if (listlistlist.Count != 0)
                    listlistlistlist.Add(listlistlist);
            }
            return listlistlistlist;
        }

        public List<(Relationship, string)> GeneratePremise(Relationship s1, Relationship s2)
        {
            GetUKS();
            List<Thing> relationshipTypeList = new List<Thing>();
            relationshipTypeList.Add(UKS.GetOrAddThing("Object", "Thing"));
            relationshipTypeList.Add(UKS.GetOrAddThing(" --> ", "Relationship"));//inheritance
            relationshipTypeList.Add(UKS.GetOrAddThing(" similar to ", "Relationship"));//simmilarity <->
            relationshipTypeList.Add(UKS.GetOrAddThing(" ~ ", "Relationship"));//intensional difference ( see (-))
            relationshipTypeList.Add(UKS.GetOrAddThing(" - ", "Relationship"));//extensional difference
            relationshipTypeList.Add(UKS.GetOrAddThing(" and ", "Relationship"));//extensional intersection (see ∩) &
            relationshipTypeList.Add(UKS.GetOrAddThing(" or ", "Relationship"));//intensional intersection (see ∪) |
            relationshipTypeList.Add(UKS.GetOrAddThing(" with ", "Relationship"));//product *
            relationshipTypeList.Add(UKS.GetOrAddThing(" ==> ", "Relationship"));//implication
            relationshipTypeList.Add(UKS.GetOrAddThing(" =/> ", "Relationship"));//predictive implication
            relationshipTypeList.Add(UKS.GetOrAddThing(" =\\> ", "Relationship"));//retrospective implication
            relationshipTypeList.Add(UKS.GetOrAddThing(" is equivalent to", "Relationship"));//equivalence (iff) <=>
            relationshipTypeList.Add(UKS.GetOrAddThing(" </> ", "Relationship"));//predictive equivalence
            relationshipTypeList.Add(UKS.GetOrAddThing(" <|> ", "Relationship"));//concurrent equivalence
            relationshipTypeList.Add(UKS.GetOrAddThing(" && ", "Relationship"));//conjunction
            relationshipTypeList.Add(UKS.GetOrAddThing(" \\ ", "Relationship"));//intensional image
            relationshipTypeList.Add(UKS.GetOrAddThing(" / ", "Relationship"));//extensional image
            relationshipTypeList.Add(UKS.GetOrAddThing(" -- ", "Relationship"));//negation (for statements, not terms)
            relationshipTypeList.Add(UKS.GetOrAddThing(" or ", "Relationship"));//disjunction ||
            relationshipTypeList.Add(UKS.GetOrAddThing(" &/ ", "Relationship"));//sequential events
            relationshipTypeList.Add(UKS.GetOrAddThing(" &| ", "Relationship"));//parallel events
            // Generate the various syllogisms and add them to statements
            List<(Relationship, string)> statements = new List<(Relationship, string)>();
            //
            Revision rev = new Revision(s1, s2, relationshipTypeList);
            Deduction ded = new Deduction(s1, s2, relationshipTypeList);
            Analogy ana = new Analogy(s1, s2, relationshipTypeList);
            Resemblance res = new Resemblance(s1, s2, relationshipTypeList);
            Abduction abd1 = new Abduction(s1, s2, relationshipTypeList);
            Abduction abd2 = new Abduction(s2, s1, relationshipTypeList) { conversionbool = true };
            Induction ind1 = new Induction(s1, s2, relationshipTypeList);
            Induction ind2 = new Induction(s2, s1, relationshipTypeList) { conversionbool = true };
            Exemplification exe = new Exemplification(s1, s2, relationshipTypeList);
            Comparison com1 = new Comparison(s1, s2, relationshipTypeList);
            Comparison com2 = new Comparison(s2, s1, relationshipTypeList);
            //Intersection ins = new Intersection(s1, s2, relationshipTypeList);
            //Union uni = new Union(s1, s2, relationshipTypeList);
            //Difference dif1 = new Difference(s1, s2, relationshipTypeList);
            //Difference dif2 = new Difference(s2, s1, relationshipTypeList) { conversionbool = true };
            statements.Add((ded.S, "Deduction"));
            statements.Add((rev.S, "Revision"));
            statements.Add((ana.S, "Analogy"));
            statements.Add((res.S, "Resemblance"));
            statements.Add((abd1.S, "Abduction+"));
            statements.Add((abd2.S, "Abduction-"));
            statements.Add((ind1.S, "Induction+"));
            statements.Add((ind2.S, "Induction-"));
            statements.Add((exe.S, "Exemplification"));
            statements.Add((com1.S, "Comparison+"));
            statements.Add((com2.S, "Comparison-"));
            //statements.Add((ins.S, "Intersection"));
            //statements.Add((uni.S, "Union"));
            //statements.Add((dif1.S, "Difference+"));
            //statements.Add((dif2.S, "Difference-"));

            //Clean up Syllogisms that didn't return a statement
            foreach ((Relationship, string) s in statements.ToList())
            {
                if (s.Item1.source == null && s.Item1.T == null && s.Item1.relType == null || s.Item1.source == s.Item1.T)
                {
                    statements.Remove(s);
                }
                else
                {
                    //Thing source = UKS.GetOrAddThing(s.Item1.source.Label, relationshipTypeList[0]);
                    //Thing target = UKS.GetOrAddThing(s.Item1.T.Label, relationshipTypeList[0]);
                    ChangeSourceRelationship(s.Item1);
                }
            }
            return statements;
        }


        //Work on this stuff later=============================================================================
        public void SearchRelationshipsAsThings(Relationship rel)
        {
            foreach(Thing t in UKS.Labeled("Object").Children)
            {
                if (rel.source == t)
                {
                    foreach(Relationship r in t.Relationships)
                    {

                    }
                }
                else if (rel.T == t)
                {
                    foreach (Relationship r in t.Relationships)
                    {

                    }
                }
                else
                {

                }
            }
        }

        //Return propper tense (work on getting tenses for query later)
        public bool GetNewTense(Relationship r1, Relationship r2, Tense newRelationshipTense)
        {
            if ((r1.sentencetype.belief != null || r1.sentencetype.goal != null) && (r2.sentencetype.belief != null || r2.sentencetype.goal != null))
            {
                if (r1.sentencetype.GetTense() == null && r2.sentencetype.GetTense() == null)
                {
                    newRelationshipTense = null;
                    return true;
                }
                else if (r1.sentencetype.GetTense() != null && r2.sentencetype.GetTense() == null)
                {
                    newRelationshipTense = null;
                    return true;
                }
                else if (r1.sentencetype.GetTense() != null && r2.sentencetype.GetTense() != null)
                {
                    if (r1 != r2)
                    {
                        newRelationshipTense = new Tense(DateTime.Now);
                        return true;
                    }
                    else
                        return false;
                }
                else//eternal and current
                {
                    if (r1 != r2)
                    {
                        newRelationshipTense = new Tense(DateTime.Now);
                        return true;
                    }
                    else
                        return false;
                }
            }
            else // for goals
                return false;
        }

        // Same statement conversion
        public static TruthValue Conversion(TruthValue tv1)
        {
            return new TruthValue { F = 1, C = tv1.F * tv1.C / (tv1.F * tv1.C + evidential_horizon) };
        }

        public static TruthValue Negation(TruthValue tv1)
        {
            return new TruthValue { F = 1 - tv1.F, C = tv1.C };
        }

        public static TruthValue Contraposition(TruthValue tv1)
        {
            return new TruthValue { F = 0, C = (1 - tv1.F) * tv1.C / ((1 - tv1.F) * tv1.C + evidential_horizon) };
        }
        // =======

        // Fill this method in with code which will execute once
        // when the module is added, when "initialize" is selected from the context menu,
        // or when the engine restart button is pressed
        public override void Initialize()
        {
            GetUKS();
        }

        // The following can be used to massage public data to be different in the xml file
        // delete if not needed
        public override void SetUpBeforeSave()
        {
        }
        public override void SetUpAfterLoad()
        {
        }

        // Called whenever the size of the module rectangle changes
        // for example, you may choose to reinitialize whenever size changes
        // delete if not needed
        public override void SizeChanged()
        {
            if (mv == null) return; //this is called the first time before the module actually exists
        }

        // called whenever the UKS performed an Initialize()
        public override void UKSInitializedNotification()
        {

        }
    }

    public class Variable
    {
        //Narseese grammar = new Narseese();
        Relationship newRelationship1 { get; set; } = new Relationship();
        Relationship newRelationship2 { get; set; } = new Relationship();
        Relationship newRelationship3 { get; set; } = new Relationship();
        private Thing v;
        public Thing variable
        {
            get { return v; }
            set { v = value; }
        }

        public Relationship GenerateGrammarTable(Relationship r1, Relationship r2, Thing relType)
        {
            /*                newRelationship1 = r1;
                            newRelationship2 = r2;

                            //grammar.FindInGrammarTable("d", "d");
                            newRelationship3.source = r1;
                            newRelationship3.T = r2;
                            newRelationship3.relationshipType = relType;
                            newRelationship3.sentencetype.belief.TRUTH.F = r2.sentencetype.belief.TRUTH.F;
                            newRelationship3.sentencetype.belief.TRUTH.C = r1.sentencetype.belief.TRUTH.F * r1.sentencetype.belief.TRUTH.C * R2.sentencetype.belief.TRUTH.C / (r1.sentencetype.belief.TRUTH.F * r1.sentencetype.belief.TRUTH.C * r2.sentencetype.belief.TRUTH.C + evidential_horizon);
                            newRelationship3.sentencetype.belief.TRUTH.EXPECT = newRelationship3.sentencetype.belief.TRUTH.sentencetype.belief.TRUTHValue(newRelationship3.sentencetype.belief.TRUTH.F, newRelationship3.sentencetype.belief.TRUTH.C);*/
            return newRelationship3;
        }

        /*            public void GenerateVariable(Relationship r1, Relationship r2, List<Thing> relationshipTypeS)
                    {
                        Thing t1 = new Thing();
                        Thing t2 = new Thing();
                        Thing copula = new Thing();
                        //extensional
                        if (r1.source == r2.source)
                        {
                            newRelationship1 = variable.AddRelationship(r1.T, r1.relationshipType);
                            newRelationship2 = variable.AddRelationship(r2.T, r2.relationshipType);
                            t1.Label = newRelationship1.source.Label + " " + newRelationship1.relationshipType.Label + " " + newRelationship1.T.Label;
                            t2.Label = newRelationship2.source.Label + " " + newRelationship2.relationshipType.Label + " " + newRelationship2.T.Label;
                        }
                        //intensional
                        else if (r1.T == r2.T)
                        {
                            newRelationship1 = variable.AddRelationship(r1.source, r1.relationshipType);
                            newRelationship2 = variable.AddRelationship(r2.source, r2.relationshipType);
                            t1.Label = newRelationship1.source.Label + " " + newRelationship1.relationshipType.Label + " " + newRelationship1.T.Label;
                            t2.Label = newRelationship2.source.Label + " " + newRelationship2.relationshipType.Label + " " + newRelationship2.T.Label;
                        }
                        foreach (Thing t in relationshipTypeS[0].Children)
                        {
                            if (t.Label == t1.Label)
                                t1 = t;
                            if (t.Label == t2.Label)
                                t2 = t;
                        }
                        if (!t1.HasAncestor(relationshipTypeS[0]))
                            t1.AddParent(relationshipTypeS[0]);
                        if (!t2.HasAncestor(relationshipTypeS[0]))
                            t2.AddParent(relationshipTypeS[0]);

                        //Thing? cop2 = t3.HasAncestor(t4) ? hasancestor : implies;
                        if (newRelationship1.relationshipType == relationshipTypeS[1])
                            newRelationship3 = t1.AddRelationship(t2, relationshipTypeS[11]);
                        else if (newRelationship1.relationshipType == relationshipTypeS[2])
                            newRelationship3 = t1.AddRelationship(t2, relationshipTypeS[14]);

                    }        */
        public void GenerateVariable(Relationship r)
        {

        }
        public void GenerateVariable(Thing t1, Thing t2)
        {

        }
        public void GenerateVariable(Thing t)
        {

        }
    }

    public class Narseese
    {
        private Dictionary<string, string> GrammarDictionary = new Dictionary<string, string>
            {
                { "task", "sentence" },
                { "sentence", "belief" },
                { "sentence", "question" },
                { "sentence", "goal" },
                //
                { ".", "statement" },//belief
                { "!", "statement" },//goal
                { "?", "statement" },//question
                //
                { "copula", "-->" },
                { "copula", "<->" },
                { "copula", "==>" },
                { "copula", "=/>" },
                { "copula", "=\\>" },
                { "copula", "=|>" },
                { "copula", "<=>" },
                { "copula", "</>" },
                { "copula", "<|>" },
                //
                { "&", "term-operator" },
                { "|", "term-operator" },
                { "-", "term-operator" },
                { "~", "term-operator" },
                { "*", "term-operator" },
                { "/1", "term-operator" },
                { "/2", "term-operator" },
                { "\\1", "term-operator" },
                { "\\2", "term-operator" },
                //
                { "term", "word" },
                { "term", "set" },
                { "term", "var" },
                { "term", "(statement)" },
                { "term", "<statement>" },
                //
                { "var", "$ word" },
                { "var", "# word" },
                { "var", "? word" }
            };

        public void FindInGrammarTable(string key, string value)
        {
            KeyValuePair<string, string> kvp = new KeyValuePair<string, string>(key, value);
            GrammarDictionary.Contains(kvp);
        }
    }

    public class SentenceType
    {
        public Belief belief = null;
        public Goal goal = null;
        public Query query = null;
        public class Belief
        {
            public TruthValue TRUTH { get; set; }
            public Tense Tense { get; set; }
            public Belief(TruthValue TV, Tense T)
            {
                TRUTH = TV;
                Tense = T;
            }

            public Belief()
            {
                TRUTH = new TruthValue();
                Tense = new Tense();
            }
        }
        public class Goal
        {
            public Tense Tense { get; set; }
            public Goal()
            {
                Tense = new Tense();
            }
            public Goal(Tense tense)
            {
                Tense = tense;
            }
        }
        /*            private class Question : SentenceType
                    {
                        public Question()
                        {
                            bool istrue = true;
                        }
                    }*/
        public class Query
        {
            public Desire Desire { get; set; }
            public Query()
            {
                Desire = new Desire();
            }
            public Query(Desire desire)
            {
                Desire = desire;
            }
        }

        public SentenceType()
        {
            belief = new Belief();
        }

        public SentenceType(string type = null, Tense T = null, TruthValue TV = null, Desire D = null)
        {
            if (type == "." || type == null)
            {
                if (TV == null)
                    TV = new TruthValue();
                belief = new Belief(TV, T);
            }
            else if (type == "!")
            {
                goal = new Goal(T);
            }
            else if (type == "?")
            {
                if (D == null)
                    D = new Desire();
                query = new Query(D);
            }
        }

        public bool IsNullSentence()
        {
            if (belief == null && goal == null && query == null)
                return true;
            else
                return false;
        }

        public SentenceType NullSentenceType()
        {
            return new SentenceType();
        }

        public bool IsJudgementOrBelief()
        {
            if (belief != null)
            {
                return true;
            }
            else if (goal != null)
            {
                return false;
            }
            else if (query != null)//Query
            {
                return false;
            }
            else
                return false;
        }

        public Tense GetTense()
        {
            if (this.belief != null || this.goal != null)
            {
                if (belief.Tense?.t != null)
                    return belief.Tense;
                else
                    return null;
            }
            else
                return null;
        }

        public TruthValue GetTruthValue()
        {
            if (this.belief != null)
            {
                return this.belief.TRUTH;
            }
            else
                return null;
        }

        public Desire GetDesire()
        {
            if (this.query != null)
            {
                return this.query.Desire;
            }
            else
                return null;
        }

        public override string ToString()
        {
            if (goal != null)
                return "Goal";
            if (belief != null)
                return "Belief";
            if (query != null)
                return "Query";
            else
                return null;
        }
    }

    public class Tense
    {
        public DateTime? t;
        public DateTime? tense { get => t; set => this.t = value; }

        public Tense()
        {
            this.tense = null;
            if (this.tense == null)
                this.ToString();
        }

        public Tense(int tenseIndicator)
        {
            if (tenseIndicator < 0)
            {
                this.tense = DateTime.MinValue;
            }
            else if (tenseIndicator == 0)
            {
                this.tense = DateTime.Now;
            }
            else
            {
                this.tense = DateTime.MaxValue;
            }
        }

        public Tense(DateTime? tense)
        {
            this.tense = SpecificTime(tense);
        }

        private static DateTime? SpecificTime(DateTime? dt)
        {
            DateTime? currentTime = DateTime.Now;
            if (dt.HasValue)
            {
                if (currentTime.Value < dt.Value)
                {
                    // Future
                    currentTime = DateTime.MaxValue;
                }
                else if (currentTime > dt.Value)
                {
                    // Past
                    currentTime = DateTime.MinValue;
                }
            }
            else // Eternal
            {
                currentTime = null;
            }
            return currentTime;
        }

        public string ToString()
        {
            string retString = "eternal";
            if (this.tense != null)
            {
                if (this.tense == DateTime.MinValue)
                    retString = "past";
                else if (this.tense == DateTime.MaxValue)
                    retString = "future";
                else
                    retString = "current";
            }
            else if (this == null)
                retString = "eternal";
            return retString;
        }
    }

    public class TruthValue
    {
        //Frequency is defined as the proportion of positive evidence among total evidence, that is, (positive evidence)/(total evidence). For the example above,
        //frequency will be 9 / 10 = 0.9. As a special case, when the amount of total evidence is 0, frequency is defined to be 0.5.
        //Now consider another example: for the same statement "Ravens are black", the system encounters 9000 black ravens and 1000 white ravens, then the
        //frequency of the statement, in this case, will be as before: 9000/10000 = 0.9. However, it is clear that the statement "Ravens are black" is more
        //"certain" in this case than in the previous one.This is why a second measurement, confidence, is used to represent.
        //Confidence indicates how sensitive the corresponding frequency is with respect to new evidence, as it is defined as the proportion of total evidence
        //among total evidence plus a constant amount of new evidence, that is, (total evidence) / (total evidence + k) where k is a system parameter and in most
        //discussions takes the default value of 1. In the first example, confidence = 10 / (10 + 1), around 0.9, while in the second it is 10000/(10000 + 1),
        //around 0.99.
        //Thus frequency can be seen as the degree of belief system has for the statement and confidence as the degree of belief for that estimation of frequency.
        public float F { get; set; }
        public float C { get; set; }
        public float EXPECT { get; set; }
        public TruthValue()
        {
            F = 1.0f;
            C = 0.9f;
            EXPECT = TRUTHValue(F, C);
        }
        public TruthValue(float f, float c)
        {
            F = f;
            C = c;
            EXPECT = TRUTHValue(F, C);
        }

        public TruthValue(int wplus, int wminus)
        {
            F = (float)((float)wplus / ((float)wplus + (float)wminus));
            C = (float)(((float)wplus + (float)wminus) / ((float)wplus + (float)wminus + evidential_horizon));
            // Expectation value = (l + u) / 2 = the mean of the upper and lower bounds for the frequency
            EXPECT = TRUTHValue(F, C);
        }

        public float TRUTHValue(float f, float c)
        {
            return (c * (f - 0.5f) + 0.5f);
        }

        public override string ToString()
        {
            return "<F: " + F + ", C: " + C + ">=EXPECT: " + EXPECT;
        }
    }

    public class Desire
    {
        //During the run time of NARS there are usually multiple conflicting goals exist. Sometimes achieving one goal makes another one harder to be achieved, therefore
        //the system must make a decision on which goal to pursue from moment to moment. As goals may not be directly related to each other in content and thus the
        //system cannot be expected to have explicit knowledge on how to handle goal conflicts and competitions. To solve this problem, each goal is assigned a desire
        //value to indicate its relative significance to the system. It allows the system to compare goals and make a selection during goal conflicts.
        //Conceptually, the desire value of a goal G is defined to be the truth value of the implication statement G ==> D where D is a virtual statement representing
        //the desired state of the system.That is, "G is desired " is interpreted as "G implies the desired state". Consequently, desire values have the same format as
        //truth values, and are also grounded in the system's experience. The desire values of input goals can be assigned by the user to reflect their relative
        //importance or take default values. Derived goals get their desire values from desire-value functions that are part of the goal-derivation rules.
        public float F { get; set; }
        public float C { get; set; }
        public float EXPECT { get; set; }
        public Desire()
        {
            F = 1.0f;
            C = 0.9f;
            EXPECT = TRUTHValue(F, C);
        }
        public Desire(float f, float c)
        {
            F = f;
            C = c;
            EXPECT = TRUTHValue(F, C);
        }

        public Desire(int wplus, int wminus)
        {
            F = (float)((float)wplus / ((float)wplus + (float)wminus));
            C = (float)(((float)wplus + (float)wminus) / ((float)wplus + (float)wminus + evidential_horizon));
            // Expectation value = (l + u) / 2 = the mean of the upper and lower bounds for the frequency
            EXPECT = TRUTHValue(F, C);
        }

        public float TRUTHValue(float f, float c)
        {
            return (c * (f - 0.5f) + 0.5f);
        }

        public override string ToString()
        {
            return "<F: " + F + ", C: " + C + ">=EXPECT: " + EXPECT;
        }
    }

    public class Budget
    {
        //A bag is a data structure that is similar to a priority queue, except that the priority of each item in it does not indicate the order by which it is removed,
        //but the probability for that to happen. Therefore, the priority of an item may correspond to its importance or urgency, depending on the type of the item.
        //Intuitively, it expresses how "active" the item is.
        //Accurately speaking, the priority of an item is positively correlated with its probability to be selected in the next round of selection within the bag, that is,
        //the higher its priority, the higher its probability to be selected.Moreover, priority is a relative measurement, since the actual probability for an item to be
        //selected not only depends on its own priority value, but also the priority values of the other items in the same bag at the moment.
        public float Priority { get; set; }
        //Given the changing environment, it is necessary to adjust priority values during run-time.One of the principles in resource allocation is to favor the items that
        //are directly related to the current context.It means that the irrelevant items will be gradually ``forgotten.''
        //Since different items should be forgotten at different rates, the Durability of an item determines how quickly its priority should be decreased.It can be viewed as
        //an "aging" or "decaying" factor of the corresponding priority value. That is, after a constant amount of time, the priority value of an item is decreased from P0
        //to P1 = P0 * D, where D is the durability value of the item.
        //Ideally, the priority values of all data items in the system should decay all the time in this manner, the forgetting mechanism cannot be directly implemented in
        //this way, given the insufficient processing time.Therefore, in each working cycle, only a constant number of data items are "touched" and have their budget values
        //adjusted afterward.One of the aspects of the adjustment is to change the priority value from P0 to P1 = P0 * D ^ T, where T is the number of cycles since the
        //previous priority decay, that is, this adjustment achieves the accumulated effects of T adjustments the item should have gone through.
        public float Durability { get; set; }
        //Quality shows the long-term significance of a data item to the system, independent to the current context.
        ////It is evaluated differently for different types of item and may be adjusted according to the collected feedback on the usage of the item.
        //Quality is a measure for "did the selection of the data item led to find answers to questions, or to the fulfillment of a goal?",
        //and more generic considerations such as "how much evidence was summarized by the inference?" [An Attentional Control Mechanism for Reasoning and Learning - page 5)] .
        //In the OpenNARS implementation, the quality value of an item corresponds to a priority value that does not decay anymore, so can be seen as its "baseline activation".
        public float Quality { get; set; }
    }

    //=====================================================================================================

    public class Difference
    {
        public bool conversionbool = false;
        public Relationship S1 { get; set; }
        public Relationship S2 { get; set; }
        public Relationship S = new Relationship();
        public Difference(Relationship s1, Relationship s2, List<Thing> relationshipTypeS)
        {
            // Difference - Term Composition
            if (((s1.T == s2.T && s1.source != s2.source) || (s1.source == s2.source && s1.T != s2.T)) && s1.relType == s2.relType)
            {
                S1 = s1;
                S2 = s2;
                //Two beliefs
                if ((s1.sentencetype?.belief != null && s2.sentencetype?.belief != null))
                {
                    //(S.sentencetype.belief.sentencetype.belief.TRUTH as TruthValue).C = (s1.sentencetype.belief.sentencetype.belief.TRUTH as TruthValue).C * (s2.sentencetype.belief.sentencetype.belief.TRUTH as TruthValue).C;
                    S.sentencetype = new();
                    S.sentencetype.belief = new SentenceType.Belief()
                    {
                        TRUTH = new TruthValue()
                        {
                            F = s1.sentencetype.belief.TRUTH.F * (1 - s2.sentencetype.belief.TRUTH.F),
                            C = s1.sentencetype.belief.TRUTH.C * s2.sentencetype.belief.TRUTH.C
                        },
                        Tense = new Tense()
                    };

                    ModulePremise mp = new ModulePremise();
                    mp.GetUKS();
                    Relationship firstTermRel = new Relationship();
                    Thing firstTerm = new Thing();
                    if (s1.T == s2.T)
                    {
                        //This is where I will change stuff.
                        if (s1.source is Thing s1source && s2.source is Thing s2source)
                        {
                            string Label = s1source.Label + relationshipTypeS[3].Label + s2source.Label;
                            firstTerm = mp.UKS.GetOrAddThing(Label, relationshipTypeS[0]);
                        }
                        if (conversionbool)
                            S.sentencetype.belief.TRUTH = Conversion(S.sentencetype.belief.TRUTH);
                        S.source = firstTerm;
                        S.T = S1.T;
                        S.relType = relationshipTypeS[1];
                    }
                    else
                    {
                        if (s1.T is Thing s1T && s2.T is Thing s2T)
                        {
                            string Label = s1T.Label + relationshipTypeS[4].Label + s2T.Label;
                            firstTerm = mp.UKS.GetOrAddThing(Label, relationshipTypeS[0]);
                        }
                        //if (conversionbool)
                        //    S.sentencetype.belief.TRUTH = Conversion(S.sentencetype.belief.TRUTH);
                        S.relType = relationshipTypeS[1];
                        S.source = S1.source;
                        S.T = firstTerm;
                    }
                }
            }
        }
        public override string ToString()
        {
            return "F: " + S.sentencetype.belief.TRUTH.F + ", C: " + S.sentencetype.belief.TRUTH.C;
        }
    }

    public class Union
    {
        public Relationship S1 { get; set; }
        public Relationship S2 { get; set; }
        public Relationship S = new Relationship();
        public Union(Relationship s1, Relationship s2, List<Thing> relationshipTypeS)
        {
            // Union - Term Composition
            if (((s1.T == s2.T && s1.source != s2.source) || (s1.source == s2.source && s1.T != s2.T)) && s1.relType == s2.relType)
            {
                S1 = s1;
                S2 = s2;
                //Two beliefs
                if ((s1.sentencetype?.belief != null && s2.sentencetype?.belief != null))
                {
                    S.sentencetype = new();
                    S.sentencetype.belief = new SentenceType.Belief()
                    {
                        TRUTH = new TruthValue()
                        {
                            F = s1.sentencetype.belief.TRUTH.F + s2.sentencetype.belief.TRUTH.F - s1.sentencetype.belief.TRUTH.F * s2.sentencetype.belief.TRUTH.F,
                            C = s1.sentencetype.belief.TRUTH.C * s2.sentencetype.belief.TRUTH.C
                        },
                        Tense = new Tense()
                    };

                    ModulePremise mp = new ModulePremise();
                    mp.GetUKS();
                    Relationship firstTermRel = new Relationship();
                    Thing firstTerm = new Thing();
                    if (s1.T == s2.T)
                    {
                        if (s1.source is Thing s1source && s2.source is Thing s2source)
                        {
                            string Label = s1source.Label + relationshipTypeS[5].Label + s2source.Label;
                            firstTerm = mp.UKS.GetOrAddThing(Label, relationshipTypeS[0]);
                            /*                            firstTermRel = s1source.AddRelationship(s2source, relationshipTypeS[5], s1.sentencetype);
                                                        foreach (Thing t in relationshipTypeS[0].Children)
                                                        {
                                                            if (t.Label == "UnknownRelationship")
                                                                foreach (Thing tt in t.Children)
                                                                {
                                                                    if (firstTermRel.Label == tt.Label)
                                                                    {
                                                                        firstTerm = tt;
                                                                        break;
                                                                    }
                                                                }
                                                        }
                                                        if (!firstTerm.HasAncestor(relationshipTypeS[0]))
                                                            firstTerm.AddParent(relationshipTypeS[0]);*/
                            S.relType = relationshipTypeS[1];
                            S.source = firstTerm;
                            S.T = s1.T;
                        }
                    }
                    else
                    {
                        if (s1.T is Thing s1T && s2.T is Thing s2T)
                        {
                            string Label = s1T.Label + relationshipTypeS[6].Label + s2T.Label;
                            firstTerm = mp.UKS.GetOrAddThing(Label, relationshipTypeS[0]);
                            /*                            firstTermRel = s1T.AddRelationship(s2T, relationshipTypeS[6], s1.sentencetype);
                                                        foreach (Thing t in relationshipTypeS[0].Children)
                                                        {
                                                            if (t.Label == "UnknownRelationship")
                                                                foreach (Thing tt in t.Children)
                                                                {
                                                                    if (firstTermRel.Label == tt.Label)
                                                                    {
                                                                        firstTerm = tt;
                                                                        break;
                                                                    }
                                                                }
                                                        }
                                                        if (!firstTerm.HasAncestor(relationshipTypeS[0]))
                                                            firstTerm.AddParent(relationshipTypeS[0]);*/
                            S.relType = relationshipTypeS[1];
                            S.source = firstTerm;
                            S.T = s1.source;
                        }
                    }
                }
            }
        }
        public override string ToString()
        {
            return "F: " + S.sentencetype.belief.TRUTH.F + ", C: " + S.sentencetype.belief.TRUTH.C;
        }
    }

    public class Intersection
    {
        public Relationship S1 { get; set; }
        public Relationship S2 { get; set; }
        public Relationship S = new Relationship();
        public Intersection(Relationship s1, Relationship s2, List<Thing> relationshipTypeS)
        {
            // Intersection - Term Composition
            if (((s1.T == s2.T && s1.source != s2.source) || (s1.source == s2.source && s1.T != s2.T)) && s1.relType == s2.relType)
            {
                S1 = s1;
                S2 = s2;
                //Two beliefs
                if ((s1.sentencetype?.belief != null && s2.sentencetype?.belief != null))
                {
                    S.sentencetype = new();
                    S.sentencetype.belief = new SentenceType.Belief()
                    {
                        TRUTH = new TruthValue()
                        {
                            F = s1.sentencetype.belief.TRUTH.F * s2.sentencetype.belief.TRUTH.F,
                            C = s1.sentencetype.belief.TRUTH.C * s2.sentencetype.belief.TRUTH.C
                        },
                        Tense = new Tense()
                    };

                    ModulePremise mp = new ModulePremise();
                    mp.GetUKS();
                    Relationship firstTermRel = new Relationship();
                    Thing firstTerm = new Thing();
                    if (s1.T == s2.T)
                    {
                        if (s1.source is Thing s1source && s2.source is Thing s2source)
                        {
                            /*firstTermRel = s1source.AddRelationship(s2source, relationshipTypeS[6], s1.sentencetype);
                                foreach (Thing t in relationshipTypeS[0].Children)
                                {
                                    if (t.Label == "UnknownRelationship")
                                        foreach (Thing tt in t.Children)
                                        {
                                            if (firstTermRel.Label == tt.Label)
                                            {
                                                firstTerm = tt;
                                                break;
                                            }
                                        }
                                }*/
                            string Label = s1source.Label + relationshipTypeS[6].Label + s2source.Label;
                            firstTerm = mp.UKS.GetOrAddThing(Label, relationshipTypeS[0]);

                            S.relType = relationshipTypeS[1];
                            S.source = firstTerm;
                            S.T = s1.T;
                        }
                    }
                    else
                    {
                        if (s1.T is Thing s1T && s2.T is Thing s2T)
                        {
                            string Label = s1T.Label + relationshipTypeS[5].Label + s2T.Label;
                            firstTerm = mp.UKS.GetOrAddThing(Label, relationshipTypeS[0]);
                            /*                            firstTermRel = s1T.AddRelationship(s2T, relationshipTypeS[5], s1.sentencetype);
                                                        foreach (Thing t in relationshipTypeS[0].Children)
                                                        {
                                                            if (t.Label == "UnknownRelationship")
                                                                foreach (Thing tt in t.Children)
                                                                {
                                                                    if (firstTermRel.Label == tt.Label)
                                                                    {
                                                                        firstTerm = tt;
                                                                        break;
                                                                    }
                                                                }
                                                        }
                                                        if (!firstTerm.HasAncestor(relationshipTypeS[0]))
                                                            firstTerm.AddParent(relationshipTypeS[0]);*/
                            S.relType = relationshipTypeS[1];
                            S.source = firstTerm;
                            S.T = s1.source;
                        }
                    }
                }
            }
        }
        public override string ToString()
        {
            return "F: " + S.sentencetype.belief.TRUTH.F + ", C: " + S.sentencetype.belief.TRUTH.C;
        }
    }

    public class Comparison
    {
        public Relationship S1 { get; set; }
        public Relationship S2 { get; set; }
        public Relationship S = new Relationship();
        public Comparison(Relationship s1, Relationship s2, List<Thing> relationshipTypeS)
        {
            // Comparison - Weak Syllogism
            if ((s1.source == s2.source || s1.T == s2.T) && s1.relType == s2.relType && s1.source != s2.source)
            {
                S1 = s1;
                S2 = s2;
                //Two beliefs
                if ((s1.sentencetype?.belief != null && s2.sentencetype?.belief != null))
                {
                    S.sentencetype = new();
                    S.sentencetype.belief = new SentenceType.Belief() { TRUTH = new TruthValue(), Tense = new Tense()};
                    if (s1.sentencetype.belief.TRUTH.F + s2.sentencetype.belief.TRUTH.F - s1.sentencetype.belief.TRUTH.F * s2.sentencetype.belief.TRUTH.F != 0)
                        S.sentencetype.belief.TRUTH.F = (s1.sentencetype.belief.TRUTH.C * s2.sentencetype.belief.TRUTH.F) / 
                            (s1.sentencetype.belief.TRUTH.F + s2.sentencetype.belief.TRUTH.F - s1.sentencetype.belief.TRUTH.F * s2.sentencetype.belief.TRUTH.F);
                    else
                        S.sentencetype.belief.TRUTH.F = 0;
                    if (s1.sentencetype.belief.TRUTH.F + s2.sentencetype.belief.TRUTH.F - s1.sentencetype.belief.TRUTH.F * s2.sentencetype.belief.TRUTH.F != 0 && 
                        s1.sentencetype.belief.TRUTH.C * s2.sentencetype.belief.TRUTH.C != 0)
                        S.sentencetype.belief.TRUTH.C = (s1.sentencetype.belief.TRUTH.C * s2.sentencetype.belief.TRUTH.C * (s1.sentencetype.belief.TRUTH.F + 
                            s2.sentencetype.belief.TRUTH.F - s1.sentencetype.belief.TRUTH.F * s2.sentencetype.belief.TRUTH.F)) / 
                            (s1.sentencetype.belief.TRUTH.C * s2.sentencetype.belief.TRUTH.C * (s1.sentencetype.belief.TRUTH.F + 
                            s2.sentencetype.belief.TRUTH.F - s1.sentencetype.belief.TRUTH.F * s2.sentencetype.belief.TRUTH.F) + evidential_horizon);
                    else
                        S.sentencetype.belief.TRUTH.C = 0;
                    if (s1.source == s2.source)
                    {
                        S.relType = relationshipTypeS[2];
                        S.source = s2.T;
                        S.T = s1.T;
                    }
                    else
                    {
                        S.relType = relationshipTypeS[2];
                        S.source = s1.source;
                        S.T = s2.source;
                    }
                    S.sentencetype.belief.TRUTH.EXPECT = S.sentencetype.belief.TRUTH.TRUTHValue(S.sentencetype.belief.TRUTH.F, S.sentencetype.belief.TRUTH.C);
                }
            }
        }
        public override string ToString()
        {
            return "F: " + S.sentencetype.belief.TRUTH.F + ", C: " + S.sentencetype.belief.TRUTH.C;
        }
    }

    public class Exemplification
    {
        public Relationship S1 { get; set; }
        public Relationship S2 { get; set; }
        public Relationship S = new Relationship();
        public Exemplification(Relationship s1, Relationship s2, List<Thing> relationshipTypeS)
        {
            // Exemplification - Weak Syllogism
            if ((s1.source == s2.T || s1.T == s2.source) && s1.relType == s2.relType && s1.relType != relationshipTypeS[2])
            {
                S1 = s1;
                S2 = s2;
                //Two beliefs
                if ((s1.sentencetype?.belief != null && s2.sentencetype?.belief != null))
                {
                    S.sentencetype.belief = new SentenceType.Belief()
                    {
                        TRUTH = new TruthValue()
                        {
                            F = 1,
                            C = (s1.sentencetype.belief.TRUTH.F * s1.sentencetype.belief.TRUTH.C * s2.sentencetype.belief.TRUTH.F * s2.sentencetype.belief.TRUTH.C) / 
                            (s1.sentencetype.belief.TRUTH.F * s1.sentencetype.belief.TRUTH.C * s2.sentencetype.belief.TRUTH.F * s2.sentencetype.belief.TRUTH.C + evidential_horizon)
                        },
                        Tense = new Tense()
                    };

                    S.relType = relationshipTypeS[1];
                    if (s1.source == s2.T)
                    {
                        S.source = s1.T;
                        S.T = s2.source;
                        S.sentencetype.belief.TRUTH = Conversion(S.sentencetype.belief.TRUTH);
                    }
                    else
                    {
                        S.source = s2.source;
                        S.T = s1.source;
                    }
                }
            }
        }
        public override string ToString()
        {
            return "F: " + S.sentencetype.belief.TRUTH.F + ", C: " + S.sentencetype.belief.TRUTH.C;
        }
    }

    public class Induction
    {
        public bool conversionbool = false;
        public Relationship S1 { get; set; }
        public Relationship S2 { get; set; }
        public Relationship S = new Relationship();
        public Induction(Relationship s1, Relationship s2, List<Thing> relationshipTypeS)
        {
            // Induction - Weak Syllogism
            if ((s1.source == s2.source) && s1.relType == s2.relType && s1.source != s2.source)
            {
                S1 = s1;
                S2 = s2;
                //Two beliefs
                if ((s1.sentencetype?.belief != null && s2.sentencetype?.belief != null))
                {
                    S.sentencetype.belief = new SentenceType.Belief()
                    {
                        TRUTH = new TruthValue()
                        {
                            F = s1.sentencetype.belief.TRUTH.F,
                            C = (s1.sentencetype.belief.TRUTH.C * s2.sentencetype.belief.TRUTH.F * s2.sentencetype.belief.TRUTH.C) / 
                            (s1.sentencetype.belief.TRUTH.C * s2.sentencetype.belief.TRUTH.F * s2.sentencetype.belief.TRUTH.C + evidential_horizon)
                        },
                        Tense = new Tense()
                    };
                    S.relType = relationshipTypeS[1];
                    S.source = s2.T;
                    S.T = s1.T;
                    if (conversionbool)
                        S.sentencetype.belief.TRUTH = Conversion(S.sentencetype.belief.TRUTH);
                }
            }
        }
        public override string ToString()
        {
            return "F: " + S.sentencetype.belief.TRUTH.F + ", C: " + S.sentencetype.belief.TRUTH.C;
        }
    }

    public class Abduction
    {
        public bool conversionbool = false;
        public Relationship S1 { get; set; }
        public Relationship S2 { get; set; }
        public Relationship S = new Relationship();
        public Abduction(Relationship s1, Relationship s2, List<Thing> relationshipTypeS)
        {
            // Abduction - Weak Syllogism
            if ((s1.T == s2.T) && s1.relType == s2.relType && s1.source != s2.source)
            {
                S1 = s1;
                S2 = s2;
                //Two beliefs
                if ((s1.sentencetype?.belief != null && s2.sentencetype?.belief != null))
                {
                    S.sentencetype = new SentenceType();
                    S.sentencetype.belief = new SentenceType.Belief()
                    {
                        TRUTH = new TruthValue()
                        {
                            F = s2.sentencetype.belief.TRUTH.F,
                            C = (s1.sentencetype.belief.TRUTH.F * s1.sentencetype.belief.TRUTH.C * s2.sentencetype.belief.TRUTH.C) / 
                            (s1.sentencetype.belief.TRUTH.F * s1.sentencetype.belief.TRUTH.C * s2.sentencetype.belief.TRUTH.C + evidential_horizon)
                        },
                        Tense = new Tense()
                    };
                    
                    S.relType = relationshipTypeS[1];
                    S.source = s1.source;
                    S.T = s2.source;
                    if (conversionbool)
                        S.sentencetype.belief.TRUTH = Conversion(S.sentencetype.belief.TRUTH);
                }
            }
        }
        public override string ToString()
        {
            return "F: " + S.sentencetype.belief.TRUTH.F + ", C: " + S.sentencetype.belief.TRUTH.C;
        }
    }

    public class Resemblance
    {
        public Relationship S1 { get; set; }
        public Relationship S2 { get; set; }
        public Relationship S = new Relationship();
        public Resemblance(Relationship s1, Relationship s2, List<Thing> relationshipTypeS)
        {
            // Resemblance - Strong Syllogism
            if (s1.relType.Label == "<->" && s1.relType.Label == s2.relType.Label && (s2.T == s1.T || s2.T == s1.source))
            {
                S1 = s1;
                S2 = s2;
                //Two beliefs
                if ((s1.sentencetype?.belief != null && s2.sentencetype?.belief != null))
                {
                    S.sentencetype.belief = new SentenceType.Belief()
                    {
                        TRUTH = new TruthValue()
                        {
                            F = s1.sentencetype.belief.TRUTH.F * s2.sentencetype.belief.TRUTH.F,
                            C = s1.sentencetype.belief.TRUTH.C * s2.sentencetype.belief.TRUTH.C * (s1.sentencetype.belief.TRUTH.F + 
                            s2.sentencetype.belief.TRUTH.F - s1.sentencetype.belief.TRUTH.F * s2.sentencetype.belief.TRUTH.F)
                        },
                        Tense = new Tense()
                    };
                    S.relType = relationshipTypeS[2];
                    S.source = s1.T;
                    S.T = s2.T;
                }
            }
        }
        public override string ToString()
        {
            return "F: " + S.sentencetype.belief.TRUTH.F + ", C: " + S.sentencetype.belief.TRUTH.C;
        }
    }
    public class Analogy
    {
        public Relationship S1 { get; set; }
        public Relationship S2 { get; set; }
        public Relationship S = new Relationship();
        public Analogy(Relationship s1, Relationship s2, List<Thing> relationshipTypeS)
        {
            // Analogy - Strong Syllogism
            if ((s1.relType == relationshipTypeS[1] && s1.relType != s2.relType && (s1.source == s1.source || s1.T == s1.source)) ||
                (s2.relType == relationshipTypeS[1] && s2.relType != s1.relType && (s1.source == s2.T || s1.source == s2.source)))
            {
                S1 = s1;
                S2 = s2;
                //Two beliefs
                if ((s1.sentencetype?.belief != null && s2.sentencetype?.belief != null))
                {
                    S.sentencetype.belief = new SentenceType.Belief()
                    {
                        TRUTH = new TruthValue()
                        {
                            F = s1.sentencetype.belief.TRUTH.F * s2.sentencetype.belief.TRUTH.F,
                            C = s1.sentencetype.belief.TRUTH.C * s2.sentencetype.belief.TRUTH.C * s2.sentencetype.belief.TRUTH.F
                        },
                        Tense = new Tense()
                    };
                    S.relType = relationshipTypeS[1];
                    if (s1.relType == relationshipTypeS[2])
                    {
                        if (s1.source == s2.T || s1.source == s2.source)
                        {
                            if (s1.source == s2.source)
                            {
                                S.source = s2.T;
                                S.T = s1.T;
                            }
                            else
                            {
                                S.source = s2.source;
                                S.T = s1.T;
                            }
                        }
                        else
                        {
                            if (s1.T == s2.source)
                            {
                                S.source = s1.source;
                                S.T = s2.T;
                            }
                            else
                            {
                                S.source = s2.source;
                                S.T = s1.source;
                            }
                        }

                    }
                    else
                    {
                        if (s2.T == s1.source || s2.T == s1.T)
                        {
                            if (s2.T == s1.source)
                            {
                                S.source = s2.source;
                                S.T = s1.T;
                            }
                            else
                            {
                                S.source = s2.source;
                                S.T = s1.source;
                            }
                        }
                        else
                        {
                            if (s2.source == s1.source)
                            {
                                S.source = s1.T;
                                S.T = s2.T;
                            }
                            else
                            {
                                S.source = s1.source;
                                S.T = s2.T;
                            }
                        }
                        S.sentencetype.belief.TRUTH = Conversion(S.sentencetype.belief.TRUTH);
                    }
                }
            }
        }
        public override string ToString()
        {
            return "F: " + S.sentencetype.belief.TRUTH.F + ", C: " + S.sentencetype.belief.TRUTH.C;
        }
    }

    public class Revision
    {
        public Relationship S1 { get; set; }
        public Relationship S2 { get; set; }
        public Relationship S = new Relationship();

        public Revision(Relationship s1, Relationship s2, List<Thing> relationshipTypeS)
        {
            // Revision - Two statements that are the same
            if (s1.source == s2.source && s1.T == s2.T)
            {
                S1 = s1;
                S2 = s2;
                //Two beliefs
                if ((s1.sentencetype?.belief != null && s2.sentencetype?.belief != null))
                {
                    S.sentencetype = new SentenceType(".");
                    ModulePremise mp = new ModulePremise();
                    if (s1.sentencetype.belief.TRUTH.C != 0 && s2.sentencetype.belief.TRUTH.C != 0)
                    {
                        S.sentencetype.belief = new SentenceType.Belief()
                        {
                            TRUTH = new TruthValue()
                            {
                                F = (s1.sentencetype.belief.TRUTH.F * s1.sentencetype.belief.TRUTH.C * (1 - s2.sentencetype.belief.TRUTH.C) +
                                s2.sentencetype.belief.TRUTH.F * s2.sentencetype.belief.TRUTH.C * (1 - s1.sentencetype.belief.TRUTH.C)) /
                                (s1.sentencetype.belief.TRUTH.C * (1 - s2.sentencetype.belief.TRUTH.C) + s2.sentencetype.belief.TRUTH.C * (1 - s1.sentencetype.belief.TRUTH.C)),
                                C = (s1.sentencetype.belief.TRUTH.C * (1 - s2.sentencetype.belief.TRUTH.C) +
                                s2.sentencetype.belief.TRUTH.C * (1 - s1.sentencetype.belief.TRUTH.C)) / (s1.sentencetype.belief.TRUTH.C * (1 - s2.sentencetype.belief.TRUTH.C) +
                                s2.sentencetype.belief.TRUTH.C * (1 - s1.sentencetype.belief.TRUTH.C) + (1 - s1.sentencetype.belief.TRUTH.C) * (1 - s2.sentencetype.belief.TRUTH.C))
                            },
                            Tense = new Tense()
                        };
                    }
                    else
                    {
                        S.sentencetype.belief = new SentenceType.Belief()
                        {
                            TRUTH = new TruthValue()
                            {
                                F = 0,
                                C = 0
                            },
                            Tense = new Tense()
                        };
                    }
                    mp.GetNewTense(S1, S2, S.sentencetype.belief.Tense);
                    S.relType = s1.relType;
                    S.source = s1.source;
                    S.T = s1.T;
                    S.sentencetype.belief.TRUTH.EXPECT = S.sentencetype.belief.TRUTH.TRUTHValue(S.sentencetype.belief.TRUTH.F, S.sentencetype.belief.TRUTH.C);
                    
                    //S.sentencetype = new SentenceType(S.sentencetype.belief.TRUTH.TRUTHValue(S.sentencetype.belief.TRUTH.F, S.sentencetype.belief.TRUTH.C, s1.belief.T);
                    s2.UpdateRelationship(S);
                }
            }
        }
        public override string ToString()
        {
            return "F: " + S.sentencetype.belief.TRUTH.F + ", C: " + S.sentencetype.belief.TRUTH.C;
        }
    }
    public class Deduction
    {
        public Relationship S1 { get; set; }
        public Relationship S2 { get; set; }
        public Relationship S = new Relationship();
        public Deduction(Relationship s1, Relationship s2, List<Thing> relationshipTypeS)
        {
            // Deduction - Strong Syllogism
            //Conditional Syllogistic Rules
            if (s1.source != null && s1.T != null)
            {
                //Two beliefs
                if ((s1.sentencetype?.belief != null && s2.sentencetype?.belief != null))
                {
                    //if (M-->P w/ S-->M) or (P -->M w/ M-->S) 
                    if ((s1.source == s2.T || s1.T == s2.source) && s1.relType == s2.relType && s1.relType != relationshipTypeS[2])
                    {
                        S.relType = s1.relType;
                        if (s1.source == s2.T)
                        {
                            S.sentencetype.belief = new SentenceType.Belief()
                            {
                                TRUTH = new TruthValue()
                                {
                                    F = s1.sentencetype.belief.TRUTH.F * s2.sentencetype.belief.TRUTH.F,
                                    C = s1.sentencetype.belief.TRUTH.F * s1.sentencetype.belief.TRUTH.C * s2.sentencetype.belief.TRUTH.F * s2.sentencetype.belief.TRUTH.C
                                },
                                Tense = new Tense()
                            };
                            if (s2.source is Thing s2source && s1.T is Thing s1T)
                                S = s2source.AddRelationship(s1T, S.relType);
                        }
                        else
                        {
                            S.sentencetype.belief = new SentenceType.Belief()
                            {
                                TRUTH = new TruthValue()
                                {
                                    F = s1.sentencetype.belief.TRUTH.F * s2.sentencetype.belief.TRUTH.F,
                                    C = s1.sentencetype.belief.TRUTH.F * s1.sentencetype.belief.TRUTH.C * s2.sentencetype.belief.TRUTH.F * s2.sentencetype.belief.TRUTH.C
                                },
                                Tense = new Tense()
                            };

                            if (s1.source is Thing s1source && s2.T is Thing s2T)
                                S = s1source.AddRelationship(s2T, relationshipTypeS[1]);
                        }
                        /*                    if (s1.relationshipType != s2.relationshipType)
                                            {
                                                if (s1.source.HasAncestor(s1.T))
                                                    S.relationshipType = s2.relationshipType;
                                                else
                                                    S.relationshipType = s1.relationshipType;
                                            }
                                            else
                                            {
                                                if (s1.source.HasAncestor(s1.T))
                                                    S.relationshipType = s1.relationshipType;
                                                else
                                                    S.relationshipType = s2.relationshipType;
                                            }*/
                    }
                }
/*                else if (s1.Source != null && s1.TT != null)
                {
                    if ((s1.Source == s2.TT || s1.TT == s2.Source) && s1.relationshipType == s2.relationshipType && s1.relationshipType != relationshipTypeS[2])
                    {
                        S.sentencetype.belief = new SentenceType.Belief()
                        {
                            TRUTH = new TruthValue()
                            {
                                F = s1.sentencetype.belief.TRUTH.F * s2.sentencetype.belief.TRUTH.F,
                                C = s1.sentencetype.belief.TRUTH.F * s1.sentencetype.belief.TRUTH.C * s2.sentencetype.belief.TRUTH.F * s2.sentencetype.belief.TRUTH.C
                            },
                            Tense = new Tense()
                        };

                        S.relationshipType = relationshipTypeS[1];
                        if (s1.Source == s2.TT)
                        {
                            if (s2.Source is Relationship s2Source && s1.TT is Relationship s1TT)
                                S = s2Source.asThing.AddRelationship(s1TT.asThing, relationshipTypeS[1]);
                        }
                        else
                        {
                            if (s1.Source is Relationship s1Source && s2.TT is Relationship s2TT)
                                S = s1Source.asThing.AddRelationship(s2TT.asThing, relationshipTypeS[1]);
                        }
                    }
                }*/
            }
        }
        public override string ToString()
        {
            return "F: " + S.sentencetype.belief.TRUTH.F + ", C: " + S.sentencetype.belief.TRUTH.C;
        }
    }
}