//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UKS;

namespace BrainSimulator.Modules
{
    public partial class ModuleUKSClauseDlg : ModuleBaseDlg
    {
        // Constructor of the ModuleUKSStatement dialog
        public ModuleUKSClauseDlg()
        {
            InitializeComponent();
        }

        // Draw gets called to draw the dialog when it needs refreshing
        public override bool Draw(bool checkDrawTimer)
        {
            if (!base.Draw(checkDrawTimer)) return false;

            return true;
        }

        // BtnAddRelationship_Click is called the AddRelationship button is clicked
        private void BtnAddRelationship_Click(object sender, RoutedEventArgs e)
        {
            string newThing = sourceText.Text;
            string targetThing = targetText.Text;
            string relationType = relationshipText.Text;
            string clauseType = clauseTypeText.Text;

            if (!CheckAddRelationshipFieldsFilled()) return;

            ModuleUKSClause UKSClause = (ModuleUKSClause)ParentModule;
            Relationship r1 = UKSClause.AddRelationship(newThing, targetThing, relationType);

            Thing theClauseType = UKSClause.GetClauseType(clauseType);
            string newThing2 = sourceText2.Text;
            string targetThing2 = targetText2.Text;
            string relationType2 = relationshipText2.Text;
            Relationship r2 = UKSClause.AddRelationship(newThing2, targetThing2, relationType2);
            r1.AddClause(theClauseType, r2);
        }

        // thingText_TextChanged is called when the thing textbox changes
        private void Text_TextChanged(object sender, TextChangedEventArgs e)
        {
            CheckThingExistence(sender);
        }

        //copied from UKSStatementDlg.cs
        private bool CheckThingExistence(object sender)
        {
            if (sender is TextBox tb)
            {
                string text = tb.Text.Trim();

                if (text == "" && !tb.Name.Contains("arget"))
                {
                    tb.Background = new SolidColorBrush(Colors.Pink);
                    SetError("Source and type cannot be empty");
                    return false;
                }
                List<Thing> tl = ModuleUKSStatement.ThingListFromString(text);
                if (tl == null || tl.Count == 0)
                {
                    tb.Background = new SolidColorBrush(Colors.LemonChiffon);
                    SetError("");
                    return false;
                }
                tb.Background = new SolidColorBrush(Colors.White);
                SetError("");
                return true;
            }
            return false;
        }



        // Check for parent existence and set background color of the textbox and the error message accordingly.
        private bool CheckAddRelationshipFieldsFilled()
        {
            SetError("");
            ModuleUKSClause UKSEvent = (ModuleUKSClause)ParentModule;

            if (sourceText.Text == "")
            {
                SetError("Source not provided");
                return false;
            }
            if (relationshipText.Text == "")
            {
                SetError("Type not provided");
                return false;
            }
            if (clauseTypeText.Text == "")
            {
                SetError("Clause type not provided");
                return false;
            }
            if (sourceText2.Text == "")
            {
                SetError("Clause source not provided");
                return false;
            }
            if (relationshipText2.Text == "")
            {
                SetError("Clause type not provided");
                return false;
            }
            return true;
        }


        // SetError turns the error text yellow and sets the message, or clears the color and the text.
        private void SetError(string message)
        {
            if (string.IsNullOrEmpty(message))
                errorText.Background = new SolidColorBrush(Colors.Gray);
            else
                errorText.Background = new SolidColorBrush(Colors.Yellow);

            errorText.Content = message;
        }
    }
}
