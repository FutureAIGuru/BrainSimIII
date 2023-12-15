//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System;
using System.Collections.Generic;
using System.Linq;

namespace BrainSimulator.Modules;

public partial class ModuleUKS : ModuleBase
{
    //This is the actual Universal Knowledge Store
    protected List<Thing> UKSList = new() { Capacity = 1000000, };

    //This is a temporary copy of the UKS which used during the save and restore process to 
    //break circular links by storing index values instead of actual links Note the use of SThing instead of Thing
    public List<SThing> UKSTemp = new();

    //keeps the file name for xml storage
    public string fileName = "";


    //set size parameters as needed in the constructor
    //set max to be -1 if unlimited
    public ModuleUKS()
    {
        allowMultipleDialogs = true;
    }

    public override void Fire()
    {
        Init();  //be sure to leave this here to enable use of the na variable
    }

    //this is needed for the dialog treeview
    public List<Thing> GetTheUKS()
    {
        return UKSList;
    }

    public virtual Thing AddThing(string label, Thing parent)
    {
        Thing newThing = new();
        newThing.Label = label;
        if (parent is not null)
        {
            newThing.AddParent(parent);
        }
        lock (UKSList)
        {
            UKSList.Add(newThing);
        }
        return newThing;
    }

    public virtual void DeleteThing(Thing t)
    {
        if (t == null) return;
        if (t.Children.Count != 0)
            return; //can't delete something with children...must delete all children first.
        foreach (Relationship r in t.Relationships)
        {
            t.RemoveRelationship(r);
        }
        foreach (Relationship r in t.RelationshipsFrom)
        {
            r.source.RemoveRelationship(r);
        }
        lock (UKSList)
        {
            UKSList.Remove(t);
        }
    }

    //returns the thing with the given label
    public Thing Labeled(string label)
    {
        Thing retVal = ThingLabels.GetThing(label);
        return retVal;
    }

    public bool ThingInTree(Thing t1, Thing t2)
    {
        if (t2 == null) return false;
        if (t1 == null) return false;
        if (t1 == t2) return true;
        if (t1.AncestorList().Contains(t2)) return true;
        if (t2.AncestorList().Contains(t1)) return true;
        return false;
    }
    bool ThingInTree(Thing t1, string label)
    {
        if (label == "") return false;
        if (t1 == null) return false;
        if (t1.Label == label) return true;
        if (t1.AncestorList().FindFirst(x => x.Label == label) != null) return true;
        if (t1.DescendentsList().FindFirst(x => x.Label == label) != null) return true;
        if (t1.HasRelationshipWithAncestorLabeled(label) != null) return true;
        return false;

    }
    List<Thing> GetTransitiveTargetChain(Thing t, Thing relType, List<Thing> results = null)
    {
        if (results == null) results = new();
        List<Relationship> targets = RelationshipTree(t, relType);
        foreach (Relationship r in targets)
            if (r.reltype == relType)
            {
                if (!results.Contains(r.target))
                {
                    results.Add(r.target);
                    results.AddRange(r.target.Descendents);
                    GetTransitiveTargetChain(r.target, r.reltype, results);
                }
            }
        return results;
    }
    List<Relationship> RelationshipTree(Thing t, Thing relType)
    {
        List<Relationship> results = new();
        results.AddRange(t.Relationships.FindAll(x => x.reltype == relType));
        foreach (Thing t1 in t.Ancestors)
            results.AddRange(t1.Relationships.FindAll(x => x.reltype == relType));
        foreach (Thing t1 in t.Descendents)
            results.AddRange(t1.Relationships.FindAll(x => x.reltype == relType));
        return results;
    }
    List<Thing> GetTransitiveSourceChain(Thing t, Thing relType, List<Thing> results = null)
    {
        if (results == null) results = new();
        List<Relationship> targets = RelationshipsByTree(t, relType);
        foreach (Relationship r in targets)
            if (r.reltype == relType)
            {
                if (!results.Contains(r.source))
                {
                    results.Add(r.source);
                    //results.AddRange(r.source.Ancestors);
                    GetTransitiveSourceChain(r.source, r.reltype, results);
                }
            }
        return results;
    }
    List<Relationship> RelationshipsByTree(Thing t, Thing relType)
    {
        List<Relationship> results = new();
        if (t == null) return results;
        results.AddRange(t.RelationshipsFrom.FindAll(x => x.reltype == relType));
        foreach (Thing t1 in t.Ancestors)
            results.AddRange(t1.RelationshipsFrom.FindAll(x => x.reltype == relType));
        foreach (Thing t1 in t.Descendents)
            results.AddRange(t1.RelationshipsFrom.FindAll(x => x.reltype == relType));
        return results;
    }



    public bool RelationshipsAreExclusive(Relationship r1, Relationship r2)
    {
        //are two relationships mutually exclusive?
        //yes if they differ by a single component property
        //   which is exclusive on a property
        //      which source and target are the ancestor of one another

        //TODO:  expand this to handle
        //  is lessthan is greaterthan
        //  several other cases

        if (r1.target != r2.target && (r1.target == null || r2.target == null)) return false;

        //return false;
        if (r1.source == r2.source ||
            r1.source.AncestorList().Contains(r2.source) ||
            r2.source.AncestorList().Contains(r1.source) ||
            FindCommonParents(r1.source, r1.source).Count() > 0)
        {

            IList<Thing> r1RelProps = GetAttributes(r1.reltype);
            IList<Thing> r2RelProps = GetAttributes(r2.reltype);
            //handle case with properties of the target
            if (r1.target != null && r1.target == r2.target &&
                (r1.target.AncestorList().Contains(r2.target) ||
                r2.target.AncestorList().Contains(r1.target) ||
                FindCommonParents(r1.target, r1.target).Count() > 0))
            {
                IList<Thing> r1TargetProps = GetAttributes(r1.target);
                IList<Thing> r2TargetProps = GetAttributes(r2.target);
                foreach (Thing t1 in r1TargetProps)
                    foreach (Thing t2 in r2TargetProps)
                    {
                        List<Thing> commonParents = FindCommonParents(t1, t2);
                        foreach (Thing t3 in commonParents)
                        {
                            if (HasDirectProperty(t3, "isexclusive") || HasDirectProperty(t3, "allowMultiple"))
                                return true;
                        }
                    }
            }
            //handle case with conflicting targets
            if (r1.target != null && r2.target != null)
            {
                List<Thing> commonParents = FindCommonParents(r1.target, r2.target);
                foreach (Thing t3 in commonParents)
                {
                    if (HasDirectProperty(t3, "isexclusive") || HasDirectProperty(t3, "allowMultiple"))
                        return true;
                }
            }
            if (r1.target == r2.target)
            {
                foreach (Thing t1 in r1RelProps)
                    foreach (Thing t2 in r2RelProps)
                    {
                        if (t1 == t2) continue;
                        List<Thing> commonParents = FindCommonParents(t1, t2);
                        foreach (Thing t3 in commonParents)
                        {
                            if (HasDirectProperty(t3, "isexclusive") || HasDirectProperty(t3, "allowMultiple"))
                                return true;
                        }
                    }
            }
            //if source and target are the same and one contains a number, assume that the other contains "1"
            // fido has a leg -> fido has 1 leg
            bool hasNumber1 = (r1RelProps.FindFirst(x => x.HasAncestorLabeled("number")) != null);
            bool hasNumber2 = (r2RelProps.FindFirst(x => x.HasAncestorLabeled("number")) != null);
            if (r1.source == r2.source && r1.target == r2.target &&
                (hasNumber1 || hasNumber2))
                return true;

            //if one of the reltypes contains negation and not the other
            Thing r1Not = r1RelProps.FindFirst(x => x.Label == "not" || x.Label == "no");
            Thing r2Not = r2RelProps.FindFirst(x => x.Label == "not" || x.Label == "no");
            if ((r1.source.Ancestors.Contains(r2.source) || 
                r2.source.Ancestors.Contains(r1.source)) && 
                r1.target == r2.target &&
                (r1Not == null && r2Not != null || r1Not != null && r2Not == null))
                return true;
        }
        else
        {
            List<Thing> commonParents = FindCommonParents(r1.target, r2.target);
            foreach (Thing t3 in commonParents)
            {
                if (HasDirectProperty(t3, "isexclusive"))
                    return true;
                if (HasDirectProperty(t3, "allowMultiple") && r1.source != r2.source)
                    return true;
            }

        }
        return false;
    }

    public IList<Thing> GetAttributes(Thing t)
    {
        List<Thing> retVal = new();
        if (t == null) return retVal;
        foreach (Relationship r in t.Relationships)
        {
            if (r.reltype != null && r.reltype.Label == "is")
                retVal.Add(r.target);
        }
        return retVal;
    }
    bool HasProperty(Thing t, string propertyName)
    {
        if (t == null) return false;
        //var v = RelationshipWithInheritance(t);
        //if (v.FindFirst(x => x.T?.Label.ToLower() == propertyName.ToLower() && x.reltype.Label == "hasProperty") != null) return true;
        return false;
    }
    bool HasDirectProperty(Thing t, string propertyName)
    {
        if (t == null) return false;
        var v = t.Relationships;
        if (v.FindFirst(x => x.T?.Label.ToLower() == propertyName.ToLower() && x.reltype.Label == "hasProperty") != null) return true;
        return false;
    }

    bool RelationshipsAreEqual(Relationship r1, Relationship r2, bool ignoreSource = true)
    {
        if (
            (r1.source == r2.source || ignoreSource) &&
            r1.target == r2.target &&
            r1.relType == r2.relType
          //ListsAreEqual(r1.typeProperties, r2.typeProperties) &&
          //ListsAreEqual(r1.targetProperties, r2.targetProperties)
          ) return true;
        return false;
    }


    //every Thing in the first list MUST occur in the 2nd
    bool ModifiersMatch(List<Thing> l1, Thing t)
    {
        foreach (Thing t1 in l1)
        {
            if (t.Relationships.FindFirst(x => x.reltype != null && x.reltype.Label == "is" && x.T == t1) == null)
                return false;
        }
        return true;
    }


    private List<Thing> ThingListFromStringList(List<string> modifiers, string defaultParent)
    {
        if (modifiers == null) return null;
        List<Thing> retVal = new();
        foreach (string s in modifiers)
        {
            Thing t = ThingFromString(s, defaultParent);
            if (t != null) retVal.Add(t);
        }
        return retVal;
    }

    private Thing ThingFromString(string label, string defaultParent, Thing source = null)
    {
        if (string.IsNullOrEmpty(label)) return null;
        Thing t = Labeled(label);

        if (t == null)
        {
            if (Labeled(defaultParent) == null)
            {
                GetOrAddThing(defaultParent, Labeled("Object"), source);
            }
            t = GetOrAddThing(label, defaultParent,source);
        }
        return t;
    }

    //temporarily public for testing
    private Thing ThingFromObject(object o, string parentLabel = "", Thing source = null)
    {
        if (parentLabel == "")
            parentLabel = "unknownObject";
        if (o is string s3)
            return ThingFromString(s3.Trim(), parentLabel, source);
        else if (o is Thing t3)
            return t3;
        else if (o is null)
            return null;
        else
            return null;
    }
    private List<Thing> ThingListFromString(string s, string parentLabel = "unknownObject")
    {
        List<Thing> retVal = new List<Thing>();
        string[] multiString = s.Split("|");
        foreach (string s1 in multiString)
        {
            Thing t = ThingFromString(s1.Trim(), parentLabel);
            if (t != null)
                retVal.Add(t);
        }
        return retVal;
    }
    //temporarily public for testing
    public List<Thing> ThingListFromObject(object o, string parentLabel = "unknownObject")
    {
        if (o is List<string> sl)
            return ThingListFromStringList(sl, parentLabel);
        else if (o is string s)
            return ThingListFromString(s, parentLabel);
        else if (o is Thing t)
            return new() { t };
        else if (o is List<Thing> tl)
            return tl;
        else if (o is null)
            return new();
        else
            throw new ArgumentException("invalid arg type in AddStatement: " + o.GetType());
    }




    //TODO ?move to Thing  t.DeleteAllChildren()
    public void DeleteAllChildren(Thing t)
    {
        if (t is not null)
        {
            while (t.Children.Count > 0)
            {
                IList<Thing> children = t.Children;
                if (t.Children[0].Parents.Count == 1)
                {
                    DeleteAllChildren(children[0]);
                    DeleteThing(children[0]);
                }
                else
                {//this thing has multiple parents.
                    t.RemoveChild(children[0]);
                }
            }
        }

    }

    // If a thing exists, return it.  If not, create it.
    // If it is currently an unknown, defining the parent can make it known
    public Thing GetOrAddThing(string label, object parent = null, Thing source = null)
    {
        Thing thingToReturn = null;

        if (string.IsNullOrEmpty(label)) return thingToReturn;

        thingToReturn = ThingLabels.GetThing(label);
        if (thingToReturn != null) return thingToReturn;

        Thing correctParent = null;
        if (parent is string s)
            correctParent = ThingLabels.GetThing(s);
        if (parent is Thing t)
            correctParent = t;
        if (correctParent == null)
            correctParent = ThingLabels.GetThing("unknownObject");

        if (correctParent is null) throw new ArgumentException("GetOrAddThing: could not find parent");

        if (label.EndsWith("*"))
        {
            string baseLabel = label.Substring(0, label.Length - 1);
            Thing newParent = ThingLabels.GetThing(baseLabel);
            //instead of creating a new label, see if the next label for this item already exists and can be reused
            if (source != null)
            {
                int digit = 0;
                while (source.Relationships.FindFirst(x => x.reltype.Label == baseLabel + digit) != null) digit++;
                Thing labeled = ThingLabels.GetThing(baseLabel + digit);
                if (labeled != null)
                    return labeled;
            }
            if (newParent == null)
                newParent = AddThing(baseLabel, correctParent);
            correctParent = newParent;
        }

        thingToReturn = AddThing(label, correctParent);
        return thingToReturn;
    }

    
    public void SetupNumbers()
    {
        AddStatement("isexclusive", "is-a", "RelationshipType");
        AddStatement("with", "is-a", "RelationshipType");
        AddStatement("and", "is-a", "Relationship");
        AddStatement("isSimilarTo", "is-a", "RelationshipType");
        AddStatement("greaterThan", "is-a", "RelationshipType");


        //put in numbers 1-4
        GetOrAddThing("zero", "number").V = (int)0;
        GetOrAddThing("one", "number").V = (int)1;
        GetOrAddThing("two", "number").V = (int)2;
        GetOrAddThing("three", "number").V = (int)3;
        GetOrAddThing("four", "number").V = (int)4;
        GetOrAddThing("ten", "number").V = (int)10;
        Thing some = GetOrAddThing("some", "number");
        Thing many = GetOrAddThing("many", "number");
        Thing none = GetOrAddThing("none", "number");
        AddStatement("number","is-a", "Object");
        none.V = (int)0;

        AddStatement("10", "isSimilarTo", "ten");
        AddStatement("10", "is-a", "number");
        AddStatement("4", "isSimilarTo", "four");
        AddStatement("4", "is-a", "number");
        AddStatement("3", "isSimilarTo", "three");
        AddStatement("3", "is-a", "number");
        AddStatement("2", "isSimilarTo", "two");
        AddStatement("2", "is-a", "number");
        AddStatement("1", "isSimilarTo", "one");
        AddStatement("1", "is-a", "number");
        AddStatement("0", "isSimilarTo", "zero");
        AddStatement("0", "is-a", "number");

        AddStatement("one", "greaterThan", "none");
        AddStatement("two", "greaterThan", "one");
        AddStatement("three", "greaterThan", "two");
        AddStatement("four", "greaterThan", "three");
        AddStatement("many", "greaterThan", "four");
        AddStatement("many", "greaterThan", "some");
        AddStatement("some", "greaterThan", "none");
        AddStatement("number", "hasProperty", "isexclusive");
    }
}