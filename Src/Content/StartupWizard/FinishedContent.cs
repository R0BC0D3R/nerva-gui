using Eto.Forms;

namespace Nerva.Desktop.Content.Wizard
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
                    new Label { Text = $"You are now ready to go. Additional settings can be found under: File > Preferences." },
                    new Label { Text = "   " },
                    new Label { Text = "If you require help, please check the help menu for some useful links." },
                    new Label { Text = "   " },
                    new Label { Text = "Press 'Finish' to start using NERVA Desktop." },
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