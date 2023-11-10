//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using Emgu.CV.Cuda;
using Emgu.CV.Stitching;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;
using System.Xml.Serialization;
using static BrainSimulator.Modules.ModuleFrequencyConfidence2;
using static System.Math;

namespace BrainSimulator.Modules
{
    public class ModuleFrequencyConfidence2 : ModuleBase
    {
        // Any public variable you create here will automatically be saved and restored  
        // with the network unless you precede it with the [XmlIgnore] directive
        // [XmlIgnore] 
        // public theStatus = 1;
        public static float evidential_horizon = 1.0f;


        // Set size parameters as needed in the constructor
        // Set max to be -1 if unlimited
        public ModuleFrequencyConfidence2()
        {
            minHeight = 1;
            maxHeight = 500;
            minWidth = 1;
            maxWidth = 500;
        }


        // Fill this method in with code which will execute
        // once for each cycle of the engine
        public override void Fire()
        {
            Init();  //be sure to leave this here
            GetUKS();
            if (UKS == null) return;
            Thing sequences = UKS.GetOrAddThing("Sequences", "Behavior");
            if (sequences != null)
            {
                foreach (Thing t in sequences.Children)
                {
                    if (t.TRUTH == null || t.TRUTH.ToString() == "<F: 0, C: 0>=EXPECT: 0")
                    {
                        SetUpTruths(t);
                    }
                    foreach (Relationship r in t.Relationships)
                    {
                        if (r.t.TRUTH == null || r.t.TRUTH.ToString() == "<F: 0, C: 0>=EXPECT: 0")
                        {
                            SetUpTruths(r.t);
                        }
/*                        if (r.SYLLOGISM == null)
                        {
                            r.SYLLOGISM = SetUpSyllogismTable(r, t);
                        }*/
                    }
                }

/*                int count = 0;
                int countMAX = 3;
                if (sequences.Children[0].Relationships[0].SYLLOGISM != null)
                {
                    while (count < countMAX)
                    {
                        SyllogismTableEnum tableEnum = new SyllogismTableEnum(sequences.Children[0].Relationships[0].SYLLOGISM as SyllogismTable);
                        //tableEnum.Syllogisms = (CombineSyllogismTable(sequences.Children[0].Relationships[0].SYLLOGISM as SyllogismTable));
                        foreach (object o in tableEnum)
                        {
                            Console.WriteLine("dsfsD");
                        }
                        sequences.Children[0].Relationships[0].SYLLOGISM = new SyllogismTable(sequences.Children[0].Relationships[0].t.TRUTH, sequences.Children[0].Relationships[0].t.TRUTH);
                        count++;
                    }
                }*/
                
            }
            UpdateDialog();
        }

        public class SyllogismTableEnum : IEnumerable<object>
        {
            public List<object> CombineSyllogismTable(SyllogismTable st)
            {
                List<object> Syllogisms = new List<object>();
                Syllogisms.Add(st.Revision);
                Syllogisms.Add(st.Deduction);
                Syllogisms.Add(st.Analogy);
                Syllogisms.Add(st.Resemblance);
                Syllogisms.Add(st.Abduction);
                Syllogisms.Add(st.Induction);
                Syllogisms.Add(st.Exemplification);
                Syllogisms.Add(st.Comparison);
                Syllogisms.Add(st.Intersection);
                Syllogisms.Add(st.Union);
                Syllogisms.Add(st.Difference);
                return Syllogisms;
            }
            public List<object> Syllogisms { get; set; }

            public IEnumerator<object> GetEnumerator()
            {
                return Syllogisms.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return Syllogisms.GetEnumerator();
            }

            public SyllogismTableEnum(SyllogismTable st)
            {

                Syllogisms = CombineSyllogismTable(st);
            }

        }

        public class SyllogismTable
        {
            private static TruthValue tV1 = new();

            /*            #region Implementation of IEnumerable
SyllogismTable st { get; set; }
List<object> Syllogisms = new List<object>();
public IEnumerator<object> GetEnumerator()
{
Syllogisms.Add(st.Revision);
Syllogisms.Add(st.Deduction);
Syllogisms.Add(st.Analogy);
Syllogisms.Add(st.Resemblance);
Syllogisms.Add(st.Abduction);
Syllogisms.Add(st.Induction);
Syllogisms.Add(st.Exemplification);
Syllogisms.Add(st.Comparison);
Syllogisms.Add(st.Intersection);
Syllogisms.Add(st.Union);
Syllogisms.Add(st.Difference);

foreach (object o in Syllogisms)
yield return o;
}
#endregion*/
            public SyllogismTable(object T1, object T2)
            {
                TV1 = T1 as TruthValue;
                TV2 = T2 as TruthValue;
            }
            public static Relationship R1 { get; set; } = new();
            public static Relationship R2 { get; set; } = new();
            public static TruthValue TV1 = R1.t.TRUTH as TruthValue;
            public static TruthValue TV2 = R2.t.TRUTH as TruthValue;
            public static float F1 { get { return TV1.F; } set { TV1.F = value; } }
            public static float F2 { get { return TV2.F; } set { TV2.F = value; } }
            public static float C1 { get { return TV1.C; } set { TV1.C = value; } }
            public static float C2 { get { return TV2.C; } set { TV2.C = value; } }
            //
            public Revision Revision { get; set; } = new Revision(R1, R2);
            //
            // STRONG SYLLOGISM
            //
            public Deduction Deduction { get; set; } = new Deduction(R1, R2);
            public Analogy Analogy { get; set; } = new Analogy(R1, R2);
            public Resemblance Resemblance { get; set; } = new Resemblance(R1, R2);
            //
            // WEAK SYLLOGISM
            //
            public Abduction Abduction { get; set; } = new Abduction(R1, R2);
            public Induction Induction { get; set; } = new Induction(R1, R2);
            public Exemplification Exemplification { get; set; } = new Exemplification(R1, R2);
            public Comparison Comparison { get; set; } = new Comparison(R1, R2);
            //
            // TERM COMPOSITION
            //
            public Intersection Intersection { get; set; } = new Intersection(R1, R2);
            public Union Union { get; set; } = new Union(R1, R2);
            public Difference Difference { get; set; } = new Difference(R1, R2);

            public int Count()
            {
                return 11;
            }
        }

        public class Difference
        {
            public float F1 { get; set; }
            public float F2 { get; set; }
            public float C1 { get; set; }
            public float C2 { get; set; }
            public float F;
            public float C;
            public TruthValue TV = new TruthValue();
            public Difference()
            {
                F1 = 0;
                F2 = 0;
                C1 = 0;
                C2 = 0;
                // Deduction - Strong Syllogism
                F = F1 * (1 - F2);
                C = C1 * C2;
                TV.C = C;
                TV.F = F;
            }
            public Difference(Relationship r1, Relationship r2)
            {
                TruthValue r1Truth = r1.t.TRUTH as TruthValue;
                TruthValue r2Truth = r2.t.TRUTH as TruthValue;

                F1 = r1Truth.F;
                F2 = r2Truth.F;
                C1 = r1Truth.C;
                C2 = r2Truth.C;
                // Deduction - Strong Syllogism
                F = F1 * (1 - F2);
                C = C1 * C2;
                TV.C = C;
                TV.F = F;
            }
            public override string ToString()
            {
                return "F: " + F + ", C: " + C;
            }
        }

        public class Union
        {
            public float F1 { get; set; }
            public float F2 { get; set; }
            public float C1 { get; set; }
            public float C2 { get; set; }
            public float F;
            public float C;
            public TruthValue TV = new TruthValue();
            public Union()
            {
                F1 = 0;
                F2 = 0;
                C1 = 0;
                C2 = 0;
                // Union - Term Composition
                F = F1 + F2 - F1 * F2;
                C = C1 * C2;
                TV.C = C;
                TV.F = F;
            }
            public Union(Relationship r1, Relationship r2)
            {
                TruthValue r1Truth = r1.t.TRUTH as TruthValue;
                TruthValue r2Truth = r2.t.TRUTH as TruthValue;

                F1 = r1Truth.F;
                F2 = r2Truth.F;
                C1 = r1Truth.C;
                C2 = r2Truth.C;
                // Union - Term Composition
                F = F1 + F2 - F1 * F2;
                C = C1 * C2;
                TV.C = C;
                TV.F = F;
            }
            public override string ToString()
            {
                return "F: " + F + ", C: " + C;
            }
        }

        public class Intersection
        {
            public float F1 { get; set; }
            public float F2 { get; set; }
            public float C1 { get; set; }
            public float C2 { get; set; }
            public float F;
            public float C;
            public TruthValue TV = new TruthValue();
            public Intersection()
            {
                F1 = 0;
                F2 = 0;
                C1 = 0;
                C2 = 0;
                // Intersection - Term Composition
                F = F1 * F2;
                C = C1 * C2;
                TV.C = C;
                TV.F = F;
            }
            public Intersection(Relationship r1, Relationship r2)
            {
                TruthValue r1Truth = r1.t.TRUTH as TruthValue;
                TruthValue r2Truth = r2.t.TRUTH as TruthValue;

                F1 = r1Truth.F;
                F2 = r2Truth.F;
                C1 = r1Truth.C;
                C2 = r2Truth.C;
                // Intersection - Term Composition
                F = F1 * F2;
                C = C1 * C2;
                TV.C = C;
                TV.F = F;
            }
            public override string ToString()
            {
                return "F: " + F + ", C: " + C;
            }
        }

        public class Comparison
        {
            public float F1 { get; set; }
            public float F2 { get; set; }
            public float C1 { get; set; }
            public float C2 { get; set; }
            public float F;
            public float C;
            public TruthValue TV = new TruthValue();
            public Comparison()
            {
                F1 = 0;
                F2 = 0;
                C1 = 0;
                C2 = 0;
                // Comparison - Weak Syllogism
                if (F1 + F2 - F1 * F2 != 0)
                    F = (C1 * F2) / (F1 + F2 - F1 * F2);
                else
                    F = 0;
                if (F1 + F2 - F1 * F2 != 0 && C1 * C2 != 0)
                    C = (C1 * C2 * (F1 + F2 - F1 * F2)) / (C1 * C2 * (F1 + F2 - F1 * F2) + evidential_horizon);
                else
                    C = 0;
                TV.C = C;
                TV.F = F;
            }
            public Comparison(Relationship r1, Relationship r2)
            {
                TruthValue r1Truth = r1.t.TRUTH as TruthValue;
                TruthValue r2Truth = r2.t.TRUTH as TruthValue;

                F1 = r1Truth.F;
                F2 = r2Truth.F;
                C1 = r1Truth.C;
                C2 = r2Truth.C;
                // Comparison - Weak Syllogism
                if (F1 + F2 - F1 * F2 != 0)
                    F = (C1 * F2) / (F1 + F2 - F1 * F2);
                else
                    F = 0;
                if (F1 + F2 - F1 * F2 != 0 && C1 * C2 != 0)
                    C = (C1 * C2 * (F1 + F2 - F1 * F2)) / (C1 * C2 * (F1 + F2 - F1 * F2) + evidential_horizon);
                else
                    C = 0;
                TV.C = C;
                TV.F = F;
            }
            public override string ToString()
            {
                return "F: " + F + ", C: " + C;
            }
        }

        public class Exemplification
        {
            public float F1 { get; set; }
            public float F2 { get; set; }
            public float C1 { get; set; }
            public float C2 { get; set; }
            public float F;
            public float C;
            public TruthValue TV = new TruthValue();
            public Exemplification()
            {
                F1 = 0;
                F2 = 0;
                C1 = 0;
                C2 = 0;
                // Exemplification - Weak Syllogism
                F = 1;
                C = (F1 * C1 * F2 * C2) / (F1 * C1 * F2 * C2 + evidential_horizon);
                TV.C = C;
                TV.F = F;
            }
            public Exemplification(Relationship r1, Relationship r2)
            {
                TruthValue r1Truth = r1.t.TRUTH as TruthValue;
                TruthValue r2Truth = r2.t.TRUTH as TruthValue;

                F1 = r1Truth.F;
                F2 = r2Truth.F;
                C1 = r1Truth.C;
                C2 = r2Truth.C;
                // Exemplification - Weak Syllogism
                F = 1;
                C = (F1 * C1 * F2 * C2) / (F1 * C1 * F2 * C2 + evidential_horizon);
                TV.C = C;
                TV.F = F;
            }
            public override string ToString()
            {
                return "F: " + F + ", C: " + C;
            }
        }

        public class Induction
        {
            public float F1 { get; set; }
            public float F2 { get; set; }
            public float C1 { get; set; }
            public float C2 { get; set; }
            public float F;
            public float C;
            public TruthValue TV = new TruthValue();
            public Induction()
            {
                F1 = 0;
                F2 = 0;
                C1 = 0;
                C2 = 0;
                // Induction - Weak Syllogism
                F = F1;
                C = (C1 * F2 * C2) / (C1 * F2 * C2 + evidential_horizon);
                TV.C = C;
                TV.F = F;
            }
            public Induction(Relationship r1, Relationship r2)
            {
                TruthValue r1Truth = r1.t.TRUTH as TruthValue;
                TruthValue r2Truth = r2.t.TRUTH as TruthValue;

                F1 = r1Truth.F;
                F2 = r2Truth.F;
                C1 = r1Truth.C;
                C2 = r2Truth.C;
                // Induction - Weak Syllogism
                F = F1;
                C = (C1 * F2 * C2) / (C1 * F2 * C2 + evidential_horizon);
                TV.C = C;
                TV.F = F;
            }
            public override string ToString()
            {
                return "F: " + F + ", C: " + C;
            }
        }

        public class Abduction
        {
            public float F1 { get; set; }
            public float F2 { get; set; }
            public float C1 { get; set; }
            public float C2 { get; set; }
            public float F;
            public float C;
            public TruthValue TV = new TruthValue();
            public Abduction()
            {
                F1 = 0;
                F2 = 0;
                C1 = 0;
                C2 = 0;
                // Abduction - Weak Syllogism
                F = F2;
                C = (F1 * C1 * C2) / (F1 * C1 * C2 + evidential_horizon);
                TV.C = C;
                TV.F = F;
            }
            public Abduction(Relationship r1, Relationship r2)
            {
                TruthValue r1Truth = r1.t.TRUTH as TruthValue;
                TruthValue r2Truth = r2.t.TRUTH as TruthValue;

                F1 = r1Truth.F;
                F2 = r2Truth.F;
                C1 = r1Truth.C;
                C2 = r2Truth.C;
                // Abduction - Weak Syllogism
                F = F2;
                C = (F1 * C1 * C2) / (F1 * C1 * C2 + evidential_horizon);
                TV.C = C;
                TV.F = F;
            }
            public override string ToString()
            {
                return "F: " + F + ", C: " + C;
            }
        }

        public class Resemblance
        {
            public float F1 { get; set; }
            public float F2 { get; set; }
            public float C1 { get; set; }
            public float C2 { get; set; }
            public float F;
            public float C;
            public TruthValue TV = new TruthValue();
            public Resemblance()
            {
                F1 = 0;
                F2 = 0;
                C1 = 0;
                C2 = 0;
                // Resemblance - Strong Syllogism
                F = F1 * F2;
                C = C1 * C2 * (F1 + F2 - F1 * F2);
                TV.C = C;
                TV.F = F;
            }
            public Resemblance(Relationship r1, Relationship r2)
            {
                TruthValue r1Truth = r1.t.TRUTH as TruthValue;
                TruthValue r2Truth = r2.t.TRUTH as TruthValue;

                F1 = r1Truth.F;
                F2 = r2Truth.F;
                C1 = r1Truth.C;
                C2 = r2Truth.C;
                // Resemblance - Strong Syllogism
                F = F1 * F2;
                C = C1 * C2 * (F1 + F2 - F1 * F2);
                TV.C = C;
                TV.F = F;
            }
            public override string ToString()
            {
                return "F: " + F + ", C: " + C;
            }
        }
        public class Analogy
        {
            public float F1 { get; set; }
            public float F2 { get; set; }
            public float C1 { get; set; }
            public float C2 { get; set; }
            public float F;
            public float C;
            public TruthValue TV = new TruthValue();
            public Analogy()
            {
                F1 = 0;
                F2 = 0;
                C1 = 0;
                C2 = 0;
                // Analogy - Strong Syllogism
                F = F1 * F2;
                C = C1 * F2 * F2;
                TV.C = C;
                TV.F = F;
            }
            public Analogy(Relationship r1, Relationship r2)
            {
                TruthValue r1Truth = r1.t.TRUTH as TruthValue;
                TruthValue r2Truth = r2.t.TRUTH as TruthValue;

                F1 = r1Truth.F;
                F2 = r2Truth.F;
                C1 = r1Truth.C;
                C2 = r2Truth.C;
                // Analogy - Strong Syllogism
                F = F1 * F2;
                C = C1 * F2 * F2;
                TV.C = C;
                TV.F = F;
            }
            public override string ToString()
            {
                return "F: " + F + ", C: " + C;
            }
        }

        public class Revision
        {
            public float F1 { get; set; }
            public float F2 { get; set; }
            public float C1 { get; set; }
            public float C2 { get; set; }
            public float F;
            public float C;
            public TruthValue TV = new TruthValue();
            public Revision()
            {
                F1 = 0;
                F2 = 0;
                C1 = 0;
                C2 = 0;
                // Revision - Two statements that are the same
                if (C1 != 0 && C2 != 0)
                {
                    F = (F1 * C1 * (1 - C2) + F2 * C2 * (1 - C1)) / (C1 * (1 - C2) + C2 * (1 - C1));
                    C = (C1 * (1 - C2) + C2 * (1 - C1)) / (C1 * (1 - C2) + C2 * (1 - C1) + (1 - C1) * (1 - C2));
                    TV.C = C;
                    TV.F = F;
                }
                else
                {
                    F = 0;
                    C = 0;
                    TV.C = C;
                    TV.F = F;
                }
            }
            public Revision(Relationship r1, Relationship r2)
            {
                TruthValue r1Truth = r1.t.TRUTH as TruthValue;
                TruthValue r2Truth = r2.t.TRUTH as TruthValue;

                F1 = r1Truth.F;
                F2 = r2Truth.F;
                C1 = r1Truth.C;
                C2 = r2Truth.C;
                // Revision - Two statements that are the same
                if (C1 != 0 && C2 != 0)
                {
                    F = (F1 * C1 * (1 - C2) + F2 * C2 * (1 - C1)) / (C1 * (1 - C2) + C2 * (1 - C1));
                    C = (C1 * (1 - C2) + C2 * (1 - C1)) / (C1 * (1 - C2) + C2 * (1 - C1) + (1 - C1) * (1 - C2));
                    TV.C = C;
                    TV.F = F;
                }
                else
                {
                    F = 0;
                    C = 0;
                    TV.C = C;
                    TV.F = F;
                }
            }
            public override string ToString()
            {
                return "F: " + F + ", C: " + C;
            }
        }
        public class Deduction
        {
            public float F1 { get; set; }
            public float F2 { get; set; }
            public float C1 { get; set; }
            public float C2 { get; set; }
            public float F;
            public float C;
            public TruthValue TV = new TruthValue();
            public Deduction()
            {
                F1 = 0;
                F2 = 0;
                C1 = 0;
                C2 = 0;
                // Deduction - Strong Syllogism
                F = F1 * F2;
                C = F1 * C1 * F2 * C2;
                TV.C = C;
                TV.F = F;
            }
            public Deduction(Relationship r1, Relationship r2)
            {
                TruthValue r1Truth = r1.t.TRUTH as TruthValue;
                TruthValue r2Truth = r2.t.TRUTH as TruthValue;

                F1 = r1Truth.F;
                F2 = r2Truth.F;
                C1 = r1Truth.C;
                C2 = r2Truth.C;
                // Deduction - Strong Syllogism
                F = F1 * F2;
                C = F1 * C1 * F2 * C2;
                TV.C = C;
                TV.F = F;
            }
            public override string ToString()
            {
                return "F: " + F + ", C: " + C;
            }
        }

        public class TruthValue
        {
            public float F { get; set; }
            public float C { get; set; }
            public float EXPECT;

            public TruthValue()
            {
                F = 1.0f;
                C = 0.9f;
                EXPECT = C * (F - 0.5f) + 0.5f;
            }

            public TruthValue(float f, float c)
            {
                F = f;
                C = c;
                EXPECT = C * (F - 0.5f) + 0.5f;
            }

            public TruthValue(int wplus, int wminus)
            {
                F = (float)(wplus / (wplus + wminus));
                C = (float)((wplus + wminus) / (wplus + wminus + evidential_horizon));
                // Expectation value = (l + u) / 2 = the mean of the upper and lower bounds for the frequency
                EXPECT = C * (F - 0.5f) + 0.5f;
            }

            public override string ToString()
            {
                return "<F: " + F + ", C: " + C + ">=EXPECT: " + EXPECT;
            }
        }

        public TruthValue SetUpTruths(Thing t)
        {
            TruthValue tv = new TruthValue();
            t.TRUTH = tv;
            return tv;
        }


        public SyllogismTable SetUpSyllogismTable(Relationship relationship1, Relationship relationship2)
        {
            int test = 0;
            if (relationship1.t.TRUTH != null && relationship2.t.TRUTH != null)
            {
                SyllogismTable ST = new SyllogismTable(relationship1.t.TRUTH, relationship2.t.TRUTH);
                return ST;
            }
            else
                return null;
        }

        // Fill this method in with code which will execute once
        // when the module is added, when "initialize" is selected from the context menu,
        // or when the engine restart button is pressed
        public override void Initialize()
        {
        }

        // The following can be used to massage public data to be different in the xml file
        // delete if not needed
        public override void SetUpBeforeSave()
        {
        }
        public override void SetUpAfterLoad()
        {
        }

        // Called whenever the size of the module rectangle changes
        // for example, you may choose to reinitialize whenever size changes
        // delete if not needed
        public override void SizeChanged()
        {
            if (mv == null) return; //this is called the first time before the module actually exists
        }

        // called whenever the UKS performed an Initialize()
        public override void UKSInitializedNotification()
        {

        }
    }
}