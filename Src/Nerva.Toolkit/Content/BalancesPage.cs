using System;
using System.Collections.Generic;
using Eto.Forms;
using AngryWasp.Logger;
using Nerva.Toolkit.Helpers;
using Nerva.Toolkit.CLI;
using Nerva.Toolkit.Content.Dialogs;
using Nerva.Rpc.Wallet;
using Configuration = Nerva.Toolkit.Config.Configuration;

namespace Nerva.Toolkit.Content
{
    public class BalancesPage
	{
		private StackLayout mainControl;
        public StackLayout MainControl => mainControl;

		GridView grid;

		Label lblTotalXnv = new Label();
		Label lblUnlockedXnv = new Label();

		private List<SubAddressAccount> accounts = new List<SubAddressAccount>();

		public BalancesPage() { }

        public void ConstructLayout()
		{
			var ctx_Mine = new Command { MenuText = "Mine" };
			var ctx_Transfer = new Command { MenuText = "Transfer" };
			var ctx_Rename = new Command { MenuText = "Rename" };
			var ctx_Info = new Command { MenuText = "Address" };

			ctx_Mine.Executed += (s, e) =>
			{
				if (grid.SelectedRow == -1)
					return;

				SubAddressAccount a = accounts[grid.SelectedRow];
				Configuration.Instance.Daemon.MiningAddress = a.BaseAddress;
				Configuration.Save();

				Cli.Instance.Daemon.Interface.StopMining();
				Log.Instance.Write("Mining stopped");

				if (Cli.Instance.Daemon.Interface.StartMining())
					Log.Instance.Write($"Mining started for @ {Conversions.WalletAddressShortForm(Configuration.Instance.Daemon.MiningAddress)} on {Configuration.Instance.Daemon.MiningThreads} threads");
			};

			ctx_Info.Executed += (s, e) =>
			{
				if (grid.SelectedRow == -1)
					return;

				SubAddressAccount a = accounts[grid.SelectedRow];

				string lbl = string.IsNullOrEmpty(a.Label) ? "No Label" : a.Label;

				TextDialog d = new TextDialog($"Address for account '{lbl}'", true, a.BaseAddress);
				d.ShowModal();
			};

			ctx_Transfer.Executed += (s, e) =>
			{
				if (grid.SelectedRow == -1)
					return;

				SubAddressAccount a = accounts[grid.SelectedRow];

				TransferDialog d = new TransferDialog(a);
				if (d.ShowModal() == DialogResult.Ok)
				{
                    //todo: we need some message in the status bar to tell the user
                    //the payment is being processed
                    Helpers.TaskFactory.Instance.RunTask("transfer", $"Transferring {d.Amount} XNV to {d.Address}", () =>
					{
						TransferResponseData txData = Cli.Instance.Wallet.Interface.TransferFunds(a, d.Address, d.PaymentId, d.Amount, d.Priority);

						if (txData != null)
							{
								Application.Instance.AsyncInvoke(() =>
								{
									MessageBox.Show(Application.Instance.MainForm, 
										$"Sent: {Conversions.FromAtomicUnits(txData.Amount)}\r\nFees: {Conversions.FromAtomicUnits(txData.Fee)}\r\nHash: {txData.TxHash}", 
										"TX Results", MessageBoxType.Information);
								});
							}
						else
						{
							Application.Instance.AsyncInvoke(() =>
							{
								MessageBox.Show(Application.Instance.MainForm, "The transfer request failed", MessageBoxType.Error);
							});
						}
					});
				}
			};

			ctx_Rename.Executed += (s, e) =>
			{
				if (grid.SelectedRow == -1)
					return;

				TextDialog d = new TextDialog("Select Account Name", false);

				if (d.ShowModal() == DialogResult.Ok)
					if (!Cli.Instance.Wallet.Interface.LabelAccount((uint)grid.SelectedRow, d.Text))
						MessageBox.Show(this.MainControl, "Failed to rename account", "Wallet rename",
                    		MessageBoxButtons.OK, MessageBoxType.Error, MessageBoxDefaultButton.OK);
			};

			ContextMenu ctx = new ContextMenu
			{
				Items = 
				{
					ctx_Info,
					ctx_Mine,
					ctx_Transfer,
					ctx_Rename
				}
			};

			grid = new GridView
			{
				GridLines = GridLines.Horizontal,
				Columns = 
				{
					new GridColumn { DataCell = new TextBoxCell { Binding = Binding.Property<SubAddressAccount, string>(r => r.Index.ToString())}, HeaderText = "#" },
					new GridColumn { DataCell = new TextBoxCell { Binding = Binding.Property<SubAddressAccount, string>(r => r.Label)}, HeaderText = "Label" },
					new GridColumn { DataCell = new TextBoxCell { Binding = Binding.Property<SubAddressAccount, string>(r => Conversions.WalletAddressShortForm(r.BaseAddress))}, HeaderText = "Address" },
					new GridColumn { DataCell = new TextBoxCell { Binding = Binding.Property<SubAddressAccount, string>(r => Conversions.FromAtomicUnits(r.Balance).ToString())}, HeaderText = "Balance" },
					new GridColumn { DataCell = new TextBoxCell { Binding = Binding.Property<SubAddressAccount, string>(r => Conversions.FromAtomicUnits(r.UnlockedBalance).ToString())}, HeaderText = "Unlocked" },
				}
			};

			grid.MouseDown += (s, e) =>
			{
				var cell = grid.GetCellAt(e.Location);
				if (cell.RowIndex == -1)
				{
					grid.UnselectAll();
					return;
				}

				if (e.Buttons != MouseButtons.Alternate)
					return;

				if (grid.SelectedRow == -1)
					return;

				ctx.Show(grid);
			};

			mainControl = new StackLayout
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
							new TableRow(
								new TableCell(new Label { Text = "Total XNV" }),
								new TableCell(lblTotalXnv, true),
								new TableCell(null)),
							new TableRow(
								new TableCell(new Label { Text = "Unlocked XNV" }),
								new TableCell(lblUnlockedXnv, true),
								new TableCell(null))
						}
					}, false),
					new StackLayoutItem(grid, true)
				}
			};
		}

		public void Update(GetAccountsResponseData a)
		{
			try
			{
				if (a != null)
				{
					lblTotalXnv.Text = Conversions.FromAtomicUnits(a.TotalBalance).ToString();
					lblUnlockedXnv.Text = Conversions.FromAtomicUnits(a.TotalUnlockedBalance).ToString();
					accounts = a.Accounts;
				}
				else
				{
					lblTotalXnv.Text = string.Empty;
					lblUnlockedXnv.Text = string.Empty;
					accounts.Clear();
				}

				if (OS.Type == OS_Type.Windows || OS.Type == OS_Type.Mac)
				{
					int si = grid.SelectedRow;
					grid.DataStore = accounts.Count == 0 ? null : accounts;
					grid.SelectRow(si);
				}
				else
					grid.DataStore = accounts.Count == 0 ? null : accounts;
			}
			catch (Exception ex)
			{
				Log.Instance.WriteNonFatalException(ex);
			}
		}
    }
}