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
    public class ModuleCollision : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XmlIgnore] 
        //public theStatus = 1;

        //if the pod accelerates faster than this we assume it collided with something
        //100 is 1g; the sensor reads up to 2g
        const int minAccelCollision = 70;
        //if vertical acceleration is less than this, the pod is not considered level
        const int maxAccelTilted = 75;

        DateTime collisionStart = DateTime.Now;
        //how long to wait after a collision before looking for tilting again
        const int collisionDuplicateIgnore = 500;

        bool tilted = false;

        DateTime askHelpStart = DateTime.Now;
        //how long to wait between each time saying "help"
        const int askHelpFrequency = 3;

        //history of accelerations in Z direction
        List<int> lastAccelZ = new();
        //how many acceleration spikes needed in a row to count as being tilted
        //just one spike may be a false alarm
        int tiltDuration = 5;

        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleCollision()
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

        public void playCollisionSound()
        {
            ModulePodAudio modulePodAudio = (ModulePodAudio)FindModule(typeof(ModulePodAudio));
            ModuleHappy moduleHappy = (ModuleHappy)FindModule(typeof(ModuleHappy));

            if (moduleHappy != null) moduleHappy.decreaseHappiness();

            if (modulePodAudio != null)
            {
                if (moduleHappy != null)
                {
                    if (moduleHappy.getMood() == "unhappy")
                    {
                        modulePodAudio.PlaySoundEffect("SallieNegative1.wav");
                    }
                    else
                    {
                        modulePodAudio.PlaySoundEffect("Ouch!.wav");
                    }
                }
                else
                {
                    modulePodAudio.PlaySoundEffect("Ouch!.wav");
                }
            }
        }

        //Ask for help every set number of seconds
        private void AskForHelp()
        {
            ModulePodAudio modulePodAudio = (ModulePodAudio)FindModule(typeof(ModulePodAudio));
            if (modulePodAudio == null) return;

            if ((DateTime.Now - askHelpStart) > TimeSpan.FromSeconds(askHelpFrequency))
            {
                askHelpStart = DateTime.Now;
                modulePodAudio.PlaySoundEffect("SallieTilted.wav");
                ModuleHappy moduleHappy = (ModuleHappy)FindModule(typeof(ModuleHappy));
                if (moduleHappy != null)
                    moduleHappy.decreaseHappiness();
            }
        }

        private static void SetColor(ModuleMentalModel MM, ModuleMentalModelUpdater MMU, Point3DPlus point3DPlus)
        {
            HSLColor red = new(360, 1, 0.5f);
            Dictionary<string, object> properties = new()
            {
                { "cen", point3DPlus },
                { "col", red },
                { "siz", (Single)1 },
                { "ang", Angle.FromDegrees(5) }

            };
            // properties.Add()
            MM.AddPhysicalObject(properties);
        }

        //stop moving if high acceleration in foward direction
        public void CheckCollisionY(int accelY, int leftRate, int rightRate)
        {
            ModulePodInterface modulePodInterface = (ModulePodInterface)FindModule(typeof(ModulePodInterface));
            ModuleMentalModel MM  = (ModuleMentalModel)FindModule(typeof(ModuleMentalModel));
            ModuleMentalModelUpdater MMU = (ModuleMentalModelUpdater)FindModule(typeof(ModuleMentalModelUpdater));
            //ignore any movement when the pod is not busy; dont want to detect a collision while the pod is stopping normally
            //also ignore two collisions in a row since they are really just one collision
            if (modulePodInterface.IsPodBusy() && Math.Abs(accelY) > minAccelCollision && tilted == false
                && (DateTime.Now - collisionStart) > TimeSpan.FromMilliseconds(collisionDuplicateIgnore))
            {
                collisionStart = DateTime.Now;
                if(Math.Sign(leftRate + rightRate) > 0)
                {
                    //collision moving foreward
                    //add object to mental model in front of pod
                    Point3DPlus point3DPlus = new Point3DPlus(3.5f, 0f, 0.5f);
                    SetColor(MM, MMU, point3DPlus);
                }
                else if (Math.Sign(leftRate + rightRate) < 0)
                {
                    //collision moving backwards
                    //add object to mental model behind pod
                    Point3DPlus point3DPlus = new Point3DPlus(-3.5f, 0f, 0.5f);
                    SetColor(MM, MMU, point3DPlus);
                }
              
                modulePodInterface.CommandStop();
                modulePodInterface.CommandMove(Math.Sign(leftRate + rightRate) * -2,false, false);
                playCollisionSound();
            }
        }

        //stop moving if high acceleration to the side
        public void CheckCollisionX(int accelX, int leftRate, int rightRate)
        {
            ModulePodInterface modulePodInterface = (ModulePodInterface)FindModule(typeof(ModulePodInterface));
            ModuleMentalModel MM = (ModuleMentalModel)FindModule(typeof(ModuleMentalModel));
            ModuleMentalModelUpdater MMU = (ModuleMentalModelUpdater)FindModule(typeof(ModuleMentalModelUpdater));

            //ignore any movement when the pod is not busy; dont want to detect a collision while the pod is stopping normally
            //also ignore two collisions in a row since they are really just one collision
            if (modulePodInterface.IsPodBusy() && Math.Abs(accelX) > minAccelCollision && tilted == false
                && (DateTime.Now - collisionStart) > TimeSpan.FromMilliseconds(collisionDuplicateIgnore))
            {
                    collisionStart = DateTime.Now;
                if (Math.Sign(leftRate + rightRate) > 0)
                {
                    Point3DPlus point3DPlus = new Point3DPlus(3.5f, 0f, 0.5f);
                    SetColor(MM, MMU, point3DPlus);
                }
                else if (Math.Sign(leftRate + rightRate) < 0)
                {
                    Point3DPlus point3DPlus = new Point3DPlus(-3.5f, 0f, 0.5f);
                    SetColor(MM, MMU, point3DPlus);
                }
               
                modulePodInterface.CommandStop();
                    modulePodInterface.CommandMove(Math.Sign(leftRate + rightRate) * -2, false, false);
                    playCollisionSound();
            }
        }

        //stop and ask for help if the pod has fallen over
        public void CheckTilted(int accelZ)
        {
            lastAccelZ.Add(accelZ);

            if (lastAccelZ.Count == tiltDuration)
            {
                bool allTilted = true;
                foreach(int accelVal in lastAccelZ)
                {
                    if (accelVal > maxAccelTilted)
                        allTilted = false;
                }
                
                if (allTilted == true)
                {
                    tilted = true;
                    //ignore tilt right aftet a collision
                    if ((DateTime.Now - collisionStart) > TimeSpan.FromMilliseconds(collisionDuplicateIgnore))
                    {
                        ModulePodInterface modulePodInterface = (ModulePodInterface)FindModule(typeof(ModulePodInterface));
                        if (modulePodInterface.IsPodBusy())
                            modulePodInterface.CommandStop();
                        AskForHelp();
                        //foreach (int accelVal in lastAccelZ)
                        //    Debug.WriteLine("AccelZ: " + accelVal);
                    }
                }
                else
                    tilted = false;

                lastAccelZ.Clear();
            }
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