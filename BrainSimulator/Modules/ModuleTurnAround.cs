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
using System.Timers;
using System.Windows.Threading;

namespace BrainSimulator.Modules
{
    public class ModuleTurnAround : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XmlIgnore] 
        //public theStatus = 1;

        //how many times to turn in a full circle
        const int numberTurns = 1;
        //how many degrees to turn before each pause
        const int stepSize = 20;
        // set milliseconds to add a wait between turns
        const int waitPerTurn = 700;
        
        DateTime lastTurned = DateTime.Now;
        bool turnNow = false;
        int timesTurned = 0;
        int degreesTurned = 0;
        DispatcherTimer timer;

        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleTurnAround()
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
            GetUKS();
            ModulePodInterface podInterface = (ModulePodInterface)FindModule(typeof(ModulePodInterface));
            if (podInterface.IsPodBusy()) return;

            Thing turn = UKS.Labeled("TurnAround");
            if (turn == null) return;

            if (turnNow)
                TurnOnce();
        }

        //Turn a small amount, eventually adding up to a full 360
        public void TurnOnce()
        {
            GetUKS();
            ModulePodInterface podInterface = (ModulePodInterface)FindModule(typeof(ModulePodInterface));

            if (turnNow == true && (DateTime.Now - lastTurned) > TimeSpan.FromMilliseconds(waitPerTurn))
            {
                lastTurned = DateTime.Now;
                podInterface.CommandTurn(Angle.FromDegrees(stepSize));
                degreesTurned += stepSize;
                if (degreesTurned == 360)
                {
                    timesTurned += 1;
                    degreesTurned = 0;
                }
                turnNow = false;
                //}
                if (timesTurned >= numberTurns)
                {
                    StopTurning();
                    turnNow = false;
                    return;
                }
            }
        }

        public void StopTurning()
        {
            GetUKS();
            if(UKS == null) return;
            Thing turn = UKS.GetOrAddThing("TurnAround", "Attention");
            UKS.DeleteThing(turn);
            timesTurned = 0;
            degreesTurned = 0;
        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 0, 0, 500);
            timer.Tick += SetTurnNow;
            timer.Start();

            turnNow = true;
            timesTurned = 0;
        }

        private void SetTurnNow(object sender, EventArgs e)
        {
            turnNow = true;
        }

        //the following can be used to massage public data to be different in the xml file
        //delete if not needed
        public override void SetUpBeforeSave()
        {
        }
        public override void SetUpAfterLoad()
        {
            Initialize();
        }

        //called whenever the size of the module rectangle changes
        //for example, you may choose to reinitialize whenever size changes
        //delete if not needed
        public override void SizeChanged()
        {
            if (mv == null) return; //this is called the first time before the module actually exists
        }

        //public override void UKSInitializedNotification()
        //{
        //    MainWindow.SuspendEngine();
        //    GetUKS();
        //    UKS.GetOrAddThing("TurnAround", "Attention");
        //    MainWindow.ResumeEngine();
        //}
    }
}
