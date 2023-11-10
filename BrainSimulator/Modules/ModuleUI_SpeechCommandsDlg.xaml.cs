//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using HelixToolkit.Wpf;
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
    public partial class ModuleUI_SpeechCommandsDlg : ModuleBaseDlg
    {
        public ModuleUI_SpeechCommandsDlg()
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

        private void ModuleBaseDlg_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            ((ModuleUserInterfaceDlg)Owner).ClosePopups();
        }

        private void CloseButton_MouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void CategoriesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GeneralCommandsGrid == null || MotionCommandsGrid == null || KnowledgeCommandsGrid == null) return;
            GeneralCommandsGrid.Visibility = Visibility.Collapsed;
            MotionCommandsGrid.Visibility = Visibility.Collapsed;
            KnowledgeCommandsGrid.Visibility = Visibility.Collapsed;
            if (((ComboBoxItem)CategoriesComboBox.SelectedItem).Content.ToString() == "General")
                GeneralCommandsGrid.Visibility = Visibility.Visible;
            else if(((ComboBoxItem)CategoriesComboBox.SelectedItem).Content.ToString() == "Motion")
                MotionCommandsGrid.Visibility = Visibility.Visible;
            else if(((ComboBoxItem)CategoriesComboBox.SelectedItem).Content.ToString() == "Knowledge")
                KnowledgeCommandsGrid.Visibility = Visibility.Visible;
            commandScrollViewer.ScrollToTop();
        }
    }
}