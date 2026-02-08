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
//
// Copyright (c) FutureAI. All rights reserved.  
// Contains confidential and  proprietary information and programs which may not be distributed without a separate license
//  

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BrainSimulator.Modules
{
    public partial class ModuleOnlineInfoDlg : ModuleBaseDlg
    {
        public ModuleOnlineInfoDlg()
        {
            InitializeComponent();
        }

        string prevOutput;
        public override bool Draw(bool checkDrawTimer)
        {
            if (!base.Draw(checkDrawTimer)) return false;
            //this has a timer so that no matter how often you might call draw, the dialog
            //only updates 10x per second

            //use a line like this to gain access to the parent's public variables
            //ModuleEmpty parent = (ModuleEmpty)base.ParentModule;

            //here are some other possibly-useful items
            //theCanvas.Children.Clear();
            //Point windowSize = new Point(theCanvas.ActualWidth, theCanvas.ActualHeight);
            //Point windowCenter = new Point(windowSize.X / 2, windowSize.Y / 2);
            //float scale = (float)Math.Min(windowSize.X, windowSize.Y) / 12;
            //if (scale == 0) return false;

            ModuleOnlineInfo mcn = (ModuleOnlineInfo)base.ParentModule;
            if (prevOutput != mcn.Output)
            {

                //txtOutput.Text += "\n\n" + mcn.Output;
                txtOutput.Text = mcn.Output;
                prevOutput = mcn.Output;
            }
            return true;
        }

        private void TheCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }

        private void txtInput_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string txt = txtInput.Text;
                ModuleOnlineInfo mcn = (ModuleOnlineInfo)base.ParentModule;
                string currentLink = ((ComboBoxItem)linkSelection.SelectedItem).Content.ToString();
                ModuleOnlineInfo.QueryType qType = ModuleOnlineInfo.QueryType.isa;
                string currentSearch = ((ComboBoxItem)comboSelection.SelectedItem).Content.ToString();
                switch (currentLink)
                {
                    case "is-a":
                        qType = ModuleOnlineInfo.QueryType.isa;
                        break;
                    case "hasa":
                        qType = ModuleOnlineInfo.QueryType.hasa;
                        break;
                }

                switch (currentSearch)
                {
                    case "ChatGPT":
                        if (txt.EndsWith("?"))
                            mcn.GetChatGPTResult(txt, ModuleOnlineInfo.QueryType.general);
                        else if (txt.StartsWith("list some"))
                            mcn.GetChatGPTResult(txt.Substring(10), ModuleOnlineInfo.QueryType.list);
                        else if (txt.StartsWith("count some"))
                            mcn.GetChatGPTResult(txt.Substring(11), ModuleOnlineInfo.QueryType.listCount);
                        else if (txt.EndsWith("can"))
                            mcn.GetChatGPTResult(txt.Substring(0,txt.Length-3), ModuleOnlineInfo.QueryType.can);
                        else
                        {
                            mcn.GetChatGPTResult(txt, qType);
                            //Thread.Sleep(1000);
                            //mcn.GetChatGPTData(txt, ModuleOnlineInfo.QueryType.hasa);
                            //Thread.Sleep(1000);
                            //mcn.GetChatGPTData(txt, ModuleOnlineInfo.QueryType.can);
                            //Thread.Sleep(1000);
                            //mcn.GetChatGPTData(txt, ModuleOnlineInfo.QueryType.count);
                            //Thread.Sleep(1000);
                            //mcn.GetChatGPTData(txt, ModuleOnlineInfo.QueryType.list);
                        }
                        break;
                    case "ConceptNet":
                        mcn.GetConceptNetData(txt);
                        break;
                    case "WikiData":
                        mcn.GetWikidataData(txt, "subclass of");
                        break;
                    case "Wiktionary":
                        mcn.GetWiktionaryData(txt);
                        break;
                    case "Free Dictionary":
                        mcn.GetFreeDictionaryAPIData(txt);
                        break;
                    case "Webster's Elementary":
                        mcn.GetWebstersDictionaryAPIData(txt);
                        break;
                    case "Kid's Definition":
                        mcn.GetKidsDefinition(txt);
                        break;
                    case "CSKG":
                        mcn.GetCSKGData(txt);
                        break;
                    case "Oxford Word List":
                        mcn.SetupWordList2(txt);
                        break;
                }
            }
        }
        private void comboSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cb)
            {
                int i = cb.SelectedIndex;
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            txtOutput.Text = "";
        }
    }
}