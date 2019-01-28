using AngryWasp.Logger;
using Eto.Forms;
using Nerva.Rpc.Wallet;
using Nerva.Toolkit.CLI;

namespace Nerva.Toolkit.Content.Dialogs
{
    public class DisplayKeysDialog : DialogBase<DialogResult>
	{
        TextBox txtPublicViewKey = new TextBox { ReadOnly = true };
        TextBox txtPrivateViewKey = new TextBox { ReadOnly = true };

        TextBox txtPublicSpendKey = new TextBox { ReadOnly = true };
        TextBox txtPrivateSpendKey = new TextBox { ReadOnly = true };

        TextArea txtSeed = new TextArea { ReadOnly = true, Wrap = true };

        public DisplayKeysDialog() : base("Restore Info")
        {
            Helpers.TaskFactory.Instance.RunTask("getkeys", $"Retrieving wallet keys", () =>
            {
                Cli.Instance.Wallet.Interface.QueryKey("all_keys", (QueryKeyResponseData e) =>
                {
                    Application.Instance.AsyncInvoke(() =>
                    {
                        txtPublicViewKey.Text = e.PublicViewKey;
                        txtPrivateViewKey.Text = e.PrivateViewKey;

                        txtPublicSpendKey.Text = e.PublicSpendKey;
                        txtPrivateSpendKey.Text = e.PrivateSpendKey;
                    });  
                });

                Cli.Instance.Wallet.Interface.QueryKey("mnemonic", (QueryKeyResponseData e) =>
                {
                    Application.Instance.AsyncInvoke(() =>
                    {
                        txtSeed.Text = e.MnemonicSeed;
                    });
                });
            });

            

            //reuse ok and cancel buttons but give a more meaningful label
            btnOk.Text = "Save";
            btnCancel.Text = "Close";

            this.DefaultButton = btnCancel;
        }

        private Control CreateKeyItem(TextBox txtPubCtrl, TextBox txtPvtCtrl)
        {
            return new TableLayout
            {
                Spacing = new Eto.Drawing.Size(10, 10),
                
                Rows =
                {
                    new TableRow(
                        new TableCell(new Label { Text = "Public:" }),
                        new TableCell(txtPubCtrl, true)),
                    new TableRow(
                        new TableCell(new Label { Text = "Private:" }),
                        new TableCell(txtPvtCtrl, true)),
                }
            };
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
                    new StackLayoutItem(new Label { Text = "View Key" }),
                    CreateKeyItem(txtPublicViewKey, txtPrivateViewKey),
                    new StackLayoutItem(new Label { Text = "Spend Key" }),
                    CreateKeyItem(txtPublicSpendKey, txtPrivateSpendKey),
                    new StackLayoutItem(new Label { Text = "Mnemonic Seed" }),
                    new StackLayoutItem(txtSeed, true),
                }
            };
        }

        protected override void OnOk()
        {
            Log.Instance.Write("Saving keys not implemented");
            this.Close(DialogResult.Ok);
        }

        protected override void OnCancel()
        {
            this.Close(DialogResult.Cancel);
        }
    }
}