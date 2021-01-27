using AngryWasp.Helpers;
using Eto.Forms;
using Nerva.Rpc.Wallet;
using Nerva.Toolkit.Helpers;

namespace Nerva.Toolkit.Content.Dialogs
{
    public class ShowTxDialog : DialogBase<DialogResult>
	{
		TableLayout tl2 = new TableLayout{ Padding = 10, Spacing = new Eto.Drawing.Size(10, 10) };
		private TextBox lblAddress = new TextBox { ReadOnly = true };
		private TextBox lblTxId = new TextBox { ReadOnly = true };
		private TextBox lblPaymentId = new TextBox { ReadOnly = true };
		private TextBox lblNote = new TextBox();
		private Label lblType = new Label();
		private Label lblTime = new Label();
		private Label lblAmount = new Label();
		private Label lblFee = new Label();
		private Label lblHeight = new Label();
		private Label lblUnlockTime = new Label();
		private Label lblIndex = new Label();
		private Label lblDoubleSpend = new Label();

        public ShowTxDialog(GetTransferByTxIDResponseData tx, string title = "Transaction Details") : base(title)
        {
			lblAddress.Text = tx.Address;
			lblTxId.Text = tx.TxId;
			lblPaymentId.Text = tx.PaymentId;
			lblNote.Text = tx.Note;

			lblType.Text = tx.Type;
			lblTime.Text = DateTimeHelper.UnixTimestampToDateTime(tx.Timestamp).ToString();
			lblAmount.Text = Conversions.FromAtomicUnits(tx.Amount).ToString();
			lblFee.Text = Conversions.FromAtomicUnits(tx.Fee).ToString();
			lblHeight.Text = tx.Height.ToString();
			lblUnlockTime.Text = (tx.UnlockTime).ToString();
			lblIndex.Text = $"{tx.SubAddressIndex.Major}.{tx.SubAddressIndex.Minor}";
			lblDoubleSpend.Text = tx.DoubleSpendSeen.ToString();

			btnOk.Text = "Save";
			btnCancel.Text = "Close";

			if (tx.Destinations.Count > 0)
			{
				tl2.Rows.Add(new TableRow(
					new TableCell(new Label { Text = "Destination" }, true),
					new TableCell(new Label { Text = "Amount" }, true)));

				foreach (var d in tx.Destinations)
					tl2.Rows.Add(new TableRow(
						new TableCell(new Label { Text = Conversions.WalletAddressShortForm(d.Address) }),
						new TableCell(new Label { Text = Conversions.FromAtomicUnits(d.Amount).ToString() })));
			}

			DefaultButton = btnCancel;
        }

		protected override void OnOk()
		{
			//todo: save TX details
			MessageBox.Show(this, "Not implemented", "Save Tx",
                MessageBoxButtons.OK, MessageBoxType.Information, MessageBoxDefaultButton.OK);
			Close(DialogResult.Ok);
		}

		protected override void OnCancel()
		{
			Close(DialogResult.Cancel);
		}

        protected override Control ConstructChildContent()
        {
            return new StackLayout
			{
				Orientation = Orientation.Vertical,
				HorizontalContentAlignment = HorizontalAlignment.Stretch,
				VerticalContentAlignment = VerticalAlignment.Stretch,
				Items = 
				{
                    new StackLayoutItem(new TableLayout
                    {
                        Padding = 10,
						Spacing = new Eto.Drawing.Size(10, 10),
                        Rows =
						{
							new TableRow (
								new TableCell(new Label { Text = "Address:" }),
								new TableCell(lblAddress, true)),
							new TableRow (
								new TableCell(new Label { Text = "TX:" }),
								new TableCell(lblTxId, true)),
                            new TableRow (
								new TableCell(new Label { Text = "ID:" }),
								new TableCell(lblPaymentId, true)),
							new TableRow (
								new TableCell(new Label { Text = "Note:" }),
								new TableCell(lblNote, true))
                        }
                    }),
					new StackLayoutItem(new TableLayout
					{
						Padding = 10,
						Spacing = new Eto.Drawing.Size(10, 10),
						Rows =
						{
							new TableRow (
								new TableCell(new Label { Text = "Type:" }),
								new TableCell(lblType, true),
								new TableCell(new Label { Text = "Time:" }),
								new TableCell(lblTime, true)),
							new TableRow (
								new TableCell(new Label { Text = "Amount:" }),
								new TableCell(lblAmount, true),
								new TableCell(new Label { Text = "Fee:" }),
								new TableCell(lblFee, true)),
							new TableRow(
								new TableCell(new Label { Text = "Height:" }),
								new TableCell(lblHeight),
								new TableCell(new Label { Text = "Unlock Time:" }),
								new TableCell(lblUnlockTime)),
							new TableRow(
								new TableCell(new Label { Text = "Index:" }),
								new TableCell(lblIndex),
								new TableCell(new Label { Text = "Double Spend:" }),
								new TableCell(lblDoubleSpend)),
							new TableRow(
								new TableCell(null),
								new TableCell(null),
								new TableCell(null),
								new TableCell(null))
						}
					}, false),
					new StackLayoutItem(tl2, true),
				}
			};
        }
    }
}