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

namespace BrainSimulator.Modules;

public partial class ModuleUKSDlg : ModuleBaseDlg
{

    public static readonly DependencyProperty ThingObjectProperty =
    DependencyProperty.Register("Thing", typeof(Thing), typeof(TreeViewItem));
    public static readonly DependencyProperty RelationshipObjectProperty =
    DependencyProperty.Register("RelationshipType", typeof(Relationship), typeof(TreeViewItem));


    private const int maxDepth = 20;
    private int totalItemCount;
    private bool mouseInTree; //prevent auto-update while the mouse is in the tree
    private bool busy;
    private List<string> expandedItems = new();
    private int charsPerLine = 60;
    private bool updateFailed;
    private List<Thing> uks;
    private int maxChildrenWhenCollapsed = 20;
    private DispatcherTimer dt;

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
        RefreshButton_Click(null, null);
        return true;
    }

    //this is here because clearing just the top-level tree creates a big memory leak.
    private static void ClearTreeViewItems(TreeViewItem t)
    {
        foreach (TreeViewItem t1 in t.Items)
            ClearTreeViewItems(t1);
        t.Items.Clear();
    }

    private void RefreshSetUp()
    {
        busy = true;
        //if (MainWindow.currentFileName.Contains("DemoNetworkVirtual"))
        //theTreeView.FontSize = 18; //use up/down arrows in the "Root" textbox
        //figure out how wide the treeview is so we can wrap the text reasonably
        Typeface typeFace = new(theTreeView.FontFamily.ToString());
        FormattedText ft = new("xxxxxxxxxx", System.Globalization.
            CultureInfo.CurrentCulture,
            theTreeView.FlowDirection,
            typeFace,
            theTreeView.FontSize,
            theTreeView.Foreground,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);
        charsPerLine = (int)(10 * theTreeView.ActualWidth / ft.Width);
        charsPerLine -= 20; //leave a little margin...the indent is calculated for individual entries
    }

    private void UpdateStatusLabel()
    {
        int childCount = 0;
        int refCount = 0;
        ModuleUKS parent = (ModuleUKS)ParentModule;
        Thing t = null;
        uks = parent.GetTheUKS();
        try
        {
            foreach (Thing t1 in uks)
            {
                t = t1;
                childCount += t1.Children.Count;
                refCount += t1.RelationshipsWithoutChildren.Count;
            }
        }

        catch (Exception ex)
        {
            //IList<Thing> ch = t.Children;
            return;  //don't update counts display if they are wrong
        }
        statusLabel.Content = uks.Count + " Things  " + (childCount + refCount) + " Relationships";
        Title = "UKS: Knowledge Base: " + Path.GetFileNameWithoutExtension(parent.fileName);
    }

    private void LoadChildrenToTreeView()
    {
        string root = textBoxRoot.Text;
        ModuleUKS parent = (ModuleUKS)ParentModule;
        Thing thing = parent.Labeled(root);
        if (thing is not null)
        {
            totalItemCount = 0;
            TreeViewItem tvi = new() { Header = thing.Label };
            tvi.ContextMenu = GetContextMenu(thing);
            tvi.IsExpanded = true; //always expand the top-level item
            theTreeView.Items.Add(tvi);
            tvi.SetValue(ThingObjectProperty, thing);
            totalItemCount++;
            if (tvi.IsExpanded)
            {
                AddChildren(thing, tvi, 0, thing.Label);
            }
        }
        else if (string.IsNullOrEmpty(root)) //search for unattached Things
        {
            try //ignore problems of collection modified
            {
                foreach (Thing t1 in uks)
                {
                    if (t1.Parents.Count == 0)
                    {
                        TreeViewItem tvi = new() { Header = t1.Label };
                        tvi.ContextMenu = GetContextMenu(t1);
                        theTreeView.Items.Add(tvi);
                    }
                }
            }
            catch { updateFailed = true; }
        }
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshSetUp();

        try
        {
            if (!updateFailed)
            {
                expandedItems.Clear();
                //for demo purposes
//                expandedItems.Add("|Thing|MentalModel");
                expandedItems.Add("|Thing|Object");
                FindExpandedItems(theTreeView.Items, "");
            }
            updateFailed = false;

            UpdateStatusLabel();

            theTreeView.Items.Clear();
            LoadChildrenToTreeView();
        }
        catch
        {
            updateFailed = true;
        }
        busy = false;
    }

    private ContextMenu GetContextMenu(Thing t)
    {
        ContextMenu menu = new ContextMenu();
        menu.SetValue(ThingObjectProperty, t);
        int ID = uks.IndexOf(t);
        MenuItem mi = new();
        string thingLabel = "___";
        if (t != null)
            thingLabel = t.Label;
        mi.Header = "Name: " + thingLabel + "  Index:" + ID;
        mi.IsEnabled = false;
        menu.Items.Add(mi);

        TextBox renameBox = new(){ Text = thingLabel,Width=200 };
        renameBox.PreviewKeyDown += RenameBox_PreviewKeyDown;
        mi = new();
        mi.Header = renameBox;
        menu.Items.Add(mi);
        mi = new();
        mi.Click += Mi_Click;
        mi.Header = "Delete";
        menu.Items.Add(mi);
        mi = new();
        mi.Click += Mi_Click;
        mi.Header = "Make Root";
        menu.Items.Add(mi);
        mi = new();
        mi.Click += Mi_Click;
        mi.Header = "Show All";
        menu.Items.Add(mi);
        mi = new();
        mi.Click += Mi_Click;
        mi.Header = "Add Types";
        menu.Items.Add(mi);
        mi = new();
        mi.Click += Mi_Click;
        mi.Header = "Add Actions";
        menu.Items.Add(mi);
        mi = new();
        mi.Click += Mi_Click;
        mi.Header = "Add Parts";
        menu.Items.Add(mi);
        mi = new();
        mi.Click += Mi_Click;
        mi.Header = "Remove Hits and Access Count";
        menu.Items.Add(mi);
        mi = new();
        mi.Header = "Parents:";
        if (t.Parents.Count == 0)
            mi.Header = "Parents: NONE";
        menu.Items.Add(mi);
        foreach (Thing t1 in t.Parents)
        {
            mi = new();
            mi.Click += Mi_Click;
            mi.Header = "    " + t1.Label;
            mi.SetValue(ThingObjectProperty, t1);
            menu.Items.Add(mi);
        }
        return menu;
    }

    private void RenameBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is System.Windows.Controls.TextBox tb)
        {
            MenuItem mi = tb.Parent as MenuItem;
            ContextMenu cm = mi.Parent as ContextMenu;
            Thing t = (Thing)cm.GetValue(ThingObjectProperty);

            if (e.Key == Key.Enter)
            {
                t.Label = tb.Text;
                cm.IsOpen = false;
            }
            if (e.Key == Key.Escape)
            {
                cm.IsOpen= false;
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
            ModuleUKS uks = (ModuleUKS)ParentModule;
            ContextMenu m = mi.Parent as ContextMenu;
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
            switch (mi.Header)
            {
                case "Show All":
                    textBoxRoot.Text = "Thing";
                    RefreshButton_Click(null, null);
                    break;
                case "Add Types":
                    ModuleOnlineInfo moi = (ModuleOnlineInfo)MainWindow.BrainSim3Data.modules.FindFirst(x => x.Label == "OnlineInfo");
                    if (moi != null)
                        moi.GetChatGPTData(t.Label, ModuleOnlineInfo.QueryType.types);
                    break;
                case "Add Parts":
                    moi = (ModuleOnlineInfo)MainWindow.BrainSim3Data.modules.FindFirst(x => x.Label == "OnlineInfo");
                    if (moi != null)
                        moi.GetChatGPTData(t.Label, ModuleOnlineInfo.QueryType.partsOf);
                    break;
                //case "Add Types":
                //    moi = (ModuleOnlineInfo)MainWindow.modules.FindFirst(x => x.Label == "OnlineInfo")?;
                //    if (moi != null)
                //        moi.GetChatGPTData(t.Label, ModuleOnlineInfo.QueryType.list);
                //    break;
                case "Add Actions":
                    moi = (ModuleOnlineInfo)MainWindow.BrainSim3Data.modules.FindFirst(x => x.Label == "OnlineInfo");
                    if (moi != null)
                        moi.GetChatGPTData(t.Label, ModuleOnlineInfo.QueryType.can);
                    break;
                case "Delete":
                    uks.DeleteAllChildren(t);
                    uks.DeleteThing(t);
                    break;
                case "Make Root":
                    textBoxRoot.Text = t.Label;
                    RefreshButton_Click(null, null);
                    break;
                case "Remove Hits and Access Count":
                    foreach (Relationship relationship in t.Relationships)
                    {
                        relationship.ClearHits();
                        relationship.ClearAccessCount();
                    }
                    break;
            }
            //force a repaint
            RefreshButton_Click(null, null);
        }
    }

    public static string LeftOfColon(string s)
    {
        int i = s.IndexOf(':');
        if (i != -1)
        {
            s = s[..i];
        }
        return s;
    }

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
        int count = child.RelationshipsWithoutChildren.Count;
        if (count > 0)
        {
            header += " Refs:" + child.RelationshipsWithoutChildren.Count;
        }
        //List<Relationship> sortedRelationships = child.RelationshipsNoCount.OrderBy(x => -x.Value1).ToList();
        //bool trueCount = true;

        //for (int j = 0; j < sortedRelationships.Count; j++)
        //{
        //    Relationship l = sortedRelationships[j];
        //    if (checkBoxNARS.IsChecked == false && l.weight <= .5) continue;

        //    if (l.reltype != null && Relationship.TrimDigits(l.relType?.Label) != "has-child")
        //    {
        //        if (header.Length - header.LastIndexOf('\n') > charsPerLine - 6 * depth)
        //            header += "\n";
        //        header += GetRelationshipString(l);
        //        if (j > maxChildrenWhenCollapsed)
        //        {
        //            trueCount = false;
        //            break;
        //        }
        //    }
        //}
        //if (!trueCount)
        //{
        //    header += "...";
        //}
        return header;
    }


    private string GetRelationshipString(Relationship l)
    {
        string retVal = "";
        if (l.relType is null || l.relType.Label != "has-child")
            retVal = l.ToString() + " ";
        if (checkBoxNARS.IsChecked == true  &&  (l.weight != 1 || l.inferred))
        {
            retVal = "<" + l.weight.ToString("f2") + "> " + retVal;
        }
        return retVal;
    }

    private void AddChildren(Thing t, TreeViewItem tvi, int depth, string parentLabel)
    {
        if (totalItemCount > 3000) return;

        List<Relationship> theChildren = new();
        foreach (Relationship r in t.Relationships)
        {
            if (Relationship.TrimDigits(r.reltype?.Label) == "has-child" && r.target != null)
            {
                theChildren.Add(r);
            }
        }
        theChildren = theChildren.OrderBy(x => x.target.Label).ToList();

        ModuleUKS UKS = (ModuleUKS)ParentModule;
        Thing shapeRoot = UKS.Labeled("shp");
        Thing colorRoot = UKS.Labeled("col");
        Thing visibleRoot = UKS.Labeled("vsb");
        Thing sizeRoot = UKS.Labeled("siz");
        Thing appearingRoot = UKS.Labeled("app");

        foreach (Relationship r in theChildren)
        {
            Thing child = r.target;
            int descCount = child.GetDescendentsCount();
            string descCountStr = (descCount < 5000) ? descCount.ToString() : "****";
            string header = child.Label;
            if (r.weight != 1 && checkBoxNARS.IsChecked == true) //prepend weight for probabilistic children
                header = "<"+r.weight.ToString("f2")+">"+header;
            if (r.reltype.HasRelationship(UKS.Labeled("not")) != null) //prepend ! for negative  children
                header = "!" + header;

            header += ":" + child.Children.Count + "," + descCountStr;
            if (child.Label.StartsWith("po") || child.Label.StartsWith("io"))
            {
                header += " [";
                if (colorRoot is not null)
                {
                    Thing tcol = child.HasRelationshipWithParent(colorRoot);
                    if (tcol is not null) header += FindPropertyName(tcol) + ", ";
                }
                if (sizeRoot is not null)
                {
                    Thing tsiz = child.HasRelationshipWithParent(sizeRoot);
                    if (tsiz is not null) header += FindPropertyName(tsiz).Substring(0, 4) + ", ";
                }
                if (visibleRoot is not null)
                {
                    Thing tvsb = child.HasRelationshipWithParent(visibleRoot);
                    if (tvsb is not null) header += FindPropertyName(tvsb) + ", ";
                }
                if (appearingRoot is not null)
                {
                    Thing tapp = child.HasRelationshipWithParent(appearingRoot);
                    if (tapp is not null) header += FindPropertyName(tapp) + ", ";
                }
                if (shapeRoot is not null)
                {
                    Thing tshp = child.HasRelationshipWithParent(shapeRoot);
                    if (tshp is not null) header += FindPropertyName(tshp);
                }
                header += "]";
            }
            else if (child.RelationshipsNoCount.Count > 0)
            {
                header = ChildHasReferences(UKS, child, header, depth);
            }
            if (child.V is not null)
            {
                if (child.V is int iVal)
                    header += " : " + iVal.ToString("X");
                else if (child.V is List<Point3DPlus>)
                    header += " : Point List";
                //else if (child.V is not UnknownArea)
                //    header += " : " + child.V.ToString();
            }

            TreeViewItem tviChild = new() { Header = header };

            if (child.Label.StartsWith("cv")&& !child.HasAncestorLabeled("Object"))
            {
                HSLColor hslColor = (HSLColor)child.V;
                Color backgroundColor = hslColor.ToColor();
                tviChild.Background = new SolidColorBrush(backgroundColor);
                double Y = 0.2126 * backgroundColor.ScR + 0.7152 * backgroundColor.ScG + 0.0722 * backgroundColor.ScB;
                tviChild.Foreground = Y > 0.4 ? Brushes.Black : Brushes.White;
            }

            tviChild.SetValue(ThingObjectProperty, child);
            if (child.lastFiredTime > DateTime.Now - TimeSpan.FromSeconds(2))
                tviChild.Background = new SolidColorBrush(Colors.LightGreen);

            if (expandedItems.Contains("|" + parentLabel + "|" + LeftOfColon(header)))
                tviChild.IsExpanded = true;
            tvi.Items.Add(tviChild);
            totalItemCount++;
            tviChild.ContextMenu = GetContextMenu(child);
            if (depth < maxDepth)
            {
                if (tviChild.IsExpanded)
                {
                    // load children and references
                    AddChildren(child, tviChild, depth + 1, parentLabel + "|" + child.Label);
                    AddReferences(child, tviChild, parentLabel);
                    AddReferencedBy(child, tviChild, parentLabel);
                }
                else if (child.Children.Count > 0 ||
                    CountNonChildRelationships(child.RelationshipsNoCount) > 0
                    || CountNonChildRelationships(child.RelationshipsFrom) > 0)
                {
                    // don't load those that aren't expanded
                    TreeViewItem emptyChild = new() { Header = "" };
                    tviChild.Items.Add(emptyChild);
                    tviChild.Expanded += EmptyChild_Expanded;
                }
            }
        }
    }

    private string FindPropertyName(Thing property)
    {
        ModuleUKS UKS = (ModuleUKS)base.ParentModule;
        Thing objectRoot = UKS.GetOrAddThing("Object", "Thing");
        if (property.GetRelationshipByWithAncestor(objectRoot) == null)
        {
            return property.Label;
        }
        else
        {
            foreach (Relationship l in property.GetRelationshipByWithAncestor(objectRoot))
            {
                if ((l.source as Thing).Relationships.Count == 1)
                {
                    return (l.source as Thing).Label;
                }
            }
        }
        if (property.V != null)
            return property.V.ToString();
        return property.Label;
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
                AddReferences(t, tvi, parentLabel);
                AddReferencedBy(t, tvi, parentLabel);
            }
        }
    }

    int CountNonChildRelationships(IList<Relationship> list)
    {
        return list.Count - list.Count(x => x.relType?.Label == "has-child");
    }

    private void AddReferences(Thing t, TreeViewItem tvi, string parentLabel)
    {
        if (CountNonChildRelationships(t.RelationshipsNoCount) == 0) return;
        TreeViewItem tviRefLabel = new() { Header = "Relationships: " + CountNonChildRelationships(t.RelationshipsNoCount).ToString() };

        string fullString = "|" + parentLabel + "|" + t.Label + "|:Relationships";
        if (expandedItems.Contains(fullString))
            tviRefLabel.IsExpanded = true;
        tviRefLabel.ContextMenu = new ContextMenu() { Visibility = Visibility.Hidden };
        tvi.Items.Add(tviRefLabel);

        totalItemCount++;
        IList<Relationship> sortedReferences = t.RelationshipsNoCount.OrderBy(x => -x.Value).ToList();
        foreach (Relationship l in sortedReferences)
        {
            if (l.relType?.Label == "has-child") continue;
            if (l.target != null && l.target.HasAncestorLabeled("Value"))
            {
                TreeViewItem tviRef = new(){Header = GetRelationshipString(l)};
                tviRef.ContextMenu = GetContextMenu(l.target);
                tviRefLabel.Items.Add(tviRef);
                totalItemCount++;
            }
            else if (l.relType is not null)
            {
                string count = "";
                if (l.count != 0) count = l.count + ")";
                TreeViewItem tviRef = new(){Header = GetRelationshipString(l),};

                if (l.source != t) tviRef.Header = l.source?.Label + "->" + tviRef.Header;
                tviRef.ContextMenu = GetRelationshipContextMenu(l);
                tviRefLabel.Items.Add(tviRef);
                totalItemCount++;
            }
            else if (!l.target.HasAncestorLabeled("Object"))
            {
                TreeViewItem tviRef = new(){Header = GetRelationshipString(l),};
                tviRef.ContextMenu = GetContextMenu(l.target);
                tviRefLabel.Items.Add(tviRef);
                totalItemCount++;
            }
            else
            {
                string refEntry = (l.target as Thing).Label + ":";
                if ((l.T as Thing).V is not null) refEntry += (l.T as Thing).V.ToString() + ": ";
                refEntry += l.hits + " : -";
                refEntry += l.misses + " : ";
                refEntry += ((float)l.hits / (float)l.misses).ToString("f3");
                
                TreeViewItem tviRef = new(){Header = refEntry,};
                tviRef.ContextMenu = GetContextMenu((l.target as Thing));
                tviRefLabel.Items.Add(tviRef);
                totalItemCount++;
            }
        }
    }

    private void DetermineRelationshipAndUpdateItemCount(Thing t, TreeViewItem tviRefLabel)
    {
        IList<Relationship> sortedReferencedBy = t.RelationshipsFrom.OrderBy(x => -x.Value).ToList();
        foreach (Relationship referencedBy in sortedReferencedBy)
        {
            if (referencedBy.relType?.Label == "has-child") continue;
            TreeViewItem tviRef;
            string headerstring1 = GetRelationshipString (referencedBy);
            tviRef = new TreeViewItem
            {
                Header = headerstring1
            };
            tviRef.ContextMenu = GetRelationshipContextMenu(referencedBy);
            tviRefLabel.Items.Add(tviRef);
            totalItemCount++;
        }
    }
    private void AddReferencedBy(Thing t, TreeViewItem tvi, string parentLabel)
    {
        if (CountNonChildRelationships(t.RelationshipsFrom) == 0) return;
        TreeViewItem tviRefLabel = new() { Header = "RelationshipsFrom: " + CountNonChildRelationships(t.RelationshipsFrom).ToString() };

        string fullString = "|" + parentLabel + "|" + t.Label + "|:RelationshipsFrom";
        if (expandedItems.Contains(fullString))
            tviRefLabel.IsExpanded = true;
        tvi.ContextMenu.Visibility = Visibility.Hidden;
        tvi.Items.Add(tviRefLabel);

        totalItemCount++;

        DetermineRelationshipAndUpdateItemCount(t, tviRefLabel);
    }

    private void TheTreeView_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        Draw(true);
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

    private void TextBoxRoot_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return)
        {
            RefreshButton_Click(null, null);
        }
        if (e.Key == Key.Up)
        {
            theTreeView.FontSize += 2;
        }
        if (e.Key == Key.Down)
        {
            if (theTreeView.FontSize > 2)
                theTreeView.FontSize -= 2;
        }
    }

    private void CheckBoxSort_Checked(object sender, RoutedEventArgs e)
    {
        Draw(false);
    }

    private void CheckBoxSort_Unchecked(object sender, RoutedEventArgs e)
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

    private void InitializeButton_Click(object sender, RoutedEventArgs e)
    {
        ModuleUKS parent = (ModuleUKS)base.ParentModule;
        theTreeView.Items.Clear();
        expandedItems.Clear();
        parent.Initialize();
        textBoxRoot.Text = "Thing";
        RefreshButton_Click(null, null);
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        ModuleUKS parent = (ModuleUKS)base.ParentModule;
        System.Windows.Forms.SaveFileDialog saveFileDialog = new()
        {
            Filter = Utils.FilterXMLs,
            Title = Utils.TitleUKSFileSave,
            InitialDirectory = Utils.GetOrAddLocalSubFolder(Utils.FolderKnowledgeFiles),
        };

        // Show the Dialog.  
        // If the user clicked OK in the dialog and  
        System.Windows.Forms.DialogResult result = saveFileDialog.ShowDialog();
        if (result == System.Windows.Forms.DialogResult.OK)
        {
            MainWindow.SuspendEngine();
            parent.fileName = saveFileDialog.FileName;
            parent.SaveUKStoXMLFile();
            MainWindow.ResumeEngine();
        }
        saveFileDialog.Dispose();
    }

    private void LoadButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            MainWindow.SuspendEngine();
            ModuleUKS parent = (ModuleUKS)base.ParentModule;
            System.Windows.Forms.OpenFileDialog openFileDialog = new()
            {
                Filter = Utils.FilterXMLs,
                Title = Utils.TitleUKSFileLoad,
                InitialDirectory = Utils.GetOrAddLocalSubFolder(Utils.FolderKnowledgeFiles),
            };

            // Show the Dialog.  
            // If the user clicked OK in the dialog and  
            System.Windows.Forms.DialogResult result = openFileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                parent.fileName = openFileDialog.FileName;
                parent.LoadUKSfromXMLFile((btn.Content.ToString()=="Merge"));
                MainWindow.ResumeEngine();
            }
            openFileDialog.Dispose();
            MainWindow.ResumeEngine();
        }
    }
}