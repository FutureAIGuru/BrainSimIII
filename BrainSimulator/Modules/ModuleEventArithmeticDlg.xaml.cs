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
    public partial class ModuleEventArithmeticDlg : ModuleBaseDlg
    {

        public ModuleEventArithmeticDlg()
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

        private void LearnButton_Click(object sender, RoutedEventArgs e)
        {
            ModuleEventArithmetic eventArithmetic = (ModuleEventArithmetic)base.ParentModule;

            int firstInt = System.Int32.Parse(firstNumberText.Text);
            int secondInt = System.Int32.Parse(secondNumberText.Text);

            eventArithmetic.Learn(firstInt, secondInt);

            firstNumLabel.Text = firstNumberText.Text;
            secondNumLabel.Text = secondNumberText.Text;
            additionResultText.Text = (firstInt + secondInt).ToString();
            multiplyResult.Text = (firstInt * secondInt).ToString();
        }

        private void populateButton_Click(object sender, RoutedEventArgs e)
        {
            ModuleEventArithmetic eventArithmetic = (ModuleEventArithmetic)base.ParentModule;

            eventArithmetic.Populate(System.Int32.Parse(minTextBox.Text), System.Int32.Parse(maxTextBox.Text));
        }
    }
}
