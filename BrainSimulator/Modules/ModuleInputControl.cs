//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using static System.Math;

namespace BrainSimulator.Modules
{
    public class ModuleInputControl : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XmlIgnore] 
        //public theStatus = 1;


        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleInputControl()
        {
            minHeight = 1;
            maxHeight = 500;
            minWidth = 1;
            maxWidth = 500;
        }

        [XmlIgnore]
        public bool turnLeft = false;
        [XmlIgnore]
        public bool turnRight = false;

        private DateTime lastCommand = DateTime.Now;


        //fill this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            Init();  //be sure to leave this here
            ModulePodInterface mpi = (ModulePodInterface)FindModule("PodInterface");
            if (mpi == null) return;
            if (DateTime.Now - lastCommand > TimeSpan.FromSeconds(.05)) {
                if (turnLeft && !turnRight)
                {
                    mpi.CommandTurn(Angle.FromDegrees(-5), true);
                    lastCommand = DateTime.Now;
                }
                else if (turnRight && !turnLeft)
                {
                    mpi.CommandTurn(Angle.FromDegrees(5), true);
                    lastCommand = DateTime.Now;
                }
            }
            //UpdateDialog();
        }

        public void MoveForwardBackward(bool forward)
        {
            ModulePodInterface mpi = (ModulePodInterface)FindModule("PodInterface");
            if (mpi == null) return;
            int distance = forward ? 1000 : -1000;
            mpi.CommandMove(distance, true);
        }

        public void Stop()
        {
            ModulePodInterface mpi = (ModulePodInterface)FindModule("PodInterface");
            if (mpi == null) return;
            turnLeft = false;
            turnRight = false;
            mpi.CommandStop();
        }

        internal void StopForwardBack()
        {
            ModulePodInterface mpi = (ModulePodInterface)FindModule("PodInterface");
            if (mpi == null) return;
            mpi.CommandStopMove();
        }

        internal void StopTurn()
        {
            ModulePodInterface mpi = (ModulePodInterface)FindModule("PodInterface");
            if (mpi == null) return;
            mpi.CommandStopTurn();
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
    }
}