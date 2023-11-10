//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Cursors = System.Windows.Forms.Cursors;

namespace BrainSimulator.Modules
{
    public partial class ModuleWords2UKSDlg : ModuleBaseDlg
    {
        string textFileName = "no file selected";
        ModuleUKS uksModule = null;
        ModuleWords2UKS wordsModule = null;

        public ModuleWords2UKSDlg()
        {
            InitializeComponent();
            tbSelectedFile.IsReadOnly = true;
            tbSelectedFile.Text = textFileName;
        }

        public override bool Draw(bool checkDrawTimer)
        {
            if (!base.Draw(checkDrawTimer)) return false;
            //this has a timer so that no matter how often you might call draw, the dialog
            //only updates 10x per second

            //use a line like this to gain access to the parent's public variables
            //ModuleEmpty parent = (ModuleEmpty)base.ParentModule;

            //here are some other possibly-useful items
            //theCanvas.Children.Clear();
            //Point windowSize = new Point(theCanvas.ActualWidth, theCanvas.ActualHeight);
            //Point windowCenter = new Point(windowSize.X / 2, windowSize.Y / 2);
            //float scale = (float)Math.Min(windowSize.X, windowSize.Y) / 12;
            //if (scale == 0) return false;

            return true;
        }

        private void TheCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // This renews the dialog's connection to the UKS module, or presents a MessageBox on failure.
        private bool ConnectUKSModule()
        {
            ModuleView naSource = MainWindow.theNeuronArray.FindModuleByLabel("UKS");
            if (naSource == null)
            {
                System.Windows.MessageBox.Show("Processing files requires a UKS module with name UKSN to be present.");
                return false;
            }
            uksModule = (ModuleUKS)naSource.TheModule;
            return true;
        }

        // This renews the dialog's connection to the Words2UKS module, or presents a MessageBox on failure.
        private bool ConnectWordsModule()
        {
            wordsModule = (ModuleWords2UKS)base.ParentModule;
            return (wordsModule != null);
        }

        private void ButtonAddFile_Click(object sender, RoutedEventArgs e)
        {
            tbSelectedFile.Text = "no file selected";
            DialogResult result = System.Windows.Forms.DialogResult.Cancel;
            OpenFileDialog openFileDialog1 = new OpenFileDialog
            {
                Filter = "TXT Command Files|*.txt",
                Title = "Select a Text File to Read into the UKS"
            };
            // Show the Dialog.  
            // If the user clicked OK in the dialog  
            System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
            {
                result = openFileDialog1.ShowDialog();
            });
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                textFileName = openFileDialog1.FileName;
                tbSelectedFile.Text = textFileName;
            }
 
            if (ConnectUKSModule() == false) return;
            if (ConnectWordsModule() == false) return;

            // Just in case this is not created yet....
            Thing relationShip = uksModule.GetOrAddThing("Relationship", "Thing");

            uksModule.GetOrAddThing("Language", "Thing");
            uksModule.GetOrAddThing("Unknown", "Language");
            uksModule.GetOrAddThing("English", "Language");
            uksModule.GetOrAddThing("French", "Language");
            uksModule.GetOrAddThing("German", "Language");
            uksModule.GetOrAddThing("Dutch", "Language");
        }

        private void buttonProcessFile_Click(object sender, RoutedEventArgs e)
        {
            if (textFileName != "")
            {
                string selectedLanguage = cbSelectedLanguage.Text.ToString();
                string[] lines = File.ReadAllLines(textFileName);

                foreach (string s in lines)
                {
                    string[] s2 = s.Split(' ');
                    string word = "";
                    foreach (string nextWord in s2)
                    {
                        string cleanWord = Utils.TrimPunctuation(word).ToLower();
                        string cleanNextWord = Utils.TrimPunctuation(nextWord).ToLower();
                        wordsModule.AddWords2UKS(word, nextWord, selectedLanguage);
                        word = cleanNextWord;
                    }
                }
            }
            tbSelectedFile.Text = "Processed, please wait for UKS to update";
        }
    }
}
