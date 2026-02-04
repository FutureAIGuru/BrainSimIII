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
        for (int i = 0; i < AllThoughts.Count; i++)
        {
            Thought t = AllThoughts[i];
            if (t.HasAncestorLabeled("BrainSim"))
                continue;
            if (t.Label == "is-a") continue;
            //if (t.Label == "Thought") continue;
            //if (t.Label == "LinkType") continue;
            if (t.Label == "hasAttribute") continue;
            if (t is not null)
            {
                DeleteThought(t);
                i--;
            }
        }

        ThoughtLabels.ClearLabelList();
        foreach (Thought t in AllThoughts)
            ThoughtLabels.AddThoughtLabel(t.Label, t);

        if (Labeled("Thought") is null)
            AddThought("Thought", null);
        Thought isA = Labeled("is-a");
        if (isA is null)
            isA = AddThought("is-a", null);
        Thought hasChild = Labeled("has-child");
        if (hasChild is null)
            hasChild = AddThought("has-child", null);
        Thought linkType = AddThought("LinkType", "Thought");
        isA.AddParent(linkType);
        hasChild.AddParent(linkType);

        GetOrAddThought("Object", "Thought");
        GetOrAddThought("Action", "Thought");
        GetOrAddThought("Link", "Thought");
        GetOrAddThought("LinkType", "Thought");
        GetOrAddThought("Thought", "Thought");
        GetOrAddThought("Unknown", "Thought");
        GetOrAddThought("is-a", "LinkType");
        GetOrAddThought("inverseOf", "LinkType");
        GetOrAddThought("hasProperty", "LinkType");
        GetOrAddThought("is", "LinkType");

        AddStatement("has-child", "inverseOf", "is-a");
        AddStatement("hasAttribute", "is-a", "LinkType");
        AddStatement("can", "is-a", "LinkType");
        AddStatement("mostRecent", "is-a", "LinkType");
        AddStatement("contains", "is-a", "LinkType");
        AddStatement("is-part-of", "is-a", "LinkType");
        AddStatement("contains", "inverseOf", "is-part-of");
        AddStatement("has", "is-a", "LinkType");
        AddStatement("not", "is-a", "LinkType");

        //properties are intenal capabilities of nodes
        AddStatement("Property", "is-a", "LinkType");
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
        AddStatement("ClauseType", "is-a", "LinkType");
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
        GetOrAddThought("number", "Object");
        AddStatement("Comparison", "is-a", "LinkType");
        AddStatement("greaterThan", "is-a", "Comparison");
        AddStatement("greaterThan", "hasProperty", "isTransitive");
        AddStatement("lessThan", "inverseOf", "greaterThan");
        AddStatement("lessThan", "is-a", "Comparison");
        AddStatement("number", "hasProperty", "isExclusive");
        GetOrAddThought("digit", "number");
        GetOrAddThought("isSimilarTo", "Comparison");
        AddStatement("isSimilarTo", "hasProperty", "isCommutative");
        AddStatement("hasDigit", "is-a", "has");


        //put in digits
        GetOrAddThought("-", "digit");
        GetOrAddThought(".", "digit");
        GetOrAddThought("0", "digit");
        GetOrAddThought("2", "digit");
        GetOrAddThought("1", "digit");
        GetOrAddThought("3", "digit");
        GetOrAddThought("4", "digit");
        GetOrAddThought("5", "digit");
        GetOrAddThought("6", "digit");
        GetOrAddThought("7", "digit");
        GetOrAddThought("8", "digit");
        GetOrAddThought("9", "digit");
        GetOrAddThought("some", "number");
        GetOrAddThought("many", "number");
        GetOrAddThought("none", "number");
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
        AddThought("BrainSim", null);
        GetOrAddThought("AvailableModule", "BrainSim");
        GetOrAddThought("ActiveModule", "BrainSim");
    }


    /// <summary>
    /// /////////////////////////////////////////////////////////// XML file load/save
    /// </summary>
    /// 
    //this is a modification of Thought which is used to store and retrieve the KB in XML
    //it eliminates circular references by replacing Thought references with int indexed into an array and makes thoughts much more compact
    public class sThought
    {
        public string label = ""; //this is just for convenience in debugging and should not be used
        public List<sThought> links = new();
        public int source = -1;
        public int target = -1;
        public int linkType = -1;
        public float weight = 0;
        public object V;
    }


    private void FormatContentForSaving()
    {
        UKSTemp.Clear();

        // TODO: Wipe transient data ...
        foreach (Thought t in AllThoughts)
        {
            sThought st = new()
            {
                label = t.Label,
                source = AllThoughts.FindIndex(x => x == t.From),
                linkType = AllThoughts.FindIndex(x => x == t.LinkType),
                target = AllThoughts.FindIndex(x => x == t.To),
                V = t.V,
            };
            if (IsSequenceFirstElement(t))
            {
                foreach (Thought l1 in t.SequenceNodes())
                {
                    sThought sR2 = ConvertLink(l1, new List<Thought>());
                    st.links.Add(sR2);
                }
            }
            foreach (Thought l in t.LinksTo)
            {
                sThought sR = ConvertLink(l, new List<Thought>());
                st.links.Add(sR);
            }
            UKSTemp.Add(st);
        }
    }

    private sThought ConvertLink(Thought l, List<Thought> stack)
    {
        if (l.LinkType.Label == "play")
        { }

        if (stack.Contains(l)) return null;
        stack.Add(l);

        sThought sR = new sThought()
        {
            label = l.Label,
            source = AllThoughts.FindIndex(x => x == l.From),
            target = AllThoughts.FindIndex(x => x == l.To),
            linkType = AllThoughts.FindIndex(x => x == l.LinkType),
            weight = l.Weight,
        };

        foreach (Thought r1 in l.RecursiveLinks)
        {
            sThought sr1 = ConvertLink(r1, stack);
            sR.links.Add(sr1);
        }
        stack.RemoveAt(stack.Count - 1);
        return sR;
    }

    private void DeFormatAndMergeContentAfterLoading()
    {
        //get all the names and weights
        foreach (sThought st in UKSTemp)
        {
            Thought t = new()
            {
                Label = st.label,
                Weight = st.weight,
                V = st.V,
            };
            AllThoughts.Add(t);
        }
        foreach (sThought st in UKSTemp)
        {
            Thought t = Labeled(st.label);
            if (st.source != -1) t.From = AllThoughts[st.source];
            if (st.linkType != -1) t.LinkType = AllThoughts[st.linkType];
            if (st.target != -1) t.To = AllThoughts[st.target];
        }
        foreach (sThought v in UKSTemp)
        {
            AddLinksAsStatements(v);
        }
    }

    private void AddLinksAsStatements(sThought v)
    {
        foreach (sThought p in v.links)
        {
            if (UKSTemp[p.linkType].label == "play")
            { }
            Thought r = AddStatement(UKSTemp[p.source].label, UKSTemp[p.linkType].label, UKSTemp[p.target].label);
            r.Label = p.label;
            AddLinksAsStatements(p);
        }
    }

    private void DeFormatContentAfterLoading()
    {
        AllThoughts.Clear();
        ThoughtLabels.ClearLabelList();
        //get all the thoughts
        foreach (sThought st in UKSTemp)
        {
            Thought t = new()
            {
                Label = st.label,
                V = st.V,
                //useCount = st.useCount
            };
            AllThoughts.Add(t);
        }
        //handle links
        for (int i = 0; i < UKSTemp.Count; i++)
        {
            sThought sT = UKSTemp[i];
            UnconvertLinks(sT);
        }
        //rebuild all the reverse linkages
        foreach (Thought t in AllThoughts)
        {
            foreach (Thought r in t.LinksTo)
            {
                Thought t1 = r.To;
                if (t1 is not null)
                    if (!t1.LinksFromWriteable.Contains(r))
                        t1.LinksFromWriteable.Add(r);
                if (r.LinkType is not null)
                    if (!r.LinkType.LinksAsTypeWriteable.Contains(r))
                        r.LinkType.LinksAsTypeWriteable.Add(r);
            }
        }
    }

    private void UnconvertLinks(sThought sT)
    {
        foreach (sThought p in sT.links)
        {
            Thought r = UnConvertLink(p, new List<sThought>());
            if (r is null) continue;
            if (r.LinkType.Label == "play")
            { }
            if (!r.From.LinksWriteable.Contains(r))
                r?.From.LinksWriteable.Add(r);
        }
    }

    private Thought UnConvertLink(sThought p, List<sThought> stack)
    {
        if (p is null) return null;
        if (stack.Contains(p)) return null;  //infinite recursions loop protection
        stack.Add(p);

        Thought source = null;
        if (p.source != -1)
            source = AllThoughts[p.source];
        else
            return null;
        Thought linkType = null;
        if (p.linkType != -1)
            linkType = AllThoughts[p.linkType];
        Thought target = null;
        if (p.target != -1)
            target = AllThoughts[p.target];
        Thought r = Labeled(p.label);
        if (r is not null)
        {
            r.From = source;
            r.To = target;
            r.LinkType = linkType;
            r.Weight = p.weight;
        }
        else
        {
            r = new()
            {
                Label = p.label,
                From = source,
                To = target,
                LinkType = linkType,
                Weight = p.weight,
            };
        }
        if (r?.LinksWriteable.Contains(r) is null)
            r.From?.LinksWriteable.Add(r);

        foreach (sThought st in p.links)
        {
            var r1 = UnConvertLink(st, stack);
            if (r?.LinksWriteable.Contains(r1) is null)
                r?.LinksWriteable.Add(r1);
        }
        stack.RemoveAt(stack.Count - 1);

        return r;
    }

    List<string> ExtractPortionOfUKS(Thought root)
    {
        List<string> uksContent = new List<string>();
        if (root is null) return uksContent;
        var descendants = root.DescendentsList;
        foreach (var descendant in root.DescendentsList())
        {
            foreach (var r in descendant.LinksTo)
            {
                uksContent.Add(r.ToString());
            }
        }
        return uksContent;
    }
    void MergeStringListIntoUKS(List<String> contentToRestore)
    {
        AddThought("BrainSim", null);
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
        foreach (Thought t in uKSList)
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
    /// <param name="merge">If true, existing UKS content is not deleted and new content is merged by Thought label</param>
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
            UKSTemp = (List<sThought>)reader1.Deserialize(file);
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

        foreach (Thought t in avaialableModules)
        {
            if (!t.Label.ToLower().StartsWith("module"))
                t.Label = "Module" + t.Label;
        }
        foreach (Thought t in activeModules)
        {
            if (!t.Label.ToLower().StartsWith("module"))
                t.Label = "Module" + t.Label;
        }

        //more hacks for compatibility old file formatting
        //this does nothought on updated file content
        AddStatement("inheritable", "is-a", "Property");
        Thought hasChild = Labeled("has-child");
        if (hasChild is not null)
        {
            hasChild.AddLink("is-a", "inverseOf");
            hasChild.RemoveLink("isTransitive", "hasProperty");
            hasChild.RemoveLink("inheritable", "hasProperty");
        }
        Thought isA = Labeled("is-a");
        if (isA is not null)
        {
            isA.AddLink("inheritable", "hasProperty");
            isA.AddLink("isTransitive", "hasProperty");
            isA.RemoveLink("has-child", "inverseOf");
            isA.RemoveLink(null, "hasProperty");
        }
        Thought has = Labeled("has");
        if (has is not null)
        {
            has.AddLink("inheritable", "hasProperty");
        }
        return true;
    }
}
