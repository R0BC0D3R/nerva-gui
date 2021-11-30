using Eto.Forms;

namespace Nerva.Desktop.Content.Dialogs
{
    public class EnterPasswordDialog : PasswordDialog
	{
        public EnterPasswordDialog(string title = "Enter Wallet Password") : base(title) { }

        protected override void OnOk()
        {
            base.OnOk();
            this.Close(DialogResult.Ok);
        }

        protected override void OnCancel()
        {
            base.OnCancel();
            this.Close(DialogResult.Cancel);
        }

        protected override Control ConstructChildContent()
        {
            return new StackLayout
            {
                Padding = 10,
                Spacing = 10,
                Orientation = Orientation.Vertical,
				HorizontalContentAlignment = HorizontalAlignment.Stretch,
				VerticalContentAlignment = VerticalAlignment.Stretch,
                Items = 
                {
                    ConstructPasswordControls()
                }
            };
        }
    }
}