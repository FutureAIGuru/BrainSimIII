//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UKS;

namespace BrainSimulator.Modules;

/// <summary>
/// Contains a collection of Thoughts linked by Links to implement Common Sense and general knowledge.
/// </summary>
public partial class ModuleUKS : ModuleBase
{
    //set size parameters as needed in the constructor
    //set max to be -1 if unlimited
    public ModuleUKS()
    {
    }


    /// <summary>
    /// Currently not used...for future background processing needs
    /// </summary>
    public override void Fire()
    {
        Init();  //be sure to leave this here to enable use of the na variable
    }

    
    /// <summary>
    /// /////////////////////////////////////////////////////////// XML File save/load
    /// </summary>

    public override void Initialize()
    {
        MainWindow.SuspendEngine();
        // Make sure all other loaded modules get notified of UKS Initialization
        UKSInitialized();
        MainWindow.ResumeEngine();
    }

    //these two functions transform the UKS into an structure which can be serialized/deserialized
    //by translating object references into array indices, all the problems of circular references go away
    public override void SetUpBeforeSave()
    {
    }

    public override void SetUpAfterLoad()
    {
    }
}
    
 