//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System.Collections.Generic;
using System.Threading;
using UKS;

namespace BrainSimulator.Modules;

public class ModuleRemoveRedundancy : ModuleBase
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
        foreach (Thing t in theUKS.UKSList)
        {
            foreach (Thing parent in t.Parents) //usually only a single parent
            {
                List<Relationship> relationshipsWithInheritance = theUKS.GetAllRelationships(new List<Thing> { parent }, false);
                for (int i = 0; i < t.Relationships.Count; i++)
                {
                    Relationship r = t.Relationships[i];
                    Relationship rMatch = relationshipsWithInheritance.FindFirst(x => x.source != r.source && x.reltype == r.reltype && x.target == r.target);
                    if (rMatch != null)
                    {
                        r.Weight -= 0.1f;
                        if (r.Weight < 0.5f)
                        {
                            t.RemoveRelationship(r);
                            i--;
                            debugString += "Removed: ";
                        }
                        debugString += $"{r}   ({r.Weight:0.00})\n";
                    }
                }
            }
        }
        debugString += "Agent  Finished\n";
        UpdateDialog();
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