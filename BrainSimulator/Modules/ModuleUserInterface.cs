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

namespace BrainSimulator.Modules
{
    public class ModuleUserInterface : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XmlIgnore] 
        //public theStatus = 1;

        public string ImageSaveLocation { get => ((ModuleUserInterfaceDlg)dlg).imageSaveLocation; }
        
        public bool GetSaveImage()
        {
            return dlg == null ? false : ((ModuleUserInterfaceDlg)dlg).saveImage;
        }

        public void SetSaveImage(bool setVal)
        {
            ModulePodAudio pa = (ModulePodAudio)FindModule(typeof(ModulePodAudio));
            if (dlg != null && pa != null)
            {
                ((ModuleUserInterfaceDlg)dlg).saveImage = setVal;
                if(setVal == true) pa.PlaySoundEffect("CameraClick2.3.wav");
            }
        }

        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleUserInterface()
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

            SetBatteryLevels();

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
            //InitializeDialog();
        }

        public void InitializeDialog()
        {
            ModuleUserInterfaceDlg userInterfaceDlg = (ModuleUserInterfaceDlg)base.dlg;
            ModulePod modulePod = (ModulePod)FindModule(typeof(ModulePod));
            ModulePodInterface modulePodInterface = (ModulePodInterface)FindModule(typeof(ModulePodInterface));

            if (userInterfaceDlg != null && modulePod != null & modulePodInterface != null)
            {
                userInterfaceDlg.Initialize(modulePod.MinPan, modulePod.MaxPan, modulePod.MinTilt, modulePod.MaxTilt, modulePodInterface.MinSpeed, modulePodInterface.MaxSpeed,
                    out float setSpeed);

                modulePodInterface.CommandSpeed(setSpeed);
            }

            ModuleSpeechInPlus moduleSpeechInPlus = (ModuleSpeechInPlus)FindModule(typeof(ModuleSpeechInPlus));
            if(moduleSpeechInPlus != null)
            {
                moduleSpeechInPlus.speechEnabled = true;
                moduleSpeechInPlus.remoteMicEnabled = true;
            }

            ModuleSpeechOut moduleSpeechOut = (ModuleSpeechOut)FindModule(typeof(ModuleSpeechOut));
            if(moduleSpeechOut != null)
            {
                moduleSpeechOut.ResumeSpeech();
                moduleSpeechOut.remoteSpeakerEnabled = true;
            }

            ModulePodCamera modulePodCamera = (ModulePodCamera)FindModule(typeof(ModulePodCamera));
            if (modulePodCamera != null)
            {
                modulePodCamera.saveImagesToDisk = true;
            }
        }

        //called whenever the size of the module rectangle changes
        //for example, you may choose to reinitialize whenever size changes
        //delete if not needed
        public override void SizeChanged()
        {
            if (mv == null) return; //this is called the first time before the module actually exists
        }

        public void SetBatteryLevels()
        {
            ModulePod modulePod = (ModulePod)FindModule(typeof(ModulePod));
            if (modulePod == null) return;

            ModuleUserInterfaceDlg moduleUserInterfaceDlg = (ModuleUserInterfaceDlg)dlg;
            if (moduleUserInterfaceDlg == null) return;

            moduleUserInterfaceDlg.SetBatteryLevels(modulePod.battery1, modulePod.battery2);
        }

        //returns true if message was set
        public bool SetErrorMessage(string msg)
        {
            if(Application.Current.Dispatcher.Invoke(() => ((ModuleUserInterfaceDlg)dlg).SetErrorMessage(msg)))
                return true;
            return false;
        }

        public void PanCamera(Angle target)
        {
            ModulePodInterface modulePodInterface = (ModulePodInterface)FindModule(typeof(ModulePodInterface));
            if (modulePodInterface == null) return;

            modulePodInterface.CommandPan(-target, false, true);
        }

        public void TiltCamera(Angle target)
        {
            ModulePodInterface modulePodInterface = (ModulePodInterface)FindModule(typeof(ModulePodInterface));
            if (modulePodInterface == null) return;

            modulePodInterface.CommandTilt(target, false, true);
        }

        public void CenterCamera()
        {
            ModulePodInterface modulePodInterface = (ModulePodInterface)FindModule(typeof(ModulePodInterface));
            if (modulePodInterface == null) return;

            modulePodInterface.ResetCamera();
        }

        public void SaveWakeWord(string newName)
        {
            ModuleSpeechInPlus moduleSpeechInPlus = (ModuleSpeechInPlus)FindModule(typeof(ModuleSpeechInPlus));
            if(moduleSpeechInPlus == null) return;

            moduleSpeechInPlus.SetWakeWord(newName);
            moduleSpeechInPlus.Initialize();
        }

        public void Recalibrate()
        {
            ModulePod modulePod = (ModulePod)FindModule(typeof(ModulePod));
            if(modulePod == null) return;

            modulePod.Recalibrate_Mpu();
        }

        public void Reboot()
        {
            ModulePod modulePod = (ModulePod)FindModule(typeof(ModulePod));
            if (modulePod == null) return;

            modulePod.Reboot_ESP();
        }

        public void UpdateEnvironment(List<ModelUIElement3D> theObjects)
        {
            ModuleUserInterfaceDlg userInterfaceDlg = (ModuleUserInterfaceDlg)dlg;
            if(userInterfaceDlg != null)
                userInterfaceDlg.UpdateEnvironment(theObjects);
        }

        public void DrawSpeechIn(string speechIn)
        {
            ModuleUserInterfaceDlg userInterfaceDlg = (ModuleUserInterfaceDlg)dlg;
            if (userInterfaceDlg != null)
                userInterfaceDlg.DrawSpeechIn(speechIn);
        }

        public void DrawSpeechOut(string speechOut)
        {
            ModuleUserInterfaceDlg userInterfaceDlg = (ModuleUserInterfaceDlg)dlg;
            if (userInterfaceDlg != null)
                userInterfaceDlg.DrawSpeechOut(speechOut);
        }
    }
}