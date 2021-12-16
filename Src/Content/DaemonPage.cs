using System;
using System.Collections.Generic;
using Nerva.Desktop.Helpers;
using System.Linq;
using Eto.Forms;
using Nerva.Desktop.CLI;
using AngryWasp.Helpers;
using Nerva.Rpc.Daemon;
using Nerva.Desktop.Config;

namespace Nerva.Desktop.Content
{
    public class DaemonPage
	{
        List<string> la = new List<string>();

		private ulong lastReportedDiff = 0;

		private bool _isCurrentlyMining = false;

        #region Form Controls

        private StackLayout mainControl;
        public StackLayout MainControl => mainControl;
		public string version;

		GridView grid;
	
		private Label lblHeight = new Label() { Text = "." };
		private Label lblRunTime = new Label() { Text = "." };
		private Label lblNetHash = new Label() { Text = "." };
		private Label lblNetwork = new Label() { Text = "." };

		private Label lblMinerStatus = new Label { Text = "Miner (Inactive)" };
		private Label lblMiningAddress = new Label() { Text = "." };
		private Label lblMiningThreads = new Label() { Text = "." };
		private Label lblMiningHashrate = new Label() { Text = "." };
		private Label lblTimeToBlock = new Label() { Text = "." };

		public Button btnStartStopMining = new Button { Text = "Start Mining", Enabled = false };

		public Button btnChangeMiningThreads = new Button { Text = "Set", Enabled = false, Size = new Eto.Drawing.Size(50, 22)  };
		public NumericStepper nsMiningThreads = new NumericStepper { MinValue = 1, MaxValue = Environment.ProcessorCount, DecimalPlaces = 0, MaximumDecimalPlaces = 0, Enabled = false, Size = new Eto.Drawing.Size(50, 22), ToolTip = "Number of CPU threads to use for mining",  };

        #endregion

        public DaemonPage() { }

        public void ConstructLayout()
        {
			try
			{				
				nsMiningThreads.Value = Configuration.Instance.Daemon.MiningThreads;
				var peersCtx_Ban = new Command { MenuText = "Ban Peer" };

				grid = new GridView
				{
					GridLines = GridLines.Horizontal,
					Columns =
					{
						new GridColumn { DataCell = new TextBoxCell { Binding = Binding.Property<GetConnectionsResponseData, string>(r => r.Address)}, HeaderText = "Address", Width = 200 },
						new GridColumn { DataCell = new TextBoxCell { Binding = Binding.Property<GetConnectionsResponseData, string>(r => r.Height.ToString())}, HeaderText = "Height", Width = 100 },
						new GridColumn { DataCell = new TextBoxCell { Binding = Binding.Property<GetConnectionsResponseData, string>(r => TimeSpan.FromSeconds(r.LiveTime).ToString(@"hh\:mm\:ss"))}, HeaderText = "Live Time", Width = 100 },
						new GridColumn { DataCell = new TextBoxCell { Binding = Binding.Property<GetConnectionsResponseData, string>(r => r.State)}, HeaderText = "State", Expand = true }
					}
				};

				grid.ContextMenu = new ContextMenu
				{
					Items = 
					{
						peersCtx_Ban
					}
				};

				peersCtx_Ban.Executed += (s, e) =>
				{
					if (grid.SelectedRow == -1)
						return;

					GetConnectionsResponseData c = (GetConnectionsResponseData)grid.DataStore.ElementAt(grid.SelectedRow);
					DaemonRpc.BanPeer(c.IP);
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
									new TableCell(new Label { Text = "Daemon" }),
									new TableCell(null),
									new TableCell(null, true),
									new TableCell(lblMinerStatus),
									new TableCell(btnStartStopMining),
									new TableCell(null)
								),
								new TableRow(
									new TableCell(new Label { Text = "Height:" }),
									new TableCell(lblHeight),
									new TableCell(null, true),
									new TableCell(new Label { Text = "Threads:" }),
									new TableCell(
										new StackLayout
										{
											Orientation = Orientation.Horizontal,
											HorizontalContentAlignment = HorizontalAlignment.Right,
											VerticalContentAlignment = VerticalAlignment.Center,
											Spacing = 15,
											Items =
											{
												lblMiningThreads,
												nsMiningThreads,
												btnChangeMiningThreads
											}
										}
									),
									new TableCell(null)
								),
								new TableRow(
									new TableCell(new Label { Text = "Run Time:" }),
									new TableCell(lblRunTime),
									new TableCell(null, true),
									new TableCell(new Label { Text = "Address:" }),
									new TableCell(lblMiningAddress),
									new TableCell(null)
								),
								new TableRow(
									new TableCell(new Label { Text = "Net Hash:" }),
									new TableCell(lblNetHash),
									new TableCell(null, true),
									new TableCell(new Label { Text = "Hash Rate:" }),
									new TableCell(lblMiningHashrate),
									new TableCell(null)
								),
								new TableRow(
									new TableCell(new Label { Text = "Network:" }),
									new TableCell(lblNetwork),
									new TableCell(null, true),
									new TableCell(new Label { Text = "Time to Block:" }),
									new TableCell(lblTimeToBlock),
									new TableCell(null)
								)
							}
						}, false),
						new StackLayoutItem(new Scrollable
						{
							Content = grid
						}, true)
					}
				};

				btnStartStopMining.Click += (s, e) =>
				{
					GlobalMethods.StartStopMining();
				};

				btnChangeMiningThreads.Click += (s, e) =>
				{
					if(nsMiningThreads.Value != Configuration.Instance.Daemon.MiningThreads)
					{
						try
						{
							Configuration.Instance.Daemon.MiningThreads = (int)nsMiningThreads.Value;
							Configuration.Save();

							string userMessage = "Number of mining threads changed to " + Configuration.Instance.Daemon.MiningThreads;
							if(_isCurrentlyMining)
							{
								Logger.LogDebug("MF.FPC", "Restarting miner...");
								DaemonRpc.StopMining();								
								DaemonRpc.StartMining();
								userMessage += " and miner restarted";
							}

							MessageBox.Show(Application.Instance.MainForm, userMessage, MessageBoxButtons.OK, MessageBoxType.Information, MessageBoxDefaultButton.OK);
						}
						catch (Exception ex2)
						{
							ErrorHandler.HandleException("DP.CY2", ex2, true);
						}
					}
				};
			}
			catch (Exception ex)
			{
				ErrorHandler.HandleException("DP.CY", ex, true);
			}
        }

		public void UpdateInfo(GetInfoResponseData info)
		{
			try
			{
				if (info != null)
				{					
					// Update the daemon info
					if (lblHeight.Text != info.Height.ToString()) { lblHeight.Text = info.Height.ToString(); }

					DateTime miningStartTime = DateTimeHelper.UnixTimestampToDateTime((ulong)info.StartTime);
					string runTime = (DateTime.Now.ToUniversalTime() - miningStartTime).ToString(@"%d\.hh\:mm\:ss");
					if (lblRunTime.Text != runTime) { lblRunTime.Text = runTime; }

					string nethash = Math.Round(((info.Difficulty / 60.0d) / 1000.0d), 2) + " kH/s";
					if (lblNetHash.Text != nethash) { lblNetHash.Text = nethash; }
					
					string network = "-";
					if (info.Mainnet)
					{
						network = "MainNet";
					}
					else if (info.Testnet)
					{
						network = "TestNet";
					}
					else
					{
						ErrorHandler.HandleException("DP.UI", new Exception("Unknown network"), "Unknown network connection type", false);
					}
					if (lblNetwork.Text != network) { lblNetwork.Text = network; }

					lastReportedDiff = info.Difficulty;
					version = info.Version;
				}
				else
				{
					if (lblNetwork.Text != "-") { lblNetwork.Text = "-"; }
					if (lblHeight.Text != "-") { lblHeight.Text = "-"; }
					if (lblNetHash.Text != "-") { lblNetHash.Text = "-"; }
					if (lblRunTime.Text != "-") { lblRunTime.Text = "-"; }

					lastReportedDiff = 0;
					version = "-";
				}
			}
			catch (Exception ex)
			{
				ErrorHandler.HandleException("DP.UI", ex, false);
			}
		}

		public void UpdateConnections(List<GetConnectionsResponseData> connections)
		{
			try
			{
				if (connections == null)
					connections = new List<GetConnectionsResponseData>();

				int si = grid.SelectedRow;
				grid.DataStore = connections;
				grid.SelectRow(si);
			}
			catch (Exception ex)
			{
				ErrorHandler.HandleException("DP.UC", ex, false);
			}
		}

		public void UpdateMinerStatus(MiningStatusResponseData mStatus)
		{
			try
			{
				if (mStatus != null && mStatus.Active)
				{
					if (!_isCurrentlyMining) { _isCurrentlyMining = true; }

					if (lblMinerStatus.Text != "Miner (Active)") { lblMinerStatus.Text = "Miner (Active)"; }
					if (btnStartStopMining.Text != "Stop Mining") { btnStartStopMining.Text = "Stop Mining"; }
					if (lblMiningAddress.Text != Conversions.WalletAddressShortForm(mStatus.Address)) { lblMiningAddress.Text = Conversions.WalletAddressShortForm(mStatus.Address); }
					if (lblMiningThreads.Text != mStatus.ThreadCount.ToString()) { lblMiningThreads.Text = mStatus.ThreadCount.ToString(); }

					string speed = string.Empty;
					if (mStatus.Speed > 1000)
					{
						speed = $"{mStatus.Speed / 1000.0d} kH/s";
					}
					else
					{					
						speed = $"{(double)mStatus.Speed} h/s";
					}					
					if (lblMiningHashrate.Text != speed) { lblMiningHashrate.Text = speed; }

					string timeToBlock = "-";
					if (lastReportedDiff != 0)
					{
						double t = ((lastReportedDiff / 60.0d) / mStatus.Speed) / 1440.0d;
						timeToBlock = String.Format("{0:F2}", Math.Round(t, 2)) + " days";
					}
					if (lblTimeToBlock.Text != timeToBlock) { lblTimeToBlock.Text = timeToBlock; }
				}
				else
				{
					if (_isCurrentlyMining) { _isCurrentlyMining = false; }

					if (lblMinerStatus.Text != "Miner (Inactive)") { lblMinerStatus.Text = "Miner (Inactive)"; }
					if (btnStartStopMining.Text != "Start Mining") { btnStartStopMining.Text = "Start Mining"; }
					if (lblMiningAddress.Text != "-") { lblMiningAddress.Text = "-"; }
					if (lblMiningThreads.Text != "-") { lblMiningThreads.Text = "-"; }
					if (lblMiningHashrate.Text != "-") { lblMiningHashrate.Text = "-"; }
					if (lblTimeToBlock.Text != "-") { lblTimeToBlock.Text = "-"; }
				}
			}
			catch (Exception ex)
			{
				ErrorHandler.HandleException("DP.UMS", ex, false);
			}
		}
    }
}