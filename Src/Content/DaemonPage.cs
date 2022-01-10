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
		#region Local Variables
        List<string> la = new List<string>();
		private ulong lastReportedDiff = 0;
		private bool _isCurrentlyMining = false;
		public DateTime LastDaemonResponseTime = DateTime.Now;
    
        private StackLayout mainControl;
        public StackLayout MainControl => mainControl;
		public string version;
		public string network = "-";

		GridView grid;
	
		private Label lblTwoSpaces = new Label() { Text = "  " };
		private Label lblNetHeight = new Label() { Text = ".", ToolTip = "Network height" };
		private Label lblHeight = new Label() { Text = ".", ToolTip = "The height you're on" };
		private Label lblRunTime = new Label() { Text = ".", ToolTip = "How long your Daemon process has been running" };
		private Label lblNetHash = new Label() { Text = ".", ToolTip = "Combined hash rate of NERVA network" };
		private Label lblNetwork = new Label() { Text = ".", ToolTip = "Network you're currently on" };

		private Label lblMinerStatus = new Label { Text = "Miner (Inactive)" };
		private Label lblMiningAddress = new Label() { Text = ".", ToolTip = "Address you're mining to" };
		private Label lblMiningHashrate = new Label() { Text = ".", ToolTip = "The speed with which you're mining" };
		private Label lblTimeToBlock = new Label() { Text = ".", ToolTip = "Approximately how long it will take to find a block with current hash rate" };

		public Button btnStartStopMining = new Button { Text = "Start Mining", ToolTip = "Start mining process", Enabled = false };

		public Button btnChangeMiningThreads = new Button { Text = "Set", Enabled = false, Size = new Eto.Drawing.Size(64, 22), ToolTip = "Change number of CPU threads used for mining" };
		public NumericStepper nsMiningThreads = new NumericStepper { MinValue = 1, MaxValue = Environment.ProcessorCount, DecimalPlaces = 0, MaximumDecimalPlaces = 0, Enabled = false, Size = new Eto.Drawing.Size(60, 22), ToolTip = "Number of CPU threads to use for mining"  };

        #endregion // Local Variables

		#region Constructor Methods
        public DaemonPage() { }

        public void ConstructLayout()
        {
			try
			{				
				nsMiningThreads.Value = Configuration.Instance.Daemon.MiningThreads;
				var cmdBanPeer = new Command { MenuText = "Ban Peer" };

				grid = new GridView
				{
					GridLines = GridLines.Horizontal,
					Columns =
					{
						new GridColumn { DataCell = new ImageViewCell { Binding = Binding.Property<GetConnectionsResponseData, Eto.Drawing.Image>(r => (r.Incoming ? Constants.TransferInImage : Constants.TransferOutImage)) }, Width = 25 },
						new GridColumn { DataCell = new TextBoxCell { Binding = Binding.Property<GetConnectionsResponseData, string>(r => r.Address)}, HeaderText = "Address", Width = 200 },
						new GridColumn { DataCell = new TextBoxCell { Binding = Binding.Property<GetConnectionsResponseData, string>(r => r.Height.ToString())}, HeaderText = "Height", Width = 100 },
						new GridColumn { DataCell = new TextBoxCell { Binding = Binding.Property<GetConnectionsResponseData, string>(r => TimeSpan.FromSeconds(r.LiveTime).ToString(@"hh\:mm\:ss"))}, HeaderText = "Live Time", Width = 100 },
						new GridColumn { DataCell = new TextBoxCell { Binding = Binding.Property<GetConnectionsResponseData, string>(r => r.State)}, HeaderText = "State", Width = 200 }
					}
				};

				grid.ContextMenu = new ContextMenu
				{
					Items = 
					{
						cmdBanPeer
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
							Spacing = new Eto.Drawing.Size(4, 10),
							Rows =
							{
								new TableRow(
									new TableCell(new Label { Text = "Net Height:" }),
									new TableCell(lblNetHeight),
									new TableCell(null, true),

									new TableCell(new Label { Text = "Your Height:" }),
									new TableCell(lblHeight),
									new TableCell(null, true),

									new TableCell(lblMinerStatus),
									new TableCell(btnStartStopMining),
									new TableCell(null)
								),
								new TableRow(
									new TableCell(new Label { Text = "Net Hash:" }),
									new TableCell(lblNetHash),
									new TableCell(null, true),

									new TableCell(new Label { Text = "Hash Rate:", Height = 22 }),
									new TableCell(lblMiningHashrate),
									new TableCell(null, true),

									new TableCell(new Label { Text = "Threads:" }),
									new TableCell(
										new StackLayout
										{
											Orientation = Orientation.Horizontal,
											HorizontalContentAlignment = HorizontalAlignment.Stretch,
											VerticalContentAlignment = VerticalAlignment.Center,
											Items =
											{
												nsMiningThreads,
												lblTwoSpaces,
												btnChangeMiningThreads
											}
										}),
									new TableCell(null)
								),
								new TableRow(
									new TableCell(new Label { Text = "Run Time:" }),
									new TableCell(lblRunTime),
									new TableCell(null, true),
																		
									new TableCell(new Label { Text = "Block Time:" }),
									new TableCell(lblTimeToBlock),
									new TableCell(null, true),

									new TableCell(new Label { Text = "Address:", Height = 22 }),
									new TableCell(lblMiningAddress),
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

				cmdBanPeer.Executed += new EventHandler<EventArgs>(cmdBanPeer_Executed);
				btnStartStopMining.Click += new EventHandler<EventArgs>(btnStartStopMining_Click);
				btnChangeMiningThreads.Click += new EventHandler<EventArgs>(btnChangeMiningThreads_Click);				
			}
			catch (Exception ex)
			{
				ErrorHandler.HandleException("DP.CL", ex, true);
			}
        }
		#endregion // Constructor Methods

		#region Helper Methods
		public void UpdateInfo(GetInfoResponseData info)
		{
			try
			{				
				bool hasAnythingChanged = false;

				if (info != null)
				{					
					// Update the daemon info
					ulong netHeight = (info.TargetHeight > info.Height ? info.TargetHeight : info.Height);
					if (!lblNetHeight.Text.Equals(netHeight.ToString())) 
					{
						lblNetHeight.Text = netHeight.ToString(); 
						hasAnythingChanged = true;
					}

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
										
					if (info.Mainnet)
					{
						if(!network.Equals("MainNet")) { network = "MainNet"; }
					}
					else if (info.Testnet)
					{
						if(!network.Equals("TestNet")) { network = "TestNet"; }
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
					if (!lblNetHeight.Text.Equals("-"))
					{
						lblNetHeight.Text = "-";
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
				{
					connections = new List<GetConnectionsResponseData>();
				}

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

					if(!nsMiningThreads.Enabled) { nsMiningThreads.Enabled = true; }
					if(mStatus.ThreadCount == Convert.ToUInt32(nsMiningThreads.Value))
					{
						// Mining threads the same as on screen. Set button should be disabled
						if(btnChangeMiningThreads.Enabled) { btnChangeMiningThreads.Enabled = false; }
					}
					else 
					{
						// Minigh threads changed. Set button should be enabled
						if(!btnChangeMiningThreads.Enabled) { btnChangeMiningThreads.Enabled = true; }
					}

					if (!lblMiningAddress.Text.Equals(Conversions.WalletAddressShortForm(mStatus.Address))) { lblMiningAddress.Text = Conversions.WalletAddressShortForm(mStatus.Address); }

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
							timeToBlock = String.Format("{0:F1}", Math.Round(blockMinutes, 1) / 1440d) + " days (est)";
						}
						else if((blockMinutes / 60.0d) > 1)
						{
							timeToBlock = String.Format("{0:F1}", Math.Round(blockMinutes, 1) / 60.0d) + " hours (est)";
						}
						else 
						{
							timeToBlock = String.Format("{0:F0}", Math.Round(blockMinutes, 0)) + " minutes (est)";
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
					if(nsMiningThreads.Enabled) { nsMiningThreads.Enabled = false; }
					if (!lblMiningAddress.Text.Equals("-")) { lblMiningAddress.Text = "-"; }
					if (!lblMiningHashrate.Text.Equals("-")) { lblMiningHashrate.Text = "-"; }
					if (!lblTimeToBlock.Text.Equals("-")) { lblTimeToBlock.Text = "-"; }					
				}
			}
			catch (Exception ex)
			{
				ErrorHandler.HandleException("DP.UMS", ex, false);
			}
		}
		#endregion // Helper Methods

		#region Event Methods
		private void cmdBanPeer_Executed(object sender, EventArgs e)
		{
            try
            {
				if (grid.SelectedRow == -1)
				{
					return;
				}

				GetConnectionsResponseData response = (GetConnectionsResponseData)grid.DataStore.ElementAt(grid.SelectedRow);
				DaemonRpc.BanPeer(response.IP);
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("DP.CBPE", ex, true);
            }
		}

		private void btnStartStopMining_Click(object sender, EventArgs e)
		{
            try
            {
				GlobalMethods.StartStopMining();
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("DP.BSMC", ex, true);
            }
		}

		private void btnChangeMiningThreads_Click(object sender, EventArgs e)
		{
            try
            {
				if(nsMiningThreads.Value != Configuration.Instance.Daemon.MiningThreads)
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
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("DP.BCTC", ex, true);
            }
		}
		#endregion // Event Methods
    }
}