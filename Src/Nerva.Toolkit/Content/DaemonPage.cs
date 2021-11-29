using System;
using System.Collections.Generic;
using Nerva.Toolkit.Helpers;
using System.Linq;
using AngryWasp.Logger;
using Eto.Forms;
using Nerva.Toolkit.CLI;
using AngryWasp.Helpers;
using Nerva.Rpc.Daemon;

namespace Nerva.Toolkit.Content
{
    public class DaemonPage
	{
        List<string> la = new List<string>();

		private ulong lastReportedDiff = 0;

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

        #endregion

        public DaemonPage() { }

        public void ConstructLayout()
        {
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
								new TableCell(new Label { Text = "Address:" }),
								new TableCell(lblMiningAddress),
								new TableCell(null)
							),
							new TableRow(
								new TableCell(new Label { Text = "Run Time:" }),
								new TableCell(lblRunTime),
								new TableCell(null, true),
								new TableCell(new Label { Text = "Threads:" }),
								new TableCell(lblMiningThreads),
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
        }

		public void UpdateInfo(GetInfoResponseData info)
		{
			try
			{
				if (info != null)
				{
					double nethash = Math.Round(((info.Difficulty / 60.0d) / 1000.0d), 2);
					//Update the daemon info
					lblHeight.Text = info.Height.ToString();
					lblNetHash.Text = nethash.ToString() + " kH/s";
					lblRunTime.Text = (DateTime.Now.ToUniversalTime() - DateTimeHelper.UnixTimestampToDateTime((ulong)info.StartTime)).ToString(@"hh\:mm\:ss");

					version = info.Version;

					lastReportedDiff = info.Difficulty;

					if (info.Mainnet)
						lblNetwork.Text = "MainNet";
					else if (info.Testnet)
						lblNetwork.Text = "TestNet";
					else
						Log.Instance.Write(Log_Severity.Fatal, "Unknown network connection type");
				}
				else
				{
					lblNetwork.Text = "-";
					lblHeight.Text = "-";
					lblNetHash.Text = "-";
					lblRunTime.Text = "-";
					version = "-";
					lastReportedDiff = 0;
				}
			}
			catch (Exception ex)
			{
				AngryWasp.Logger.Log.Instance.Write(Log_Severity.Error, $".NET Exception, {ex.Message}");
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
				AngryWasp.Logger.Log.Instance.Write(Log_Severity.Error, $".NET Exception, {ex.Message}");
			}
		}

		public void UpdateMinerStatus(MiningStatusResponseData mStatus)
		{
			try
			{
				if (mStatus != null && mStatus.Active)
				{
					lblMinerStatus.Text = "Miner (Active)";
					btnStartStopMining.Text = "Stop Mining";
					lblMiningAddress.Text = Conversions.WalletAddressShortForm(mStatus.Address);
					lblMiningThreads.Text = mStatus.ThreadCount.ToString();

					string speed;
					if (mStatus.Speed > 1000)
						speed = $"{mStatus.Speed / 1000.0d} kH/s";
					else
						speed = $"{(double)mStatus.Speed} h/s";
					
					lblMiningHashrate.Text = speed;

					if (lastReportedDiff != 0)
					{
						double t = ((lastReportedDiff / 60.0d) / mStatus.Speed) / 1440.0d;
						lblTimeToBlock.Text = String.Format("{0:F2}", Math.Round(t, 2)) + " days";
					}
					else
						lblTimeToBlock.Text = "-";
				}
				else
				{
					lblMinerStatus.Text = "Miner (Inactive)";
					btnStartStopMining.Text = "Start Mining";
					lblMiningAddress.Text = "-";
					lblMiningThreads.Text = "-";
					lblMiningHashrate.Text = "-";
					lblTimeToBlock.Text = "-";
				}
			}
			catch (Exception ex)
			{
				AngryWasp.Logger.Log.Instance.Write(Log_Severity.Error, $".NET Exception, {ex.Message}");
			}
		}
    }
}