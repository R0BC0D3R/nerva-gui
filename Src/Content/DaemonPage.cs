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
		public DateTime LastDaemonResponseTime = DateTime.Now;

        #region Form Controls

        private StackLayout mainControl;
        public StackLayout MainControl => mainControl;
		public string version;

		GridView grid;
	
		private Label lblHeight = new Label() { Text = ".", ToolTip = "Network height you're on" };
		private Label lblRunTime = new Label() { Text = ".", ToolTip = "How long your Daemon process has been running" };
		private Label lblNetHash = new Label() { Text = ".", ToolTip = "Combined hash rate of NERVA network" };
		private Label lblNetwork = new Label() { Text = ".", ToolTip = "Network you're currently on" };

		private Label lblMinerStatus = new Label { Text = "Miner (Inactive)" };
		private Label lblMiningAddress = new Label() { Text = ".", ToolTip = "Address you're mining to" };
		private Label lblMiningThreads = new Label() { Text = ".", ToolTip = "Number of CPU threads you're mining with"  };
		private Label lblMiningHashrate = new Label() { Text = ".", ToolTip = "The speed with which you're mining" };
		private Label lblTimeToBlock = new Label() { Text = ".", ToolTip = "Approximately how long it will take to find a block with current hash rate" };

		public Button btnStartStopMining = new Button { Text = "Start Mining", ToolTip = "Start mining process", Enabled = false };

		public Button btnChangeMiningThreads = new Button { Text = "Set", Enabled = false, Size = new Eto.Drawing.Size(50, 22), ToolTip = "Change number of CPU threads used for mining" };
		public NumericStepper nsMiningThreads = new NumericStepper { MinValue = 1, MaxValue = Environment.ProcessorCount, DecimalPlaces = 0, MaximumDecimalPlaces = 0, Enabled = false, Size = new Eto.Drawing.Size(50, 22), ToolTip = "Number of CPU threads to use for mining"  };

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

							MessageBox.Show(Application.Instance.MainForm, userMessage, MessageBoxButtons.OK, MessageBoxType.Information);
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
				bool hasAnythingChanged = false;

				if (info != null)
				{					
					// Update the daemon info
					if (!lblHeight.Text.Equals(info.Height.ToString())) 
					{
						lblHeight.Text = info.Height.ToString(); 
						hasAnythingChanged = true;
					}

					DateTime miningStartTime = DateTimeHelper.UnixTimestampToDateTime((ulong)info.StartTime);
					string runTime = (DateTime.Now.ToUniversalTime() - miningStartTime).ToString(@"%d\.hh\:mm\:ss");
					if (!lblRunTime.Text.Equals(runTime))
					{
						lblRunTime.Text = runTime;
						hasAnythingChanged = true;
					}

					string nethash = Math.Round(((info.Difficulty / 60.0d) / 1000.0d), 2) + " kH/s";
					if (!lblNetHash.Text.Equals(nethash))
					{
						lblNetHash.Text = nethash;
						hasAnythingChanged = true;
					}
					
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
					if (!lblNetwork.Text.Equals(network))
					{
						lblNetwork.Text = network;
						hasAnythingChanged = true;
					}

					lastReportedDiff = info.Difficulty;
					version = info.Version;
				}
				else
				{
					if (!lblNetwork.Text.Equals("-"))
					{
						lblNetwork.Text = "-";
						hasAnythingChanged = true;
					}
					if (!lblHeight.Text.Equals("-"))
					{
						lblHeight.Text = "-";
						hasAnythingChanged = true;
					}
					if (!lblNetHash.Text.Equals("-"))
					{
						lblNetHash.Text = "-";
						hasAnythingChanged = true;
					}
					if (!lblRunTime.Text.Equals("-"))
					{
						lblRunTime.Text = "-";
						hasAnythingChanged = true;
					}

					lastReportedDiff = 0;
					version = "-";
				}

				if (hasAnythingChanged)
				{
					LastDaemonResponseTime = DateTime.Now;
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

					if (!lblMinerStatus.Text.Equals("Miner (Active)")) { lblMinerStatus.Text = "Miner (Active)"; }
					if (!btnStartStopMining.Text.Equals("Stop Mining"))
					{
						btnStartStopMining.Text = "Stop Mining";
						btnStartStopMining.ToolTip = "Stop mining process";
					}
					if (!lblMiningAddress.Text.Equals(Conversions.WalletAddressShortForm(mStatus.Address))) { lblMiningAddress.Text = Conversions.WalletAddressShortForm(mStatus.Address); }
					if (!lblMiningThreads.Text.Equals(mStatus.ThreadCount.ToString())) { lblMiningThreads.Text = mStatus.ThreadCount.ToString(); }

					string speed = string.Empty;
					if (mStatus.Speed > 1000)
					{
						speed = $"{mStatus.Speed / 1000.0d} kH/s";
					}
					else
					{					
						speed = $"{(double)mStatus.Speed} h/s";
					}					
					if (!lblMiningHashrate.Text.Equals(speed)) { lblMiningHashrate.Text = speed; }

					string timeToBlock = "-";
					if (lastReportedDiff != 0)
					{
						double blockMinutes = ((lastReportedDiff / 60.0d) / mStatus.Speed);

						if((blockMinutes / 1440d) > 1)
						{
							timeToBlock = String.Format("{0:F1}", Math.Round(blockMinutes, 1) / 1440d) + " days (estimated)";
						}
						else if((blockMinutes / 60.0d) > 1)
						{
							timeToBlock = String.Format("{0:F1}", Math.Round(blockMinutes, 1) / 60.0d) + " hours (estimated)";
						}
						else 
						{
							timeToBlock = String.Format("{0:F0}", Math.Round(blockMinutes, 0)) + " minutes (estimated)";
						}
					}
					if (!lblTimeToBlock.Text.Equals(timeToBlock)) { lblTimeToBlock.Text = timeToBlock; }
				}
				else
				{
					if (_isCurrentlyMining) { _isCurrentlyMining = false; }

					if (!lblMinerStatus.Text.Equals("Miner (Inactive)")) { lblMinerStatus.Text = "Miner (Inactive)"; }
					if (!btnStartStopMining.Text.Equals("Start Mining"))
					{
						btnStartStopMining.Text = "Start Mining";
						btnStartStopMining.ToolTip = "Stop mining process";
					}
					if (!lblMiningAddress.Text.Equals("-")) { lblMiningAddress.Text = "-"; }
					if (!lblMiningThreads.Text.Equals("-")) { lblMiningThreads.Text = "-"; }
					if (!lblMiningHashrate.Text.Equals("-")) { lblMiningHashrate.Text = "-"; }
					if (!lblTimeToBlock.Text.Equals("-")) { lblTimeToBlock.Text = "-"; }
				}
			}
			catch (Exception ex)
			{
				ErrorHandler.HandleException("DP.UMS", ex, false);
			}
		}
    }
}