/*
 * Brain Simulator Through
 *
 * Copyright (c) 2026 Charles Simon
 *
 * This file is part of Brain Simulator Through and is licensed under
 * the MIT License. You may use, copy, modify, merge, publish, distribute,
 * sublicense, and/or sell copies of this software under the terms of
 * the MIT License.
 *
 * See the LICENSE file in the project root for full license information.
 */
//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using System;
using System.Collections.Generic;
using System.Linq;
using UKS;

namespace BrainSimulator.Modules;

public class ModuleUKSQuery : ModuleBase
{
    public ModuleUKSQuery()
    {
    }
    public override void Fire()
    {
        Init();  //be sure to leave this here
    }
    public override void Initialize()
    {
    }

    /*
    Conventions [hard-coded links]:

All Thought labels are sigularized unless they start with a capital letter. Case is preserved but all searches are case-insensitive.
is-a = has parent of (inverse of has-child)
is = has attribute of
has = has a part of  (arm has elbow) (al
owns = possesses  (Mary owns red hat)
goes = implies location in target
can = implies action possibility

Every item can have subclasses with attributes.
In source and target, attributes precede the class, in type, attributes follow the class. “red hat” “big brown dog” “can play”  “has 5”
When adding:
Hand has 5 fingers creates subclass of has with the attribute of 5 [has->has-child->has.5  has.5->is->5, hand->has.5->fingers
Every subclass will match the search of its parents (searching for has fingers)

When searching, text field may contain:
Item label 
Subclass label (with dots)  has.5
Label and list of attribute labels has 5
List of attributes labels  (resolves to items containing all attributes)
Sequence of labels (resolves to items containing the labels in order)



Query type
Source
Source + type
Source + type + target
Type + target
Target only (handled as source)

Always follow is-a links for inheritance
Follow has ONLY if called out in type

     */

    public List<(Thought r, float confidence)> QueryUKS(string sourceIn, string linkTypeIn, string targetIn,
            string filter, out List<Thought> thoughtResult, out List<Thought> links)
    {
        thoughtResult = new();
        links = new();
        GetUKS();
        if (theUKS is null) return null;
        string source = sourceIn.Trim();
        string linkType = linkTypeIn.Trim();
        string target = targetIn.Trim();

        bool reverse = false;
        //if (source == "" && target == "") return;
        int paramCount = 0;
        if (source != "") paramCount++;
        if (linkType != "") paramCount++;
        if (target != "") paramCount++;

        if (source == "")
        {
            (source, target) = (target, source);
            reverse = true;
        }

        List<Thought> sourceList = ModuleUKSStatement.ThoughtListFromString(source);
        //if (sourceList.Count == 0) return;
        List<Thought> linkTypeList = ModuleUKSStatement.ThoughtListFromString(linkType);
        List<Thought> targetList = ModuleUKSStatement.ThoughtListFromString(target);


        //Handle is-a queries as a special case
        if (linkType.Contains("is-a") && reverse ||
            linkType.Contains("has-child") && !reverse)
        {
            if (sourceList.Count > 0)
                thoughtResult = sourceList[0].Children.ToList();
            return null;
        }
        if (linkType.Contains("is-a") && !reverse ||
            linkType.Contains("has-child") && reverse)
        {
            if (sourceList.Count > 0)
                thoughtResult = sourceList[0].Ancestors.ToList();
            return null;
        }

        //check for target sequence
        if (sourceList.Count > 1)
        {
            float confidence = 0.0f;
            List<Thought> targets = new();
            foreach (Thought t in sourceList)
                targets.Add(t);
            var results1 = theUKS.HasSequence(targets, null);
            Thought tDict = theUKS.Labeled("location");
            if (tDict is not null)
            {
                var seq = tDict.LinksTo.Where(x => x.LinkType.Label == "spelled").ToList()[0].To;
                var testing = theUKS.FlattenSequence(seq);
            }
            return results1;
        }

        links = theUKS.GetAllLinks(sourceList);

        //unreverse the source and target
        if (reverse)
        {
            (source, target) = (target, source);
            (sourceList, targetList) = (targetList, sourceList);
        }

        //handle compound link types
        if (linkTypeList.Count > 0)
            linkType = linkTypeList[0].Label;

        //filter the links
        for (int i = 0; i < links.Count; i++)
        {
            Thought r = links[i];
            if (targetList.Count > 0 && target != "" && !r.To.HasAncestor(targetList[0]))
            { links.RemoveAt(i); i--; continue; }
            if (r.LinkType is not null && linkType != "" && !r.LinkType.HasAncestor(linkType))
            { links.RemoveAt(i); i--; continue; }
        }

        if (filter != "")
        {
            List<Thought> filterThoughts = ModuleUKSStatement.ThoughtListFromString(filter);
            links = theUKS.FilterResults(links, filterThoughts).ToList();
        }

        //if (paramCount == 2)
        //{
        //    foreach (Thought r in links)
        //    {
        //        if (sourceIn == "") thoughtResult.Add(r.source);
        //        if (targetIn == "") thoughtResult.Add(r.target);
        //        if (linkTypeIn == "") thoughtResult.Add(r.linkType);
        //    }
        //}
        return null;
    }
}
