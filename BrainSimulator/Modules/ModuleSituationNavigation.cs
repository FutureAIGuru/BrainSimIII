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
using System.Windows.Media.Media3D;
using System.Diagnostics;

namespace BrainSimulator.Modules
{
    public class ModuleSituationNavigation : ModuleBase
    {
        //true when navigating to a landmark
        public bool navigatingToLM = false;

        //true when moving to the current group
        public bool movingToCur = false;

        //how close to get to the edge of a landmark
        const int boundaryDist = 10;

        public ModuleSituationNavigation()
        {
            minHeight = 1;
            maxHeight = 500;
            minWidth = 1;
            maxWidth = 500;
        }

        //This module only works in conjuction with ModuleSituation, all the logic is in ModuleSituation.Fire()
        public override void Fire()
        {
            GetUKS();

            Init();  //be sure to leave this here

            UpdateDialog();
        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
        }

        //move to the group being explored
        public void MoveToCurrentGroup()
        {
            ModuleSituation moduleSituation = (ModuleSituation)FindModule(typeof(ModuleSituation));

            //move to within 10 units of the boundary before saving the path
            ModuleGoTo moduleGoTo = (ModuleGoTo)FindModule(typeof(ModuleGoTo));

            float radius = ModuleSituation.FindRadius(moduleSituation.currentExploringGroup);
            Point3DPlus center = ModuleSituation.FindCentroid(moduleSituation.currentExploringGroup);
            float magnitude = (float)Math.Sqrt(Math.Pow(center.P.X, 2) + Math.Pow(center.P.Y, 2));
            float distRatio = (magnitude - radius - boundaryDist) / magnitude;
            //move if not already within the boundary of the landmark
            if (distRatio > 0.1)
            {
                center = new Point3DPlus(center.X * distRatio, center.Y * distRatio, center.Z * distRatio);
                if (!moduleGoTo.RouteToPoint(center))
                {
                    errorStr = "Routing Failed";
                    Thing exploreModeThing = UKS.Labeled("FindLandmarks", UKS.GetOrAddThing("Attention", "Thing").Children);
                    UKS.DeleteThing(exploreModeThing);
                    moduleSituation.currentExploringGroup = null;
                }
            }
        }

        public float GetGroupDistance(List<Thing> group)
        {
            Point3DPlus centroid = ModuleSituation.FindCentroid(group);
            return centroid.R;
        }

        //returns true if the group is within Sally's range of vision
        public bool IsGroupVisible(List<Thing> group)
        {
            Point3DPlus centroid = ModuleSituation.FindCentroid(group);
            float angle = centroid.Theta.Degrees;
            ModuleMentalModel moduleMentalModel = (ModuleMentalModel)FindModule(typeof(ModuleMentalModel));
            float visibleAngle = UnknownArea.FieldOfVision;

            if (angle < visibleAngle && angle > -visibleAngle)
                return true;
            else
                return false;
        }

        //create an event that gets Sallie from one landmark to another 
        public void SaveNaviEvent(List<Thing> fromGroup, List<Thing> toGroup)
        {
            GetUKS();

            ModuleSituation moduleSituation = (ModuleSituation)FindModule(typeof(ModuleSituation));

            Thing fromLM = moduleSituation.CompareLandmarks(fromGroup);
            Thing toLM = moduleSituation.CompareLandmarks(toGroup);

            if (fromLM == null || toLM == null || fromLM == toLM)
            {
                return;
            }

            Point3DPlus fromCentroid = ModuleSituation.FindCentroid(fromGroup);
            Point3DPlus toCentroid = ModuleSituation.FindCentroid(toGroup);

            float angleBetween = toCentroid.Theta.Degrees - fromCentroid.Theta.Degrees;

            float curCentroidDist = (float)Math.Sqrt(Math.Pow(toCentroid.P.X, 2) + Math.Pow(toCentroid.P.Y, 2));
            float lastCentroidDist = (float)Math.Sqrt(Math.Pow(fromCentroid.P.X, 2) + Math.Pow(fromCentroid.P.Y, 2));

            //law of cosines to find distance between the two landmarks
            double centroidDifference = Math.Sqrt(Math.Pow(curCentroidDist, 2) + Math.Pow(lastCentroidDist, 2)
                - (2 * curCentroidDist * lastCentroidDist * Math.Cos(Math.Abs(angleBetween) * Math.PI / 180)));

            Thing events = UKS.GetOrAddThing("Event", "Behavior");

            if(centroidDifference > moduleSituation.navigateVisionDistance)
            {
                return;
            }

            //find event that takes place at lastLandmark
            Thing theEvent = null;

            foreach (Thing anEvent in events.Children)
            {
                foreach (Thing eventChild in anEvent.Children)
                {
                    if (eventChild.Label == fromLM.Label) theEvent = anEvent;
                }
            }

            if (theEvent == null)
            {
                theEvent = UKS.GetOrAddThing("Navi*", "Event");
                theEvent.AddChild(fromLM);
            }
            else //check if an event between the two landmarks already exists
            {
                foreach (Thing result in theEvent.Children)
                {
                    foreach (Relationship reference in result.Relationships)
                    {
                        if (reference.T == toLM)
                        {
                            return;
                        }
                    }
                }
            }

            //save outcome
            Thing eventResult = UKS.AddThing(theEvent.Label + "_Result*", theEvent);
            eventResult.AddRelationship(toLM);
            Thing outcomes = UKS.GetOrAddThing("Outcome", "Behavior");
            if (toLM.HasAncestor(outcomes) == false)
                outcomes.AddChild(toLM);
        }

        //sequence of actions leading to the next landmark to stop at on the way to the goal landmark
        Thing EventResultTowardGoal;
        [XmlIgnore]
        public Thing pathingTarget;
        private string errorStr = "";

        //finds the next landmark on the way to the goal landmark
        //returns string error message for testing dialog
        public void GoToLandmark()
        {
            ModuleSituation moduleSituation = (ModuleSituation)FindModule("Situation");
            Thing goToThing = UKS.GetOrAddThing("GoToLandmark", "Attention");
            string goalStr = goToThing.RelationshipsAsThings[0].Label;

            //if not at a landmark see if the goal landmark is in the mental model
            if (moduleSituation.curClosestLM == null)
            {
                List<List<Thing>> groupsCopy = new(moduleSituation.groups);
                foreach(List<Thing> group in groupsCopy)
                {
                    Thing LM = moduleSituation.CompareLandmarks(group);
                    if (LM.Label == goalStr)
                    {
                        if (GetGroupDistance(group) > moduleSituation.navigateVisionDistance) break;
                        pathingTarget = LM;
                        navigatingToLM = true;
                        errorStr = "Moving to " + pathingTarget.Label;
                        return;
                    }
                }
                errorStr = "Not currently at a landmark and goal landmark not found in range";
                return;
            }

            //move forward to landmark in view if it is the goal
            if(moduleSituation.curClosestLM.Label == goalStr)
            {
                List<List<Thing>> groupsCopy = new(moduleSituation.groups);
                foreach (List<Thing> group in groupsCopy)
                {
                    Thing LM = moduleSituation.CompareLandmarks(group);
                    if (LM.Label == goalStr)
                    {
                        if (GetGroupDistance(group) > moduleSituation.navigateVisionDistance)
                        {
                            errorStr = "Goal landmark out of range";
                            return;
                        }
                        else break;
                    }
                }
                pathingTarget = moduleSituation.curClosestLM;
                navigatingToLM = true;
                errorStr = "Moving closer to " + pathingTarget.Label;
                return;
            }

            ModuleEvent EventModule = (ModuleEvent)FindModule("Event");
            ModulePodInterface podInterface = (ModulePodInterface)FindModule(typeof(ModulePodInterface));

            //find goal outcome
            Thing goalThing = UKS.Labeled(goalStr, UKS.GetOrAddThing("Outcome", "Behavior").Children);
            if (goalThing == null)
            {
                List<List<Thing>> groupsCopy = new(moduleSituation.groups);
                foreach (List<Thing> group in groupsCopy)
                {
                    Thing LM = moduleSituation.CompareLandmarks(group);
                    if (LM.Label == goalStr)
                    {
                        if (GetGroupDistance(group) > moduleSituation.navigateVisionDistance)
                        {
                            errorStr = "Goal landmark out of range";
                            return;
                        };
                        pathingTarget = LM;
                        navigatingToLM = true;
                        errorStr = "Moving to " + pathingTarget.Label;
                        return;
                    }
                }
                errorStr = "Goal landmark not found";
                return;
            }

            EventResultTowardGoal = EventModule.FindTowardGoal(goalThing, moduleSituation.curClosestLM, out string step);

            if (EventResultTowardGoal == null)
            {
                List<List<Thing>> groupsCopy = new(moduleSituation.groups);
                foreach (List<Thing> group in groupsCopy)
                {
                    Thing LM = moduleSituation.CompareLandmarks(group);
                    if (LM.Label == goalStr)
                    {
                        if (GetGroupDistance(group) > moduleSituation.navigateVisionDistance) break;
                        pathingTarget = LM;
                        navigatingToLM = true;
                        errorStr = "Moving to " + pathingTarget.Label;
                        return;
                    }
                }
                errorStr = "No path to goal from " + moduleSituation.curClosestLM.Label;
                return;
            }
            pathingTarget = EventResultTowardGoal.RelationshipsAsThings.Last();

            //start the pathing sequence
            navigatingToLM = true;

            errorStr = "Moving from " + moduleSituation.curClosestLM.Label + " to " + pathingTarget.Label;
        }

        //move the correct distance in the correct direction to get the target landmark
        //the target landmark is either the goal landmark or the first landmark leading to the goal landmark 
         public void NavigateToLandmark()
        {
            ModuleSituation moduleSituation = (ModuleSituation)FindModule(typeof(ModuleSituation));

            List<Thing> toGroup = null;

            foreach (List<Thing> group in moduleSituation.groups)
            {
                Thing LM = moduleSituation.CompareLandmarks(group);
                if (LM == pathingTarget)
                    toGroup = group;
            }

            //move to within 10 units of the boundary before saving the path
            ModuleGoTo moduleGoTo = (ModuleGoTo)FindModule(typeof(ModuleGoTo));

            float radius = ModuleSituation.FindRadius(toGroup);
            Point3DPlus center = ModuleSituation.FindCentroid(toGroup);
            float magnitude = (float)Math.Sqrt(Math.Pow(center.P.X, 2) + Math.Pow(center.P.Y, 2));
            float distRatio = (magnitude - radius - boundaryDist) / magnitude;
            //move if not already within the boundary of the landmark
            if (distRatio > 0.1)
            {
                center = new Point3DPlus(center.X * distRatio, center.Y * distRatio, center.Z * distRatio);
                if (!moduleGoTo.RouteToPoint(center))
                {
                    errorStr += "\n---Routing Failed";
                }
            }
        }

        public void EnterExploreMode()
        {
            GetUKS();
            if (UKS == null) return;
            UKS.GetOrAddThing("FindLandmarks", "Attention");
        }

        public void ExitExploreMode()
        {
            GetUKS();
            if (UKS == null) return;
            Thing exploreModeThing = UKS.Labeled("FindLandmarks", UKS.GetOrAddThing("Attention", "Thing").Children);
            UKS.DeleteThing(exploreModeThing);
        }

        public void EnterNavigationMode(string goalLabel)
        {
            GetUKS();
            if (UKS == null) return;
            Thing situationRoot = UKS.GetOrAddThing("Situation", "Behavior");
            Thing goalLM = null;
            foreach(Thing LM in situationRoot.Children)
            {
                if(LM.Label == goalLabel)
                    goalLM = LM;
            }
            if(goalLM == null)
            {
                errorStr = "Goal landmark not found";
                return;
            }
            Thing goToThing = UKS.GetOrAddThing("GoToLandmark", "Attention");
            goToThing.AddRelationship(goalLM);
        }

        public void UpdateErrorMsg()
        {
            if (dlg == null) return;
            ModuleSituationNavigationDlg dialog = (ModuleSituationNavigationDlg)dlg;
            dialog.curError = errorStr;
        }

        //provide output speech after being verbally told to navigate to a landmark
        public void SpeechOutErrorMessage()
        {
            Thing currentVerbalResponse = UKS.GetOrAddThing("CurrentVerbalResponse", "Attention");
            currentVerbalResponse.RemoveAllRelationships();
            foreach (string s in errorStr.Split(" "))
            {
                Thing word = new();
                word.V = s;
                currentVerbalResponse.AddRelationship(word);
            }
        }
    }
}