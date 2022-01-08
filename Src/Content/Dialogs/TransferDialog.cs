using System;
using System.Text;
using System.Collections.Generic;
using AngryWasp.Helpers;
using Eto.Forms;
using Eto.Drawing;
using Nerva.Rpc.Wallet;
using Nerva.Rpc.Wallet.Helpers;
using Nerva.Desktop.Helpers;

namespace Nerva.Desktop.Content.Dialogs
{
    public class TransferDialog : DialogBase<DialogResult>
	{        
        TextBox txtAddress = new TextBox();
        TextBox txtPaymentId = new TextBox();
        Button btnGenPayId = new Button { Text = "Generate" };
        TextBox txtAmount = new TextBox();
        ComboBox cbxPriority = new ComboBox();
        DropDown ddAccounts = new DropDown();
        Label lblAmount = new Label();
        Button btnAddressBook = new Button{ Text = "Address Book", Width = 100, Height = 24 };
        CheckBox cbxTransferSplit = new CheckBox { Text = "Split Transfer", ToolTip = "Use this option if your transaction is too large and would not get processed otherwise" };

        private double amt;
        private string address;
        private string payid;
        private SendPriority priority;
        private bool isTransferSplit;
        private SubAddressAccount selectedAccount;
        private bool abortTransfer;

        public string Address => address;
        public string PaymentId => payid;
        public double Amount => amt;
        public bool IsTransferSplit => isTransferSplit;
        public SubAddressAccount SelectedAccount => selectedAccount;
        public SendPriority Priority => priority;
        public bool AbortTransfer => abortTransfer;

        public TransferDialog(SubAddressAccount gridAccount, List<SubAddressAccount> accountList) : base("Transfer NERVA")
        {
            this.MinimumSize = new Size(500, 420);

            if(accountList == null || accountList.Count == 0)
            {
                MessageBox.Show(this, "No accounts loaded. Please open Wallet and try again.", MessageBoxButtons.OK, MessageBoxType.Error, MessageBoxDefaultButton.OK);
                abortTransfer = true;
                return;
            }

            if(gridAccount != null)
            {
                selectedAccount = gridAccount;
            }

            foreach(SubAddressAccount account in accountList)
            {
                string addressText = Conversions.WalletAddressShortForm(account.BaseAddress) + " (" + (string.IsNullOrEmpty(account.Label) ? "No Label" : account.Label) + ")";
                ddAccounts.Items.Add(addressText, account.Index.ToString());

                if(selectedAccount == null)
                {
                    // Pick first account
                    selectedAccount = account;
                }
            }

            ddAccounts.SelectedKey = selectedAccount.Index.ToString();
            lblAmount.Text = Conversions.FromAtomicUnits4Places(selectedAccount.Balance).ToString();

            cbxPriority.DataStore = Enum.GetNames(typeof(SendPriority));
            cbxPriority.SelectedIndex = 0;

            btnGenPayId.Click += (s, e) => txtPaymentId.Text = StringHelper.GenerateRandomHexString(64);
            btnAddressBook.Click += (s, e) =>
            {
                AddressBookDialog dlg = new AddressBookDialog();
                if (dlg.ShowModal() == DialogResult.Ok)
                {
                    var se = dlg.SelectedEntry;
                    txtAddress.Text = se.Address;
                    txtPaymentId.Text = se.PaymentId;
                }
            };

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

                lblAmount.Text = Conversions.FromAtomicUnits4Places(selectedAccount.Balance).ToString();
            };
        }

        private void ddAccounts_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        protected override void OnOk()
        {
            if (MessageBox.Show(this, "Are you sure?", MessageBoxButtons.YesNo, MessageBoxType.Question, MessageBoxDefaultButton.Yes) == DialogResult.Yes)
            {
                StringBuilder errors = new StringBuilder();

                if (!double.TryParse(txtAmount.Text, out amt))
                    errors.AppendLine("Amount to send is incorrect format");

                if (cbxPriority.SelectedIndex == -1)
                    errors.AppendLine("Priority not specified");

                //todo: need to validate address that it is correct format
                if (string.IsNullOrEmpty(txtAddress.Text))
                    errors.AppendLine("Address not provided");

                if (txtPaymentId.Text.Length != 0 && (txtPaymentId.Text.Length != 16 && txtPaymentId.Text.Length != 64))
                    errors.AppendLine($"Payment ID must be 16 or 64 characters long\r\nCurrent Payment ID length is {txtPaymentId.Text.Length} characters");

                string errorString = errors.ToString();
                if (!string.IsNullOrEmpty(errorString))
                {
                    MessageBox.Show(this, $"Transfer failed:\r\n{errorString}", "Transfer Nerva",
                        MessageBoxButtons.OK, MessageBoxType.Error, MessageBoxDefaultButton.OK);
                    return;
                }

                address = txtAddress.Text;
                payid = txtPaymentId.Text;
                priority = (SendPriority)cbxPriority.SelectedIndex;
                isTransferSplit = (bool)cbxTransferSplit.Checked;

                this.Close(DialogResult.Ok);
            }
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
                    new Label { Text = "Send From"},
                    ddAccounts,
                    new Label { Text = "Balance" },
                    lblAmount,
                    new TableLayout
                    {
                        Spacing = new Eto.Drawing.Size(10, 10),
                        Rows = {
                            new TableRow(new TableCell(new Label { Text = "Send To" }, true), btnAddressBook)                            
                        }
                    },
                    txtAddress,
                    new TableLayout
                    {
				        Spacing = new Eto.Drawing.Size(10, 10),
                        Rows = {                            
                            new TableRow(new TableCell(new Label { Text = "Payment ID" }, true) ),
                            new TableRow(txtPaymentId, btnGenPayId),
                            new TableRow(new Label { Text = "Amount" }, new Label { Text = "Priority"} ),
                            new TableRow(txtAmount, cbxPriority)
                        }
                    },
                    new StackLayoutItem(new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalContentAlignment = HorizontalAlignment.Right,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Items =
                        {                            
                            cbxTransferSplit,
                            new StackLayoutItem(null, true)                            
                        }
                    })
                }
            };
        }
    }
}