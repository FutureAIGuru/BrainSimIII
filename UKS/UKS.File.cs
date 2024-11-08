using System.Xml.Serialization;
using System.Diagnostics;

namespace UKS;

public partial class UKS
{
    static string fileName = "";

    public string FileName { get => fileName; }

    public void CreateInitialStructure()
    {
        if (Labeled("Thing") == null)
            AddThing("Thing", null);
        Thing hasChild = AddThing("has-child", null);
        Thing relType = GetOrAddThing("RelationshipType", "Thing");
        hasChild.AddParent(relType);
        GetOrAddThing("Object", "Thing");
        GetOrAddThing("Action", "Thing");
        GetOrAddThing("RelationshipType", "Thing");
        GetOrAddThing("unknownObject", "Object");
        GetOrAddThing("is-a", "RelationshipType");
        GetOrAddThing("inverseOf", "RelationshipType");
        GetOrAddThing("hasProperty", "RelationshipType");
        GetOrAddThing("is", "RelationshipType");

        AddStatement("is-a", "inverseOf", "has-child");
        AddStatement("isExclusive", "is-a", "RelationshipType");
        AddStatement("isTransitive", "is-a", "RelationshipType");
        AddStatement("has", "is-a", "RelationshipType");
        //        AddStatement("is-part-of", "is-a", "RelationshipType");
        //        AddStatement("has", "inverseOf", "is-part-of");

        AddStatement("ClauseType", "is-a", "RelationshipType");
        //        AddStatement("is-part-of", "hasProperty", "isTransitive");
        AddStatement("has-child", "hasProperty", "isTransitive");

        AddBrainSimConfigSectionIfNeeded();
        SetupNumbers();
    }

    public void SetupNumbers()
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
        if (Labeled("BrainSim") != null) return;
        AddThing("BrainSim", null);
        GetOrAddThing("AvailableModule", "BrainSim");
        GetOrAddThing("ActiveModule", "BrainSim");
    }


    /// <summary>
    /// /////////////////////////////////////////////////////////// XML file load/save
    /// </summary>

    private void FormatContentForSaving()
    {
        UKSTemp.Clear();

        // TODO: Wipe transient data ...
        foreach (Thing t in UKSList)
        {
            SThing st = new()
            {
                label = t.Label,
                V = t.V,
                useCount = t.useCount
            };
            foreach (Relationship l in t.Relationships)
            {
                SRelationship sR = ConvertRelationship(l, new List<Relationship>());
                st.relationships.Add(sR);
            }
            UKSTemp.Add(st);
        }
    }

    private SRelationship ConvertRelationship(Relationship l, List<Relationship> stack)
    {
        if (stack.Contains(l)) return null;
        stack.Add(l);
        List<SClauseType> clauseList = null;
        if (l.Clauses.Count > 0) clauseList = new();
        foreach (Clause c in l.Clauses)
        {
            int clauseType = UKSList.FindIndex(x => x == c.clauseType);
            SClauseType ct = new() { clauseType = clauseType, r = ConvertRelationship(c.clause, stack) };
            clauseList.Add(ct);
        }

        SRelationship sR = new SRelationship()
        {
            source = UKSList.FindIndex(x => x == l.source),
            target = UKSList.FindIndex(x => x == l.target),
            relationshipType = UKSList.FindIndex(x => x == l.relType),
            weight = l.Weight,
            hits = l.Hits,
            misses = l.Misses,
            count = l.count,
            GPTVerified = l.GPTVerified,
            clauses = clauseList,
        };
        return sR;
    }

    private void DeFormatAndMergeContentAfterLoading()
    {
        //TODO  add handline of clauses
        foreach (SThing st in UKSTemp)
        {
            if (Labeled(st.label) == null)
            {
                Thing t = new()
                {
                    Label = st.label,
                    V = st.V,
                    useCount = st.useCount
                };
                UKSList.Add(t);
            }
        }
        foreach (SThing v in UKSTemp)
        {
            foreach (SRelationship p in v.relationships)
            {
                AddStatement(UKSTemp[p.source].label, UKSTemp[p.relationshipType].label, UKSTemp[p.target].label);
            }
        }
    }

    private void DeFormatContentAfterLoading()
    {
        UKSList.Clear();
        ThingLabels.ClearLabelList();
        foreach (SThing st in UKSTemp)
        {
            Thing t = new()
            {
                Label = st.label,
                V = st.V,
                useCount = st.useCount
            };
            UKSList.Add(t);
        }
        for (int i = 0; i < UKSTemp.Count; i++)
        {
            foreach (SRelationship p in UKSTemp[i].relationships)
            {
                Relationship r = UnConvertRelationship(p, new List<SRelationship>());
                if (r != null)
                    UKSList[i].RelationshipsWriteable.Add(r);
            }
        }
        //rebuild all the reverse linkages
        foreach (Thing t in UKSList)
        {
            foreach (Relationship r in t.Relationships)
            {
                Thing t1 = r.target;
                if (t1 != null)
                    if (!t1.RelationshipsFromWriteable.Contains(r))
                        t1.RelationshipsFromWriteable.Add(r);
                if (r.relType != null)
                    if (!r.relType.RelationshipsAsTypeWriteable.Contains(r))
                        r.relType.RelationshipsAsTypeWriteable.Add(r);
                AddClauses(r, new List<Relationship>());
            }
        }
    }

    private void AddClauses(Relationship r, List<Relationship> stack)
    {
        if (stack.Contains(r)) return;
        stack.Add(r);
        foreach (Clause c in r.Clauses)
        {
            if (!c.clause.clausesFrom.Contains(r))
                c.clause.clausesFrom.Add(r);
            AddClauses(c.clause, stack);
        }
    }

    private Relationship UnConvertRelationship(SRelationship p, List<SRelationship> stack)
    {
        if (p == null)
            return null;
        if (stack.Contains(p))
            return null;
        stack.Add(p);
        Thing source = null;
        if (p.source != -1)
            source = UKSList[p.source];
        Thing relationshipType = null;
        if (p.relationshipType != -1)
            relationshipType = UKSList[p.relationshipType];
        Thing target = null;
        if (p.target != -1)
            target = UKSList[p.target];
        Relationship r = new()
        {
            source = source,
            target = target,
            relType = relationshipType,
            Hits = p.hits,
            Misses = p.misses,
            Weight = p.weight,
            GPTVerified = p.GPTVerified,
            count = p.count,
            //sentencetype = p.sentencetype as SentenceType,
        };
        if (p.clauses != null)
        {
            foreach (SClauseType sc in p.clauses)
            {
                Clause ct = new Clause() { clauseType = UKSList[sc.clauseType], clause = UnConvertRelationship(sc.r, stack) };
                if (ct.clause != null)
                    r.Clauses.Add(ct);
            }
        }
        return r;
    }

    public static bool CanWriteTo(string fileName, out string message)
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

    List<string> ExtractPortionOfUKS(Thing root)
    {
        List<string> uksContent = new List<string>();
        if (root == null) return uksContent;
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

        if (!CanWriteTo(fileName, out string message))
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
            File.Copy(tempFilePath, fullPath,overwrite: true);
        }
        catch (Exception e)
        {
            if (e.InnerException != null)
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
        foreach (Thing t in uKSList)
        {
            if (t.V != null)
            {
                var theType = t.V.GetType();
                if (!extraTypes.Contains(theType))
                    extraTypes.Add(theType);
            }
        }
        /*
                // Add classes so XML saving works
                extraTypes.Add(typeof(Angle));
                //extraTypes.Add(typeof(CornerTwoD));
                extraTypes.Add(typeof(HSLColor));
                //extraTypes.Add(typeof(KnownArea));
                extraTypes.Add(typeof(Point3DPlus));
                extraTypes.Add(typeof(List<Point3DPlus>));
                extraTypes.Add(typeof(PointPlus));
                //extraTypes.Add(typeof(PointTwoD));
                //extraTypes.Add(typeof(Polar));
                //extraTypes.Add(typeof(SegmentTwoD));
                //extraTypes.Add(typeof(SentenceType));
                //the following are needed to handle the new nested graphic representation
                extraTypes.Add(typeof(System.Windows.Media.Color));
                extraTypes.Add(typeof(System.Windows.Media.Media3D.TranslateTransform3D));
                extraTypes.Add(typeof(System.Windows.Media.Media3D.ScaleTransform3D));
                extraTypes.Add(typeof(System.Windows.Media.Media3D.RotateTransform3D));
                extraTypes.Add(typeof(System.Windows.Media.Media3D.AxisAngleRotation3D));
                extraTypes.Add(typeof(ValueTuple<string, string>));
           */
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
            UKSTemp = (List<SThing>)reader1.Deserialize(file);
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

        if (Labeled("BrainSim") == null)
        {
            MergeStringListIntoUKS(contentToRestore);
        }

        // prepend "Module" to any module names which don't have it
        // this is needed for the UKS content change from module names starting with the word "module" to avoid naming collisions
        var activeModules = Labeled("ActiveModule").Children;
        var avaialableModules = Labeled("AvailableModule").Children;

        foreach (Thing t in avaialableModules)
        {
            if (!t.Label.ToLower().StartsWith("module"))
                t.Label = "Module" + t.Label;
        }
        foreach (Thing t in activeModules)
        {
            if (!t.Label.ToLower().StartsWith("module"))
                t.Label = "Module" + t.Label;
        }
        return true;
    }
}
