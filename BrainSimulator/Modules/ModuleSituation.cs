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
using System.Windows.Threading;
using System.Windows.Media.Media3D;

namespace BrainSimulator.Modules
{
    public class ModuleSituation : ModuleBase
    {
        public ModuleSituation()
        {
            minHeight = 1;
            maxHeight = 500;
            minWidth = 1;
            maxWidth = 500;
        }

        //mental model in UKS
        Thing mentalModel = null;
        //Situation under Behavior in UKS
        Thing situationRoot = null;

        //current landmark in view
        [XmlIgnore]
        public Thing curClosestLM = null;
        //the landmark to save a path towards
        [XmlIgnore]
        public Thing toLandmark = null;
        //contains all LMs that have been explored
        List<Thing> explored = new();

        //true if looking for paths between landmarks
        [XmlIgnore]
        public bool findLandmarks = false;

        //true if looking around for objects
        [XmlIgnore]
        public bool findingPOs = false;
        //true if already looked around
        bool foundPOs = false;
        //amount turned so far while looking around for objects
        int exploreTurned = 0;
        //how much to turn each fire while looking around for objects
        const int exploreTurnIncrement = 20;
        //time we last turned so we know how long to wait for image recognition to update the mental model
        DateTime lastLookTurn = DateTime.MinValue;
        //time in milliseconds to wait between turns while image recognition works
        const int lookWaitTime = 4000;

        //true if we were already exploring in the previous fire
        bool prevExploreMode = false;
        //how confident we need to be about the position of an object
        const float positionAccuracy = 0.009f;
        //true if just turned to find objects
        bool newLook = false;

        //things in view that need to be ranged
        List<Thing> needRanging = new();

        //maximum distance between two objects in a landmark
        const float maxGroupDist = 25;

        //the group we are located at while searching for landmarks
        [XmlIgnore]
        public List<Thing> currentExploringGroup = null;

        //current visible groups
        [XmlIgnore]
        public List<List<Thing>> groups = new();

        //true when input came from speech
        [XmlIgnore]
        public bool speechResponseNeeded = false;

        //the max distance an object can be seen from when creating landmarks
        float findVisionDistance = float.MaxValue;
        //the max distance a landmark can be seen from when navigating
        [XmlIgnore]
        public float navigateVisionDistance = 100;

        [XmlIgnore]
        public int minItemsInLandmark = 2;
        int maxItemsInLandmark = 10;

        //minimum percentage of matching objects for two situations to be considered the same
        //1 means any difference is counted
        double minMatchObjects = 1;
        //minimum ratio between sums of all distances between objects in two situations to be equal situations
        double minMatchDistances = .9; //positions not entirely accurate in UKS due to rounding errors

        double sallieLastMoved = Utils.GetPreciseTime() - 100;

        public override void Initialize()
        {
            Init();
        }

        public override void UKSInitializedNotification()
        {
            MainWindow.SuspendEngine();
            GetUKS();
            explored.Clear();
            MainWindow.ResumeEngine();
        }

        public override void Fire()
        {
            GetUKS();
            if (UKS is null) return;
            mentalModel = UKS.GetOrAddThing("MentalModel", "Thing");
            if (mentalModel is null) return;
            situationRoot = UKS.GetOrAddThing("Situation", "Behavior");
            if (situationRoot is null) return;
            Init();

            ModuleSituationNavigation situationNavigation = (ModuleSituationNavigation)FindModule(typeof(ModuleSituationNavigation));
            if (situationNavigation == null) return;
            ModulePodInterface podInterface = (ModulePodInterface)FindModule(typeof(ModulePodInterface));
            ModuleMentalModel moduleMentalModel = (ModuleMentalModel)FindModule(typeof(ModuleMentalModel));

            Thing exploreModeThing = UKS.Labeled("FindLandmarks", UKS.GetOrAddThing("Attention", "Thing").Children);
            if (exploreModeThing == null)
            {
                currentExploringGroup = null;
                situationNavigation.movingToCur = false;
                findingPOs = false;
                foundPOs = false;
                exploreTurned = 0;
                newLook = false;
            }
            if (curClosestLM != null) curClosestLM.SetFired();

            //always let Sallie finish moving before evaluating the situation or commanding a new movement
            if (Sallie.lastMoved > sallieLastMoved)
            {
                sallieLastMoved = Sallie.lastMoved;
                return;
            }

            Thing rangeUKS = UKS.Labeled("Range", UKS.GetOrAddThing("Attention", "Thing").Children);
            Thing rangeThingUKS = UKS.Labeled("RangeThing", UKS.GetOrAddThing("Attention", "Thing").Children);
            if (rangeUKS != null || rangeThingUKS != null) return;

            ModuleIntegratedVision iVision = (ModuleIntegratedVision)FindModule("IntegratedVision");
            List<Thing> POs = moduleMentalModel.GetAllPhysicalObjects();
            if (needRanging.Count > 0)
            {
                //ranging only works with image recognition
                //also make sure the object has not been deleted already
                if (iVision != null && iVision.isEnabled && POs.Contains(needRanging[0]))
                {
                    Thing attentionThing = UKS.GetOrAddThing("Attention", "Thing");
                    Thing rangeThing = UKS.GetOrAddThing("RangeThing", attentionThing);
                    rangeThing.AddRelationship(needRanging[0]);
                    //UKS.GetOrAddThing("Range", "Attention"); alternate ranging method
                }
                needRanging.RemoveAt(0);
                //needRanging.Clear();
                return;
            }

            //wait for image recognition to add visible objects to mental model
            if ((DateTime.Now - lastLookTurn) < TimeSpan.FromMilliseconds(lookWaitTime)) return;

            //range objects if unsure about their distance
            if (newLook)
            {
                newLook = false;
                ModuleRangeThing rangeThingModule = (ModuleRangeThing)FindModule(typeof(ModuleRangeThing));
                if (rangeThingModule == null || rangeThingModule.IsEnabled() == false) return;
                foreach (Thing PO in POs)
                {
                    Point3DPlus center = (Point3DPlus)PO.GetRelationshipsAsDictionary()["cen"];
                    //if (moduleMentalModel.PhysObjectIsVisible(PO) && center.Conf < positionAccuracy)
                    if (moduleMentalModel.ThingIsVisible(PO) && center.Conf < positionAccuracy)
                    {
                        needRanging.Add(PO);
                    }
                }
                if (needRanging.Count > 0) return;
            }

            //update mental model by looking around
            if (findingPOs)
            {
                LookAround();
                newLook = true;
                return;
            }

            //move to current landmark
            if (situationNavigation.movingToCur)
            {
                if (foundPOs == false)
                {
                    findingPOs = true;
                    return;
                }
                situationNavigation.MoveToCurrentGroup();
                situationNavigation.movingToCur = false;
                findingPOs = true;
                foundPOs = false;
                return;
            }

            IList<Thing> physicalObjects = UKS.GetOrAddThing("MentalModel", "Thing").Children;
            physicalObjects = physicalObjects.OrderBy(x => ((Point3DPlus)x.GetRelationshipsAsDictionary()["cen"]).Theta.Degrees).ToList();

            //remove objects too far away
            for (int i = 0; i < physicalObjects.Count; i++)
            {
                try
                {
                    Thing po = physicalObjects[i];
                    if (((Point3DPlus)po.GetRelationshipsAsDictionary()["cen"]).R > findVisionDistance)
                    {
                        physicalObjects.Remove(po);
                    }
                }
                catch { }
            }

            FindGroups(physicalObjects);
            CreateUKSGroups();

            //create landmarks out of groups and save paths from current landmark to all the found ones if they are in range
            foreach (List<Thing> group in groups)
            {
                CompareLandmarks(group);
                if (currentExploringGroup != null)
                    situationNavigation.SaveNaviEvent(currentExploringGroup, group);
            }

            //find the group closest to Sallie
            groups = groups.OrderBy(x => FindCentroid(x).R).ToList();

            Thing currentLandmark = UKS.GetOrAddThing("CurrentLandmark", "Situation");
            UKS.DeleteAllChildren(currentLandmark);
            if (groups.Count == 0 || FindCentroid(groups[0]).R > navigateVisionDistance)
            {
                curClosestLM = null;
            }
            else
            {
                curClosestLM = CompareLandmarks(groups[0]);
                UKS.DeleteAllChildren(currentLandmark);
                foreach (Thing po in groups[0])
                {
                    Thing LMCM = UKS.GetOrAddThing("LMCM*", currentLandmark);
                    foreach (Thing property in po.RelationshipsAsThings)
                    {
                        if (property.Label.StartsWith("shp") || property.Label.StartsWith("col"))
                        {
                            UKS.GetOrAddThing(property.Label, LMCM);
                        }
                        else if (property.Label.StartsWith("cen"))
                        {
                            Thing newProp = UKS.GetOrAddThing("cen", LMCM);
                            newProp.V = new Point3DPlus((Point3DPlus)property.V);
                        }
                    }
                }
                //add centroid and orientation
                Thing centroid = new();
                centroid.Label = "Centroid";
                centroid.V = FindCentroid(currentLandmark);
                currentLandmark.AddChild(centroid);
            }

            Thing goToLMTHing = UKS.Labeled("GoToLandmark", UKS.GetOrAddThing("Attention", "Thing").Children);

            if (goToLMTHing != null)
            {
                situationNavigation.GoToLandmark();
                UKS.DeleteThing(goToLMTHing);
            }

            if (speechResponseNeeded)
            {
                situationNavigation.SpeechOutErrorMessage();
                speechResponseNeeded = false;
            }

            situationNavigation.UpdateErrorMsg();

            if (situationNavigation.navigatingToLM == true)
            {
                situationNavigation.NavigateToLandmark();
                situationNavigation.navigatingToLM = false;
                return;
            }
            ///////////////////////////////////////////////////////////

            // Explore Mode

            ///////////////////////////////////////////////////////////
            if (exploreModeThing == null)
            {
                prevExploreMode = false;
                return;
            }

            if (prevExploreMode == false)
            {
                findingPOs = true;
                prevExploreMode = true;
                return;
            }
            prevExploreMode = true;

            //set currentExploringLM
            Thing currentExploringLM;
            if (currentExploringGroup == null) currentExploringLM = null;
            else currentExploringLM = CompareLandmarks(currentExploringGroup);

            currentExploringGroup = null;
            foreach (List<Thing> group in groups)
            {
                Thing landmark = CompareLandmarks(group);
                if (explored.Contains(landmark) == false)
                {
                    currentExploringGroup = group;
                    explored.Add(landmark);
                    break;
                }
            }

            if (currentExploringGroup == null)
                UKS.DeleteThing(exploreModeThing);
            else
                situationNavigation.movingToCur = true;
            ///////////////////////////////////////////////////////////
        }

        //spin to update the mental model
        private void LookAround()
        {
            ModulePodInterface podInterface = (ModulePodInterface)FindModule(typeof(ModulePodInterface));

            podInterface.CommandTurn(Angle.FromDegrees(exploreTurnIncrement));
            exploreTurned += exploreTurnIncrement;
            lastLookTurn = DateTime.Now;
            if (exploreTurned >= 360)
            {
                exploreTurned = 0;
                findingPOs = false;
                foundPOs = true;
            }
        }

        //sort objects into groups based on location
        private void FindGroups(IList<Thing> physicalObjects)
        {
            groups.Clear();
            while (physicalObjects.Count > 0)
            {
                Thing po1, po2;
                float closestDist = closestObjects(physicalObjects, out po1, out po2);
                if (closestDist > maxGroupDist) break;
                float maxDistance = closestDist * 1.5f;
                if (maxDistance > maxGroupDist) maxDistance = maxGroupDist;

                List<Thing> group = new();
                group.Add(po1);
                physicalObjects.Remove(po1);
                group.Add(po2);
                physicalObjects.Remove(po2);
                
                bool addedPO = true;
                while (addedPO == true)
                {
                    addedPO = false;
                    for (int i = 0; i < physicalObjects.Count; i++)
                    {
                        Thing po = physicalObjects[i];
                        foreach (Thing groupPO in group)
                        {
                            if (distBetween(po, groupPO) < maxDistance)
                            {
                                group.Add(po);
                                physicalObjects.Remove(po);
                                addedPO = true;
                                break;
                            }
                        }
                    }
                }
                groups.Add(group);
            }
        }

        //add found groups to the UKS
        private void CreateUKSGroups()
        {
            Thing UKSGroups = UKS.GetOrAddThing("Groups", "TransientProperty");
            //remove from the UKS groups that are no longer in view
            foreach (Thing UKSGroup in UKSGroups.Children)
            {
                bool groupFound = false;
                foreach (List<Thing> group in groups)
                {
                    if (group.Count != UKSGroup.RelationshipsFrom.Count) continue;
                    bool allObjectsFound = true;
                    foreach (Relationship reference in UKSGroup.RelationshipsFrom)
                    {
                        Thing po = (reference.T as Thing);
                        if (group.Contains(po) == false)
                        {
                            allObjectsFound = false;
                            break;
                        }
                    }
                    if (allObjectsFound == false) continue;
                    groupFound = true;
                    break;
                }
                if (groupFound == false)
                {
                    UKS.DeleteThing(UKSGroup);
                }
            }

            //add new groups to the UKS
            foreach (List<Thing> group in groups)
            {
                bool groupFound = false;
                foreach (Thing UKSGroup in UKSGroups.Children)
                {
                    if (group.Count != UKSGroup.RelationshipsFrom.Count) continue;
                    bool allObjectsFound = true;
                    foreach (Relationship reference in UKSGroup.RelationshipsFrom)
                    {
                        Thing po = (reference.T as Thing);
                        if (group.Contains(po) == false)
                        {
                            allObjectsFound = false;
                            break;
                        }
                    }
                    if (allObjectsFound == false) continue;
                    groupFound = true;
                    break;
                }
                if (groupFound == false)
                {
                    Thing newUKSGroup = UKS.AddThing("Group*", UKSGroups);
                    foreach (Thing po in group)
                    {
                        po.AddRelationship(newUKSGroup);
                    }
                }
            }
        }

        //find the two closest objects in a list and the distance between them
        private float closestObjects(IList<Thing> physicalObjects, out Thing closest1, out Thing closest2)
        {
            float minDist = float.MaxValue;
            closest1 = null;
            closest2 = null;
            foreach (Thing po1 in physicalObjects)
                foreach (Thing po2 in physicalObjects)
                {
                    if (po1 == po2) continue;
                    if (distBetween(po1, po2) < minDist)
                    {
                        minDist = distBetween(po1, po2);
                        closest1 = po1;
                        closest2 = po2;
                    }
                }
            return minDist;
        }

        //returns the distance between two physical objects
        private float distBetween(Thing po1, Thing po2)
        {
            try
            {
                Point3DPlus center1 = (Point3DPlus)po1.GetRelationshipsAsDictionary()["cen"];
                Point3DPlus center2 = (Point3DPlus)po2.GetRelationshipsAsDictionary()["cen"];

                if (center1 == null || center2 == null) return float.MaxValue;

                return (float)Math.Sqrt(Math.Pow(center2.P.X - center1.P.X, 2) + Math.Pow(center2.P.Y - center1.P.Y, 2) + Math.Pow(center2.P.Z - center1.P.Z, 2));
            }
            catch 
            {
                return float.MaxValue;
            }
        }

        //returns the center of the group of objects
        public static Point3DPlus FindCentroid(Thing landmark)
        {
            Point3DPlus centroid = new Point3DPlus();
            int objectCount = 0;
            foreach (Thing LMCM in landmark.Children)
            {
                if (LMCM.Label.StartsWith("LMCM") == false) continue;
                Point3DPlus lmcmCenter = null;
                foreach (Thing property in LMCM.Children)
                    if (property.Label.StartsWith("cen"))
                        lmcmCenter = (Point3DPlus)property.V;
                //centroid.X is a float but centroid.P.X is a double
                if (lmcmCenter != null)
                {
                    centroid.P = new(centroid.P.X + lmcmCenter.P.X, centroid.P.Y + lmcmCenter.P.Y, centroid.P.Z + lmcmCenter.P.Z);
                    objectCount++;
                }
            }

            centroid.P = new(centroid.P.X / objectCount, centroid.P.Y / objectCount, centroid.P.Z / objectCount);
            return centroid;
        }

        //returns the center of the group of objects
        public static Point3DPlus FindCentroid(List<Thing> group)
        {
            Point3DPlus centroid = new Point3DPlus();
            int groupCount = 0;
            foreach (Thing po in group)
            {
                Point3DPlus center = null;
                foreach (Thing property in po.RelationshipsAsThings)
                    if (property.Label.StartsWith("cen"))
                        center = (Point3DPlus)property.V;

                if (center != null)
                {
                    centroid.P = new(centroid.P.X + center.P.X, centroid.P.Y + center.P.Y, centroid.P.Z + center.P.Z);
                    groupCount++;
                }
            }

            centroid.P = new(centroid.P.X / groupCount, centroid.P.Y / groupCount, centroid.P.Z / groupCount);
            return centroid;
        }

        //the orientation of a landmark is arbitrary but remains constant no matter the view of a given landmark
        //the orientation is the direction from the centroid to the farthest object in the landmark
        public static Vector3D FindOrientation(Thing landmark)
        {
            Point3DPlus centroid = FindCentroid(landmark);

            List<Thing> farthestThings = new();
            float farthestDist = 0;

            foreach (Thing LMCM in landmark.Children)
            {
                if (LMCM.Label.StartsWith("LMCM") == false) continue;

                Point3DPlus fromCentroid = null;
                foreach (Thing property in LMCM.Children)
                    if (property.Label.StartsWith("cen"))
                        fromCentroid = (Point3DPlus)property.V;
                fromCentroid.P = new(fromCentroid.P.X - centroid.P.X, fromCentroid.P.Y - centroid.P.Y, fromCentroid.P.Z - centroid.P.Z);
                if (Math.Round(fromCentroid.R, 3) > Math.Round(farthestDist, 3) || farthestThings.Count == 0)
                {
                    farthestThings.Clear();
                    farthestThings.Add(LMCM);
                    farthestDist = fromCentroid.R;
                }
                else if (Math.Round(fromCentroid.R, 3) == Math.Round(farthestDist, 3))
                {
                    farthestThings.Add(LMCM);
                }
            }

            Point3DPlus farthestThingCenter = null;

            if (farthestThings.Count == 0) return new Vector3D(0, 0, 0);

            if (farthestThings.Count == 1)
            {
                foreach (Thing property in farthestThings[0].Children)
                    if (property.Label.StartsWith("cen"))
                        farthestThingCenter = (Point3DPlus)property.V;
                return new Vector3D(farthestThingCenter.P.X - centroid.P.X, farthestThingCenter.P.Y - centroid.P.Y, farthestThingCenter.P.Z - centroid.P.Z);
            }

            //if multiple things are at the same distance use shape as a tiebreaker
            List<Thing> farthestThingsByShape = new();
            Dictionary<Thing, string> thingShape = new();
            foreach (Thing t in farthestThings)
            {
                foreach (Thing property in t.Children)
                {
                    if (property.Label.StartsWith("shp"))
                    {
                        thingShape.Add(t, property.Label);
                        break;
                    }
                }
            }

            farthestThings = farthestThings.OrderBy(x => thingShape[x]).ToList();

            string firstShape = thingShape[farthestThings[0]];
            foreach (Thing LMCM in farthestThings)
            {
                if (thingShape[LMCM] == firstShape)
                    farthestThingsByShape.Add(LMCM);
            }

            if (farthestThingsByShape.Count == 1)
            {
                farthestThingCenter = null;
                foreach (Thing property in farthestThingsByShape[0].Children)
                    if (property.Label.StartsWith("cen"))
                        farthestThingCenter = (Point3DPlus)property.V;
                return new Vector3D(farthestThingCenter.P.X - centroid.P.X, farthestThingCenter.P.Y - centroid.P.Y, farthestThingCenter.P.Z - centroid.P.Z);
            }

            //if multiple things have the same distance and shape use color as a tiebreaker
            List<Thing> farthestThingsByColor = new();
            Dictionary<Thing, string> thingColor = new();

            foreach (Thing t in farthestThingsByShape)
            {
                foreach (Thing property in t.Children)
                {
                    if (property.Label.StartsWith("col"))
                    {
                        thingColor.Add(t, property.Label);
                        break;
                    }
                }
            }

            farthestThingsByShape = farthestThingsByShape.OrderBy(x => thingColor[x]).ToList();

            string firstColor = thingColor[farthestThingsByShape[0]];
            foreach (Thing LMCM in farthestThingsByShape)
            {
                if (thingColor[LMCM] == firstColor)
                    farthestThingsByColor.Add(LMCM);
            }

            //if multiple things have the same distance, shape, and color Sallie will just guess that the first one is the correct one and may get lost
            //to be fair, we would probably also get confused if we saw two of the exact same thing
            farthestThingCenter = null;
            foreach (Thing property in farthestThingsByColor[0].Children)
                if (property.Label.StartsWith("cen"))
                    farthestThingCenter = (Point3DPlus)property.V;
            return new Vector3D(farthestThingCenter.P.X - centroid.P.X, farthestThingCenter.P.Y - centroid.P.Y, farthestThingCenter.P.Z - centroid.P.Z);
        }

        //return the distance from the centroid to the farthest object
        public static float FindRadius(Thing landmark)
        {
            Point3DPlus centroid = FindCentroid(landmark);

            float farthest = -1;
            foreach (Thing lmcm in landmark.Children)
            {
                if (lmcm.Label.StartsWith("LMCM") == false) continue;
                Point3DPlus curCenter = null;
                foreach (Thing property in lmcm.Children)
                    if (property.Label.StartsWith("cen"))
                        curCenter = (Point3DPlus)property.V;
                float curDistance = (float)Math.Sqrt(Math.Pow(centroid.X - curCenter.X, 2) + Math.Pow(centroid.Y - curCenter.Y, 2) + Math.Pow(centroid.Z - curCenter.Z, 2));
                if (curDistance > farthest)
                {
                    farthest = curDistance;
                }
            }

            return farthest;
        }

        //return the distance from the centroid to the farthest object
        public static float FindRadius(List<Thing> group)
        {
            Point3DPlus centroid = FindCentroid(group);

            float farthest = -1;
            foreach (Thing po in group)
            {
                Point3DPlus curCenter = null;
                foreach (Thing property in po.RelationshipsAsThings)
                    if (property.Label.StartsWith("cen"))
                        curCenter = (Point3DPlus)property.V;
                float curDistance = (float)Math.Sqrt(Math.Pow(centroid.X - curCenter.X, 2) + Math.Pow(centroid.Y - curCenter.Y, 2) + Math.Pow(centroid.Z - curCenter.Z, 2));
                if (curDistance > farthest)
                {
                    farthest = curDistance;
                }
            }

            return farthest;
        }

        //return a new landmark thing built from objects in currentGroup
        private Thing NewLandmark(IList<Thing> currentGroup)
        {
            ModuleSituationNavigation situationNavigation = (ModuleSituationNavigation)FindModule("SituationNavigation");

            //ignore partials
            //int fullObjects = 0;
            //int j = maxItemsInLandmark;
            //for (int i = 0; i < currentVisibleMentalModel.Count && i < j; i++)
            //{
            //    Thing currentThing = currentVisibleMentalModel[i];
            //    var properties = currentThing.GetReferencesAsDictionary();
            //    if ((string)properties["shp"] != "Partial")
            //    {
            //        j++;
            //        fullObjects++;
            //    }
            //}

            if (currentGroup.Count < minItemsInLandmark || maxItemsInLandmark < minItemsInLandmark)
            {
                return null;
            }

            Thing newLandmark = UKS.AddThing("LM_*", situationRoot);

            foreach (Thing lmcm in currentGroup)
            {
                Thing newLMCM = UKS.AddThing("LMCM*", newLandmark);

                foreach (Thing property in lmcm.RelationshipsAsThings)
                {
                    if (property.Label.StartsWith("shp") || property.Label.StartsWith("col"))
                    {
                        UKS.GetOrAddThing(property.Label, newLMCM);
                    }
                    else if (property.Label.StartsWith("cen"))
                    {
                        Thing newProp = UKS.GetOrAddThing(property.Label, newLMCM);
                        newProp.V = new Point3DPlus((Point3DPlus)property.V);
                    }
                }
            }

            if (situationNavigation != null && situationNavigation.IsEnabled())
            {
                toLandmark = newLandmark;
            }

            return newLandmark;
        }

        //returns the landmark that represents a group or a new one if none matching
        public Thing CompareLandmarks(List<Thing> currentGroup)
        {
            //match against each stored landmark (with a score)
            Thing storedLandmark;
            for (int i = 0; i < situationRoot.Children.Count; i++)
            {
                storedLandmark = situationRoot.Children[i];
                if (storedLandmark.Label.StartsWith("LM"))
                {
                    List<double> score = SingleLandmarkMatch(storedLandmark, currentGroup);

                    if (score[0] >= minMatchObjects && score[1] >= minMatchDistances)
                        return storedLandmark;
                    if (score[0] == 0 && score[1] == 0)
                        return null;
                }
            }
            return NewLandmark(currentGroup);
        }

        //Compare a group of objects to an existing landmark
        //return [percent mathcing objects, percent matching distances of objects]
        private List<double> SingleLandmarkMatch(Thing savedLandmark, List<Thing> currentGroup)
        {
            for (int i = 0; i < currentGroup.Count; i++)
            {
                Thing po = currentGroup[i];
                if (po.RelationshipsAsThings.Count == 0)
                    currentGroup.Remove(po);
            }

            List<double> score = new() { 0, 0 };
            int matchingObjects = 0;
            //compare properties of individual objects
            foreach (Thing currentItem in currentGroup)
            {
                string curShape = "";
                string curColor = "";
                foreach (Thing property in currentItem.RelationshipsAsThings)
                {
                    if (property.Label.StartsWith("shp"))
                    {
                        curShape = property.Label;
                    }
                    else if (property.Label.StartsWith("col"))
                    {
                        curColor = property.Label;
                    }
                }
                if (curShape == "" || curColor == "") return score;

                foreach (Thing storedItem in savedLandmark.Children)
                {
                    if (storedItem.Label.StartsWith("LMCM") == false) continue;

                    string storedShape = "";
                    string storedColor = "";
                    foreach (Thing property in storedItem.Children)
                    {
                        if (property.Label.StartsWith("shp"))
                        {
                            storedShape = property.Label;
                        }
                        else if (property.Label.StartsWith("col"))
                        {
                            storedColor = property.Label;
                        }
                    }

                    if (curShape == storedShape && curColor == storedColor)
                    {
                        matchingObjects += 1;
                        break;
                    }
                }
            }

            //find distances between objects in current landmark
            //sum of distances from each object to each other object
            double currentDistance = 0;
            foreach (Thing firstObject in currentGroup)
            {
                Point3DPlus firstLocation = null;
                foreach (Thing property in firstObject.RelationshipsAsThings)
                    if (property.Label.StartsWith("cen"))
                        firstLocation = (Point3DPlus)property.V;
                if (firstLocation == null) continue;

                foreach (Thing secondObject in currentGroup)
                {
                    Point3DPlus secondLocation = null;
                    foreach (Thing property in secondObject.RelationshipsAsThings)
                        if (property.Label.StartsWith("cen"))
                            secondLocation = (Point3DPlus)property.V;
                    if (secondLocation == null) continue;

                    currentDistance += Math.Sqrt(Math.Pow(secondLocation.X - firstLocation.X, 2)
                        + Math.Pow(secondLocation.Y - firstLocation.Y, 2) + Math.Pow(secondLocation.Z - firstLocation.Z, 2));
                }
            }

            //find distances between objects in stored landmark
            double storedDistance = 0;
            foreach (Thing firstObject in savedLandmark.Children)
            {
                if (firstObject.Label.StartsWith("LMCM") == false) continue;
                Point3DPlus firstLocation = null;
                foreach (Thing property in firstObject.Children)
                    if (property.Label.StartsWith("cen"))
                        firstLocation = (Point3DPlus)property.V;
                if (firstLocation == null) continue;

                foreach (Thing secondObject in savedLandmark.Children)
                {
                    if (secondObject.Label.StartsWith("LMCM") == false) continue;
                    Point3DPlus secondLocation = null;
                    foreach (Thing property in secondObject.Children)
                        if (property.Label.StartsWith("cen"))
                            secondLocation = (Point3DPlus)property.V;
                    if(secondLocation == null) continue;

                    storedDistance += Math.Sqrt(Math.Pow(secondLocation.X - firstLocation.X, 2)
                        + Math.Pow(secondLocation.Y - firstLocation.Y, 2) + Math.Pow(secondLocation.Z - firstLocation.Z, 2));
                }
            }

            int currentObjects = currentGroup.Count;

            int existingObjects = 0;
            foreach (Thing LMChild in savedLandmark.Children)
            {
                if (LMChild.Label.StartsWith("LMCM")) existingObjects++;
            }

            score[0] = (double)matchingObjects / (double)Max(currentObjects, existingObjects);
            score[1] = Min(currentDistance, storedDistance) / Max(currentDistance, storedDistance);
            return score;
        }
    }
}