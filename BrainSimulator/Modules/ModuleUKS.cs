//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml.Serialization;

namespace BrainSimulator.Modules;

/// <summary>
/// Contains a collection of Things linked by Relationships to implement Common Sense and general knowledge.
/// </summary>
public partial class ModuleUKS : ModuleBase
{
    //This is the actual Universal Knowledge Store
    protected List<Thing> UKSList = new() { Capacity = 1000000, };

    //This is a temporary copy of the UKS which used during the save and restore process to 
    //break circular links by storing index values instead of actual links Note the use of SThing instead of Thing
    public List<SThing> UKSTemp = new();

    //keeps the file name for xml storage
    public string fileName = "";


    /// <summary>
    /// Currently not used...for future background processing needs
    /// </summary>
    public override void Fire()
    {
        Init();  //be sure to leave this here to enable use of the na variable
    }

    private void CreateInitialStructure()
    {
        GetUKS();
        UKSList.Clear();
        AddThing("Thing", null);
        GetOrAddThing("Object", "Thing");
        GetOrAddThing("Action", "Thing");
        GetOrAddThing("unknownObject", "Object");
        GetOrAddThing("is-a", "RelationshipType");
        GetOrAddThing("inverseOf", "RelationshipType");
        GetOrAddThing("hasProperty", "RelationshipType");
        GetOrAddThing("is", "RelationshipType");

        //This hack is here because at startup some Things don't get put into the UKS List.
        foreach (Thing t in ThingLabels.AllThingsInLabelList())
            if (!UKSList.Contains(t))
                UKSList.Add(t);


        AddStatement("is-a", "inverseOf", "has-child");
        AddStatement("isExclusive", "is-a", "RelationshipType");
        AddStatement("isTransitive", "is-a", "RelationshipType");
        AddStatement("has", "is-a", "RelationshipType");
        AddStatement("ClauseType", "is-a", "RelationshipType");
        AddStatement("has", "hasProperty", "isTransitive");
        AddStatement("has-child", "hasProperty", "isTransitive");


        SetupNumbers();
    }

    private  void SetupNumbers()
    {
        GetOrAddThing("number", "Object");
        AddStatement("greaterThan", "is-a", "RelationshipType");
        AddStatement("greaterThan", "hasProperty", "isTransitive");
        AddStatement("lessThan", "inverseOf", "greaterThan");
        AddStatement("lessThan", "is-a", "RelationshipType");
        AddStatement("number", "hasProperty", "isExclusive");
        GetOrAddThing("digit", "number");
        GetOrAddThing("isSimilarTo", "RelationshipType");
        GetOrAddThing("isCommutative", "RelationshipType");
        AddStatement("isSimilarTo", "hasProperty", "isCommutative");
        AddStatement("hasDigit", "is-a", "has");


        //put in digits
        GetOrAddThing(".", "digit");
        GetOrAddThing("0", "digit");
        GetOrAddThing("2", "digit");
        GetOrAddThing("1", "digit");
        GetOrAddThing("3", "digit");
        GetOrAddThing("4", "digit");
        GetOrAddThing("5", "digit");
        GetOrAddThing("6", "digit");
        GetOrAddThing("7", "digit");
        GetOrAddThing("8", "digit");
        GetOrAddThing("9", "digit");
        GetOrAddThing("some", "number");
        GetOrAddThing("many", "number");
        GetOrAddThing("none", "number");


        //AddStatement("pi", "is-a", "number");
        //AddStatement("pi", "hasDigit*", "3");
        //AddStatement("pi", "hasDigit*", ".");
        //AddStatement("pi", "hasDigit*", "1");
        //AddStatement("pi", "hasDigit*", "4");
        //AddStatement("pi", "hasDigit*", "1");
        //AddStatement("pi", "hasDigit*", "5");
        //AddStatement("pi", "hasDigit*", "9");
    }



    /// <summary>
    /// Creates the initial structure of the UKS
    /// </summary>
    public override void Initialize()
    {
        MainWindow.SuspendEngine();
        UKSList.Clear();
        ThingLabels.ClearLabelList();

        CreateInitialStructure();

        UKSTemp.Clear();

        // Make sure all other loaded modules get notified of UKS Initialization
        UKSInitialized();
        MainWindow.ResumeEngine();
    }

    //these two functions transform the UKS into an structure which can be serialized/deserialized
    //by translating object references into array indices, all the problems of circular references go away
    public override void SetUpBeforeSave()
    {
        base.SetUpBeforeSave();
        if (fileName != null && fileName.Length > 0)
        {
            SaveUKStoXMLFile();
        }
        UKSTemp = new();
    }
    public override void SetUpAfterLoad()
    {
        GetUKS();
        base.SetUpAfterLoad();
        if (!string.IsNullOrEmpty(fileName))
        {
            fileName = Utils.RebaseFolderToCurrentDevEnvironment(fileName);
            LoadUKSfromXMLFile();
        }
        else
        {
            UKSList.Clear();
            CreateInitialStructure();

            UKSTemp.Clear();
        }
    }


    //this is needed for the dialog treeview
    public List<Thing> GetTheUKS()
    {
        return UKSList;
    }

    /// <summary>
    /// This is a primitive method needed only to create ROOT Things which have no parents
    /// </summary>
    /// <param name="label"></param>
    /// <param name="parent">May be null</param>
    /// <returns></returns>
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

    /// <summary>
    /// This is a primitive method to Delete a Thing...the Thing must not have any children
    /// </summary>
    /// <param name="t">The Thing to delete</param>
    public virtual void DeleteThing(Thing t)
    {
        if (t == null) return;
        if (t.Children.Count != 0)
            return; //can't delete something with children...must delete all children first.
        foreach (Relationship r in t.Relationships)
            t.RemoveRelationship(r);
        foreach (Relationship r in t.RelationshipsFrom)
            r.source.RemoveRelationship(r);
        ThingLabels.RemoveThingLabel(t.Label);
        lock (UKSList)
            UKSList.Remove(t);
    }

    //returns the thing with the given label
    /// <summary>
    /// Uses a hash table to return the Thing with the given label or null if it does not exist
    /// </summary>
    /// <param name="label"></param>
    /// <returns>The Thing or null</returns>
    public Thing Labeled(string label)
    {
        Thing retVal = ThingLabels.GetThing(label);
        return retVal;
    }

    private bool ThingInTree(Thing t1, Thing t2)
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
            if (r.relType == relType)
            {
                if (!results.Contains(r.target))
                {
                    results.Add(r.target);
                    results.AddRange(r.target.Descendents);
                    GetTransitiveTargetChain(r.target, r.relType, results);
                }
            }
        return results;
    }
    List<Relationship> RelationshipTree(Thing t, Thing relType)
    {
        List<Relationship> results = new();
        results.AddRange(t.Relationships.FindAll(x => x.relType == relType));
        foreach (Thing t1 in t.Ancestors)
            results.AddRange(t1.Relationships.FindAll(x => x.relType == relType));
        foreach (Thing t1 in t.Descendents)
            results.AddRange(t1.Relationships.FindAll(x => x.relType == relType));
        return results;
    }
    List<Thing> GetTransitiveSourceChain(Thing t, Thing relType, List<Thing> results = null)
    {
        if (results == null) results = new();
        List<Relationship> targets = RelationshipsByTree(t, relType);
        foreach (Relationship r in targets)
            if (r.relType == relType)
            {
                if (!results.Contains(r.source))
                {
                    results.Add(r.source);
                    //results.AddRange(r.source.Ancestors);
                    GetTransitiveSourceChain(r.source, r.relType, results);
                }
            }
        return results;
    }
    List<Relationship> RelationshipsByTree(Thing t, Thing relType)
    {
        List<Relationship> results = new();
        if (t == null) return results;
        results.AddRange(t.RelationshipsFrom.FindAll(x => x.relType == relType));
        foreach (Thing t1 in t.Ancestors)
            results.AddRange(t1.RelationshipsFrom.FindAll(x => x.relType == relType));
        foreach (Thing t1 in t.Descendents)
            results.AddRange(t1.RelationshipsFrom.FindAll(x => x.relType == relType));
        return results;
    }



    private bool RelationshipsAreExclusive(Relationship r1, Relationship r2)
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

            IList<Thing> r1RelProps = GetAttributes(r1.relType);
            IList<Thing> r2RelProps = GetAttributes(r2.relType);
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
                            if (HasProperty(t3, "isexclusive") || HasProperty(t3, "allowMultiple"))
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
                    if (HasProperty(t3, "isexclusive") || HasProperty(t3, "allowMultiple"))
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
                            if (HasProperty(t3, "isexclusive") || HasProperty(t3, "allowMultiple"))
                                return true;
                        }
                    }
            }
            //if source and target are the same and one contains a number, assume that the other contains "1"
            // fido has a leg -> fido has 1 leg
            bool hasNumber1 = (r1RelProps.FindFirst(x => x.HasAncestorLabeled("number")) != null);
            bool hasNumber2 = (r2RelProps.FindFirst(x => x.HasAncestorLabeled("number")) != null);
            if (r1.target == r2.target &&
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
                if (HasProperty(t3, "isexclusive"))
                    return true;
                if (HasProperty(t3, "allowMultiple") && r1.source != r2.source)
                    return true;
            }

        }
        return false;
    }

    private  IList<Thing> GetAttributes(Thing t)
    {
        List<Thing> retVal = new();
        if (t == null) return retVal;
        foreach (Relationship r in t.Relationships)
        {
            if (r.relType != null && r.relType.Label == "is")
                retVal.Add(r.target);
        }
        return retVal;
    }

    private bool HasAttribute(Thing t, string name)
    {
        if (t == null) return false;
        foreach (Relationship r in t.Relationships)
        {
            if (r.relType != null && r.relType.Label == "is"  && r.target.Label == name)
                return true;
        }
        return false;
    }

    bool HasDirectProperty(Thing t, string propertyName)
    {
        if (t == null) return false;
        if (HasProperty(t, propertyName) )return true;
        foreach (Thing p in t.Parents)
            return HasProperty(p, propertyName);
        return false;
    }
    bool HasProperty(Thing t, string propertyName)
    {
        if (t == null) return false;
        var v = t.Relationships;
        if (v.FindFirst(x => x.target?.Label.ToLower() == propertyName.ToLower() && x.relType.Label == "hasProperty") != null) return true;
        return false;
    }

    bool RelationshipsAreEqual(Relationship r1, Relationship r2, bool ignoreSource = true)
    {
        if (
            (r1.source == r2.source || ignoreSource) &&
            r1.target == r2.target &&
            r1.relType == r2.relType
          ) return true;
        return false;
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
        if (label == "") return null;
        Thing t = Labeled(label);

        if (t == null)
        {
            if (Labeled(defaultParent) == null)
            {
                GetOrAddThing(defaultParent, Labeled("Object"), source);
            }
            t = GetOrAddThing(label, defaultParent, source);
        }
        return t;
    }

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

    private  List<Thing> ThingListFromObject(object o, string parentLabel = "unknownObject")
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



    /// <summary>
    /// Recursively removes all the descendants of a Thing. If these descendants have no other parents, they will be deleted as well
    /// </summary>
    /// <param name="t">The Thing to remove the children from</param>
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
    /// <summary>
    /// Creates a new Thing in the UKS OR returns an existing Thing, based on the label
    /// </summary>
    /// <param name="label">The new label OR if it ends in an asterisk, the astrisk will be replaced by digits to create a new Thing with a unique label.</param>
    /// <param name="parent"></param>
    /// <param name="source"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
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
                while (source.Relationships.FindFirst(x => x.relType.Label == baseLabel + digit) != null) digit++;
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
    /// <summary>
    /// Saves the UKS content to an XML file
    /// </summary>
    /// <param name="fileNameIn">Leave null or empty to use file name from previous operation  </param>
    public void SaveUKStoXMLFile(string fileNameIn = "")
    {
        if (!string.IsNullOrEmpty(fileNameIn)) fileName = fileNameIn;
        string fullPath = GetFullPathFromKnowledgeFileName(fileName);

        if (!XmlFile.CanWriteTo(fileName, out string message))
        {
            MessageBox.Show("Could not save file because: " + message);
            return;
        }
        FormatContentForSaving();
        List<Type> extraTypes = GetTypesInUKS();
        Stream file = File.Create(fullPath);
        file.Position = 0;
        try
        {
            XmlSerializer writer = new XmlSerializer(UKSTemp.GetType(), extraTypes.ToArray());
            writer.Serialize(file, UKSTemp);
        }
        catch (Exception e)
        {
            if (e.InnerException != null)
                MessageBox.Show("Xml file write failed because: " + e.InnerException.Message);
            else
                MessageBox.Show("Xml file write failed because: " + e.Message);
            file.Close();
            return;
        }
        file.Close();
    }
    /// <summary>
    /// Loads UKS content from a prvsiously-saved XML file
    /// </summary>
    /// <param name="fileNameIn">Leave null or empty to use file name from previous operation  </param>
    /// <param name="merge">If true, existing UKS content is not deleted and new content is merged by Thing label</param>
    public void LoadUKSfromXMLFile(string fileNameIn = "", bool merge = false)
    {
        if (!string.IsNullOrEmpty(fileNameIn)) fileName = fileNameIn;

        Stream file;
        string fullPath = fileName;
        try
        {
            file = File.Open(fullPath, FileMode.Open, FileAccess.Read);
        }
        catch (Exception e)
        {
            MessageBox.Show("Could not open file because: " + e.Message);
            return;
        }

        List<Type> extraTypes = GetTypesInUKS();
        XmlSerializer reader1 = new XmlSerializer(UKSTemp.GetType(), extraTypes.ToArray());
        try
        {
            UKSTemp = (List<SThing>)reader1.Deserialize(file);
        }
        catch (Exception e)
        {
            file.Close();
            MessageBox.Show("Network file load failed, a blank network will be opened. \r\n\r\n" + e.InnerException, "File Load Error",
                MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
            return;
        }
        file.Close();
        if (merge)
            DeFormatAndMergeContentAfterLoading();
        else
            DeFormatContentAfterLoading();
        base.UKSReloaded();
    }


}

