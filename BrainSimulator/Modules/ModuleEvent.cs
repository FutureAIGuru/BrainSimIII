//
// Copyright (c) FutureAI. All rights reserved.  
// Code is proprietary and may not be distributed without a separate licence
//  

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Xml.Serialization;
using static System.Math;

namespace BrainSimulator.Modules
{
    public class ModuleEvent : ModuleBase
    {
        //set these to interact with the module

        //Thing in Situation where we currently are
        [XmlIgnore] public Thing start;
        //event that occurs at current situation
        [XmlIgnore] public Thing currentEvent;
        //Thing in Outcomes we want to get to
        [XmlIgnore] public Thing goal;


        //get this to know what action to take next
        [XmlIgnore] public Thing actionToTake;


        [XmlIgnore] public string stepTaken;//testing


        int pointCount = 0;
        int eventCount = 0;
        int landmarkCount = 0;
        int pairCount = 0;

        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleEvent()
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

            actionToTake = FindTowardGoal(goal, start, out stepTaken);
        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
            pointCount = 0;
            eventCount = 0;
            landmarkCount = 0;
            pairCount = 0;
            GetUKS();
            if (UKS == null) return;
            //UKSInitializedNotification();
        }

        //hierarchy should be as follows:
        //E*
        //->S0
        //->ER0(Action, outcome)
        //->ER1(Action, outcome)
        //...

        public Thing GetOrAddSituation(string label)
        {
            GetUKS();
            return UKS.GetOrAddThing(label, "Situation");
        }

        public Thing AddEvent(Thing situation)
        {
            GetUKS();
            Thing newEvent = UKS.GetOrAddThing("E*", "Event");
            newEvent.AddChild(situation);
            return newEvent;
        }

        public Thing GetOrAddEvent(Thing situation)
        {
            GetUKS();
            Thing eventParent = UKS.GetOrAddThing("Event", "Behavior");
            foreach(Thing anEvent in eventParent.Children)
            {
                if (anEvent.Children.Count > 0 && anEvent.Children[0] == situation)
                    return anEvent;
            }

            Thing newEvent = UKS.GetOrAddThing("E*", "Event");
            newEvent.AddChild(situation);
            return newEvent;
        }

        public Thing GetOrAddAction(string actionName, Thing relationship = null)
        {
            GetUKS();
            
            Thing newAction = null;
            Thing actionParent = UKS.GetOrAddThing("Action", "Behavior");
            foreach(Thing action in actionParent.Children)
            {
                if(action.Label == actionName && ((action.RelationshipsAsThings.Count > 0 && action.RelationshipsAsThings[0] == relationship) || relationship == null))
                {
                    newAction = action;
                    break;
                }
            }

            if (newAction == null)
            {
                newAction = UKS.AddThing(actionName, UKS.AddThing("Action", null));

                if (relationship != null)
                    newAction.AddRelationship(relationship);
            }

            return newAction;
        }

        public Thing GetOrAddOutcome(string outcomeName)
        {
            GetUKS();
            Thing outcome = UKS.GetOrAddThing(outcomeName, "Situation");
            UKS.GetOrAddThing("Outcome", "Behavior").AddChild(outcome);
            return outcome;
        }

        public void AddEventResult(Thing baseEvent, Thing action, Thing outcome, float probability = 1)
        {
            ChooseAction(baseEvent, action);
            ChooseOutcome(baseEvent, outcome, probability);
        }

        public List<Thing> SituationList()
        {
            List<Thing> SituationList = new List<Thing>();
            GetUKS();

            if (UKS is null) return SituationList;

            Thing situationRoot = UKS.Labeled("Situation");

            if (situationRoot == null)
                return SituationList;

            foreach (Thing t in situationRoot.Children)
            {
                if (t.Label != "CurrentLandmark")
                {
                    SituationList.Add(t);
                }
            }

            return SituationList;
        }

        public void InitializeEvent(Thing situation)
        {
            currentEvent = UKS.GetOrAddThing("E*", "Event");
            currentEvent.AddChild(situation);
        }

        //only for use by the testing dialogue
        public void ActionButton(string action)
        {
            Thing actionThing = UKS.GetOrAddThing(action, "Action");
            ChooseAction(currentEvent, actionThing);
        }

        //add an event result with an action
        private void ChooseAction(Thing baseEvent, Thing action)
        {
            if (baseEvent == null) return;

            foreach (Thing t1 in baseEvent.Children)
            {
                Relationship E1 = t1.HasRelationship(action);
                if (E1 != null)
                {
                    return;
                }
            }

            Thing t = UKS.AddThing(baseEvent.Label + "_ER*", baseEvent);
            t.AddRelationship(action);
        }

        //for use only by the testing dialogue
        public void OutcomeButton(string outcome)
        {
            Thing outcomeThing = UKS.GetOrAddThing(outcome, "Outcome");
            ChooseOutcome(currentEvent, outcomeThing);
        }

        private void ChooseOutcome(Thing baseEvent, Thing outcome, float probability = 1)
        {
            if (baseEvent == null) { return; }

            //E has a situation and an action
            if (baseEvent.Children.Count > 1)
            {
                //Action already has an outcome
                if (baseEvent.Children.Last().Relationships.Count > 1) return;

                baseEvent.Children.Last().AddRelationship(outcome);
                UKS.GetOrAddThing("probability: " + probability.ToString(), baseEvent.Children.Last());
            }
        }

        //for testing with dialog
        public string GoToGoalButton(string goal, string start)
        {
            Thing goalThing = UKS.GetOrAddThing(goal, "Outcome");
            Thing startThing = UKS.GetOrAddThing(start, "Situation");
            return GoToGoal(goalThing, startThing, out float probability);
        }

        //for testing with dialog
        public Thing FindTowardGoalButton(string goal, string start, out string step)
        {
            Thing goalThing = UKS.GetOrAddThing(goal, "Outcome");
            Thing startThing = UKS.GetOrAddThing(start, "Situation");
            return FindTowardGoal(goalThing, startThing, out step);
        }

        //searches a list of event results for a situation
        private bool PathContainsSituation(Thing startingSituation, List<Thing> path, Thing searchSituation)
        {
            if (path == null) return false;

            if (startingSituation.Label == searchSituation.Label)
                return true;

            foreach(Thing eventResult in path)
                if (eventResult.RelationshipsAsThings[1].Label == searchSituation.Label)
                    return true;

            return false;
        }

        //returns lists of event sequences from start to goal
        //each list is paired with a probability of its occurence
        public List<Tuple<List<Thing>, float>> GetAllPathsToGoal(Thing start, Thing goal, List<Thing> curPath = null, float curProb = 1)
        {
            List<Tuple<List<Thing>, float>> paths = new();

            GetUKS();
            IList<Thing> events = UKS.GetOrAddThing("Event", "Behavior").Children;
            foreach (Thing anEvent in events)
            {
                if (anEvent.Children[0].Label == start.Label)
                {
                    for(int i = 1; i < anEvent.Children.Count; i++)
                    {
                        Thing eventResult = anEvent.Children[i];
                        Thing eventResultOutcome = eventResult.RelationshipsAsThings[1];
                        
                        if (PathContainsSituation(start, curPath, anEvent.Children[0])) 
                            continue;

                        List<Thing> newPath;
                        if (curPath != null) newPath = curPath;
                        else newPath = new();
                        newPath.Add(eventResult);
                        if (eventResultOutcome.Label == goal.Label)
                            paths.Add(new(newPath,curProb * float.Parse(eventResult.Children[0].Label.Split(":").Last())));
                        else
                            paths.AddRange(GetAllPathsToGoal(eventResultOutcome, goal, newPath, curProb * float.Parse(eventResult.Children[0].Label.Split(":").Last())));
                    }
                }
            }

            return paths;
        }

        public void PrintPaths(List<Tuple<List<Thing>, float>> eventPaths)
        {
            if (dlg == null) return;

            string printedPaths = "";

            if (eventPaths.Count == 0) printedPaths = "No paths found";

            for(int i = 0; i < eventPaths.Count; i++)
            {
                List<Thing> path = eventPaths[i].Item1;
                printedPaths += "Path: " + i + 1 + " Probability: " + eventPaths[i].Item2.ToString("0.00");
                foreach(Thing eventResult in path)
                {
                    printedPaths += "\n[" + eventResult.Parents[0].Label + "," + eventResult.RelationshipsAsThings[0].Label + "]-->\n" + eventResult.RelationshipsAsThings[1].Label;
                }
                printedPaths += "\n";
            }

            ((ModuleEventDlg)dlg).resultText.Text = printedPaths;
        }

        //find any path of actions from start situation to any outcome of goal Label
        //do nothing if already at goal or no goal found
        //returns a sequence of situations and actions taken to get from start to goal
        public string GoToGoal(Thing goalThing, Thing startThing, out float probability)
        {
            probability = 1;
            if (goalThing == null || startThing == null) return "Entry Fields Missing";

            if (startThing.Label == goalThing.Label) return goalThing.Label + " Reached";

            string step = "";
            string path = "";
            Thing curEvent = new();
            Thing curSituation = startThing;
            //string cur = startThing.Label;

            while (curEvent != null)
            {
                //if (curEvent.Label.Split(" ")[0] == goalLabel) return path + "-->\nGoal Reached";

                curEvent = FindTowardGoal(goalThing, curSituation, out step);

                if (curEvent == null) return step;

                probability *= float.Parse(curEvent.Children[0].Label.Split(':').Last());

                curSituation = curEvent.RelationshipsAsThings[1];

                //goal found at current situation
                if (curSituation.Label == goalThing.Label) return "Probability: " + probability.ToString("0.00") + "\n" + "Path Taken:\n" + 
                        path + step + "-->\n" + goalThing.Label + " Reached";

                //cur = curEvent.RelationshipsAsThings[1].Label;

                path += step + "-->\n";
            }

            if (step == "Goal Not Found") return step;

            else return "Probability: " + probability.ToString("0.00") + "\n" + "Path Taken:\n" + path;
        }

        //returns an event result that moves toward goal
        //returns null if already at goal or goal does not exist
        //step is the action taken
        public Thing FindTowardGoal(Thing goalThing, Thing startThing, out string step)
        {
            if (goalThing == null || startThing == null)
            {
                step = "Entry Fields Missing";
                return null;
            }

            IList<Relationship> goalLinks = goalThing.RelationshipsFrom;

            List<Thing> allGoals = new();

            //get all events with desired outcome
            foreach (var goalLink in goalLinks)
            {
                if (goalLink.source is Thing gl)
                    allGoals.Add(gl);
            }

            if (startThing.Label == goalThing.Label)
            {
                step = goalThing.Label + " Reached";
                return null;
            }

            GetUKS();
            Thing Event = UKS.GetOrAddThing("Event", "Behavior");
            IList<Thing> allEvents = Event.Children;

            List<Thing> eventResults = new();
            foreach (var anEvent in allEvents)
            {
                //all but the first child are event results
                for (int i = 1; i < anEvent.Children.Count; i++)
                {
                    eventResults.Add(anEvent.Children[i]);
                }
            }

            List<Thing> searchSpace = allGoals;
            for (int i = 0; i < searchSpace.Count; i++)
            {
                Thing search = searchSpace[i];

                //event takes place in the current situation
                if (search.Parents[0].Children[0].Label == startThing.Label.Split(":")[0])
                {
                    step = search.Parents[0].Children[0].Label + "[" + search.Parents[0].Label + ", " + search.RelationshipsAsThings[0].Label + "]";
                    return search;
                }

                //find events that lead to current situation
                List<Thing> previousEvent = new();
                foreach (var eventThing in eventResults)
                {
                    if (eventThing.RelationshipsAsThings.Last().Label.Split(":")[0] == search.Parents[0].Children[0].Label)
                        previousEvent.Add(eventThing);
                }

                //List<Thing> previousEvent = search.RelationshipsAsThings;

                //add to search space
                foreach (var previous in previousEvent)
                {
                    if (searchSpace.Contains(previous) == false)
                        searchSpace.Add(previous);
                }
            }

            step = "Goal Not Found";
            return null;
        }

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
            MainWindow.SuspendEngine();
            GetUKS();

            MainWindow.ResumeEngine();
        }
    }
}
