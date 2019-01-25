using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AngryWasp.Logger;
using Eto.Forms;
using Nerva.Rpc.Daemon;
using Nerva.Rpc.Wallet;
using Nerva.Toolkit.CLI;
using Nerva.Toolkit.Config;
using Nerva.Toolkit.Content.Dialogs;
using Nerva.Toolkit.Content.Wizard;
using Nerva.Toolkit.Helpers;

namespace Nerva.Toolkit
{
    public partial class MainForm : Form
    {    
        AsyncTaskContainer updateWalletTask;
        AsyncTaskContainer updateDaemonTask;

        uint lastTxHeight = 0;
        uint lastHeight = 0;
        uint averageHashrateAmount;
        uint averageHashRateCount;

        public MainForm(bool newConfig)
        {
            SuspendLayout();
            ConstructLayout();
            ResumeLayout();

            WalletHelper.Instance.WalletWizardEvent += (Open_Wallet_Dialog_Result result, object additionalData) =>
            {
                CloseWallet(false);
            };

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

                    GetAccountsResponseData account = null;
                    GetTransfersResponseData transfers = null;
                    await Task.Run( () =>
                    {
                        try
                        {
                            account = Cli.Instance.Wallet.Interface.GetAccounts();
                            transfers = Cli.Instance.Wallet.Interface.GetTransfers(lastTxHeight, out lastTxHeight);
                        }
                        catch (Exception) { }
                    });
                    

                    if (token.IsCancellationRequested)
                        token.ThrowIfCancellationRequested();

                    if (account != null)
                    {
                        string walletStatus = (account != null) ? $"Account(s): {account.Accounts.Count}  | Balance: {Conversions.FromAtomicUnits(account.TotalBalance)} XNV" : "WALLET CLOSED";

                        Application.Instance.AsyncInvoke(() =>
                        {
                            lblWalletStatus.Text = walletStatus;
                            balancesPage.Update(account);
                            transfersPage.Update(transfers);
                        });
                    }
                    else
                    {
                        Application.Instance.AsyncInvoke(() =>
                        {
                            lblWalletStatus.Text = "OFFLINE";
                            lastTxHeight = 0;
                            balancesPage.Update(null);
                            transfersPage.Update(null);
                        });
                    }

                    if (token.IsCancellationRequested)
                        token.ThrowIfCancellationRequested();

                    await Task.Delay(Constants.ONE_SECOND);

                    if (token.IsCancellationRequested)
                        token.ThrowIfCancellationRequested();
                }
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

                    GetInfoResponseData info = null;
                    List<GetConnectionsResponseData> connections = null;
                    uint height = 0;
                    MiningStatusResponseData mStatus = null;

                    await Task.Run( () =>
                    {
                        try
                        {
                            height = Cli.Instance.Daemon.Interface.GetBlockCount();
                            info = Cli.Instance.Daemon.Interface.GetInfo();
                            connections = Cli.Instance.Daemon.Interface.GetConnections();
                            mStatus = Cli.Instance.Daemon.Interface.MiningStatus();
                        }
                        catch (Exception) { }
                    });

                    if (token.IsCancellationRequested)
                        token.ThrowIfCancellationRequested();

                    if (info != null)
                    {
                        Application.Instance.Invoke(() =>
                        {
                            lblDaemonStatus.Text = $"Height: {height} | Connections: {info.OutgoingConnectionsCount}(out)+{info.IncomingConnectionsCount}(in)";

                            if (info.TargetHeight != 0 && info.Height < info.TargetHeight)
                                lblDaemonStatus.Text += " | Syncing";
                            else
                                lblDaemonStatus.Text += " | Sync OK";

                            lblVersion.Text = $"Version: {info.Version}";
                            ad.Version = $"GUI: {Constants.VERSION}\r\nCLI: {info.Version}";

                            daemonPage.Update(info, connections, mStatus);
                            chartsPage.HrPlot.AddDataPoint(0, mStatus.Speed);

                            if (lastHeight != height)
                            {
                                lastHeight = height;
                                if (averageHashRateCount > 0)
                                {
                                    uint avg = averageHashrateAmount / averageHashRateCount;
                                    chartsPage.HrPlot.AddDataPoint(1, avg);
                                    averageHashrateAmount = 0;
                                    averageHashRateCount = 0;
                                }
                            }

                            averageHashrateAmount += (uint)mStatus.Speed;
                            ++averageHashRateCount;
                        });
                    }
                    else
                    {
                        Application.Instance.Invoke(() =>
                        {
                            lblDaemonStatus.Text = "OFFLINE";
                            daemonPage.Update(null, null, null);
                        });
                    }

                    if (token.IsCancellationRequested)
                        token.ThrowIfCancellationRequested();

                    await Task.Delay(Constants.ONE_SECOND);

                    if (token.IsCancellationRequested)
                        token.ThrowIfCancellationRequested();
                }
            });
        }

        private void CloseWallet(bool clearSavedWallet)
        {
            Helpers.TaskFactory.Instance.RunTask("closewallet", "Closing the wallet", () => 
            {
                Log.Instance.Write("Closing wallet");
                Cli.Instance.Wallet.StopCrashCheck();
                updateWalletTask.Stop();
                Cli.Instance.Wallet.ForceClose();
                lastTxHeight = 0;

                Application.Instance.AsyncInvoke( () =>
                {
                    balancesPage.Update(null);
                    transfersPage.Update(null);
                    lblWalletStatus.Text = "OFFLINE";
                });

                if (clearSavedWallet)
                {
                    Configuration.Instance.Wallet.LastOpenedWallet = null;
                    Configuration.Instance.Wallet.LastWalletPassword = null;
                    Configuration.Save();
                }
                else
                {
                    Cli.Instance.Wallet.ResumeCrashCheck();
                    StartUpdateWalletUiTask();
                }
            });
        }

        protected void daemon_ToggleMining_Clicked(object sender, EventArgs e)
        {
            MiningStatusResponseData ms = Cli.Instance.Daemon.Interface.MiningStatus();

            if (ms.Active)
            {
                Cli.Instance.Daemon.Interface.StopMining();
                Log.Instance.Write("Mining stopped");
            }
            else
                if (Cli.Instance.Daemon.Interface.StartMining())
                Log.Instance.Write("Mining started for @ {0} on {1} threads",
                    Conversions.WalletAddressShortForm(Configuration.Instance.Daemon.MiningAddress),
                    Configuration.Instance.Daemon.MiningThreads);
        }

        protected void daemon_Restart_Clicked(object sender, EventArgs e)
        {
            //Log the restart and kill the daemon
            Log.Instance.Write("Restarting daemon");
            Cli.Instance.Daemon.Interface.StopDaemon();
            //From here the crash handler should reboot the daemon
        }

        protected void wallet_Select_Clicked(object sender, EventArgs e)
        {
            WalletHelper.Instance.ShowWalletWizard();
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
            CloseWallet(true);
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
                	if (Cli.Instance.Wallet.Interface.CreateAccount(d.Text) == null)
                	{
                    	Application.Instance.AsyncInvoke(() =>
                    	{
                        	MessageBox.Show(this, "Failed to create new account", "Create Account",
                        		MessageBoxButtons.OK, MessageBoxType.Error, MessageBoxDefaultButton.OK);
                    	});
                	}
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
                            daemonPage.Update(null, null, null);
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
                UpdateManager.CheckForCliUpdates();

                switch (UpdateManager.UpdateStatus)
                {
                    case Update_Status_Code.UpToDate:
                        Log.Instance.Write("NERVA CLI tools are up to date");
                        Application.Instance.AsyncInvoke(() =>
                        {
                            MessageBox.Show(this, "The NERVA CLI tools are up to date.", "Nerva Updater",
                                MessageBoxButtons.OK, MessageBoxType.Information, MessageBoxDefaultButton.OK);
                        });
                        break;
                    case Update_Status_Code.NewVersionAvailable:
                        Log.Instance.Write("A new version of the NERVA CLI tools are available");
                        Application.Instance.AsyncInvoke(() =>
                        {
                            if (MessageBox.Show(this, "An update to the NERVA CLI tools is available.\r\nWould you like to download it?", "Nerva Updater",
                                MessageBoxButtons.YesNo, MessageBoxType.Question, MessageBoxDefaultButton.Yes) == DialogResult.Yes)
                            {
                                VersionManager.GetVersionInfo(UpdateManager.CurrentLocalVersion, () =>
                                {
                                    TaskContainer tc = null;
                                    tc = Helpers.TaskFactory.Instance.RunTask("downloadingupdate", "Downloading Update", () =>
                                    {
                                        Cli.Instance.Daemon.StopCrashCheck();
                                        Cli.Instance.Wallet.StopCrashCheck();

                                        Cli.Instance.Daemon.ForceClose();
                                        Cli.Instance.Wallet.ForceClose();

                                        Thread.Sleep(5000);

                                        string link = null;

                                        switch (OS.Type)
                                        {
                                            case OS_Type.Windows:
                                                link = VersionManager.VersionInfo.WindowsLink;
                                                break;
                                            case OS_Type.Linux:
                                                link = VersionManager.VersionInfo.LinuxLink;
                                                break;
                                            case OS_Type.Mac:
                                                link = VersionManager.VersionInfo.MacLink;
                                                break;
                                        }

                                        VersionManager.DownloadFile(link, (DownloadProgressChangedEventArgs ea) =>
                                        {
                                            Application.Instance.AsyncInvoke(() =>
                                            {
                                                float max = (float)ea.TotalBytesToReceive;
                                                float val = (float)ea.BytesReceived;

                                                double percent = Math.Round(val / max, 1);
                                                tc.Description = $"Downloading Update {percent}%";
                                            });

                                        }, (bool success, string dest) =>
                                        {
                                            Application.Instance.AsyncInvoke(() =>
                                            {
                                                if (!success)
                                                {
                                                    MessageBox.Show(Application.Instance.MainForm, "An error occured while downloading/extracting the NERVA CLI tools.\r\n" +
                                                    "Please refer to the log file and try again later", "Request Failed", MessageBoxButtons.OK, MessageBoxType.Error, MessageBoxDefaultButton.OK);
                                                }
                                            });

                                            Cli.Instance.Daemon.ResumeCrashCheck();
                                            Cli.Instance.Wallet.ResumeCrashCheck();
                                        });
                                    });
                                });
                            }
                        });
                        break;
                    default:
                        Log.Instance.Write(Log_Severity.Error, "An error occurred checking for updates.");
                        MessageBox.Show(this, "An error occurred checking for updates.", "Nerva Updater",
                                MessageBoxButtons.OK, MessageBoxType.Error, MessageBoxDefaultButton.OK);
                        break;
                }
            });
            
        }

        protected void quit_Clicked(object sender, EventArgs e)
        {
            Application.Instance.Quit();
        }
    }
}