//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace BrainSimulator.Modules
{
    public class ModuleWords2UKS : ModuleBase
    {
        //any public variable you create here will automatically be stored with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XmlIgnore] 
        //public theStatus = 1;

        //fill this method in with code which will execute
        //once for each cycle of the engine

        ModuleUKS uksModule = null;

        private bool ConnectUKSModule()
        {
            ModuleView naSource = MainWindow.theNeuronArray.FindModuleByLabel("UKS");
            if (naSource == null)
            {
                System.Windows.MessageBox.Show("Processing files requires a UKS module to be present.");
                return false;
            }
            uksModule = (ModuleUKS)naSource.TheModule;
            return true;
        }

        public override void Fire()
        {
            Init();  //be sure to leave this here
            ConnectUKSModule();
        }

        public ModuleWords2UKS()
        {
            minHeight = 2;
            minWidth = 2;
        }

        Dictionary<string, int> wordDictionary = new Dictionary<string, int>();

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
        }

        public void AddWords2UKS(string word, string nextWord, string language)
        {
            // better safe than sorry...
            word = Utils.TrimPunctuation(word).ToLower();
            nextWord = Utils.TrimPunctuation(nextWord).ToLower();

            // Determine which Language section we will interact with...
            // TODO: make it properly capitalized just in case. 
            Thing theLanguage = uksModule.GetOrAddThing(language, "Language");

            // Add the relationship things...
            Thing hasWord = uksModule.GetOrAddThing("has word", "Relationship");
            Thing isLanguage = uksModule.GetOrAddThing("is language", "Relationship");
            Thing follows = uksModule.GetOrAddThing("follows", "Relationship");
            Thing followedBy = uksModule.GetOrAddThing("followed by", "Relationship");

            // Try finding the word thing...
            Thing theWord = uksModule.GetOrAddThing(word, theLanguage);

            // Try finding the next word too...
            Thing theNextWord = uksModule.GetOrAddThing(nextWord, theLanguage);

            // Make or strengthen the link between the two... 
            if (word != "" && nextWord != "")
            {
                theWord.AddRelationship(theNextWord, followedBy);
                theNextWord.AddRelationship(theWord, follows);
            }
            if (word != "" && nextWord == "")
            {
                theLanguage.AddRelationship(theWord, hasWord);
                theWord.AddRelationship(theLanguage, isLanguage);
                AddWordRecognitionRelationships(theWord.Label);
            }
            if (nextWord != "")
            {
                theLanguage.AddRelationship(theNextWord, hasWord);
                theNextWord.AddRelationship(theLanguage, isLanguage);
                AddWordRecognitionRelationships(theNextWord.Label);
            }
        }

        public void AddWordRecognitionRelationships(string word)
        {
            if (word == "") return;

            // Add categories for word recognition
            Thing recognition = uksModule.GetOrAddThing("Recognition", "Thing");
            Thing words = uksModule.GetOrAddThing("Words", "Recognition");
            Thing letters = uksModule.GetOrAddThing("Letters", "Recognition");

            // Add relationship categories
            Thing startsWith = uksModule.GetOrAddThing("starts with", "Relationship");
            Thing endsWith = uksModule.GetOrAddThing("ends with", "Relationship");
            Thing contains = uksModule.GetOrAddThing("contains", "Relationship");

            // Add the specifics of this word
            Thing theWord = uksModule.GetOrAddThing(word, "Words");
            Thing theFirstLetter = uksModule.GetOrAddThing(word.Substring(0, 1), "Letters");
            Thing theLastLetter = uksModule.GetOrAddThing(word.Substring(word.Length - 1, 1), "Letters");

            theWord.AddRelationship(startsWith, theFirstLetter);
            theWord.AddRelationship(endsWith, theLastLetter);
            for (int i = 0; i < word.Length; i++)
            {
                Thing theLetter = uksModule.GetOrAddThing(word.Substring(i, 1), "Letters");
                theWord.AddRelationship(contains, theLetter);
            }
        }
    }
}
