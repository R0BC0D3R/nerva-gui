using System;
using Eto.Forms;
using Nerva.Desktop.Helpers;

namespace Nerva.Desktop.Content.Wizard
{
    public class IntroContent : WizardContent
    {
        private Control content;

        public override string Title => "NERVA Desktop Setup Wizard";

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
                        new Label { Text = "Welcome to NERVA" },
                        new Label { Text = "   " },
                        new Label { Text = "This wizard will guide you through all the steps required to get up and running." },
                        new Label { Text = "   " },
                        new Label { Text = "Click 'Next' to continue" },
                        new Label { Text = "   " },
                    }
                };

                
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleException("IntroContent.CreateContent", ex, true);
            }

            return layout;
        }
    }
}