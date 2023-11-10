//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using static System.Math;

namespace BrainSimulator.Modules;

public class ModuleAttention : ModuleBase
{
    //any public variable you create here will automatically be saved and restored  with the network
    //unless you precede it with the [XmlIgnore] directive
    //[XmlIgnore] 
    //public theStatus = 1;

    //This module directs a ImageZoom module to intersting locations

    Random rand = new Random();
    int autoAttention = 0;
    int generationsBeforeAttentionChange = 10;// number of generations before changing attention
    [XmlIgnore]
    public bool attentionLock = false;

    //set size parameters as needed in the constructor
    //set max to be -1 if unlimited
    public ModuleAttention()
    {
        minHeight = 1;
        maxHeight = 5;
        minWidth = 1;
        maxWidth = 5;
    }

    Thing currentAttention = null;
    Thing prevAttention = null;
    [XmlIgnore]
    public List<Thing> MentalModelList = new();

    //fill this method in with code which will execute
    //once for each cycle of the engine

    public override void Fire()
    {
        Init();  //be sure to leave this here

        GetUKS();
        if (UKS == null) return;

        ModulePodInterface podInterface = (ModulePodInterface)FindModule(typeof(ModulePodInterface));
        ModuleSituation moduleSituation = (ModuleSituation)FindModule(typeof(ModuleSituation));

        ManageAttention();

        if (MainWindow.theNeuronArray.Generation % generationsBeforeAttentionChange != 0) return;

        Thing mentalModel = UKS.GetOrAddThing("MentalModel", "Thing");
        if (mentalModel is null) return;
        if (mentalModel.Children.Count == 0) return;
        MentalModelList.Clear();
        for (int i = 0; i < mentalModel.Children.Count; i++)
        {
            MentalModelList.Add(mentalModel.Children[i]);
        }

        //ModuleAttentionDlg attentionDlg = (ModuleAttentionDlg)child;
        //attentionDlg.updateComboBox();

        Thing newAttention = NewAttention();
        if (!attentionLock)
        {
            if (newAttention != null)
            {
                currentAttention = newAttention;
            }
            else if (prevAttention == null)
            {
                currentAttention = mentalModel.Children[rand.Next(mentalModel.Children.Count)];
                currentAttention.useCount++;
            }
            else
            {
                currentAttention = ProxyAttention(prevAttention);
                if (currentAttention == null)
                    currentAttention = mentalModel.Children[rand.Next(mentalModel.Children.Count)];
            }

            AddAtttention(currentAttention);
            currentAttention.SetFired();


            currentAttention.useCount++;
            prevAttention = currentAttention;         

            foreach (Relationship l in currentAttention.Relationships)
            {
               
                if (l.T != null && (l.T as Thing).V != null && (l.T as Thing).V.ToString() == "White")
                {
                    continue;
                }
                (l.T as Thing).SetFired();
            }
        }

        //if attention doesn't reference Object Tree and is center object
        // Lock attention, ask "what is this?"
        //ModuleQuery moduleQuery = (ModuleQuery)FindModule(typeof(ModuleQuery));
        //ModuleSpeechInPlus moduleSpeechInPlus = (ModuleSpeechInPlus)FindModule(typeof(ModuleSpeechInPlus));
        //if (!currentAttention.RelationshipsAsThings.Exists(t => t.HasAncestorLabeled("Object")) &&
        //        currentAttention == moduleQuery.FindObjectMostCenter())
        //{
        //    if (attentionLock == true) return;
        //    attentionLock = true;
        //    Thing queryResult = UKS.Labeled("CurrentQueryResult");
        //    if (queryResult == null)
        //    {
        //        return;
        //    }
        //    Thing action = new Thing();
        //    action.V = "Speak";
        //    action.AddRelationship(UKS.Labeled("resWhatIsThis"));
        //    moduleSpeechInPlus.StartContinuousRecognition();
        //    queryResult.AddRelationship(action);
        //    if (podInterface == null) return;
        //    podInterface.CommandPause(1500); 
        //}
        //else
        {
            if (podInterface == null) return;
            podInterface.CommandUnpause();
            attentionLock = false;
        }

        UpdateDialog();
    }

    void ManageAttention()
    {
        GetUKS();
        Thing attention = UKS.GetOrAddThing("Attention", "Thing");

        foreach (var currentAttention in attention.Children)
        {
            if (currentAttention.Label == "Stop")
            {
                UKS.DeleteThing(UKS.Labeled("TurnAround"));
                UKS.DeleteThing(UKS.Labeled("Explore"));
                UKS.DeleteThing(UKS.Labeled("GoTo"));
                UKS.DeleteThing(currentAttention);
                break;
            }
        }
    }

    public void AddAtttention(Thing t)
    {

        GetUKS();
        Thing attn = UKS.GetOrAddThing("Attention", "Thing");
        Thing parent = t.Parents[t.Parents.Count - 1];
        attn.RemoveRelationshpsWithAncestor(UKS.Labeled("Thing"));
        attn.AddRelationship(t);
        currentAttention = t;
        UpdateDialog();

    }

    public void LockAttention(bool lck)
    {
        attentionLock = lck;
    }

    //fill this method in with code which will execute once
    //when the module is added, when "initialize" is selected from the context menu,
    //or when the engine restart button is pressed
    public override void Initialize()
    {
        GetUKS();
        autoAttention = 0;
        currentAttention = null;
        if (UKS != null)
        {
            Thing attn = UKS.GetOrAddThing("Attention", "Thing");
            UKS.DeleteAllChildren(attn);
        }
    }

    public Thing NewAttention()
    {
        Thing mentalModel = UKS.GetOrAddThing("MentalModel", "Thing");
        Thing newCollection = new Thing();
        Thing bestNew = null;
        Angle angle = Angle.FromDegrees(0);
        Angle min = Angle.FromDegrees(1000);

        foreach (Thing t in mentalModel.Children)
        {

            if (t.useCount == 0)
                newCollection.AddChild(t);

        }
        if (newCollection.Children.Count != 0)
        {
            foreach (Thing t in newCollection.Children)
            {
                var test = t.GetRelationshipsAsDictionary();
                if (test.ContainsKey("Cen"))
                {
                    var p = test["cen"];

                    if (p is Point3DPlus p1)
                    {
                        if (Math.Abs(p1.Theta + p1.Phi - angle) <= min)
                        {
                            angle = p1.Theta + p1.Phi;
                            min = angle;
                            bestNew = t;
                        }
                    }
                }
                else return null;
            }
            return bestNew;
        }
        return null;
    }

    public Thing ProxyAttention(Thing prev)
    {
        Thing mentalModel = UKS.GetOrAddThing("MentalModel", "Thing");
        Thing bestNew = null;
        Angle min = Angle.FromDegrees(1000);
        var dict = prev.GetRelationshipsAsDictionary();
        Point3DPlus prevCenter;
        if (dict.ContainsKey("cen"))
        {
            prevCenter = (Point3DPlus)dict["cen"];

            foreach (Thing t in mentalModel.Children)
            {
                if (t != prev && t.useCount < prev.useCount)
                {
                    var reference = t.GetRelationshipsAsDictionary();
                    if (reference.ContainsKey("cen"))
                    {
                        //                            var p = reference["cen"];
                        if (reference["cen"] is Point3DPlus p1)
                        {
                            Angle diff = Math.Abs(prevCenter.Theta - p1.Theta) +
                                Math.Abs(prevCenter.Phi - p1.Phi);
                            if (diff < min)
                            {
                                min = diff;
                                bestNew = t;
                            }
                        }
                        else
                            return null;
                    }
                }
            }
            return bestNew;
        }
        return null;
    }
    public void SetAttention(Thing t, int delay = 10)
    {
        if (t == null)
        {
            if (autoAttention > -2)
                autoAttention = 0;
            return;
        }
        if (autoAttention != -2)
            autoAttention = delay;
        AddAtttention(t);
        t.SetFired();
    }

    public void SetEnable(bool enable)
    {
        if (!enable)
            autoAttention = -2;
        else if (autoAttention == -2)
            autoAttention = 0;
    }

    public override void UKSInitializedNotification()
    {
        GetUKS();
        if (UKS == null) return;
        MainWindow.SuspendEngine();


        MainWindow.ResumeEngine();
    }
}