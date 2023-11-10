//
// Copyright (c) FutureAI. All rights reserved.  
// Code is proprietary and may not be distributed without a separate licence
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
    public partial class ModuleNLPDlg : ModuleBaseDlg
    {
        public ModuleNLPDlg()
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
            //theCanvas.Children.Clear();
            //Point windowSize = new Point(theCanvas.ActualWidth, theCanvas.ActualHeight);
            //Point windowCenter = new Point(windowSize.X / 2, windowSize.Y / 2);
            //float scale = (float)Math.Min(windowSize.X, windowSize.Y) / 12;
            //if (scale == 0) return false;

            ModuleNLP parent = (ModuleNLP)base.ParentModule;
            if (parent == null) return false;
            string phrase = parent.Phrase;

            InputPhraseTextBox.Text = phrase;
            if (parent.r != null)
                RelationshipTextBox.Text = parent.r.ToString();


            string NLPResults = "INDEX\tTEXT\t\tLEMMA\t\tDEP\t\tPOS\tTAG\tHEAD\tMORPH\n";
            foreach (var item in parent.nlpResults)
            {
                NLPResults += item.ToString("\t") + "\n";
            }
            NLPResultsTextBox.Text = NLPResults;

            return true;
        }

        private void TheCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }

        private void NLPOnlyCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            ModuleNLP parent = (ModuleNLP)base.ParentModule;
            if (sender is CheckBox cb)
                parent.nlpOnly = (bool)cb.IsChecked;
        }
    }
}
