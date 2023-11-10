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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Xml;
using System.Web;
using System;
using System.Diagnostics;
using System.IO;
using System.Xml.Linq;

namespace BrainSimulator.Modules
{
    public class ModuleWikidata : ModuleBase
    {
        // Any public variable you create here will automatically be saved and restored  
        // with the network unless you precede it with the [XmlIgnore] directive
        // [XmlIgnore] 
        // public theStatus = 1;
        private static string urlBegin = @"https://query.wikidata.org/sparql?query=";


        // Set size parameters as needed in the constructor
        // Set max to be -1 if unlimited
        public ModuleWikidata()
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

            UpdateDialog();
        }

        public void AddObjectPropertyToMentalModel(string lbl, string propValue, string propName)
        {
            GetUKS();

            List<string> stringList = new List<string>(new string[] { "color", "col" });
            List<string> shapeList = new List<string>(new string[] { "shape", "shp" });
            List<string> sizeList = new List<string>(new string[] { "size", "siz" });
            List<string> angleList = new List<string>(new string[] { "angle", "ang" });
            List<string> areaList = new List<string>(new string[] { "area", "are" });
            List<string> centerList = new List<string>(new string[] { "center", "cen" });
            List<List<string>> listAll = new List<List<string>>();
            listAll.Add(stringList);
            listAll.Add(shapeList);
            listAll.Add(sizeList);
            listAll.Add(angleList);
            listAll.Add(areaList);
            listAll.Add(centerList);

            Thing mentalModelRoot = UKS.GetOrAddThing("MentalModel", "Thing");
            Thing objectRoot = UKS.GetOrAddThing("Object", "Thing");
            Thing propertyRoot = UKS.GetOrAddThing("Property", "Thing");



            string lbl0 = "";
            string lbl1 = "";

            lbl = lbl.Trim();
            if (lbl == "basketball ball")
            {
                lbl0 = "basketball";
                lbl1 = "basketball-ball";
            }
            Thing searchObject = UKS.Labeled(lbl0);
            Thing propProp = UKS.Labeled(propName);
            Thing propObject = new Thing();
            if (propProp == null)
            {
                propProp = UKS.GetOrAddThing(propName, propertyRoot);
            }
            if (propObject.Label == "")
            {
                propObject = UKS.GetOrAddThing(propProp.Label, objectRoot);
            }



            bool firstTime = true;
            foreach (Relationship s in searchObject.Relationships)
            {
                foreach (List<string> ls in listAll)
                {
                    if (ls[0] == propObject.Label)
                    {
                        int count = 0;
                        Thing property = new Thing();
                        foreach (Thing p in UKS.GetOrAddThing(ls[1], propertyRoot).Children)
                        {
                            Thing reff = UKS.GetOrAddThing(lbl0, objectRoot);
                            foreach (Relationship r in reff.Relationships)
                            {
                                if ((r.T as Thing).Label == p.Label && firstTime)
                                {
                                    firstTime = false;
                                    Thing pValue = UKS.GetOrAddThing(propValue, propObject);
                                    pValue.AddRelationWithoutDuplicate(p);

                                    string itemName = pValue.Label;
                                    string theWord = "w" + char.ToUpper(itemName[0]) + itemName.Substring(1);
                                    Thing response = UKS.GetOrAddThing("CurrentVerbalResponse", "Attention");
                                    response.AddRelationship(UKS.GetOrAddThing(theWord, UKS.Labeled("Word"),itemName));
                                }
                            }
                            count++;
                        }
                    }
                }

            }

        }
        //This method gets all the properties or subproperties of a property propName from a Thing
        // with name propName and wikidata query number value numberOfName associated with propName
        private async void GetPropertiesFromURL(string numberOfName, string name, string propName)
        {
            GetUKS();
            propName = propName.ToLower();
            Thing search = UKS.GetOrAddThing("Search", "Attention");
            Thing item = UKS.GetOrAddThing(name, search);
            Network.httpClientBusy = true;
            var urlBegin = @"https://query.wikidata.org/sparql?query=";
            var url = @"SELECT ?wdLabel ?ps_Label ?wdpqLabel ?pq_Label " +
                      @"{VALUES(?company) { (wd:" + numberOfName + @")} " +
                      @"?company ?p ?statement. ?statement ?ps ?ps_. ?wd " +
                      @"wikibase:claim ?p. ?wd wikibase:statementProperty " +
                      @"?ps.OPTIONAL{?statement ?pq ?pq_. ?wdpq wikibase:qualifier " +
                      @"?pq .} SERVICE wikibase:label { bd:serviceParam wikibase:language ""en"" }} " +
                      @"ORDER BY ?wd ?statement ?ps_";
            url = urlBegin + Uri.EscapeUriString(url);
            var myClient = new HttpClient();
            myClient.DefaultRequestHeaders.Add("User-Agent", "c# program");
            var responseURL = await myClient.GetAsync(url);
            var propertyURL = await responseURL.Content.ReadAsStringAsync();
            XmlDocument propertyDoc = new XmlDocument();
            string propertyURLResult = propertyURL.ToString();
            propertyDoc.LoadXml(propertyURLResult);

            Thing props = UKS.GetOrAddThing("Properties", item);
            if (propName != "")
            {
                Thing prop = UKS.GetOrAddThing(propName, props);
                var propertyToCheck = prop.Label;

                var docPropertyValues = propertyDoc.GetElementsByTagName("result");
                string[][] end = new string[docPropertyValues.Count][];
                int docCount = 0;
                foreach (XmlElement xn in docPropertyValues)
                {
                    bool found = false;
                    string[] fn = new string[xn.ChildNodes.Count];
                    for (int i = 0; i < xn.ChildNodes.Count; i++)
                    {
                        if (xn.ChildNodes[i].InnerText == propertyToCheck)
                        {
                            found = true;
                        }
                    }
                    if (found)
                    {
                        for (int i = 0; i < xn.ChildNodes.Count; i++)
                        {
                            if (xn.ChildNodes[i].InnerText != propertyToCheck)
                            {

                                if (i == 1) // comment this out to get all the other properties
                                    fn[i] = (xn.ChildNodes[i].InnerText.ToString());
                            }
                        }
                    }
                    if (fn != null)
                    {
                        end[docCount] = fn;
                    }
                    docCount++;
                }
                string propString = "";
                //TextBoxWiki.Text = "";
                for (int i = 0; i < docPropertyValues.Count; i++)
                {
                    foreach (var text in end[i])
                    {
                        if (text != null)
                        {
                            Debug.WriteLine(">>>"+text);
                            //UKS.GetOrAddThing(text, prop);
                            //AddObjectPropertyToMentalModel(name, text, prop.Label);
                            //TextBoxWiki.Text += text + '\n';
                        }
                    }
                }
            }
            else
            {
                var docPropertyValues = propertyDoc.GetElementsByTagName("result");
                string[][] end = new string[docPropertyValues.Count][];
                int docCount = 0;
                foreach (XmlElement xn in docPropertyValues)
                {

                    bool found = false;
                    string[] fn = new string[xn.ChildNodes.Count];
                    Thing prop = new Thing();
                    for (int i = 0; i < xn.ChildNodes.Count; i++)
                    {
                        if (i == 0)
                        {
                            prop = UKS.GetOrAddThing(xn.ChildNodes[i].InnerText, props);
                        }
                        else
                        {
                            fn[i] = (xn.ChildNodes[i].InnerText.ToString());
                            UKS.GetOrAddThing(xn.ChildNodes[i].InnerText, prop);
                        }
                    }

                    if (fn != null)
                    {
                        end[docCount] = fn;
                    }
                    docCount++;
                }

                string propString = "";
                //TextBoxWiki.Text = "";
                for (int i = 0; i < docPropertyValues.Count; i++)
                {

                    foreach (var text in end[i])
                    {
                        if (text != null)
                        {
                            UKS.GetOrAddThing(text, props);
                            AddObjectPropertyToMentalModel(name, text, props.Label);
                            //TextBoxWiki.Text += text + '\n';
                        }
                    }
                }
            }
        }

        //Generates a list of the properties or subproperties of the queried property of 
        // name itemProp for the Thing with name itemString.
        public List<string> ReturnListOfProperties(string itemString, string itemProp)
        {
            GetUKS();
            Thing item = UKS.GetOrAddThing(itemString, "Search");
            Thing props = UKS.GetOrAddThing("Properties", item);

            List<string> listOfProperties = new List<string>();
            if (itemProp == "")
            {
                foreach (Thing parentProp in props.Children)
                {
                    foreach (Thing childProp in parentProp.Children)
                    {
                        listOfProperties.Add(parentProp.Label + " :: " + childProp.Label);
                    }
                }
            }
            else
            {
                Thing prop = UKS.GetOrAddThing(itemProp, props);
                foreach (Thing t in prop.Children)
                {

                    listOfProperties.Add(t.Label);
                }
            }

            return listOfProperties;
        }

        //This method places all properties, or subproperties of the 
        // queried property with name itemProp, from the Thing with name
        // itemName into the UKS
        public Thing SetItemInUKS(string itemName, string itemProp)
        {
            GetUKS();
            Thing search = UKS.GetOrAddThing("Search", "Attention");
            Thing item = UKS.GetOrAddThing(itemName, search);
            Thing props = UKS.GetOrAddThing("Properties", item);
            if (itemProp != "")
            {
                Thing prop = UKS.GetOrAddThing(itemProp, props);
            }
            else
            {

            }

            return (item);
        }

        //This method rerieves the wikidata query number value from wikidata query
        // for the Thing item and passes that value to GetPropertiesFromURL along
        // with the property query named prop
        public async void GetItemAndPropsFromURL(Thing item, string prop)
        {
            GetUKS();
            var itemLabel = item.Label;
            if (itemLabel == "basketball") itemLabel = "basketball ball";
            try
            {
                //var thingToCheck = ItemSearchBox.Text;
                Network.httpClientBusy = true;
                var url = "http://" + "www.wikidata.org/w/api.php?action=wbsearchentities&search=" +
                          itemLabel +
                          "&language=en&format=xml";
                var waitURL = await Network.theHttpClient.GetAsync(url);

                if (waitURL != null)
                {
                    var contentFromURL = waitURL.Content.ReadAsStringAsync();
                    XmlDocument xmlItemDoc = new XmlDocument();
                    xmlItemDoc.LoadXml(contentFromURL.Result.ToString());
                    var xmlItemDocValue = xmlItemDoc.GetElementsByTagName("entity");
                    var xmlItemDocValueFirst = xmlItemDocValue[0];
                    if (xmlItemDocValueFirst != null)
                    {
                        var nameLabel = xmlItemDocValueFirst.Attributes[0];
                        //TextBoxWiki.Text = something.Result.ToString();
                        string numberValueOfName = nameLabel.InnerXml;
                        Network.httpClientBusy = false;
                        GetPropertiesFromURL(numberValueOfName, itemLabel, prop);///
                    }
                }
            }
            catch { }

        }

        public static async Task<string> GetColorDescription(string colorName)
        {
            string ColorDescriptionQuery = $@"SELECT ?description WITH {{
                                            SELECT distinct ?item WHERE {{
                                                ?item ?label ""{colorName}""@en.
                                                ?item wdt:P31 wd:Q1075. 
                                                SERVICE wikibase:label {{ bd:serviceParam wikibase:language ""en"". }}
                                            }}
                                        }} as %i
                                        WHERE {{ INCLUDE %i
                                            SERVICE wikibase:label {{
                                                bd:serviceParam wikibase:language ""en"".
                                                ?item schema:description ?description .
                                            }}.
                                            OPTIONAL {{?item wdt:P465 ?hex.}}
                                        }}";
            var url = ModuleWikidata.urlBegin + Uri.EscapeUriString(ColorDescriptionQuery);
            var myClient = new HttpClient();
            myClient.DefaultRequestHeaders.Add("User-Agent", "c# program");
            var responseURL = await myClient.GetAsync(url);
            var propertyURL = await responseURL.Content.ReadAsStringAsync();
            //Debug.WriteLine(propertyURL);
            StringReader stringReader = new StringReader(propertyURL.ToString());
            XmlDocument doc = new XmlDocument();
            doc.Load(stringReader);
            var elements = doc.GetElementsByTagName("binding");
            if (elements.Count > 0) return elements[0].InnerText;
            else return "";
        }

        public static async Task<List<string>> GetLabelsByColorName(string colorName)
        {
            string ColorDescriptionQuery = $@"SELECT DISTINCT ?label
    WITH {{
    SELECT distinct ?color WHERE {{  
        ?color ?label ""{colorName}""@en.
        ?color wdt:P31 wd:Q1075. 
        SERVICE wikibase:label {{ bd:serviceParam wikibase:language ""en"". }}    
    }}
    }} as %i  
WHERE {{
    INCLUDE %i
    ?item wdt:P462 ?color.
    ?item rdfs:label ?label. 
    ?item wdt:P462 ?itemColor
    FILTER( lang(?label) = ""en"")
    FILTER( strlen(?label) <= 10)
    BIND(MD5(CONCAT(STR(RAND()), STR(?label), STR(?item), STR(RAND()))) AS ?random)
}}
ORDER BY ?random
LIMIT 5";
            var url = urlBegin + Uri.EscapeUriString(ColorDescriptionQuery);
            var myClient = new HttpClient();
            myClient.DefaultRequestHeaders.Add("User-Agent", "c# program");
            var responseURL = await myClient.GetAsync(url);
            var propertyURL = await responseURL.Content.ReadAsStringAsync();
            //Debug.WriteLine(propertyURL);
            StringReader stringReader = new StringReader(propertyURL.ToString());
            XmlDocument doc = new XmlDocument();
            doc.Load(stringReader);
            var elements = doc.GetElementsByTagName("binding");
            List<string> results = new();
            foreach (XmlNode element in elements)
            {
                results.Add(element.InnerText);
                Debug.WriteLine(element.InnerText);
            }
            return results;
        }

        public static async Task<string> GetColorFromHex(List<string> hex)
        {
            string colorFromHex = $@"SELECT DISTINCT ?label WHERE {{
  ?color wdt:P31/wdt:P279* wd:Q1075.
  ?color wdt:P465 ?rgb.
  ?color rdfs:label ?label.
  FILTER (lang(?label) = ""en"" && (regex(?rgb, ""{hex[0]}"")";
            for (int i = 1; i < hex.Count; i++) colorFromHex += $@" || regex(?rgb, ""{hex[i]}"")";
            colorFromHex += "))}";
            var url = ModuleWikidata.urlBegin + Uri.EscapeUriString(colorFromHex);
            url = url.Replace("&", "%26");
            var myClient = new HttpClient();
            myClient.DefaultRequestHeaders.Add("User-Agent", "c# program");
            var responseURL = await myClient.GetAsync(url);
            var propertyURL = await responseURL.Content.ReadAsStringAsync();
            StringReader stringReader = new StringReader(propertyURL.ToString());
            XmlDocument doc = new XmlDocument();
            doc.Load(stringReader);
            var elements = doc.GetElementsByTagName("binding");
            if (elements.Count == 0) return "";
            return elements[0].InnerText;
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