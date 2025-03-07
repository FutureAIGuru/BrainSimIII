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

public class ModuleClassCreate : ModuleBase
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
            if (t.HasAncestor("Object") && !t.Label.Contains(".") && !t.Label.Contains("unknown"))
            {
                HandleClassWithCommonAttributes(t);
            }
        }
        debugString += "Agent  Finished\n";
        UpdateDialog();
    }

    void HandleClassWithCommonAttributes(Thing t)
    {
        //build a List of counts of the attributes
        //build a List of all the Relationships which this thing's children have
        List<RelDest> attributes = new();
        foreach (Thing t1 in t.ChildrenWithSubclasses)
        {
            foreach (Relationship r in t1.Relationships)
            {
                if (r.reltype == Thing.HasChild) continue;
                Thing useRelType = GetInstanceType(r.reltype);

                RelDest foundItem = attributes.FindFirst(x => x.relType == useRelType && x.target == r.target);
                if (foundItem == null)
                {
                    foundItem = new RelDest { relType = useRelType, target = r.target };
                    attributes.Add(foundItem);
                }
                foundItem.relationships.Add(r);
            }
        }
        //create intermediate parent Things
        foreach (var key in attributes)
        {
            if (key.relationships.Count >= minCommonAttributes)
            {
                Thing newParent = theUKS.GetOrAddThing(t.Label + "." + key.relType + "." + key.target, t);
                newParent.AddRelationship(key.target, key.relType);
                debugString += "Created new subclass " + newParent;
                foreach (Relationship r in key.relationships)
                {
                    Thing tChild = (Thing)r.source;
                    tChild.AddParent(newParent);
                    tChild.RemoveParent(t);
                }
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