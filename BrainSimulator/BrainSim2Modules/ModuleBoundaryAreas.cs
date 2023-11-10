//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BrainSimulator.Modules
{

    public class ModuleBoundaryAreas : ModuleBase
    {

        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleBoundaryAreas()
        {
            minHeight = 2;
            maxHeight = 500;
            minWidth = 2;
            maxWidth = 500;
        }
        ModuleUKS uks = null;
        List<ModuleBoundarySegments.Arc> boundaries = new List<ModuleBoundarySegments.Arc>();

        //fill this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            Init();  //be sure to leave this here

            ModuleView naSource = theNeuronArray.FindModuleByLabel("UKS");
            if (naSource == null) return;
            uks = (ModuleUKS)naSource.TheModule;

            GetSegmentsFromUKS();
            if (boundaries.Count > 120) return;//TODO startup hack
            FindCorners();
            ConnectCorners();
            FindAreas();

            GetAreaGreatestLengths();
            GetAreaCentroids();
            GetAreaColors();

            SetAreasToUKS();

            //if you want the dlg to update, use the following code whenever any parameter changes
            // UpdateDialog();
        }

        // Gets the list of children named "Segment" from the UKS
        // and turns their two children into a new ModuleBoundarySegments.Arc object
        // which is stored in the boundaries list. The Segments in the UKS are left unharmed.
        private void GetSegmentsFromUKS()
        {
            boundaries.Clear();
            IList<Thing> boundarySegments = uks.GetChildren(uks.Labeled("Segment"));
            for (int i = 0; i < boundarySegments.Count; i++)
            {
                Thing seg = boundarySegments[i];
                if (seg.Children.Count < 2)
                {
                    continue;
                }
                boundaries.Add(new ModuleBoundarySegments.Arc
                {
                    p1 = (PointPlus)seg.Children[0].V,
                    p2 = (PointPlus)seg.Children[1].V,
                });
            }
        }

        // An object of class Corner connects two ModuleBoundarySegments.Arc objects.
        // it stores the location where they connect, and has cached elements for the 
        // opposite corners and the distance to them, and it can compute the angle between the two arcs.
        public class Corner
        {
            public Point loc;
            public ModuleBoundarySegments.Arc seg1;
            public ModuleBoundarySegments.Arc seg2;
            public Corner c1;
            public float dist1;
            public Corner c2;
            public float dist2;
            public Angle Angle
            {
                get
                {
                    Angle a = seg1.Angle - seg2.Angle;
                    a = Math.Abs(a);
                    if (a.ToDegrees() >= 180)
                        a -= Math.PI;
                    return a;
                }
            }
        }

        float cornerDistLimit = 3;
        List<Corner> corners = new List<Corner>();

        // FindCorners() iterates over the boundaries list and compares each pair to find the corners.
        // If any points are within the cornerDistLimit they are possibly intersecting, and so 
        // we check their angles to be different, and their lengths to be over 0, 
        // and then call Utils.FindIntersection() to get the location and add a new corner to the corners list. 
        // for any lone segments we add TWO corners to the list, one on either end.
        private void FindCorners()
        {
            corners.Clear();
            for (int i = 0; i < boundaries.Count; i++)
            {
                bool boundaryIntersection = false;
                for (int j = i + 1; j < boundaries.Count; j++)
                {
                    if ((((Vector)boundaries[i].p1 - (Vector)boundaries[j].p1).Length < cornerDistLimit) ||
                        (((Vector)boundaries[i].p1 - (Vector)boundaries[j].p2).Length < cornerDistLimit) ||
                        (((Vector)boundaries[i].p2 - (Vector)boundaries[j].p1).Length < cornerDistLimit) ||
                        (((Vector)boundaries[i].p2 - (Vector)boundaries[j].p2).Length < cornerDistLimit))
                    {
                        if (boundaries[i].Angle != boundaries[j].Angle && boundaries[i].Length > 0 && boundaries[j].Length > 0)
                        {
                            Utils.FindIntersection(boundaries[i].p1, boundaries[i].p2, boundaries[j].p1, boundaries[j].p2, out Point intersection);
                            corners.Add(new Corner
                            {
                                loc = intersection,
                                seg1 = boundaries[i],
                                seg2 = boundaries[j]
                            });
                            boundaryIntersection = true;
                        }
                    }
                }
                if (!boundaryIntersection) //didn't intersect anything must be a lone segment
                {
                    corners.Add(new Corner
                    {
                        loc = boundaries[i].p1,
                        seg1 = boundaries[i],
                        seg2 = boundaries[i]
                    });
                    corners.Add(new Corner
                    {
                        loc = boundaries[i].p2,
                        seg1 = boundaries[i],
                        seg2 = boundaries[i]
                    });
                }
            }
        }

        // This methods is a code de-duplication fix for four almost identical batches of code in ConnectCorners().
        // It basically checks what other corner is closest, and updates it in the corners list. 
        public void DetermineNearestCorner(Point theOtherPoint, int i, bool modifyFirstCorner)
        {
            Point thisPoint = corners[i].loc;
            float bestDist = float.MaxValue;

            //which corner is nearest theOtherPoint
            for (int j = 0; j < corners.Count; j++)
            {
                if (j == i) continue;
                float dist = (float)((Vector)theOtherPoint - (Vector)corners[j].loc).Length;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    if (modifyFirstCorner)
                    {
                        Debug.WriteLine("bestDist1: " + i.ToString());
                        corners[i].c1 = corners[j];
                        corners[i].dist1 = (float)((Vector)corners[j].loc - (Vector)thisPoint).Length;
                    }
                    else
                    {
                        Debug.WriteLine("bestDist2: " + i.ToString());
                        corners[i].c2 = corners[j];
                        corners[i].dist2 = (float)((Vector)corners[j].loc - (Vector)thisPoint).Length;
                    }
                }
            }
        }

        // ConnectCorners() needs to check both points of both segments of the corner to determine which belong together. 
        // It uses DetermineNearestCorner() in four different cases, to update the correct corners in the list. 
        private void ConnectCorners()
        {
            //Debug.WriteLine("ConnectCorners started");
            for (int i = 0; i < corners.Count; i++)
            {
                Debug.WriteLine("checking corner: " + i.ToString());
                if (((Vector)corners[i].seg1.p1 - (Vector)corners[i].loc).Length < ((Vector)corners[i].seg1.p2 - (Vector)corners[i].loc).Length)
                {
                    Debug.WriteLine("corners[i].seg1.p2");
                    //seg1. p1 is the point at this corner, which point is at the other end?
                    Point theOtherPoint = corners[i].seg1.p2;
                    DetermineNearestCorner(theOtherPoint, i, true);
                }
                else
                {
                    Debug.WriteLine("corners[i].seg1.p1");
                    //seg1. p2 is the point at this corner, which point is at the other end?
                    Point theOtherPoint = corners[i].seg1.p1;
                    DetermineNearestCorner(theOtherPoint, i, true);
                }

                //repeat for the second segment
                if (((Vector)corners[i].seg2.p1 - (Vector)corners[i].loc).Length < ((Vector)corners[i].seg2.p2 - (Vector)corners[i].loc).Length)
                {
                    Debug.WriteLine("corners[i].seg2.p2");
                    //seg2. p1 is the point at this corner, which point is at the other end?
                    Point theOtherPoint = corners[i].seg2.p2;
                    DetermineNearestCorner(theOtherPoint, i, false);
                }
                else
                {
                    Debug.WriteLine("corners[i].seg2.p1");
                    //seg2. p2 is the point at this corner, which point is at the other end?
                    Point theOtherPoint = corners[i].seg2.p1;
                    DetermineNearestCorner(theOtherPoint, i, false);
                }
            }
            //Debug.WriteLine("ConnectCorners ended");
        }

        // Area is a class that represents a simple polygon, which has a list of corners, 
        // and a few basic attributes pertaining to the area which are computed and cached for performance. 
        public class Area
        {
            public HSLColor avgColor;
            public float greatestLength;
            public Angle orientation;
            public Point centroid;
            public List<Corner> areaCorners = new List<Corner>();
        }

        List<Area> areas = new List<Area>();

        // CAREFUL: AddCornerToList() is RECURSIVE!!
        // it determines if the corners list contains the passed in corner, 
        // then removes the found corner from the corners list, 
        // and then calls itself for the two opposing corners of the removed corner.
        private void AddCornerToList(Area a, Corner c)
        {
            if (corners.Contains(c))
            {
                a.areaCorners.Add(c);
                corners.Remove(c);
                AddCornerToList(a, c.c1);
                AddCornerToList(a, c.c2);
            }
        }

        // FindAreas loops through the corners list as long as it is filled, 
        // creates a new Area and tries to complete it by adding all connected corners 
        // using the recursive AddCornerToList() 
        private void FindAreas()
        {
            areas.Clear();
            while (corners.Count > 0)
            {
                Area theArea = new Area();
                areas.Add(theArea);
                AddCornerToList(theArea, corners[0]);  // always [0] because the corner is removed... 
            }
        }

        // Calculates the area color for each of the areas in areas, by calling GetAreaColor()
        private void GetAreaColors()
        {
             
            foreach (Area a in areas)
            {
                a.avgColor = GetAreaColor(a);
            }
        }

        // Calculates the centroids for all areas in the areas list, by building lists of distinct points
        // and then calling Utils.GetCentroid() on the list to calculate the actual centroid.
        private void GetAreaCentroids()
        {
            foreach (Area a in areas)
            {
                List<Point> pts = a.areaCorners.Select(x => x.loc).Distinct().ToList();
                a.centroid = Utils.GetCentroid(pts);
            }
        }

        // Go over all areas, find the corners in each area that are connected to the greatest lengths...
        // We then update the greatestLength and orientation of the area
        private void GetAreaGreatestLengths()
        {
            foreach (Area a in areas)
            {
                a.greatestLength = 0;
                a.orientation = 0;
                foreach (Corner c in a.areaCorners)
                {
                    if (c.dist1 > a.greatestLength)
                    {
                        a.greatestLength = c.dist1;
                        a.orientation = c.seg1.Angle;
                    }
                    if (c.dist2 > a.greatestLength)
                    {
                        a.greatestLength = c.dist2;
                        a.orientation = c.seg2.Angle;
                    }
                }
            }
        }

        // Gets the area color for each area from the neurons present in the ImageZoom module.
        // if the area has two corners, we get it from one of the corners, or else from the centroid.
        private HSLColor GetAreaColor(Area a)
        {
            HSLColor retVal = new HSLColor(0, 0, 0);
            ModuleView source = theNeuronArray.FindModuleByLabel("ImageZoom");
            if (source == null) return retVal;

            if (a.areaCorners.Count == 2)
            {
                if (source.GetNeuronAt((int)a.areaCorners[0].loc.X, (int)a.areaCorners[0].loc.Y) is Neuron n0)
                {
                    System.Drawing.Color c2 = Utils.IntToDrawingColor(n0.LastChargeInt);
                    retVal = new HSLColor(c2.GetHue(), c2.GetSaturation(), c2.GetBrightness());
                }
            }
            else
            {
                if(source.GetNeuronAt((int)a.centroid.X, (int)a.centroid.Y) is Neuron n1)
                {
                    System.Drawing.Color c2 = Utils.IntToDrawingColor(n1.LastChargeInt);
                    retVal = new HSLColor(c2.GetHue(), c2.GetSaturation(), c2.GetBrightness());
                }
            }
            return retVal;

#pragma warning disable 162
            //this code works but is too slow to be useful...replace it with some OPENCV code
            //Get the bounds of the area
            List<Point> pts = new List<Point>();
            foreach (var x in a.areaCorners)
            {
                if (!double.IsNaN(x.loc.X) && !double.IsNaN(x.loc.Y))
                {
                    pts.Add(x.loc);
                }
            }

            int minx = (int)pts.Min(x => x.X);
            int miny = (int)pts.Min(x => x.Y);
            int maxx = (int)pts.Max(x => x.X);
            int maxy = (int)pts.Max(x => x.Y);

            //get the average color within the bounds
            float hueTot = 0;
            float brightTot = 0;
            float satTot = 0;
            int pointCount = 0;

            for (int i = minx; i < maxx; i++)
                for (int j = miny; j < maxy; j++)
                {
                    if (Utils.IsPointInPolygon(pts.ToArray(), new Point(i, j)))
                    {
                        if (source.GetNeuronAt(i, j) is Neuron n)
                        {
                            System.Drawing.Color c1 = Utils.IntToDrawingColor(n.LastChargeInt);
                            pointCount++;
                            hueTot += c1.GetHue();
                            brightTot += c1.GetBrightness();
                            satTot += c1.GetSaturation();
                        }
                    }
                }

            hueTot /= pointCount;
            brightTot /= pointCount;
            satTot /= pointCount;
            retVal = new HSLColor(hueTot, satTot, brightTot);
            return retVal;
#pragma warning restore 162
        }

        // SetAreasToUKS() replaces all children of the "CurrentlyViisible" "Visual" thing, 
        // and replaces them by new "Area*" things to which it adds "Value" things for the 
        // attributes of the area. It also adds list of corners to the UKS under the "Points" thing.
        // Any invalid areas are deleted from the UKS.
        private void SetAreasToUKS()
        {
            Debug.WriteLine("SetAreasToUKS called, but has an early return!!!!!");
            return;
            Thing currentlyVisibleParent = uks.GetOrAddThing("CurrentlyVisible", "Visual");
            uks.DeleteAllChildren(currentlyVisibleParent);
            Thing valueParent = uks.GetOrAddThing("Value", "Thing");

            foreach (Area a in areas)//all the areas currently visible
            {
                Thing theArea = uks.AddThing("Area*", currentlyVisibleParent);

                uks.SetValue(theArea, a.greatestLength, "Siz",0);
                uks.SetValue(theArea, (float)a.orientation, "Ang", 2);
                uks.SetValue(theArea, (float)a.centroid.X, "CtrX",0);
                uks.SetValue(theArea, (float)a.centroid.Y, "CtrY",0);
                uks.SetValue(theArea, (float)a.avgColor.hue/360f, "Hue",2);
                uks.SetValue(theArea, (float)a.avgColor.saturation, "Sat",2);
                uks.SetValue(theArea, (float)a.avgColor.luminance, "Lum", 2);

                List<Thing> theCorners = new List<Thing>();
                foreach (Corner c in a.areaCorners)
                {
                    theCorners.Add(uks.AddThing("Cnr*", theArea));

                    Thing theLocation = uks.Valued(c.loc, uks.Labeled("Point").Children);
                    if (theLocation == null)
                    {
                        theLocation = uks.AddThing("p*", "Point");
                        theLocation.V = c.loc;
                    }
                    theLocation.AddParent(theCorners.Last());
                }
                //add the reference links
                if (theCorners.Count == 2)
                {
                    theCorners[0].AddReference(theCorners[1]); //add lengths to these?
                    theCorners[1].AddReference(theCorners[0]);
                }
                else for (int i = 0; i < theCorners.Count; i++)
                {
                    int j = a.areaCorners.IndexOf(a.areaCorners[i].c1);
                    int k = a.areaCorners.IndexOf(a.areaCorners[i].c2);
                    if (k == -1 || j == -1 || j == k)
                    { // the area is not valid, delete it
                        uks.DeleteAllChildren(theArea);
                        uks.DeleteThing(theArea);
                        break;
                    }
                    theCorners[i].AddReference(theCorners[j]); //add lengths to these?
                    theCorners[i].AddReference(theCorners[k]);
                }
            }

        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
        }


        //the following can be used to massage public data to be different in the xml file
        //delete if not needed
        public override void SetUpBeforeSave()
        {
        }
        public override void SetUpAfterLoad()
        {
        }

        //called whenever the size of the module rectangle changes
        //for example, you may choose to reinitialize whenever size changes
        //delete if not needed
        public override void SizeChanged()
        {
            if (mv == null) return; //this is called the first time before the module actually exists
        }
    }
}
