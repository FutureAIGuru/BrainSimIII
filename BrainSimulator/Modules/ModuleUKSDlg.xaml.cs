//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using UKS;

namespace BrainSimulator.Modules;

public partial class ModuleUKSDlg : ModuleBaseDlg
{

    public static readonly DependencyProperty ThingObjectProperty =
    DependencyProperty.Register("Thing", typeof(Thing), typeof(TreeViewItem));
    public static readonly DependencyProperty TreeViewItemProperty =
    DependencyProperty.Register("TreeViewItem", typeof(TreeViewItem), typeof(TreeViewItem));
    public static readonly DependencyProperty RelationshipObjectProperty =
    DependencyProperty.Register("RelationshipType", typeof(Relationship), typeof(TreeViewItem));


    private const int maxDepth = 20;
    private int totalItemCount;
    private bool mouseInTree; //prevent auto-update while the mouse is in the tree
    private bool busy;
    private List<string> expandedItems = new();
    private bool updateFailed;
    private DispatcherTimer dt;
    private string expandAll = "";  //all the children below this named node will be expanded

    public ModuleUKSDlg()
    {
        InitializeComponent();
    }
    public override bool Draw(bool checkDrawTimer)
    {
        //this has a timer so that no matter how often you might call draw, the dialog
        //only updates 10x per second
        if (!base.Draw(checkDrawTimer)) return false;
        if (busy) return false;
        if (!checkBoxAuto.IsChecked == true) { return false; }
        RefreshButton_Click(null, null);
        return true;
    }

    private void UpdateStatusLabel()
    {
        int childCount = 0;
        int refCount = 0;
        ModuleUKS parent = (ModuleUKS)ParentModule;
        Thing t = null;
        try
        {
            foreach (Thing t1 in parent.theUKS.UKSList)
            {
                t = t1;
                childCount += t1.Children.Count;
                refCount += t1.Relationships.Count - t1.Children.Count;
            }
        }

        catch (Exception ex)
        {
            //you might get this exception if there is a collision
            return;
        }
        statusLabel.Content = parent.theUKS.UKSList.Count + " Things  " + (childCount + refCount) + " Relationships";
        Title = "The Universal Knowledgs Store (UKS)  --  File: " + Path.GetFileNameWithoutExtension(parent.theUKS.FileName);
    }

    private void LoadContentToTreeView()
    {
        ModuleUKS parent = (ModuleUKS)ParentModule;
        expandAll = parent.GetAttribute("ExpandAll");
        string root = parent.GetAttribute("Root");
        if (root == null)
        {
            root = "Thing";
            parent.SetAttribute("Root", root);
        }
        Thing thing = parent.theUKS.Labeled(root);
        if (thing is not null)
        {
            totalItemCount = 0;
            TreeViewItem tvi = new() { Header = thing.ToString() };
            tvi.ContextMenu = GetContextMenu(thing,tvi);
            tvi.IsExpanded = true; //always expand the top-level item
            theTreeView.Items.Add(tvi);
            tvi.SetValue(ThingObjectProperty, thing);
            totalItemCount++;
            AddChildren(thing, tvi, 0, thing.Label);
            AddRelationships(thing,tvi,"");
            if (reverseCB.IsChecked == true)
                AddRelationshipsFrom(thing, tvi, "");
        }
        else if (string.IsNullOrEmpty(root)) //search for unattached Things
        {
            try //ignore problems of collection modified
            {
                foreach (Thing t1 in parent.theUKS.UKSList)
                {
                    if (t1.Parents.Count == 0)
                    {
                        TreeViewItem tvi = new() { Header = t1.Label };
                        tvi.ContextMenu = GetContextMenu(t1,tvi);
                        theTreeView.Items.Add(tvi);
                    }
                }
            }
            catch { updateFailed = true; }
        }
    }
    private void AddChildren(Thing t, TreeViewItem tvi, int depth, string parentLabel)
    {
        if (totalItemCount > 3000) return;

        List<Relationship> theChildren = new();
        foreach (Relationship r in t.Relationships)
        {
            if (r.relType?.Label.StartsWith("has-child") == true && r.target != null)
            {
                theChildren.Add(r);
            }
        }
        theChildren = theChildren.OrderBy(x => x.target.Label).ToList();

        ModuleUKS UKS = (ModuleUKS)ParentModule;

        foreach (Relationship r in theChildren)
        {
            Thing child = r.target;
            int descCount = child.GetDescendentsCount();
            string descCountStr = (descCount < 5000) ? descCount.ToString() : "****";
            string header = child.ToString();
            if (header == "") header = "\u25A1"; //put in a small empty box--if the header is completely empty, you can never right-click 
            if (r.Weight != 1 && detailsCB.IsChecked == true) //prepend weight for probabilistic children
                header = "<" + r.Weight.ToString("f2") + "," + (r.TimeToLive == TimeSpan.MaxValue ? "∞" : (r.LastUsed + r.TimeToLive - DateTime.Now).ToString(@"mm\:ss")) + "> " + header;
            if (r.reltype.HasRelationship(null,null,UKS.theUKS.Labeled("not")) != null) //prepend ! for negative  children
                header = "!" + header;
            if (detailsCB.IsChecked == true)
                header += ":" + child.Children.Count + "," + descCountStr;
            if (child.RelationshipsNoCount.Count > 0)
                header = ChildHasReferences(UKS, child, header, depth);

            TreeViewItem tviChild = new() { Header = header };

            //change color of things which just fired or are about to expire
            tviChild.SetValue(ThingObjectProperty, child);
            if (child.lastFiredTime > DateTime.Now - TimeSpan.FromSeconds(2))
                tviChild.Background = new SolidColorBrush(Colors.LightGreen);
            if (r.TimeToLive != TimeSpan.MaxValue && r.LastUsed + r.TimeToLive < DateTime.Now + TimeSpan.FromSeconds(3))
                tviChild.Background = new SolidColorBrush(Colors.LightYellow);

            if (expandedItems.Contains("|" + parentLabel + "|" + LeftOfColon(header)))
                tviChild.IsExpanded = true;
            if (r.target.AncestorList().Contains(ThingLabels.GetThing(expandAll)) &&
                (child.Label == "" || !parentLabel.Contains("|" + child.Label)))
                tviChild.IsExpanded = true;

            tvi.Items.Add(tviChild);

            totalItemCount++;
            tviChild.ContextMenu = GetContextMenu(child,tviChild);
            if (depth < maxDepth)
            {
                int childCount = child.Children.Count;
                int relCount = CountNonChildRelationships(child.RelationshipsNoCount);
                int relFromCount = CountNonChildRelationships(child.RelationshipsFrom);
                if (tviChild.IsExpanded)
                {
                    // load children and references
                    AddChildren(child, tviChild, depth + 1, parentLabel + "|" + child.Label);
                    AddRelationships(child, tviChild, parentLabel);
                    if (reverseCB.IsChecked == true)
                        AddRelationshipsFrom(child, tviChild, parentLabel);
                }
                else if (child.Children.Count > 0 ||
                    CountNonChildRelationships(child.RelationshipsNoCount) > 0
                    || CountNonChildRelationships(child.RelationshipsFrom) > 0)
                {
                    // don't load those that aren't expanded, put in a dummy instead so there is and expander-handle
                    TreeViewItem emptyChild = new() { Header = "" };
                    tviChild.Items.Add(emptyChild);
                    tviChild.Expanded += EmptyChild_Expanded;
                }
                else
                {
                    //not expandable
                    //Debug.Write("x");
                }
            }
        }
    }

    private void AddRelationships(Thing t, TreeViewItem tvi, string parentLabel)
    {
        if (CountNonChildRelationships(t.RelationshipsNoCount) == 0) return;
        TreeViewItem tviRefLabel = new() { Header = "Relationships: " };
        if (detailsCB.IsChecked == true)
            tviRefLabel.Header += CountNonChildRelationships(t.RelationshipsNoCount).ToString();

        string fullString = "|" + parentLabel + "|" + t.Label + "|:Relationships";
        fullString = fullString.Replace("||", "|"); //needed to make top level work
        if (expandedItems.Contains(fullString))
            tviRefLabel.IsExpanded = true;
        if (t.AncestorList().Contains(ThingLabels.GetThing(expandAll)))
            tviRefLabel.IsExpanded = true;
        tvi.Items.Add(tviRefLabel);

        totalItemCount++;
        IList<Relationship> sortedReferences = t.RelationshipsNoCount.OrderBy(x => x.relType?.Label).ToList();
        foreach (Relationship r in sortedReferences)
        {
            if (r.relType?.Label == "has-child") continue;
            if (r.target != null && r.target.HasAncestorLabeled("Value"))
            {
                TreeViewItem tviRef = new() { Header = GetRelationshipString(r) };
                tviRef.ContextMenu = GetContextMenu(r.target,tviRef);
                tviRefLabel.Items.Add(tviRef);
                totalItemCount++;
            }
            else if (r.relType is not null) //should ALWAYS be true
            {
                TreeViewItem tviRef = new() { Header = GetRelationshipString(r), };

                if (r.source != t) tviRef.Header = r.source?.Label + "->" + tviRef.Header;
                tviRef.ContextMenu = GetRelationshipContextMenu(r);
                tviRefLabel.Items.Add(tviRef);
                if (r.LastUsed > DateTime.Now - TimeSpan.FromSeconds(2))
                    tviRef.Background = new SolidColorBrush(Colors.LightGreen);
                if (r.TimeToLive != TimeSpan.MaxValue && r.LastUsed + r.TimeToLive < DateTime.Now + TimeSpan.FromSeconds(3))
                    tviRef.Background = new SolidColorBrush(Colors.LightYellow);
                totalItemCount++;
            }
        }
    }


    private void AddRelationshipsFrom(Thing t, TreeViewItem tvi, string parentLabel)
    {
        if (CountNonChildRelationships(t.RelationshipsFrom) == 0) return;
        TreeViewItem tviRefLabel = new() { Header = "RelationshipsFrom: " };
        if (detailsCB.IsChecked == true)
            tviRefLabel.Header += CountNonChildRelationships(t.RelationshipsFrom).ToString();

        string fullString = "|" + parentLabel + "|" + t.Label + "|:RelationshipsFrom";
        fullString = fullString.Replace("||", "|"); //needed to make top level work
        if (expandedItems.Contains(fullString))
            tviRefLabel.IsExpanded = true;
        if (t.AncestorList().Contains(ThingLabels.GetThing(expandAll)))
            tviRefLabel.IsExpanded = true;
        tvi.Items.Add(tviRefLabel);

        foreach (Relationship r in t.RelationshipsFrom)
        {
            if (r.relType?.Label == "has-child") continue;
            TreeViewItem tviRef;
            string headerstring1 = GetRelationshipString(r);
            tviRef = new TreeViewItem { Header = headerstring1 };
            tviRef.ContextMenu = GetRelationshipContextMenu(r);
            tviRefLabel.Items.Add(tviRef);
            if (r.LastUsed > DateTime.Now - TimeSpan.FromSeconds(2))
                tviRef.Background = new SolidColorBrush(Colors.LightGreen);
            if (r.TimeToLive != TimeSpan.MaxValue && r.LastUsed + r.TimeToLive < DateTime.Now + TimeSpan.FromSeconds(3))
                tviRef.Background = new SolidColorBrush(Colors.LightYellow);
            totalItemCount++;
        }
        totalItemCount++;
    }


    //the treeview is populated only with expanded items or it would contain the entire UKS content
    //when an item is expanded, its content needs to be created in the treeview
    private void EmptyChild_Expanded(object sender, RoutedEventArgs e)
    {
        int count = 0;
        foreach (var item in theTreeView.Items)
        {
            count++;
            if (item is TreeViewItem tvi1)
                count += TreeviewItemCount(tvi1);
        }
        // what tree view item is this
        if (sender is TreeViewItem tvi)
        {
            string name = tvi.Header.ToString(); // to help debug
            Thing t = (Thing)tvi.GetValue(ThingObjectProperty);
            string parentLabel = "|" + t.Label;
            TreeViewItem tvi1 = tvi;
            int depth = 0;
            while (tvi1.Parent != null && tvi1.Parent is TreeViewItem tvi2)
            {
                tvi1 = tvi2;
                Thing t1 = (Thing)tvi1.GetValue(ThingObjectProperty);
                parentLabel = "|" + t1.Label + parentLabel;
                depth++;
            }
            if (!expandedItems.Contains(parentLabel))
            {
                expandedItems.Add(parentLabel);
                tvi.Items.Clear(); // delete empty child
                AddChildren(t, tvi, depth, parentLabel);
                AddRelationships(t, tvi, parentLabel);
                if (reverseCB.IsChecked == true)
                    AddRelationshipsFrom(t, tvi, parentLabel);
            }
        }
    }

    //Context Menu creation and handling
    private ContextMenu GetContextMenu(Thing t, TreeViewItem tvi)
    {
        ContextMenu menu = new ContextMenu();
        menu.SetValue(ThingObjectProperty, t);
        menu.SetValue(TreeViewItemProperty, tvi);
        ModuleUKS parent = (ModuleUKS)ParentModule;
        int ID = parent.theUKS.UKSList.IndexOf(t);
        MenuItem mi = new();
        string thingLabel = "___";
        if (t != null)
            thingLabel = t.Label;
        mi.Header = "Name: " + thingLabel + "  Index: " + ID;
        mi.IsEnabled = false;
        menu.Items.Add(mi);

        TextBox renameBox = new() { Text = thingLabel, Width = 200, Name = "RenameBox" };
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
        mi.Header = "Fetch GPT Info";
        menu.Items.Add(mi);
        mi = new();
        mi.Header = "Parents:";
        if (t.Parents.Count == 0)
            mi.Header = "Parents: NONE";
        mi.IsEnabled = false;
        menu.Items.Add(mi);
        foreach (Thing t1 in t.Parents)
        {
            mi = new();
            mi.Click += Mi_Click;
            mi.Header = "    " + t1.Label;
            mi.SetValue(ThingObjectProperty, t1);
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
            Thing t = (Thing)cm.GetValue(ThingObjectProperty);
            string testName = tb.Text + e.Key;
            Thing testThing = ThingLabels.GetThing(testName);
            if (testName != "" && testThing != null && testThing != t)
            {
                tb.Background = new SolidColorBrush(Colors.Pink);
                return;
            }
            tb.Background = new SolidColorBrush(Colors.White);
            if (e.Key == Key.Enter)
            {
                t.Label = tb.Text;
                cm.IsOpen = false;
            }
            if (e.Key == Key.Escape)
            {
                cm.IsOpen = false;
            }
        }
    }

    private ContextMenu GetRelationshipContextMenu(Relationship r)
    {
        ContextMenu menu = new ContextMenu();
        menu.SetValue(RelationshipObjectProperty, r);
        MenuItem mi = new();
        mi.Click += Mi_Click;
        mi.Header = "Delete";
        menu.Items.Add(mi);
        return menu;
    }

    private void Mi_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem mi)
        {
            UKS.UKS theUKS = ((ModuleUKS)ParentModule).theUKS;
            ContextMenu m = mi.Parent as ContextMenu;
            //handle setting parent to root
            Thing tParent = (Thing)mi.GetValue(ThingObjectProperty);
            if (tParent != null)
            {
                textBoxRoot.Text = tParent.Label;
                RefreshButton_Click(null, null);
            }
            Thing t = (Thing)m.GetValue(ThingObjectProperty);
            if (t == null)
            {
                Relationship r = (Relationship)m.GetValue(RelationshipObjectProperty);
                (r.source as Thing).RemoveRelationship(r);
                //force a repaint
                RefreshButton_Click(null, null);
                return;
            }
            ModuleUKS parent = (ModuleUKS)ParentModule;
            switch (mi.Header)
            {
                case "Expand All":
                    expandAll = t.Label;
                    expandedItems.Clear();
                    expandedItems.Add("|Thing|Object");
                    parent.SetAttribute("ExpandAll", expandAll);
                    updateFailed = true; //this forces the expanded items list not to rebuild
                    break;
                case "Collapse All":
                    expandAll = "";
                    expandedItems.Clear();
                    expandedItems.Add("|Thing|Object");
                    updateFailed = true;
                    parent.SetAttribute("ExpandAll", expandAll);
                    break;
                case "Fetch GPT Info":
                    //the following is an async call so an immediate refresh is not useful
                    ModuleGPTInfo.GetChatGPTData(t.Label);
                    break;
                case "Delete":
                    theUKS.DeleteAllChildren(t);
                    theUKS.DeleteThing(t);
                    break;
                case "Delete Child":
                    //figure out which item (and its parent) clicked us
                    TreeViewItem tvi = (TreeViewItem)m.GetValue(TreeViewItemProperty);
                    DependencyObject parent1 = VisualTreeHelper.GetParent((DependencyObject)tvi);
                    while (parent1 != null && !(parent1 is TreeViewItem))
                        parent1 = VisualTreeHelper.GetParent(parent1);
                    Thing parentThing = (Thing)parent1.GetValue(ThingObjectProperty);
                    //now delete the relationship
                    if (parentThing != null && t != null)
                        parentThing.RemoveChild(t);
                    break;
                case "Make Root":
                    textBoxRoot.Text = t.Label;
                    RefreshButton_Click(null, null);
                    break;
            }
            //force a repaint
            RefreshButton_Click(null, null);
        }
    }

    //if things are expanded and the details are displayed, this gets the thing name out of the header
    public static string LeftOfColon(string s)
    {
        int i = s.IndexOf(':');
        if (i != -1)
            s = s[..i];
        return s;
    }

    //keep track of which tree items are expanded
    private void FindExpandedItems(ItemCollection items, string parentLabel)
    {
        foreach (TreeViewItem tvi1 in items)
        {
            if (tvi1.IsExpanded)
            {
                if (!tvi1.Header.ToString().Contains("Relationships", StringComparison.CurrentCulture))
                {
                    expandedItems.Add(parentLabel + "|" + LeftOfColon(tvi1.Header.ToString()));
                }
                else if (tvi1.Header.ToString().IndexOf("RelationshipsFrom") != -1)
                {
                    expandedItems.Add(parentLabel + "|" + ":RelationshipsFrom");
                }
                else if (tvi1.Header.ToString().IndexOf("Relationships") != -1)
                {
                    expandedItems.Add(parentLabel + "|" + ":Relationships");
                }
            }
            FindExpandedItems(tvi1.Items, parentLabel + "|" + LeftOfColon(tvi1.Header.ToString()));
        }
    }

    private string ChildHasReferences(ModuleUKS UKS, Thing child, string header, int depth)
    {
        int childCount = child.Children.Count;
        int count = child.Relationships.Count - childCount;
        if (count > 0)
        {
            if (detailsCB.IsChecked == true)
                header += " Rels:" + count;
        }
        return header;
    }


    private string GetRelationshipString(Relationship r)
    {
        string retVal = "";
        if (r.relType is null || r.relType.Label != "has-child")
            retVal = r.ToString() + " ";
        if (detailsCB.IsChecked == true)
            retVal = "<" + r.Weight.ToString("f2") + "," + (r.TimeToLive == TimeSpan.MaxValue ? "∞" : (r.LastUsed + r.TimeToLive - DateTime.Now).ToString(@"mm\:ss")) + "> " + retVal;
        return retVal;
    }


    //for debug/test
    int TreeviewItemCount(TreeViewItem tvi)
    {
        int retVal = tvi.Items.Count;
        foreach (TreeViewItem item in tvi.Items)
        {
            retVal += TreeviewItemCount(item);
        }
        return retVal;
    }

    int CountNonChildRelationships(IList<Relationship> list)
    {
        return list.Count - list.Count(x => x.relType?.Label == "has-child");
    }


    //EVENTS
    private void TheTreeView_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        Draw(true);
    }

    private void TextBoxRoot_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        //ModuleUKS parent = (ModuleUKS)ParentModule;
        //parent.SetAttribute("Root", textBoxRoot.Text);
        //RefreshButton_Click(null, null);
    }
    private void textBoxRoot_TextChanged(object sender, TextChangedEventArgs e)
    {
        ModuleUKS parent = (ModuleUKS)ParentModule;
        if (parent == null) return;
        parent.SetAttribute("Root", textBoxRoot.Text);
        RefreshButton_Click(null, null);

    }

    //using the mouse-wheel while pressing ctrl key changes the font size
    private void theTreeView_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if ((Keyboard.GetKeyStates(Key.LeftCtrl) & KeyStates.Down | Keyboard.GetKeyStates(Key.RightCtrl) & KeyStates.Down) != 0)
        {
            if (e.Delta > 0)
            {
                if (theTreeView.FontSize > 1)
                    theTreeView.FontSize -= 1;
            }
            else if (e.Delta < 0)
            {
                theTreeView.FontSize += 1;
            }
        }
    }

    private void CheckBoxAuto_Checked(object sender, RoutedEventArgs e)
    {
        dt = new DispatcherTimer
        {
            Interval = new TimeSpan(0, 0, 0, 0, 200)
        };
        dt.Tick += Dt_Tick;
        dt.Start();
    }

    private void Dt_Tick(object sender, EventArgs e)
    {
        if (!mouseInTree)
            Draw(true);
    }

    private void CheckBoxAuto_Unchecked(object sender, RoutedEventArgs e)
    {
        dt.Stop();
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
        mouseInTree = true;
        theTreeView.Background = new SolidColorBrush(Colors.LightSteelBlue);
    }
    private void TheTreeView_MouseLeave(object sender, MouseEventArgs e)
    {
        mouseInTree = false;
        theTreeView.Background = new SolidColorBrush(Colors.LightGray);
    }


    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!updateFailed)
            {
                expandedItems.Clear();

                expandedItems.Add("|Thing|Object");
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

        //parent.theUKS.UKSList.Clear();
        for (int i = 0; i < parent.theUKS.UKSList.Count; i++)
        {
            Thing t = parent.theUKS.UKSList[i];
            if (t.HasAncestorLabeled("BrainSim")) 
                continue;
            if (t.Label == "has-child") continue;
            if (t.Label == "Thing") continue;
            if (t.Label == "RelationshipType") continue;
            if (t.Label == "hasAttribute") continue;
            if (t != null)
            {
                //                    parent.theUKS.DeleteAllChildren(t);
                parent.theUKS.DeleteThing(t);
                i--;
            }
        }

        parent.theUKS.CreateInitialStructure();

        CollapseAll();
        expandAll = parent.GetAttribute("ExpandAll");
        if (expandAll == null) expandAll = "";
        string root = parent.GetAttribute("root");
        if (string.IsNullOrEmpty(root)) root = "Thing";
        textBoxRoot.Text = root;
        RefreshButton_Click(null, null);
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
        textBoxRoot.Text = parent.GetAttribute("Root");
    }
}