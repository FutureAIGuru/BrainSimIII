using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;
using System.Windows;
using System.Diagnostics;
using static BrainSimulator.Modules.ModuleOnlineInfo;

namespace BrainSimulator.Modules
{
    public partial class ModuleUKS
    {

        private void CreateInitialStructure()
        {
            GetUKS();
            UKSList.Clear();
            AddThing("Thing", null);
            GetOrAddThing("Object", "Thing");
            GetOrAddThing("unknownObject", "Object");
            GetOrAddThing("is-a", "RelationshipType");
            GetOrAddThing("inverseOf", "RelationshipType");
            GetOrAddThing("hasProperty", "RelationshipType");
            GetOrAddThing("is", "RelationshipType");
            AddStatement("is-a", "inverseOf", "has-child");
            AddStatement("isExclusive", "is-a", "RelationshipType");
            AddStatement("has", "is-a", "RelationshipType");

            //This hack is here because some Things created at startup don't get put into the UKS List.
            foreach (Thing t in ThingLabels.AllThingsInLabelList())
                if (!UKSList.Contains(t))
                    UKSList.Add(t);
            SetupNumbers();
        }

        public void SetupNumbers()
        {
            GetOrAddThing("number", "Object");
            AddStatement("greaterThan", "is-a", "RelationshipType");
            AddStatement("lessThan", "inverseOf", "greaterThan");
            AddStatement("lessThan", "is-a", "RelationshipType");
            AddStatement("number", "hasProperty", "isExclusive");
            GetOrAddThing("digit", "number");
            GetOrAddThing("isSimilarTo", "RelationshipType");
            GetOrAddThing("commutative", "RelationshipType");
            AddStatement("isSimilarTo", "hasProperty", "commutative");
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

            //AddStatement("none", "isSimilarTo", "0");
            //AddStatement("1", "greaterThan", "0");
            //AddStatement("2", "greaterThan", "1");
            //AddStatement("3", "greaterThan", "2");
            //AddStatement("4", "greaterThan", "3");
            //AddStatement("5", "greaterThan", "4");
            //AddStatement("6", "greaterThan", "5");
            //AddStatement("7", "greaterThan", "6");
            //AddStatement("8", "greaterThan", "7");
            //AddStatement("9", "greaterThan", "8");

            //AddStatement("some", "greaterThan", "1");
            //AddStatement("many", "greaterThan", "4");

            AddStatement("pi", "is-a", "number");
            AddStatement("pi", "hasDigit*", "3");
            AddStatement("pi", "hasDigit*", ".");
            AddStatement("pi", "hasDigit*", "1");
            AddStatement("pi", "hasDigit*", "4");
            AddStatement("pi", "hasDigit*", "1");
            AddStatement("pi", "hasDigit*", "5");
            AddStatement("pi", "hasDigit*", "9");
        }



        /// <summary>
        /// /////////////////////////////////////////////////////////// XML File save/load
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


        /// <summary>
        /// /////////////////////////////////////////////////////////// XML file load/save
        /// </summary>
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

        private void FormatContentForSaving()
        {
            UKSTemp.Clear();

            // TODO: Wipe transient data ...
            GetUKS();
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
                    SRelationship sR = ConvertRelationship(l);
                    st.relationships.Add(sR);
                }
                UKSTemp.Add(st);
            }
        }

        private SRelationship ConvertRelationship(Relationship l)
        {
            List<SClauseType> clauseList = null;
            if (l.clauses.Count > 0) clauseList = new();
            foreach (ClauseType c in l.clauses)
            {
                int clauseType = UKSList.FindIndex(x => x == c.clauseType);
                SClauseType ct = new() { clauseType = clauseType, r = ConvertRelationship(c.clause) };
                clauseList.Add(ct);
            }

            SRelationship sR = new SRelationship()
            {
                source = UKSList.FindIndex(x => x == l.source),
                target = UKSList.FindIndex(x => x == l.target),
                relationshipType = UKSList.FindIndex(x => x == l.relType),
                weight = l.weight,
                hits = l.hits,
                misses = l.misses,
                count = l.count,
                //sentencetype = l.sentencetype,
                clauses = clauseList,
            };
            return sR;
        }

        public override void SetUpAfterLoad()
        {
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
        private void DeFormatAndMergeContentAfterLoading()
        {
            //TODO  add handline of clauses
            foreach (SThing st in UKSTemp)
            {
                if (UKS.Labeled(st.label) == null)
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
                    UKS.AddStatement(UKSTemp[p.source].label, UKSTemp[p.relationshipType].label, UKSTemp[p.target].label);
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
                    Relationship r = UnConvertRelationship(p);

                    UKSList[i].RelationshipsWriteable.Add(r);
                }
            }
            //rebuild all the reverse linkages
            foreach (Thing t in UKSList)
            {
                foreach (Relationship r in t.Relationships)
                {
                    Thing t1 = r.T;
                    if (t1 != null)
                        t1.RelationshipsFromWriteable.Add(r);
                    if (r.relType != null)
                        r.relType.RelationshipsAsTypeWriteable.Add(r);
                    AddClauses(r);
                }
            }
        }

        private void AddClauses(Relationship r)
        {
            foreach (ClauseType c in r.clauses)
            {
                Relationship r1 = c.clause;
                if (r1.source != null)
                    r1.source.RelationshipsWriteable.Add(r1);
                if (r1.target != null)
                    r1.target.RelationshipsFromWriteable.Add(r1);
                if (r1.relType != null)
                    r1.relType.RelationshipsFromWriteable.Add(r1);
                c.clause.clausesFrom.Add(r);
                AddClauses(r1);
            }
        }

        private Relationship UnConvertRelationship(SRelationship p)
        {
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
                hits = p.hits,
                misses = p.misses,
                weight = p.weight,
                count = p.count,
                //sentencetype = p.sentencetype as SentenceType,
            };
            if (p.clauses != null)
            {
                foreach (SClauseType sc in p.clauses)
                {
                    r.clauses.Add(new ClauseType() { clauseType = UKSList[sc.clauseType], clause = UnConvertRelationship(sc.r) });
                }
            }
            return r;
        }

        public void SaveUKStoXMLFile()
        {
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

        private static List<Type> GetTypesInUKS()
        {
            List<Type> extraTypes = new List<Type>();
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

        public void LoadUKSfromXMLFile(bool merge = false)
        {
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
}
