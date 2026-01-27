//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

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

    public static readonly DependencyProperty ThingObjectProperty =
    DependencyProperty.Register("Thought", typeof(Thought), typeof(TreeViewItem));
    public static readonly DependencyProperty TreeViewItemProperty =
    DependencyProperty.Register("TreeViewItem", typeof(TreeViewItem), typeof(TreeViewItem));
    public static readonly DependencyProperty LinkObjectProperty =
    DependencyProperty.Register("LinkType", typeof(Thought), typeof(TreeViewItem));


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
        Refresh();
        return true;
    }

    private void UpdateStatusLabel()
    {
        int childCount = 0;
        int refCount = 0;
        ModuleUKS parent = (ModuleUKS)ParentModule;
        Thought t = null;
        try
        {
            foreach (Thought t1 in parent.theUKS.AllThings)
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
        statusLabel.Content = parent.theUKS.AllThings.Count + " Things  " + (childCount + refCount) + " Rels.";
        Title = "The Universal Knowledgs Store (UKS)  --  File: " + Path.GetFileNameWithoutExtension(parent.theUKS.FileName);
    }

    private void LoadContentToTreeView()
    {
        ModuleUKS parent = (ModuleUKS)ParentModule;
        expandAll = parent.GetSavedDlgAttribute("ExpandAll");
        string root = parent.GetSavedDlgAttribute("Root");
        string sizeString = parent.GetSavedDlgAttribute("fontSize");
        int.TryParse(sizeString, out int fontSize);
        if (fontSize != 0)
            theTreeView.FontSize = fontSize;
        if (root is null)
        {
            root = "Thought";
            parent.SetSavedDlgAttribute("Root", root);
        }
        Thought thought = parent.theUKS.Labeled(root);
        if (thought is not null)
        {
            totalItemCount = 0;
            TreeViewItem tvi = new() { Header = thought.ToString() };
            tvi.ContextMenu = GetContextMenu(thought, tvi);
            tvi.IsExpanded = true; //always expand the top-level item
            theTreeView.Items.Add(tvi);
            tvi.SetValue(ThingObjectProperty, thought);
            totalItemCount++;
            AddChildren(thought, tvi, 0, thought.Label);
            AddLinks(thought, tvi, "");
            if (reverseCB.IsChecked == true)
                AddLinksFrom(thought, tvi, "");
        }
        else if (string.IsNullOrEmpty(root)) //search for unattached Things
        {
            try //ignore problems of collection modified
            {
                foreach (Thought t1 in parent.theUKS.AllThings)
                {
                    if (t1.Parents.Count == 0)
                    {
                        TreeViewItem tvi = new() { Header = t1.Label };
                        tvi.ContextMenu = GetContextMenu(t1, tvi);
                        theTreeView.Items.Add(tvi);
                    }
                }
            }
            catch { updateFailed = true; }
        }
    }
    private void AddChildren(Thought t, TreeViewItem tvi, int depth, string parentLabel)
    {
        if (totalItemCount > 500) return;

        List<Thought> theChildren = t.LinksFrom.Where(x => x.LinkType.Label.StartsWith("is-a") && x.To is not null).ToList();
        theChildren = theChildren.OrderBy(x => x.From.Label).ToList();

        ModuleUKS UKS = (ModuleUKS)ParentModule;

        foreach (Thought r in theChildren)
        {
            if (totalItemCount > 500) return;
            var child = r.From;
            string header = child.ToString();
            if (header == "") header = "\u25A1"; //put in a small empty box--if the header is completely empty, or you can never right-click 
            if (r.Weight != 1 && detailsCB.IsChecked == true) //prepend weight for probabIReadOnlyListic children
                header = "<" + r.Weight.ToString("f2") + "," + (r.TimeToLive == TimeSpan.MaxValue ? "∞" : (r.LastFiredTime + r.TimeToLive - DateTime.Now).ToString(@"mm\:ss")) + "> " + header;
            if (r.LinkType.HasLink(null, null, UKS.theUKS.Labeled("not")) is not null) //prepend ! for negative  children
                header = "!" + header;
            if (detailsCB.IsChecked == true)
                header += ":" + child.Children.Count;
            if (child.LinksTo.Count > 0)
                header = ChildHasReferences(UKS, child, header, depth);

            if (showConditionals.IsChecked == true && r.LinkType?.Label == "is-a") //hack to show conditions on is-a links
                foreach (Thought r1 in r.LinksTo)
                    header += "  " + r1.ToString();

            TreeViewItem tviChild = new() { Header = header };

            //change color of things which just fired or are about to expire
            tviChild.SetValue(ThingObjectProperty, child);
            Thought mostRecent = UKS.theUKS.Labeled("mostRecent");
            mostRecent = mostRecent?.LinksTo.FindFirst(x => x.LinkType.Label == "is")?.To;
            if (child == mostRecent)
                tviChild.Background = new SolidColorBrush(Colors.Pink);
            if (child.LastFiredTime > DateTime.Now - TimeSpan.FromMilliseconds(500))
                tviChild.Background = new SolidColorBrush(Colors.LightGreen);
            if (r.TimeToLive != TimeSpan.MaxValue && r.LastFiredTime + r.TimeToLive < DateTime.Now + TimeSpan.FromSeconds(3))
                tviChild.Background = new SolidColorBrush(Colors.LightYellow);

            if (expandedItems.Contains("|" + parentLabel + "|" + LeftOfColon(header)))
                tviChild.IsExpanded = true;
            if (r.From.AncestorList().Contains(expandAll) &&
                (child.Label == "" || !parentLabel.Contains("|" + child.Label)))
                tviChild.IsExpanded = true;

            tvi.Items.Add(tviChild);

            totalItemCount++;
            tviChild.ContextMenu = GetContextMenu(child, tviChild);
            if (depth < maxDepth)
            {
                int childCount = child.Children.Count;
                int relCount = CountNonChildLinks(child.LinksTo);
                int relFromCount = CountNonChildLinks(child.LinksFrom);
                if (tviChild.IsExpanded)
                {
                    // load children and references
                    AddChildren(child, tviChild, depth + 1, parentLabel + "|" + child.Label);
                    AddLinks(child, tviChild, parentLabel);
                    if (reverseCB.IsChecked == true)
                        AddLinksFrom(child, tviChild, parentLabel);
                }
                else if (child.Children.Count > 0 ||
                    CountNonChildLinks(child.LinksTo) > 0
                    || CountNonChildLinks(child.LinksFrom) > 0)
                {
                    // don't load those that aren't expanded, put in a dummy instead so there is an expander-handle
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

    private void AddLinks(Thought t, TreeViewItem tvi, string parentLabel)
    {
        if (t.Label.StartsWith("cat-s"))
        { }
        if (CountNonChildLinks(t.LinksTo) == 0)
        {
            //Possible IMPROVEMENT to be able to see other links of unlabeled links
            //if (t.Source is Thought r)
            //  AddLinks(r, tvi, parentLabel);
            //if (t.Target is Thought r1)
            //    AddLinks(r1, tvi, parentLabel);
            if (t.From is null && t.To is null)
                return;
        }
        TreeViewItem tviLinksHeader = new() { Header = "Links: " };
        if (detailsCB.IsChecked == true)
            tviLinksHeader.Header += CountNonChildLinks(t.LinksTo).ToString();

        //add the "Links:" entry
        string fullString = "|" + parentLabel + "|" + t.ToString() + "|Links:";
        fullString = fullString.Replace("||", "|"); //needed to make top level work
        if (expandedItems.Contains(fullString))
            tviLinksHeader.IsExpanded = true;
        if (t.AncestorList().Contains(ThoughtLabels.GetThing(expandAll)))
            tviLinksHeader.IsExpanded = true;
        if (t.Children.Count == 0)
            tviLinksHeader.IsExpanded = true;
        tvi.Items.Add(tviLinksHeader);
        totalItemCount++;

        //For sequences
        ModuleUKS parent = (ModuleUKS)ParentModule;
        if (parent.theUKS.IsSequenceElement(t))
        {
            TreeViewItem tviSeqHeader = new() { Header = $"{t?.Label}->{t.LinkType?.Label}->{t?.To?.Label}" };
            tviLinksHeader.Items.Add(tviSeqHeader);
            tviSeqHeader.Expanded += EmptyChild_Expanded;
            tviSeqHeader.SetValue(ThingObjectProperty, t.To);
        }

        //add each of the links as a "child" of the "Links:" entry    
        IReadOnlyList<Thought> sortedLinks = t.LinksTo.OrderBy(x => x?.LinkType?.Label).ToList();
        foreach (Thought r in sortedLinks)
        {
            if (r?.LinkType?.Label == "is-a") continue;
            if (r is null) continue;
            //if (showConditionals.IsChecked != true && (r.HasProperty("isCondition") || r.HasProperty("isResult"))) continue; //hide conditionals

            TreeViewItem tviRel = new() { Header = GetLinkString(r), };
            if (t.HasProperty("isCondition") || t.HasProperty("isResult"))
                tviRel.Header = "*" + tviRel.Header;
            else if (r.HasProperty("isCondition") || r.HasProperty("isResult"))
                tviRel.Header = "*" + tviRel.Header;
            //if (r.Source != t) tviRel.Header = r.Source?.Label + "->" + tviRel.Header;
            //context menu
            tviRel.ContextMenu = GetLinkContextMenu(r);
            tviLinksHeader.Items.Add(tviRel);

            //special colors
            if (r.LastFiredTime > DateTime.Now - TimeSpan.FromSeconds(2))
                tviRel.Background = new SolidColorBrush(Colors.LightGreen);
            if (r.TimeToLive != TimeSpan.MaxValue && r.LastFiredTime + r.TimeToLive < DateTime.Now + TimeSpan.FromSeconds(3))
                tviRel.Background = new SolidColorBrush(Colors.LightYellow);
            totalItemCount++;

            //is this item expanded
            string tempString = fullString + "|" + tviRel.Header;
            if (expandedItems.Contains(tempString))
                tviRel.IsExpanded = true;

            //make this link expandable
            tviRel.SetValue(ThingObjectProperty, r);
            tviRel.Expanded += EmptyChild_Expanded;

            if (r.LinksTo.Count > 0)
                AddLinks(r, tviRel, fullString);
            //AddLinks(r.Target, tviRef, fullString);
        }
    }


    private void AddLinksFrom(Thought t, TreeViewItem tvi, string parentLabel)
    {
        if (CountNonChildLinks(t.LinksFrom) == 0) return;
        TreeViewItem tveLinkHeader = new() { Header = "LinksFrom: " };
        if (detailsCB.IsChecked == true)
            tveLinkHeader.Header += CountNonChildLinks(t.LinksFrom).ToString();

        string fullString = "|" + parentLabel + "|" + t.Label + "|:LinksFrom";
        fullString = fullString.Replace("||", "|"); //needed to make top level work
        if (expandedItems.Contains(fullString))
            tveLinkHeader.IsExpanded = true;
        if (t.AncestorList().Contains(ThoughtLabels.GetThing(expandAll)))
            tveLinkHeader.IsExpanded = true;
        tvi.Items.Add(tveLinkHeader);

        foreach (Thought r in t.LinksFrom)
        {
            if (r.LinkType?.Label == "has-child") continue;
            string headerstring1 = GetLinkString(r);
            TreeViewItem tviRel = new TreeViewItem { Header = headerstring1 };
            if (t.HasProperty("isCondition") || t.HasProperty("isResult"))
                tviRel.Header = "*" + tviRel.Header;
            else if (r.HasProperty("isCondition") || r.HasProperty("isResult"))
                tviRel.Header = "*" + tviRel.Header;

            tviRel.ContextMenu = GetLinkContextMenu(r);
            tveLinkHeader.Items.Add(tviRel);
            if (r.LastFiredTime > DateTime.Now - TimeSpan.FromSeconds(2))
                tviRel.Background = new SolidColorBrush(Colors.LightGreen);
            if (r.TimeToLive != TimeSpan.MaxValue && r.LastFiredTime + r.TimeToLive < DateTime.Now + TimeSpan.FromSeconds(3))
                tviRel.Background = new SolidColorBrush(Colors.LightYellow);
            totalItemCount++;
        }
        totalItemCount++;
    }


    //the treeview is populated only with expanded items or it would contain the entire UKS content
    //when an item is expanded, its content needs to be created in the treeview
    private void EmptyChild_Expanded(object sender, RoutedEventArgs e)
    {
        // what tree view item is this
        if (sender is TreeViewItem tvi)
        {
            string name = tvi.Header.ToString(); // to help debug
            Thought t = (Thought)tvi.GetValue(ThingObjectProperty);
            string parentLabel = "|" + t.ToString();
            TreeViewItem tvi1 = tvi;
            int depth = 0;
            while (tvi1.Parent is not null && tvi1.Parent is TreeViewItem tvi2)
            {
                tvi1 = tvi2;
                Thought t1 = (Thought)tvi1.GetValue(ThingObjectProperty);
                if (t1 is not null)
                    parentLabel = "|" + t1.ToString() + parentLabel;
                else
                    parentLabel = "|" + "Links:" + parentLabel;
                depth++;
            }
            if (!expandedItems.Contains(parentLabel))
            {
                expandedItems.Add(parentLabel);
                tvi.Items.Clear(); // delete empty child
                if (t.Children.Count > 0)
                    AddChildren(t, tvi, depth, parentLabel);
                if (t.LinksTo.Count > 0)
                    AddLinks(t, tvi, parentLabel);
                if (reverseCB.IsChecked == true && t.LinksFrom.Count > 0)
                    AddLinksFrom(t, tvi, parentLabel);

                //for seqnece expansion
                ModuleUKS parent = (ModuleUKS)ParentModule;
                if (parent.theUKS.IsSequenceElement(t.To))
                {
                    AddLinks(t.To, tvi, parentLabel);
                }
            }
        }
    }

    //Context Menu creation and handling
    private ContextMenu GetContextMenu(Thought t, TreeViewItem tvi)
    {
        ContextMenu menu = new ContextMenu();
        menu.SetValue(ThingObjectProperty, t);
        menu.SetValue(TreeViewItemProperty, tvi);
        ModuleUKS parent = (ModuleUKS)ParentModule;
        int ID = parent.theUKS.AllThings.IndexOf(t);
        MenuItem mi = new();
        string thingLabel = "___";
        if (t is not null)
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
            Thought t = (Thought)cm.GetValue(ThingObjectProperty);
            string testName = tb.Text + e.Key;
            Thought testThing = ThoughtLabels.GetThing(testName);
            if (testName != "" && testThing is not null && testThing != t)
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
        mi.SetValue(ThingObjectProperty, r.From);
        menu.Items.Add(mi);

        mi = new();
        mi.Click += Mi_Click;
        mi.Header = "    " + r.LinkType.Label;
        mi.SetValue(ThingObjectProperty, r.LinkType);
        menu.Items.Add(mi);

        mi = new();
        mi.Click += Mi_Click;
        mi.Header = "    " + r.To?.Label;
        mi.SetValue(ThingObjectProperty, r.To);
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
            Thought tParent = (Thought)mi.GetValue(ThingObjectProperty);
            if (tParent is not null)
            {
                textBoxRoot.Text = tParent.Label;
                Refresh();
            }
            Thought t = (Thought)m.GetValue(ThingObjectProperty);
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
                    theUKS.DeleteThing(t);
                    break;
                case "Delete Child":
                    //figure out which item (and its parent) clicked us
                    TreeViewItem tvi = (TreeViewItem)m.GetValue(TreeViewItemProperty);
                    DependencyObject parent1 = VisualTreeHelper.GetParent((DependencyObject)tvi);
                    while (parent1 is not null && !(parent1 is TreeViewItem))
                        parent1 = VisualTreeHelper.GetParent(parent1);
                    Thought parentThing = (Thought)parent1.GetValue(ThingObjectProperty);
                    //now delete the link
                    if (parentThing is not null && t is not null)
                        parentThing.RemoveChild(t);
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

    //if things are expanded and the details are displayed, this gets the thought name out of the header
    public static string LeftOfColon(string s)
    {
        int i = s.IndexOf(':');
        i++;
        if (i != 0)
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
                if (!tvi1.Header.ToString().Contains("Links:", StringComparison.CurrentCulture))
                {
                    expandedItems.Add(parentLabel + "|" + LeftOfColon(tvi1.Header.ToString()));
                }
                else if (tvi1.Header.ToString().IndexOf("LinksFrom") != -1)
                {
                    expandedItems.Add(parentLabel + "|" + "LinksFrom:");
                }
                else if (tvi1.Header.ToString().IndexOf("Links:") != -1)
                {
                    expandedItems.Add(parentLabel + "|" + "Links:");
                }
            }
            FindExpandedItems(tvi1.Items, parentLabel + "|" + LeftOfColon(tvi1.Header.ToString()));
        }
    }

    private string ChildHasReferences(ModuleUKS UKS, Thought child, string header, int depth)
    {
        int childCount = child.Children.Count;
        int count = child.LinksTo.Count - childCount;
        if (count > 0)
        {
            if (detailsCB.IsChecked == true)
                header += " Rels:" + count;
        }
        return header;
    }


    private string GetLinkString(Thought r)
    {
        string retVal = r?.ToString();
        //        if (r.RelType is null || r.RelType.Label != "has-child")
        //            retVal = r.ToString() + " ";
        if (detailsCB.IsChecked == true)
            retVal = "<" + r.Weight.ToString("f2") + "," + (r.TimeToLive == TimeSpan.MaxValue ? "∞" : (r.LastFiredTime + r.TimeToLive - DateTime.Now).ToString(@"mm\:ss")) + "> " + retVal;
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

    int CountNonChildLinks(IReadOnlyList<Thought> list)
    {
        return list.Count - list.Count(x => x?.LinkType?.Label == "is-a");
    }


    //EVENTS
    private bool _isTextChangingInternally;
    private void TheTreeView_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        Draw(true);
    }

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
            if (suggestion is not null) suggestion = ThoughtLabels.GetThing(suggestion).Label;

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


    private void Refresh()
    {
        try
        {
            if (!updateFailed)
            {
                expandedItems.Clear();

                expandedItems.Add("|Thought|Object");
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

        parent.theUKS.CreateInitialStructure();
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
            await Task.Run(() => parent.theUKS.ImportTextFile(path));

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
            await Task.Run(() => parent.theUKS.ExportTextFile(root, path));
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