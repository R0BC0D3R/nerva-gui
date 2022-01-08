using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Nerva.Rpc.Wallet;
using Nerva.Desktop.Helpers;

namespace Nerva.Desktop.Content.Dialogs
{
    public class AddressViewDialog : DialogBase<DialogResult>
	{        
        private TextBox txtAddress = new TextBox();
        private TextBox txtPaymentId = new TextBox();
        private DropDown ddAccounts = new DropDown();
        private Button btnCopyAddress = new Button{ Text = "Copy to Clipboard", Width = 140, Height = 24 };

        private SubAddressAccount selectedAccount;

        public SubAddressAccount SelectedAccount => selectedAccount;


        public AddressViewDialog(SubAddressAccount gridAccount, List<SubAddressAccount> accountList) : base("View Addresses")
        {
            try
            {        
                this.MinimumSize = new Size(500, 420);

                if(accountList == null || accountList.Count == 0)
                {
                    MessageBox.Show(this, "No accounts loaded. Please open Wallet and try again.", MessageBoxButtons.OK, MessageBoxType.Error, MessageBoxDefaultButton.OK);
                    return;
                }

                if(gridAccount != null)
                {
                    selectedAccount = gridAccount;
                }

                foreach(SubAddressAccount account in accountList)
                {
                    string addressText = (string.IsNullOrEmpty(account.Label) ? "No Label" : account.Label) + " (" + Conversions.WalletAddressShortForm(account.BaseAddress) + ")";
                    ddAccounts.Items.Add(addressText, account.Index.ToString());

                    if(selectedAccount == null)
                    {
                        // Pick first account
                        selectedAccount = account;
                    }
                }

                ddAccounts.SelectedKey = selectedAccount.Index.ToString();
                txtAddress.Text = selectedAccount.BaseAddress;

                ddAccounts.SelectedIndexChanged  += (s, e) =>
                {
                    foreach(SubAddressAccount account in accountList)
                    {
                        if(ddAccounts.SelectedKey.Equals(account.Index.ToString()))
                        {
                            selectedAccount = account;
                            break;
                        }
                    }

                    txtAddress.Text = selectedAccount.BaseAddress;
                };

                btnCopyAddress.Click += (s, e) =>
                {
                    Clipboard.Instance.Text = txtAddress.Text;
                };
            }
            catch (Exception ex)
			{
				ErrorHandler.HandleException("AVD.AVD", ex, false);
			}
        }

        protected override void OnOk()
        {
            this.Close(DialogResult.Ok);
        }

        protected override void OnCancel() 
        {
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
                    new Label { Text = "Select Account to View Full Address"},
                    ddAccounts,
                    new TableLayout
                    {
                        Spacing = new Eto.Drawing.Size(10, 10),
                        Rows = {
                            new TableRow(new TableCell(new Label { Text = "Wallet Address" }, true), btnCopyAddress)
                        }
                    },
                    txtAddress
                }
            };
        }
    }
}