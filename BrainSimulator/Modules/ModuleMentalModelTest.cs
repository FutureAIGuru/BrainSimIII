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
using System.Xml.Serialization;
using static System.Math;

namespace BrainSimulator.Modules;

public class ModuleMentalModelTest : ModuleBase
{
    //any public variable you create here will automatically be saved and restored  with the network
    //unless you precede it with the [XmlIgnore] directive
    //[XlmIgnore] 
    //public theStatus = 1;


    //set size parameters as needed in the constructor
    //set max to be -1 if unlimited
    public ModuleMentalModelTest()
    {
        minHeight = 2;
        maxHeight = 500;
        minWidth = 2;
        maxWidth = 500;
    }


    //fill this method in with code which will execute
    //once for each cycle of the engine

    Random rand = new Random();
    public override void Fire()
    {
        Init();  //be sure to leave this here
                 //if you want the dlg to update, use the following code whenever any parameter changes
                 // UpdateDialog();

        ModuleMentalModel mentalModel = (ModuleMentalModel)FindModule(typeof(ModuleMentalModel));
        if (mentalModel == null) return;
        if (mv.GetNeuronAt(0, 0).Fired())
            mentalModel.Move(new Point3DPlus(.1f, 0f, 0f));
        if (mv.GetNeuronAt(0, 1).Fired())
            mentalModel.Move(new Point3DPlus(-.1f, 0f, 0f));
        if (mv.GetNeuronAt(1, 0).Fired())
            mentalModel.Turn(Angle.FromDegrees(5));
        if (mv.GetNeuronAt(1, 1).Fired())
            mentalModel.Turn(Angle.FromDegrees(-5));
    }

    //fill this method in with code which will execute once
    //when the module is added, when "initialize" is selected from the context menu,
    //or when the engine restart button is pressed
    public override void Initialize()
    {
        GetUKS();  if (UKS == null) return;
        ModuleMentalModel mentalModel = (ModuleMentalModel)FindModule(typeof(ModuleMentalModel));
        if (mentalModel == null) return;
        mentalModel.Clear();
        for (int i = 0; i < 10; i++)
        {
            if (mentalModel is null) return;
            try
            {
                Thing y = mentalModel.AddPhysicalObject(new Dictionary<string, object>  {
                    { "Center",new Point3DPlus(((float)rand.Next(-10, 10)) / 10f, ((float)rand.Next(-10, 10)) / 10f, 0f)},
                    { "UpperLeft",new Point3DPlus(((float)rand.Next(-10, 10)) / 10f, ((float)rand.Next(-10, 10)) / 10f, 0f)},
                    { "Color",new HSLColor(100, 100, 100)}
                });
                Thing w = mentalModel.AddPhysicalObject(new Dictionary<string, object>  {
                    { "Center",new Point3DPlus(((float)rand.Next(-10, 10)) / 10f, ((float)rand.Next(-10, 10)) / 10f, 0f)},
                    { "UpperLeft",new Point3DPlus(((float)rand.Next(-10, 10)) / 10f, ((float)rand.Next(-10, 10)) / 10f, 0f)},
                    { "Color",new HSLColor(100, 100, 100)}
                });
                mentalModel.UpdateProperties(w, new Dictionary<string, object>
                {
                    {"Center",new Point3DPlus(0,0,0f) }
                });
                mentalModel.DeletePhysicalObject(w);
                //testing
                // Dictionary<string, object> data = y.GetReferencesAsDictionary();
                // Thing z = mentalModel.SearchPhysicalObject(data);
                // Dictionary<string, object> data1 = z.GetReferencesAsDictionary();

            }
            catch (Exception ex)
            {
                Debug.WriteLine("EXCEPTION: " + ex.Message);
            }
        }

        //var z = mentalModel.AddObject(new object[] {
        //            new Point3DPlus(5,0f,0),
        //            new HSLColor(100,100,rand.Next(0,10)),
        //        });

        //mentalModel.SetCurrentlyVisible();
        //var z1 = mentalModel.SearchObject(new object[] {
        //            new Point3DPlus(5,0f,0),
        //            new HSLColor(100,100,rand.Next(0,10)),
        //        });
        //var x = mentalModel.SearchObject(new object[] {
        //            new Point3DPlus(((float)rand.Next(-10, 10)) / 10f, ((float)rand.Next(-10, 10)) / 10f, 0f),
        //            new HSLColor(100,100,rand.Next(0,10)),
        //});


        mv.GetNeuronAt(0, 0).Label = @"  /\";
        mv.GetNeuronAt(0, 1).Label = @"  \/";
        mv.GetNeuronAt(1, 0).Label = @"  <";
        mv.GetNeuronAt(1, 1).Label = @"  >";
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

