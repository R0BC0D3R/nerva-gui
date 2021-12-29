using System;
using System.Text;
using AngryWasp.Helpers;
using Eto.Forms;
using Eto.Drawing;
using Nerva.Rpc.Wallet;
using Nerva.Desktop.Helpers;

namespace Nerva.Desktop.Content.Dialogs
{
    public class TransferDialog : DialogBase<DialogResult>
	{
        private SubAddressAccount accData;

        TextBox txtAddress = new TextBox();
        TextBox txtPaymentId = new TextBox();
        TextBox txtAmount = new TextBox();
        ComboBox cbxPriority = new ComboBox();
        Label lblAccount = new Label();
        Label lblAmount = new Label();
        Button btnAddressBook = new Button{ Text = "Address Book"};

        private double amt;
        private string address;
        private string payid;
        private Send_Priority priority;

        public string Address => address;
        public string PaymentId => payid;
        public double Amount => amt;

        public Send_Priority Priority => priority;

        Button btnGenPayId = new Button { Text = "Generate" };

        public TransferDialog(SubAddressAccount accData) : base("Transfer NERVA")
        {
            this.MinimumSize = new Size(300, 410);

            this.accData = accData;
            lblAccount.Text = $"{Conversions.WalletAddressShortForm(accData.BaseAddress)} ({(string.IsNullOrEmpty(accData.Label) ? "No Label" : accData.Label)})";
            lblAmount.Text = Conversions.FromAtomicUnits4Places(accData.Balance).ToString();
            cbxPriority.DataStore = Enum.GetNames(typeof(Send_Priority));
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
                priority = (Send_Priority)cbxPriority.SelectedIndex;

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
                    lblAccount,
                    new Label { Text = "Balance" },
                    lblAmount,
                    new Label { Text = "Send To" },
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
                            new StackLayoutItem(null, true),
                            btnAddressBook
                        }
                    })
                }
            };
        }
    }
}