/*
 * Brain Simulator Thought
 *
 * Copyright (c) 2026 Charles Simon
 *
 * This file is part of Brain Simulator Thought and is licensed under
 * the MIT License. You may use, copy, modify, merge, publish, distribute,
 * sublicense, and/or sell copies of this software under the terms of
 * the MIT License.
 *
 * See the LICENSE file in the project root for full license information.
 */
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


    public Thought AddLink(Thought tSource, string linkType, string to)
    {
        GetUKS();
        if (theUKS is null) return null;

        //figure out the LinkType
        Thought tLinkType = theUKS.CreateThoughtFromMultipleAttributes(linkType, true);

        //Figure out the target
        var targetParts = Singular(to.Split(" ", StringSplitOptions.RemoveEmptyEntries));
        Thought tTarget = null;

        //handle sequence creation
        if (to.StartsWith("^"))
        {
            List<Thought> targets = new();
            targetParts = to[1..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (string label in targetParts)
            {
                Thought t = theUKS.GetOrAddThought(label);
                targets.Add(t);
            }
            Thought r1 = theUKS.AddSequence(tSource, tLinkType, targets);
            return r1;
        }
 
        if (targetParts.Length == 3)
            tTarget = theUKS.AddStatement(targetParts[0], targetParts[1], targetParts[2]);
        if (tTarget is null)
            tTarget = theUKS.CreateThoughtFromMultipleAttributes(to, false);

        //TODO what is this case?
        if (to == "" && linkType == "is-a")
        {
            //if (from != "")
            //    theUKS.AddThought(from, null);
            return null;
        }

        //Create the link
        Thought r = theUKS.AddStatement(tSource, tLinkType, tTarget);

        if (tLinkType.Label == "IF")  //this is a HACK which must be fixed later
        {
            tSource.AddLink("isResult", "hasProperty");
            tTarget.AddLink("isCondition", "hasProperty");
        }
        return r;
    }

    public string[]  Singular(string[] s)
    {
        IPluralize pluralizer = new Pluralizer();
        for (int i = 0; i < s.Length; i++)
        {
            if (!char.IsUpper(s[i][0]) && s[i].Length > 2)
                s[i] = pluralizer.Singularize(s[i]);
        }
        return s;
    }

    public static List<Thought> ThoughtListFromString(string source)
    {
        List<Thought> retVal = new();
        IPluralize pluralizer = new Pluralizer();
        source = source.Trim();
        string[] tempStringArray = source.Split(' ');
        //first, build a list of all the Thoughts in the list
        for (int i = 0; i < tempStringArray.Length; i++)
        {
            if (tempStringArray[i] == "") continue;
            if (!char.IsUpper(tempStringArray[i][0]) && tempStringArray[i].Length > 2)
                tempStringArray[i] = pluralizer.Singularize(tempStringArray[i]);
            Thought t = ThoughtLabels.GetThought(tempStringArray[i]);
            if (t is null) return retVal;
            retVal.Add(t);
        }

        return retVal;
    }
}

