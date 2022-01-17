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
        #region Local Variables
        TextBox txtAddress = new TextBox();
        TextBox txtPaymentId = new TextBox();
        Button btnGenPayId = new Button { Text = "Generate" };
        TextBox txtAmount = new TextBox();
        ComboBox cbxPriority = new ComboBox();
        DropDown ddAccounts = new DropDown();
        Label lblBalance = new Label();
        Label lblUnlockedBalance = new Label();
        Button btnAddressBook = new Button{ Text = "Address Book", Width = 100, Height = 24 };
        CheckBox cbxTransferSplit = new CheckBox { Text = "Split Transfer", ToolTip = "Use this option if your transaction is too large and would not get processed otherwise" };
        Label lblRequiredMessage = new Label() { Text = "* Indicates required fields" };

        private double amt;
        private string address;
        private string payid;
        private SendPriority priority;
        private bool isTransferSplit;
        private SubAddressAccount selectedAccount;
        private List<SubAddressAccount> userAccountList;
        private bool abortTransfer;

        public string Address => address;
        public string PaymentId => payid;
        public double Amount => amt;
        public bool IsTransferSplit => isTransferSplit;
        public SubAddressAccount SelectedAccount => selectedAccount;
        public SendPriority Priority => priority;
        public bool AbortTransfer => abortTransfer;
        #endregion // Local Variables

        #region Constructor Methods
        public TransferDialog(SubAddressAccount gridAccount, List<SubAddressAccount> accountList) : base("Transfer NERVA")
        {
            try
            {                
                this.MinimumSize = new Size(500, 440);
                this.btnOk.Text = "Send";

                if(accountList == null || accountList.Count == 0)
                {
                    MessageBox.Show(this, "No accounts loaded. Please open Wallet and try again.", MessageBoxButtons.OK, MessageBoxType.Error, MessageBoxDefaultButton.OK);
                    abortTransfer = true;
                    OnCancel();
                    return;
                }

                userAccountList = accountList;
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
                lblBalance.Text = Conversions.FromAtomicUnits4Places(selectedAccount.Balance).ToString();
                lblUnlockedBalance.Text = Conversions.FromAtomicUnits4Places(selectedAccount.UnlockedBalance).ToString();

                cbxPriority.DataStore = Enum.GetNames(typeof(SendPriority));
                cbxPriority.SelectedIndex = 0;

                btnGenPayId.Click += new EventHandler<EventArgs>(btnGenPayId_Click);
                btnAddressBook.Click += new EventHandler<EventArgs>(btnAddressBook_Click);
                ddAccounts.SelectedIndexChanged  += new EventHandler<EventArgs>(ddAccounts_SelectedIndexChanged);                
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("TD.TD", ex, true);
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
                    new Label { Text = "Send From"},
                    ddAccounts,
                    new TableLayout
                    {
                        Spacing = new Eto.Drawing.Size(10, 10),
                        Rows = {
                            new TableRow(new TableCell(new Label { Text = "Balance (XNV): " }), lblBalance),
                            new TableRow(new TableCell(new Label { Text = "Unlocked (XNV):" }), lblUnlockedBalance)
                        }
                    },
                    new TableLayout
                    {
                        Spacing = new Eto.Drawing.Size(10, 10),
                        Rows = {
                            new TableRow(new TableCell(new Label { Text = "* Send To" }, true), btnAddressBook)
                        }
                    },
                    txtAddress,
                    new TableLayout
                    {
				        Spacing = new Eto.Drawing.Size(10, 10),
                        Rows = {
                            new TableRow(new Label { Text = "* Amount" }, new Label { Text = "Priority"} ),
                            new TableRow(txtAmount, cbxPriority),
                            new TableRow(new TableCell(new Label { Text = "Payment ID" }, true) ),
                            new TableRow(txtPaymentId, btnGenPayId)
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
                    }),
                    lblRequiredMessage
                }
            };
        }

        protected override void OnOk()
        {
            if (MessageBox.Show(this, "Are you sure?", MessageBoxButtons.YesNo, MessageBoxType.Question, MessageBoxDefaultButton.Yes) == DialogResult.Yes)
            {
                StringBuilder errors = new StringBuilder();

                if (!double.TryParse(txtAmount.Text, out amt))
                {
                    errors.AppendLine("Amount to send is incorrect format");
                }

                if (cbxPriority.SelectedIndex == -1)
                {
                    errors.AppendLine("Priority not specified");
                }

                //todo: need to validate address that it is correct format
                if (string.IsNullOrEmpty(txtAddress.Text))
                {
                    errors.AppendLine("Address not provided");
                }

                if (txtPaymentId.Text.Length != 0 && (txtPaymentId.Text.Length != 16 && txtPaymentId.Text.Length != 64))
                {
                    errors.AppendLine($"Payment ID must be 16 or 64 characters long\r\nCurrent Payment ID length is {txtPaymentId.Text.Length} characters");
                }

                string errorString = errors.ToString();
                if (!string.IsNullOrEmpty(errorString))
                {
                    MessageBox.Show(this, $"Transfer failed:\r\n{errorString}", "Transfer Nerva", MessageBoxButtons.OK, MessageBoxType.Error, MessageBoxDefaultButton.OK);
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
        #endregion // Base Class Methods

        #region Event Methods
        private void btnGenPayId_Click(object sender, EventArgs e)
        {
            try
            {
                txtPaymentId.Text = StringHelper.GenerateRandomHexString(64);
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("TD.BGPC", ex, true);
            }
        }
        
        private void btnAddressBook_Click(object sender, EventArgs e)
        {
            try
            {
                AddressBookDialog dlg = new AddressBookDialog();
                if (dlg.ShowModal() == DialogResult.Ok)
                {
                    AddressBookEntry selectedEntry = dlg.SelectedEntry;

                    if(selectedEntry != null)
                    {
                        txtAddress.Text = selectedEntry.Address;
                        txtPaymentId.Text = selectedEntry.PaymentId;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("TD.BABC", ex, true);
            }
        }

        private void ddAccounts_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                foreach(SubAddressAccount account in userAccountList)
                {
                    if(ddAccounts.SelectedKey.Equals(account.Index.ToString()))
                    {
                        selectedAccount = account;
                        break;
                    }
                }                

                lblBalance.Text = Conversions.FromAtomicUnits4Places(selectedAccount.Balance).ToString();
                lblUnlockedBalance.Text = Conversions.FromAtomicUnits4Places(selectedAccount.UnlockedBalance).ToString();
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("TD.ASIC", ex, false);
            }
        }
        #endregion // Event Methods        
    }
}