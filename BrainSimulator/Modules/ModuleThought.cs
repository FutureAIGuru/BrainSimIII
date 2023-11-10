//
// Copyright (c) FutureAI. All rights reserved.  
// Contains confidential and  proprietary information and programs which may not be distributed without a separate license
//  

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace BrainSimulator.Modules
{
    public class ModuleThought : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XlmIgnore] 
        //public theStatus = 1;


        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleThought()
        {
            minHeight = 1;
            maxHeight = 1;
            minWidth = 1;
            maxWidth = 1;
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
            GetUKS();
            UKS.SetupNumbers();

            //this checks for an already-saved UKS
            List<Relationship> rawResults;
            List<Thing> thingLista = UKS.ComplexQuery("steve", "went", "", "with|and", "person", out rawResults);
            if (thingLista.Count == 2 && rawResults.Count == 2)
                return;


            Thing t;
            UKS.GetOrAddThing("is-a", "Relationship");
            UKS.AddStatement("is-a", "inverseOf", "has-child");
            UKS.GetOrAddThing("person", "Object");
            UKS.GetOrAddThing("animal", "Object");
            UKS.GetOrAddThing("has", "Relationship");
            UKS.AddStatement("has", "hasProperty", "makeInstance");
            UKS.GetOrAddThing("action", "Object");
            UKS.GetOrAddThing("thought", "Object");
            UKS.AddStatement("with", "hasProperty", "makeInstance");
            UKS.AddStatement("with", "hasProperty", "commutative");
            UKS.AddStatement("with", "hasProperty", "transitive");


            UKS.SetParent("red", "color");
            UKS.SetParent("color", "Object");
            UKS.GetOrAddThing("black", "color");
            UKS.GetOrAddThing("white", "color");
            UKS.GetOrAddThing("green", "color");



            //test?
            //the boy with the red hat went to the store with the girl wearing a green hat


            //a few tests of creation of instances of a hat
            //TODO: when mary has a red hat, it is assumed to be the same as one of suzie's red hats
            UKS.AddStatement("suzie", "is-a", "person");
            UKS.AddStatement("suzie", "has", "hat");
            UKS.AddStatement("suzie", "has", "hat", null, null, new List<string> { "red", "big" });
            UKS.AddStatement("suzie", "has", "hat", null, null, new List<string> { "red", "big" });
            UKS.AddStatement("suzie", "has", "hat", null, null, "small|red");
            UKS.AddStatement("hat", "is-a", "clothe");
            UKS.AddStatement("clothe", "is-a", "Object");

            UKS.AddStatement("mary", "is-a", "person");
            UKS.AddStatement("mary", "has", "hat", null, null, "green");
            UKS.AddStatement("mary", "has", "hat", null, null, "red");

            var queryResult = UKS.Query("suzie", "", "hat");
            Debug.Assert(queryResult.Count == 3);
            var z1 = UKS.ResultsOfType(queryResult, "number");
            Debug.Assert(z1[0].Label == "three");
            queryResult = UKS.Query("mary", "", "hat");
            Debug.Assert(queryResult.Count == 2);
            queryResult = UKS.Query("mary", "", "hat", null, null, "green");
            Debug.Assert(queryResult.Count == 1);
            queryResult = UKS.Query("suzie", "has", "hat");
            Debug.Assert(queryResult.Count == 3);
            z1 = UKS.ResultsOfType(queryResult, "color");
            Debug.Assert(z1[0].Label == "red"); //at this point, the only colored hats are red

            UKS.AddStatement("dog", "is-a", "animal");
            UKS.AddStatement("fido", "is-a", "dog");
            UKS.AddStatement("dog", "has", "tail", null, null, new List<string> { "waggy" });
            UKS.AddStatement("fido", "has", "tail", null, null, new List<string> { "stubby" });
            UKS.AddStatement("horse", "is-a", "animal");
            UKS.AddStatement("horse", "has", "tail", null, null, new List<string> { "hair", "long" });
            UKS.AddStatement("tail ", "is-a", "bodyPart");
            queryResult = UKS.Query("dog", "", "tail");
            Debug.Assert(queryResult.Count == 1);
            queryResult = UKS.Query(queryResult[0].target, "is", "");
            Debug.Assert(queryResult.Count == 1);
            Debug.Assert(queryResult[0].target.Label == "waggy");
            queryResult = UKS.Query("fido", "", "tail");
            Debug.Assert(queryResult.Count == 2);
            queryResult = UKS.Query(queryResult[0].target, "is", ""); // properties of fido's tail
            Debug.Assert(queryResult.Count == 1);
            Debug.Assert(queryResult[0].target.Label == "stubby");
            queryResult = UKS.Query("", "has", "tail");
            Debug.Assert(queryResult.Count == 3);

            //suzie likes mary
            UKS.AddStatement("bill", "is-a", "person");
            UKS.AddStatement("fred", "is-a", "person");
            UKS.AddStatement("suzie", "likes", "mary");
            UKS.AddStatement("suzie", "likes", "bill");
            UKS.AddStatement("bill", "likes", "mary");

            queryResult = UKS.Query("suzie", "likes", "");
            queryResult = UKS.Query("", "likes", "mary");

            //suzie doesn't like fred
            UKS.AddStatement("suzie", "likes", "fred", null, "not");
            queryResult = UKS.Query("suzie");

            //suzie went to the groceery store
            Relationship rx = UKS.AddStatement("suzie", "went", "store", null, "to", "grocery");
            queryResult = UKS.Query("suzie", "", "store");
            Debug.Assert(queryResult.Count == 1);

            //handle clauses
            //suzie went to the store with mary
            Relationship ry = UKS.AddStatement("", "with", "mary");
            rx.AddClause(AppliesTo.target, ry);
            queryResult = UKS.Query("suzie", "", "store");
            Debug.Assert(queryResult.Count == 1 && queryResult[0].clauses.Count == 1);

            //the man went to the store
            rx = UKS.AddStatement("man", "went", "store", new List<string> { "tall", "dark" }, "to").
                AddClause(AppliesTo.source, UKS.AddStatement("", "with", "hat", null, null, "striped"));
            queryResult = UKS.Query("", "went", "store");
            Debug.Assert(queryResult.Count == 2);
            UKS.AddStatement("man", "is-a", "person");
            
            //the man in the red had is Fred
            //suzie ran to the store quickly
            rx = UKS.AddStatement("suzie", "ran", "store", null, new List<string> { "quickly", "to" });
            queryResult = UKS.Query("suzie", "", "store");
            Debug.Assert(queryResult.Count == 2);

            //dog has 4 legs
            UKS.AddStatement("dog", "has", "leg", null, "four");
            queryResult = UKS.Query("dog", "", "leg");
            Debug.Assert(queryResult.Count == 1);
            Debug.Assert(queryResult[0].relType.Relationships.Count == 1 && queryResult[0].relType.Relationships[0].target.Label == "four");
            z1 = UKS.ResultsOfType(queryResult, "number");
            Debug.Assert(z1[0].Label == "four");

            UKS.AddStatement("fido", "is", "brown");

            //test of inheriting the number of legs
            queryResult = UKS.Query("fido", "", "");
            Debug.Assert(queryResult.Count == 4);
            UKS.AddStatement("spot", "is-a", "dog");
            UKS.AddStatement("spot", "has", "leg", null, "three");
            queryResult = UKS.Query("spot", "", "leg");
            Debug.Assert(queryResult.Count == 1);
            Debug.Assert(queryResult[0].relType.Relationships.Count == 1 && queryResult[0].relType.Relationships[0].target.Label == "three");
            queryResult = UKS.Query("spot", "", "");
            Debug.Assert(queryResult.Count == 2);
            UKS.AddStatement("leg", "is-a", "bodyPart");

            UKS.AddStatement("suzie", "sing", "", null, "can");
            queryResult = UKS.Query("suzie", "", "", null, "can");
            Debug.Assert(queryResult.Count == 1);
            UKS.AddStatement("suzie", "dance", "", null, "can");
            queryResult = UKS.Query("suzie", "", "", null, "can");
            Debug.Assert(queryResult.Count == 2);
            UKS.AddStatement("suzie", "play", "", null, "can");
            queryResult = UKS.Query("suzie", "", "", null, "can");
            Debug.Assert(queryResult.Count == 3);
            //TODO make this case work
            //UKS.AddStatement("suzie", "can", "", null, new List<string> { "play", "not" });
            //z = UKS.Query("suzie", "can");

            //a dumptruck is a toy and is red
            UKS.AddStatement("dumptruck", "is-a", "toy");
            queryResult = UKS.Query("dumptruck", "", "");
            Debug.Assert(queryResult.Count == 0);  //dumptruck is a toy but toy has not properties
            UKS.AddStatement("dumptruck", "is", "red");

            //current state of dumptruck "Name some toys"
            queryResult = UKS.Query("", "is-a", "toy");
            Debug.Assert(queryResult.Count == 1);
            //current state of dumptruck
            queryResult = UKS.Query("dumptruck", "", "");
            Debug.Assert(queryResult.Count == 1);
            //is the dumptruck broken?
            queryResult = UKS.Query("dumptruck", "is", "broken");
            Debug.Assert(queryResult.Count == 0);

            //a person can play with a tow if it is fixed
            Relationship r1 = UKS.AddStatement("toy", "is", "fixed");
            queryResult = UKS.Query("suzie", "can");
            Relationship r2 = UKS.AddStatement("person", "play", "toy", null, "can", "");
            queryResult = UKS.Query("suzie", "can");
            r2.AddClause(AppliesTo.all, r1);
            Debug.Assert(r2.clauses.Count == 1);


            //broken and fixed are conditions and are exclusive
            //UKS.GetOrAddThing("broken", "condition");
            //UKS.GetOrAddThing("fixed", "condition");
            UKS.AddStatement("fixed", "is-a", "condition");
            UKS.AddStatement("broken", "is-a", "condition");
            Relationship r100 = UKS.AddStatement("condition", "hasProperty", "isexclusive");

            //can suzie play dumptruck?
            queryResult = UKS.Query("suzie", "play", "dumptruck", null, "can");
            Debug.Assert(queryResult.Count == 1);
            queryResult = UKS.Why();
            //break the dumptruck
            UKS.AddStatement("dumptruck", "is", "broken");
            queryResult = UKS.Query("dumptruck");
            //now can suzie play dumptruck? "Can suzie play with a dumptruck?"
            queryResult = UKS.Query("suzie", "play", "dumptruck", null, "can");
            Debug.Assert(queryResult.Count == 0);
            queryResult = UKS.Why();


            UKS.AddStatement("suzie", "has", "leg", null, null, "long");
            UKS.AddStatement("long", "is-a", "length");
            UKS.AddStatement("short", "is-a", "length");
            UKS.AddStatement("length", "hasProperty", "isexclusive");
            UKS.AddStatement("length", "is-a", "size");


            UKS.GetOrAddThing("flea", "animal");

            //TODO: this does not work yet
            r2 = UKS.AddStatement("fido", "is-a", "dog", null, "not", null);


            //"properties of dog"
            queryResult = UKS.Query("dog", "", "");
            UKS.AddStatement("dog", "has", "fur");
            UKS.AddStatement("animal", "has", "eye", null, "two");
            UKS.AddStatement("fido", "has", "leg", null, "three");
            UKS.AddStatement("fido", "has", "leg", null, null, "long");
            UKS.AddStatement("fido", "has", "flea", null, "many");
            UKS.AddStatement("fido", "has", "tail");
            UKS.AddStatement("animal", "has", "flea", null, "none");

            //"properties of fido"
            queryResult = UKS.Query("fido", "has", "");
            Debug.Assert(queryResult.Count == 7);
            //"what references legs?"
            queryResult = UKS.Query("", "", "leg");
            Debug.Assert(queryResult.Count == 6);
            //"what has legs?"
            queryResult = UKS.Query("", "has", "leg");
            Debug.Assert(queryResult.Count == 5);
            //"what has four legs?"
            queryResult = UKS.Query("", "has", "leg", null, "four");
            Debug.Assert(queryResult.Count == 1);
            //"what has three legs?"
            queryResult = UKS.Query("", "has", "leg", null, "three");
            Debug.Assert(queryResult.Count == 2);
            //"what has long legs?"
            queryResult = UKS.Query("", "has", "leg", null, null, "long");
            Debug.Assert(queryResult.Count == 2);
            //"does fido have fleas?"
            queryResult = UKS.Query("fido", "has", "flea");
            Debug.Assert(queryResult.Count == 1);

            //does suzie have fingers
            queryResult = UKS.Query("suzie", "has", "finger");
            Debug.Assert(queryResult.Count == 0);
            UKS.AddStatement("person", "has", "finger");
            //does suzie have fingers
            queryResult = UKS.Query("suzie", "has", "finger");
            Debug.Assert(queryResult.Count == 1);

            //more toys
            UKS.SetParent("train", "toy");
            UKS.SetParent("blocks", "toy");
            UKS.SetParent("doll", "toy");
            UKS.SetParent("computer", "toy");
            UKS.SetParent("locomotive", "toy");
            UKS.SetParent("engine", "toy");
            //name some toys
            queryResult = UKS.Query("toy", "has-child");
            Debug.Assert(queryResult.Count == 7);


            UKS.GetOrAddThing("zebra", "animal");
            UKS.AddStatement("zebra", "is", "black");
            UKS.AddStatement("zebra", "is", "white");
            UKS.GetOrAddThing("babyZebra", "zebra");
            queryResult = UKS.Query("color");
            Debug.Assert(queryResult.Count == 4);
            z1 = UKS.ResultsOfType(queryResult, "number");
            Debug.Assert(z1[0].Label == "many");
            queryResult = UKS.Query("babyZebra");
            UKS.AddStatement("color", "hasProperty", "allowMultiple");
            queryResult = UKS.Query("babyZebra");
            Debug.Assert(queryResult.Count == 4);
            z1 = UKS.ResultsOfType(queryResult, "color");
            Debug.Assert(z1.Count == 2);

            UKS.GetOrAddThing("panther", "animal");
            UKS.AddStatement("panther", "is", "black");
            UKS.GetOrAddThing("babyPanther", "panther");
            queryResult = UKS.Query("babyPanther");
            Debug.Assert(queryResult.Count == 3);
            Debug.Assert(queryResult[0].target.Label == "black");
            UKS.AddStatement("babyPanther", "is", "white");
            queryResult = UKS.Query("babyPanther");
            Debug.Assert(queryResult.Count == 3);
            Debug.Assert(queryResult[0].target.Label == "white");



            Relationship r7 = UKS.AddStatement("dumptruck", "wheel", "lost", null, null, null);
            Relationship r8 = UKS.AddStatement("dumptruck", "is", "old");
            r7.AddClause(AppliesTo.all, r8);
            Relationship r9 = UKS.AddStatement("metal", "is", "red");
            Relationship r10 = UKS.AddStatement("metal", "is", "hot");
            r10.AddClause(AppliesTo.all, r9);
            Relationship r15 = UKS.AddStatement("you", "have", "finger", null, null, null);
            Relationship r16 = UKS.AddStatement("you", "have", "hand", null, null, null);
            r16.AddClause(AppliesTo.all, r15);
            Relationship r17 = UKS.AddStatement("you", "have", "hand", null, null, null);
            Relationship r18 = UKS.AddStatement("you", "have", "body", null, null, null);
            r18.AddClause(AppliesTo.all, r17);
            Relationship r19 = UKS.AddStatement("you", "have", "body", null, null, null);
            Relationship r20 = UKS.AddStatement("you", "have", "brain", null, null, null);
            r19.AddClause(AppliesTo.all, r20);

            //does suzie have fingers?
            Relationship r101 = UKS.AddStatement("you", "have", "finger", null, null, null);
            queryResult = UKS.Query("you", "have", "hand");
            Debug.Assert(queryResult.Count == 1);


            //suzie is a girl
            t = UKS.SetParent(UKS.GetOrAddThing("suzie", null), "girl");
            UKS.AddStatement("girl", "is-a", "person");
            //suzie has two legs
            UKS.SetParent("leg", "bodyPart");
            UKS.SetParent("bodyPart", "Object");
            UKS.AddStatement("person", "has", "leg", "", "two");
            queryResult = UKS.Query("suzie", "", "leg");
            Debug.Assert(queryResult.Count == 2);


            //crowd is many person
            UKS.AddStatement("crowd", "is", "person", null, "many");


            UKS.AddStatement("can", "oppositeOf", "cannot");
            UKS.AddStatement("is-a", "inverseOf", "has-child");
            UKS.AddStatement("greaterThan", "hasProperty", "transitive");
            UKS.AddStatement("lessThan", "inverseOf", "greaterThan");
            UKS.AddStatement("isSimilarTo", "hasProperty", "transitive");
            UKS.AddStatement("isSimilarTo", "hasProperty", "commutative");
            UKS.AddStatement("isSimilarTo", "oppositeOf", "isDifferentFrom");
            UKS.AddStatement("transitive", "is-a", "Relationship");
            UKS.AddStatement("commutative", "is-a", "Relationship");
            UKS.AddStatement("isSimilarTo", "is-a", "Relationship");
            UKS.AddStatement("isDifferentFrom", "is-a", "Relationship");
            UKS.AddStatement("lessThan", "is-a", "Relationship");
            UKS.AddStatement("greaterThan", "is-a", "Relationship");
            UKS.AddStatement("allowMultiple", "is-a", "Relationship");



            UKS.AddStatement("locomotive", "isSimilarTo", "engine");
            queryResult = UKS.Query("locomotive");
            Debug.Assert(queryResult.Count == 2);
            queryResult = UKS.Query("engine");
            Debug.Assert(queryResult.Count == 2);
            UKS.AddStatement("jerry", "isSimilarTo", "engine");
            queryResult = UKS.Query("locomotive");
            Debug.Assert(queryResult.Count == 2);
            queryResult = UKS.Query("engine");
            Debug.Assert(queryResult.Count == 3);
            queryResult = UKS.Query("jerry");
            Debug.Assert(queryResult.Count == 1);
            queryResult = UKS.Query("jerry", "isSimilarTo");
            Debug.Assert(queryResult.Count == 2);
            queryResult = UKS.Query("engine", "isSimilarTo");
            Debug.Assert(queryResult.Count == 2);
            queryResult = UKS.Query("locomotive", "isSimilarTo");
            Debug.Assert(queryResult.Count == 2);
            queryResult = UKS.Query("", "isSimilarTo", "jerry");
            Debug.Assert(queryResult.Count == 2);
            queryResult = UKS.Query("", "isSimilarTo", "engine");
            Debug.Assert(queryResult.Count == 2);
            queryResult = UKS.Query("locomotive", "isSimilarTo");
            Debug.Assert(queryResult.Count == 2);
            queryResult = UKS.Query("engine", "", "locomotive");
            Debug.Assert(queryResult.Count == 1);
            queryResult = UKS.Query("engine", "", "jerry");
            Debug.Assert(queryResult.Count == 1);


            //test of transitive properties
            queryResult = UKS.Query("four", "greaterThan", "one");
            Debug.Assert(queryResult.Count == 1);
            queryResult = UKS.Query("one", "greaterThan", "four");
            Debug.Assert(queryResult.Count == 0);
            queryResult = UKS.Query("four", "greaterThan");
            Debug.Assert(queryResult.Count == 4);
            queryResult = UKS.Query("", "greaterThan", "one");
            Debug.Assert(queryResult.Count == 4);
            queryResult = UKS.Query("four", "", "one");
            Debug.Assert(queryResult.Count == 1);
            queryResult = UKS.Query("four");
            Debug.Assert(queryResult.Count == 2);
            queryResult = UKS.Query("none");
            Debug.Assert(queryResult.Count == 1);


            //circular transitive references are set bu not followed
            Relationship r = UKS.AddStatement("one", "greaterThan", "three");
            queryResult = UKS.Query("three", "greaterThan");
            Debug.Assert(queryResult.Count == 3);
            queryResult = UKS.Query("one", "greaterThan");
            r.source.RemoveRelationship(r);
            queryResult = UKS.Query("three", "greaterThan");
            Debug.Assert(queryResult.Count == 3);

            queryResult = UKS.Query("one", "lessThan");
            Debug.Assert(queryResult.Count == 4);
            queryResult = UKS.Query("three", "", "one");
            Debug.Assert(queryResult.Count == 1);
            Debug.Assert(queryResult[0].reltype.Label == "greaterThan");
            queryResult = UKS.Query("one", "", "three");
            Debug.Assert(queryResult.Count == 1);


            //what is Transitive
            queryResult = UKS.Query(null, "hasProperty", "transitive");
            Debug.Assert(queryResult.Count == 3);

            UKS.AddStatement("suzie", "wear", "hat", null, null, "red");
            UKS.AddStatement("suzie", "wear", "hat", null, null, "green");
            queryResult = UKS.Query("suzie", "wear", "hat");
            Debug.Assert(queryResult.Count == 2);
            z1 = UKS.ResultsOfType(queryResult, "color");
            Debug.Assert(z1[0].Label == "green");
            UKS.AddStatement("suzie", "wear", "hat", null, null, "red");
            queryResult = UKS.Query("suzie", "wear", "hat");
            z1 = UKS.ResultsOfType(queryResult, "color");
            Debug.Assert(z1[0].Label == "red");  //the most-recently worn is red

            // a few cleanups
            UKS.AddStatement("eye", "is-a", "bodyPart");
            UKS.AddStatement("brain", "is-a", "bodyPart");
            UKS.AddStatement("finger", "is-a", "bodyPart");
            UKS.AddStatement("hand", "is-a", "bodyPart");
            UKS.AddStatement("fur", "is-a", "bodyPart");
            UKS.AddStatement("brown", "is-a", "color");
            UKS.AddStatement("makeInstance", "is-a", "Relationship");
            UKS.AddStatement("cannot", "is-a", "Relationship");
            UKS.AddStatement("you", "is-a", "person");
            UKS.AddStatement("jerry", "is-a", "person");
            UKS.AddStatement("old", "is-a", "condition");
            UKS.AddStatement("new", "is-a", "condition");
            UKS.AddStatement("hot", "is-a", "condition");
            UKS.AddStatement("lost", "is-a", "condition");
            UKS.AddStatement("crowd", "is-a", "size");
            UKS.AddStatement("metal", "is-a", "material");
            UKS.AddStatement("wood", "is-a", "material");
            UKS.AddStatement("plastic", "is-a", "material");
            UKS.AddStatement("bodyPart", "partOf", "body");
            UKS.AddStatement("location", "is-a", "Object");
            UKS.AddStatement("store", "is-a", "location");
            UKS.AddStatement("home", "is-a", "location");
            UKS.AddStatement("school", "is-a", "location");
            UKS.AddStatement("with", "is-a", "action");
            UKS.AddStatement("animal", "is-a", "livingThing");
            UKS.AddStatement("plant", "is-a", "livingThing");
            UKS.AddStatement("big", "is-a", "size");
            UKS.AddStatement("tall", "is-a", "size");
            UKS.AddStatement("short", "is-a", "size");
            UKS.AddStatement("small", "is-a", "size");
            UKS.AddStatement("hair", "is-a", "bodyPart");
            UKS.AddStatement("tail", "is-a", "bodyPart");
            UKS.AddStatement("dark", "is-a", "color");
            UKS.AddStatement("girl", "is-a", "person");


            //test to ensure that transitive can work on inherited relationships
            UKS.AddStatement("elephant", "is-a", "animal");
            UKS.AddStatement("elephant", "greaterThan", "person");
            queryResult = UKS.Query("elephant", "", "suzie");
            Debug.Assert(queryResult.Count == 1 && queryResult[0].relType.Label == "greaterThan");
            UKS.AddStatement("suzie", "greaterThan", "toy");
            queryResult = UKS.Query("elephant", "", "dumptruck");
            Debug.Assert(queryResult.Count == 1 && queryResult[0].relType.Label == "greaterThan");
            queryResult = UKS.Query("suzie", "", "elephant");
            Debug.Assert(queryResult.Count == 1 && queryResult[0].relType.Label == "lessThan");
            queryResult = UKS.Query("dumptruck", "", "elephant");
            Debug.Assert(queryResult.Count == 1 && queryResult[0].relType.Label == "lessThan");

            UKS.AddStatement("bill", "went", "store", null, null, "to")
                .AddClause(AppliesTo.source, UKS.AddStatement("", "and", "mary"))
                .AddClause(AppliesTo.source, UKS.AddStatement("", "and", "steve"))
                .AddClause(AppliesTo.type, UKS.AddStatement("", "on", "tuesday"));
            UKS.AddStatement("steve", "is-a", "person");
            UKS.AddStatement("store", "is-a", "location");
            UKS.AddStatement("baseball", "is-a", "location");
            UKS.AddStatement("beach", "is-a", "location");
            UKS.AddStatement("steve", "went", "baseball", null, null, "to")
                .AddClause(AppliesTo.source, UKS.AddStatement("", "and", "mary"));
            UKS.AddStatement("mary", "went", "beach", null, null, "to")
                .AddClause(AppliesTo.type, UKS.AddStatement("", "on", "yesterday"));

            //TODO: moving these after the addstatements causes failure
            UKS.GetOrAddThing("time", "Object");
            UKS.AddStatement("yesterday", "is-a", "time");
            UKS.AddStatement("tuesday", "is-a", "time");



            //"with" and "and" are a bit tricky because
            //if the data is bill and mary, if you search bill, it is a source, if you search mary it is a clause target, but you don't know which 


            queryResult = UKS.Query("", "went", "");
            Debug.Assert(queryResult.Count == 5);

            //who went to the store?  Tested 2 ways
            queryResult = UKS.Query("", "went", "store");
            Debug.Assert(queryResult.Count == 3);
            var thingList = UKS.GetThingsWithAncestor(queryResult, "person");
            Debug.Assert(thingList.Count == 6);

            List<Thing> thingList1 = UKS.ComplexQuery("", "went","store", "with|and", "person", out rawResults);
            Debug.Assert(thingList1.Count == 6);
            Debug.Assert(rawResults.Count == 3);

            //where did steve go?
            List<Thing> thingList2 = UKS.ComplexQuery("steve", "went", "", "with|and", "location", out rawResults);
            Debug.Assert(thingList2.Count == 2);
            Debug.Assert(rawResults.Count == 2);

            //test for clause searches
            //who went with suzie?
            List<Thing> thingList3 = UKS.ComplexQuery("suzie", "went", "", "with|and", "person", out rawResults);
            Debug.Assert(thingList3.Count == 1);
            Debug.Assert(rawResults.Count == 1);

            //where did mary go?
            thingList2 = UKS.ComplexQuery("mary", "went", "", "with|and", "location", out rawResults);
            Debug.Assert(thingList2.Count == 4);
            Debug.Assert(rawResults.Count == 4);

            thingList2 = UKS.ComplexQuery("mary", "went", "store", "with|and", "time", out rawResults);
            Debug.Assert(thingList2.Count == 1);
            Debug.Assert(rawResults.Count == 3);


            //who went with mary
            thingList2 = UKS.ComplexQuery("mary", "went", "", "with|and", "person", out rawResults);
            Debug.Assert(thingList2.Count == 3);
            Debug.Assert(rawResults.Count == 4);


            //who -> hasAncestor(person);

            //who went with mary  (with is commutative)

            //who went with suzie




            //Ideas . TODO
            //antecedents  suzie is a girl. tell me about her?
            //end of transitive:  (biggest,smallest)
            //improved error correction: fido is big, fido is small
            //complex conditional
            //search into clauses
            //negatives
            //a is greater than b is equivalent to a greater than b  ditch is's or make "is greater than" a single relationship
            //possessives  
            //abiguity (multiple meanings for words
            return;


        }
  

        List<Relationship> GetRelationships(Relationship clause, IList<Relationship> searchSpace = null)
        {
            //exhaustive UKS search for clauses containing "with" or "and"
            if (searchSpace == null)
            {
                searchSpace = GetAllRelationships();
            }
            List<Relationship> queryResult = new();
            foreach (Relationship rv in searchSpace)
            {
                foreach (ClauseType cv in rv.clauses)
                {
                    if (cv.clause == clause)
                    {
                        if (!queryResult.Contains(rv))
                            queryResult.Add(rv);
                        goto Added;
                    }
                }
            Added:
                continue;
            }
            return queryResult;
        }

        List<Relationship> GetClauses(IList<string> relTypes, IList<Relationship> searchSpace)
        {
            List<Relationship> queryResult = new();
            foreach (Relationship rv in searchSpace)
            {
                foreach (ClauseType cv in rv.clauses)
                {
                    foreach (string s in relTypes)
                        if (cv.clause.reltype.Label == s)
                        {
                            if (!queryResult.Contains(rv))
                                queryResult.Add(rv);
                            goto Added;
                        }
                }
            Added:
                continue;
            }
            return queryResult;
        }


        private List<Relationship> GetAllRelationships()
        {
            List<Relationship> searchSpace = new();
            List<Thing> theUKS = UKS.GetTheUKS();
            foreach (Thing tv in theUKS)
            {
                foreach (Relationship rv in tv.Relationships)
                {
                    if (!searchSpace.Contains(rv))
                        searchSpace.Add(rv);
                }
            }

            return searchSpace;
        }


        private void AddRelationshipToCurrentThought(Relationship theRelationship)
        {
            //add the new relatinoship to the current thought
            Thing thought = UKS.GetOrAddThing("thought", "Object");
            Thing currentThought = UKS.GetOrAddThing("currentThought", "thought");
            currentThought.AddParent(thought);
            currentThought.RelationshipsWriteable.Insert(0, theRelationship);
            currentThought.SetFired();
            if (currentThought.RelationshipsWriteable.Count > 7)
                currentThought.RelationshipsWriteable.RemoveAt(currentThought.RelationshipsWriteable.Count - 1);
        }


        //the following can be used to massage public data to be different in the xml file
        //delete if not needed
        public override void SetUpBeforeSave()
        {
        }
        public override void SetUpAfterLoad()
        {
        }

        public override void UKSInitializedNotification()
        {
            Initialize();
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