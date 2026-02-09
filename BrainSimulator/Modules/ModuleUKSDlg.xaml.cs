/*
 * Brain Simulator Thought
 *
 * Copyright (c) 2026 Charles Simon
 *
 * This file is part of Brain Simulator Thought and is licensed under
 * the MIT License. You may use, copy, modify, merge, publish, distribute,
 * sublicense, and/or sell copies of this software under the terms of
 * the MIT License.
 *
 * See the LICENSE file in the project root for full license information.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using UKS;

namespace BrainSimulator.Modules;

public partial class ModuleUKSDlg : ModuleBaseDlg
{

    public static readonly DependencyProperty ThoughtObjectProperty = //used in TreeView items
    DependencyProperty.Register("Thought", typeof(Thought), typeof(TreeViewItem));
    public static readonly DependencyProperty TreeViewItemProperty = //used in Thought context menus
    DependencyProperty.Register("TreeViewItem", typeof(TreeViewItem), typeof(TreeViewItem));
    public static readonly DependencyProperty LinkObjectProperty = //used in Link context menus
    DependencyProperty.Register("LinkType", typeof(Thought), typeof(TreeViewItem));


    private const int maxDepth = 20;
    private int totalItemCount;
    private bool mouseInWindow; //prevent auto-update while the mouse is in the tree
    private bool busy;
    private List<string> expandedItems = new();
    private bool updateFailed;
    private DispatcherTimer dt;
    private string expandAll = "";  //all the children below this named node will be expanded
    private UKS.UKS theUKS = null;


    public ModuleUKSDlg()
    {
        InitializeComponent();
        theUKS = MainWindow.theUKS;
    }
    public override bool Draw(bool checkDrawTimer)
    {
        //this has a timer so that no matter how often you might call draw, the dialog
        //only updates 10x per second
        if (!base.Draw(checkDrawTimer)) return false;
        if (busy) return false;
        if (!checkBoxAuto.IsChecked == true) { return false; }
        Refresh();
        return true;
    }



    private void LoadContentToTreeView()
    {
        //get the parameters
        ModuleUKS parent = (ModuleUKS)ParentModule;
        expandAll = parent.GetSavedDlgAttribute("ExpandAll");
        string root = parent.GetSavedDlgAttribute("Root");
		//root = "BrainSim";
		Thought Root = theUKS.Labeled(root);
  
        if (Root is null) Root = (Thought)"Thought";
        string sizeString = parent.GetSavedDlgAttribute("fontSize");
        int.TryParse(sizeString, out int fontSize);
        if (fontSize != 0)
            theTreeView.FontSize = fontSize;

        //if the root is null, display the roots instead of the contetn
        if (!string.IsNullOrEmpty(root))
        {
            totalItemCount = 0;
            TreeViewItem tvi = new() { Header = Root.ToString() };
            tvi.ContextMenu = GetContextMenu(Root, tvi);
            tvi.IsExpanded = true; //always expand the top-level item
            theTreeView.Items.Add(tvi);
            tvi.SetValue(ThoughtObjectProperty, Root);
            AddLinks(Root, tvi, 1, "");
            AddChildren(Root, tvi, 0, Root.Label);
        }
        else if (string.IsNullOrEmpty(root)) //search for unattached Thoughts
        {
            for (int i = 0; i < theUKS.AllThoughts.Count; i++)
            {
                Thought t1 = theUKS.AllThoughts[i];
                if (t1.Parents.Count == 0)
                {
                    TreeViewItem tvi = new() { Header = t1.Label };
                    tvi.ContextMenu = GetContextMenu(t1, tvi);
                    theTreeView.Items.Add(tvi);
                }
            }
        }
    }
    private void AddChildren(Thought t, TreeViewItem tvi, int depth, string parentLabel)
    {
        if (totalItemCount > 500) return;
        depth++;
        if (depth > maxDepth) return;

        List<Thought> theChildren = t.LinksFrom.Where(x => x.LinkType.Label.StartsWith("is-a") && x.To is not null).ToList();
        theChildren = theChildren.OrderBy(x => x.From.Label).ToList();

        foreach (Thought l in theChildren)
        {
            //"l" is the link defining the is-a link so child is the from of it
            var child = l.From;
            TreeViewItem tviChild = GetTreeChildFormatted(parentLabel, child);
            tvi.Items.Add(tviChild);
            tviChild.ContextMenu = GetContextMenu(child, tviChild);

            if (l.LinksTo.Count > 0)  //there is provenance on this is-a link
                AddLinks(l, tviChild, 1, parentLabel);

            int childCount = child.Children.Count;
            int linkCount = child.LinksTo.Count(x => x.LinkType.Label != "is-a");
            int linkFromCount = child.LinksFrom.Count;
            if (tviChild.IsExpanded)
            {
                // load children and links
                AddLinks(child, tviChild, 1, parentLabel);
                AddChildren(child, tviChild, depth, parentLabel + "|" + child.Label);
            }
        }
    }
    private void AddLinks(Thought t, TreeViewItem tvi, int depth, string parentLabel)
    {
        if (t.LinksTo.Count == 0 && t.From is null && t.To is null) return;

        //build the entry for the tabel of expanded items
        string currentLabel = "|" + parentLabel + "|" + t.Label;
        if (theUKS.IsSequenceElement(t)) //this skips over the level of the sequence element itself and only shows the links
            currentLabel = "|" + parentLabel;
        currentLabel = currentLabel.Replace("||", "|"); //needed to make top level work

        //add each of the links as a "child" of the parent entry
        //display is-a links if deteails are requested
        var sortedLinks = t.LinksTo.OrderBy(x => x?.LinkType?.Label).ToList();
        if (detailsCB.IsChecked == false)
            sortedLinks = t.LinksTo.Where(x => x.LinkType.Label != "is-a").OrderBy(x => x?.LinkType?.Label).ToList();
        foreach (Thought l in sortedLinks)
        {
            if (showConditionals.IsChecked != true)
                if (l.HasProperty("isCondition") || l.HasProperty("isResult")) continue; //hide conditionals
            var x = expandedItems;

            TreeViewItem tviLink = GetTreeChildFormatted(currentLabel, l);
            tviLink.ContextMenu = GetLinkContextMenu(l);
            tvi.Items.Add(tviLink);

            if (tviLink.IsExpanded && l.LinksTo.Count > 0) //get provenance, etc. on this link
                AddLinks(l, tviLink, depth, currentLabel);
            if (tviLink.IsExpanded && theUKS.IsSequenceElement(l?.To)) //expand sequence elements
                AddLinks(l.To, tviLink, depth, currentLabel + "|" + l.ToString());
        }
        if (reverseCB.IsChecked == true)
            AddLinksFrom(t, tvi, currentLabel);
    }
    private void AddLinksFrom(Thought t, TreeViewItem tvi, string parentLabel)
    {
        if (t.LinksFrom.Count == 0 && t.From is null && t.To is null) return;

        //add the entry to the entry of expanded items
        parentLabel = "|" + parentLabel + "|" + t.ToString();
        parentLabel = parentLabel.Replace("||", "|"); //needed to make top level work

        //add each of the links as a "child" of the parent entry
        //display is-a links if deteails are requested
        var sortedLinks = t.LinksFrom.OrderBy(x => x?.LinkType?.Label).ToList();
        foreach (Thought r in sortedLinks)
        {
            if (showConditionals.IsChecked != true)
                if (r.HasProperty("isCondition") || r.HasProperty("isResult")) continue; //hide conditionals

            TreeViewItem tviLink = GetTreeChildFormatted(parentLabel, r);
            tviLink.ContextMenu = GetLinkContextMenu(r);
            tvi.Items.Add(tviLink);
        }
    }


    //build and format the TreeView item for this Thought
    private TreeViewItem GetTreeChildFormatted(string parentLabel, Thought r)
    {
        Thought child = r;
        string header = "";
        if (r.LinkType is null && r.To is null)
        {
            //Format a node-like line in the treeview
            header = child.ToString();
            if (header == "") header = "\u25A1"; //put in a small empty box--if the header is unlabeled, so you can right-click 
            if (showConditionals.IsChecked == true && r.LinkType?.Label == "is-a") //hack to show conditions on is-a links
                foreach (Thought r1 in r.LinksTo)
                    header += "  " + r1.ToString();
        }
        else
        {
            //format a link-like line in the treeview
            header = r.ToString();
            //show sequence content unless details are selected
            if (detailsCB.IsChecked == false && theUKS.IsSequenceElement(r.To))
            {
                string joinCharacter = "";
                if (r.LinkType.Label == "events") joinCharacter = "\n\t\t"; //hack for better dieplay of longer items
                string sequence = "^" + string.Join(joinCharacter, theUKS.FlattenSequence(r.To));
                header = $"[{r.From.Label}->{r.LinkType.Label}->{sequence}]";
            }
        }
        header = AddDetails(r, header);

        //create the treeview entry
        TreeViewItem tviChild = new() { Header = header };

        //change color of thoughts which just fired or are about to expire
        tviChild.SetValue(ThoughtObjectProperty, child);
        if (child.LastFiredTime > DateTime.Now - TimeSpan.FromMilliseconds(500))
            tviChild.Background = new SolidColorBrush(Colors.LightGreen);
        if (r.TimeToLive != TimeSpan.MaxValue && r.LastFiredTime + r.TimeToLive < DateTime.Now + TimeSpan.FromSeconds(3))
            tviChild.Background = new SolidColorBrush(Colors.LightYellow);

        //is this expanded?
        string currentLabel = "|" + parentLabel + "|" + (string.IsNullOrEmpty(child?.Label) ? child?.ToString() : child?.Label);
        currentLabel = currentLabel.Replace("||", "|"); //parentLabel may or may not have a leading '|'
        if (expandedItems.Contains(currentLabel))
            tviChild.IsExpanded = true;
        if (child.AncestorList().Contains(expandAll) &&
            (child.Label == "" || !parentLabel.Contains("|" + child.Label)))
            tviChild.IsExpanded = true;
        tviChild.Expanded += EmptyChild_Expanded;

        totalItemCount++;
        return tviChild;
    }

    //if the "details" box is checked, add the details
    private string AddDetails(Thought r, string header)
    {
        if (detailsCB.IsChecked == false) return header;
        string timeToLive = (r.TimeToLive == TimeSpan.MaxValue ? "8" : (r.LastFiredTime + r.TimeToLive - DateTime.Now).ToString(@"mm\:ss"));
        if (r.Weight != 1f || r.TimeToLive != TimeSpan.MaxValue)
            header = $"<{r.Weight.ToString("f2")}, {timeToLive}> " + header;
        if (r.LinkType?.HasLink(null, null, theUKS.Labeled("not")) is not null) //prepend ! for negative  children
            header = "!" + header;
        header += ": Children:" + r.Children.Count;
        header += " Links:" + r.LinksTo.Count;
        return header;
    }




    //the treeview is populated only with expanded items or it would contain the entire UKS content
    //when an item is expanded, its content needs to be created into the treeview
    private void EmptyChild_Expanded(object sender, RoutedEventArgs e)
    {
        // what tree view item is this
        if (sender is TreeViewItem tvi)
        {
            string name = tvi.Header.ToString(); // to help debug
            Thought t = (Thought)tvi.GetValue(ThoughtObjectProperty);
            string parentLabel = "|" + t.ToString();
            TreeViewItem tvi1 = tvi;
            int depth = 0;
            //work your way up the tree to find all ancestors of this leaf
            while (tvi1.Parent is not null && tvi1.Parent is TreeViewItem tvi2)
            {
                tvi1 = tvi2;
                Thought t1 = (Thought)tvi1.GetValue(ThoughtObjectProperty);
                parentLabel = "|" + (string.IsNullOrEmpty(t1?.Label)?t1?.ToString():t1?.Label) + parentLabel;
                depth++;
            }
            if (!expandedItems.Contains(parentLabel))
                expandedItems.Add(parentLabel);

            tvi.Items.Clear(); // delete empty child
            if (t.Children.Count > 0)
                AddChildren(t, tvi, depth, parentLabel);
            if (t.LinksTo.Count > 0)
                AddLinks(t, tvi, 1, parentLabel);
            if (reverseCB.IsChecked == true && t.LinksFrom.Count > 0)
                AddLinksFrom(t, tvi, parentLabel);
            if (theUKS.IsSequenceElement(t.To))
                AddLinks(t.To, tvi, 1, parentLabel);
            e.Handled = true;
        }
    }

    //find out which tree items are already expanded by recursively following the tree items 
    private void FindExpandedItems(ItemCollection items, string parentLabel)
    {
        foreach (TreeViewItem tvi1 in items)
        {
            var t = tvi1.GetValue(ThoughtObjectProperty);
            if (t is null) continue;
            if (tvi1.IsExpanded) expandedItems.Add(parentLabel + "|" + t.ToString());
            FindExpandedItems(tvi1.Items, parentLabel + "|" + t.ToString());
        }
    }

    //Context Menu creation and handling
    private ContextMenu GetContextMenu(Thought t, TreeViewItem tvi)
    {
        ContextMenu menu = new ContextMenu();
        menu.SetValue(ThoughtObjectProperty, t);
        menu.SetValue(TreeViewItemProperty, tvi);
        int ID = theUKS.AllThoughts.IndexOf(t);
        MenuItem mi = new();
        string thoughtLabel = "___";
        if (t is not null)
            thoughtLabel = t.Label;
        mi.Header = "Name: " + thoughtLabel + "  Index: " + ID;
        mi.IsEnabled = false;
        menu.Items.Add(mi);

        TextBox renameBox = new() { Text = thoughtLabel, Width = 200, Name = "RenameBox" };
        renameBox.PreviewKeyDown += RenameBox_PreviewKeyDown;
        mi = new();
        mi.Header = renameBox;
        menu.Items.Add(mi);

        mi = new();
        mi.Click += Mi_Click;
        if (t.Label == expandAll)
            mi.Header = "Collapse All";
        else
            mi.Header = "Expand All";
        menu.Items.Add(mi);
        mi = new();
        mi.Click += Mi_Click;
        mi.Header = "Delete";
        menu.Items.Add(mi);

        mi = new();
        mi.Click += Mi_Click;
        mi.Header = "Delete Child";
        menu.Items.Add(mi);

        mi = new();
        mi.Click += Mi_Click;
        mi.Header = "Make Root";
        menu.Items.Add(mi);
        mi = new();
        mi.Click += Mi_Click;
        mi.Header = "Fire";
        menu.Items.Add(mi);
        //mi = new();
        //mi.Click += Mi_Click;
        //mi.Header = "Fetch GPT Info";
        //menu.Items.Add(mi);
        mi = new();
        mi.Header = "Parents:";
        if (t.Parents.Count == 0)
            mi.Header = "Parents: NONE";
        mi.IsEnabled = false;
        menu.Items.Add(mi);
        foreach (Thought t1 in t.Parents)
        {
            mi = new();
            mi.Click += Mi_Click;
            mi.Header = "    " + t1.Label;
            mi.SetValue(ThoughtObjectProperty, t1);
            menu.Items.Add(mi);
        }

        menu.Opened += Menu_Opened;
        menu.Closed += Menu_Closed;
        return menu;
    }

    private void Menu_Closed(object sender, RoutedEventArgs e)
    {
        Draw(true);
    }

    private void Menu_Opened(object sender, RoutedEventArgs e)
    {
        //when the context menu opens, focus on the label and position text cursor to end
        if (sender is ContextMenu cm)
        {
            Control cc = Utils.FindByName(cm, "RenameBox");
            if (cc is TextBox tb)
            {
                tb.Focus();
                tb.Select(0, tb.Text.Length);
            }
        }
    }

    private void RenameBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is System.Windows.Controls.TextBox tb)
        {
            MenuItem mi = tb.Parent as MenuItem;
            ContextMenu cm = mi.Parent as ContextMenu;
            Thought t = (Thought)cm.GetValue(ThoughtObjectProperty);
            string testName = tb.Text + e.Key;
            Thought testThought = ThoughtLabels.GetThought(testName);
            if (testName != "" && testThought is not null && testThought != t)
            {
                tb.Background = new SolidColorBrush(Colors.Pink);
                return;
            }
            tb.Background = new SolidColorBrush(Colors.White);
            if (e.Key == Key.Enter)
            {
                t.Label = tb.Text;
                //clear any time-to-live on this new image
                t.LinksFrom.FindFirst(x => x.LinkType.Label == "is-a")?.TimeToLive = TimeSpan.MaxValue;
                cm.IsOpen = false;
            }
            if (e.Key == Key.Escape)
            {
                cm.IsOpen = false;
            }
        }
    }

    private ContextMenu GetLinkContextMenu(Thought r)
    {
        ContextMenu menu = new ContextMenu();
        menu.SetValue(LinkObjectProperty, r);
        MenuItem mi = new();
        mi.Click += Mi_Click;
        mi.Header = "Delete";
        menu.Items.Add(mi);
        mi = new();
        mi.Header = "Go To:";
        mi.IsEnabled = false;
        menu.Items.Add(mi);

        mi = new();
        mi.Click += Mi_Click;
        mi.Header = "    " + r.From.Label;
        mi.SetValue(ThoughtObjectProperty, r.From);
        menu.Items.Add(mi);

        mi = new();
        mi.Click += Mi_Click;
        mi.Header = "    " + r.LinkType.Label;
        mi.SetValue(ThoughtObjectProperty, r.LinkType);
        menu.Items.Add(mi);

        mi = new();
        mi.Click += Mi_Click;
        mi.Header = "    " + r.To?.Label;
        mi.SetValue(ThoughtObjectProperty, r.To);
        menu.Items.Add(mi);

        return menu;
    }

    private void Mi_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem mi)
        {
            ContextMenu m = mi.Parent as ContextMenu;
            //handle setting parent to root
            Thought tParent = (Thought)mi.GetValue(ThoughtObjectProperty);
            if (tParent is not null)
            {
                textBoxRoot.Text = tParent.Label;
                Refresh();
            }
            Thought t = (Thought)m.GetValue(ThoughtObjectProperty);
            if (t is null)
            {
                Thought r = (Thought)m.GetValue(LinkObjectProperty);
                (r.From as Thought).RemoveLink(r);
                //force a repaint
                Refresh();
                return;
            }
            ModuleUKS parent = (ModuleUKS)ParentModule;
            switch (mi.Header)
            {
                case "Expand All":
                    expandAll = t.Label;
                    expandedItems.Clear();
                    expandedItems.Add("|Thought|Object");
                    parent.SetSavedDlgAttribute("ExpandAll", expandAll);
                    updateFailed = true; //this forces the expanded items list not to rebuild
                    break;
                case "Collapse All":
                    expandAll = "";
                    expandedItems.Clear();
                    expandedItems.Add("|Thought|Object");
                    updateFailed = true;
                    parent.SetSavedDlgAttribute("ExpandAll", expandAll);
                    break;
                case "Fetch GPT Info":
                    //the following is an async call so an immediate refresh is not useful
                    //ModuleGPTInfo.GetChatGPTData(t.Label);
                    break;
                case "Fire":
                    t.Fire();
                    break;
                case "Delete":
                    theUKS.DeleteAllChildren(t);
                    theUKS.DeleteThought(t);
                    break;
                case "Delete Child":
                    //figure out which item (and its parent) clicked us
                    TreeViewItem tvi = (TreeViewItem)m.GetValue(TreeViewItemProperty);
                    DependencyObject parent1 = VisualTreeHelper.GetParent((DependencyObject)tvi);
                    while (parent1 is not null && !(parent1 is TreeViewItem))
                        parent1 = VisualTreeHelper.GetParent(parent1);
                    Thought parentThought = (Thought)parent1.GetValue(ThoughtObjectProperty);
                    //now delete the link
                    if (parentThought is not null && t is not null)
                        parentThought.RemoveChild(t);
                    break;
                case "Make Root":
                    textBoxRoot.Text = t.Label;
                    Refresh();
                    break;
            }
            //force a repaint
            Refresh();
        }
    }

    //EVENTS
    private void TheTreeView_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        Draw(true);
    }

    private void UpdateStatusLabel()
    {
        int childCount = 0;
        int refCount = 0;
        Thought t = null;
        try
        {
            foreach (Thought t1 in theUKS.AllThoughts)
            {
                t = t1;
                childCount += t1.Children.Count;
                refCount += t1.LinksTo.Count - t1.Children.Count;
            }
        }

        catch (Exception ex)
        {
            //you might get this exception if there is a collision
            return;
        }
        statusLabel.Content = theUKS.AllThoughts.Count + " Thoughts  " + (childCount + refCount) + " Links.";
        Title = "The Universal Knowledgs Store (UKS)  --  File: " + Path.GetFileNameWithoutExtension(theUKS.FileName);
    }

    private bool _isTextChangingInternally;  //lockout so we can change the text without retriggering the event
    private void TextBoxRoot_KeyDown(object sender, KeyEventArgs e)
    {
        // Allow text changes when keys like backspace, delete are pressed
        if (e.Key == Key.Back || e.Key == Key.Delete)
        {
            _isTextChangingInternally = true;
            int caretIndex = textBoxRoot.CaretIndex;
            if (e.Key == Key.Back) caretIndex--;
            if (caretIndex < 0) caretIndex = 0;
            textBoxRoot.Text = textBoxRoot.Text.Substring(0, caretIndex);
            textBoxRoot.CaretIndex = caretIndex;
            e.Handled = true;
            _isTextChangingInternally = false;
            //get a new suggestion
            if (e.Key == Key.Back)
                textBoxRoot_TextChanged(null, null);
        }
        if (e.Key == Key.Enter)
        {
            textBoxRoot.SelectionLength = 0;
        }
    }
    private void textBoxRoot_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isTextChangingInternally)
            return;

        string searchText = textBoxRoot.Text;
        if (!string.IsNullOrEmpty(searchText))
        {
            //get the first label
            var suggestion = ThoughtLabels.LabelList.Keys
                .Where(key => key.StartsWith(searchText, StringComparison.OrdinalIgnoreCase))
                .OrderBy(key => key)
                .FirstOrDefault();
            //get the real label to get the capitalization right
            if (suggestion is not null) suggestion = ThoughtLabels.GetThought(suggestion).Label;

            if (suggestion is not null && !suggestion.Equals(searchText, StringComparison.OrdinalIgnoreCase))
            {
                int caretIndex = textBoxRoot.CaretIndex;
                _isTextChangingInternally = true;
                textBoxRoot.Text = suggestion;
                textBoxRoot.CaretIndex = caretIndex;
                textBoxRoot.SelectionStart = caretIndex;
                textBoxRoot.SelectionLength = suggestion.Length - caretIndex;
                textBoxRoot.SelectionOpacity = .4;
                _isTextChangingInternally = false;
            }
        }
        ModuleUKS parent = (ModuleUKS)ParentModule;
        if (parent is null) return;
        parent.SetSavedDlgAttribute("Root", textBoxRoot.Text);
        Refresh();

    }

    //using the mouse-wheel while pressing ctrl key changes the font size
    private void theTreeView_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if ((Keyboard.GetKeyStates(Key.LeftCtrl) & KeyStates.Down | Keyboard.GetKeyStates(Key.RightCtrl) & KeyStates.Down) != 0)
        {
            if (e.Delta < 0)
            {
                if (theTreeView.FontSize > 2)
                    theTreeView.FontSize -= 1;
            }
            else if (e.Delta > 0)
            {
                theTreeView.FontSize += 1;
            }
            ModuleUKS parent = (ModuleUKS)ParentModule;
            parent.SetSavedDlgAttribute("fontSize", theTreeView.FontSize.ToString());

        }
    }

    private void CheckBoxAuto_Checked(object sender, RoutedEventArgs e)
    {
        if (dt is null)
            dt = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, 0, 0, 200)
            };
        dt.Tick += Dt_Tick;
        dt.Start();
    }

    private void Dt_Tick(object sender, EventArgs e)
    {
        if (!mouseInWindow)
            Draw(true);
        RefreshButton?.Visibility = Visibility.Hidden;
    }

    private void CheckBoxAuto_Unchecked(object sender, RoutedEventArgs e)
    {
        dt.Stop();
        RefreshButton.Visibility = Visibility.Visible;
    }

    private void CheckBoxDetails_Checked(object sender, RoutedEventArgs e)
    {
        Draw(false);
    }

    private void CheckBoxDetails_Unchecked(object sender, RoutedEventArgs e)
    {
        Draw(false);
    }

    private void TheTreeView_MouseEnter(object sender, MouseEventArgs e)
    {
        mouseInWindow = true;
        theTreeView.Background = new SolidColorBrush(Colors.LightSteelBlue);
    }
    private void TheTreeView_MouseLeave(object sender, MouseEventArgs e)
    {
        mouseInWindow = false;
        theTreeView.Background = new SolidColorBrush(Colors.LightGray);
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        Refresh();
    }

    private void Refresh()
    {
        try
        {
            if (!updateFailed)
            {
                expandedItems.Clear();
                FindExpandedItems(theTreeView.Items, "");
            }
            updateFailed = false;

            UpdateStatusLabel();

            theTreeView.Items.Clear();
            LoadContentToTreeView();
        }
        catch
        {
            updateFailed = true;
        }
        busy = false;
    }

    private void InitializeButton_Click(object sender, RoutedEventArgs e)
    {
        ModuleUKS parent = (ModuleUKS)base.ParentModule;

        theUKS.CreateInitialStructure();
        parent.Initialize();

        CollapseAll();
        expandAll = parent.GetSavedDlgAttribute("ExpandAll");
        if (expandAll is null) expandAll = "";
        string root = parent.GetSavedDlgAttribute("Root");
        if (string.IsNullOrEmpty(root))
            root = "Thought";
        textBoxRoot.Text = root;
        Refresh();
    }

    private void CollapseAll()
    {
        foreach (TreeViewItem item in theTreeView.Items)
            CollapseTreeviewItems(item);
    }

    //recursively collapse all the children
    private void CollapseTreeviewItems(TreeViewItem Item)
    {
        Item.IsExpanded = false;

        foreach (TreeViewItem item in Item.Items)
        {
            item.IsExpanded = false;

            if (item.HasItems)
                CollapseTreeviewItems(item);
        }
    }

    private void Dlg_Loaded(object sender, RoutedEventArgs e)
    {
        ModuleUKS parent = (ModuleUKS)ParentModule;
        textBoxRoot.Text = parent.GetSavedDlgAttribute("Root");
    }

    private string Browse(bool open)
    {
        string path = "";
        System.Windows.Forms.FileDialog dlg;
        if (open)
            dlg = new System.Windows.Forms.OpenFileDialog
            {
                Title = "Select UKS .txt file",
                Filter = "UKS text (*.txt)|*.txt|All files (*.*)|*.*",
                CheckFileExists = false,
                Multiselect = false
            };
        else
            dlg = new System.Windows.Forms.SaveFileDialog
            {
                Title = "Select UKS .txt file",
                Filter = "UKS text (*.txt)|*.txt|All files (*.*)|*.*",
                CheckFileExists = false,
            };
        if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            path = dlg.FileName;
        return path;
    }

    private async void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        SetStatus("");
        var path = Browse(true); ;
        if (string.IsNullOrEmpty(path)) return;

        if (string.IsNullOrWhiteSpace(path))
        {
            SetStatus("Choose a file first.");
            return;
        }
        if (!File.Exists(path))
        {
            SetStatus("File not found.");
            return;
        }

        try
        {
            ModuleUKS parent = (ModuleUKS)base.ParentModule;
            // Run ingest off the UI thread to keep the window responsive
            await Task.Run(() => theUKS.ImportTextFile(path));

            SetStatus("Success");
        }
        catch (Exception ex)
        {
            Mouse.OverrideCursor = null;

            // Show a friendly error, but include details for debugging.
            System.Windows.MessageBox.Show(this,
                "Import failed.\n\n" + ex.Message,
                "UKS Import",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private async void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        var path = Browse(true); ;
        if (string.IsNullOrEmpty(path)) return;

        if (string.IsNullOrWhiteSpace(path))
        {
            SetStatus("Choose a file first.");
            return;
        }
        try
        {
            ModuleUKS parent = (ModuleUKS)base.ParentModule;
            //get the root to save the contents of from the UKS dialog root
            string root = parent.GetSavedDlgAttribute("Root");
            await Task.Run(() => theUKS.ExportTextFile(root, path));
            SetStatus("Success");
        }
        catch (Exception ex)
        {
            Mouse.OverrideCursor = null;

            // Show a friendly error, but include details for debugging.
            System.Windows.MessageBox.Show(this,
                "Import failed.\n\n" + ex.Message,
                "UKS Import",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }
}