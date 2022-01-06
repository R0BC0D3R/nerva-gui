using System;
using AngryWasp.Helpers;
using Eto.Forms;
using Eto.Drawing;
using Nerva.Desktop.Config;
using Nerva.Desktop.Helpers;

namespace Nerva.Desktop.Content.Dialogs
{
    public class PreferencesDialog : DialogBase<DialogResult>
    {
        private CheckBox chkTestnet = new CheckBox { Text = "Testnet", ToolTip = "Connect to the NERVA testnet" };
        private TextBox txtToolsPath = new TextBox { PlaceholderText = "CLI tools path", ToolTip = "Enter the full path to the NERVA CLI tools" };
        private Button btnToolsBrowse = new Button { Text = "Browse", ToolTip = "Find NERVA CLI tools" };

        private CheckBox chkStopOnExit = new CheckBox { Text = "Stop daemon on GUI exit", ToolTip = "Stop daemon when GUI exits. This will stop mining" };
        private CheckBox chkAutoStartMining = new CheckBox { Text = "Auto start mining when GUI starts", ToolTip = "Automatically start mining when GUI starts" };
        private TextBox  txtAdditionalArguments = new TextBox { PlaceholderText = "Additional nervad arguments", ToolTip = "Additional arguments to pass to nervad" };
        private TextBox txtMiningAddress = new TextBox { PlaceholderText = "Mining Address", ToolTip = "Address to send mining rewards to" };
        private NumericStepper nsMiningThreads = new NumericStepper { MinValue = 1, MaxValue = Environment.ProcessorCount, DecimalPlaces = 0, MaximumDecimalPlaces = 0, ToolTip = "Number of CPU threads to use for mining" };

        private NumericStepper nsDaemonPort = new NumericStepper { MinValue = 1000, MaxValue = 50000, DecimalPlaces = 0, MaximumDecimalPlaces = 0, ToolTip = "Daemon port. Default is recommended" };
        private NumericStepper nsDaemonLogLevel = new NumericStepper { MinValue = 0, MaxValue = 4, DecimalPlaces = 0, MaximumDecimalPlaces = 0, ToolTip = "Daemon log level" };
        private Button btnGenRandDaemonPort = new Button { Text = "Random", ToolTip = "Generate a random port number" };
        private Button btnUseDefaultPort = new Button { Text = "Default", ToolTip = "Use default port (Recommended)" };
        
        private TextBox txtWalletPath = new TextBox { PlaceholderText = "Wallet path", ToolTip = "Enter the full path to save NERVA wallets", ReadOnly = true };
        private Button btnWalletBrowse = new Button { Text = "Browse", ToolTip = "Find NERVA Wallets" };
        
        private NumericStepper nsWalletPort = new NumericStepper { MinValue = 1000, MaxValue = 50000, DecimalPlaces = 0, MaximumDecimalPlaces = 0, ToolTip = "Wallet PRC port number" };
        private NumericStepper nsWalletLogLevel = new NumericStepper { MinValue = 0, MaxValue = 4, DecimalPlaces = 0, MaximumDecimalPlaces = 0, ToolTip = "Wallet RPC log level" };

        private Button btnGenRandWalletPort = new Button { Text = "Random", ToolTip = "Generate a random port number" };

        private bool restartDaemonRequired = false;
        private bool restartWalletRequired = false;
        private bool restartMinerRequired = false;

        public bool RestartDaemonRequired => restartDaemonRequired;
        public bool RestartWalletRequired => restartWalletRequired;
        public bool RestartMinerRequired => restartMinerRequired; 

        public PreferencesDialog() : base("Preferences")
        {
            this.MinimumSize = new Size(300, 325);
            
            btnGenRandDaemonPort.Click += (s, e) => nsDaemonPort.Value = MathHelper.Random.NextInt((int)nsDaemonPort.MinValue, (int)nsDaemonPort.MaxValue);
            btnUseDefaultPort.Click += (s, e) => nsDaemonPort.Value = (chkTestnet.Checked.Value ? Constants.NERVAD_RPC_PORT_TESTNET : Constants.NERVAD_RPC_PORT_MAINNET);

            chkTestnet.CheckedChanged += (s, e) =>
            {
                if (nsDaemonPort.Value == Constants.NERVAD_RPC_PORT_MAINNET || nsDaemonPort.Value == Constants.NERVAD_RPC_PORT_TESTNET)
                    nsDaemonPort.Value = (chkTestnet.Checked.Value ? Constants.NERVAD_RPC_PORT_TESTNET : Constants.NERVAD_RPC_PORT_MAINNET);
            };

            btnToolsBrowse.Click += (s, e) =>
            {
                //todo: we should check if the selected directory contains the required tools
                SelectFolderDialog d = new SelectFolderDialog { Directory = txtToolsPath.Text };
                if (d.ShowDialog(this) == DialogResult.Ok)
                {
                    if (FileNames.DirectoryContainsCliTools(d.Directory))
                        txtToolsPath.Text = d.Directory;
                    else
                        MessageBox.Show(this, "Could not find the NERVA CLI tools at the specified path.", "Invalid Config",
							MessageBoxButtons.OK, MessageBoxType.Warning, MessageBoxDefaultButton.OK);
                }
            };

            btnWalletBrowse.Click += (s, e) =>
            {
                SelectFolderDialog d = new SelectFolderDialog { Directory = txtWalletPath.Text };
                if (d.ShowDialog(this) == DialogResult.Ok)
                {
                    txtWalletPath.Text = d.Directory;
                }
            };

            btnGenRandWalletPort.Click += (s, e) => nsWalletPort.Value = MathHelper.Random.NextInt((int)nsWalletPort.MinValue, (int)nsWalletPort.MaxValue);
        }

        protected override Control ConstructChildContent()
        {        
            txtToolsPath.Text = Configuration.Instance.ToolsPath;
            chkTestnet.Checked = Configuration.Instance.Testnet;

            chkStopOnExit.Checked = Configuration.Instance.Daemon.StopOnExit;
            chkAutoStartMining.Checked = Configuration.Instance.Daemon.AutoStartMining;
            txtAdditionalArguments.Text = Configuration.Instance.Daemon.AdditionalArguments;
            txtMiningAddress.Text = Configuration.Instance.Daemon.MiningAddress;
            nsMiningThreads.Value = Configuration.Instance.Daemon.MiningThreads;
            nsDaemonPort.Value = Configuration.Instance.Daemon.Rpc.Port;
            nsDaemonLogLevel.Value = Configuration.Instance.Daemon.Rpc.LogLevel;

            txtWalletPath.Text = Configuration.Instance.Wallet.WalletDir;
            nsWalletPort.Value = Configuration.Instance.Wallet.Rpc.Port;
            nsWalletLogLevel.Value = Configuration.Instance.Wallet.Rpc.LogLevel;

            return new TabControl
            {
                Pages = 
                {
                    new TabPage
                    {
                        Text = "General",
                        Content = new StackLayout
                        {
                            Padding = 10,
                            Spacing = 10,
                            Orientation = Orientation.Vertical,
                            HorizontalContentAlignment = HorizontalAlignment.Stretch,
                            VerticalContentAlignment = VerticalAlignment.Stretch,
                            Items = 
                            {
                                new Label { Text = "CLI Tools Path" },
                                new StackLayout
                                {
                                    Orientation = Orientation.Horizontal,
                                    HorizontalContentAlignment = HorizontalAlignment.Right,
                                    VerticalContentAlignment = VerticalAlignment.Center,
                                    Spacing = 10,
                                    Items =
                                    {
                                        new StackLayoutItem(txtToolsPath, true),
                                        btnToolsBrowse
                                    }
                                },
                                chkTestnet,
                            }
                        }
                    },
                    new TabPage
                    {
                        Text = "Daemon",
                        Content = new StackLayout
                        {
                            Padding = 10,
                            Spacing = 10,
                            Orientation = Orientation.Vertical,
                            HorizontalContentAlignment = HorizontalAlignment.Stretch,
                            VerticalContentAlignment = VerticalAlignment.Stretch,
                            Items = 
                            {
                                chkStopOnExit,
                                chkAutoStartMining,
                                new Label { Text = "Additional Daemon Arguments" },
                                txtAdditionalArguments,
                                new StackLayout
                                {
                                    Orientation = Orientation.Horizontal,
                                    HorizontalContentAlignment = HorizontalAlignment.Right,
                                    VerticalContentAlignment = VerticalAlignment.Center,
                                    Spacing = 10,
                                    Items =
                                    {
                                        new StackLayoutItem(new Label { Text = "Mining Address" }, true),
                                        new Label { Text = "Threads       " }
                                    }
                                },                                                            
                                new StackLayout
                                {
                                    Orientation = Orientation.Horizontal,
                                    HorizontalContentAlignment = HorizontalAlignment.Right,
                                    VerticalContentAlignment = VerticalAlignment.Center,
                                    Spacing = 10,
                                    Items =
                                    {
                                        new StackLayoutItem(txtMiningAddress, true),
                                        nsMiningThreads
                                    }
                                },
                                new StackLayout
                                {
                                    Orientation = Orientation.Horizontal,
                                    HorizontalContentAlignment = HorizontalAlignment.Right,
                                    VerticalContentAlignment = VerticalAlignment.Center,
                                    Spacing = 10,
                                    Items =
                                    {
                                        new StackLayoutItem(new Label { Text = "Port Number" }, true),
                                        new Label { Text = "Log Level       " }
                                    }
                                },
                                new StackLayout
                                {
                                    Orientation = Orientation.Horizontal,
                                    HorizontalContentAlignment = HorizontalAlignment.Right,
                                    VerticalContentAlignment = VerticalAlignment.Center,
                                    Spacing = 10,
                                    Items =
                                    {
                                        new StackLayoutItem(nsDaemonPort, true),
                                        btnUseDefaultPort,
                                        btnGenRandDaemonPort,
                                        new StackLayoutItem(nsDaemonLogLevel, false)
                                    }
                                }
                            }
                        }
                    },
                    new TabPage
                    {
                        Text = "Wallet",
                        Content = new StackLayout
                        {
                            Padding = 10,
                            Spacing = 10,
                            Orientation = Orientation.Vertical,
                            HorizontalContentAlignment = HorizontalAlignment.Stretch,
                            VerticalContentAlignment = VerticalAlignment.Stretch,
                            Items = 
                            {
                                new Label { Text = "Wallet Path" },
                                new StackLayout
                                {
                                    Orientation = Orientation.Horizontal,
                                    HorizontalContentAlignment = HorizontalAlignment.Right,
                                    VerticalContentAlignment = VerticalAlignment.Center,
                                    Spacing = 10,
                                    Items =
                                    {
                                        new StackLayoutItem(txtWalletPath, true),
                                        btnWalletBrowse
                                    }
                                },
                                new StackLayout
                                {
                                    Orientation = Orientation.Horizontal,
                                    HorizontalContentAlignment = HorizontalAlignment.Right,
                                    VerticalContentAlignment = VerticalAlignment.Center,
                                    Spacing = 10,
                                    Items =
                                    {
                                        new StackLayoutItem(new Label { Text = "Port Number" }, true),
                                        new Label { Text = "Log Level       " }
                                    }
                                },
                                new StackLayout
                                {
                                    Orientation = Orientation.Horizontal,
                                    HorizontalContentAlignment = HorizontalAlignment.Right,
                                    VerticalContentAlignment = VerticalAlignment.Center,
                                    Spacing = 10,
                                    Items =
                                    {
                                        new StackLayoutItem(nsWalletPort, true),
                                        btnGenRandWalletPort,
                                        new StackLayoutItem(nsWalletLogLevel, false)
                                    }
                                }                                                            
                            }
                        }
                    }
                }
            };
        }

        protected override void OnCancel()
        {
            this.Close(DialogResult.Cancel);
        }

        protected override void OnOk()
        {
            if (!FileNames.DirectoryContainsCliTools(txtToolsPath.Text))
            {
                MessageBox.Show(this, "Could not find the NERVA CLI tools at the specified path.", "Invalid Config",
							MessageBoxButtons.OK, MessageBoxType.Warning, MessageBoxDefaultButton.OK);
                return;
            }

            if (chkTestnet.Checked != Configuration.Instance.Testnet || // Network type changed
                txtToolsPath.Text != Configuration.Instance.ToolsPath || // Tool path changed
                nsDaemonPort.Value != Configuration.Instance.Daemon.Rpc.Port || // Daemon port changed
                nsDaemonLogLevel.Value != Configuration.Instance.Daemon.Rpc.LogLevel || // Daemon log level changed

                txtAdditionalArguments.Text != Configuration.Instance.Daemon.AdditionalArguments
            ) {
                // Restart Daemon and everything else
                restartDaemonRequired = true;
            }
            else if(
                nsWalletPort.Value != Configuration.Instance.Wallet.Rpc.Port || // Wallet RPC port changed
                nsWalletLogLevel.Value != Configuration.Instance.Wallet.Rpc.LogLevel || // Wallet RPC log level changed
                txtWalletPath.Text != Configuration.Instance.Wallet.WalletDir // Wallet port changed
            ) {
                // Only restart Wallet RPC
                restartWalletRequired = true;
            }
            else if (txtMiningAddress.Text != Configuration.Instance.Daemon.MiningAddress ||
                nsMiningThreads.Value != Configuration.Instance.Daemon.MiningThreads                
            ) {
                // Only restart miner
                restartMinerRequired = true;
            }
                
            Configuration.Instance.ToolsPath = txtToolsPath.Text;
            Configuration.Instance.Testnet = chkTestnet.Checked.Value;

            Configuration.Instance.Daemon.StopOnExit = chkStopOnExit.Checked.Value;
            Configuration.Instance.Daemon.AutoStartMining = chkAutoStartMining.Checked.Value;
            Configuration.Instance.Daemon.MiningAddress = txtMiningAddress.Text;
            Configuration.Instance.Daemon.MiningThreads = (int)nsMiningThreads.Value;
            Configuration.Instance.Daemon.Rpc.Port = (uint)nsDaemonPort.Value;
            Configuration.Instance.Daemon.Rpc.LogLevel = (uint)nsDaemonLogLevel.Value;

            Configuration.Instance.Wallet.WalletDir = txtWalletPath.Text;
            Configuration.Instance.Wallet.Rpc.Port = (uint)nsWalletPort.Value;
            Configuration.Instance.Wallet.Rpc.LogLevel = (uint)nsWalletLogLevel.Value;

            Configuration.Instance.Daemon.AdditionalArguments = txtAdditionalArguments.Text;

            this.Close(DialogResult.Ok);
        }
    }
}