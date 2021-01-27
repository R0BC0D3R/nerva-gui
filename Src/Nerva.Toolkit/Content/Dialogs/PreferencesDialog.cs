using System;
using AngryWasp.Helpers;
using Eto.Forms;
using Nerva.Toolkit.Config;
using Nerva.Toolkit.Helpers;

namespace Nerva.Toolkit.Content.Dialogs
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
        private Button btnGenRandDaemonPort = new Button { Text = "Random", ToolTip = "Generate a random port number" };
        private Button btnUseDefaultPort = new Button { Text = "Default", ToolTip = "Use default port (Recommended)" };
        
        private TextBox txtWalletPath = new TextBox { PlaceholderText = "Wallet path", ToolTip = "Enter the full path to save NERVA wallets", ReadOnly = true };
        private Button btnWalletBrowse = new Button { Text = "Browse", ToolTip = "Find NERVA Wallets" };
        
        private NumericStepper nsWalletPort = new NumericStepper { MinValue = 1000, MaxValue = 50000, DecimalPlaces = 0, MaximumDecimalPlaces = 0, ToolTip = "Wallet port" };
        private Button btnGenRandWalletPort = new Button { Text = "Random", ToolTip = "Generate a random port number" };

        private bool restartCliRequired = false;
        private bool restartMinerRequired = false;

        public bool RestartMinerRequired => restartMinerRequired;

        public bool RestartCliRequired => restartCliRequired;

        public PreferencesDialog() : base("Preferences")
        {
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
                    txtWalletPath.Text = d.Directory;
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

            txtWalletPath.Text = Configuration.Instance.Wallet.WalletDir;
            nsWalletPort.Value = Configuration.Instance.Wallet.Rpc.Port;

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
                                new Label { Text = "Mining Address" },
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
                                        new StackLayoutItem(nsDaemonPort, true),
                                        btnUseDefaultPort,
                                        btnGenRandDaemonPort,
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
                                        new StackLayoutItem(nsWalletPort, true),
                                        btnGenRandWalletPort
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

            if (chkTestnet.Checked != Configuration.Instance.Testnet || // network type changed
                txtToolsPath.Text != Configuration.Instance.ToolsPath || // tool path changed
                nsDaemonPort.Value != Configuration.Instance.Daemon.Rpc.Port || // daemon port changed
                nsWalletPort.Value != Configuration.Instance.Wallet.Rpc.Port ||
                txtWalletPath.Text != Configuration.Instance.Wallet.WalletDir || // wallet port changed
                txtAdditionalArguments.Text != Configuration.Instance.Daemon.AdditionalArguments) 
                restartCliRequired = true;

            //Miner details have changed. Only restart miner
            if (txtMiningAddress.Text != Configuration.Instance.Daemon.MiningAddress || nsMiningThreads.Value != Configuration.Instance.Daemon.MiningThreads)
                restartMinerRequired = true;
                
            Configuration.Instance.ToolsPath = txtToolsPath.Text;
            Configuration.Instance.Testnet = chkTestnet.Checked.Value;

            Configuration.Instance.Daemon.StopOnExit = chkStopOnExit.Checked.Value;
            Configuration.Instance.Daemon.AutoStartMining = chkAutoStartMining.Checked.Value;
            Configuration.Instance.Daemon.MiningAddress = txtMiningAddress.Text;
            Configuration.Instance.Daemon.MiningThreads = (int)nsMiningThreads.Value;
            Configuration.Instance.Daemon.Rpc.Port = (uint)nsDaemonPort.Value;

            Configuration.Instance.Wallet.WalletDir = txtWalletPath.Text;
            Configuration.Instance.Wallet.Rpc.Port = (uint)nsWalletPort.Value;

            Configuration.Instance.Daemon.AdditionalArguments = txtAdditionalArguments.Text;

            this.Close(DialogResult.Ok);
        }
    }
}