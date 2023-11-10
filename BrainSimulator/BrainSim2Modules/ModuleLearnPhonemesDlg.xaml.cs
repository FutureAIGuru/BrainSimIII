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

namespace BrainSimulator.Modules
{
    public partial class ModuleLearnPhonemesDlg : ModuleBaseDlg
    {
        public ModuleLearnPhonemesDlg()
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

            return true;
        }

        private void TheCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            ModuleLearnPhonemes parent = (ModuleLearnPhonemes)base.ParentModule;
            string filePath = @$"{FilePathTextBox.Text}";


            if (!File.Exists(filePath))
            {
                System.Windows.Forms.MessageBox.Show("Invalid entry, please enter a valid file path");
                return;
            }

            int delimitSize;
            try
            {
                if (string.IsNullOrEmpty(DelimitSize.Text))
                {
                    System.Windows.Forms.MessageBox.Show("Invalid entry, please enter a number");
                    return;
                }
                delimitSize = int.Parse(DelimitSize.Text);
            }
            catch (Exception)
            {
                System.Windows.Forms.MessageBox.Show("Invalid entry, please enter a number");
                return;
            }


            try
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;
                LearnedPhonemesTextBox.Text = parent.RunFileThroughLearnedAsString(filePath, delimitSize);
            }
            finally
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default;
            }
        }
    }
}
