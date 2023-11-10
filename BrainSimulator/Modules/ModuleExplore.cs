
// Copyright (c) FutureAI. All rights reserved.  
// Code is proprietary and may not be distributed without a separate licence
//  

using System;
using System.Collections.Generic;
using System.Windows.Media.Media3D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using static System.Math;

namespace BrainSimulator.Modules
{
    public class ModuleExplore : ModuleBase
    {
        public ModuleExplore()
        {
            minHeight = 1;
            maxHeight = 500;
            minWidth = 1;
            maxWidth = 500;
        }

        //list of mental model objects
        List<Thing> objectList = new List<Thing>();

        //marks destination point, so only one move is done per fire
        Point3DPlus unfinishedMove = null;

        //most simple go to method, takes a point, and turns or moves without any checks 
        public void GoToPlace(Point3DPlus point)
        {
            ModulePodInterface podInterface = (ModulePodInterface)FindModule(typeof(ModulePodInterface));
            //sets unfinishedMove to allow the move to be finished in pieces.
            unfinishedMove = point;
            if (point.Theta < Angle.FromDegrees(1.4f))
            {
                podInterface.CommandTurn(-point.Theta);
                return;
            }

            if (((float)Sqrt(Pow(point.X, 2) + Pow(point.Y, 2))) != 0)
            {
                podInterface.CommandTurn((float)Sqrt(Pow(point.X, 2) + Pow(point.Y, 2)));
                return;
            }
            unfinishedMove = null;
            return;
        }

        //method parses a thing and runs referenced point through GoToPlace method
        public override void Fire()
        {
            Init();  //be sure to leave this here
            GetUKS();
            ModulePodInterface podInterface = (ModulePodInterface)FindModule(typeof(ModulePodInterface));
            Thing attention = UKS.GetOrAddThing("Attention", "Thing");
            IList<Thing> attentionRoot = attention.Children;

            if (podInterface.IsPodBusy())
                return;

            if (attentionRoot == null) { return; }
            //check if any unfinished moves exist and complete them if so
            if (unfinishedMove != null)
            {
                GoToPlace(unfinishedMove);
                return;
            }

            foreach (Thing attentionObject in attentionRoot)
            {//determine if Explore or GoTo are in attention
                if (attentionObject.Label == "Explore" && !podInterface.IsPodBusy())
                {
                    Explore(attentionObject);
                    return;
                }
            }
        }

        Thing obstacle = new();

        public void Explore(Thing attentionObject)
        {
            ModuleSafety Safety = (ModuleSafety)FindModule(typeof(ModuleSafety));
            ModuleGoTo GoTo = (ModuleGoTo)FindModule(typeof(ModuleGoTo));

            Thing explore = UKS.GetOrAddThing("Explore", "Attention");
            Thing attention = UKS.GetOrAddThing("Attention", "Thing");
            Thing mentalModel = UKS.GetOrAddThing("MentalModel", "Thing");
            ModulePodInterface podInterface = (ModulePodInterface)FindModule(typeof(ModulePodInterface));
            
            if (objectList.Count == 0)
            {
                foreach (Thing mm in mentalModel.Children)
                {
                    objectList.Add(mm);
                }
            }
            if (attentionObject.Relationships.Count == 1)
            {
                if (objectList.Count != 0)
                    foreach (Thing mentalModelObject in objectList)
                    {
                        //check each mental model object and go to the one furthest away
                        //going to furthest away object is a secondary goal, the most important thing is to as many objects as possible
                        attentionObject.Relationships[0].T = mentalModelObject;
                        var test = mentalModelObject.GetRelationshipsAsDictionary();
                        if (podInterface.IsPodBusy() || !podInterface.QueueIsEmpty()) return;
                        GoTo.RouteToThing(mentalModelObject);
                        objectList.Remove(mentalModelObject);
                        if (objectList.Count == 0)
                            attention.RemoveChild(explore);
                        return;
                    }
                //no direction that is untraveled exists
                objectList.Clear();
                attention.RemoveChild(explore);
                return;
            }
            else
            {
                //if no object is referenced in attention, add reference to first object in mental model
                if (objectList.Count > 0)
                {
                    attentionObject.AddRelationship(objectList[0]);
                    return;
                }

            }
        }
        public override void Initialize()
        {
        }
        public override void SetUpBeforeSave()
        {
        }
        public override void SetUpAfterLoad()
        {
        }

        public override void SizeChanged()
        {
            if (mv == null) return;
        }

        public override void UKSInitializedNotification()
        {
            MainWindow.SuspendEngine();
            GetUKS();

            MainWindow.ResumeEngine();
        }
    }
}
