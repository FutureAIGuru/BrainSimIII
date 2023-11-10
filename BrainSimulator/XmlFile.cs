using BrainSimulator.Modules;
using System;
using System.Text;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml;
using System.Xml.Serialization;

namespace BrainSimulator
{
    public class XmlFile
    {
        //this is the set of moduletypes that the xml serializer will save
        static public Type[] GetModuleTypes()
        {
            Type[] listOfBs = (from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
                               from assemblyType in domainAssembly.GetTypes()
                               where assemblyType.IsSubclassOf(typeof(ModuleBase))
                               //                               where typeof(ModuleBase).IsAssignableFrom(assemblyType)
                               select assemblyType).ToArray();
            List<Type> list = new List<Type>();
            for (int i = 0; i < listOfBs.Length; i++)
                list.Add(listOfBs[i]);
            // Add classes so XML saving works
            list.Add(typeof(Angle));
            list.Add(typeof(Cone));
            list.Add(typeof(CornerTwoD));
            list.Add(typeof(Cube));
            list.Add(typeof(Cylinder));
            list.Add(typeof(EnvironmentObject));
            list.Add(typeof(HSLColor));
            list.Add(typeof(KnownArea));
            list.Add(typeof(Point3DPlus));
            list.Add(typeof(PointPlus));
            list.Add(typeof(PointTwoD));
            list.Add(typeof(Polar));
            list.Add(typeof(SegmentTwoD));
            list.Add(typeof(Sphere));
            list.Add(typeof(UnknownArea));
            list.Add(typeof(Wall));
            list.Add(typeof(Triangle2D));
            list.Add(typeof(ModuleOnlineInfo.KidsWord));
            return list.ToArray();
        }

        public static void RemoveFileFromMRUList(string filePath)
        {
            StringCollection MRUList = (StringCollection)Properties.Settings.Default["MRUList"];
            if (MRUList == null)
                MRUList = new StringCollection();
            MRUList.Remove(filePath); //remove it if it's already there
            Properties.Settings.Default["MRUList"] = MRUList;
            Properties.Settings.Default.Save();
        }

        public static bool CanWriteTo(string fileName)
        {
            return CanWriteTo(fileName, out _);
        }
        public static bool CanWriteTo(string fileName, out string message)
        {
            FileStream file1;
            message = "";
            if (File.Exists(fileName))
            {
                try
                {
                    file1 = File.Open(fileName, FileMode.Open);
                    file1.Close();
                    return true;
                }
                catch (Exception e)
                {
                    message = e.Message;
                    return false;
                }
            }
            return true;

        }
    }
}
