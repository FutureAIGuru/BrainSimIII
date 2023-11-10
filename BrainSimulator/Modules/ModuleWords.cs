//
// Copyright (c) FutureAI. All rights reserved.  
// Contains confidential and  proprietary information and programs which may not be distributed without a separate license
//  

using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using static BrainSimulator.Modules.ModuleOnlineInfo;
using static System.Math;

namespace BrainSimulator.Modules;

public class ModuleWords : ModuleBase
{
    //any public variable you create here will automatically be saved and restored  with the network
    //unless you precede it with the [XmlIgnore] directive
    //[XlmIgnore] 
    //public theStatus = 1;


    //set size parameters as needed in the constructor
    //set max to be -1 if unlimited
    public ModuleWords()
    {
        minHeight = 1;
        maxHeight = 500;
        minWidth = 1;
        maxWidth = 500;
    }


    //fill this method in with code which will execute
    //once for each cycle of the engine
    DateTime lastWordTime;
    public override void Fire()
    {
        Init();  //be sure to leave this here
        GetUKS();
        Thing incomingInfo = UKS.GetOrAddThing("CurrentIncomingDefinition", "Attention");
        if (incomingInfo.V is string s)
        {
            Thing phrase = UKS.GetOrAddThing("CurrentNLPPhrase", "Attention");
            phrase.V = "def: " + s;
            incomingInfo.V = null;
        }
        if (incomingInfo.V is KidsWord defns)
        {
            string def = defns.definitions[0].definition;
            if (def.StartsWith("noun"))
            {
                string sentence = "a " + defns.word + " is " + def.Substring(5);
                int index = sentence.IndexOf("."); //only the first sentence
                if (index != -1) sentence = sentence.Substring(0, index);
                GetUKS();
                Thing phrase = UKS.GetOrAddThing("CurrentNLPPhrase", "Attention");
                phrase.V = "def: " + sentence;
            }
            incomingInfo.V = null;
        }

        if (lastWordTime == null)
            lastWordTime = DateTime.Now;

        if (lastWordTime > DateTime.Now - TimeSpan.FromSeconds(10)) return;
        Thing wordsParent = UKS.GetOrAddThing("WordsIWantToKnow", "Attention");
        if (wordsParent.Children.Count > 0)
        {
            Thing wordToLookUp = wordsParent.Children[0];
            string word = wordToLookUp.Label.Substring(1).ToLower();
            wordsParent.RemoveChild(wordToLookUp);
            Thing wordThing = UKS.Labeled(word);
            if (wordThing == null || wordThing.Parents.FindFirst(x => x.Label == "Object") != null) 
            {
                ModuleOnlineInfo info = (ModuleOnlineInfo)FindModule("OnlineInfo");
                info.GetChatGPTData(word);
                lastWordTime = DateTime.Now;
                Debug.WriteLine("Getting definition for: " + word);
            }
        }

        //if you want the dlg to update, use the following code whenever any parameter changes
        // UpdateDialog();
    }

    public void GetOnlineData(string word)
    {
        ModuleOnlineInfo info = (ModuleOnlineInfo)FindModule("OnlineInfo");
        if (info == null) return;

        //info.GetKidsDefinition(word);
        info.GetChatGPTData(word);
    }

    //fill this method in with code which will execute once
    //when the module is added, when "initialize" is selected from the context menu,
    //or when the engine restart button is pressed
    public override void Initialize()
    {
        GetOnlineData("cat");
    }

    //the following can be used to massage public data to be different in the xml file
    //delete if not needed
    public override void SetUpBeforeSave()
    {
    }
    public override void SetUpAfterLoad()
    {
    }

    //called whenever the size of the module rectangle changes
    //for example, you may choose to reinitialize whenever size changes
    //delete if not needed
    public override void SizeChanged()
    {
        if (mv == null) return; //this is called the first time before the module actually exists
    }
}

