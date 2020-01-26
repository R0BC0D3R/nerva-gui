using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AngryWasp.Logger;
using Eto.Forms;
using Nerva.Rpc;
using Nerva.Rpc.Daemon;
using Nerva.Rpc.Wallet;
using Nerva.Toolkit.CLI;
using Nerva.Toolkit.Content.Dialogs;
using Nerva.Toolkit.Content.Wizard;
using Nerva.Toolkit.Helpers;
using Configuration = Nerva.Toolkit.Config.Configuration;
using Log = AngryWasp.Logger.Log;

namespace Nerva.Toolkit
{
    public partial class MainForm : Form
    {    
        AsyncTaskContainer updateWalletTask;
        AsyncTaskContainer updateDaemonTask;

        ulong lastTxHeight = 0;
        ulong lastHeight = 0;
        uint averageHashrateAmount;
        uint averageHashRateCount;

        public MainForm(bool newConfig)
        {
            SuspendLayout();
            ConstructLayout();
            ResumeLayout();

            Application.Instance.Initialized += (s, e) =>
            {       
                StartUpdateTaskList();

                bool needSetup = newConfig || !FileNames.DirectoryContainsCliTools(Configuration.Instance.ToolsPath);
                if (needSetup)
                    new SetupWizard().Run();

                Configuration.Save();

                Cli.Instance.StartDaemon();
                Cli.Instance.StartWallet();

                StartUpdateDaemonUiTask();
                StartUpdateWalletUiTask();
            };

            this.Closing += (s, e) =>
            {
                Cli.Instance.Wallet.StopCrashCheck();
                Cli.Instance.Wallet.ForceClose();
                Program.Shutdown();
            };
        }

        public void StartUpdateTaskList()
        {
            AsyncTaskContainer updateTaskListTask = new AsyncTaskContainer();

            updateTaskListTask.Start(async (CancellationToken token) =>
            {
                while (true)
                {
                    Helpers.TaskFactory.Instance.Prune();

                    Application.Instance.AsyncInvoke(() =>
                    {
                        int i = (int)lblTaskList.Tag;
                        if (i != Helpers.TaskFactory.Instance.GetHashCode())
                        {
                            lblTaskList.Text = $"Tasks: {Helpers.TaskFactory.Instance.Count}";
                            lblTaskList.ToolTip = Helpers.TaskFactory.Instance.ToString().TrimEnd();
                            lblTaskList.Tag = Helpers.TaskFactory.Instance.GetHashCode();
                        }
                    });

                    await Task.Delay(Constants.ONE_SECOND / 2);

                    if (token.IsCancellationRequested)
                        token.ThrowIfCancellationRequested();
                }
            });
        }

        public void StartUpdateWalletUiTask()
        {
            if (Debugger.IsAttached && updateWalletTask != null && updateWalletTask.IsRunning)
                Debugger.Break();

            updateWalletTask = new AsyncTaskContainer();
            updateWalletTask.Start(async (CancellationToken token) =>
            {
                while (true)
                {
                    if (token.IsCancellationRequested)
                        token.ThrowIfCancellationRequested();

                    if (CliInterface.GetRunningProcesses(Cli.Instance.Wallet.BaseExeName).Count == 0)
                    {
                        await Task.Delay(Constants.ONE_SECOND);
                        continue;
                    }

                    if (token.IsCancellationRequested)
                        token.ThrowIfCancellationRequested();

                    await Task.Run( () =>
                    {
                        Cli.Instance.Wallet.Interface.GetAccounts((GetAccountsResponseData ra) =>
                        {
                            Application.Instance.AsyncInvoke( () =>
                            {
                                lblWalletStatus.Text = $"Account(s): {ra.Accounts.Count}  | Balance: {Conversions.FromAtomicUnits(ra.TotalBalance)} XNV";
                                balancesPage.Update(ra);
                            });
                        }, WalletUpdateFailed);

                        Cli.Instance.Wallet.Interface.GetTransfers(lastTxHeight, (GetTransfersResponseData rt) =>
                        {
                            Application.Instance.AsyncInvoke( () =>
                            {
                                uint i = 0, o = 0, l = 0;
                                lastTxHeight = 0;

                                if (rt.Incoming != null && rt.Incoming.Count > 0)
                                    i = rt.Incoming[rt.Incoming.Count - 1].Height;
                                        
                                if (rt.Outgoing != null && rt.Outgoing.Count > 0)
                                    o = rt.Outgoing[rt.Outgoing.Count - 1].Height;

                                l = Math.Max(i, o);

                                lastTxHeight = l;
                                transfersPage.Update(rt);
                            });
                        }, WalletUpdateFailed);
                    });
                    
                    if (token.IsCancellationRequested)
                        token.ThrowIfCancellationRequested();

                    await Task.Delay(Constants.ONE_SECOND);

                    if (token.IsCancellationRequested)
                        token.ThrowIfCancellationRequested();
                }
            });
        }

        void WalletUpdateFailed(RequestError e)
        {
            if (e.Code != -13) //skip messages about not having a wallet open
                Log.Instance.Write(Log_Severity.Error, $"Wallet update failed, Code {e.Code}: {e.Message}");
            Application.Instance.AsyncInvoke(() =>
            {
                lblWalletStatus.Text = "OFFLINE";
                lastTxHeight = 0;
                balancesPage.Update(null);
                transfersPage.Update(null);
            });
        }

        public void StartUpdateDaemonUiTask()
        {
            if (updateDaemonTask != null && updateDaemonTask.IsRunning)
                Debugger.Break();

            updateDaemonTask = new AsyncTaskContainer();
            updateDaemonTask.Start(async (CancellationToken token) =>
            {
                while (true)
                {
                    if (token.IsCancellationRequested)
                        token.ThrowIfCancellationRequested();

                    if (CliInterface.GetRunningProcesses(Cli.Instance.Daemon.BaseExeName).Count == 0)
                    {
                        await Task.Delay(Constants.ONE_SECOND);
                        continue;
                    }

                    if (token.IsCancellationRequested)
                        token.ThrowIfCancellationRequested();

                    await Task.Run( () =>
                    {
                        try
                        {
                            Cli.Instance.Daemon.Interface.GetInfo((GetInfoResponseData r) =>
                            {
                                Application.Instance.Invoke(() =>
                                {
                                    daemonPage.UpdateInfo(r);

                                    lblDaemonStatus.Text = $"Height: {r.Height} | Connections: {r.OutgoingConnectionsCount}(out)+{r.IncomingConnectionsCount}(in)";

                                    if (r.TargetHeight != 0 && r.Height < r.TargetHeight)
                                        lblDaemonStatus.Text += " | Syncing";
                                    else
                                        lblDaemonStatus.Text += " | Sync OK";

                                    lblVersion.Text = $"Version: {r.Version}";
                                    ad.Version = $"GUI: {Constants.VERSION}\r\nCLI: {r.Version}";
                                });

                                if (lastHeight != r.Height)
                                {
                                    lastHeight = r.Height;
                                    if (averageHashRateCount > 0)
                                    {
                                        uint avg = averageHashrateAmount / averageHashRateCount;
                                        chartsPage.HrPlot.AddDataPoint(1, avg);
                                        averageHashrateAmount = 0;
                                        averageHashRateCount = 0;
                                    }
                                }
                            }, (RequestError e) =>
                            {
                                Application.Instance.Invoke(() =>
                                {
                                    lblDaemonStatus.Text = "OFFLINE";
                                    daemonPage.UpdateInfo(null);
                                });
                            });

                            Cli.Instance.Daemon.Interface.GetConnections((List<GetConnectionsResponseData> r) =>
                            {
                                Application.Instance.Invoke(() =>
                                {
                                    daemonPage.UpdateConnections(r);
                                });
                            }, (RequestError e) =>
                            {
                                Application.Instance.Invoke(() =>
                                {
                                    daemonPage.UpdateConnections(null);
                                });
                            });

                            Cli.Instance.Daemon.Interface.MiningStatus((MiningStatusResponseData r) =>
                            {
                                Application.Instance.Invoke(() =>
                                {
                                    daemonPage.UpdateMinerStatus(r);
                                    chartsPage.HrPlot.AddDataPoint(0, r.Speed);
                                    averageHashrateAmount += (uint)r.Speed;
                                    ++averageHashRateCount;
                                });
                            }, (RequestError e) =>
                            {
                                Application.Instance.Invoke(() =>
                                {
                                    daemonPage.UpdateMinerStatus(null);
                                });
                            });
                        }
                        catch (Exception) { }
                    });

                    if (token.IsCancellationRequested)
                        token.ThrowIfCancellationRequested();

                    await Task.Delay(Constants.ONE_SECOND);

                    if (token.IsCancellationRequested)
                        token.ThrowIfCancellationRequested();
                }
            });
        }

        protected void daemon_ToggleMining_Clicked(object sender, EventArgs e)
        {
            Cli.Instance.Daemon.Interface.MiningStatus(( MiningStatusResponseData r) =>
            {
                if (r.Active)
                {
                    Cli.Instance.Daemon.Interface.StopMining();
                    Log.Instance.Write("Mining stopped");
                }
                else
                    if (Cli.Instance.Daemon.Interface.StartMining())
                    Log.Instance.Write($"Mining started for @ {Conversions.WalletAddressShortForm(Configuration.Instance.Daemon.MiningAddress)} on {Configuration.Instance.Daemon.MiningThreads} threads");
            }, null);
        }

        protected void daemon_Restart_Clicked(object sender, EventArgs e)
        {
            //Log the restart and kill the daemon
            Log.Instance.Write("Restarting daemon");
            Cli.Instance.Daemon.Interface.StopDaemon();
            //From here the crash handler should reboot the daemon
        }

        protected void wallet_New_Clicked(object sender, EventArgs e)
        {
            NewWalletDialog d = new NewWalletDialog();
            if (d.ShowModal() == DialogResult.Ok)
            {
                Helpers.TaskFactory.Instance.RunTask("createwallet", $"Creating wallet", () =>
                {
                    if (d.HwWallet)
                    {
                        Cli.Instance.Wallet.Interface.CreateHwWallet(d.Name, d.Password,
                            (CreateHwWalletResponseData result) =>
                        {
                            CreateSuccess(result.Address);
                        }, CreateError);
                    }
                    else
                    {
                        Cli.Instance.Wallet.Interface.CreateWallet(d.Name, d.Password,
                            (CreateWalletResponseData result) =>
                        {
                            CreateSuccess(result.Address);
                        }, CreateError);
                    }
                    
                });
            }
        }

        protected void wallet_Open_Clicked(object sender, EventArgs e)
        {
            OpenWalletDialog d = new OpenWalletDialog();
            if (d.ShowModal() == DialogResult.Ok)
                OpenNewWallet(d.Name, d.Password);
        }

        protected void wallet_Import_Clicked(object sender, EventArgs e)
        {
            ImportWalletDialog d = new ImportWalletDialog();
            if (d.ShowModal() == DialogResult.Ok)
            {
                Helpers.TaskFactory.Instance.RunTask("importwallet", $"Importing wallet", () =>
                {
                    switch (d.ImportType)
                    {
                        case Import_Type.Key:
                            Cli.Instance.Wallet.Interface.RestoreWalletFromKeys(d.Name, d.Address, d.ViewKey, d.SpendKey, d.Password, d.Language,
                                (RestoreWalletFromKeysResponseData result) =>
                            {
                                CreateSuccess(result.Address);
                            }, CreateError);
                        break;
                        case Import_Type.Seed:
                            Cli.Instance.Wallet.Interface.RestoreWalletFromSeed(d.Name, d.Seed, d.SeedOffset, d.Password, d.Language,
                            (RestoreWalletFromSeedResponseData result) =>
                            {
                                CreateSuccess(result.Address);
                            }, CreateError);
                        break;
                    }
                });
            }
        }

        private void OpenNewWallet(string name, string password)
        {
            Application.Instance.AsyncInvoke(() =>
            {
                lblWalletStatus.Text = "OFFLINE";
                lastTxHeight = 0;
                balancesPage.Update(null);
                transfersPage.Update(null);
            });

            Cli.Instance.Wallet.Interface.CloseWallet(() =>
            {
                Cli.Instance.Wallet.Interface.OpenWallet(name, password, () => {
                    Log.Instance.Write("Opened wallet");
                    WalletHelper.SaveWalletLogin(name);
                }, OpenError);
            }, (RequestError error) =>
            {
                if (error.Code != -13)
                    Log.Instance.Write(Log_Severity.Error, $"Error closing wallet: {error.Message}");

                Cli.Instance.Wallet.Interface.OpenWallet(name, password, () => {
                    Log.Instance.Write($"Opened wallet {name}");
                    WalletHelper.SaveWalletLogin(name);
                }, OpenError);
            });
        }

        private void CreateSuccess(string address)
        {
            Application.Instance.AsyncInvoke( () =>
            {
                balancesPage.Update(null);
                transfersPage.Update(null);

                if (MessageBox.Show(Application.Instance.MainForm, "Wallet creation complete.\nWould you like to use this as the mining address?", "Create Wallet",
                    MessageBoxButtons.YesNo, MessageBoxType.Question, MessageBoxDefaultButton.Yes) == DialogResult.Yes)
                {
                    Configuration.Instance.Daemon.MiningAddress = address;
                    Configuration.Save();
                }
            });
        }

        private void CreateError(RequestError error)
        {
            Application.Instance.AsyncInvoke( () =>
            {
                MessageBox.Show(Application.Instance.MainForm, $"Wallet creation failed.\r\nError Code: {error.Code}\r\n{error.Message}", "Create Wallet",
                MessageBoxButtons.OK, MessageBoxType.Error, MessageBoxDefaultButton.OK);
            });
        }

        private void OpenError(RequestError error)
        {
            Application.Instance.AsyncInvoke( () =>
            {
                if (error.Code != -1)
                    MessageBox.Show(Application.Instance.MainForm, $"Failed to open the wallet.\r\nError Code: {error.Code}\r\n{error.Message}", "Open Wallet",
                        MessageBoxButtons.OK, MessageBoxType.Error, MessageBoxDefaultButton.OK);
                else
                    MessageBox.Show(Application.Instance.MainForm, $"Failed to open the wallet.\r\nPlease check your password and make sure the network type is correct", "Open Wallet",
                        MessageBoxButtons.OK, MessageBoxType.Error, MessageBoxDefaultButton.OK);
            });
        }

        protected void wallet_Store_Clicked(object sender, EventArgs e)
        {
			Helpers.TaskFactory.Instance.RunTask("store", $"Saving wallet information", () =>
           	{
               	Log.Instance.Write("Saving wallet");
            	Cli.Instance.Wallet.Interface.Store();

               	Application.Instance.AsyncInvoke(() =>
               	{
                   	MessageBox.Show(this, "Wallet Save Complete", "NERVA Wallet", MessageBoxButtons.OK,
                		MessageBoxType.Information, MessageBoxDefaultButton.OK);
               	});
           	});
        }

        protected void wallet_Stop_Clicked(object sender, EventArgs e)
        {
            Helpers.TaskFactory.Instance.RunTask("closewallet", "Closing the wallet", () => 
            {
                Log.Instance.Write("Closing wallet");
                lastTxHeight = 0;

                Cli.Instance.Wallet.Interface.CloseWallet(null, null);
                Configuration.Instance.Wallet.LastOpenedWallet = null;
                Configuration.Save();

                Application.Instance.AsyncInvoke( () =>
                {
                    balancesPage.Update(null);
                    transfersPage.Update(null);
                    lblWalletStatus.Text = "OFFLINE";
                });
            });
        }

        protected void wallet_RescanSpent_Clicked(object sender, EventArgs e)
        {
            Helpers.TaskFactory.Instance.RunTask("rescanspent", $"Rescanning spent outputs", () =>
           	{
               	Log.Instance.Write("Rescanning spent outputs");
               	if (!Cli.Instance.Wallet.Interface.RescanSpent())
                   	Log.Instance.Write("Rescanning spent outputs failed");
               	else
                   	Log.Instance.Write("Rescanning spent outputs success");

               	Application.Instance.AsyncInvoke(() =>
               	{
                   	MessageBox.Show(this, "Rescanning spent outputs complete", "Rescan Spent",
                    	MessageBoxButtons.OK, MessageBoxType.Information, MessageBoxDefaultButton.OK);
               	});
           	});
        }

        protected void wallet_RescanBlockchain_Clicked(object sender, EventArgs e)
        {
            Helpers.TaskFactory.Instance.RunTask("rescanchain", $"Rescanning the blockchain", () =>
        	{
               	Log.Instance.Write("Rescanning blockchain");
               	if (!Cli.Instance.Wallet.Interface.RescanBlockchain())
            		Log.Instance.Write("Rescanning blockchain failed");
            	else
                	Log.Instance.Write("Rescanning blockchain success");

            	Application.Instance.AsyncInvoke(() =>
            	{
                	MessageBox.Show(this, "Rescanning blockchain complete", "Rescan Blockchain",
                    	MessageBoxButtons.OK, MessageBoxType.Information, MessageBoxDefaultButton.OK);
               	});
        	});
        }

        protected void wallet_Keys_View_Clicked(object sender, EventArgs e)
        {
            new DisplayKeysDialog().ShowModal();
        }

        protected void wallet_Account_Create_Clicked(object sender, EventArgs e)
        {
            TextDialog d = new TextDialog("Enter Account Name", false);
            if (d.ShowModal() == DialogResult.Ok)
            {
                Helpers.TaskFactory.Instance.RunTask("createwallet", "Creating new wallet", () =>
            	{
                	Cli.Instance.Wallet.Interface.CreateAccount(d.Text, (CreateAccountResponseData r) =>
                    {
                        Application.Instance.AsyncInvoke(() =>
                    	{
                        	MessageBox.Show(this, $"New account {d.Text} created", "Create Account",
                        		MessageBoxButtons.OK, MessageBoxType.Error, MessageBoxDefaultButton.OK);
                    	});
                    }, (RequestError err) =>
                    {
                        Application.Instance.AsyncInvoke(() =>
                    	{
                        	MessageBox.Show(this, "Failed to create new account", "Create Account",
                        		MessageBoxButtons.OK, MessageBoxType.Error, MessageBoxDefaultButton.OK);
                    	});
                    });
            	});
            }
        }

        protected void about_Clicked(object sender, EventArgs e)
        {
            ad.ShowDialog(this);
        }

        protected void discord_Clicked(object sender, EventArgs e)
        {
            Process.Start("https://discord.gg/jsdbEns");
        }

        protected void twitter_Clicked(object sender, EventArgs e)
        {
            Process.Start("https://twitter.com/nervacurrency");
        }

        protected void reddit_Clicked(object sender, EventArgs e)
        {
            Process.Start("https://www.reddit.com/r/Nerva/");
        }

        protected void file_Preferences_Clicked(object sender, EventArgs e)
        {
            PreferencesDialog d = new PreferencesDialog();
            if (d.ShowModal() == DialogResult.Ok)
            {
                Configuration.Save();

                if (d.RestartCliRequired)
                {
                    //if thge daemon has to be restarted, there is a good chance the wallet has to be restarted, so just do it
                    MessageBox.Show(this, "The NERVA CLI backend will now restart to apply your changes", "NERVA Preferences",
                        MessageBoxButtons.OK, MessageBoxType.Information, MessageBoxDefaultButton.OK);

                    Log.Instance.Write("Restarting CLI");

                    Helpers.TaskFactory.Instance.RunTask("restartcli", "Restarting the CLI", () =>
                    {
                        updateWalletTask.Stop();
                        updateDaemonTask.Stop();

                        Task.Delay(Constants.ONE_SECOND).Wait();

                        Cli.Instance.Daemon.ForceClose();
                        Cli.Instance.Wallet.ForceClose();

                        Application.Instance.AsyncInvoke( () =>
                        {
                            daemonPage.UpdateInfo(null);
                            daemonPage.UpdateConnections(null);
                            daemonPage.UpdateMinerStatus(null);
                            balancesPage.Update(null);
                            transfersPage.Update(null);
                        });
                        
                        StartUpdateWalletUiTask();
                        StartUpdateDaemonUiTask();
                    });
                }
                else
                {
                    if (d.RestartMinerRequired)
                    {
                        Cli.Instance.Daemon.Interface.StopMining();
                        Cli.Instance.Daemon.Interface.StartMining();
                    }
                }
            }
        }

        protected void file_UpdateCheck_Clicked(object sender, EventArgs e)
        {
            Helpers.TaskFactory.Instance.RunTask("updatecheck", "Checking for updates", () =>
            {
                UpdateManager.CheckUpdate();

                string cliMsg = "CLI: ";

                bool updateRequired = false;

                if (UpdateManager.CliUpdateInfo.UpdateStatus == Update_Status_Code.NewVersionAvailable)
                {
                    cliMsg += $"{UpdateManager.CliUpdateInfo.ToString()}";
                    updateRequired = true;
                }
                else
                    cliMsg += "Up to date";

                Application.Instance.AsyncInvoke(() =>
				{
                    if (updateRequired)
                    {
                        if (MessageBox.Show(Application.Instance.MainForm, $"{cliMsg}\r\nWould you like to download the available updates?", "NERVA Updater", 
                            MessageBoxButtons.YesNo, MessageBoxType.Question, MessageBoxDefaultButton.No) == DialogResult.Yes)
                        {

                            if (UpdateManager.CliUpdateInfo != null && UpdateManager.CliUpdateInfo.UpdateStatus == Update_Status_Code.NewVersionAvailable)
                                Helpers.TaskFactory.Instance.RunTask("dlcliupdate", $"Downloading CLI update", () =>
                                {
                                    UpdateManager.DownloadUpdate(Update_Type.CLI, UpdateManager.CliUpdateInfo.DownloadLink, null, (b, s) =>
                                    {
                                        DisplayUpdateResult(b, s);
                                    });
                                });
                        }
                    }
                    else
                        MessageBox.Show(Application.Instance.MainForm, $"You are up to date", "NERVA Updater", 
                            MessageBoxButtons.OK, MessageBoxType.Information);
				});
                    
            });
        }

        private void DisplayUpdateResult(bool ok, string file)
        {
            if (ok)
            {
                Application.Instance.AsyncInvoke(() =>
                {
                    MessageBox.Show(Application.Instance.MainForm, $"Update downloaded to {file}", "NERVA Updater", 
                        MessageBoxButtons.OK, MessageBoxType.Information);
                });
            }
            else
            {
                if (File.Exists(file))
                    File.Delete(file);
                                                
                Application.Instance.AsyncInvoke(() =>
                {
                    MessageBox.Show(Application.Instance.MainForm, $"An error occurred during the update", "NERVA Updater", 
                        MessageBoxButtons.OK, MessageBoxType.Error);
                });
            }
        }

        protected void quit_Clicked(object sender, EventArgs e)
        {
            Application.Instance.Quit();
        }
    }
}