using Eto.Forms;

namespace Nerva.Toolkit.Content.Wizard
{
    public class FinishedContent : WizardContent
    {
        private Control content;

        public override string Title => "Additional Info";

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
                    new Label { Text = $"You are now set and ready to go." },
                    new Label { Text = "Additional settings can be found in" },
                    new Label { Text = "File->Preferences. If you require any" },
                    new Label { Text = "help, please check the help menu for" },
                    new Label { Text = "some useful links." },
                    new Label { Text = "   " },
                    new StackLayoutItem(null, true),
                }
            };
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