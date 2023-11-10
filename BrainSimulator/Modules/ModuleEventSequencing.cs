//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using static System.Math;

namespace BrainSimulator.Modules
{
    public class ModuleEventSequencing : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XmlIgnore] 
        //public theStatus = 1;

        string curSituation, lastSituation, curAction;

        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleEventSequencing()
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

            UpdateDialog();
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

        //create an example situation structure describing how driving through stoplights works
        public void StoplightEvents()
        {
            ModuleEvent moduleEvent = (ModuleEvent)FindModule(typeof(ModuleEvent));

            Thing redDriving = moduleEvent.GetOrAddSituation("Driving toward red light");
            Thing yellowDriving = moduleEvent.GetOrAddSituation("Driving toward yellow light");
            Thing greenDriving = moduleEvent.GetOrAddSituation("Driving toward green light");
            Thing noLightDriving = moduleEvent.GetOrAddSituation("Driving toward no light");
            Thing redStopped = moduleEvent.GetOrAddSituation("Stopped at red light");
            Thing greenStopped = moduleEvent.GetOrAddSituation("Stopped at green light");
            Thing crash = moduleEvent.GetOrAddSituation("Crash");

            moduleEvent.AddEventResult(moduleEvent.GetOrAddEvent(redDriving), moduleEvent.GetOrAddAction("Stop"), moduleEvent.GetOrAddOutcome("Stopped at red light"), .95f);
            moduleEvent.AddEventResult(moduleEvent.GetOrAddEvent(redDriving), moduleEvent.GetOrAddAction("Wait"), moduleEvent.GetOrAddOutcome("Crash"), .8f);

            moduleEvent.AddEventResult(moduleEvent.GetOrAddEvent(redStopped), moduleEvent.GetOrAddAction("Wait", greenStopped), moduleEvent.GetOrAddOutcome("Stopped at green light"), .95f);
            moduleEvent.AddEventResult(moduleEvent.GetOrAddEvent(redStopped), moduleEvent.GetOrAddAction("Drive"), moduleEvent.GetOrAddOutcome("Crash"), .8f);

            moduleEvent.AddEventResult(moduleEvent.GetOrAddEvent(yellowDriving), moduleEvent.GetOrAddAction("Wait", redDriving), moduleEvent.GetOrAddOutcome("Driving toward red light"), .45f);
            moduleEvent.AddEventResult(moduleEvent.GetOrAddEvent(yellowDriving), moduleEvent.GetOrAddAction("Wait", noLightDriving), moduleEvent.GetOrAddOutcome("Driving toward no light"), .45f);
            moduleEvent.AddEventResult(moduleEvent.GetOrAddEvent(yellowDriving), moduleEvent.GetOrAddAction("Stop"), moduleEvent.GetOrAddOutcome("Crash"), .2f);

            moduleEvent.AddEventResult(moduleEvent.GetOrAddEvent(noLightDriving), moduleEvent.GetOrAddAction("Wait", redDriving), moduleEvent.GetOrAddOutcome("Driving toward red light"), .2f);
            moduleEvent.AddEventResult(moduleEvent.GetOrAddEvent(noLightDriving), moduleEvent.GetOrAddAction("Wait", yellowDriving), moduleEvent.GetOrAddOutcome("Driving toward yellow light"), .2f);
            moduleEvent.AddEventResult(moduleEvent.GetOrAddEvent(noLightDriving), moduleEvent.GetOrAddAction("Wait", greenDriving), moduleEvent.GetOrAddOutcome("Driving toward green light"), .5f);
            moduleEvent.AddEventResult(moduleEvent.GetOrAddEvent(noLightDriving), moduleEvent.GetOrAddAction("Stop"), moduleEvent.GetOrAddOutcome("Crash"), .6f);

            moduleEvent.AddEventResult(moduleEvent.GetOrAddEvent(greenDriving), moduleEvent.GetOrAddAction("Wait", noLightDriving), moduleEvent.GetOrAddOutcome("Driving toward no light"), .7f);
            moduleEvent.AddEventResult(moduleEvent.GetOrAddEvent(greenDriving), moduleEvent.GetOrAddAction("Wait", yellowDriving), moduleEvent.GetOrAddOutcome("Driving toward yellow light"), .2f);
            moduleEvent.AddEventResult(moduleEvent.GetOrAddEvent(greenDriving), moduleEvent.GetOrAddAction("Stop"), moduleEvent.GetOrAddOutcome("Crash"), .6f);

            moduleEvent.AddEventResult(moduleEvent.GetOrAddEvent(greenStopped), moduleEvent.GetOrAddAction("Drive"), moduleEvent.GetOrAddOutcome("Driving toward no light"), .95f);
            moduleEvent.AddEventResult(moduleEvent.GetOrAddEvent(greenStopped), moduleEvent.GetOrAddAction("Wait"), moduleEvent.GetOrAddOutcome("Road rage"), .2f);

            //testing
            //List<Tuple<List<Thing>, float>> paths = moduleEvent.GetAllPathsToGoal(noLightDriving, yellowDriving);
            //List<Tuple<List<Thing>, float>> paths = moduleEvent.GetAllPathsToGoal(redDriving, redStopped);
            //moduleEvent.PrintPaths(paths);
        }

        public void UpdateSituation(string situationName)
        {
            lastSituation = curSituation;
            curSituation = situationName;

            ModuleEvent moduleEvent = (ModuleEvent)FindModule(typeof(ModuleEvent));

            if (String.IsNullOrEmpty(curSituation) || String.IsNullOrEmpty(curAction)) return;

            moduleEvent.AddEventResult(moduleEvent.GetOrAddEvent(moduleEvent.GetOrAddSituation(lastSituation)), 
                moduleEvent.GetOrAddAction(curAction), moduleEvent.GetOrAddOutcome(curSituation));

            curAction = "";
        }

        public void TakeAction(string actionName)
        {
            curAction = actionName;
        }
    }
}