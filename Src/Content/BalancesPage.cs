using System;
using System.Collections.Generic;
using Eto.Forms;
using Nerva.Desktop.Helpers;
using Nerva.Desktop.Content.Dialogs;
using Nerva.Rpc.Wallet;
using Configuration = Nerva.Desktop.Config.Configuration;
using Nerva.Rpc;
using Nerva.Desktop.CLI;
using System.Text;
using System.IO;
using AngryWasp.Helpers;

namespace Nerva.Desktop.Content
{
    public class BalancesPage
	{
		private StackLayout mainControl;
        public StackLayout MainControl => mainControl;

		GridView grid;

		Label lblTotalXnv = new Label();
		Label lblUnlockedXnv = new Label();

		private Button btnTransfer = new Button { Text = "Trunsfer Funds", ToolTip = "Send XNV to another address", Width = 120, Enabled = false };

		private List<SubAddressAccount> accounts = new List<SubAddressAccount>();
		public List<SubAddressAccount> Accounts => accounts;

		public BalancesPage() { }

        public void ConstructLayout()
		{			
			var ctx_Info = new Command { MenuText = "Address" };
			var ctx_IntAddr = new Command { MenuText = "Integrated Address" };
			var ctx_Rename = new Command { MenuText = "Rename" };
			var ctx_Mine = new Command { MenuText = "Mine" };					
			var ctx_Transfer = new Command { MenuText = "Transfer" };
			var ctx_ExportTransfers = new Command { MenuText = "Export Transfers" };

			ctx_Mine.Executed += (s, e) =>
			{
				if (grid.SelectedRow == -1)
					return;

				SubAddressAccount a = accounts[grid.SelectedRow];
				Configuration.Instance.Daemon.MiningAddress = a.BaseAddress;
				Configuration.Save();

				DaemonRpc.StopMining();
				Logger.LogDebug("BP.CTL", "Mining stopped");

				if (DaemonRpc.StartMining())
				{
					Logger.LogDebug("BP.CTL", $"Mining started to {Conversions.WalletAddressShortForm(Configuration.Instance.Daemon.MiningAddress)} on {Configuration.Instance.Daemon.MiningThreads} threads");
				}
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

			ctx_IntAddr.Executed += (s, e) =>
			{
				if (grid.SelectedRow == -1)
					return;

				SubAddressAccount a = accounts[grid.SelectedRow];

				Helpers.TaskFactory.Instance.RunTask("makeintaddr", $"Creating integrated address", () =>
				{
					WalletRpc.MakeIntegratedAddress(a.BaseAddress, (MakeIntegratedAddressResponseData r) =>
					{
						Application.Instance.AsyncInvoke(() =>
						{
							MessageBox.Show(Application.Instance.MainForm, 
							$"Address: {r.IntegratedAddress}\r\nPayment ID: {r.PaymentId}", 
							"Integrated Address", MessageBoxType.Information);
						});
					}, (RequestError err) =>
					{
						Application.Instance.AsyncInvoke(() =>
						{
							MessageBox.Show(Application.Instance.MainForm, "Could not create integrated address", MessageBoxType.Error);
						});
					});
				});
			};

			ctx_ExportTransfers.Executed += (s, e) =>
			{
				if (grid.SelectedRow == -1) { return; }

				SaveFileDialog saveDialog = new SaveFileDialog();
				saveDialog.Filters.Insert(0, new FileFilter("CSV Files", new string[] { ".csv" }));
				saveDialog.Filters.Insert(1, new FileFilter("All Files", new string[] { ".*" }));

				if(saveDialog.ShowDialog(Application.Instance.MainForm) == DialogResult.Ok)
				{
					string saveFile = saveDialog.FileName;
					SubAddressAccount subAddress = accounts[grid.SelectedRow];

					if(!string.IsNullOrEmpty(saveFile))
					{
						WalletRpc.GetTransfers(0, (GetTransfersResponseData responseData) =>
						{
							Application.Instance.AsyncInvoke( () =>
							{
								try
								{									 								
									StringBuilder exportBuilder = new StringBuilder();
									SortedDictionary<string, TransferItem> transferRows = new SortedDictionary<string, TransferItem>();

									// Create header row
									exportBuilder.AppendLine("height,type,locked,timestamp,amount,running balance,hash,payment id,fee,destination,note");
									
									// Add Incoming tranfers to dictionary
									foreach(TransferItem transfer in responseData.Incoming)
									{
										transferRows.Add(transfer.Height + ":" + transfer.TxId, transfer);
									}

									// Add Outgoing tranfers to dictionary
									foreach(TransferItem transfer in responseData.Outgoing)
									{
										transferRows.Add(transfer.Height + ":" + transfer.TxId, transfer);
									}
									
									double runningBalance = 0.0;
									foreach(TransferItem transfer in transferRows.Values)
									{
										// Keep running balance
										double transferAmount = Conversions.FromAtomicUnits(transfer.Amount);
										double fee = 0.0;
										if(transfer.Type.Equals("out"))
										{
											// Subtract from total
											fee = Conversions.FromAtomicUnits(transfer.Fee);
											runningBalance -= transferAmount - fee;
										}
										else 
										{
											// Add to total. There is no fee for receiver of funds
											runningBalance += transferAmount;
										}

										// Add row to string builder
										exportBuilder.AppendLine(transfer.Height + "," + 
											transfer.Type + "," + 
											(transfer.Locked ? "locked" : "unlocked") + "," +
											DateTimeHelper.UnixTimestampToDateTime(transfer.Timestamp).ToString() + "," +
											transferAmount.ToString("F12") + "," +
											runningBalance.ToString("F12") + "," +
											transfer.TxId + "," +
											transfer.PaymentId + "," +
											fee.ToString("F12") + "," +
											((transfer.Destinations != null && transfer.Destinations.Count > 0) ? transfer.Destinations[0].Address : subAddress.BaseAddress) + "," +
											"\"" + transfer.Note + "\""									
										);
									}

									// Write transfer rows to file
									File.WriteAllText(saveFile, exportBuilder.ToString());

									MessageBox.Show(Application.Instance.MainForm, "Transfers exported successfully!", MessageBoxType.Information);
								}
								catch (Exception ex)
								{
									ErrorHandler.HandleException("BP.ETE", ex, true);
								}
							});
						}, (RequestError error) =>
						{
							Application.Instance.AsyncInvoke(() =>
							{
								Logger.LogError("BP.ETE", "Error exporting transfers: " + error.Message);
								MessageBox.Show(Application.Instance.MainForm, "Error exporting transfers", MessageBoxType.Error);
							});
						});
					}
				}
			};

			ctx_Transfer.Executed += (s, e) =>
			{
				TransferFunds();
			};

			ctx_Rename.Executed += (s, e) =>
			{
				if (grid.SelectedRow == -1)
					return;

				TextDialog d = new TextDialog("Select Account Name", false);

				if (d.ShowModal() == DialogResult.Ok)
					if (!WalletRpc.LabelAccount((uint)grid.SelectedRow, d.Text))
						MessageBox.Show(this.MainControl, "Failed to rename account", "Wallet rename",
                    		MessageBoxButtons.OK, MessageBoxType.Error, MessageBoxDefaultButton.OK);
			};

			ContextMenu ctx = new ContextMenu
			{
				Items = 
				{
					ctx_Info,
					ctx_IntAddr,
					ctx_Rename,
					ctx_Mine,
					new SeparatorMenuItem(),					
					ctx_Transfer,
					ctx_ExportTransfers
				}
			};

			grid = new GridView
			{
				GridLines = GridLines.Horizontal,
				Columns = 
				{
					new GridColumn { DataCell = new TextBoxCell { Binding = Binding.Property<SubAddressAccount, string>(r => r.Index.ToString())}, HeaderText = "#", Width = 30 },
					new GridColumn { DataCell = new TextBoxCell { Binding = Binding.Property<SubAddressAccount, string>(r => r.Label)}, HeaderText = "Label", Width = 150 },
					new GridColumn { DataCell = new TextBoxCell { Binding = Binding.Property<SubAddressAccount, string>(r => Conversions.WalletAddressShortForm(r.BaseAddress))}, HeaderText = "Address", Width = 200 },
					new GridColumn { DataCell = new TextBoxCell { Binding = Binding.Property<SubAddressAccount, string>(r => Conversions.FromAtomicUnits4Places(r.Balance).ToString())}, HeaderText = "Balance", Width = 100 },
					new GridColumn { DataCell = new TextBoxCell { Binding = Binding.Property<SubAddressAccount, string>(r => Conversions.FromAtomicUnits4Places(r.UnlockedBalance).ToString())}, HeaderText = "Unlocked", Width = 120 },
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
								new TableCell(btnTransfer)),
							new TableRow(
								new TableCell(new Label { Text = "Unlocked XNV" }),
								new TableCell(lblUnlockedXnv, true),
								new TableCell(null))
						}
					}, false),
					new StackLayoutItem(grid, true)
				}
			};

			btnTransfer.Click += (s, e) =>
			{
				TransferFunds();
			};
		}

		private void TransferFunds()
		{
			SubAddressAccount account = null;
			if(grid != null && grid.SelectedRow != -1)
			{
				account = accounts[grid.SelectedRow];
			}
			
			TransferDialog transferDialog = new TransferDialog(account, accounts);
			if (transferDialog.ShowModal() == DialogResult.Ok)
			{
				if(transferDialog.IsTransferSplit)
				{
					GlobalMethods.TransferFundsUsingSplit(transferDialog);
				}
				else 
				{
					GlobalMethods.TransferFundsNoSplit(transferDialog);
				}
			}
		}		

		public void Update(GetAccountsResponseData a)
		{
			try
			{
				if (a != null)
				{
					lblTotalXnv.Text = Conversions.FromAtomicUnits4Places(a.TotalBalance).ToString();
					lblUnlockedXnv.Text = Conversions.FromAtomicUnits4Places(a.TotalUnlockedBalance).ToString();
					accounts = a.Accounts;
					btnTransfer.Enabled = true;
				}
				else
				{
					lblTotalXnv.Text = string.Empty;
					lblUnlockedXnv.Text = string.Empty;
					accounts.Clear();
					btnTransfer.Enabled = false;
				}

				int si = grid.SelectedRow;
				grid.DataStore = accounts.Count == 0 ? null : accounts;
				grid.SelectRow(si);
			}
			catch (Exception ex)
			{
				ErrorHandler.HandleException("BP.UPD", ex, $".NET Exception, {ex.Message}", false);
			}
		}
    }
}