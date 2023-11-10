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
    public partial class ModuleSituationNavigationDlg : ModuleBaseDlg
    {
        public ModuleSituationNavigationDlg()
        {
            InitializeComponent();
        }

        public string curError = "";
        public override bool Draw(bool checkDrawTimer)
        {
            if (!base.Draw(checkDrawTimer)) return false;
            UpdateErrorMsg(curError);
            return true;
        }

        private void TheGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }

        private void GoToButton_Click(object sender, RoutedEventArgs e)
        {
            ModuleSituationNavigation parent = (ModuleSituationNavigation)base.ParentModule;

            //parent.goalStr = goalText.Text;
            //string errorMessage = parent.GoToLandmarkGroups();
            parent.EnterNavigationMode("LM_" + goalText.Text);

            //if (errorMessage == "")
            //    errorText.ClearValue(Control.ForegroundProperty);
            //else
            //    errorText.Foreground = new SolidColorBrush(Colors.Pink);

            //errorText.Text = errorMessage;
        }

        private void GoalText_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ModuleSituationNavigation parent = (ModuleSituationNavigation)base.ParentModule;

                //parent.goalStr = goalText.Text;
                //string errorMessage = parent.GoToLandmarkGroups();
                parent.EnterNavigationMode("LM_" + goalText.Text);

                //if (errorMessage == "")
                //    errorText.ClearValue(Control.ForegroundProperty);
                //else
                //    errorText.Foreground = new SolidColorBrush(Colors.Pink);

                //errorText.Text = errorMessage;
            }
        }

        public void UpdateErrorMsg(string newError)
        {
            errorText.Text = newError;

            if (errorText.Text == "")
                errorText.ClearValue(Control.ForegroundProperty);
            else
                errorText.Foreground = new SolidColorBrush(Colors.Pink);
        }

        private void StartExploreButton_Click(object sender, RoutedEventArgs e)
        {
            ModuleSituationNavigation parent = (ModuleSituationNavigation)base.ParentModule;
            parent.EnterExploreMode();
        }

        private void stopExploreButton_Click(object sender, RoutedEventArgs e)
        {
            ModuleSituationNavigation parent = (ModuleSituationNavigation)base.ParentModule;
            parent.ExitExploreMode();
        }
    }
}