//
// Copyright (c) FutureAI. All rights reserved.  
// Contains confidential and  proprietary information and programs which may not be distributed without a separate license
//  

using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using static System.Math;

namespace BrainSimulator.Modules
{
    public class ModuleGraphics : ModuleBase
    {
        Color currentColor = Colors.Black;
        List<Transform3D> transformStack = new();

        Point3DPlus pendingPosition = new Point3DPlus();
        Point3DPlus pendingSize = new Point3DPlus(1, 1, 1f);
        List<Thing> pendingRotation = new List<Thing>();
        string pendingColor = "";

        public ModuleGraphics()
        {
            minHeight = 2;
            maxHeight = 500;
            minWidth = 2;
            maxWidth = 500;
        }


        public override void Fire()
        {
            Init();  //be sure to leave this here

            //if you want the dlg to update, use the following code whenever any parameter changes
            // UpdateDialog();
        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
            //if you do this, all the graphics transforms are created in the wrong thread
            //CreateLibrary();
        }

        public bool ImagineThing(string name)
        {
            GetUKS();
            if (UKS is null) return false;

            Thing graphicParent = UKS.Labeled("graphic");
            if (graphicParent is null)
            {
                CreateLibrary();
                graphicParent = UKS.Labeled("graphic");
            }

            Thing rootThing = UKS.Labeled(name);
            if (rootThing is null)
                rootThing = UKS.Labeled(name.ToUpper());
            if (rootThing is null) return false;
            Thing toDraw = null;
            if (rootThing.HasAncestorLabeled("graphic"))
                toDraw = rootThing;
            else
                rootThing = rootThing.Children.FindFirst(x => x.HasAncestorLabeled("MentalModel"));
            if (rootThing is null) return false;

            foreach (Relationship r in rootThing.Relationships)
            {
                if (r.target.HasAncestorLabeled("graphic") && toDraw == null)
                {
                    toDraw = r.target;
                }
                if (pendingColor == "")
                {
                    if (r.target.Label.StartsWith("col"))
                    {
                        if (r.target.Children.Count > 0 && r.target.Children[0].V is HSLColor c)
                            pendingColor = Utils.GetColorNameFromHSL(c);
                    }
                }
                if (pendingSize.X == 1 && pendingSize.Y == 1 && pendingSize.Z == 1)
                {
                    if (r.target.Label.StartsWith("siz"))
                    {
                        if (r.target.V is float size)
                            pendingSize = new Point3DPlus(size / 10, size / 10, size / 10);
                    }
                }
            }
            if (toDraw == null) return false;
            AddItemToImagination(toDraw);
            return true;
        }

        public void SetUpsideDown()
        {
            Thing rotateX = UKS.Labeled("rotateX");
            pendingRotation.Add(rotateX);
            pendingRotation.Add(rotateX);

        }
        public void SetBackwards()
        {
            Thing rotateZ = UKS.Labeled("rotateZ");
            pendingRotation.Add(rotateZ);
            pendingRotation.Add(rotateZ);
        }

        void SetStartingTransform(Thing t)
        {
            transformStack.Add(((Transform3D)t.V).Clone());
        }
        public void SetStartingPosition(Point3DPlus p)
        {
            pendingPosition = p;
        }
        public void SetStartingColor(string color)
        {
            pendingColor = color;
        }
        public void SetStartingSize(Point3DPlus p)
        {
            pendingSize = p.Clone();
        }
        public void RedrawImagination()
        {
            if (UKS == null) return;
            Thing imagination = UKS.GetOrAddThing("imagination", "Thing");
            foreach (Thing t in imagination.Children)
            {
                DrawTheItem(t);
            }
        }
        private void AddItemToImagination(Thing graphic)
        {
            ModuleMentalModel mm = (ModuleMentalModel)FindModule("MentalModel");

            Thing imagination = UKS.GetOrAddThing("imagination", "Thing"); //get the imagination
            Thing imaginationObject = UKS.GetOrAddThing("io*", "imagination"); //create a new imagination object
            Relationship r = imagination.Relationships.FindFirst(x => x.target == imaginationObject);
            r.lastUsed = DateTime.Now;
            r.TimeToLive = TimeSpan.FromSeconds(15);
            imaginationObject.AddChild(graphic);
            Thing cen = mm.GetExistingPropertyWhichHasValue("cen", pendingPosition);
            if (cen == null)
                cen = UKS.GetOrAddThing("cen*", "cen", pendingPosition);

            Thing col = mm.GetExistingPropertyWhichHasValue("col", Utils.ColorFromName(pendingColor));
            if (col == null)
                col = UKS.GetOrAddThing("col*", "col", Utils.ColorFromName(pendingColor));

            Thing siz = mm.GetExistingPropertyWhichHasValue("siz", pendingSize);
            if (siz == null)
                siz = UKS.GetOrAddThing("siz*", "siz", pendingSize);

            Thing rot = mm.GetExistingPropertyWhichHasValue("rot", new List<Thing>(pendingRotation));
            if (rot == null)
                rot = UKS.GetOrAddThing("rot*", "rot", new List<Thing>(pendingRotation));

            imaginationObject.AddRelationship(cen);
            imaginationObject.AddRelationship(col);
            imaginationObject.AddRelationship(siz);
            imaginationObject.AddRelationship(rot);
            pendingColor = "";
            pendingPosition = new Point3DPlus(0, 0, 0f);
            pendingRotation.Clear();
            pendingSize = new Point3DPlus(1, 1, 1f);

            //draw the thing in the UI thread
            Application.Current.Dispatcher.Invoke((Action)delegate{DrawTheItem(imaginationObject);});
        }

        private void DrawTheItem(Thing imaginationObject)
        {
            transformStack.Clear();

            //get the item parameters
            currentColor = (System.Windows.Media.Color)imaginationObject.Relationships.FindFirst(x => x.target.Label.StartsWith("col")).target.V;
            Point3DPlus startPosition = (Point3DPlus)imaginationObject.Relationships.FindFirst(x => x.target.Label.StartsWith("cen")).target.V;
            transformStack.Add(new TranslateTransform3D(-startPosition.Y, startPosition.X, startPosition.Z));
            Point3DPlus startScale = (Point3DPlus)imaginationObject.Relationships.FindFirst(x => x.target.Label.StartsWith("siz")).target.V;
            transformStack.Add(new ScaleTransform3D(startScale.X, startScale.Y, startScale.Z));
            var rotations = (List<Thing>)imaginationObject.Relationships.FindFirst(x => x.target.Label.StartsWith("rot")).target.V;
            foreach (var r in rotations)
                transformStack.Add((RotateTransform3D)r.V);
            Thing graphic = imaginationObject.Children.FindFirst(x => x.HasAncestorLabeled("graphic"));

            //add change-of-axes rotation last so it is performed before any others
            transformStack.Add(((Transform3D)UKS.Labeled("rotateX").V).Clone());

            //add Item To Display
            UIObjects = new(); //the list from the drawing engine
            DrawThing(graphic);

            ModuleMentalModel mm = (ModuleMentalModel)FindModule("MentalModel");
            mm.AddMMDlgItems(UIObjects);

            transformStack.Clear();
        }

        void DrawThing(Thing t)
        {
            if (t.V is List<Point3DPlus> points1)
            {
                StartMesh();
                TrangulatePolygon(points1);
                EndMesh1(currentColor, t.Label);
            }
            foreach (Relationship r1 in t.Relationships)
            {
                if (r1.reltype.Label == "contains")
                {
                    foreach (var clause in r1.clauses)
                    {
                        if (clause.a == AppliesTo.target)
                        {
                            Thing target = clause.clause.target;
                            if (target.V is System.Windows.Media.Color c)
                            {
                                currentColor = c;
                            }
                            if (target.V is Transform3D tt)
                            {
                                transformStack.Add(tt);
                            }
                        }
                    }
                    DrawThing(r1.target);
                    foreach (var clause in r1.clauses)
                    {
                        if (clause.a == AppliesTo.target)
                        {
                            Thing target = clause.clause.target;
                            if (target.V is System.Windows.Media.Color c)
                            {
                                currentColor = c;
                            }
                            if (target.V is Transform3D tt)
                            {
                                transformStack.RemoveAt(transformStack.Count - 1);
                            }
                        }
                    }
                }
            }
        }

        void RoundCorners(object pointsIn)
        {
            if (pointsIn is List<Point3DPlus> points)
            {
                for (int i = points.Count - 1; i >= 0; i--)
                {
                    Point3DPlus point = points[i];
                    if (point.Z != 0)
                    {
                        MakeArc(points, i, point.Z);
                    }
                }
            }
        }
        void MakeArc(List<Point3DPlus> points, int pointToRound, float radius)
        {
            int myMod(int index, int limit) { while (index < 0) index += limit; return index % limit; }
            Angle increment = Angle.FromDegrees(5);

            //initial just 3 points
            PointPlus p1 = new PointPlus(points[myMod(pointToRound - 1, points.Count)].X, points[myMod(pointToRound - 1, points.Count)].Y);
            PointPlus p2 = new PointPlus(points[pointToRound].X, points[pointToRound].Y);
            PointPlus p3 = new PointPlus(points[(pointToRound + 1) % points.Count].X, points[(pointToRound + 1) % points.Count].Y);
            points.RemoveAt(pointToRound);
            PointPlus p12 = p2 - p1;
            PointPlus p32 = p2 - p3;
            if (radius != -1)
            {
                PointPlus pointPlus = new PointPlus { R = radius, Theta = p12.Theta };
                pointPlus.Theta += Angle.FromDegrees(180);
                p1 = p2 + pointPlus;
                pointPlus = new PointPlus { R = radius, Theta = p32.Theta };
                pointPlus.Theta += Angle.FromDegrees(180);
                p3 = p2 + pointPlus;
            }
            p12.Theta += Angle.FromDegrees(90); //normals to the 2 given segments
            p32.Theta += Angle.FromDegrees(90);
            p12 += p1;
            p32 += p3;
            Utils.FindIntersection(p1.P, p12.P, p3.P, p32.P, out System.Windows.Point center);
            PointPlus pCenter = new PointPlus(center);
            PointPlus pStart = p1 - pCenter;
            PointPlus pEnd = p3 - pCenter;
            Angle startAngle = pStart.Theta;
            Angle endAngle = pEnd.Theta;
            if (startAngle <= -Angle.FromDegrees(179) && endAngle > 0) startAngle = Angle.FromDegrees(180);
            if (endAngle <= -Angle.FromDegrees(179) && startAngle > 0) endAngle = Angle.FromDegrees(180);
            if (Abs(startAngle - endAngle) > Angle.FromDegrees(180))
            {
            }
            int count = 0;
            if (startAngle < endAngle)
            {
                for (Angle theta = startAngle; theta <= endAngle; theta += increment)
                {
                    pStart.Theta = theta;
                    PointPlus pNew = center + pStart;
                    Point3DPlus p = new(pNew.X, pNew.Y, 0);
                    points.Insert(pointToRound + count++, p);
                }
            }
            else
            {
                for (Angle theta = startAngle; theta >= endAngle; theta -= increment)
                {
                    pStart.Theta = theta;
                    PointPlus pNew = center + pStart;
                    Point3DPlus p = new(pNew.X, pNew.Y, 0);
                    points.Insert(pointToRound + count++, p);
                }
            }

            points.Insert(pointToRound + count++, new Point3DPlus(p3.X, p3.Y, 0f));
        }

        Thing CreatePrimitive(string name, List<Point3DPlus> points)
        {
            Thing retVal = UKS.GetOrAddThing(name, "graphic");
            retVal.V = points;
            return retVal;
        }
        Relationship CreateContainerRelationship(string source, string target, List<Thing> transforms, bool makeNew = false)
        {
            Thing graphicParent = UKS.GetOrAddThing("graphic", "Object");
            Thing tRelType = UKS.GetOrAddThing("contains", "Relationship");
            Thing tSource = UKS.GetOrAddThing(source, graphicParent);
            if (makeNew) //clear out ht existing relationships
            {
                for (int i = tSource.Relationships.Count - 1; i >= 0; i--)
                    tSource.RemoveRelationship(tSource.Relationships[i]);
            }
            Thing tTarget = UKS.GetOrAddThing(target, graphicParent);
            Relationship r = new Relationship
            {
                source = tSource,
                target = tTarget,
                relType = tRelType,
            };
            ModuleUKS.WriteTheRelationship(r);
            foreach (Thing t in transforms)
                r.clauses.Add(new ClauseType(AppliesTo.target, new Relationship { target = t }));
            return r;
        }

        private void StartMesh()
        {
            meshBuilder = new();
        }

        bool IsPtInsidePolygon(List<Point3DPlus> pts, PointPlus pt)
        {
            System.Windows.Point[] pts2D = new System.Windows.Point[pts.Count];
            for (int i = 0; i < pts.Count; i++)
            {
                Point3DPlus pt1 = pts[i];
                pts2D[i] = new System.Windows.Point(pt1.X, pt1.Y);
            }
            return Utils.IsPointInPolygon(pts2D, new System.Windows.Point(pt.X, pt.Y));
        }
        private void TrangulatePolygon(List<Point3DPlus> pts)
        {
            //fan algorithm -- works only with convex polygons
            //for (int i = 1; i < pts.Count - 1; i++)
            //{
            //    AddTriangle(pts[0], pts[i], pts[i + 1]);
            //}

            //cutting ears algorithm
            //in every polygon without holes, there will be at least one pair of adjascent segments  which form
            //two legs of a triangle which does not intersect any other edges. This triangle can be added to the 
            //draw stack and removed from the polygon which will then be one triangle smaller. Call recursively until done.
            if (pts.Count < 3) return;
            for (int i = 0; i < pts.Count; i++)
            {
                //is this an ear?
                PointPlus p1 = new(pts[i].X, pts[i].Y);
                PointPlus p2 = new PointPlus(pts[(i + 2) % pts.Count].X, pts[(i + 2) % pts.Count].Y);
                PointPlus midPoint = new PointPlus((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2);
                if (!IsPtInsidePolygon(pts, midPoint)) continue;
                //pt-p2 is the hypotenues of the 'ear'.  if no other segments cross it, then it is an ear
                for (int j = 0; j < pts.Count; j++)
                {
                    if (j == i || j == (i + 1) % pts.Count || j == (i + 2) % pts.Count || (j + 1) % pts.Count == i) continue;
                    PointPlus p3 = new(pts[j].X, pts[j].Y);
                    PointPlus p4 = new PointPlus(pts[(j + 1) % pts.Count].X, pts[(j + 1) % pts.Count].Y);
                    if (Utils.SegmentsIntersect(p1.P, p2.P, p3.P, p4.P))
                    { goto notEar; }
                }
                //yes
                AddTriangle(pts[i], pts[(i + 1) % pts.Count], pts[(i + 2) % pts.Count]);
                List<Point3DPlus> clonedList = new List<Point3DPlus>(pts);
                clonedList.RemoveAt((i + 1) % pts.Count);
                TrangulatePolygon(clonedList);
                break;
            notEar:;
            }
        }
        public void AddTriangle(Point3DPlus p1, Point3DPlus p2, Point3DPlus p3)
        {
            //draw the triangle in two faces so it will show regardless of orientation
            meshBuilder.AddTriangle(p1.P, p2.P, p3.P);
            meshBuilder.AddTriangle(p2.P, p1.P, p3.P);
        }


        int endOf = 0;
        MeshBuilder meshBuilder = null;
        List<ModelUIElement3D> UIObjects = null;

        private void EndMesh1(System.Windows.Media.Color c, string tooltip)
        {
            var element = new ModelUIElement3D();
            var geometry = new GeometryModel3D();
            //we might use this some day
            // geometry.Material = new SpecularMaterial(new SolidColorBrush(c), 0.1);
            //geometry.Material = new EmissiveMaterial(theBrush1);  //makes the object transparent

            SolidColorBrush theBrush = new SolidColorBrush(c);
            theBrush.Opacity = 0.5;
            geometry.Material = new DiffuseMaterial(theBrush);
            geometry.Geometry = meshBuilder.ToMesh();
            element.Model = geometry;
            UIObjects.Add(element);

            //TODO add the tooltips
            //ModuleMentalModelDlg dialog = (ModuleMentalModelDlg)dlg;
            //dialog.modelProperties.Add(element, tooltip);// AssembleTooltip(t));
            Transform3DGroup myTransformer = new();
            for (int i = transformStack.Count - 1; i >= 0; i--)
                myTransformer.Children.Add(transformStack[i].Clone());
            element.Transform = myTransformer;
            return;
        }

        //the following can be used to massage public data to be different in the xml file
        //delete if not needed
        public override void SetUpBeforeSave()
        {
        }
        public override void SetUpAfterLoad()
        {
            GetUKS();
            Thing graphicParent = UKS.Labeled("graphic");
            if (graphicParent is null)
            {
                CreateLibrary();
                graphicParent = UKS.Labeled("graphic");
            }
        }

        //called whenever the size of the module rectangle changes
        //for example, you may choose to reinitialize whenever size changes
        //delete if not needed
        public override void SizeChanged()
        {
            if (mv == null) return; //this is called the first time before the module actually exists
        }

        public void CreateLibrary()
        {
            GetUKS();

            Thing graphicParent = UKS.GetOrAddThing("graphic", "Object");
            UKS.DeleteAllChildren(graphicParent);
            Thing green = UKS.GetOrAddThing("green", graphicParent, Colors.Green);
            Thing orange = UKS.GetOrAddThing("orange", graphicParent, Colors.Orange);
            Thing blue = UKS.GetOrAddThing("elue", graphicParent, Colors.Blue);
            Thing red = UKS.GetOrAddThing("red", graphicParent, Colors.Red);
            Thing yellow = UKS.GetOrAddThing("yellow", graphicParent, Colors.Yellow);

            Thing xPlus10 = UKS.GetOrAddThing("xPlus10", graphicParent, new TranslateTransform3D(10, 0, 0));
            Thing xPlus6 = UKS.GetOrAddThing("xPlus6", graphicParent, new TranslateTransform3D(6, 0, 0));
            Thing xPlus4 = UKS.GetOrAddThing("xPlus4", graphicParent, new TranslateTransform3D(4, 0, 0));
            Thing xPlus2 = UKS.GetOrAddThing("xPlus2", graphicParent, new TranslateTransform3D(2, 0, 0));
            Thing xPlus1 = UKS.GetOrAddThing("xPlus1", graphicParent, new TranslateTransform3D(1, 0, 0));
            Thing xMinus10 = UKS.GetOrAddThing("xMinus10", graphicParent, new TranslateTransform3D(-10, 0, 0));
            Thing yPlus10 = UKS.GetOrAddThing("yPlus10", graphicParent, new TranslateTransform3D(0, 10, 0));
            Thing yPlus8 = UKS.GetOrAddThing("yPlus8", graphicParent, new TranslateTransform3D(0, 8, 0));
            Thing yPlus4 = UKS.GetOrAddThing("yPlus4", graphicParent, new TranslateTransform3D(0, 4, 0));
            Thing yPlus2 = UKS.GetOrAddThing("yPlus2", graphicParent, new TranslateTransform3D(0, 2, 0));
            Thing yPlus1 = UKS.GetOrAddThing("yPlus1", graphicParent, new TranslateTransform3D(0, 1, 0));
            Thing yMinus10 = UKS.GetOrAddThing("yMinus10", graphicParent, new TranslateTransform3D(0, -10, 0));
            Thing zPlus10 = UKS.GetOrAddThing("zPlus10", graphicParent, new TranslateTransform3D(0, 0, 10));
            Thing zMinus10 = UKS.GetOrAddThing("zMinus10", graphicParent, new TranslateTransform3D(0, 0, -10));
            Thing rotateX = UKS.GetOrAddThing("rotateX", graphicParent, new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 90)));
            Thing rotateY = UKS.GetOrAddThing("rotateY", graphicParent, new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90)));
            Thing rotateZ = UKS.GetOrAddThing("rotateZ", graphicParent, new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 90)));
            Thing rotateX45 = UKS.GetOrAddThing("rotateX45", graphicParent, new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 45)));
            Thing rotateY45 = UKS.GetOrAddThing("rotateY45", graphicParent, new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 45)));
            Thing rotateZ45 = UKS.GetOrAddThing("rotateZ45", graphicParent, new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 45)));
            Thing scaleX1_5 = UKS.GetOrAddThing("scaleX1_5", graphicParent, new ScaleTransform3D(1.5, 1, 1));

            Thing square = CreatePrimitive("square", new List<Point3DPlus> {
                new Point3DPlus(-10, -10, 0f),
                new Point3DPlus(-10, 10, 0f),
                new Point3DPlus(10, 10, 0f),
                new Point3DPlus(10, -10, 0f),
            });
            Thing arc180 = CreatePrimitive("arc180", new List<Point3DPlus> {
                new Point3DPlus (0,0,0f),
                new Point3DPlus (0,2,0f),
                new Point3DPlus (3,2,2f),
                new Point3DPlus (3,8,2f),
                new Point3DPlus (0,8,0f),
                new Point3DPlus (0,10,0f),
                new Point3DPlus (5,10,4f),
                new Point3DPlus (5,0,4f),
            });
            Thing disk = CreatePrimitive("disk", new List<Point3DPlus> {
                new Point3DPlus(-10, -10, 10f),
                new Point3DPlus(-10, 10, 10f),
                new Point3DPlus(10, 10, 10f),
                new Point3DPlus(10, -10, 10f),
            });

            Thing vStroke10 = CreatePrimitive("vStroke10", new List<Point3DPlus> {
                new Point3DPlus(0, 0, 0f),
                new Point3DPlus(0, 10, 0f),
                new Point3DPlus(2, 10, 0f),
                new Point3DPlus(2, 0, 0f),
            });
            Thing vStroke8 = CreatePrimitive("vStroke8", new List<Point3DPlus> {
                new Point3DPlus(0, 3, 0f),
                new Point3DPlus(0, 10, 0f),
                new Point3DPlus(2, 10, 0f),
                new Point3DPlus(2, 3, 0f),
            });
            Thing vStroke4 = CreatePrimitive("vStroke4", new List<Point3DPlus> {
                new Point3DPlus(0, 0, 0f),
                new Point3DPlus(0, 4, 0f),
                new Point3DPlus(2, 4, 0f),
                new Point3DPlus(2, 0, 0f),
            });
            Thing vStroke5 = CreatePrimitive("vStroke5", new List<Point3DPlus> {
                new Point3DPlus(0, 0, 0f),
                new Point3DPlus(0, 5, 0f),
                new Point3DPlus(2, 5, 0f),
                new Point3DPlus(2, 0, 0f),
            });
            Thing hStroke = CreatePrimitive("hStroke", new List<Point3DPlus> {
                new Point3DPlus(0, 0, 0f),
                new Point3DPlus(0, 2, 0f),
                new Point3DPlus(5, 2, 0f),
                new Point3DPlus(5, 0, 0f),
            });
            Thing hStroke8 = CreatePrimitive("hStroke8", new List<Point3DPlus> {
                new Point3DPlus(0, 0, 0f),
                new Point3DPlus(0, 2, 0f),
                new Point3DPlus(8, 2, 0f),
                new Point3DPlus(8, 0, 0f),
            });
            Thing hStroke4 = CreatePrimitive("hStroke4", new List<Point3DPlus> {
                new Point3DPlus(0, 0, 0f),
                new Point3DPlus(0, 2, 0f),
                new Point3DPlus(4, 2, 0f),
                new Point3DPlus(4, 0, 0f),
            });
            Thing hStroke5 = CreatePrimitive("hStroke5", new List<Point3DPlus> {
                new Point3DPlus(0, 0, 0f),
                new Point3DPlus(0, 2, 0f),
                new Point3DPlus(5, 2, 0f),
                new Point3DPlus(5, 0, 0f),
            });
            Thing hStroke2 = CreatePrimitive("hStroke2", new List<Point3DPlus> {
                new Point3DPlus(0, 0, 0f),
                new Point3DPlus(0, 2, 0f),
                new Point3DPlus(2, 2, 0f),
                new Point3DPlus(2, 0, 0f),
            });
            Thing C = CreatePrimitive("C", new List<Point3DPlus> {
                new Point3DPlus(0, 0, 3f),
                new Point3DPlus(0, 10, 3f),
                new Point3DPlus(6, 10, 3f),
                new Point3DPlus(6, 6, 0f),
                new Point3DPlus(4, 6, 0f),
                new Point3DPlus(4, 8, 1f),
                new Point3DPlus(2, 8, 1f),
                new Point3DPlus(2, 2, 1f),
                new Point3DPlus(4, 2, 1f),
                new Point3DPlus(4, 4, 0f),
                new Point3DPlus(6, 4, 0f),
                new Point3DPlus(6, 0, 3f),
            });
            Thing urStroke = CreatePrimitive("urStroke", new List<Point3DPlus> {
                new Point3DPlus(0, 0, 0f),
                new Point3DPlus(3, 10, 0f),
                new Point3DPlus(5, 10, 0f),
                new Point3DPlus(2, 0, 0f),
            });
            Thing drStroke = CreatePrimitive("drStroke", new List<Point3DPlus> {
                new Point3DPlus(0, 10, 0f),
                new Point3DPlus(2, 10, 0f),
                new Point3DPlus(5, 0, 0f),
                new Point3DPlus(3, 0, 0f),
            });
            Thing urStroke5 = CreatePrimitive("urStroke5", new List<Point3DPlus> {
                new Point3DPlus(0, 0, 0f),
                new Point3DPlus(3, 5, 0f),
                new Point3DPlus(5, 5, 0f),
                new Point3DPlus(2, 0, 0f),
            });
            Thing drStroke5 = CreatePrimitive("drStroke5", new List<Point3DPlus> {
                new Point3DPlus(0, 5, 0f),
                new Point3DPlus(2, 5, 0f),
                new Point3DPlus(5, 0, 0f),
                new Point3DPlus(3, 0, 0f),
            });
            Thing arc45 = CreatePrimitive("acr45", new List<Point3DPlus> {
                new Point3DPlus(0, 0, 0f),
                new Point3DPlus(0, 3, 3f),
                new Point3DPlus(3, 5, 0f),
                new Point3DPlus(5, 5, 0f),
                new Point3DPlus(2, 1, 1f),
                new Point3DPlus(2, 0, 0f),
            });
            Thing arcD180 = CreatePrimitive("arcD180", new List<Point3DPlus> {
                new Point3DPlus(0, 0, 3f),
                new Point3DPlus(0, 3, 0f),
                new Point3DPlus(2, 3, 0f),
                new Point3DPlus(2, 2, 1f),
                new Point3DPlus(4, 2, 1f),
                new Point3DPlus(4, 3, 0f),
                new Point3DPlus(6, 3, 0f),
                new Point3DPlus(6, 0, 3f),
            });
            Thing arcD225 = CreatePrimitive("arcD225", new List<Point3DPlus> {
                new Point3DPlus(0, 0, 2f),
                new Point3DPlus(0, 2, 0f),
                new Point3DPlus(0, 3, 1f),
                new Point3DPlus(1.9f, 5, 0f),
                new Point3DPlus(4.1f, 5, 0f),
                new Point3DPlus(2, 3.0f, .5f),
                new Point3DPlus(2, 2.5f, 0f),
                new Point3DPlus(2, 1.5f, .75f),
                new Point3DPlus(4, 1.5f, 1f),
                new Point3DPlus(4, 3, 0f),
                new Point3DPlus(6, 3, 0f),
                new Point3DPlus(6, 0, 3f),
            });
            Thing arcD225b = CreatePrimitive("arcD225b", new List<Point3DPlus> {
                new Point3DPlus(0, 0, 2f),
                new Point3DPlus(0, 2, 0f),
                new Point3DPlus(0, 3, 1f),
                new Point3DPlus(4f, 8, 0f),
                new Point3DPlus(6, 8, 0f),
                new Point3DPlus(2, 3.0f, .5f),
                new Point3DPlus(2, 2.5f, 0f),
                new Point3DPlus(2, 1.5f, .75f),
                new Point3DPlus(4, 1.5f, 1f),
                new Point3DPlus(4, 3, 0f),
                new Point3DPlus(6, 3, 0f),
                new Point3DPlus(6, 0, 3f),
            });
            Thing arcD270 = CreatePrimitive("arcD270", new List<Point3DPlus> {
                new Point3DPlus(0, 0, 3f),
                new Point3DPlus(0, 3, 0f),
                new Point3DPlus(2, 3, 0f),
                new Point3DPlus(2, 2, 1f),
                new Point3DPlus(4, 2, 1f),
                new Point3DPlus(4, 3, 0f),
                new Point3DPlus(4, 4, 0f),
                new Point3DPlus(2, 4, 0f),
                new Point3DPlus(2, 6, 0f),
                new Point3DPlus(6, 6, 3f),
                new Point3DPlus(6, 3, 0f),
                new Point3DPlus(6, 0, 3f),
            });
            Thing letters = UKS.GetOrAddThing("letters", "Object");
            Thing A = CreateContainerRelationship("A", "urStroke", new List<Thing> { }).source;
            CreateContainerRelationship("A", "drStroke", new List<Thing> { xPlus2, xPlus1 });
            CreateContainerRelationship("A", "hStroke4", new List<Thing> { xPlus2, yPlus1, yPlus1 });
            letters.AddChild(A);
            Thing B = CreateContainerRelationship("B", "P", new List<Thing> { }).source;
            CreateContainerRelationship("B", "arcD180", new List<Thing> { xPlus6, xPlus1, rotateZ });
            CreateContainerRelationship("B", "hStroke4", new List<Thing> { });
            Thing C1 = CreateContainerRelationship("C1", "arcD180", new List<Thing> { }).source;
            CreateContainerRelationship("C1", "arcD180", new List<Thing> { yPlus10, rotateX, rotateX });
            CreateContainerRelationship("C1", "vStroke4", new List<Thing> { yPlus2, yPlus1 });
            Thing D = CreateContainerRelationship("D", "arc180", new List<Thing> { xPlus2 }).source;
            CreateContainerRelationship("D", "vStroke10", new List<Thing> { });
            Thing E = CreateContainerRelationship("E", "F", new List<Thing> { }).source;
            CreateContainerRelationship("E", "hStroke", new List<Thing> { });
            Thing F = CreateContainerRelationship("F", "vStroke10", new List<Thing> { }).source;
            CreateContainerRelationship("F", "hStroke5", new List<Thing> { yPlus8 });
            CreateContainerRelationship("F", "hStroke4", new List<Thing> { yPlus4 });
            Thing G = CreateContainerRelationship("G", "C", new List<Thing> { }).source;
            CreateContainerRelationship("G", "hStroke2", new List<Thing> { xPlus2, xPlus1, yPlus1, yPlus1, yPlus1, scaleX1_5 });
            Thing H = CreateContainerRelationship("H", "vStroke10", new List<Thing> { }).source;
            CreateContainerRelationship("H", "hStroke", new List<Thing> { yPlus4 });
            CreateContainerRelationship("H", "vStroke10", new List<Thing> { xPlus4 });
            Thing I = CreateContainerRelationship("I", "vStroke10", new List<Thing> { xPlus2, xPlus1 }).source;
            Thing J = CreateContainerRelationship("J", "arcD180", new List<Thing> { }).source;
            CreateContainerRelationship("J", "vStroke8", new List<Thing> { xPlus4 });
            Thing K = CreateContainerRelationship("K", "vStroke10", new List<Thing> { }).source;
            CreateContainerRelationship("K", "urStroke5", new List<Thing> { yPlus4, yPlus1, xPlus1 });
            CreateContainerRelationship("K", "drStroke5", new List<Thing> { xPlus1 });
            Thing L = CreateContainerRelationship("L", "vStroke10", new List<Thing> { }).source;
            CreateContainerRelationship("L", "hStroke", new List<Thing> { });
            Thing M = CreateContainerRelationship("M", "vStroke10", new List<Thing> { }).source;
            CreateContainerRelationship("M", "vStroke10", new List<Thing> { xPlus6 });
            CreateContainerRelationship("M", "drStroke5", new List<Thing> { yPlus4, yPlus1 });
            CreateContainerRelationship("M", "urStroke5", new List<Thing> { yPlus4, yPlus1, xPlus1, xPlus2 });
            Thing N = CreateContainerRelationship("N", "vStroke10", new List<Thing> { }).source;
            CreateContainerRelationship("N", "vStroke10", new List<Thing> { xPlus4, xPlus1 });
            CreateContainerRelationship("N", "drStroke", new List<Thing> { xPlus1 });
            Thing O = CreateContainerRelationship("O", "arcD180", new List<Thing> { }).source;
            CreateContainerRelationship("O", "arcD180", new List<Thing> { yPlus10, rotateX, rotateX });
            CreateContainerRelationship("O", "vStroke4", new List<Thing> { yPlus2, yPlus1 });
            CreateContainerRelationship("O", "vStroke4", new List<Thing> { xPlus4, yPlus2, yPlus1 });
            Thing P = CreateContainerRelationship("P", "vStroke10", new List<Thing> { }).source;
            CreateContainerRelationship("P", "arcD180", new List<Thing> { xPlus6, yPlus4, rotateZ });
            CreateContainerRelationship("P", "hStroke4", new List<Thing> { yPlus4 });
            CreateContainerRelationship("P", "hStroke4", new List<Thing> { yPlus8 });
            Thing Q = CreateContainerRelationship("Q", "O", new List<Thing> { }).source;
            CreateContainerRelationship("Q", "drStroke5", new List<Thing> { xPlus2 });
            Thing R = CreateContainerRelationship("R", "P", new List<Thing> { }).source;
            CreateContainerRelationship("R", "drStroke5", new List<Thing> { xPlus2 });
            Thing S = CreateContainerRelationship("S", "arcD225", new List<Thing> { xPlus6, rotateY, rotateY }).source;
            CreateContainerRelationship("S", "arcD225", new List<Thing> { yPlus10, rotateX, rotateX });
            Thing T = CreateContainerRelationship("T", "vStroke10", new List<Thing> { xPlus2, xPlus1 }).source;
            CreateContainerRelationship("T", "hStroke8", new List<Thing> { yPlus8 });
            Thing U = CreateContainerRelationship("U", "J", new List<Thing> { }).source;
            CreateContainerRelationship("U", "vStroke8", new List<Thing> { });
            Thing V = CreateContainerRelationship("V", "urStroke", new List<Thing> { xPlus2, xPlus1 }).source;
            CreateContainerRelationship("V", "drStroke", new List<Thing> { });
            Thing W = CreateContainerRelationship("W", "V", new List<Thing> { }).source;
            CreateContainerRelationship("W", "V", new List<Thing> { xPlus2, xPlus1 });
            Thing X = CreateContainerRelationship("X", "urStroke", new List<Thing> { }).source;
            CreateContainerRelationship("X", "drStroke", new List<Thing> { });
            Thing Y = CreateContainerRelationship("Y", "urStroke5", new List<Thing> { yPlus4, yPlus1, xPlus2, xPlus1 }).source;
            CreateContainerRelationship("Y", "drStroke5", new List<Thing> { yPlus4, yPlus1 });
            CreateContainerRelationship("Y", "vStroke5", new List<Thing> { xPlus2, xPlus1 });
            Thing Z = CreateContainerRelationship("Z", "urStroke", new List<Thing> { }).source;
            CreateContainerRelationship("Z", "hStroke4", new List<Thing> { yPlus8 });
            CreateContainerRelationship("Z", "hStroke5", new List<Thing> { });

            letters.AddChild(A);
            letters.AddChild(B);
            letters.AddChild(C);
            letters.AddChild(D);


            Thing d1 = CreateContainerRelationship("d1", "I", new List<Thing> { }).source;
            CreateContainerRelationship("d1", "urStroke5", new List<Thing> { yPlus4, yPlus1 });
            Thing d2 = CreateContainerRelationship("d2", "arcD225b", new List<Thing> { yPlus10, xPlus6, rotateZ, rotateZ }).source;
            CreateContainerRelationship("d2", "hStroke", new List<Thing> { });
            Thing d3 = CreateContainerRelationship("d3", "arcD270", new List<Thing> { }).source;
            CreateContainerRelationship("d3", "arcD270", new List<Thing> { yPlus10, rotateX, rotateX });
            Thing d4 = CreateContainerRelationship("d4", "d1", new List<Thing> { }).source;
            CreateContainerRelationship("d4", "hStroke4", new List<Thing> { yPlus2, yPlus1 });
            CreateContainerRelationship("d4", "hStroke2", new List<Thing> { yPlus2, yPlus1, xPlus4 });
            Thing d5 = CreateContainerRelationship("d5", "arcD270", new List<Thing> { }).source;
            CreateContainerRelationship("d5", "hStroke5", new List<Thing> { yPlus8 });
            CreateContainerRelationship("d5", "vStroke4", new List<Thing> { yPlus4 });
            Thing d6 = CreateContainerRelationship("d6", "d9", new List<Thing> { yPlus10, xPlus6, rotateZ, rotateZ }).source;
            Thing d7 = CreateContainerRelationship("d7", "urStroke", new List<Thing> { }).source;
            CreateContainerRelationship("d7", "hStroke4", new List<Thing> { yPlus8 });
            Thing d8 = CreateContainerRelationship("d8", "d3", new List<Thing> { }).source;
            CreateContainerRelationship("d8", "d3", new List<Thing> { xPlus6, rotateY, rotateY });
            Thing d9 = CreateContainerRelationship("d9", "C", new List<Thing> { xPlus6, rotateY, rotateY }).source;
            CreateContainerRelationship("d9", "arcD225", new List<Thing> { yPlus4 });
            Thing d0 = CreateContainerRelationship("d0", "O", new List<Thing> { }).source;
            CreateContainerRelationship("d0", "urStroke5", new List<Thing> { yPlus1, yPlus1 });


            Thing alphabet = CreateContainerRelationship("alphabet", " ", new List<Thing> { }).source;
            string alpha = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            int count = 0;
            foreach (char c in alpha)
            {
                List<Thing> offset = new List<Thing>();
                for (int i = 0; i < count / 2; i++) { offset.Add(xPlus10); };
                for (int i = 0; i < count % 2; i++) { offset.Add(yMinus10); };
                CreateContainerRelationship("alphabet", c.ToString(), offset);
                count++;
            }

            Thing digits = CreateContainerRelationship("digits", " ", new List<Thing> { }).source;
            string digitString = "1234567890";
            count = 0;
            foreach (char c in digitString)
            {
                List<Thing> offset = new List<Thing>();
                for (int i = 0; i < count; i++) { offset.Add(xPlus10); };
                CreateContainerRelationship("digits", "d" + c.ToString(), offset);
                count++;
            }

            Thing test = CreateContainerRelationship("test", "arc", new List<Thing> { orange, rotateX }).source;
            //            CreateContainerRelatinship("diskSquare", "square", new List<Thing>{blue,xPlus10,xPlus10,rotateX});

            Thing FEEL = CreateContainerRelationship("FEEL", "F", new List<Thing> { }).source;
            CreateContainerRelationship("FEEL", "E", new List<Thing> { xPlus6 });
            CreateContainerRelationship("FEEL", "E", new List<Thing> { xPlus6, xPlus6, });
            CreateContainerRelationship("FEEL", "L", new List<Thing> { xPlus6, xPlus6, xPlus6 });

            Thing text = CreateContainerRelationship("text", "FEEL", new List<Thing> { orange, rotateX }).source;
            CreateContainerRelationship("text", "FEEL", new List<Thing> { green, zPlus10, rotateX });
            CreateContainerRelationship("text", "FEEL", new List<Thing> { blue, rotateZ45, rotateX });

            Thing ell = CreateContainerRelationship("ell", "square", new List<Thing> { orange, xPlus10, rotateY }).source;
            CreateContainerRelationship("ell", "square", new List<Thing> { green, zMinus10 });

            CreateContainerRelationship("box", "ell", new List<Thing> { rotateY, rotateY, xPlus10 });
            CreateContainerRelationship("box", "ell", new List<Thing> { xMinus10 });
            CreateContainerRelationship("box", "square", new List<Thing> { blue, rotateX, xMinus10, zMinus10 });

            Thing boxes = CreateContainerRelationship("boxes", "box", new List<Thing> { xMinus10, xMinus10, rotateY45 }, true).source;
            CreateContainerRelationship("boxes", "box", new List<Thing> { xPlus10, xPlus10, xPlus10 });

            Thing manyBoxes = CreateContainerRelationship("manyBoxes", "boxes", new List<Thing> { }).source;
            CreateContainerRelationship("manyBoxes", "boxes", new List<Thing> { yPlus10, yPlus10, yPlus10 });
            CreateContainerRelationship("manyBoxes", "boxes", new List<Thing> { yPlus10, yPlus10, yPlus10, yPlus10, yPlus10, yPlus10 });
            CreateContainerRelationship("manyBoxes", "boxes", new List<Thing> { zPlus10, zPlus10, zPlus10, zPlus10, zPlus10, rotateX45, rotateY45, });

            foreach (Thing t in graphicParent.Children)
            {
                RoundCorners(t.V);
            }
        }
        public override void UKSInitializedNotification()
        {
            UKS.GetOrAddThing("graphic", "Object");
            CreateLibrary();
        }

    }
}