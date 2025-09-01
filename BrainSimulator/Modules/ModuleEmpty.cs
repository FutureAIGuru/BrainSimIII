//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using UKS;
using static System.Math;

namespace BrainSimulator.Modules;

public class ModuleEmpty : ModuleBase
{
    // Any public variable you create here will automatically be saved and restored  
    // with the network unless you precede it with the [XmlIgnore] directive
    // [XmlIgnore] 
    // public theStatus = 1;


    // Fill this method in with code which will execute
    // once for each cycle of the engine
    public override void Fire()
    {
        Init();

        UpdateDialog();
    }

    // Fill this method in with code which will execute once
    // when the module is added, when "initialize" is selected from the context menu,
    // or when the engine restart button is pressed
    public override void Initialize()
    {
    }

    // The following can be used to massage public data to be different in the xml file
    // delete if not needed
    public override void SetUpBeforeSave()
    {
    }
    public override void SetUpAfterLoad()
    {
    }

    // called whenever the UKS performs an Initialize()
    public override void UKSInitializedNotification()
    {

    }
}
