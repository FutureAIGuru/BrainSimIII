//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using static System.Math;
using System.Diagnostics;

namespace BrainSimulator.Modules
{
    public class ModuleQuery : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XmlIgnore] 
        //public theStatus = 1;


        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleQuery()
        {
            minHeight = 1;
            maxHeight = 500;
            minWidth = 1;
            maxWidth = 500;
        }


        //fill this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            Init();  //be sure to leave this here

            //if you want the dlg to update, use the following code whenever any parameter changes
            UpdateDialog();

            GetUKS();
        }

        internal Thing findObjectByRelation(Thing relation, Thing objectReference)
        {
            Thing ret = null;
            Thing relatedObject = null;

            if (objectReference.V?.ToString() == "Self")
            {
                relatedObject = FindObjectInRelationToSallie(relation.RelationshipsAsThings[0]);
            }
            else
            {
                Thing physicalObject = FindObjectFromReference(objectReference);

                if (physicalObject == null) return null;

                relatedObject = FindObjectFromRelation(relation, physicalObject);
            }



            if (relatedObject == null)
            {
                ret = new Thing();
                ret.V = "NoRelation";
                return ret;
            }

            return DescribeObject(relatedObject);
        }

        private Thing FindObjectInRelationToSallie(Thing relation)
        {
            IList<Thing> relatedObjects = UKS.SearchByAngle(UKS.Labeled("MentalModel").Children, directionToAngle(relation));
            if (relatedObjects.Count > 0) return relatedObjects[0];
            else return null;
        }

        private Thing FindObjectFromRelation(Thing relation, Thing physicalObject)
        {
            IList<Thing> relatedObjects = UKS.QueryRelationships(physicalObject, directionToRelation(relation.RelationshipsAsThings[0]));
            if (relatedObjects.Count > 0) return relatedObjects[0];
            else return null;
        }

        // This returns a thing with a value describing 
        // an object reference. 
        // If the input is a reference to "this"
        // returns parent or description of central object.
        // Such as, "basketball" or "orange sphere"
        internal Thing DescribeObjectReference(Thing reference)
        {
            Thing physicalObject = FindObjectFromReference(reference);
            if (physicalObject == null) return null;
            return DescribeObject(physicalObject);
        }

        internal Thing FindObjectFromReference(Thing reference)
        {
            Thing physicalObject = null;
            if (reference.V == null)
            {
                if (reference.Label == "Object")
                {
                    physicalObject = UKS.Labeled(reference.RelationshipsAsThings[0].V.ToString());
                }
                else physicalObject = UKS.QueryProperties(new List<String> { reference.RelationshipsAsThings[0].V.ToString() });
            }
            else if (reference.V.ToString().Equals("AttentionObject"))
            {
                physicalObject = FindObjectMostCenter();
            }
            else if (reference.V.ToString().Equals("ObjectProperties"))
            {
                List<String> properties = reference.RelationshipsAsThings.ConvertAll(t => t.V.ToString());

                string objectLabel = "";
                foreach (Thing t in reference.RelationshipsAsThings) objectLabel += t.V.ToString() + "-";
                objectLabel = objectLabel.Substring(0, objectLabel.Length - 1);
                physicalObject = UKS.Labeled(objectLabel, UKS.Labeled("Object").Descendents.ToList());

                if (physicalObject == null)
                {
                    physicalObject = UKS.QueryPropertiesThroughObjectTree(properties);
                }
            }

            return physicalObject;
        }

        // takes in an object, returns thing
        // describing parent or said objects properties.
        private Thing DescribeObject(Thing physicalObject)
        {
            Thing ret;
            Thing parent = new();

            parent.V = BestReference(physicalObject);

            if (physicalObject.HasAncestor(UKS.Labeled("Object")))
            {
                parent.V = BestParent(physicalObject.Parents);
                //physicalObject.Parents[0].Label;
            }

            if (parent.V == null)
            {
                parent.V = "No Description";
                return parent;
            }

            Thing size = new();

            Thing color = new();
            Thing shape = new();

            foreach (Relationship l in physicalObject.Relationships)
            {
                if ((l.T as Thing).Parents[0].Label == "col") color = (l.T as Thing);
                else if ((l.T as Thing).Parents[0].Label == "shp") shape = (l.T as Thing);
                else if ((l.T as Thing).Parents[0].Label == "siz") size = (l.T as Thing);
            }
            ret = new();

            if (physicalObject.Parents.Contains(UKS.Labeled("MentalModel")))
            {
                if (!(parent.V.ToString() == FindPropertyName(shape) ||
                parent.V.ToString() == FindPropertyName(color) ||
                parent.V.ToString() == FindPropertyName(size)))
                {
                    ret.V += parent.V.ToString();
                }
                else
                {
                    if (FindPropertyName(size) != null) ret.V += FindPropertyName(size) + " ";
                    if (FindPropertyName(color) != null) ret.V += FindPropertyName(color) + " ";
                    if (FindPropertyName(shape) != null) ret.V += FindPropertyName(shape);
                }
            }
            else
            {
                if (FindPropertyName(size) != null) ret.V += FindPropertyName(size) + " ";
                if (FindPropertyName(color) != null) ret.V += FindPropertyName(color) + " ";

                if (!(parent.V.ToString() == FindPropertyName(shape) ||
                parent.V.ToString() == FindPropertyName(color) ||
                parent.V.ToString() == FindPropertyName(size)))
                {
                    if (parent.V.ToString() == "Object")
                    {
                        if (FindPropertyName(shape) != null) ret.V += FindPropertyName(shape) + " ";
                    }
                    ret.V += parent.V.ToString();
                }
                else
                {
                    if (FindPropertyName(shape) != null) ret.V += FindPropertyName(shape);
                }
            }

            return ret;
        }

        internal bool doesThingHaveAllRelations(Thing source, List<(Thing, int, Thing, string)> relationList)
        {
            foreach ((Thing relationshipType, int count, Thing target, String exclude) in relationList)
            {
                if (!doesThingHaveRelation(source, relationshipType, count, target)) return false;
            }
            return true;
        }

        internal bool doesThingHaveRelation(Thing source, Thing relationType, int count, Thing target)
        {
            List<Relationship> relationships = GetRelationships(source);

            foreach (Relationship relationship in relationships)
            {
                if (relationship.relType == relationType &&
                    (count == -1 || relationship.count == count) &&
                    relationship.T == target) return true;
            }

            return false;
        }

        internal List<Thing> QueryObjectsWithAllRelations(List<(Thing, int, Thing, string)> relationList)
        {
            List<Thing> objects = new();

            foreach ((Thing relationshipType, int count, Thing target, string action) in relationList)
            {
                List<Thing> things = QueryObjectsWithRelation(relationshipType, count, target);
                if (objects.Count == 0) objects.AddRange(things);
                else if (action == "exclude")
                {
                    objects = objects.Except(things).ToList();
                }
                else
                {
                    objects = objects.Intersect(things).ToList();
                }
            }


            return objects;
        }

        internal List<Thing> QueryObjectsWithRelation(Thing relation, int count, Thing target)
        {
            List<Thing> objectsWithRelation = new();

            foreach (Relationship l in relation.RelationshipsFrom)
            {
                if (l.target == target && (count == -1 || l.count == count))
                {
                    objectsWithRelation.Add((l.source as Thing));
                    foreach (Thing descendent in (l.source as Thing).Descendents)
                    {
                        bool addDescendent = true;
                        foreach (Relationship relationship in descendent.Relationships)
                        {
                            if (relationship.relType == relation &&
                                relationship.T == target) addDescendent = false;
                        }
                        if (addDescendent) objectsWithRelation.Add(descendent);
                    }
                }
            }

            return objectsWithRelation;
        }

        private string FindPropertyName(Thing property)
        {
            GetUKS();
            Thing objectRoot = UKS.GetOrAddThing("Object", "Thing");
            if (property.GetRelationshipByWithAncestor(objectRoot) == null)
            {
                return null;
            }
            else
            {
                foreach (Relationship l in property.GetRelationshipByWithAncestor(objectRoot))
                {

                    if ((l.T as Thing).Relationships.Count == 1)
                    {
                        return (l.T as Thing).Label;
                    }
                }
            }
            if (property.V != null)
                return property.V.ToString();
            return null;
        }

        public void RemoveFromHierarchy(Thing child, Thing parent)
        {
            Thing objectRoot = UKS.GetOrAddThing("Object", "Thing");
            //should only use code below if object exists in the object tree
            if (objectRoot.Descendents.Contains(child))
            {

                child.RemoveParent(parent);

                if (!objectRoot.Descendents.Contains(child))
                {
                    child.AddParent(objectRoot);
                }
            }
            else
            {
                child.RemoveRelationship(parent);
            }
        }

        public void NotProperty(Thing Target, Thing property)
        {
            GetUKS();
            ModuleObject x = (ModuleObject)FindModule(typeof(ModuleObject));
            Thing Object = UKS.GetOrAddThing("Object", "Thing");

            if (Target.RelationshipsAsThings.Contains(property))
            {
                Target.RemoveRelationship(property);
                x.AddAllMatches(Target);
            }

        }

        private static string BestParent(IList<Thing> parents)
        {
            float max = 0;
            int x = 0;
            if (parents.Count > 1)
            {
                for (int i = 0; i < parents.Count; i++)
                {
                    if (max < parents[i].Relationships.Count && parents[i].HasAncestorLabeled("Object"))
                    {
                        max = parents[i].Relationships.Count;
                        x = i;
                    }

                }
                return parents[x].Label;
            }
            else
            {
                return parents[0].Label;
            }
        }

        private static string BestReference(Thing Object)
        {
            float max = 0;
            int x = 0;
            if (Object.Relationships.Count > 1)
            {
                for (int i = 0; i < Object.Relationships.Count; i++)
                {
                    if (Object.RelationshipsAsThings[i].HasAncestorLabeled("Object"))
                    {
                        x = i;
                        max = Object.RelationshipsAsThings[i].Relationships.Count;
                    }
                    else if (max < Object.RelationshipsAsThings[i].Relationships.Count)
                    {
                        max = Object.RelationshipsAsThings[i].Relationships.Count;
                        x = i;
                    }
                }
                if (Object.RelationshipsAsThings[x].Relationships.Count == 0)
                {
                    return null;
                }
                else
                {
                    return Object.RelationshipsAsThings[x].Label;
                }
            }
            else
            {
                if (Object.RelationshipsAsThings.Count == 0 || Object.RelationshipsAsThings[0].Relationships.Count == 0)
                {
                    return null;
                }
                return Object.RelationshipsAsThings[0].Label;
            }
        }

        public Thing FindObjectMostCenter()
        {
            GetUKS();
            Thing physicalObject = null;
            Angle a = (Angle)(UKS.Labeled("CameraPan").V);
            IList<Thing> objects = UKS.SearchByAngle(UKS.Labeled("MentalModel").Children, a);
            if (objects.Count > 0) physicalObject = objects[0];
            return physicalObject;
        }

        internal Thing CountObjects(Thing reference)
        {
            List<Thing> properties = new();
            for (int i = 0; i < reference.Relationships.Count; i++)
            {
                properties.Add((reference.Relationships[i].T as Thing));
            }

            IList<Thing> queryResults = UKS.QueryPropertiesByAssociatedWords(properties, true);

            Thing count = new Thing();
            count.Label = "cCount";
            count.V = queryResults.Count;

            return count;
        }

        internal List<Relationship> GetRelationships(Thing t1)
        {
            Dictionary<(Thing type, Thing target), Relationship> relations = new();
            Queue<Thing> thingsToProcess = new();
            thingsToProcess.Enqueue(t1);

            while (thingsToProcess.Count != 0)
            {
                Thing toProcess = thingsToProcess.Dequeue();

                foreach (Relationship l in toProcess.RelationshipsWithoutChildren)
                {

                    if (l.relType is not null && !relations.ContainsKey((l.relType, (l.T as Thing))))
                    {
                        relations.Add((l.relType, (l.T as Thing)), l);
                    }
                }

                foreach (Thing parent in toProcess.Parents)
                {
                    if (parent.Label != "Object") thingsToProcess.Enqueue(parent);
                }
            }

            return relations.Values.ToList();
        }

        internal Thing HasHowMany(Thing source, Thing target)
        {
            var z = UKS.Query(source, null, target);
            return UKS.ResultsOfType(z, "number")[0];
        }

        private static List<string> directionToRelation(Thing direction)
        {
            List<String> ret = new();
            switch (direction.V.ToString())
            {
                case "left":
                    ret.Add("<cen");
                    break;
                case "right":
                    ret.Add(">cen");
                    break;
                case "front":
                    ret.Add("vcen");
                    break;
                case "behind":
                    ret.Add("^cen");
                    break;
                default:
                    break;
            }
            return ret;
        }

        private static Angle directionToAngle(Thing relation)
        {
            switch (relation.V.ToString())
            {
                case "left": return Angle.FromDegrees(90);
                case "right": return Angle.FromDegrees(-90);
                case "front": return Angle.FromDegrees(0);
                case "behind": return Angle.FromDegrees(180);
                default: return Angle.FromDegrees(-0);
            }
        }

        public List<Thing> QueryCreation(List<string> properties, List<string> relationships)
        {
            GetUKS();
            if (UKS == null) return null;
            Thing baseThing = UKS.QueryProperties(properties);
            if (baseThing == null) return null;
            List<Thing> retVal = UKS.QueryRelationships(baseThing, relationships);
            if (retVal == null) retVal = new List<Thing>();
            retVal.Insert(0, baseThing);
            return retVal;
        }

        public IList<Thing> QueryPosition()
        {
            GetUKS();
            if (UKS == null) return null;
            Thing mentalModel = UKS.GetOrAddThing("MentalModel", "Thing");
            if (mentalModel is null) return null;
            return UKS.OrderByDistanceFromCenter(mentalModel.Children);
        }

        public Thing CreateChildParentRelationship(List<string> properties, string parent)
        {
            GetUKS();
            if (UKS == null) return null;
            ModuleObject x = (ModuleObject)FindModule(typeof(ModuleObject));
            Thing physObject = UKS.QueryProperties(properties);
            if (physObject == null) return null;
            if (x != null && parent != "") { x.AddParentReference(physObject, parent); }

            return physObject;
        }

        public Thing CreateChildParentRelationship(Thing physObject, string parent)
        {
            GetUKS();
            if (UKS == null) return null;
            ModuleObject x = (ModuleObject)FindModule(typeof(ModuleObject));
            if (physObject == null) return null;
            if (parent == "") return null;
            if (x != null) { x.AddParentReference(physObject, parent); }

            Thing parentThing = UKS.Labeled(parent);
            if (parentThing.Relationships.Count == 0 ||
                    parentThing.Relationships.All(r => r.relType != UKS.Labeled("HasProperty")))
            {
                ExtractProperties(physObject, parentThing);
            }
            else
            {
               PairProperties(physObject, parent);
            }

            return physObject;
        }

        public void ExtractProperties(Thing objectThing, Thing parentThing)
        {
            if (objectThing.HasAncestor(UKS.Labeled("Object")))
            {
                foreach (Relationship r in objectThing.Relationships)
                {
                    if (r.relType == UKS.Labeled("HasProperty"))
                    {
                        parentThing.AddRelationship((r.T as Thing), r.relType);
                    }
                }
            }
            if (objectThing.HasAncestor(UKS.Labeled("MentalModel")))
            {
                foreach (Relationship r in objectThing.Relationships)
                {
                    if (r.T == null) continue;
                    if (r.T.HasAncestor(UKS.Labeled("Property")) &&
                        !r.T.HasAncestor(UKS.Labeled("TransientProperty")))
                    {
                        parentThing.AddRelationship((r.T as Thing), UKS.GetOrAddThing("HasProperty", "Relationship"));
                    }
                }
            }
        }

        public void PairProperties(Thing physObject, string parent)
        {
            Thing parentThing = UKS.Labeled(parent);
            List<Relationship> toRemove = new();
            foreach (Relationship parentRelationship in parentThing.Relationships)
            {
                if (parentRelationship.relType != UKS.Labeled("HasProperty")) continue;
                bool removeRelation = true;
                foreach (Relationship objectRelationship in physObject.Relationships)
                {
                    if (parentRelationship.T == objectRelationship.T) removeRelation = false;
                }
                if (removeRelation) toRemove.Add(parentRelationship);
            }
            foreach (Relationship r in toRemove)
            {
                parentThing.RemoveRelationship((r.T as Thing), r.relType);
            }
        }

        public Thing CreateChildParentRelationship(string label, string parent)
        {
            GetUKS();
            if (UKS == null) return null;
            ModuleObject x = (ModuleObject)FindModule(typeof(ModuleObject));
            Thing labeledObject = UKS.Labeled(label);
            if (labeledObject == null) return null;
            if (x != null && parent != "") { x.AddParentReference(labeledObject, parent); }

            return labeledObject;
        }

        public void AddParentToObject(string parentName, string objectLabel)
        {
            GetUKS();
            if (UKS == null) return;

            ModuleObject x = (ModuleObject)FindModule(typeof(ModuleObject));
            if (x == null) return;
            Thing Object = UKS.GetOrAddThing("Object", "Thing");
            Thing oldParent = new();
            foreach (Thing obj in Object.Descendents)
            {
                if (obj.Label == objectLabel)
                {
                    oldParent = obj;
                    break;
                }
            }

            if (oldParent.Label != "")
            {
                x.AddParentObject(oldParent, parentName);
            }
        }

        public Thing QueryCreation(List<string> properties)
        {
            GetUKS();
            if (UKS == null) return null;
            return UKS.QueryProperties(properties);
        }
        public IList<Thing> QueryProperties(List<string> properties)
        {
            GetUKS();
            if (UKS == null) return null;
            return UKS.QueryProperties1(properties);
        }

        public void QueryCreation(string parent, List<string> Children)
        {
            GetUKS();
            if (UKS == null) return;

            ModuleObject x = (ModuleObject)FindModule(typeof(ModuleObject));
            Thing Object = UKS.GetOrAddThing("Object", "Thing");
            Thing oldParent = new();
            bool leaveLoop = false;
            foreach (string c in Children)
            {
                foreach (Thing obj in Object.Descendents)
                {
                    if (obj.Label == c)
                    {
                        oldParent = obj;
                        leaveLoop = true;
                        break;
                    }
                }
                if (leaveLoop) { break; }
            }
            if (oldParent.Label != "")
            {
                x.AddParentObject(oldParent, parent);
            }
            return;
        }
        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
        }

        //the following can be used to massage public data to be different in the xml file
        //delete if not needed
        public override void SetUpBeforeSave()
        {
        }
        public override void SetUpAfterLoad()
        {
        }

        //called whenever the size of the module rectangle changes
        //for example, you may choose to reinitialize whenever size changes
        //delete if not needed
        public override void SizeChanged()
        {
            if (mv == null) return; //this is called the first time before the module actually exists
        }

        public override void UKSInitializedNotification()
        {

        }
    }
}
