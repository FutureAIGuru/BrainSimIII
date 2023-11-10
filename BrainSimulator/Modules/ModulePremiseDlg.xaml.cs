//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using Microsoft.VisualBasic;
using SharpDX.Multimedia;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace BrainSimulator.Modules
{
    public partial class ModulePremiseDlg : ModuleBaseDlg
    {
        public ModulePremiseDlg()
        {
            InitializeComponent();
        }

        public override bool Draw(bool checkDrawTimer)
        {
            if (!base.Draw(checkDrawTimer)) return false;
            //this has a timer so that no matter how often you might call draw, the dialog
            //only updates 10x per second

            //use a line like this to gain access to the parent's public variables
            //ModuleEmpty parent = (ModuleEmpty)base.ParentModule;

            //here are some other possibly-useful items
            //theGrid.Children.Clear();
            //Point windowSize = new Point(theGrid.ActualWidth, theGrid.ActualHeight);
            //Point windowCenter = new Point(windowSize.X / 2, windowSize.Y / 2);
            //float scale = (float)Math.Min(windowSize.X, windowSize.Y) / 12;
            //if (scale == 0) return false;
            GetSyllogismButton.IsDefault = true;
            return true;
        }
        private void TheGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }


        //This module is currently being worked on.In its current state it takes in a thing (eg.animal) for the (1) box , known as a term, and another thing(eg.lifeform) in box(2),
        //a copula(verb) is selected(--> or<->) as a whole statement or relationship.Then the "Get Syllogism" button is clicked to retrieve the resulting statement.NOTE:  Not all
        //the boxes and buttons do something.This is due to a previous itteration.However, they will not break the system and they may be used again in the future.
        //way all this currently works is that a relationship is created from the input text and copula, then it is put through ModulePremise (see line 106 of the Dlg).  From here,
        //each thing in the Object tree is then itterated over the input relationship and then the reasoning steps are performed and stored in a Syllogism table from
        //GeneratePremise.GeneratePremise takes in two relationships and generates a list of syllogisms by way of the reasoning steps(like deduction, abduciton, ..., etc.).  Each
        //sysllogism will be generated for a given relationship, but some will be null because they did not meet the syllogism requirements.Null syllogisms will be discarded while
        //all other syllogisms will be stored in the list for each relationship within the list for each thing.  All these will then be outputed into the text box underneath
        //statement #1 and #2.  

        private void GetSyllogismButton_Click(object sender, RoutedEventArgs e)
        {
            ModulePremise mp = (ModulePremise)base.ParentModule;
            mp.GetUKS();
            //Checkes objects
            StatementDerivedLabel.Items.Clear();
            //Gets the sentence type from each text box, i.e. '.' = belief, '!' = goal, and '?' = query
            List<string> sentence = new List<string>();
            GetSentenceAsList(ref sentence);
            if (sentence.Count < 3 || sentence.Count > 5)
            {
                OutputError();
                return;
            }
            char sentType = new char();
            if (sentence.Last().Last() == '.')
            {
                sentType = sentence.Last().Last();
                sentence[sentence.Count - 1] = sentence.Last().Remove(sentence.Last().Length - 1);
            }
            else
                sentType = '.';
            //Correct the indefinite articles for the determiners 'an' and 'a' to just 'a'
            //Get the plural of a thing
            //Get term one
            string termOne = (sentence[0] == "a" || sentence[0] == "an" || sentence[0] == "the") ? sentence[1] : sentence[0];
            //Get copula
            string copula = (sentence[0] == "a" || sentence[0] == "an" || sentence[0] == "the") ? (((sentence[3] == "a" || sentence[3] == "an" || sentence[3] == "the") &&
                sentence[2] == "is") ?
                mp.UKS.Labeled("has-child").Label : sentence[2]) : ((sentence[2] == "a" || sentence[2] == "an" || sentence[2] == "the") && sentence[1] == "is" ?
                mp.UKS.Labeled("has-child").Label : sentence[1]);
            //Get term two
            string termTwo = sentence[sentence.Count() - 1];
            //Get the sentence type
            Thing relationshp = mp.UKS.GetOrAddThing("Relationship", "Thing");
            Thing implies = mp.UKS.GetOrAddThing(copula, "Relationship");
            Thing similarto = mp.UKS.GetOrAddThing("<->", "Relationship");

            sentence[sentence.Count - 1] = sentence.Last().Remove(sentence.Last().Length - 1);
            ModuleThought mt = new ModuleThought();
            Thing t2 = mp.UKS.Labeled(termOne);
            Thing t1 = mp.UKS.Labeled(termTwo);
            Relationship r1 = new()
            {
                relType = implies,
                sentencetype = new SentenceType(sentType.ToString() != "." ? "." : sentType.ToString(),
                            ((bool)_1_current.IsChecked) ? new Tense(DateAndTime.Now) { tense = DateAndTime.Now } : new Tense(),
                            new TruthValue() /*, new Desire()*/)
                { }
            };
            if (copula == "has-child")
            {
                if (t2 == null)
                {

                    t1 = mp.UKS.GetOrAddThing(termTwo, "Object");
                    t2 = mp.UKS.GetOrAddThing(termOne, t1.Label);
                    r1.source = t1;
                    r1.T = t2;
                    r1 = t1.HasRelationship(t2);
                }
                else
                {
                    if (t1 == null)
                        t1 = mp.UKS.GetOrAddThing(termOne, t2.Label);
                    else
                    {
                        r1.source = t1;
                        r1.T = t2;
                    }
                }
            }
            else
            {

                if (t1 == null)
                    t1 = mp.UKS.GetOrAddThing(termTwo, "unknownObject");
                if (t2 == null)
                {
                    t2 = mp.UKS.GetOrAddThing(termOne, "unknownObject");
                    r1 = t2.AddRelationship(t1, implies, new SentenceType()
                    {
                        belief = new SentenceType.Belief()
                        {
                            TRUTH = new TruthValue(),
                            Tense = ((bool)_1_current.IsChecked) ? new Tense(DateAndTime.Now) { tense = DateAndTime.Now } : new Tense()
                        }
                    });
                }
                //r1 = t2.AddRelationship(t1, implies, new SentenceType(sentType.ToString() != "." ? "." : sentType.ToString()));
                /*                    r1 = new Relationship()
                                    {
                                        relationshipType = implies,
                                        // little hack for now that only that only takes in eternal statements
                                        sentencetype = new SentenceType(sentType.ToString() != "." ? "." : sentType.ToString(),
                                                                        ((bool)_1_current.IsChecked) ? new Tense(DateAndTime.Now) { tense = DateAndTime.Now } : null,
                                                                        new TruthValue() *//*, new Desire()*//*)
                                        { }
                                    };
                                    */


                r1.source = t2;
                r1.T = t1;

            }

            /*            t2 = (copula == "has-child") ? df.LocalGetOrAddThing(termTwo, "Object") : df.LocalGetOrAddThing(termOne, "Object");
                        t1 = (copula == "has-child") ? df.LocalGetOrAddThing(termOne, t2.Label) : df.LocalGetOrAddThing(termTwo, "Object");*/



            //ModulePremise modprem = new ModulePremise(r1, r2);
            //int cycles = 1;
            int cycles = 1;
            ModulePremise modprem = new ModulePremise();
            if (t1 != null && t2 != null)
                modprem = new ModulePremise(r1, cycles);
            if (modprem.CycleStatements.Count != 0)
            {
                int tt = modprem.CycleStatements.Count;
                for (int i = 0; i < modprem.CycleStatements.Count; i++)
                {
                    for (int j = 0; j < modprem.CycleStatements[i].Count; j++)
                    {
                        for (int k = 0; k < modprem.CycleStatements[i][j].Count; k++)
                        {
                            for (int l = 0; l < modprem.CycleStatements[i][j][k].Count; l++)
                            {
                                if (modprem.CycleStatements[i][j][k].Count != 0)
                                {
                                    // c=cycle, t=thing, r=relationship, l=reasoning step
                                    StatementDerivedLabel.Items.Add("<" + (modprem.CycleStatements[i][j][k][l].Item1.source as Thing).Label + " " +
                                    modprem.CycleStatements[i][j][k][l].Item1.relType + " " +
                                    (modprem.CycleStatements[i][j][k][l].Item1.T as Thing).Label + " " + "> <" +
                                    "F:" + modprem.CycleStatements[i][j][k][l].Item1.sentencetype.belief?.TRUTH.F.ToString() + ", " +
                                    "C:" + modprem.CycleStatements[i][j][k][l].Item1.sentencetype.belief?.TRUTH.C.ToString() + ", " +
                                    "T:" + modprem.CycleStatements[i][j][k][l].Item1.sentencetype.belief?.TRUTH.EXPECT.ToString() + ">" +
                                    modprem.CycleStatements[i][j][k][l].Item2.ToString() + "c:" + i + "t:" + j + "r:" + k + "p:" + l);
                                }
                            }
                        }
                    }
                }
                //Relationship derivedRelationshp = modprem.Statements[0].term1.AddRelationship(modprem.Statements[0].term2, modprem.Statements[0].copula);
            }
            int ksda = 0;
        }

        private Relationship SetupRelationship(string item1, string item2)
        {
            ModulePremise mfc = (ModulePremise)ParentModule;
            mfc.GetUKS();
            Relationship relationship = new Relationship();
            //Right now, we are assumning parent-child relationships.  UPDATE THIS LATER TO INCORPERATE ALL OTHER TYPES
            Thing t = mfc.UKS.GetOrAddThing(item1, item2);

            return t.Relationships[0];
        }

        private void GetSentenceAsList(ref List<string> sentence)
        {
            string curString = "";
            int count = 0;
            string theText = FirstTextBox.Text;
            if (theText != "")
            {
                if (theText.Last() != '.')
                {
                    theText += ".";
                }
            }
            else
            {
                OutputError();
            }

            string[] theWords = theText.Split(' ');

            sentence = theWords.ToList();

        }

        private void OutputError()
        {
            StatementDerivedLabel.Items.Add("Please, enter a valid sentence of the form 'term verb term'.\n " +
                                            "Sentences like 'a dog is an animal' or 'the house is big' all work.");
        }

        private void TermOne_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void TermTwo_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void TermThree_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void TermFour_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void AnalogyButton_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void DeductionButton_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void ResemblanceButton_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void AbductionRadio_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void IndictionRadio_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void ExemplificationButton_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void ComparisonButton_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void IntersectionButton_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void UnionButton_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void DifferenceButton_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void RevisonButton_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void implySecondRadio_Checked(object sender, RoutedEventArgs e)
        {

        }
    }
}