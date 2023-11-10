//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

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

namespace BrainSimulator.Modules
{
    public partial class ModuleWikidataDlg : ModuleBaseDlg
    {
        public ModuleWikidataDlg()
        {
            InitializeComponent();
        }


        public override bool Draw(bool checkDrawTimer)
        {
            if (!base.Draw(checkDrawTimer)) return false;
            //this has a timer so that no matter how often you might call draw, the dialog
            //only updates 10x per second

            //use a line like this to gain access to the parent's public variables
            //ModuleEmpty parent = (ModuleEmpty)base.ParentModule;

            //here are some other possibly-useful items
            //theGrid.Children.Clear();
            //Point windowSize = new Point(theGrid.ActualWidth, theGrid.ActualHeight);
            //Point windowCenter = new Point(windowSize.X / 2, windowSize.Y / 2);
            //float scale = (float)Math.Min(windowSize.X, windowSize.Y) / 12;
            //if (scale == 0) return false;

            return true;
        }

        private void TheGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            //TextBoxWiki.Text = 
        }


        //This is the property textbox which will display all properties, or all
        // the subproperties of the queried property, for the Thing called Item.
        //If there the item search box is empty, return an error.
        private void UpdateMainTextBox()
        {
            ModuleWikidata parent = (ModuleWikidata)base.ParentModule;
            if (ItemSearchBox.Text != "")
            {
                List<string> listOfProperties = parent.ReturnListOfProperties(ItemSearchBox.Text, PropertySearchBox.Text);
                TextBoxWiki.Text = "";
                foreach (string t in listOfProperties)
                {
                    TextBoxWiki.Text += t + '\n';
                }
            }
            else
            {
                TextBoxWiki.Text = "Please, enter an item to search for." + '\n' +
                                   "Enter a property to look at all the subproperties of that property.";
            }
        }

        //This button form can be called to search for the properties or queired property of 
        // an item.  IF no item name is provided, an error is displayed.
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (ItemSearchBox.Text != "")
            {
                ModuleWikidata parent = (ModuleWikidata)base.ParentModule;
                Thing item = parent.SetItemInUKS(ItemSearchBox.Text, PropertySearchBox.Text);
                parent.GetItemAndPropsFromURL(item, PropertySearchBox.Text);
                UpdateMainTextBox();
            }
            else
            {
                UpdateMainTextBox();
            }

        }
    }
}