//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Shapes;
using System.Xml.Serialization;

namespace BrainSimulator
{

    public partial class ModuleView
    {
        int firstNeuron = 0;
        string label;
        string moduleTypeStr;
        int color;
        Modules.ModuleBase theModule = null;
        int width = 0;
        int height = 0;

        [XmlIgnore] //used when displaying the module at small scales
        public System.Windows.Media.Imaging.WriteableBitmap bitmap = null;

        public ModuleView(int firstNeuron1, int width, int height, string theLabel, string theModuleTypeStr, int theColor)
        {
            int index = theModuleTypeStr.IndexOf("(");
            if (index != -1)
                theModuleTypeStr = theModuleTypeStr.Substring(0, index);


            Label = theLabel;
            ModuleTypeStr = theModuleTypeStr;
            color = theColor;

            Type t = Type.GetType("BrainSimulator.Modules." + theModuleTypeStr);
            theModule = (Modules.ModuleBase)Activator.CreateInstance(t);
        }

        public string Label { get => label.StartsWith("Module") ? label.Replace("Module", "") : label; set => label = value; }

        public string ModuleTypeStr { get => moduleTypeStr; set => moduleTypeStr = value; }

        public Modules.ModuleBase TheModule { get => theModule; set => theModule = value; }
    }
}