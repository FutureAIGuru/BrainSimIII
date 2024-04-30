using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;
using System.Windows;

namespace BrainSimulator.Modules;

public partial class ModuleUKS
{

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
        List<SClause> clauseList = null;
        if (l.Clauses.Count > 0) clauseList = new();
        foreach (Clause c in l.Clauses)
        {
            int clauseType = UKSList.FindIndex(x => x == c.clauseType);
            SClause ct = new() { clauseType = clauseType, r = ConvertRelationship(c.clause, stack) };
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
            clauses = clauseList,
        };
        return sR;
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
            if (!c.clause.ClausesFrom.Contains(r))
                c.clause.ClausesFrom.Add(r);
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
        };
        if (p.clauses != null)
        {
            foreach (SClause sc in p.clauses)
            {
                Clause ct = new Clause() { clauseType = UKSList[sc.clauseType], clause = UnConvertRelationship(sc.r, stack) };
                if (ct.clause != null)
                    r.Clauses.Add(ct);
            }
        }
        return r;
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

    private static string GetFullPathFromKnowledgeFileName(string fileName)
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


    
}
