//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Imaging;
using UKS;
using System.Windows.Media;
using System.Windows;
using static System.Math;

namespace BrainSimulator.Modules
{
    public partial class ModuleMNIST : ModuleBase
    {
        public ModuleMNIST()
        {
        }
        public int theDigit = 0;
        public bool running = false;
        public bool step = false;

        //fill this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            Init();  //be sure to leave this here
            UpdateDialog();
            if (files == null || files.Length==0) return;

            //if the two needed modules are not found, return
            ModuleVision mv = (ModuleVision)MainWindow.theWindow.GetModule("ModuleVision0");
            if (mv == null) return;
            ModuleShape ms = (ModuleShape)MainWindow.theWindow.GetModule("ModuleShape0");
            if (ms == null) return;
            if (!running && !step)
            {
                ms.MNISTDigit = "";
                return; 
            }
            step = false;

            //select a random file from within the folder tree
            Random rnd = new Random();
            int index = rnd.Next(files.Length);
            string fileName = files[index];
            //the digit in the image is the parent folder of the file OR the name of the file
            theDigit = -1;
            if (!int.TryParse(System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(fileName)),out theDigit))
                int.TryParse(System.IO.Path.GetFileName(System.IO.Path.GetFileNameWithoutExtension(fileName)),out theDigit);
            if (theDigit < 0 || theDigit > 9) return;
            mv.CurrentFilePath = fileName;
            ms.MNISTDigit = "MNIST" + theDigit;
        }

        public override void Initialize()
        {
        }

        // called whenever the size of the module rectangle changes
        // for example, you may choose to reinitialize whenever size changes
        // delete if not needed
        public override void SizeChanged()
        {

        }
        public override void UKSInitializedNotification()
        {
        }

        string[] files;
        internal void LoadFileList(string selectedPath)
        {
            //get all the files in the selected folder and put them in a static cache;
            if (string.IsNullOrEmpty(selectedPath)) return;
            files = System.IO.Directory.GetFiles(selectedPath, "*.png", System.IO.SearchOption.AllDirectories);
            if (files.Count() == 0)
            {
                files = System.IO.Directory.GetFiles(selectedPath, "*.jpg", System.IO.SearchOption.AllDirectories);
            }
        }
    }
}
