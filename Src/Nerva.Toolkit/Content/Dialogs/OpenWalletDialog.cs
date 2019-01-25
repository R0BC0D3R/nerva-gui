using System.IO;
using Eto.Drawing;
using Eto.Forms;
using Nerva.Toolkit.CLI;

namespace Nerva.Toolkit.Content.Dialogs
{
    public class OpenWalletDialog : PasswordDialog
	{
        protected string name;
        public string Name => name;

        DropDown ddName = new DropDown();

        public OpenWalletDialog(string title = "Open Wallet") : base(title)
        {
            FileInfo[] files;
            if (WalletHelper.GetWalletFiles(out files))
                foreach (var f in files)
                    ddName.Items.Add(Path.GetFileNameWithoutExtension(f.FullName));

            ddName.SelectedIndex = 0;
        }

        protected override void OnOk()
        {
            base.OnOk();
            
            if (ddName.SelectedValue == null)
                return;

            name = ddName.SelectedValue.ToString();
            this.Close(DialogResult.Ok);
        }

        protected override void OnCancel()
        {
            base.OnCancel();
            name = null;
            this.Close(DialogResult.Cancel);
        }

        protected override void OnShow()
        {
            int oldIndex = ddName.SelectedIndex;
            base.OnShow();
            ddName.SelectedIndex = oldIndex;
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
                    new StackLayoutItem(new Label { Text = "Wallet Name" }),
                    new StackLayoutItem(ddName),
                    new StackLayoutItem(new Label { Text = "Password" }),
                    ConstructPasswordControls()
                }
            };
        }
    }
}