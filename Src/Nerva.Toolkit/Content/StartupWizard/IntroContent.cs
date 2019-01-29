using Eto.Drawing;
using Eto.Forms;

namespace Nerva.Toolkit.Content.Wizard
{
    public class IntroContent : WizardContent
    {
        private Control content;

        public override string Title => "NERVA Toolkit Wizard";

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
            return new StackLayout
            {
                Orientation = Orientation.Vertical,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                Items = 
                {
                    new Label { Text = "Welcome to NERVA" },
                    new Label { Text = "   " },
                    new Label { Text = "This wizard will guide you through all" },
                    new Label { Text = "the steps required to get up and running" },
                    new Label { Text = "Click 'Next' to continue" },
                }
            };
        }
    }
}