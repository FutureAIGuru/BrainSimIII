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
    public partial class ModuleSpeechInDlg : ModuleBaseDlg
    {
        public ModuleSpeechInDlg()
        {
            InitializeComponent();
        }

        public override bool Draw(bool checkDrawTimer)
        {
            if (!base.Draw(checkDrawTimer)) return false;
            ModuleSpeechIn parent = (ModuleSpeechIn)base.ParentModule;
            if (parent != null)
            {
                if (!parent.speechEnabled)
                {
                    cbEnabled.IsChecked = false;
                }
            }
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

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;
                ModuleSpeechIn parent = (ModuleSpeechIn)base.ParentModule;
                parent.ReceiveInputFromText(TypedEntryTextBox.Text);                
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            ModuleSpeechIn parent = (ModuleSpeechIn)base.ParentModule;
            if (parent == null) return;
            parent.speechEnabled = true;
            parent.ResumeRecognition();
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            ModuleSpeechIn parent = (ModuleSpeechIn)base.ParentModule;
            if (parent == null) return;
            parent.speechEnabled = false;
            parent.PauseRecognition();
        }
    }
}
