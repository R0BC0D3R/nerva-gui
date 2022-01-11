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
		#region Local Variables
		private StackLayout mainControl;
        public StackLayout MainControl => mainControl;

		GridView grid;
		ContextMenu context;

		Label lblTotalXnv = new Label();
		Label lblUnlockedXnv = new Label();

		private Button btnTransfer = new Button { Text = "Transfer Funds", ToolTip = "Send XNV to another address", Width = 120, Enabled = false };
		private Button btnViewAddresses = new Button { Text = "Address Info", ToolTip = "Get your address information", Width = 120, Enabled = false };

		private List<SubAddressAccount> accounts = new List<SubAddressAccount>();
		public List<SubAddressAccount> Accounts => accounts;
		#endregion // Local Variables

		#region Constructor Methods
		public BalancesPage() { }

        public void ConstructLayout()
		{
			try
			{		
				var cmdInfo = new Command { MenuText = "Address Info" };
				var cmdRename = new Command { MenuText = "Rename" };
				var cmdMine = new Command { MenuText = "Mine" };					
				var cmdTransfer = new Command { MenuText = "Transfer Funds" };
				var cmdExportTransfers = new Command { MenuText = "Export Transfers" };

				context = new ContextMenu
				{
					Items = 
					{
						cmdInfo,
						cmdRename,
						cmdMine,
						new SeparatorMenuItem(),					
						cmdTransfer,
						cmdExportTransfers
					}
				};

				grid = new GridView
				{
					GridLines = GridLines.Horizontal,
					Columns = 
					{
						new GridColumn { DataCell = new ImageViewCell { Binding = Binding.Property<SubAddressAccount, Eto.Drawing.Image>(r => r.Index > 0 ? Constants.WalletImage : Constants.WalletImage) }, Width = 25 },
						new GridColumn { DataCell = new TextBoxCell { Binding = Binding.Property<SubAddressAccount, string>(r => r.Index.ToString())}, HeaderText = "#", Width = 25 },
						new GridColumn { DataCell = new TextBoxCell { Binding = Binding.Property<SubAddressAccount, string>(r => r.Label)}, HeaderText = "Label", Width = 150 },
						new GridColumn { DataCell = new TextBoxCell { Binding = Binding.Property<SubAddressAccount, string>(r => Conversions.WalletAddressShortForm(r.BaseAddress))}, HeaderText = "Address", Width = 200 },
						new GridColumn { DataCell = new TextBoxCell { Binding = Binding.Property<SubAddressAccount, string>(r => Conversions.FromAtomicUnits4Places(r.Balance).ToString())}, HeaderText = "Balance", Width = 100 },
						new GridColumn { DataCell = new TextBoxCell { Binding = Binding.Property<SubAddressAccount, string>(r => Conversions.FromAtomicUnits4Places(r.UnlockedBalance).ToString())}, HeaderText = "Unlocked", Width = 100 },
					}
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
									new TableCell(btnViewAddresses))
							}
						}, false),
						new StackLayoutItem(grid, true)
					}
				};

				cmdMine.Executed += new EventHandler<EventArgs>(cmdMine_Executed);		
				cmdInfo.Executed += new EventHandler<EventArgs>(cmdInfo_Executed);		
				cmdExportTransfers.Executed += new EventHandler<EventArgs>(cmdExportTransfers_Executed);		
				cmdTransfer.Executed += new EventHandler<EventArgs>(cmdTransfer_Executed);
				cmdRename.Executed += new EventHandler<EventArgs>(cmdRename_Executed);

				btnTransfer.Click += new EventHandler<EventArgs>(btnTransfer_Click);
				btnViewAddresses.Click += new EventHandler<EventArgs>(btnViewAddresses_Click);

				grid.MouseDown += new EventHandler<MouseEventArgs>(grid_MouseDown);
				grid.CellDoubleClick += new EventHandler<GridCellMouseEventArgs>(grid_CellDoubleClick);
			}
			catch (Exception ex)
			{
				ErrorHandler.HandleException("BP.CSL", ex, true);
			}
		}
		#endregion // Constructor Methods

		#region Helper Methods
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
					btnViewAddresses.Enabled = true;
				}
				else
				{
					lblTotalXnv.Text = string.Empty;
					lblUnlockedXnv.Text = string.Empty;
					accounts.Clear();
					btnTransfer.Enabled = false;
					btnViewAddresses.Enabled = false;
				}

				int si = grid.SelectedRow;
				grid.DataStore = accounts.Count == 0 ? null : accounts;
				grid.SelectRow(si);
			}
			catch (Exception ex)
			{
				ErrorHandler.HandleException("BP.UPD", ex, false);
			}
		}

		private void TransferFunds()
		{
			try
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
			catch (Exception ex)
			{
				ErrorHandler.HandleException("BP.TRF", ex, true);
			}
		}		

		private void ViewAddresses()
		{
			try
			{
				SubAddressAccount account = null;
				if(grid != null && grid.SelectedRow != -1)
				{
					account = accounts[grid.SelectedRow];
				}

				AddressViewDialog viewAddressesDialog = new AddressViewDialog(account, accounts);
				viewAddressesDialog.ShowModal();
			}
			catch (Exception ex)
			{
				ErrorHandler.HandleException("BP.VAD", ex, true);
			}
		}
		#endregion // Helper Methods

		#region Event Methods
		private void cmdMine_Executed(object sender, EventArgs e)
		{
            try
            {
				if (grid.SelectedRow == -1)
				{
					return;
				}

				SubAddressAccount a = accounts[grid.SelectedRow];
				Configuration.Instance.Daemon.MiningAddress = a.BaseAddress;
				Configuration.Save();

				DaemonRpc.StopMining();
				Logger.LogDebug("BP.CME", "Mining stopped");

				if (DaemonRpc.StartMining())
				{
					Logger.LogDebug("BP.CME", $"Mining started to {Conversions.WalletAddressShortForm(Configuration.Instance.Daemon.MiningAddress)} on {Configuration.Instance.Daemon.MiningThreads} threads");
				}
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("BP.CME", ex, true);
            }
		}

		private void cmdInfo_Executed(object sender, EventArgs e)
		{
            try
            {
				ViewAddresses();
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("BP.CIE", ex, true);
            }
		}		

		private void cmdTransfer_Executed(object sender, EventArgs e)
		{
            try
            {
				TransferFunds();
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("BP.CTE", ex, true);
            }
		}

		private void cmdRename_Executed(object sender, EventArgs e)
		{
            try
            {
				if (grid.SelectedRow == -1)
				{
					return;
				}

				TextDialog d = new TextDialog("Select Account Name", false);

				if (d.ShowModal() == DialogResult.Ok)
				{
					if (!WalletRpc.LabelAccount((uint)grid.SelectedRow, d.Text))
					{
						MessageBox.Show(this.MainControl, "Failed to rename account", "Wallet rename", MessageBoxButtons.OK, MessageBoxType.Error, MessageBoxDefaultButton.OK);
					}
				}
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("BP.CRE", ex, true);
            }
		}

		private void cmdExportTransfers_Executed(object sender, EventArgs e)
		{
            try
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
									ErrorHandler.HandleException("BP.CETE", ex, true);
								}
							});
						}, (RequestError error) =>
						{
							Application.Instance.AsyncInvoke(() =>
							{
								Logger.LogError("BP.CETE", "Error exporting transfers: " + error.Message);
								MessageBox.Show(Application.Instance.MainForm, "Error exporting transfers", MessageBoxType.Error);
							});
						});
					}
				}
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("BP.CETE", ex, true);
            }
		}

		private void btnTransfer_Click(object sender, EventArgs e)
		{
            try
            {
				TransferFunds();
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("BP.BTC", ex, true);
            }
		}

		private void btnViewAddresses_Click(object sender, EventArgs e)
		{
            try
            {
				ViewAddresses();
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("BP.BVAC", ex, true);
            }
		}

		private void grid_MouseDown(object sender, EventArgs e)
		{
            try
            {
				var cell = grid.GetCellAt(((MouseEventArgs)e).Location);
				if (cell.RowIndex == -1)
				{
					grid.UnselectAll();
					return;
				}

				if (((MouseEventArgs)e).Buttons != MouseButtons.Alternate)
					return;

				if (grid.SelectedRow == -1)
					return;

				context.Show(grid);
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("BP.GMD", ex, false);
            }
		}

		private void grid_CellDoubleClick(object sender, EventArgs e)
        {
            try
            {
                if(grid.DataStore != null && ((List<SubAddressAccount>)grid.DataStore).Count > 0 && grid.SelectedRow > -1)
                {
                    ViewAddresses();
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("BP.GCDC", ex, true);
            }
        }
		#endregion // Event Methods
    }
}