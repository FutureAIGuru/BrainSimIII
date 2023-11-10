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
    public partial class ModuleEventDlg : ModuleBaseDlg
    {
        public ModuleEventDlg()
        {
            InitializeComponent();
        }

        public override bool Draw(bool checkDrawTimer)
        {
            if (!base.Draw(checkDrawTimer)) return false;
            ModuleEvent parent = (ModuleEvent)base.ParentModule;
            List<Thing> Situations = parent.SituationList();
            foreach (Thing s in Situations)
            {
                if (situationComboBox.Items.Contains(s) == false)
                    situationComboBox.Items.Add(s);

                if (outcomeComboBox.Items.Contains(s) == false)
                    outcomeComboBox.Items.Add(s);

                if (goalComboBox.Items.Contains(s) == false)
                    goalComboBox.Items.Add(s);

                if (startComboBox.Items.Contains(s) == false)
                    startComboBox.Items.Add(s);
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

        private void SetAction_Click(object sender, RoutedEventArgs e)
        {
            if (actionComboBox.SelectedItem != null)
            {
                string actionSelected = actionComboBox.SelectedItem.ToString();
                string[] temp = actionSelected.Split(' ');
                actionSelected = temp[1];
                if (temp.Count() > 2)
                {
                    for (int i = 2; i < temp.Count(); i++)
                    {
                        actionSelected += " ";
                        actionSelected += temp[i];
                    }
                }
                ModuleEvent parent = (ModuleEvent)base.ParentModule;
                parent.ActionButton(actionSelected);
            }
        }

        private void SetOutcome_Click(object sender, RoutedEventArgs e)
        {
            if (outcomeComboBox.SelectedItem != null)
            {
                string outcomeText = outcomeComboBox.Text.Split(":")[0]; ;
                ModuleEvent parent = (ModuleEvent)base.ParentModule;
                parent.OutcomeButton(outcomeText);
            }
        }

        private void CreateEventButton_Click(object sender, RoutedEventArgs e)
        {
            if (situationComboBox.SelectedItem != null)
            {
                Thing selectedThing = situationComboBox.SelectedItem as Thing;
                ModuleEvent parent = (ModuleEvent)base.ParentModule;
                parent.InitializeEvent(selectedThing);
            }
        }

        private void CreateSituation()
        {
            string label = situationText.Text;

            if (label != "")
            {
                ModuleEvent parent = (ModuleEvent)ParentModule;

                parent.GetOrAddSituation(label);

                List<Thing> Situations = parent.SituationList();
                foreach (Thing s in Situations)
                {
                    if (situationComboBox.Items.Contains(s) == false)
                        situationComboBox.Items.Add(s);

                    if (outcomeComboBox.Items.Contains(s) == false)
                        outcomeComboBox.Items.Add(s);

                    if (goalComboBox.Items.Contains(s) == false)
                        goalComboBox.Items.Add(s);

                    if (startComboBox.Items.Contains(s) == false)
                        startComboBox.Items.Add(s);
                }
            }
        }

        private void CreateSituationButton_Click(object sender, RoutedEventArgs e)
        {
            CreateSituation();
        }

        private void FindGoalButton_Click(object sender, RoutedEventArgs e)
        {
            ModuleEvent parent = (ModuleEvent)ParentModule;

            string goalSelection = goalComboBox.Text.Split(":")[0];
            string startSelection = startComboBox.Text.Split(":")[0];

            resultText.Text = parent.GoToGoalButton(goalSelection, startSelection);
        }

        private void SituationText_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) CreateSituation();
        }

        private void GoTowardGoalButton_Click(object sender, RoutedEventArgs e)
        {
            ModuleEvent parent = (ModuleEvent)ParentModule;

            string step = "";
            string goalSelection = goalComboBox.Text.Split(":")[0];
            string startSelection = startComboBox.Text.Split(":")[0];

            parent.FindTowardGoalButton(goalSelection, startSelection, out step);

            resultText.Text = "Path Taken:\n" + step;
        }



        private void SetCreateVisibility(Visibility newVisibility)
        {
            newSituationLabel.Visibility = newVisibility;
            situationText.Visibility = newVisibility;
            createSituationButton.Visibility = newVisibility;
            selectActionLabel.Visibility = newVisibility;
            actionComboBox.Visibility = newVisibility;
            setActionButton.Visibility = newVisibility;

            selectSituationLabel.Visibility = newVisibility;
            situationComboBox.Visibility = newVisibility;
            createEventButton.Visibility = newVisibility;
            selectOutcomeLabel.Visibility = newVisibility;
            outcomeComboBox.Visibility = newVisibility;
            setOutcomeButton.Visibility = newVisibility;
        }

        private void SetFindVisibility(Visibility newVisibility)
        {
            startLabel.Visibility = newVisibility;
            startComboBox.Visibility = newVisibility;
            findGoalLabel.Visibility = newVisibility;
            goalComboBox.Visibility = newVisibility;
            findGoalButton.Visibility = newVisibility;
            goTowardGoalButton.Visibility = newVisibility;
            resultText.Visibility = newVisibility;
        }
        private void CreateRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            SetFindVisibility(Visibility.Collapsed);
            SetCreateVisibility(Visibility.Visible);
        }

        private void FindRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            SetCreateVisibility(Visibility.Collapsed);
            SetFindVisibility(Visibility.Visible);
        }
    }
}
