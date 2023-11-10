//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using System;
using System.Collections.Generic;
using static System.Math;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using HelixToolkit.Wpf;

namespace BrainSimulator.Modules
{
    public class ModuleMentalModel : ModuleBase
    {
        // This gets set by the module class to alert its dialog to 
        // update the objects and their positions when needed. 
        public bool MentalModelChanged = false;

        public bool SalliesView = false;

        int maxCornersPerObject = 50;

        public ModuleMentalModel()
        {
            minHeight = 1;
            maxHeight = 500;
            minWidth = 1;
            maxWidth = 500;
        }

        public override void Fire()
        {
            Init();  //be sure to leave this here
            GetUKS();

            UpdateDialog();
        }

        public override void Initialize()
        {
            MentalModelChanged = true;
        }


        private Point3DPlus lastMove = new();
        public Point3DPlus LastMove { get => lastMove; set => lastMove = value; }

        public void ResetMentalModel()
        {
            // if image recognition is on, old objects will be recognized after the mental model is deleted
            ModulePodInterface thePod = (ModulePodInterface)FindModule("PodInterface");
            if (thePod != null)
            {
                thePod.CommandStop();
                thePod.UpdateSallieSelf();
            }

            ModuleMentalModel theModel = (ModuleMentalModel)FindModule("MentalModel");
            if (theModel == null) return;
            theModel.Clear();
            Thing trans = UKS.Labeled("TransientProperty");
            for (int i = 0; trans.Children.Count > i;)
            {
                UKS.DeleteAllChildren(trans);
            }
            MentalModelChanged = true;
        }

        public void Move(Point3DPlus delta)
        {
            GetUKS();
            lastMove = delta;  // Do not add to this, we only calculate the last move...

            Thing pointsRoot = UKS.Labeled("Property");
            if (pointsRoot == null) return;
            foreach (Thing t in pointsRoot.Descendents)
            {
                if (t.V is Point3DPlus p && t.Parents.FindFirst(x => x.Label.StartsWith("siz")) == null)
                {
                    p.X -= delta.X;
                    p.Y -= delta.Y;
                    p.Z -= delta.Z;
                }
            }
            MentalModelChanged = true;
        }

        public void Turn(Angle delta)
        {
            GetUKS();
            if (delta == 0) return;
            Thing pointsRoot = UKS.Labeled("Property");

            if (pointsRoot == null) return;
            foreach (Thing t in pointsRoot.Descendents)
            {
                if (t.V is Point3DPlus p && t.Parents.FindFirst(x => x.Label.StartsWith("siz")) == null)
                {
                    p.Theta += delta;
                }
            }
            MentalModelChanged = true;
        }
        public void Clear()
        {
            GetUKS();
            if (UKS == null) return;
            Thing mentalModelRoot = UKS.GetOrAddThing("MentalModel", "Thing");
            UKS.DeleteAllChildren(mentalModelRoot);
            MentalModelChanged = true;
        }

        // EXPLANATION OF PROPERTIES LAYOUT:
        // MentalModel
        //  p0
        //    refs: prop1, prop2 prop3
        // Properties
        //    siz     each property type has children which are the property value group
        //      siz0    each property value group has children which are exemplars of values 
        //         sizN V  examplars
        //      siz1
        //    shp
        //    col
        //    . . .
        //    TransientProperties
        //      cen
        //         cen0 V has has a specific value because exemplars are not necessary
        //      area
        //      . . .


        public void UpdateProperties(Thing poThing, Dictionary<string, object> properties)
        {
            if (poThing == null) return;
            GetUKS();
            if (UKS == null) return;

            List<Thing> existingProperties = poThing.RelationshipsAsThings;
            foreach (KeyValuePair<string, object> prop in properties)
            {
                string propName = prop.Key;
                object propVal = prop.Value;
                if (propName.StartsWith("col"))
                { }
                Thing oldRef = existingProperties.FindFirst(x => x.Label.StartsWith(prop.Key));
                if (prop.Value is Thing t)
                {
                    poThing.RemoveRelationship(oldRef);
                    poThing.AddRelationship(t);
                }
                else if (oldRef == null)
                {
                    t = SetPropertyValue(prop.Key, prop.Value);
                    poThing.AddRelationship(t);
                }
                else if (oldRef.RelationshipsFrom.Count < 2)
                {
                    if (IsPropertyTransient(prop.Key))
                        oldRef.V = prop.Value;
                    else
                        oldRef.Children[0].V = prop.Value;
                }
                else
                {
                    t = SetPropertyValue(prop.Key, prop.Value);
                    poThing.RemoveRelationship(oldRef);
                    poThing.AddRelationship(t);
                }
            }

            MentalModelChanged = true;
        }

        //TODO   Not really implemented
        public void DeleteProperties(Thing t, Dictionary<string, object> properties)
        {
            GetUKS();
            Thing propertiesRoot = UKS.GetOrAddThing("Property", "Thing");
            // might need to replace properties with transient or vice versa
            Thing transPropertiesRoot = UKS.GetOrAddThing("TransientProperty", "Property");

            foreach (KeyValuePair<string, object> prop in properties)
            {
                string propName = prop.Key;
                Thing propRoot = UKS.Labeled(propName, propertiesRoot.Children);
                if (propRoot != null)
                {
                    List<Relationship> linked = t.GetRelationshipsWithAncestor(propRoot);
                    foreach (Relationship l in t.GetRelationshipsWithAncestor(propRoot))
                    {
                        if (l.T is Thing lT)
                            if (properties.ContainsKey(t.Label))
                            {
                                t.RemoveRelationship(lT);
                            }
                    }
                }
            }
            foreach (KeyValuePair<string, object> prop in properties)
            {
                string propName = prop.Key;
                Thing propRoot = UKS.Labeled(propName, transPropertiesRoot.Children);
                if (propRoot != null)
                {
                    List<Relationship> linked = t.GetRelationshipsWithAncestor(propRoot);
                    foreach (Relationship l in t.GetRelationshipsWithAncestor(propRoot))
                    {
                        if (l.T is Thing lT)
                            if (properties.ContainsKey(t.Label))
                            {
                                t.RemoveRelationship(lT);
                            }
                    }
                }
            }
            MentalModelChanged = true;
        }

        public bool PropertyNear(object o1, object o2, object tolerance)
        {
            if (o1 is Point3DPlus p1 && o2 is Point3DPlus p2)
            {
                Point3DPlus tolerancePt = new();
                if (tolerance is float tf)
                {
                    tolerancePt.Theta = tf;
                    tolerancePt.Phi = tf;
                    tolerancePt.R = tf;
                }
                else if (tolerance is Point3DPlus p)
                {
                    tolerancePt = p;
                }
                if (Math.Abs(p1.Theta - p2.Theta) < tolerancePt.Theta &&
                    Math.Abs(p1.Phi - p2.Phi) < tolerancePt.Phi &&
                    Math.Abs(p1.R - p2.R) < tolerancePt.R
                    )
                    return true;
            }
            else if (o1 is HSLColor c1 && o2 is HSLColor c2)
            {
                if (tolerance is float t)
                {
                    if (Math.Abs(c1.hue - c2.hue) < t)
                        return true;
                }
            }
            else if (o1.ToString() == "Partial" || o2.ToString() == "Partial")
                return true;
            else if (o1.ToString() == o2.ToString())
                return true;
            return false;
        }

        public Thing SearchPhysicalObject(Dictionary<string, object> propertiesToSearch, Dictionary<string, object> tolerances = null, float tolerance = 3)
        {
            Thing retVal = null;
            GetUKS();
            if (UKS == null) return null;
            Thing mentalModelRoot = UKS.GetOrAddThing("MentalModel", "Thing");

            foreach (Thing t in mentalModelRoot.Children)
            {
                int matchingPropCount = 0;
                var thingProps = t.GetRelationshipsAsDictionary();
                foreach (var prop in propertiesToSearch)
                {
                    if (thingProps.ContainsKey(prop.Key))
                    {
                        if (prop.Value is HSLColor propColor)
                        {
                            bool colorMatch = false;
                            foreach (Thing colorVal in ((Thing)thingProps[prop.Key]).Children)
                            {
                                if (colorVal.V == prop.Value)
                                    colorMatch = true;
                            }
                            if (!colorMatch) break;
                            matchingPropCount++;
                        }
                        else if (prop.Value is Thing t1)
                        {
                            if (t1 != prop.Value) break;
                            matchingPropCount++;
                        }
                        else if (prop.Value is Point3DPlus p)
                        {
                            if (tolerances != null && tolerances.ContainsKey(prop.Key))
                            {
                                if (!PropertyNear(p, thingProps[prop.Key], tolerances[prop.Key])) break;
                            }
                            else
                            {
                                if (!PropertyNear(p, thingProps[prop.Key], tolerance)) break;
                            }
                            matchingPropCount++;
                        }
                    }
                }
                if (matchingPropCount == propertiesToSearch.Count)
                {
                    retVal = t;
                    break;
                }
            }

            return retVal;
        }

        //given a property name, is it transient
        private bool IsPropertyTransient(string propName)
        {
            Thing propertiesRoot = UKS.GetOrAddThing("Property", "Thing");
            Thing t = propertiesRoot.Children.FindFirst(x => x.Label == propName);
            return t == null;
        }
        private Thing GetPropertyRoot(string propName)
        {
            Thing propertiesRoot = UKS.GetOrAddThing("Property", "Thing");
            Thing t = propertiesRoot.Children.FindFirst(x => x.Label == propName);
            if (t != null) return t;
            t = UKS.GetOrAddThing(propName, "TransientProperty");
            return t;
        }
        public Thing GetExistingPropertyWhichHasValue(string propName, object propVal)
        {
            Thing propRoot = GetPropertyRoot(propName);
            if (propRoot == null)
                propRoot = UKS.GetOrAddThing(propName, "TransientProperty");
            foreach (Thing t in propRoot.Children)
            {
                if (propVal is Thing t1 && t == t1) return t;
                if (t.V is Point3DPlus p1 && propVal is Point3DPlus p2 && p1 == p2) return t;
                if (t.V is float f1 && propVal is float f2 && f1 == f2) return t;
                if (t.V is System.Windows.Media.Color c1 && propVal is System.Windows.Media.Color c2 && c1 == c2) return t;
            }
            return null;
        }
        private Thing SetPropertyValue(string propName, object propVal)
        {
            if (propName == "siz")
            { }
            Thing t = GetExistingPropertyWhichHasValue(propName, propVal);
            if (t != null && t.RelationshipsFrom.Count < 2)
            {
                //reuse the existing thing because it already exists & no one eles is using it
                if (propVal is not Thing t1)
                    t.V = propVal;
            }
            else if (t == null)
            {
                //create a new Thing
                Thing propRoot = GetPropertyRoot(propName);
                t = UKS.GetOrAddThing(propName + "*", propRoot);
                if (IsPropertyTransient(propName))
                    t.V = propVal;
                else
                    UKS.GetOrAddThing(propName + "*", t, propVal);
            }
            if (propName == "col" && t.Children.Count > 0 && !(t.Children[0].V is HSLColor x))
            { }
            return t;
        }
        public Thing AddPhysicalObject(Dictionary<string, object> properties)
        {
            GetUKS();
            if (UKS == null || properties == null) return null;
            Thing mentalModelRoot = UKS.GetOrAddThing("MentalModel", "Thing");
            Thing objectRoot = UKS.GetOrAddThing("Object", "Thing");
            Thing physicalObject = UKS.AddThing("po*", mentalModelRoot);
            foreach (KeyValuePair<string, object> prop in properties)
            {
                // add each property to the property tree
                // complicated by the fact that each non transient property type entry has children which are abstract with grandchildren with the value
                // complicated by the fact that each     transient property type entry has children with the value
                string propName = prop.Key;
                object propVal = prop.Value;
                Thing propNode = SetPropertyValue(propName, propVal);
                physicalObject.AddRelationship(propNode);
            }
            //check object decendants for shared references with new phys objects
            bool exact = true;
            if (exact)
                MatchPhysObjToObjectTree(objectRoot, physicalObject);
            else
                BestPhysObjToObjectTree(objectRoot, physicalObject);
            MentalModelChanged = true;
            return physicalObject;
        }


        private static void MatchPhysObjToObjectTree(Thing objectRoot, Thing physicalObject)
        {
            foreach (Thing t in objectRoot.Descendents)
            {
                if (t.Label == "ball")
                { }
                if (t.Relationships.Count != 0)
                {
                    int i = 0;
                    foreach (Relationship r in t.RelationshipsWithoutChildren)
                    {
                        if (r.weight < 0.7) continue;
                        if (physicalObject.RelationshipsAsThings.Contains(r.T))
                        {
                            i++;
                            if (i == t.RelationshipsWithoutChildren.FindAll(r => r.weight > 0.7f).Count)
                            {
                                //matches all Objects properties with weight > 0.8
                                //physicalObject.AddRelationship(r.source);
                                r.source.AddChild(physicalObject).inferred = true;
                            }
                        }
                    }
                }
            }
        }

        private static void BestPhysObjToObjectTree(Thing objectRoot, Thing physicalObject)
        {
            // if multiple references are wanted, remove return calls
            int j = 9;
            foreach (Thing t in objectRoot.Descendents)
            {
                if (t.Relationships.Count != 0)
                {
                    int i = 0;
                    if (t.Relationships.Count > 1)
                    {

                        foreach (Thing r in t.RelationshipsAsThings)
                        {
                            if (physicalObject.RelationshipsAsThings.Contains(r))
                            {
                                i++;
                                if (i == t.Relationships.Count || i == physicalObject.Relationships.Count - j)//phys object has 12 props, 9 are transient
                                {
                                    //matches all phys objects properties
                                    physicalObject.AddRelationship(t);
                                    return;
                                    j++;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (physicalObject.RelationshipsAsThings.Contains(t.RelationshipsAsThings[0]))
                        {
                            physicalObject.AddRelationship(t);
                            return;
                            j++;
                        }
                    }
                }
            }
        }

        public List<Thing> GetAllPhysicalObjects()
        {
            GetUKS();
            Thing mentalModelRoot = UKS.GetOrAddThing("MentalModel", "Thing");
            List<Thing> tList = new List<Thing>();
            if (mentalModelRoot is null) return tList;
            foreach (Thing t in mentalModelRoot.Children)
            {
                if (t != null && t.Label.StartsWith("po"))
                {
                    tList.Add(t);
                }
            }
            return tList;
        }

        public List<Thing> GetAllVisiblePhysicalObjects()
        {
            GetUKS();
            Thing mentalModelRoot = UKS.GetOrAddThing("MentalModel", "Thing");
            List<Thing> tList = new List<Thing>();
            if (mentalModelRoot is null) return tList;
            foreach (Thing t in mentalModelRoot.Children)
            {
                if (t != null)
                    if (t.Label.StartsWith("po"))
                        if (PhysObjectIsVisible(t))
                        {
                            tList.Add(t);
                        }
            }
            return tList;
        }

        public void DeletePhysicalObject(Thing t)
        {
            if (t == null) return;
            while (t.Relationships.Count > 0)
            {
                Relationship l = t.Relationships[0];
                if (l.T is Thing lT)
                {
                    t.RemoveRelationship(lT);
                    if (lT.RelationshipsFrom.Count == 0)
                    {
                        UKS.DeleteAllChildren(lT);
                        UKS.DeleteThing(lT);
                    }
                }
            }
            if (t.Relationships.Count == 0)
                UKS.DeleteThing(t);
            MentalModelChanged = true;
        }


        public bool PhysObjectIsVisible(Thing t)
        {
            //TODO, the camera angle system doesn't work yet.
            Angle cameraPan = 0;
            Angle cameraTilt = 0;
            ModulePodInterface thePod = (ModulePodInterface)FindModule("PodInterface");
            if (thePod != null)
            {
                cameraPan = thePod.GetCameraPan();
                cameraTilt = thePod.GetCameraTilt();
            }

            GetUKS();
            foreach (Relationship l in t.Relationships)
            {
                if (l.T is Thing lT)
                    if (lT.Label.StartsWith("vsb"))
                    {
                        string visibility = (string)lT.V;
                        if (visibility == "Visible")
                        {
                            return true;
                        }
                    }
            }
            return false;
        }

        // Visibility by angle instead of by visibility property
        // angle offset turns sallie's view
        // expandAngle expands the range of sallies view
        public bool ThingIsVisible(Thing t, float offsetAngle = 0, float expandAngle = 0)
        {
            Angle offsetFromDegrees = Angle.FromDegrees(offsetAngle);
            Angle expandFromDegrees = Angle.FromDegrees(expandAngle);
            Angle cameraPan = 0;
            Angle cameraTilt = 0;

            ModulePodInterface thePod = (ModulePodInterface)FindModule("PodInterface");
            if (thePod != null)
            {
                cameraPan = thePod.GetCameraPan();
                cameraTilt = thePod.GetCameraTilt();
            }

            GetUKS();

            // Debug.Write("visibleAngleH;");
            // Debug.Write("visibleAngleV;");
            // Debug.Write("aTheta;");
            // Debug.Write("aPhi;");
            // Debug.Write("aMinusTheta;");
            // Debug.Write("aPlusTheta;");
            // Debug.Write("aMinusPhi;");
            // Debug.Write("aPlusPhi;");
            // Debug.Write("minVisibleThetaExpanded;");
            // Debug.Write("plusVisibleThetaExpanded;");
            // Debug.Write("minVisiblePhiExpanded;");
            // Debug.WriteLine("plusVisiblePhiExpanded;");

            foreach (Relationship l in t.Relationships)
            {
                if ((l.T is not Thing centerThing) || (centerThing.V is not Point3DPlus centerPoint))
                    continue;

                // Collision objects dont have dimensions
                double width = 0, height = 0;
                if (t.GetRelationshipsAsDictionary().ContainsKey("wid") &&
                    t.GetRelationshipsAsDictionary().ContainsKey("hgt"))
                {
                    width = (float)t.GetRelationshipsAsDictionary()["wid"];
                    height = (float)t.GetRelationshipsAsDictionary()["hgt"];
                }

                Angle visibleAngleH = Angle.FromDegrees(UnknownArea.FieldOfVision / 2);
                Angle visibleAngleV = visibleAngleH / UnknownArea.AspectRatio;
                Angle aTheta = centerPoint.Theta - cameraPan + offsetFromDegrees;
                Angle aPhi = centerPoint.Phi - cameraTilt;

                // Debug.Write(visibleAngleH.ToString() + ";");
                // Debug.Write(visibleAngleV.ToString() + ";");
                // Debug.Write(aTheta.ToString() + ";");
                // Debug.Write(aPhi.ToString() + ";");

                Angle aMinusTheta = aTheta - width / centerPoint.R / 2;
                Angle aPlusTheta = aTheta + width / centerPoint.R / 2;
                Angle aMinusPhi = aPhi - height / centerPoint.R / 2;
                Angle aPlusPhi = aPhi + height / centerPoint.R / 2;

                // Debug.Write(aMinusTheta.ToString() + ";");
                // Debug.Write(aPlusTheta.ToString() + ";");
                // Debug.Write(aMinusPhi.ToString() + ";");
                // Debug.Write(aPlusPhi.ToString() + ";");

                Angle minVisibleThetaExpanded = -visibleAngleH - expandFromDegrees;
                Angle plusVisibleThetaExpanded = visibleAngleH + expandFromDegrees;
                Angle minVisiblePhiExpanded = -visibleAngleV - expandFromDegrees;
                Angle plusVisiblePhiExpanded = visibleAngleV + expandFromDegrees;

                // Debug.Write(minVisibleThetaExpanded.ToString() + ";");
                // Debug.Write(plusVisibleThetaExpanded.ToString() + ";");
                // Debug.Write(minVisiblePhiExpanded.ToString() + ";");
                // Debug.Write(plusVisiblePhiExpanded.ToString() + ";");

                if (aMinusTheta > minVisibleThetaExpanded &&
                    aPlusTheta < plusVisibleThetaExpanded &&
                    aMinusPhi > minVisiblePhiExpanded &&
                    aPlusPhi < plusVisiblePhiExpanded)
                {
                    // Debug.WriteLine("ThingIsVisible() returning true");
                    return true;
                }
            }
            // Debug.WriteLine("ThingIsVisible() returning false");
            return false;
        }

        public void Prune()
        {

        }

        public void GetObjectsByLocation()
        {
            GetUKS();
        }
        public void GetLocationsByObjectType()
        {
            GetUKS();
        }

        public override void SetUpBeforeSave()
        {
            //SerializeKeys = HiddenValues.Keys.ToList();
            //SerializeValues = HiddenValues.Values.ToList();
        }

        public override void SetUpAfterLoad()
        {
            MentalModelChanged = true; //forces a repaint
        }

        //called whenever the size of the module rectangle changes
        //for example, you may choose to reinitialize whenever size changes
        //delete if not needed
        public override void SizeChanged()
        {
            if (mv == null) return; //this is called the first time before the module actually exists
            MentalModelChanged = true;
        }

        public override void UKSInitializedNotification()
        {
            MainWindow.SuspendEngine();
            GetUKS();

            MainWindow.ResumeEngine();
            MentalModelChanged = true;
        }

        public override void UKSReloadedNotification()
        {
            MentalModelChanged = true;
            UpdateDialog();
        }


        //Drawing Stuff
        public void UpdateMentalObjects()
        {
            ModuleMentalModelDlg dialog = (ModuleMentalModelDlg)dlg;
            endUIObjects = new();

            dialog.modelProperties.Clear();
            AddRedArrowMarker(endUIObjects);
            dialog.DrawObjects(endUIObjects);
            BuildObjectList(endUIObjects);
            dialog.DrawObjects(endUIObjects);

            ModuleUserInterface UI = (ModuleUserInterface)FindModule(typeof(ModuleUserInterface));
            if (UI != null)
                UI.UpdateEnvironment(endUIObjects);

            ModuleGraphics mg = (ModuleGraphics)FindModule("Graphics");
            mg.RedrawImagination();
        }
        public void AddMMDlgItems(List<ModelUIElement3D> items)
        {
            ModuleMentalModelDlg dialog = (ModuleMentalModelDlg)dlg;
            dialog.AddObjects(items);
        }
        public void DeleteMMDlgItems(List<ModelUIElement3D> items)
        {
            ModuleMentalModelDlg dialog = (ModuleMentalModelDlg)dlg;
            dialog.DeleteObjects(items);
        }
        private void AddRedArrowMarker(List<ModelUIElement3D> endUIObjects)
        {
            Constructor3D construct = new();
            endUIObjects.Add(construct.Cube(new Cube() { size = 1.0f, color = "Red", position = new Point3DPlus(0, 0, .5F) }));
            endUIObjects.Add(construct.Cube(new Cube() { size = 1.0f, color = "Red", position = new Point3DPlus(0, 1, .5F) }));
            endUIObjects.Add(construct.Cube(new Cube() { size = 1.0f, color = "Red", position = new Point3DPlus(0, 2, .5F) }));
            endUIObjects.Add(construct.Cube(new Cube() { size = 1.0f, color = "Red", position = new Point3DPlus(0, 3, .5F) }));
            endUIObjects.Add(construct.Cube(new Cube() { size = 1.0f, color = "Red", position = new Point3DPlus(0, 4, .5F) }));
            endUIObjects.Add(construct.Cube(new Cube() { size = 1.0f, color = "Red", position = new Point3DPlus(0, 5, .5F) }));
            endUIObjects.Add(construct.Cube(new Cube() { size = 1.0f, color = "Red", position = new Point3DPlus(.5f, 4, .5F) }));
            endUIObjects.Add(construct.Cube(new Cube() { size = 1.0f, color = "Red", position = new Point3DPlus(-.5f, 4, .5F) }));
            endUIObjects.Add(construct.Cube(new Cube() { size = 1.0f, color = "Red", position = new Point3DPlus(1, 3.5f, .5F) }));
            endUIObjects.Add(construct.Cube(new Cube() { size = 1.0f, color = "Red", position = new Point3DPlus(-1, 3.5f, .5F) }));
            endUIObjects.Add(construct.Cube(new Cube() { size = 1.0f, color = "Red", position = new Point3DPlus(1.5f, 3, .5F) }));
            endUIObjects.Add(construct.Cube(new Cube() { size = 1.0f, color = "Red", position = new Point3DPlus(-1.5f, 3, .5F) }));
        }

        private void BuildObjectList(List<ModelUIElement3D> endUIObjects)
        {
            Constructor3D construct = new();
            List<Thing> objectList = GetAllPhysicalObjects();
            foreach (Thing t in objectList)
            {
                PhysicalObject po = new PhysicalObject(t);
                if (po.center == null)
                {
                    continue;
                }
                // po.CompensatePointsFor3DModel();

                UnknownArea areaToDraw = (UnknownArea)t.V;
                if (areaToDraw == null)
                {
                    //DrawObjectFrom3DModel(endUIObjects, po);
                    continue;
                }

                if (areaToDraw.AreaSegments.Count < 1)
                    continue;

                Dictionary<string, object> properties = t.GetRelationshipsAsDictionary();
                if (!properties.ContainsKey("cen"))
                    continue;

                //center is the centroid of the object relative to the eye-line from whitch it is seen
                //this is the point from which the triangle fan is drawn
                Point3DPlus pcen = new Point3DPlus((Point3DPlus)properties["cen"]);
                pcen.Z += 5; //compensate for eye-level offset

                //center is in the refrence frame of the display coordinates
                Point3DPlus center = new Point3DPlus(pcen);
                center.Theta = center.Theta + Angle.FromDegrees(90);

                HSLColor c = (HSLColor)properties["col"];
                System.Windows.Media.Color objectColor = areaToDraw.AvgColor.ToColor();

                AddObjectBaseMarker(endUIObjects, center);

                if (!properties.ContainsKey("siz")) continue;
                double size = (float)properties["siz"] / 33f;

                List<Point3D> points = new List<Point3D>();
                CreateTriangleSegments(t, pcen, center, size, objectColor, endUIObjects);
            }
        }

        private void AddObjectBaseMarker(List<ModelUIElement3D> endUIObjects, Point3DPlus center)
        {
            Constructor3D construct = new();
            Point3DPlus markerCenter = new(center);
            markerCenter.Z = 1;
            endUIObjects.Add(construct.Cube(new Cube() { size = 1.0f, color = "Red", position = markerCenter }));
        }
        private void CreateTriangleSegments(Thing thingToDraw, Point3DPlus pcen, Point3DPlus center, double size, System.Windows.Media.Color objectColor,
                                        List<ModelUIElement3D> endUIObjects)
        {
            Point3DPlus curPt = null;
            Point3DPlus prevPt = null;
            Point3DPlus firstPt = null;
            UnknownArea areaToDraw = thingToDraw.V as UnknownArea;
            int cornersToDraw = Min(areaToDraw.AreaCorners.Count, maxCornersPerObject);
            if (areaToDraw.AreaCorners.Count < 3) return;
            StartMesh();

            for (int i = 0; i < areaToDraw.AreaCorners.Count; i++)
            {
                CornerTwoD curCorner = areaToDraw.AreaCorners[i];

                curPt = areaToDraw.GetPoint3DPlusFromPixel(new System.Windows.Point(-curCorner.loc.X, curCorner.loc.Y));
                if (prevPt == null)
                {
                    firstPt = curPt;
                }
                else
                {
                    CreateTriangle(pcen, center, curPt, prevPt, size);
                }

                prevPt = curPt;
            }
            CreateTriangle(pcen, center, firstPt, curPt, size);

            EndMesh(objectColor, AssembleTooltip(thingToDraw));
        }


        private void DrawObjectFrom3DModel(List<ModelUIElement3D> endUIObjects, PhysicalObject po)
        {
            Constructor3D construct = new();
            endUIObjects.Add(construct.Cube(new Cube() { size = 1.0f, color = po.color, position = po.center.Clone() }));

            if (po.topleft != null && po.topright != null && po.bottomleft != null && po.bottomright != null)
            {
                endUIObjects.Add(construct.Cube(new Cube() { size = 1.0f, color = po.color, position = po.topleft.Clone() }));
                endUIObjects.Add(construct.Cube(new Cube() { size = 1.0f, color = po.color, position = po.topright.Clone() }));
                endUIObjects.Add(construct.Cube(new Cube() { size = 1.0f, color = po.color, position = po.bottomleft.Clone() }));
                endUIObjects.Add(construct.Cube(new Cube() { size = 1.0f, color = po.color, position = po.bottomright.Clone() }));
                Interpolate(construct, po.topright, po.topleft, po.color, endUIObjects);
                Interpolate(construct, po.topleft, po.bottomleft, po.color, endUIObjects);
                Interpolate(construct, po.bottomleft, po.bottomright, po.color, endUIObjects);
                Interpolate(construct, po.bottomright, po.topright, po.color, endUIObjects);
            }
        }

        private void CreateTriangle(Point3DPlus pcen,
                                            Point3DPlus center,
                                            Point3DPlus curPt,
                                            Point3DPlus prevPt,
                                            double size)
        {
            Point3DPlus p2 = new Point3DPlus(pcen);
            p2.Y += (float)(curPt.Y * size);
            p2.Z += (float)(curPt.Z * size);
            p2.R = pcen.R;
            p2.Theta += Angle.FromDegrees(90);

            Point3DPlus p1 = new Point3DPlus(pcen);
            p1.Y += (float)(prevPt.Y * size);
            p1.Z += (float)(prevPt.Z * size);
            p1.R = pcen.R;
            p1.Theta += Angle.FromDegrees(90);

            AddTriangle(center, p1, p2);
        }

        public void Interpolate(Constructor3D construct, Point3DPlus from, Point3DPlus to, string objectcolor, List<ModelUIElement3D> endUIObjects)
        {
            Point3DPlus step = to - from;
            int count = (int)step.R;
            step.R = 1;
            Point3DPlus marker = from + step;
            for (int index = 1; index <= count; index++)
            {
                endUIObjects.Add(construct.Cube(new Cube() { size = 1.0f, color = objectcolor, position = marker }));
                marker += step;
            }
        }

        string AssembleTooltip(Thing t)
        {
            Dictionary<string, object> properties = t.GetRelationshipsAsDictionary(); //contains position in mental model in the UKS
            string retVal = "Instance: " + t.Label;
            Thing tParent = t.Parents.FindFirst(x => x.HasAncestorLabeled("Object"));
            retVal += "   Object Type: ";
            if (tParent != null)
                retVal += tParent.Label;
            else
                retVal += "?????";
            foreach (var prop in properties)
            {
                retVal += "\n" + prop.Key + ": ";
                if (prop.Value != null)
                    retVal += prop.Value.ToString();
            }
            GetUKS();
            Thing objectType = t.GetRelationshipWithAncestor(UKS.Labeled("Object"));
            if (objectType != null)
            {
                retVal += "\nObject: " + objectType.Label;
            }
            return retVal;
        }

        //TODO make objects have 3D models
        //TODO make the object face in the correct direction
        //TODO make the object display the corrrect face

        public void Draw2DBoundaryThing(Thing t)
        {

        }

        MeshBuilder meshBuilder = null;
        List<ModelUIElement3D> endUIObjects = null;
        private void StartMesh()
        {
            meshBuilder = new();
        }
        private void EndMesh(System.Windows.Media.Color c, string tooltip)
        {
            var element = new ModelUIElement3D();
            var geometry = new GeometryModel3D();
            var theBrush = new SolidColorBrush(c);
            geometry.Material = new DiffuseMaterial(theBrush);
            geometry.Geometry = meshBuilder.ToMesh();
            element.Model = geometry;
            endUIObjects.Add(element);

            ModuleMentalModelDlg dialog = (ModuleMentalModelDlg)dlg;
            dialog.modelProperties.Add(element, tooltip);// AssembleTooltip(t));
        }


        public void AddTriangle(Point3DPlus p1, Point3DPlus p2, Point3DPlus p3)
        {
            //draw the triangle in two faces so it will show regardless of orientation
            meshBuilder.AddTriangle(p1.P, p2.P, p3.P);
            meshBuilder.AddTriangle(p2.P, p1.P, p3.P);
        }

        public ModelUIElement3D Polygon(IList<Point3D> points, System.Windows.Media.Color c)
        {
            var element = new ModelUIElement3D();
            var geometry = new GeometryModel3D();
            var meshBuilder = new MeshBuilder();
            meshBuilder.AddPolygon(points);
            //            meshBuilder.AddTriangle(p1.P, p2.P, p3.P);
            geometry.Geometry = meshBuilder.ToMesh();
            //geometry.Material = new SpecularMaterial(new SolidColorBrush(c), 0.0);
            geometry.Material = new DiffuseMaterial(new SolidColorBrush(c));
            element.Model = geometry;
            return element;
        }

    }
}
