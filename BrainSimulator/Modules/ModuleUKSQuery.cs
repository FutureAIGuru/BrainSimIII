//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using System;
using System.Collections.Generic;
using System.Linq;
using UKS;

namespace BrainSimulator.Modules
{
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
        Conventions [hard-coded relationships]:

All Thing labels are sigularized. Case is preserved but all searches are case-insensitive.
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

Always follow is-a relationships for inheritance
Follow has ONLY if called out in type

         */

        public void QueryUKS(string sourceIn, string relTypeIn, string targetIn,
                string filter, out List<Thing> thingResult, out List<Relationship> relationships)
        {
            thingResult = new();
            relationships = new();
            GetUKS();
            if (UKS == null) return;
            string source = sourceIn.Trim();
            string relType = relTypeIn.Trim();
            string target = targetIn.Trim();

            bool reverse = false;
            //if (source == "" && target == "") return;
            int paramCount = 0;
            if (source != "") paramCount++;
            if (relType != "") paramCount++;
            if (target != "") paramCount++;

            if (source == "")
            {
                (source, target) = (target, source);
                reverse = true;
            }

            List<Thing> sourceList = ModuleUKSStatement.ThingListFromString(source);
            //if (sourceList.Count == 0) return;
            List<Thing> targetList = ModuleUKSStatement.ThingListFromString(target);

            //Handle is-a queries as a special case
            if (relType.Contains("is-a") && reverse ||
                relType.Contains("has-child") && !reverse)
            {
                thingResult = sourceList[0].Children.ToList();
                return;
            }
            if (relType.Contains("is-a") && !reverse ||
                relType.Contains("has-child") && reverse)
            {
                thingResult = sourceList[0].Ancestors.ToList();
                return;
            }

            relationships = UKS.GetAllRelationships(sourceList, reverse);

            //unreverse the source and target
            if (reverse)
            {
                (source, target) = (target, source);
                (sourceList, targetList) = (targetList, sourceList);
            }

            //handle compound relationship types
            List<Thing> relTypeList = ModuleUKSStatement.ThingListFromString(relType);
            if (relTypeList.Count > 0) 
                relType = relTypeList[0].Label;

            //filter the relationships
            for (int i = 0; i < relationships.Count; i++)
            {
                Relationship r = relationships[i];
                if (target != "" && !r.target.HasAncestor(targetList[0]))
                { relationships.RemoveAt(i); i--; continue; }
                if (relType != "" && !r.reltype.HasAncestorLabeled(relType))
                { relationships.RemoveAt(i); i--; continue; }
            }

            if (filter != "")
            {
                List<Thing> filterThings = ModuleUKSStatement.ThingListFromString(filter);
                relationships = UKS.FilterResults(relationships, filterThings).ToList();
            }

            if (paramCount == 2)
            {
                foreach (Relationship r in relationships)
                {
                    if (sourceIn == "") thingResult.Add(r.source);
                    if (targetIn == "") thingResult.Add(r.target);
                    if (relTypeIn == "") thingResult.Add(r.relType);
                }
            }
        }
    }
}
