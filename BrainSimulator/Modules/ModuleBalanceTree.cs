//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using UKS;
using static BrainSimulator.Modules.ModuleAttributeBubble;

namespace BrainSimulator.Modules;

public class ModuleBalanceTree : ModuleBase
{
    // Fill this method in with code which will execute
    // once for each cycle of the engine
    public override void Fire()
    {
        //This agent works on a timer and "Fire" is not used

        Init();

        UpdateDialog();
    }

    public bool isEnabled { get; set; }

    private Timer timer;
    //private UKS.UKS theUKS1;
    public string debugString = "Initialized\n";
    private int maxChildren = 12;
    private int minCommonAttributes = 3;
    public int MaxChildren { get => maxChildren; set => maxChildren = value; }

    private void Setup()
    {
        if (timer is null)
        {
            timer = new Timer(SameThreadCallback, null, 0, 10000);
        }
    }
    private void SameThreadCallback(object state)
    {
        if (!isEnabled) return;
        new Thread(() =>
        {
            DoTheWork();
        }).Start();
    }


    public void DoTheWork()
    {
        debugString = "Agent Started\n";
        for (int i = 0; i < theUKS.UKSList.Count; i++)
        {
            Thing t = theUKS.UKSList[i];
            if (t.HasAncestor("Object") && !t.Label.Contains("."))
            {
                HandleExcessiveChildren(t);
            }
        }
        debugString += "Agent  Finished\n";
        UpdateDialog();
    }
    void HandleExcessiveChildren(Thing t)
    {
        while (t.Children.Count > MaxChildren)
        {
            Thing newParent = theUKS.AddThing(t.Label, t);
            debugString += $"Created new class:  {newParent.Label} \n";
            while (newParent.Children.Count < MaxChildren)
            {
                Thing theChild = t.Children[0];
                theChild.RemoveParent(t);
                theChild.AddParent(newParent);
            }
        }
    }


    // Fill this method in with code which will execute once
    // when the module is added, when "initialize" is selected from the context menu,
    // or when the engine restart button is pressed
    public override void Initialize()
    {
        Setup();
    }

    // The following can be used to massage public data to be different in the xml file
    // delete if not needed
    public override void SetUpBeforeSave()
    {
    }
    public override void SetUpAfterLoad()
    {
        Setup();
    }

    // called whenever the UKS performs an Initialize()
    public override void UKSInitializedNotification()
    {

    }
}