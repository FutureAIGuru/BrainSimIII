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
using static BrainSimulator.Modules.ModuleSpeakPhonemes;
using static BrainSimulator.Modules.ModuleLearnSequence;
using System.Text.RegularExpressions;

namespace BrainSimulator.Modules
{
    //TODO: add connection between module neuron board and the stored sequence. Then take action: use LearnSequence
    public class ModuleLearnPhonemes : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XlmIgnore] 
        //public theStatus = 1;

        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleLearnPhonemes()
        {
            minHeight = 5;
            maxHeight = 5;
            minWidth = 8;
            maxWidth = 8;
        }


        //fill this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            Init();  //be sure to leave this here

            //if you want the dlg to update, use the following code whenever any parameter changes
            UpdateDialog();
        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
            SetUpLablesForModule();
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
            Initialize();
        }

        public void SetUpLablesForModule()
        {
            // TODO: get list of all phonemes, not just english
            string[] phonemesArray = { "a", "ɑ", "æ", "b", "ɔ", "d", "ð", "e", "ə", "ɛ", "ɜ", "f", "g", "h", "i", "ɪ", "j", "k", "l", "m", "n", "ŋ", "o", "p", "ɻ", "s", "ʃ", "t", "u", "ʊ", "v", "ʌ", "w", "z", "ʒ", "θ", "\u0361" };

            foreach (string phonemes in phonemesArray)
            {
                AddLabel(phonemes);
            }

        }

        public static int FindIndexOfSequenceInList(List<NeuronSequence> storedSequences, NeuronSequence sequence)
        {
            for (int i = 0; i < storedSequences.Count; i++)
            {
                bool match = true;
                if (storedSequences[i].sequence.Count == sequence.sequence.Count)
                {
                    for (int j = 0; j < storedSequences[i].sequence.Count; j++)
                    {
                        if (!storedSequences[i].sequence[j].NeuronID.Equals(sequence.sequence[j].NeuronID, StringComparison.Ordinal))
                        {
                            match = false;
                            break;
                        }
                    }
                    if (match) return i;
                }
            }

            return -1;
        }

        public static List<NeuronSequence> LoadPhonemesArrayIntoNeuronSequenceList(string[] phonemeArray)
        {


            List<NeuronSequence> storedSequences = new();

            // load phoneme words as NeuronSequence into list
            for (int i = 0; i < phonemeArray.Length; i++)
            {
                NeuronSequence sequence = new();
                // load indvidual phonemes of words into NeuronSequence
                for (int j = 0; j < phonemeArray[i].Length; j++)
                {
                    sequence.AddToSequence(phonemeArray[i][j].ToString());
                }

                int index = FindIndexOfSequenceInList(storedSequences, sequence);
                if (index == -1)
                {
                    storedSequences.Add(sequence);
                }
                else
                {
                    storedSequences[index].useCount++;
                }
            }

            SortStoredSequencesByUseCount(storedSequences);

            return storedSequences;
        }



        public static string NeuronListToString(List<NeuronSequence> storedSequences)
        {
            // to print to dialog
            StringBuilder sb = new();
            sb.AppendLine("Learned   Uses   Sequence");

            for (int i = 0; i < storedSequences.Count; i++)
            {
                if (storedSequences[i].GetUseCount() >= 1)
                {
                    sb.AppendLine($"{i}\t{storedSequences[i].GetUseCount()}\t{storedSequences[i].GetSequenceAsString()}");
                }
            }

            return sb.ToString();
        }

        public string RunFileThroughLearnedAsString(string filePath, int delimitSize)
        {
            string[] phonemeArray = FileToStringArrayOfPhonemes(filePath, delimitSize);
            List<NeuronSequence> storedSequences = LoadPhonemesArrayIntoNeuronSequenceList(phonemeArray);
            return NeuronListToString(storedSequences);
        }

        public static string[] FileToStringArrayOfPhonemes(string filePath, int delimitSize)
        {
            string fileString;
            using (StreamReader streamReader = File.OpenText(filePath))
                fileString = streamReader.ReadToEnd().ToLower();

            fileString = RemoveNotLettersOrSpaces(fileString);

            string[] wordArray = fileString.Split(' ');
            string wordsAsPhonemes = "";

            foreach (var word in wordArray)
            {
                // GetPronunciationFromText errors at about 380 chars, doing one word at a time
                //TODO: look into this function. current bottlenck, noticeably slow
                string pronounciation = GetPronunciationFromText(word);
                wordsAsPhonemes += pronounciation;
            }

            wordsAsPhonemes = wordsAsPhonemes.Trim();

            if (delimitSize <= 0)
            {
                return wordsAsPhonemes.Split(' ');
            }

            return DelimitBySize(wordsAsPhonemes, delimitSize);
        }


        public static string RemoveSpecialCharacters(string input)
        {
            if (input is null)
            {
                return input;
            }

            return new string(input.ToCharArray()
                .Where(c => char.IsLetter(c))
                .ToArray());
        }

        public static string RemoveNotLettersOrSpaces(string input)
        {
            if (input is null)
            {
                return input;
            }

            input = new string(input.ToCharArray()
                .Where(c => char.IsLetter(c) || char.IsWhiteSpace(c))
                .ToArray());


            input = Regex.Replace(input, @"\s+", " ");

            return input;
        }

        public static string[] DelimitBySize(string input, int size)
        {
            // break string into an array of strings broken up by index of size

            // enforce min size of 1
            if (size < 1)
            {
                size = 1;
            }

            if (input is null)
            {
                return new string[] { "" };
            }

            input = Regex.Replace(input, @"\s+", "");

            // add space at each size index
            for (int i = 0; i < input.Length - 1; i += size + 1)
            {
                int distance = i + size;
                if ((i + size) > input.Length - 1) // avoids out of index exception
                {
                    distance = input.Length;
                }

                string begining;
                string middle;
                string end;

                if (i == 0) // first input size
                {
                    begining = input[0..i];
                    middle = input[i..distance] + " ";
                    end = input[distance..input.Length];
                }
                else // remaining input size
                {
                    begining = input[0..i];
                    middle = input[(begining.Length)..distance] + " ";
                    end = input[distance..input.Length];
                }


                input = (begining + middle + end).Trim(); // trim to remove space at end of string
            }

            if (!input.Contains(' ', StringComparison.Ordinal)) // no spaces means that it will only have one index for the array (possibly empty)
            {
                return new string[] { input };
            }

            return input.Split(' ');
        }
    }
}
