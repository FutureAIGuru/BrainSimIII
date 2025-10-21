//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using UKS;
using static BrainSimulator.Modules.ModuleAttributeBubble;

namespace BrainSimulator.Modules;

public partial class ModuleUKSQueryDlg : ModuleBaseDlg
{
    DispatcherTimer requeryTimer = new DispatcherTimer();

    public ModuleUKSQueryDlg()
    {
        InitializeComponent();
        requeryTimer.Interval = TimeSpan.FromSeconds(3);
        requeryTimer.Tick += RequeryTimer_Tick;
        requeryTimer.Start();
    }

    private void RequeryTimer_Tick(object sender, EventArgs e)
    {
        QueryForAttributes();
        QueryByAttributes();
    }

    //List<Relationship> result = new();


    // Draw gets called to draw the dialog when it needs refreshing
    public override bool Draw(bool checkDrawTimer)
    {
        if (!base.Draw(checkDrawTimer)) return false;
        return true;
    }

    private void BtnRelationships_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b)
        {
            if (b.Content.ToString() == "Add")
            {
                string newText = typeText1.Text + "," + targetText1.Text;
                if (!queryText1.Text.Contains(newText))
                {
                    if (!string.IsNullOrEmpty(queryText1.Text))
                        queryText1.Text += "\n";
                    queryText1.Text += newText;
                }
                QueryByAttributes();
            }
            if (b.Content.ToString() == "Clear")
            {
                queryText1.Text = "";
                resultText1.Text = "";
            }
            if (b.Content.ToString() == "Query")
            {
                QueryForAttributes();
            }
        }
    }
    private void QueryForAttributes()
    {
        string source = sourceText.Text;
        string type = typeText.Text;
        string target = targetText.Text;
        string filter = filterText.Text;

        List<Thing> things;
        List<Relationship> relationships;
        ModuleUKSQuery UKSQuery = (ModuleUKSQuery)ParentModule;
        UKSQuery.QueryUKS(source, type, target, filter, out things, out relationships);

        if (things.Count > 0)
            OutputResults(things);
        else
            OutputResults(relationships, target == "", source == "");
    }

    //query by attributes
    private void QueryByAttributes()
    {
        ModuleUKSQuery UKSQuery = (ModuleUKSQuery)ParentModule;
        var theUKS = UKSQuery.theUKS;

        Thing ancestor = theUKS.Labeled(ancestorText1.Text);
        if (ancestor == null)
            ancestor = theUKS.Labeled("Thing");

        //build the query object
        Thing queryThing = CreateTheQueryThing();
        if (queryThing == null)
        {
            SetStatus("Could not create query");
            return;
        }

        var allResults = theUKS.SearchForClosestMatch(queryThing, ancestor);

        if (allResults.Count == 0)
        {
            resultText1.Text = "<No Results>";
            BtnNo.IsEnabled = true;
            if (queryThing.Relationships.Count > 0)
                BtnLearn.IsEnabled = true;
            else
                BtnLearn.IsEnabled = false;
            UKSQuery.theUKS.DeleteThing(queryThing);
            return;
        }
        BtnNo.IsEnabled = true;
        //        BtnLearn.IsEnabled = false;
        BtnLearn.IsEnabled = true;

        resultText1.Text = "";
        foreach (var result1 in allResults)
        {
            resultText1.Text += result1.t.Label + "   " + result1.conf.ToString("0.00") + "\n";
        }

        if (allResults.Count == 1 || allResults[0].conf > allResults[1].conf)
        {
            UpdateMostRecent(allResults[0].t);
        }

        theUKS.DeleteThing(queryThing);
    }

    private Thing CreateTheQueryThing()
    {
        ModuleUKSQuery UKSQuery = (ModuleUKSQuery)ParentModule;
        Thing queryThing = new Thing() { Label = "theQuery" };
        string[] rels = queryText1.Text.Split('\n');
        foreach (string s in rels)
        {
            string[] relParams = s.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (relParams.Length > 1)
            {
                Thing relType = UKSQuery.theUKS.CreateThingFromMultipleAttributes(relParams[0], true);
                Thing relTarget = UKSQuery.theUKS.CreateThingFromMultipleAttributes(relParams[1], false);
                if (relType == null)
                {
                    resultText1.Text = $"<{relParams[0]} not found>";
                    return null;
                }
                if (relTarget == null)
                {
                    resultText1.Text = $"<{relParams[1]} not found>";
                    return null;
                }

                //put target
                if (relType.Label == "can")
                {
                    relTarget.AddParent("Action");
                    relTarget.RemoveParent("unknownObject");
                }

                float conf = .9f;
                if (relParams.Length > 2) float.TryParse(relParams[2], out conf);
                Relationship r1 = queryThing.AddRelationship(relTarget, relType);
                r1.Weight = conf;
            }
        }

        return queryThing;
    }

    private void BtnLearn_Click(object sender, RoutedEventArgs e)
    {
        ModuleUKSQuery UKSQuery = (ModuleUKSQuery)ParentModule;
        UKS.UKS theUKS = UKSQuery.theUKS;

        Thing ancestor = theUKS.Labeled(ancestorText1.Text);
        if (ancestor == null)
            ancestor = theUKS.Labeled("Thing");

        //build the query object
        Thing queryThing = CreateTheQueryThing();
        if (queryThing == null)
        {
            SetStatus("Could not create query");
            theUKS.DeleteThing(queryThing);
            return;
        }
        SetStatus("OK");

        float confidence = 0;
        var allResults = theUKS.SearchForClosestMatch(queryThing, ancestor);

        if (allResults.Count == 0)
        {
            //case 1: no results, create a new Thing
            lock (theUKS.UKSList)
            {
                theUKS.UKSList.Add(queryThing);
            }
            queryThing.Label = "Unl*";
            queryThing.AddParent("UnknownObject");
            UpdateMostRecent(queryThing);
            return;
        }

        //what attributes are missing from the search result?
        var missingAttributes = GetMissingAttributes(queryThing, allResults[0].t);

        //case 2: ambiguous results
        //get top matching query results (of equal weight)
        int matchingTopEntries = 1;
        int i = 1;
        float matchingTopConfidence = allResults[0].conf;
        while (i < allResults.Count && allResults[i++].conf== matchingTopConfidence)
            matchingTopEntries++;

        if (matchingTopEntries > 1)
        {
            //add the thing to UKS
            lock (theUKS.UKSList)
            {
                theUKS.UKSList.Add(queryThing);
            }
            queryThing.Label = "Unl*";
            for (i = 0; i < matchingTopEntries; i++)
            {
                var r = queryThing.AddParent(allResults[i].t);
                r.Weight = 1.1f / (float)matchingTopEntries;
            }

            RemoveRedundantInheritedAttributes(queryThing);

            UpdateMostRecent(queryThing);
            return;
        }

        //case 2a: new attribute conflicts with children, create a new child
        var topResult = allResults[0].t;
        bool newChildNeeded = false;
        if (missingAttributes.Count > 0)
        {
            foreach (var child in topResult.Children)
            {
                if (theUKS.ThingsHaveSimilarRelationship(queryThing,child))
                {
                    newChildNeeded = true;
                    break;
                }    
            }
            if (newChildNeeded)
            {
                lock (theUKS.UKSList)
                    theUKS.UKSList.Add(queryThing);
                queryThing.Label = "Unl*";
                Relationship r1 = queryThing.AddParent(topResult);
                r1.Weight = .9f;
                UpdateMostRecent(queryThing);
                RemoveRedundantInheritedAttributes(queryThing);
                return;
            }
        }


        //case 3: additional attributes need to be added
        bool bubbleNeeded = false;
        foreach (var r in missingAttributes)
        {
            var r1 = topResult.AddRelationship(r.target, r.relType);
            r1.Weight = r.Weight;
            bubbleNeeded = true;
        }
        if (bubbleNeeded)
            BubbleCommonAttributes(topResult);


        //case 4: additional attributes change parentage
        if (matchingTopEntries == 1)
        {
            List<int> missingCount = new();
            foreach (Thing t in topResult.Parents)
                missingCount.Add(GetMissingAttributes(queryThing, t).Count);
            float ave = (float)missingCount.Average();
            int m = 0;
            foreach (Thing t in topResult.Parents)
            {
                var m1 = missingCount[m++];
                var w = theUKS.GetRelationshipWeight(topResult, t);
                if (m1 < ave)
                {
                    w = 1 - (1 - w) / 2;
                    theUKS.SetRelationshipWeight(topResult, t, w);
                }
                if (m1 > ave)
                {
                    w = w / 2;
                    if (w > .1f)
                        theUKS.SetRelationshipWeight(topResult, t, w);
                    else
                        topResult.RemoveParent(t);
                }

            }
        }

        theUKS.DeleteThing(queryThing);
    }

    private void BubbleCommonAttributes(Thing queryThing)
    {
        if (queryThing.Parents.Count == 0) return;
        var parent = queryThing.Parents[0];
        //build a List of counts of the attributes
        //build a List of all the Relationships which this thing's children have
        List<RelDest> attributes = new();
        foreach (var child in parent.Children)
            CountAttributes(child, attributes);

        foreach (var key in attributes)
        {
            if (key.relationships.Count < 2 || key.relationships.Count < parent.Children.Count) continue;
            parent.AddRelationship(key.target, key.relType).Weight = .9f;
        }
        foreach (var child in parent.Children)
        {
            RemoveRedundantInheritedAttributes(child);
        }

    }

    private void RemoveRedundantInheritedAttributes(Thing queryThing)
    {
        //remove any attributes which are common to all parents
        for (int i = 0; i < queryThing.Relationships.Count; i++)
        {
            Relationship r = queryThing.Relationships[i];
            if (r.reltype.Label == "is-a") continue;
            bool relationshipIsCommonToAllParents = true;
            foreach (Thing parent in queryThing.Parents)
            {
                if (parent.HasRelationship(parent, r.reltype, r.target) == null)
                {
                    relationshipIsCommonToAllParents = false;
                    break;
                }
            }
            if (relationshipIsCommonToAllParents)
            {
                queryThing.RemoveRelationship(r.target, r.relType);
                i--;
                //Thread.Sleep(1000);
            }
        }
    }

    List<Relationship> GetMissingAttributes(Thing queryThing, Thing foundThing)
    {
        ModuleUKSQuery UKSQuery = (ModuleUKSQuery)ParentModule;
        var theUKS = UKSQuery.theUKS;

        List<Relationship> missingAttributes = new();
        var inheritableRelationships = theUKS.GetAllRelationships(new List<Thing> { foundThing });
        foreach (Relationship r in queryThing.Relationships)
        {
            if (inheritableRelationships.FindFirst(x => x.relType == r.relType && x.target == r.target) == null)
                missingAttributes.Add(r);
        }
        return missingAttributes;
    }

    void UpdateMostRecent(Thing t)
    {
        ModuleUKSQuery UKSQuery = (ModuleUKSQuery)ParentModule;
        Thing mostRecent = UKSQuery.theUKS.GetOrAddThing("mostRecent", "RelationshipType");
        //delete any previous mostRecent relationships
        mostRecent.RemoveRelationships("is");
        mostRecent.AddRelationship(t, "is");
    }

    private void BtnNo_Click(object sender, RoutedEventArgs e)
    {
        ModuleUKSQuery UKSQuery = (ModuleUKSQuery)ParentModule;
        UKS.UKS theUKS = UKSQuery.theUKS;

        Thing ancestor = theUKS.Labeled(ancestorText1.Text);
        if (ancestor == null)
            ancestor = theUKS.Labeled("Thing");

        //build the query object
        Thing queryThing = CreateTheQueryThing();
        if (queryThing == null)
        {
            SetStatus("Could not create query");
            if (queryThing != null)
                theUKS.DeleteThing(queryThing);
            return;
        }
        SetStatus("OK");

        var allResults = theUKS.SearchForClosestMatch(queryThing, ancestor);
        if (allResults.Count == 0)
        {
            if (queryThing != null)
                theUKS.DeleteThing(queryThing);
            return;
        }

        var topResult = allResults[0].t;

        // does the query thing have all the same relationships as the result?
        bool allMatch = true;
        foreach (Relationship r in queryThing.Relationships)
        {
            if (topResult.HasRelationship(topResult, r.relType, r.target) == null)
            {
                //not the same
                allMatch = false;
                break;
            }
        }
        if (allMatch)
        {
            SetStatus("Query matches existing object");
            theUKS.DeleteThing(queryThing);
            return;
        }

        if (topResult != null)
        {
            //case 1: no results
            //add the thing to UKS
            lock (theUKS.UKSList)
            {
                theUKS.UKSList.Add(queryThing);
            }
            queryThing.Label = "Unl*";
            queryThing.AddParent("UnknownObject");
            UpdateMostRecent(queryThing);

            //the following happens after a 2 second delay
            Task.Run(() =>
            {
                Thread.Sleep(2000);
                CreateClassWithCommonAttributes(topResult, queryThing);
                //MyFunction();
            });
            return;
        }
        theUKS.DeleteThing(queryThing);
        SetStatus("OK");
    }

    void CreateClassWithCommonAttributes(Thing tExisting, Thing tNew)
    {
        int minCommonAttributes = 2;
        //build a List of counts of the attributes
        //build a List of all the Relationships which this thing's children have
        List<RelDest> attributes = new();

        CountAttributes(tExisting, attributes);
        CountAttributes(tNew, attributes);

        //create intermediate parent Things
        //bubble up the common attributes
        ModuleUKSQuery UKSQuery = (ModuleUKSQuery)ParentModule;
        Thing newParent = null;

        foreach (var key in attributes)
        {
            if (key.relationships.Count >= minCommonAttributes)
            {
                if (newParent == null)
                    newParent = UKSQuery.theUKS.GetOrAddThing("newParent", tExisting.Parents[0]);
                newParent.AddRelationship(key.target, key.relType);
                foreach (Relationship r in key.relationships)
                {
                    Thing tChild = (Thing)r.source;
                    Relationship rp = tChild.AddParent(newParent);
                    rp.Weight = .9f;
                    if (tChild.Parents[0] != newParent)
                        tChild.RemoveParent(tChild.Parents[0]);
                    RemoveRedundantInheritedAttributes(tChild);  //for animation...do one at a time
                }
            }
        }
        newParent.Label = "Unl*";
    }

    private static void CountAttributes(Thing tExisting, List<RelDest> attributes)
    {
        foreach (Relationship r in tExisting.Relationships)
        {
            if (r.reltype == Thing.IsA) continue;
            Thing useRelType = GetInstanceType(r.reltype);

            RelDest foundItem = attributes.FindFirst(x => x.relType == useRelType && x.target == r.target);
            if (foundItem == null)
            {
                foundItem = new RelDest { relType = useRelType, target = r.target };
                attributes.Add(foundItem);
            }
            if (foundItem.relationships.FindFirst(x => x.source == r.source && x.target == r.target) == null)
                foundItem.relationships.Add(r);
        }
    }

    private void OutputResults<T>(IList<T> r, bool noSource = false, bool noTarget = false)
    {
        string resultString = "";
        if (r == null || r.Count == 0)
        {
            resultString = "No Results";
        }
        else
        {
            foreach (var r1 in r)
            {
                if (r1 is Relationship r2)
                {
                    if (noSource && r2.Clauses.Count == 0 && fullCB.IsChecked == false)
                        resultString += $"{r2.relType?.ToString()} {r2.target.ToString()}  ({r2.Weight.ToString("0.00")})\n";
                    else if (noTarget && r2.Clauses.Count == 0 && fullCB.IsChecked == false)
                        resultString += $"{r2.source.ToString()} {r2.relType.ToString()}  ({r2.Weight.ToString("0.00")})\n";
                    else
                    {
                        Thing theSource = UKS.UKS.GetNonInstance(r2.source);
                        if (fullCB.IsChecked == true)
                            resultString += $"{theSource.Label} ";
                        resultString += $"{r2.relType.ToString()} {r2.target.ToString()}  ({r2.Weight.ToString("0.00")})\n";
                    }
                }
                else
                    resultString += r1.ToString() + "\n";
            }
        }
        resultText.Text = resultString;
    }

    // thingText_TextChanged is called when the thing textbox changes
    private void Text_TextChanged(object sender, TextChangedEventArgs e)
    {
        CheckThingExistence(sender);
    }
    //copied from UKSStatementDlg.cs
    private Thing CheckThingExistence(object sender)
    {
        if (sender is TextBox tb)
        {
            string text = tb.Text.Trim();

            if (text == "" && !tb.Name.Contains("arget") && tb.Name != "typeText")
            {
                tb.Background = new SolidColorBrush(Colors.Pink);
                SetStatus("Source and type cannot be empty");
                return null;
            }
            List<Thing> tl = ModuleUKSStatement.ThingListFromString(text);
            if (tl == null || tl.Count == 0)
            {
                tb.Background = new SolidColorBrush(Colors.LemonChiffon);
                SetStatus("");
                return null;
            }
            tb.Background = new SolidColorBrush(Colors.White);
            SetStatus("");
            return tl[0];
        }
        return null;
    }
}
