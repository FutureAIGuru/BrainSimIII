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
    public partial class ModuleUI_KnowledgeDlg : ModuleBaseDlg
    {
        public ModuleUI_KnowledgeDlg()
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

            DrawKnowledge();

            return true;
        }

        private void TheGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }

        bool expanded = false;
        private void ExpandCollapseButton_Click(object sender, RoutedEventArgs e)
        {
            expanded = ((ModuleUserInterfaceDlg)Owner).ExpandCollapseWindow(this.GetType());
            if (expanded)
            {
                theChrome.ResizeBorderThickness = SystemParameters.WindowResizeBorderThickness;
                theChrome.CaptionHeight = 40;
                ExpandButton.Visibility = Visibility.Collapsed;
                CollapseButton.Visibility = Visibility.Visible;
            }
            else
            {
                WindowState = WindowState.Normal;
                ((ModuleUserInterfaceDlg)Owner).MoveChildren();
                theChrome.ResizeBorderThickness = new Thickness(0);
                theChrome.CaptionHeight = 0;
                CollapseButton.Visibility = Visibility.Collapsed;
                ExpandButton.Visibility = Visibility.Visible;
            }
        }

        private void Dlg_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            ModuleUI_Knowledge parent = (ModuleUI_Knowledge)ParentModule;
            if (parent == null) return;
            ModuleInputControl moduleInputControl = (ModuleInputControl)parent.FindModule(typeof(ModuleInputControl));
            if (moduleInputControl == null) return;

            //let the arrow keys be used for other things like navigating in a text box on other pages
            //if (tabHome.IsSelected == false) return;

            switch (e.Key)
            {
                case Key.Up:
                    if (!e.IsRepeat) moduleInputControl.MoveForwardBackward(true);
                    //prevent arrow keys from doing anything else on the home page
                    e.Handled = true;
                    break;
                case Key.Down:
                    if (!e.IsRepeat) moduleInputControl.MoveForwardBackward(false);
                    e.Handled = true;
                    break;
                case Key.Left:
                    moduleInputControl.turnLeft = true;
                    e.Handled = true;
                    break;
                case Key.Right:
                    moduleInputControl.turnRight = true;
                    e.Handled = true;
                    break;
            }
        }

        private void Dlg_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            ModuleUI_Knowledge parent = (ModuleUI_Knowledge)ParentModule;
            if (parent == null) return;
            ModuleInputControl moduleInputControl = (ModuleInputControl)parent.FindModule(typeof(ModuleInputControl));
            if (moduleInputControl == null) return;

            //let the arrow keys be used for other things like navigating in a text box
            //if (tabHome.IsSelected == false) return;

            switch (e.Key)
            {
                case Key.Up:
                    moduleInputControl.StopForwardBack();
                    e.Handled = true;
                    break;
                case Key.Down:
                    moduleInputControl.StopForwardBack();
                    e.Handled = true;
                    break;
                case Key.Left:
                    moduleInputControl.turnLeft = false;
                    moduleInputControl.StopTurn();
                    e.Handled = true;
                    break;
                case Key.Right:
                    moduleInputControl.turnRight = false;
                    moduleInputControl.StopTurn();
                    e.Handled = true;
                    break;
            }
        }

        private void Dlg_Deactivated(object sender, System.EventArgs e)
        {
            ModuleUI_Knowledge parent = (ModuleUI_Knowledge)ParentModule;
            if (parent == null) return;
            ModuleInputControl moduleInputControl = (ModuleInputControl)parent.FindModule(typeof(ModuleInputControl));
            if (moduleInputControl == null) return;

            moduleInputControl.Stop();
        }

        private void DrawKnowledge()
        {
            ModuleUI_Knowledge parent = (ModuleUI_Knowledge)ParentModule;
            if (parent == null) return;

            Thing root = parent.GetUKSDisplayRoot();
            if (root == null) return;

            List<KeyValuePair<string, string>> expandedItems = FindExpandedItems(knowledgeTree.Items);
            knowledgeTree.Items.Clear();

            AddChildrenToTree(root.Children, null, expandedItems);
        }

        private void AddChildrenToTree(IList<Thing> children, TreeViewItem parent, List<KeyValuePair<string, string>> expandedItems)
        {
            foreach (Thing child in children)
            {
                TreeViewItem newChild = new();
                newChild.Header = child.Label;

                if ((parent == null && expandedItems.Contains(new(newChild.Header.ToString(), "")))
                    || (parent != null && expandedItems.Contains(new(newChild.Header.ToString(), parent.Header.ToString()))))
                    newChild.IsExpanded = true;

                if (parent == null)
                    knowledgeTree.Items.Add(newChild);
                else
                    parent.Items.Add(newChild);
                AddChildrenToTree(child.Children, newChild, expandedItems);
            }
        }

        private List<KeyValuePair<string, string>> FindExpandedItems(ItemCollection items)
        {
            List<KeyValuePair<string, string>> expanded = new();
            Queue<TreeViewItem> search = new();
            foreach (TreeViewItem tvItem in items)
            {
                if (tvItem.IsExpanded)
                    expanded.Add(new KeyValuePair<string, string>(tvItem.Header.ToString(), ""));

                search.Enqueue(tvItem);
            }

            while (search.Any())
            {
                TreeViewItem cur = search.Dequeue();

                foreach (TreeViewItem child in cur.Items)
                {
                    if (child.IsExpanded)
                        expanded.Add(new KeyValuePair<string, string>(child.Header.ToString(), cur.Header.ToString()));

                    search.Enqueue(child);
                }
            }

            return expanded;
        }

        private void ModuleBaseDlg_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            ((ModuleUserInterfaceDlg)Owner).ClosePopups();
        }
    }
}