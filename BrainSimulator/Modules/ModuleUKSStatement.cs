//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using Pluralize.NET;
using System;
using System.Collections.Generic;
using System.Runtime.Intrinsics.Arm;
using System.Windows.Documents;
using UKS;
using static BrainSimulator.Modules.ModuleOnlineInfo;

namespace BrainSimulator.Modules;

public class ModuleUKSStatement : ModuleBase
{
    //any public variable you create here will automatically be saved and restored  with the network
    //unless you precede it with the [XmlIgnore] directive

    public ModuleUKSStatement()
    {
    }

    //fill this method in with code which will execute
    //once for each cycle of the engine
    public override void Fire()
    {
        Init();  //be sure to leave this here

        // if you want the dlg to update, use the following code whenever any parameter changes
        // UpdateDialog();
    }

    // fill this method in with code which will execute once
    // when the module is added, when "initialize" is selected from the context menu,
    // or when the engine restart button is pressed
    public override void Initialize()
    {
    }

    // the following can be used to massage public data to be different in the xml file
    // delete if not needed
    public override void SetUpBeforeSave()
    {
    }

    public override void SetUpAfterLoad()
    {
    }

    // called whenever the size of the module rectangle changes
    // for example, you may choose to reinitialize whenever size changes
    // delete if not needed
    public override void SizeChanged()
    {

    }


    public Thought AddLink(string source, string target, string linkType)
    {
        GetUKS();
        if (theUKS is null) return null;
        IPluralize pluralizer = new Pluralizer();
        if (pluralizer.IsPlural(source) && pluralizer.IsPlural(target) && linkType == "are")
            linkType = "is-a";

        //Figure out the source
        var sourceParts = Singular(source.Split(" ", StringSplitOptions.RemoveEmptyEntries));
        Thought tSource = null;
        if (sourceParts.Length == 3)
            tSource = theUKS.AddStatement(sourceParts[0], sourceParts[1], sourceParts[2]);
        if (tSource is null)
        {
            tSource = theUKS.CreateThingFromMultipleAttributes(source, false);
        }
        //figure out the RelType
        Thought tRelType = theUKS.CreateThingFromMultipleAttributes(linkType, true);


        //Figure out the target
        var targetParts = Singular(target.Split(" ", StringSplitOptions.RemoveEmptyEntries));
        Thought tTarget = null;

        if (target.StartsWith("*"))
        {
            List<Thought> targets = new();
            targetParts = target[1..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (string label in targetParts)
            {
                Thought t = theUKS.GetOrAddThing(label);
                targets.Add(t);
            }
            Thought r1 = theUKS.AddSequence(tSource, tRelType, targets);
            return r1;
        }
 
        if (targetParts.Length == 3)
            tTarget = theUKS.AddStatement(targetParts[0], targetParts[1], targetParts[2]);
        if (tTarget is null)
            tTarget = theUKS.CreateThingFromMultipleAttributes(target, false);
        if (target == "" && linkType == "is-a")
        {
            if (target == "" && source != "")
                theUKS.AddThing(source, null);
            return null;
        }

        //Create the link
        Thought r = theUKS.AddStatement(tSource, tRelType, tTarget);

        if (tRelType.Label == "IF")  //this is a HACK which must be fixed later
        {
            tSource.AddLink("isResult", "hasProperty");
            tTarget.AddLink("isCondition", "hasProperty");
        }
        return r;
    }

    string[] Singular(string[] s)
    {
        IPluralize pluralizer = new Pluralizer();
        for (int i = 0; i < s.Length; i++)
        {
            if (!char.IsUpper(s[i][0]) && s[i].Length > 2)
                s[i] = pluralizer.Singularize(s[i]);
        }
        return s;
    }

    public static List<Thought> ThingListFromString(string source)
    {
        List<Thought> retVal = new();
        IPluralize pluralizer = new Pluralizer();
        source = source.Trim();
        string[] tempStringArray = source.Split(' ');
        //first, build a list of all the Things in the list
        for (int i = 0; i < tempStringArray.Length; i++)
        {
            if (tempStringArray[i] == "") continue;
            if (!char.IsUpper(tempStringArray[i][0]) && tempStringArray[i].Length > 2)
                tempStringArray[i] = pluralizer.Singularize(tempStringArray[i]);
            Thought t = ThoughtLabels.GetThing(tempStringArray[i]);
            if (t is null) return retVal;
            retVal.Add(t);
        }

        return retVal;
    }
}

