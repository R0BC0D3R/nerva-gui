using System;
using Eto.Forms;
using Nerva.Desktop.Helpers;

namespace Nerva.Desktop.Content.Wizard
{
    public class FinishedContent : WizardContent
    {
        private Control content;

        public override string Title => "NERVA Desktop Setup Wizard - Finished";

        public override Control Content
        {
            get
            {
                if (content == null)
                    content = CreateContent();

                return content;
            }
        }

        public override Control CreateContent()
        {
            StackLayout layout = null;

            try
            {                
                layout = new StackLayout
                {
                    Orientation = Orientation.Vertical,
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    VerticalContentAlignment = VerticalAlignment.Stretch,
                    Items =
                    {
                        new Label { Text = "Press 'Finish' to start using NERVA Desktop." },
                        new Label { Text = "   " },
                        new Label { Text = "Look in Wallet menu to create/restore/open wallet." },                        
                        new Label { Text = "   " },
                        new Label { Text = "Set your mining preferences under File > Preferences." },                        
                        new Label { Text = "   " },
                        new Label { Text = "If you require help, please check the help menu for some useful links." },
                        new Label { Text = "   " },
                        new StackLayoutItem(null, true),
                    }
                };
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleException("FinishedContent.CreateContent", ex, true);
            }

            return layout;
        }
    
        public override void OnAssignContent()
        {
            Parent.AllowNavigation(true);
        }

        public override void OnNext()
        {
            Parent.WizardEnd = true;
            Parent.Close();
        }
    }
}