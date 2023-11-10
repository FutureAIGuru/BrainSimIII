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
    public partial class ModuleUKSInteractDlg : ModuleBaseDlg
    {
        private bool validParent = false;

        public ModuleUKSInteractDlg()
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

        private void TheGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateUKS();
        }

        private void TheGrid_Loaded(object sender, RoutedEventArgs e)
        {
            addThingRadio.IsChecked = true;
        }

        private void UpdateUKS()
        {
            ModuleUKSInteract uksInteract = (ModuleUKSInteract)ParentModule;
            errorText.Text = "";

            if (addThingRadio.IsChecked == true)
            {
                string newThing = thingText.Text;
                string parent = parentText.Text;

                if (newThing == "" || parent == "")
                {
                    errorText.Text = "Fill all entry fields.";
                    return;
                }

                if (checkIsParentValid() == false)
                {
                    errorText.Text = "Parent does not exist.";
                    return;
                }

                if (uksInteract.AddChildButton(newThing, parent) == false)
                {
                    errorText.Text = "Thing already exists.";
                    return;
                }
            }
            else if (addReferenceRadio.IsChecked == true)
            {
                string thing = thingText.Text;
                string parent = parentComboBox.Text;
                string reference = referenceText.Text;
                string relType = relationshipTypeTextBox.Text;
                string referenceParent = referenceParentComboBox.Text;
                string[] modifiersText = ModifiersTextBox.Text.Split("\r\n");

                if (thing == "" || parent == "" || reference == "" || referenceParent == "")
                {
                    errorText.Text = "Fill all entry fields.";
                    return;
                }

                uksInteract.AddReference(thing, parent, reference, relType,referenceParent,modifiersText);

                UpdateParentBox();
                UpdateReferenceParentBox();
            }
            else //delete thing
            {
                string thing = thingText.Text;
                string parent = parentComboBox.Text;

                if (thing == "" || parent == "")
                {
                    errorText.Text = "Fill all entry fields.";
                    return;
                }

                if (uksInteract.DeleteThing(thing, parent) == false)
                {
                    errorText.Text = "Cannot delete a thing with children.";
                    return;
                }

                UpdateParentBox();
            }
        }

        private void UpdateParentBox()
        {
            if (deleteThingRadio.IsChecked == true || addReferenceRadio.IsChecked == true)
            {
                parentComboBox.Items.Clear();

                ModuleUKSInteract uksInteract = (ModuleUKSInteract)ParentModule;

                //repopulate selections

                List<Thing> thingParents = uksInteract.ParentsOfLabel(thingText.Text);

                if (thingParents == null) return;
                foreach (var parent in thingParents)
                {
                    parentComboBox.Items.Add(parent.Label);
                }

                if (parentComboBox.Items.Count == 1)
                    parentComboBox.SelectedIndex = 0;
            }
        }

        private void UpdateReferenceParentBox()
        {
            referenceParentComboBox.Items.Clear();

            ModuleUKSInteract uksInteract = (ModuleUKSInteract)ParentModule;

            //repopulate selections

            List<Thing> referenceParents = uksInteract.ParentsOfLabel(referenceText.Text);

            if (referenceParents == null) return;
            foreach (var parent in referenceParents)
            {
                referenceParentComboBox.Items.Add(parent.Label);
            }

            if (referenceParentComboBox.Items.Count == 1)
                referenceParentComboBox.SelectedIndex = 0;
        }
        private void updateThingBox()
        {
            if (addThingRadio.IsChecked == false)
            {
                ModuleUKSInteract uksInteract = (ModuleUKSInteract)ParentModule;

                if (uksInteract.SearchLabelUKS(thingText.Text) == null || uksInteract.isRoot(thingText.Text))
                    thingText.Background = new SolidColorBrush(Colors.Pink);
                else
                    thingText.ClearValue(Control.BackgroundProperty);
            }
        }

        private void updateReferenceBox()
        {
            ModuleUKSInteract uksInteract = (ModuleUKSInteract)ParentModule;

            if (uksInteract.SearchLabelUKS(referenceText.Text) == null || uksInteract.isRoot(referenceText.Text))
                referenceText.Background = new SolidColorBrush(Colors.Pink);
            else
                referenceText.ClearValue(Control.BackgroundProperty);
        }

        private void AddThingRadio_Checked(object sender, RoutedEventArgs e)
        {
            thingText.ClearValue(Control.BackgroundProperty);

            //disable reference action
            referenceLabel.Visibility = Visibility.Collapsed;
            referenceParentLabel.Visibility = Visibility.Collapsed;
            referenceText.Visibility = Visibility.Collapsed;
            referenceParentComboBox.Visibility = Visibility.Collapsed;
            relationshipTypeTextBox.Visibility = Visibility.Collapsed;
            relationshipTypeLabel.Visibility = Visibility.Collapsed;
            ModifiersTextBox.Visibility = Visibility.Collapsed;

            //disable delete action
            parentComboBox.Visibility = Visibility.Collapsed;

            //enable add thing action
            parentText.Visibility = Visibility.Visible;

            updateButton.Content = "Add Thing";

            errorText.Text = "";
        }

        private void AddReferenceRadio_Checked(object sender, RoutedEventArgs e)
        {
            referenceLabel.Visibility = Visibility.Visible;
            referenceParentLabel.Visibility = Visibility.Visible;
            referenceText.Visibility = Visibility.Visible;
            referenceParentComboBox.Visibility = Visibility.Visible;
            relationshipTypeTextBox.Visibility = Visibility.Visible;
            relationshipTypeLabel.Visibility = Visibility.Visible;
            ModifiersTextBox.Visibility = Visibility.Visible;

            parentText.Visibility = Visibility.Collapsed;

            parentComboBox.Visibility = Visibility.Visible;

            updateButton.Content = "Add Relationship";

            errorText.Text = "";

            updateThingBox();
            updateReferenceBox();
            UpdateParentBox();
            UpdateReferenceParentBox();
        }

        private void DeleteThingRadio_Checked(object sender, RoutedEventArgs e)
        {
            referenceLabel.Visibility = Visibility.Collapsed;
            referenceParentLabel.Visibility = Visibility.Collapsed;
            referenceText.Visibility = Visibility.Collapsed;
            referenceParentComboBox.Visibility = Visibility.Collapsed;
            relationshipTypeTextBox.Visibility = Visibility.Collapsed;
            relationshipTypeLabel.Visibility = Visibility.Collapsed;
            ModifiersTextBox.Visibility = Visibility.Collapsed;

            parentText.Visibility = Visibility.Collapsed;

            parentComboBox.Visibility = Visibility.Visible;

            updateButton.Content = "Delete Thing";

            errorText.Text = "";

            updateThingBox();
            UpdateParentBox();
        }

        private void enterKey(KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                UpdateUKS();
        }

        private void ThingText_KeyUp(object sender, KeyEventArgs e)
        {
            enterKey(e);
        }

        private void ParentText_KeyUp(object sender, KeyEventArgs e)
        {
            enterKey(e);
        }

        private void ReferenceText_KeyUp(object sender, KeyEventArgs e)
        {
            enterKey(e);
        }

        private void ParentComboBox_KeyUp(object sender, KeyEventArgs e)
        {
            enterKey(e);
        }
        private void RelationshipTypeComboBox_KeyUp(object sender, KeyEventArgs e)
        {
            enterKey(e);
        }

        private void ReferenceParentComboBox_KeyUp(object sender, KeyEventArgs e)
        {
            enterKey(e);
        }

        private void AddThingRadio_KeyUp(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
                addThingRadio.IsChecked=true;
        }

        private void AddReferenceRadio_KeyUp(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
                addReferenceRadio.IsChecked=true;
        }

        private void DeleteThingRadio_KeyUp(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
                deleteThingRadio.IsChecked=true;
        }

        private void thingText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (addThingRadio.IsChecked == false)
                UpdateParentBox();

            updateThingBox();
        }

        private void parentText_TextChanged(object sender, TextChangedEventArgs e)
        {
            ModuleUKSInteract uksInteract = (ModuleUKSInteract)ParentModule;

            if (!checkIsParentValid()) return;

            validParent = true;
            parentText.ClearValue(Control.BackgroundProperty);
        }

        private bool checkIsParentValid()
        {
            ModuleUKSInteract uksInteract = (ModuleUKSInteract)ParentModule;
            if (uksInteract.SearchLabelUKS(parentText.Text) == null)
            {
                validParent = false;
                parentText.Background = new SolidColorBrush(Colors.Pink);
                return false;
            }
            return true;
        }

        private void referenceText_TextChanged(object sender, TextChangedEventArgs e)
        {
            ModuleUKSInteract uksInteract = (ModuleUKSInteract)ParentModule;

            updateReferenceBox();

            UpdateReferenceParentBox();
        }
    }
}
