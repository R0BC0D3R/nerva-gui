using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Nerva.Rpc;
using Nerva.Rpc.Wallet;
using Nerva.Desktop.CLI;
using Nerva.Desktop.Helpers;

namespace Nerva.Desktop.Content.Dialogs
{
    public class AddressViewDialog : DialogBase<DialogResult>
	{
        #region Local Variables
        private TextBox txtAddress = new TextBox();
        private DropDown ddAccounts = new DropDown();
        private Button btnCopyAddress = new Button{ Text = "Copy to Clipboard", Width = 140, Height = 24 };

        private Button btnMakeIntegratedAddress = new Button{ Text = "Make Integrated Address", Width = 200, Height = 24 };
        private Button btnCopyIntegrAddress = new Button{ Text = "Copy to Clipboard", Width = 140, Height = 24 };
        private Button btnCopyPaymentId = new Button{ Text = "Copy to Clipboard", Width = 140, Height = 24 };
        private TextBox txtIntegratedAddress = new TextBox();
        private TextBox txtPaymentId = new TextBox();

        private List<SubAddressAccount> subAddressAccountList;
        private SubAddressAccount selectedAccount;
        #endregion // Local Variables

        #region Constructor Methods
        public AddressViewDialog(SubAddressAccount gridAccount, List<SubAddressAccount> accountList) : base("Address Information")
        {
            try
            {        
                this.MinimumSize = new Size(500, 420);
                this.btnOk.Visible = false;
                this.btnCancel.Text = "Close";

                if(accountList == null || accountList.Count == 0)
                {
                    MessageBox.Show(this, "No accounts loaded. Please open Wallet and try again.", MessageBoxButtons.OK, MessageBoxType.Error, MessageBoxDefaultButton.OK);
                    return;
                }

                subAddressAccountList = accountList;
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

                ddAccounts.SelectedIndexChanged += new EventHandler<EventArgs>(ddAccounts_SelectedIndexChanged);
                
                btnMakeIntegratedAddress.Click += new EventHandler<EventArgs>(btnMakeIntegratedAddress_Click);
                btnCopyAddress.Click += new EventHandler<EventArgs>(btnCopyAddress_Click);                
                btnCopyIntegrAddress.Click += new EventHandler<EventArgs>(btnCopyIntegrAddress_Click);
                btnCopyPaymentId.Click += new EventHandler<EventArgs>(btnCopyPaymentId_Click);                
            }
            catch (Exception ex)
			{
				ErrorHandler.HandleException("AVD.AVD", ex, false);
			}
        }
        #endregion // Constructor Methods

        #region Base Class Methods
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
                    new Label { Text = "Select Account to View Full Address" },
                    ddAccounts,
                    new TableLayout
                    {
                        Spacing = new Eto.Drawing.Size(10, 10),
                        Rows = {
                            new TableRow(new TableCell(new Label { Text = "Wallet Address" }, true), btnCopyAddress)
                        }
                    },
                    txtAddress,

                    new Label { Text = "" },
                    new Label { Text = "If You Need Integrated Address You Can Generate It Using Below Button" },
                    new TableLayout
                    {
                        Spacing = new Eto.Drawing.Size(10, 10),
                        Rows = {
                            new TableRow(new TableCell(btnMakeIntegratedAddress), new Label { Text = ""})
                        }
                    },
                    new TableLayout
                    {
                        Spacing = new Eto.Drawing.Size(10, 10),
                        Rows = {
                            new TableRow(new TableCell(new Label { Text = "Integrated Address" }, true), btnCopyIntegrAddress)
                        }
                    },
                    txtIntegratedAddress,
                    new TableLayout
                    {
                        Spacing = new Eto.Drawing.Size(10, 10),
                        Rows = {
                            new TableRow(new TableCell(new Label { Text = "Payment ID" }, true), btnCopyPaymentId)
                        }
                    },
                    txtPaymentId
                }
            };
        }

        protected override void OnOk()
        {
            this.Close(DialogResult.Ok);
        }

        protected override void OnCancel() 
        {
            this.Close(DialogResult.Cancel);
        }        
        #endregion // Base Class Methods

        #region Event Methods
        private void ddAccounts_SelectedIndexChanged(object sender, EventArgs e)
		{
            try
            {
                foreach(SubAddressAccount account in subAddressAccountList)
                {
                    if(ddAccounts.SelectedKey.Equals(account.Index.ToString()))
                    {
                        selectedAccount = account;
                        break;
                    }
                }

                txtAddress.Text = selectedAccount.BaseAddress;
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("AVD.ASIC", ex, true);
            }
		}

        private void btnCopyAddress_Click(object sender, EventArgs e)
		{
            try
            {
                Clipboard.Instance.Text = txtAddress.Text;
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("AVD.BCIA", ex, true);
            }
		}

        private void btnCopyIntegrAddress_Click(object sender, EventArgs e)
		{
            try
            {
                Clipboard.Instance.Text = txtIntegratedAddress.Text;
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("AVD.BCIA", ex, true);
            }
		}

        private void btnCopyPaymentId_Click(object sender, EventArgs e)
		{
            try
            {
                Clipboard.Instance.Text = txtPaymentId.Text;
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("AVD.BCPI", ex, true);
            }
		}

        private void btnMakeIntegratedAddress_Click(object sender, EventArgs e)
		{
            try
            {
				if (selectedAccount == null)
				{
					MessageBox.Show(this, "No accounts loaded. Please open Wallet and try again.", MessageBoxButtons.OK, MessageBoxType.Error, MessageBoxDefaultButton.OK);
                    return;
				}

				WalletRpc.MakeIntegratedAddress(selectedAccount.BaseAddress, (MakeIntegratedAddressResponseData response) =>
				{
					Application.Instance.AsyncInvoke(() =>
					{
                        txtIntegratedAddress.Text = response.IntegratedAddress;
                        txtPaymentId.Text = response.PaymentId;
					});
				}, (RequestError err) =>
				{
					Application.Instance.AsyncInvoke(() =>
					{
                        Logger.LogError("AVD.BMIA", "Message: " + err.Message + ", code: " + err.Code);
						MessageBox.Show(this, "Could not make Integrated Address:\r\n" + err.Message, MessageBoxType.Error);
					});
				});
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("AVD.BMIA", ex, true);
            }
		}
        #endregion // Event Methods
    }
}