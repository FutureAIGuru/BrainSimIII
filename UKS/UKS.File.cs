using System.Diagnostics;
using System.Reflection.Emit;
using System.Xml.Serialization;

namespace UKS;

public partial class UKS
{
    static string fileName = "";

    public string FileName { get => fileName; }

    public void CreateInitialStructure()
    {
        //this hack is needed to preserve the info relating to module layout
        for (int i = 0; i < AllThings.Count; i++)
        {
            Cogneme t = AllThings[i];
            if (t.HasAncestorLabeled("BrainSim"))
                continue;
            if (t.Label == "is-a") continue;
            //if (t.Label == "Cogneme") continue;
            //if (t.Label == "RelationshipType") continue;
            if (t.Label == "hasAttribute") continue;
            if (t is not null)
            {
                DeleteThing(t);
                i--;
            }
        }

        CognemeLabels.ClearLabelList();
        foreach (Cogneme t in AllThings)
            CognemeLabels.AddThingLabel(t.Label, t);

        if (Labeled("Cogneme") is null)
            AddThing("Cogneme", null);
        Cogneme isA = Labeled("is-a");
        if (isA is null)
            isA = AddThing("is-a", null);
        Cogneme hasChild = Labeled("has-child");
        if (hasChild is null)
            hasChild = AddThing("has-child", null);
        Cogneme relType = AddThing("RelationshipType", "Cogneme");
        isA.AddParent(relType);
        hasChild.AddParent(relType);

        GetOrAddThing("Object", "Cogneme");
        GetOrAddThing("Action", "Cogneme");
        GetOrAddThing("Relationship", "Cogneme");
        GetOrAddThing("RelationshipType", "Cogneme");
        GetOrAddThing("Cogneme", "Cogneme");
        GetOrAddThing("Unknown", "Cogneme");
        GetOrAddThing("is-a", "RelationshipType");
        GetOrAddThing("inverseOf", "RelationshipType");
        GetOrAddThing("hasProperty", "RelationshipType");
        GetOrAddThing("is", "RelationshipType");

        AddStatement("has-child", "inverseOf", "is-a");
        AddStatement("hasAttribute", "is-a", "RelationshipType");
        AddStatement("can", "is-a", "RelationshipType");
        AddStatement("mostRecent", "is-a", "RelationshipType");
        AddStatement("contains", "is-a", "RelationshipType");
        AddStatement("is-part-of", "is-a", "RelationshipType");
        AddStatement("contains", "inverseOf", "is-part-of");
        AddStatement("has", "is-a", "RelationshipType");
        AddStatement("not", "is-a", "RelationshipType");

        //properties are intenal capabilities of nodes
        AddStatement("Property", "is-a", "RelationshipType");
        AddStatement("isExclusive", "is-a", "Property");
        AddStatement("isTransitive", "is-a", "Property");
        AddStatement("isInstance", "is-a", "Property");
        AddStatement("isTransitive", "is-a", "Property");
        AddStatement("isCommutative", "is-a", "Property");
        AddStatement("allowMultiple", "is-a", "Property");
        AddStatement("inheritable", "is-a", "Property");
        AddStatement("isCondition", "is-a", "Property");
        AddStatement("isResult", "is-a", "Property");


        ////colors
        //AddStatement("color", "is-a", "object");
        //AddStatement("color", "hasProperty", "isExclusive");
        //AddStatement("red", "is-a", "color");
        //AddStatement("orange", "is-a", "color");
        //AddStatement("yellow", "is-a", "color");
        //AddStatement("green", "is-a", "color");
        //AddStatement("blue", "is-a", "color");
        //AddStatement("purple", "is-a", "color");
        //AddStatement("brown", "is-a", "color");
        //AddStatement("pink", "is-a", "color");
        //AddStatement("black", "is-a", "color");
        //AddStatement("white", "is-a", "color");
        //AddStatement("gray", "is-a", "color");

        //underlying properties
        AddStatement("is-a", "hasProperty", "isTransitive");
        AddStatement("is-a", "hasProperty", "inheritable");
        AddStatement("has", "hasProperty", "isTransitive");
        AddStatement("has", "hasProperty", "inheritable");

        //Clauses
        AddStatement("ClauseType", "is-a", "RelationshipType");
        AddStatement("IF", "is-a", "ClauseType");
        AddStatement("BECAUSE", "is-a", "ClauseType");
        AddStatement("AFTER", "is-a", "ClauseType");
        AddStatement("NXT", "is-a", "ClauseType");
        AddStatement("VLU", "is-a", "ClauseType");
        AddStatement("AND", "is-a", "ClauseType");
        AddStatement("OR", "is-a", "ClauseType");
        AddStatement("NOT", "is-a", "ClauseType");
        AddStatement("FRST", "is-a", "ClauseType");




        AddBrainSimConfigSectionIfNeeded();
        SetupNumbers();
    }

    public void SetupNumbers()
    {
        return;
        GetOrAddThing("number", "Object");
        AddStatement("Comparison", "is-a", "RelationshipType");
        AddStatement("greaterThan", "is-a", "Comparison");
        AddStatement("greaterThan", "hasProperty", "isTransitive");
        AddStatement("lessThan", "inverseOf", "greaterThan");
        AddStatement("lessThan", "is-a", "Comparison");
        AddStatement("number", "hasProperty", "isExclusive");
        GetOrAddThing("digit", "number");
        GetOrAddThing("isSimilarTo", "Comparison");
        AddStatement("isSimilarTo", "hasProperty", "isCommutative");
        AddStatement("hasDigit", "is-a", "has");


        //put in digits
        GetOrAddThing("-", "digit");
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
        for (int i = 9; i > 0; i--)
            AddStatement(i.ToString(), "greaterThan", (i - 1).ToString());



        //demo to add PI to the structure
        AddStatement("pi", "is-a", "number");
        AddStatement("pi", "hasDigit*", "3");
        AddStatement("pi", "hasDigit*", ".");
        AddStatement("pi", "hasDigit*", "1");
        AddStatement("pi", "hasDigit*", "4");
        AddStatement("pi", "hasDigit*", "1");
        AddStatement("pi", "hasDigit*", "5");
        AddStatement("pi", "hasDigit*", "9");
    }

    void AddBrainSimConfigSectionIfNeeded()
    {
        if (Labeled("BrainSim") is not null) return;
        AddThing("BrainSim", null);
        GetOrAddThing("AvailableModule", "BrainSim");
        GetOrAddThing("ActiveModule", "BrainSim");
    }


    /// <summary>
    /// /////////////////////////////////////////////////////////// XML file load/save
    /// </summary>
    /// 
    //this is a modification of Thing which is used to store and retrieve the KB in XML
    //it eliminates circular references by replacing Thing references with int indexed into an array and makes things much more compact
    public class sCogneme
    {
        public string label = ""; //this is just for convenience in debugging and should not be used
        public List<sCogneme> relationships = new();
        public int source = -1;
        public int target = -1;
        public int relationshipType = -1;
        public float weight = 0;
        public object V;
    }


    private void FormatContentForSaving()
    {
        UKSTemp.Clear();

        // TODO: Wipe transient data ...
        foreach (Cogneme t in AllThings)
        {
            sCogneme st = new()
            {
                label = t.Label,
                source = AllThings.FindIndex(x => x == t.Source),
                relationshipType = AllThings.FindIndex(x => x == t.RelType),
                target = AllThings.FindIndex(x => x == t.Target),
                V = t.V,
            };
            foreach (Cogneme l in t.RecursiveRelationships)
            {
                sCogneme sR = ConvertRelationship(l, new List<Cogneme>());
                st.relationships.Add(sR);
            }
            UKSTemp.Add(st);
        }
    }

    private sCogneme ConvertRelationship(Cogneme l, List<Cogneme> stack)
    {
        if (l.RelType.Label == "play")
        { }

        if (stack.Contains(l)) return null;
        stack.Add(l);

        sCogneme sR = new sCogneme()
        {
            label = l.Label,
            source = AllThings.FindIndex(x => x == l.Source),
            target = AllThings.FindIndex(x => x == l.Target),
            relationshipType = AllThings.FindIndex(x => x == l.RelType),
            weight = l.Weight,
        };

        foreach (Cogneme r1 in l.RecursiveRelationships)
        {
            sCogneme sr1 = ConvertRelationship(r1, stack);
            sR.relationships.Add(sr1);
        }
        stack.RemoveAt(stack.Count - 1);
        return sR;
    }

    private void DeFormatAndMergeContentAfterLoading()
    {
        //get all the names and weights
        foreach (sCogneme st in UKSTemp)
        {
            Cogneme t = new()
            {
                Label = st.label,
                Weight = st.weight,
                V = st.V,
            };
            AllThings.Add(t);
        }
        foreach (sCogneme st in UKSTemp)
        {
            Cogneme t = Labeled(st.label);
            if (st.source != -1) t.Source = AllThings[st.source];
            if (st.relationshipType != -1) t.RelType = AllThings[st.relationshipType];
            if (st.target != -1) t.Target = AllThings[st.target];
        }
        foreach (sCogneme v in UKSTemp)
        {
            AddRelationshipsAsStatements(v);
        }
    }

    private void AddRelationshipsAsStatements(sCogneme v)
    {
        foreach (sCogneme p in v.relationships)
        {
            if (UKSTemp[p.relationshipType].label == "play")
            { }
            Cogneme r = AddStatement(UKSTemp[p.source].label, UKSTemp[p.relationshipType].label, UKSTemp[p.target].label);
            r.Label = p.label;
            AddRelationshipsAsStatements(p);
        }

    }

    private void DeFormatContentAfterLoading()
    {
        AllThings.Clear();
        CognemeLabels.ClearLabelList();
        //get all the things
        foreach (sCogneme st in UKSTemp)
        {
            Cogneme t = new()
            {
                Label = st.label,
                V = st.V,
                //useCount = st.useCount
            };
            AllThings.Add(t);
        }
        //handle relationships
        for (int i = 0; i < UKSTemp.Count; i++)
        {
            sCogneme sT = UKSTemp[i];
            UnconvertRelationships(sT);
        }
        //rebuild all the reverse linkages
        foreach (Cogneme t in AllThings)
        {
            foreach (Cogneme r in t.Relationships)
            {
                Cogneme t1 = r.Target;
                if (t1 is not null)
                    if (!t1.RelationshipsFromWriteable.Contains(r))
                        t1.RelationshipsFromWriteable.Add(r);
                if (r.RelType is not null)
                    if (!r.RelType.RelationshipsAsTypeWriteable.Contains(r))
                        r.RelType.RelationshipsAsTypeWriteable.Add(r);
            }
        }
    }

    private void UnconvertRelationships(sCogneme sT)
    {
        foreach (sCogneme p in sT.relationships)
        {
            Cogneme r = UnConvertRelationship(p, new List<sCogneme>());
            if (r is null) continue;
            if (r.RelType.Label == "play")
            { }
            if (!r.Source.RelationshipsWriteable.Contains(r))
                r?.Source.RelationshipsWriteable.Add(r);
        }
    }

    private Cogneme UnConvertRelationship(sCogneme p, List<sCogneme> stack)
    {
        if (p is null) return null;
        if (stack.Contains(p)) return null;  //infinite recursions loop protection
        stack.Add(p);

        Cogneme source = null;
        if (p.source != -1)
            source = AllThings[p.source];
        else
            return null;
        Cogneme relationshipType = null;
        if (p.relationshipType != -1)
            relationshipType = AllThings[p.relationshipType];
        Cogneme target = null;
        if (p.target != -1)
            target = AllThings[p.target];
        Cogneme r = Labeled(p.label);
        if (r is not null)
        {
            r.Source = source;
            r.Target = target;
            r.RelType = relationshipType;
            r.Weight = p.weight;
        }
        else
        {
            r = new()
            {
                Label = p.label,
                Source = source,
                Target = target,
                RelType = relationshipType,
                Weight = p.weight,
            };
        }
        if (r?.RelationshipsWriteable.Contains(r) is null)
            r.Source?.RelationshipsWriteable.Add(r);

        foreach (sCogneme st in p.relationships)
        {
            var r1 = UnConvertRelationship(st, stack);
            if (r?.RelationshipsWriteable.Contains(r1) is null)
                r?.RelationshipsWriteable.Add(r1);
        }
        stack.RemoveAt(stack.Count - 1);

        return r;
    }

    List<string> ExtractPortionOfUKS(Cogneme root)
    {
        List<string> uksContent = new List<string>();
        if (root is null) return uksContent;
        var descendants = root.DescendentsList;
        foreach (var descendant in root.DescendentsList())
        {
            foreach (var r in descendant.Relationships)
            {
                uksContent.Add(r.ToString());
            }
        }
        return uksContent;
    }
    void MergeStringListIntoUKS(List<String> contentToRestore)
    {
        AddThing("BrainSim", null);
        foreach (string s in contentToRestore)
        {
            string[] strings = s.Split("->");
        }
    }


    /// <summary>
    /// Saves the UKS content to an XML file
    /// </summary>
    /// <param name="fileNameIn">Leave null or empty to use file name from previous operation  </param>

    public bool SaveUKStoXMLFile(string filenameIn = "")
    {
        if (!String.IsNullOrEmpty(filenameIn)) { fileName = filenameIn; }
        string fullPath = GetFullPathFromKnowledgeFileName(fileName);

        if (!CanWriteToFile(fileName, out string message))
        {
            Debug.WriteLine("Could not save file because: " + message);
            return false;
        }

        string tempFilePath = Path.GetTempFileName();
        FormatContentForSaving();
        List<Type> extraTypes = GetTypesInUKS();
        Stream file = File.Create(tempFilePath);
        file.Position = 0;
        try
        {
            XmlSerializer writer = new XmlSerializer(UKSTemp.GetType(), extraTypes.ToArray());
            writer.Serialize(file, UKSTemp);
            file.Close();
            File.Copy(tempFilePath, fullPath, overwrite: true);
        }
        catch (Exception e)
        {
            if (e.InnerException is not null)
                Debug.WriteLine("Xml file write failed because: " + e.InnerException.Message);
            else
                Debug.WriteLine("Xml file write failed because: " + e.Message);
            return false;
        }
        finally
        {
            file.Close();
            UKSTemp = new();
        }
        return true;
    }

    private static List<Type> GetTypesInUKS()
    {
        //TODO, This works for writing but not for reading
        List<Type> extraTypes = new List<Type>();
        foreach (Cogneme t in uKSList)
        {
            if (t.V is not null)
            {
                var theType = t.V.GetType();
                if (!extraTypes.Contains(theType))
                    extraTypes.Add(theType);
            }
        }
        return extraTypes;
    }

    public static string GetFullPathFromKnowledgeFileName(string fileName)
    {
        if (fileName.ToLower().Contains("brainsimulator\\networks\\knowledgefiles"))
        {
            string fullPath = Path.GetFullPath(".");
            if (fullPath.ToLower().Contains("bin\\debug\\net6.0-windows"))
                fullPath = fullPath.ToLower().Replace("bin\\debug\\net6.0-windows", "");
            fullPath += @"networks\KnowledgeFiles\";
            fileName = Path.GetFileNameWithoutExtension(fileName);
            fullPath += fileName + ".xml";
            return fullPath;
        }
        return fileName;
    }

    public static bool CanWriteToFile(string fileName, out string message)
    {
        FileStream file1;
        message = "";
        if (File.Exists(fileName))
        {
            try
            {
                file1 = File.Open(fileName, FileMode.Open);
                file1.Close();
                return true;
            }
            catch (Exception e)
            {
                message = e.Message;
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Loads UKS content from a prvsiously-saved XML file
    /// </summary>
    /// <param name="fileNameIn">Leave null or empty to use file name from previous operation  </param>
    /// <param name="merge">If true, existing UKS content is not deleted and new content is merged by Thing label</param>
    public bool LoadUKSfromXMLFile(string filenameIn = "", bool merge = false)
    {
        //stash the current BrainSim configuration
        var contentToRestore = ExtractPortionOfUKS(Labeled("BrainSim"));

        Stream file;
        if (!String.IsNullOrEmpty(filenameIn)) { fileName = filenameIn; }
        string fullPath = fileName;
        try
        {
            file = File.Open(fullPath, FileMode.Open, FileAccess.Read);
        }
        catch (Exception e)
        {
            Debug.WriteLine("Could not open file because: " + e.Message);
            return false;
        }

        List<Type> extraTypes = new();
        XmlSerializer reader1 = new XmlSerializer(UKSTemp.GetType(), extraTypes.ToArray());
        try
        {
            UKSTemp = (List<sCogneme>)reader1.Deserialize(file);
        }
        catch (Exception e)
        {
            file.Close();
            Debug.WriteLine("Network file load failed, a blank network will be opened. \r\n\r\n" + e.InnerException);//, "File Load Error",
            return false;
        }
        file.Close();
        if (merge)
            DeFormatAndMergeContentAfterLoading();
        else
            DeFormatContentAfterLoading();

        AddBrainSimConfigSectionIfNeeded();

        if (Labeled("BrainSim") is null)
        {
            MergeStringListIntoUKS(contentToRestore);
        }

        // prepend "Module" to any module names which don't have it
        // this is needed for the UKS content change from module names starting with the word "module" to avoid naming collisions
        var activeModules = Labeled("ActiveModule").Children;
        var avaialableModules = Labeled("AvailableModule").Children;

        foreach (Cogneme t in avaialableModules)
        {
            if (!t.Label.ToLower().StartsWith("module"))
                t.Label = "Module" + t.Label;
        }
        foreach (Cogneme t in activeModules)
        {
            if (!t.Label.ToLower().StartsWith("module"))
                t.Label = "Module" + t.Label;
        }

        //more hacks for compatibility old file formatting
        //this does nothing on updated file content
        AddStatement("inheritable", "is-a", "Property");
        Cogneme hasChild = Labeled("has-child");
        if (hasChild is not null)
        {
            hasChild.AddRelationship("is-a", "inverseOf");
            hasChild.RemoveRelationship("isTransitive", "hasProperty");
            hasChild.RemoveRelationship("inheritable", "hasProperty");
        }
        Cogneme isA = Labeled("is-a");
        if (isA is not null)
        {
            isA.AddRelationship("inheritable", "hasProperty");
            isA.AddRelationship("isTransitive", "hasProperty");
            isA.RemoveRelationship("has-child", "inverseOf");
            isA.RemoveRelationship(null, "hasProperty");
        }
        Cogneme has = Labeled("has");
        if (has is not null)
        {
            has.AddRelationship("inheritable", "hasProperty");
        }
        return true;
    }
}
