/*
 * Brain Simulator Through
 *
 * Copyright (c) 2026 Charles Simon
 *
 * This file is part of Brain Simulator Through and is licensed under
 * the MIT License. You may use, copy, modify, merge, publish, distribute,
 * sublicense, and/or sell copies of this software under the terms of
 * the MIT License.
 *
 * See the LICENSE file in the project root for full license information.
 */
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
            //list.Add(typeof(Cone));
            //list.Add(typeof(CornerTwoD));
            //list.Add(typeof(Cube));
            //list.Add(typeof(Cylinder));
            //list.Add(typeof(DisplayParams));
            //list.Add(typeof(EnvironmentObject));
            //list.Add(typeof(HSLColor));
            //list.Add(typeof(KnownArea));
            list.Add(typeof(Point3DPlus));
            list.Add(typeof(PointPlus));
            //list.Add(typeof(PointTwoD));
            //list.Add(typeof(Polar));
            //list.Add(typeof(SegmentTwoD));
            //list.Add(typeof(Sphere));
            //list.Add(typeof(UnknownArea));
            //list.Add(typeof(Wall));
            //list.Add(typeof(Triangle2D));
            //list.Add(typeof(ModuleOnlineInfo.KidsWord));
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

        public static bool Load(string fileName)
        {
            Stream file;
            try
            {
                file = File.Open(fileName, FileMode.Open, FileAccess.Read);
            }
            catch (Exception e)
            {
                MessageBox.Show("Could not open file because: " + e.Message);
                RemoveFileFromMRUList(fileName);
                return false;
            }

            // first check if the required start tag is present in the file...
            byte[] buffer = new byte[200];
            file.Read(buffer, 0, 200);
            string line = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
            if (line.Contains("BrainSim3Data"))
            {
                file.Seek(0, SeekOrigin.Begin);
            }
            else
            {
                file.Close();
                MessageBox.Show("File is not a valid Brain Simulator III XML file.");
                return false;
            }

            XmlSerializer reader1 = new XmlSerializer(typeof(BrainSim3Data), GetModuleTypes());
            try
            {
                MainWindow.BrainSim3Data = (BrainSim3Data)reader1.Deserialize(file);
            }
            catch (Exception e)
            {
                file.Close();
                MessageBox.Show("Network file load failed, a blank network will be opened. \r\n\r\n"+e.InnerException,"File Load Error",
                    MessageBoxButton.OK,MessageBoxImage.Error,MessageBoxResult.OK,MessageBoxOptions.DefaultDesktopOnly);
                return false;
            }

            return true;
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

        public static bool Save(string fileName)
        {
            Stream file;
            string tempFile = "";

            if (!CanWriteTo(fileName, out string message))
            {
                MessageBox.Show("Could not save file because: " + message);
                return false;
            }

            // tempFile = System.IO.Path.GetTempFileName();
            file = File.Create(fileName);
            Type[] extraTypes = GetModuleTypes();
            try
            {
                XmlSerializer writer = new XmlSerializer(typeof(BrainSim3Data), extraTypes);
                writer.Serialize(file, MainWindow.BrainSim3Data);
            }
            catch (Exception e)
            {
                if (e.InnerException != null)
                    MessageBox.Show("Xml file write failed because: " + e.InnerException.Message);
                else
                    MessageBox.Show("Xml file write failed because: " + e.Message);
                //MainWindow.thisWindow.SetProgress(100,"");
                return false;
            }
            file.Close();
            
            return true;
        }
    }
}
