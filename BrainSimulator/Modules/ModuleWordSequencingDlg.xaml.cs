//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace BrainSimulator.Modules
{
    public partial class ModuleWordSequencingDlg : ModuleBaseDlg
    {
        public ModuleWordSequencingDlg()
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

            return true;
        }

        private void TheGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }

        private void findMatchButton_Click(object sender, RoutedEventArgs e)
        {
            findMatch();
        }

        private void sequenceStartTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                findMatch();
        }

        private void findMatch()
        {
            List<string> sequenceStart = sequenceStartTextBox.Text.Split(' ').ToList();
            ModuleWordSequencing parent = (ModuleWordSequencing)ParentModule;
            List<Thing> sequenceMatches = parent.PredictSequence(sequenceStart);
            sequenceMatches.Sort((x,y) => y.useCount.CompareTo(x.useCount));
            string output = "";

            if (sequenceMatches.Count == 0)
            {
                output = "*No match found*";
            }
            else
            {
                foreach (Thing match in sequenceMatches)
                {
                    if (output != "") output += "\n";
                    output += match.Label + ":";
                    foreach (Thing word in match.RelationshipsAsThings)
                    {
                        output += " " + word.Label;
                    }
                    output += " Uses: " + match.useCount;
                }
            }

            resultTextBlock.Text = output;
        }
    }
}