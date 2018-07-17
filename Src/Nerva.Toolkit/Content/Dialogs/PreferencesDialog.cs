using System;
using AngryWasp.Helpers;
using AngryWasp.Logger;
using Eto.Forms;
using Nerva.Toolkit.CLI;
using Nerva.Toolkit.CLI.Structures.Response;
using Nerva.Toolkit.Config;
using Nerva.Toolkit.Helpers;

namespace Nerva.Toolkit.Content.Dialogs
{
    public class PreferencesDialog : DialogBase<DialogResult>
    {
        private CheckBox chkCheckForCliUpdate = new CheckBox { Text = "Check for update on start", ToolTip = "Check for updated CLI tools when GUI starts" };
        private CheckBox chkTestnet = new CheckBox { Text = "Testnet", ToolTip = "Connect to the NERVA testnet" };
        private CheckBox chkReconnectToDaemon = new CheckBox { Text = "Reconnect to daemon", ToolTip = "Reconnect to running nervad instance when GUI starts" };
        private TextBox txtToolsPath = new TextBox { PlaceholderText = "CLI tools path", ToolTip = "Enter the full path to the NERVA CLI tools" };
        private Button btnToolsBrowse = new Button { Text = "Browse", ToolTip = "Find NERVA CLI tools" };

        private CheckBox chkStopOnExit = new CheckBox { Text = "Stop daemon on GUI exit", ToolTip = "Stop daemon when GUI exits. This will stop mining" };
        private CheckBox chkAutoStartMining = new CheckBox { Text = "Auto start mining when GUI starts", ToolTip = "Automatically start mining when GUI starts" };
        private TextBox txtMiningAddress = new TextBox { PlaceholderText = "Mining Address", ToolTip = "Address to send mining rewards to" };
        private NumericStepper nsMiningThreads = new NumericStepper { MinValue = 1, MaxValue = Environment.ProcessorCount, DecimalPlaces = 0, MaximumDecimalPlaces = 0, ToolTip = "Number of CPU threads to use for mining" };

        private NumericStepper nsDaemonPort = new NumericStepper { MinValue = 1000, MaxValue = 50000, DecimalPlaces = 0, MaximumDecimalPlaces = 0, ToolTip = "Daemon port. Default is recommended" };
        private Button btnGenRandDaemonPort = new Button { Text = "Random", ToolTip = "Generate a random port number" };
        private Button btnUseDefaultPort = new Button { Text = "Default", ToolTip = "Use default port (Recommended)" };
        
        private TextBox txtWalletPath = new TextBox { PlaceholderText = "Wallet path", ToolTip = "Enter the full path to save NERVA wallets" };
        private Button btnWalletBrowse = new Button { Text = "Browse", ToolTip = "Find NERVA Wallets" };
        
        private TextBox txtLastOpenedWallet = new TextBox { ToolTip = "No saved wallet", ReadOnly = true };
        private Button btnClearPass = new Button { Text = "Clear Pass", ToolTip = "Clear any saved passwords for the wallet" };
        private Button btnClearAll = new Button { Text = "Clear All", ToolTip = "Clear all previously opened wallet info" };
        
        private CheckBox chkSaveWalletPassword = new CheckBox { Text = "Save wallet password", ToolTip = "Save wallet password. Recommended off" };
        private NumericStepper nsWalletPort = new NumericStepper { MinValue = 1000, MaxValue = 50000, DecimalPlaces = 0, MaximumDecimalPlaces = 0, ToolTip = "Wallet port" };
        private Button btnGenRandWalletPort = new Button { Text = "Random", ToolTip = "Generate a random port number" };

        private bool restartDaemonRequired = false;
        private bool restartMinerRequired = false;
        private bool restartWalletRequired = false;

        public bool RestartDaemonRequired => restartDaemonRequired;

        public bool RestartMinerRequired => restartMinerRequired;

        public bool RestartWalletRequired => restartWalletRequired;

        public PreferencesDialog() : base("Preferences")
        {
            btnGenRandDaemonPort.Click += (s, e) => nsDaemonPort.Value = MathHelper.Random.NextInt((int)nsDaemonPort.MinValue, (int)nsDaemonPort.MaxValue);
            btnUseDefaultPort.Click += (s, e) => nsDaemonPort.Value = (chkTestnet.Checked.Value ? 18566 : 17566);

            chkTestnet.CheckedChanged += (s, e) =>
            {
                if (nsDaemonPort.Value == 17566 || nsDaemonPort.Value == 18566)
                    nsDaemonPort.Value = (chkTestnet.Checked.Value ? 18566 : 17566);
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

            btnClearPass.Click += (s, e) => Configuration.Instance.Wallet.LastWalletPassword = null;
            btnClearAll.Click  += (s, e) =>
            {
                Configuration.Instance.Wallet.LastWalletPassword = null;
                Configuration.Instance.Wallet.LastOpenedWallet = null;
                txtLastOpenedWallet.Text = null;
            };

            btnGenRandWalletPort.Click += (s, e) => nsWalletPort.Value = MathHelper.Random.NextInt((int)nsWalletPort.MinValue, (int)nsWalletPort.MaxValue);
        }

        protected override Control ConstructChildContent()
        {
            txtToolsPath.Text = Configuration.Instance.ToolsPath;
            chkCheckForCliUpdate.Checked = Configuration.Instance.Testnet;
            chkReconnectToDaemon.Checked = Configuration.Instance.ReconnectToDaemonProcess;
            chkTestnet.Checked = Configuration.Instance.Testnet;

            chkStopOnExit.Checked = Configuration.Instance.Daemon.StopOnExit;
            chkAutoStartMining.Checked = Configuration.Instance.Daemon.AutoStartMining;
            txtMiningAddress.Text = Configuration.Instance.Daemon.MiningAddress;
            nsMiningThreads.Value = Configuration.Instance.Daemon.MiningThreads;
            nsDaemonPort.Value = Configuration.Instance.Daemon.Rpc.Port;

            txtWalletPath.Text = Configuration.Instance.Wallet.WalletDir;
            txtLastOpenedWallet.Text = Configuration.Instance.Wallet.LastOpenedWallet;
            chkSaveWalletPassword.Checked = Configuration.Instance.Wallet.SaveWalletPassword;
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
                                chkCheckForCliUpdate,
                                chkReconnectToDaemon,
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
                                        btnGenRandDaemonPort
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
                                new Label { Text = "Last Saved Wallet" },
                                new StackLayout
                                {
                                    Orientation = Orientation.Horizontal,
                                    HorizontalContentAlignment = HorizontalAlignment.Right,
                                    VerticalContentAlignment = VerticalAlignment.Center,
                                    Spacing = 10,
                                    Items =
                                    {
                                        new StackLayoutItem(txtLastOpenedWallet, true),
                                        btnClearPass,
                                        btnClearAll
                                    }
                                },
                                chkSaveWalletPassword,
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

            if (chkTestnet.Checked != Configuration.Instance.Testnet || txtToolsPath.Text != Configuration.Instance.ToolsPath)
                restartDaemonRequired = true;

            if (txtMiningAddress.Text != Configuration.Instance.Daemon.MiningAddress || nsMiningThreads.Value != Configuration.Instance.Daemon.MiningThreads)
                restartMinerRequired = true;

            if (nsDaemonPort.Value != Configuration.Instance.Daemon.Rpc.Port)
                restartDaemonRequired = true;

            //if restartDaemonRequired == true, we will restart the wallet anyway
            if (nsWalletPort.Value != Configuration.Instance.Wallet.Rpc.Port && !restartDaemonRequired)
                restartWalletRequired = true;
                
            if (chkTestnet.Checked != Configuration.Instance.Testnet)
            {
                //switched to/from testnet. wipe the saved wallet information
                //to force the user to reopen (hopefully) the right wallet
                Configuration.Instance.Wallet.LastOpenedWallet = null;
                Configuration.Instance.Wallet.LastWalletPassword = null;
            }
                
            Configuration.Instance.ToolsPath = txtToolsPath.Text;
            Configuration.Instance.CheckForUpdateOnStartup = chkCheckForCliUpdate.Checked.Value;
            Configuration.Instance.Testnet = chkTestnet.Checked.Value;
            Configuration.Instance.ReconnectToDaemonProcess = chkReconnectToDaemon.Checked.Value;

            Configuration.Instance.Daemon.StopOnExit = chkStopOnExit.Checked.Value;
            Configuration.Instance.Daemon.AutoStartMining = chkAutoStartMining.Checked.Value;
            Configuration.Instance.Daemon.MiningAddress = txtMiningAddress.Text;
            Configuration.Instance.Daemon.MiningThreads = (int)nsMiningThreads.Value;
            Configuration.Instance.Daemon.Rpc.Port = (int)nsDaemonPort.Value;

            Configuration.Instance.Wallet.WalletDir = txtWalletPath.Text;
            Configuration.Instance.Wallet.SaveWalletPassword = chkSaveWalletPassword.Checked.Value;
            Configuration.Instance.Wallet.Rpc.Port = (int)nsWalletPort.Value;

            this.Close(DialogResult.Ok);
        }
    }
}