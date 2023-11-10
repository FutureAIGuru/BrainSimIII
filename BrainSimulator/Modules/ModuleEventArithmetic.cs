//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using static System.Math;

namespace BrainSimulator.Modules;

public class ModuleEventArithmetic : ModuleBase
{
    //any public variable you create here will automatically be saved and restored  with the network
    //unless you precede it with the [XmlIgnore] directive
    //[XmlIgnore] 
    //public theStatus = 1;


    //set size parameters as needed in the constructor
    //set max to be -1 if unlimited
    public ModuleEventArithmetic()
    {
        minHeight = 1;
        maxHeight = 500;
        minWidth = 1;
        maxWidth = 500;
    }


    //fill this method in with code which will execute
    //once for each cycle of the engine
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

    public void Learn(int numberOne, int numberTwo)
    {
        Add(numberOne, numberTwo);
        Multiply(numberOne, numberTwo);
    }

    public void Add(int numberOne, int numberTwo)
    {
        GetUKS();
        ModuleEvent parent = (ModuleEvent)FindModule(typeof(ModuleEvent));
        if (UKS == null || parent == null) return;

        Thing firstNum = UKS.GetOrAddThing("num" + numberOne.ToString(), "Situation");
        Thing secondNum = UKS.GetOrAddThing("num" + numberTwo.ToString(), "Situation");
        Thing resultNum = UKS.GetOrAddThing("num" + (numberOne + numberTwo).ToString(), "Situation");

        Thing firstOutcome = UKS.GetOrAddThing("num" + numberOne.ToString(), "Outcome");
        Thing secondOutcome = UKS.GetOrAddThing("num" + numberTwo.ToString(), "Outcome");
        Thing resultOutcome = UKS.GetOrAddThing("num" + (numberOne + numberTwo).ToString(), "Outcome");

        Thing addFirst = UKS.GetOrAddThing("add" + numberOne.ToString(), "Action");
        Thing addSecond = UKS.GetOrAddThing("add" + numberTwo.ToString(), "Action");

        Thing subFirst = UKS.GetOrAddThing("subtract" + numberOne.ToString(), "Action");
        Thing subSecond = UKS.GetOrAddThing("subtract" + numberTwo.ToString(), "Action");

        IList<Thing> allEvents = UKS.GetOrAddThing("Event", "Behavior").Children;
        Thing firstEvent = null;
        Thing secondEvent = null;
        Thing resultEvent = null;

        foreach (Thing anEvent in allEvents)
        {
            if (anEvent.Children[0] == firstNum)
            {
                firstEvent = anEvent;
                break;
            }
        }

        if (firstEvent == null)
            firstEvent = parent.AddEvent(firstNum);

        foreach (Thing anEvent in allEvents)
        {
            if (anEvent.Children[0] == secondNum)
            {
                secondEvent = anEvent;
                break;
            }
        }

        if (secondEvent == null)
            secondEvent = parent.AddEvent(secondNum);

        foreach (Thing anEvent in allEvents)
        {
            if (anEvent.Children[0] == resultNum)
            {
                resultEvent = anEvent;
                break;
            }
        }

        if (resultEvent == null)
            resultEvent = parent.AddEvent(resultNum);

        parent.AddEventResult(secondEvent, addFirst, resultOutcome);
        parent.AddEventResult(firstEvent, addSecond, resultOutcome);

        parent.AddEventResult(resultEvent, subFirst, secondOutcome);
        parent.AddEventResult(resultEvent, subSecond, firstOutcome);




        //Thing startNum = UKS.GetOrAddThing("num" + numberOne.ToString(), "Situation");
        //Thing endNum = UKS.GetOrAddThing("num" + numberTwo.ToString(), "Situation");

        //Thing toAdd = UKS.GetOrAddThing("add" + (numberTwo - numberOne).ToString(), "Action");

        //event.addEventResult(startNum, toAdd, endNum);
    }

    public void Multiply(int numberOne, int numberTwo)
    {
        GetUKS();
        ModuleEvent parent = (ModuleEvent)FindModule(typeof(ModuleEvent));
        if (UKS == null || parent == null) return;

        Thing firstNum = UKS.GetOrAddThing("num" + numberOne.ToString(), "Situation");
        Thing secondNum = UKS.GetOrAddThing("num" + numberTwo.ToString(), "Situation");
        Thing resultNum = UKS.GetOrAddThing("num" + (numberOne * numberTwo).ToString(), "Situation");

        Thing firstOutcome = UKS.GetOrAddThing("num" + numberOne.ToString(), "Outcome");
        Thing secondOutcome = UKS.GetOrAddThing("num" + numberTwo.ToString(), "Outcome");
        Thing resultOutcome = UKS.GetOrAddThing("num" + (numberOne * numberTwo).ToString(), "Outcome");

        Thing multiplyFirst = UKS.GetOrAddThing("multiply" + numberOne.ToString(), "Action");
        Thing multiplySecond = UKS.GetOrAddThing("multiply" + numberTwo.ToString(), "Action");

        Thing divideFirst = UKS.GetOrAddThing("divide" + numberOne.ToString(), "Action");
        Thing divideSecond = UKS.GetOrAddThing("divide" + numberTwo.ToString(), "Action");

        IList<Thing> allEvents = UKS.GetOrAddThing("Event", "Behavior").Children;
        Thing firstEvent = null;
        Thing secondEvent = null;
        Thing resultEvent = null;

        foreach (Thing anEvent in allEvents)
        {
            if (anEvent.Children[0] == firstNum)
            {
                firstEvent = anEvent;
                break;
            }
        }

        if (firstEvent == null)
            firstEvent = parent.AddEvent(firstNum);

        foreach (Thing anEvent in allEvents)
        {
            if (anEvent.Children[0] == secondNum)
            {
                secondEvent = anEvent;
                break;
            }
        }

        if (secondEvent == null)
            secondEvent = parent.AddEvent(secondNum);

        foreach (Thing anEvent in allEvents)
        {
            if (anEvent.Children[0] == resultNum)
            {
                resultEvent = anEvent;
                break;
            }
        }

        if (resultEvent == null)
            resultEvent = parent.AddEvent(resultNum);

        parent.AddEventResult(secondEvent, multiplyFirst, resultOutcome);
        parent.AddEventResult(firstEvent, multiplySecond, resultOutcome);

        parent.AddEventResult(resultEvent, divideFirst, secondOutcome);
        parent.AddEventResult(resultEvent, divideSecond, firstOutcome);
    }

    public void Populate(int min, int max)
    {
        for (int i = min; i <= max; i++)
            for (int j = min; j <= max; j++)
                Learn(i, j);
    }
}