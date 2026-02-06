using System.ComponentModel;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Xml.Serialization;

namespace UKS;

public partial class UKS
{
    static string fileName = "";

    public string FileName { get => fileName; }

    /// <summary>
    /// /////////////////////////////////////////////////////////// XML file load/save
    /// </summary>
    /// 
    //this is a modification of Thought which is used to store and retrieve the UKS in XML
    //it eliminates circular references by replacing Thought references with int indexed into an array 
    public class sThought
    {
        public int index;
        public string label = ""; 
        public List<sThought> links = new();
        [DefaultValue(-1)]
        public int source = -1;
        [DefaultValue(-1)]
        public int linkType = -1;
        [DefaultValue(-1)]
        public int target = -1;
        [DefaultValue(1)]
        public float weight = 1;
        [DefaultValue(null)]
        public object V;
        public bool ShouldSerializelinks() => links is not null && links.Count > 0;
        public override string ToString()
        {
            return $"{ index}, {label}";
        }
    }

    /// <summary>
    /// Saves the UKS content to an XML file
    /// </summary>
    /// <param name="fileNameIn">Leave null or empty to use file name from previous operation  </param>
    public bool SaveUKStoXMLFile(string filenameIn = "")
    {
        //if you don't pass in a file name, it uses the previous name
        if (!String.IsNullOrEmpty(filenameIn)) { fileName = filenameIn; }
        if (!CanWriteToFile(fileName, out string message))
        {
            Debug.WriteLine("Could not save file because: " + message);
            return false;
        }

        string tempFilePath = Path.GetTempFileName();
        UKSTemp.Clear();
        FormatContentForSaving("BrainSim");
        FormatContentForSaving("Thought");

        //List<Type> extraTypes = GetTypesInUKS();
        Stream file = File.Create(tempFilePath);
        file.Position = 0;
        try
        {
            XmlSerializer writer = new XmlSerializer(UKSTemp.GetType());
            writer.Serialize(file, UKSTemp);
            file.Close();
            File.Copy(tempFilePath, fileName, overwrite: true);
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

    //gets the index of a Thought in the output array
    //and creates an entry if it's not already there.
    int GetIndex(Thought t)
    {
        if (t is null) return -1;
        if (string.IsNullOrWhiteSpace(t.Label))  // Put the GUID into the label only when it's unlabeled
            t.Label = $"unl_{Guid.NewGuid().ToString("N")[..8]}";
        int index = UKSTemp.FindIndex(x => x.label == t.Label);
        if (index == -1)
        {
            sThought st = new()
            {
                index = UKSTemp.Count,
                label = t.Label,
                source = GetIndex(t.From),
                linkType = GetIndex(t.LinkType),
                target = GetIndex(t.To),
                V = t.V,
            };
            index = UKSTemp.Count;
            UKSTemp.Add(st);
        }
        return index;
    }

    private void FormatContentForSaving(Thought root)
    {
        // TODO: Wipe transient data ...
        //foreach (Thought t in AllThoughts)
        GetIndex(root);
        foreach (var t in root.EnumerateSubThoughts())
        {
            string label = t.Label;
            if (string.IsNullOrWhiteSpace(label))  // Put the GUID into the label only when it's unlabeled
                label = $"unl_{Guid.NewGuid().ToString("N")[..8]}";
            int from = GetIndex(t.From);
            int sType = GetIndex(t.LinkType);
            int to = GetIndex(t.To);
            if (UKSTemp.Count >= 170)
            { }

            sThought st = new()
            {
                index = UKSTemp.Count,
                label = label,
                source = from,
                linkType = sType,
                target = to,
                weight = t.Weight,
                V = t.V,
            };
            UKSTemp.Add(st);
        }
        RemoveTempLabels(root);
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
    public bool LoadUKSfromXMLFile(string filenameIn = "")
    {
        //stash the current BrainSim configuration
        var contentToRestore = ExtractPortionOfUKS(Labeled("BrainSim"));

        Stream file;
        if (!String.IsNullOrEmpty(filenameIn)) { fileName = filenameIn; }
        try
        {
            file = File.Open(fileName, FileMode.Open, FileAccess.Read);
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

        DeFormatContentAfterLoading();

        //EVERYTHING below is for compatibility with older xml files.

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
                Weight = st.weight,
                V = st.V,
            };
            if (st.source != -1)
                t.From = theUKS.Labeled(UKSTemp[st.source].label);
            if (st.linkType != -1)
                t.LinkType = theUKS.Labeled(UKSTemp[st.linkType].label);
            if (st.target!= -1)
                t.To = theUKS.Labeled(UKSTemp[st.target].label);
            AllThoughts.Add(t);
            t.From?.AddLink(t.To, t.LinkType);
        }
        RemoveTempLabels("Thought");
        RemoveTempLabels("BrainSim");

        //handle links
        //In the updated format, there are no separate links entries
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
}
