using Eto.Drawing;
using Eto.Forms;

namespace Nerva.Toolkit.Content.Dialogs
{
    public class NewWalletDialog : PasswordDialog
	{
        protected string name;
        protected bool hwWallet;

        public string Name => name;
        public bool HwWallet => hwWallet;

        protected TextBox txtName = new TextBox();
        protected CheckBox chkHwWallet = new CheckBox{ Text = "Hardware Wallet" };

        public NewWalletDialog(string title = "Create New Wallet") : base(title)
        {
            //the RPC wallet needs to be open to create a new wallet
            CLI.Cli.Instance.Wallet.ResumeCrashCheck();
        }

        protected override void OnOk()
        {
            base.OnOk();
            name = txtName.Text;
            hwWallet = chkHwWallet.Checked.Value;
            this.Close(DialogResult.Ok);
        }

        protected override void OnCancel()
        {
            base.OnCancel();
            name = null;
            hwWallet = false;
            this.Close(DialogResult.Cancel);
        }

        protected override void OnShow()
        {
            string oldName = txtName.Text;
            bool oldHw = chkHwWallet.Checked.Value;
            base.OnShow();
            txtName.Text = oldName;
            chkHwWallet.Checked = oldHw;
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
                    new StackLayoutItem(txtName),
                    new StackLayoutItem(new Label { Text = "Password" }),
                    ConstructPasswordControls(),
                    new StackLayoutItem(chkHwWallet),
                }
            };
        }
    }
}