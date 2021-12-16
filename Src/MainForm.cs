using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Eto.Forms;
using Eto.Drawing;
using Nerva.Rpc;
using Nerva.Rpc.Daemon;
using Nerva.Rpc.Wallet;
using Nerva.Desktop.CLI;
using Nerva.Desktop.Content.Dialogs;
using Nerva.Desktop.Content.Wizard;
using Nerva.Desktop.Helpers;
using Configuration = Nerva.Desktop.Config.Configuration;

namespace Nerva.Desktop
{
    public partial class MainForm : Form
    {
        public System.Timers.Timer masterTimer = null;
        public bool killMasterProcess = false;

        ulong lastTxHeight = 0;
        private bool isInitialDaemonConnectionSuccess = false;

        public MainForm(bool newConfig)
        {
            try 
            {
                SuspendLayout();
                ConstructLayout();
                ResumeLayout();

                Application.Instance.Initialized += (s, e) =>
                {
                    StartUpdateTaskList();

                    StartMasterUpdateProcess();

                    Configuration.SetMissingElements();
                    bool needSetup = newConfig || !FileNames.DirectoryContainsCliTools(Configuration.Instance.ToolsPath);

                    if (needSetup)
                    {
                        new SetupWizard().Run();
                    }

                    Configuration.Save();                    
                };

                this.Closing += (s, e) => MainFormClosing();
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("MF.C", ex, true);
            }
        }

        public void MainFormClosing()
        {
            try
            {
                killMasterProcess = true;
                masterTimer.Stop();

                Program.Shutdown(false);
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("MF.MFC", ex, false);
            }
        }

        public void StartUpdateTaskList()
        {
            AsyncTaskContainer updateTaskListTask = new AsyncTaskContainer();

            try
            {
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
            catch (Exception ex)
            {
                ErrorHandler.HandleException("MF.SUTL", ex, false);
            }
        }

        public void StartMasterUpdateProcess()
        {
            try
            {
                Logger.LogDebug("MF.SMUP", "Start Master Update Process");

                if(masterTimer == null)
                {
                    masterTimer = new System.Timers.Timer();
                    masterTimer.Interval = 5000;
                    masterTimer.Elapsed += (s, e) => MasterUpdateProcess();
                    masterTimer.Start();

                    Logger.LogDebug("MF.SMUP", "Master timer will start in 5 seconds");
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("MF.SMUP", ex, false);
            }
        }

        private void MasterUpdateProcess()
        {
            try
            {
                if (masterTimer != null)
                {
                    masterTimer.Stop();
                }

                // If kill master process is issued at any point, skip everything else and do not restrt master timer

                if(!killMasterProcess)
                {
                    KeepDaemonRunning();
                }


                if(!killMasterProcess)
                {
                    KeepWalletProcessRunning();
                }


                // Update UI
                if(!killMasterProcess)
                {
                    DaemonUiUpdate();
                }

                if(!killMasterProcess && isInitialDaemonConnectionSuccess)
                {
                    WalletUiUpdate();
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("MF.MUP", ex, false);
            }
            finally
            {
                // Restart timer
                if (masterTimer == null)
                {
                    Logger.LogError("MF.MUP", "Timer is NULL. Recreating. Why?");
                    masterTimer = new System.Timers.Timer();
                    masterTimer.Interval = 5000;
                    masterTimer.Elapsed += (s, e) => MasterUpdateProcess();
                }

                if(!killMasterProcess)
                {
                    masterTimer.Start();
                }
            }
        }

        private void KeepDaemonRunning()
        {
            try
            {
                Process p = null;

                if (!ProcessManager.IsRunning(FileNames.NERVAD, out p))
                {
                    if(FileNames.DirectoryContainsCliTools(Configuration.Instance.ToolsPath))
                    {
                        DaemonProcess.ForceClose();
                        Logger.LogDebug("MF.KDR", "Starting daemon process");
                        ProcessManager.StartExternalProcess(FileNames.DaemonPath, DaemonProcess.GenerateCommandLine());
                        isInitialDaemonConnectionSuccess = false;
                    }
                    else 
                    {
                        Logger.LogDebug("MF.KDR", "CLI tools not found");
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("MF.KDR", ex, false);
            }
        }

        private void KeepWalletProcessRunning()
        {
            try
            {
                Process p = null;

                if (!ProcessManager.IsRunning(FileNames.RPC_WALLET, out p))
                {
                    if(FileNames.DirectoryContainsCliTools(Configuration.Instance.ToolsPath))
                    {
                        WalletProcess.ForceClose();
                        Logger.LogDebug("MF.KWPR", "Starting wallet process");
                        ProcessManager.StartExternalProcess(FileNames.RpcWalletPath, WalletProcess.GenerateCommandLine());
                    }
                    else 
                    {
                        Logger.LogDebug("MF.KWPR", "CLI tools not found");
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("MF.KWPR", ex, false);
            }
        }
        public void WalletUiUpdate()
        {
            try
            {
                WalletRpc.GetAccounts((GetAccountsResponseData ra) =>
                {
                    Application.Instance.AsyncInvoke( () =>
                    {
                        lblWalletStatus.Text = $"Account(s): {ra.Accounts.Count}  | Balance: {Conversions.FromAtomicUnits(ra.TotalBalance)} XNV";
                        balancesPage.Update(ra);
                    });
                }, WalletUpdateFailed);

                WalletRpc.GetTransfers(lastTxHeight, (GetTransfersResponseData rt) =>
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
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("MF.WUU", ex, false);
            }
        }

        void WalletUpdateFailed(RequestError e)
        {
            try
            {
                if (e.Code != -13) //skip messages about not having a wallet open
                {
                    Logger.LogError("MF.WUF", $"Wallet update failed, Code {e.Code}: {e.Message}");
                }

                Application.Instance.AsyncInvoke(() =>
                {
                    lblWalletStatus.Text = "Wallet Offline - See Wallet Menu";
                    lastTxHeight = 0;
                    balancesPage.Update(null);
                    transfersPage.Update(null);
                });
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("MF.WUF", ex, false);
            }
        }

        public void DaemonUiUpdate()
        {
            try
            {
                if(!isInitialDaemonConnectionSuccess)
                {
                    Application.Instance.Invoke(() =>
                    {
                        lblDaemonStatus.Text = "Trying to establish connection with daemon...";                                    
                        daemonPage.UpdateInfo(null);
                    });                    
                }

                bool isGetInfoSuccessful = false;
                DaemonRpc.GetInfo((GetInfoResponseData r) =>
                {
                    Application.Instance.Invoke(() =>
                    {
                        if(isInitialDaemonConnectionSuccess == false)
                        {
                            // This will be used to get rid of establishing connection message and to StartWalletUiUpdate 
                            isInitialDaemonConnectionSuccess = true;
                            daemonPage.btnStartStopMining.Enabled = true;
                        }

                        isGetInfoSuccessful = true;

                        daemonPage.UpdateInfo(r);

                        lblDaemonStatus.Text = $"Height: {r.Height} | Connections: {r.OutgoingConnectionsCount}(out)+{r.IncomingConnectionsCount}(in)";                        

                        if (r.TargetHeight != 0 && r.Height < r.TargetHeight)
                            lblDaemonStatus.Text += " | Syncing";
                        else
                            lblDaemonStatus.Text += " | Sync OK";

                        lblVersion.Text = $"Version: {r.Version}";
                    });
                }, (RequestError e) =>
                {
                    Application.Instance.Invoke(() =>
                    {
                        lblDaemonStatus.Text = "Daemon not responding";
                        daemonPage.UpdateInfo(null);
                    });
                });

                if (isGetInfoSuccessful)
                {
                    DaemonRpc.GetConnections((List<GetConnectionsResponseData> r) =>
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

                    DaemonRpc.MiningStatus((MiningStatusResponseData r) =>
                    {
                        Application.Instance.Invoke(() =>
                        {
                            daemonPage.UpdateMinerStatus(r);
                        });
                    }, (RequestError e) =>
                    {
                        Application.Instance.Invoke(() =>
                        {
                            daemonPage.UpdateMinerStatus(null);
                        });
                    });
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("MF.DUU", ex, false);
            }
        }

        protected void daemon_ToggleMining_Clicked(object sender, EventArgs e)
        {
            try
            {
                GlobalMethods.StartStopMining();
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("MF.DTMC", ex, true);
            }
        }

        protected void daemon_Restart_Clicked(object sender, EventArgs e)
        {
            try
            {
                //Log the restart and kill the daemon
                Logger.LogDebug("MF.DRC", "Restarting daemon");
                DaemonRpc.StopDaemon();
                DaemonProcess.ForceClose();
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("MF.DRC", ex, true);
            }
        }

        protected void wallet_New_Clicked(object sender, EventArgs e)
        {
            try
            {
                NewWalletDialog d = new NewWalletDialog();
                if (d.ShowModal() == DialogResult.Ok)
                {
                    Helpers.TaskFactory.Instance.RunTask("createwallet", $"Creating wallet", () =>
                    {
                        if (d.HwWallet)
                        {
                            WalletRpc.CreateHwWallet(d.Name, d.Password,
                                (CreateHwWalletResponseData result) =>
                            {
                                CreateSuccess(result.Address);
                            }, CreateError);
                        }
                        else
                        {
                            WalletRpc.CreateWallet(d.Name, d.Password,
                                (CreateWalletResponseData result) =>
                            {
                                CreateSuccess(result.Address);
                            }, CreateError);
                        }
                    
                    });
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("MF.WNC", ex, true);
            }
        }

        protected void wallet_Open_Clicked(object sender, EventArgs e)
        {
            try
            {
                OpenWalletDialog d = new OpenWalletDialog();
                if (d.ShowModal() == DialogResult.Ok)
                {
                    OpenNewWallet(d.Name, d.Password);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("MF.OWC", ex, true);
            }
        }

        protected void wallet_Import_Clicked(object sender, EventArgs e)
        {
            try
            {
                ImportWalletDialog d = new ImportWalletDialog();
                if (d.ShowModal() == DialogResult.Ok)
                {
                    Helpers.TaskFactory.Instance.RunTask("importwallet", $"Importing wallet", () =>
                    {
                        switch (d.ImportType)
                        {
                            case Import_Type.Key:
                                WalletRpc.RestoreWalletFromKeys(d.Name, d.Address, d.ViewKey, d.SpendKey, d.Password, d.Language,
                                    (RestoreWalletFromKeysResponseData result) =>
                                {
                                    CreateSuccess(result.Address);
                                }, CreateError);
                            break;
                            case Import_Type.Seed:
                                WalletRpc.RestoreWalletFromSeed(d.Name, d.Seed, d.SeedOffset, d.Password, d.Language,
                                (RestoreWalletFromSeedResponseData result) =>
                                {
                                    CreateSuccess(result.Address);
                                }, CreateError);
                            break;
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("MF.IWC", ex, true);
            }
        }

        private void OpenNewWallet(string name, string password)
        {
            try
            {
                Application.Instance.AsyncInvoke(() =>
                {
                    lblWalletStatus.Text = "Wallet Offline";
                    lastTxHeight = 0;
                    balancesPage.Update(null);
                    transfersPage.Update(null);
                });

                WalletRpc.CloseWallet(() =>
                {
                    Helpers.TaskFactory.Instance.RunTask("openwallet", $"Opening wallet {name}", () =>
                    {
                        WalletRpc.OpenWallet(name, password, () => {
                            Logger.LogDebug("MF.ONW", $"Opened wallet {name}");
                        }, OpenError);
                    });
                }, (RequestError error) =>
                {
                    if (error.Code != -13)
                    {
                        Logger.LogError("MF.ONW", $"Error closing wallet: {error.Message}");
                    }

                    Helpers.TaskFactory.Instance.RunTask("openwallet", $"Opening wallet {name}", () =>
                    {
                        WalletRpc.OpenWallet(name, password, () => {
                            Logger.LogDebug("MF.ONW", $"Opened wallet {name}");
                        }, OpenError);
                    });
                });
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("MF.ONW", ex, false);
            }
        }

        private void CreateSuccess(string address)
        {
            try
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
            catch (Exception ex)
            {
                ErrorHandler.HandleException("MF.CS", ex, false);
            }
        }

        private void CreateError(RequestError error)
        {
            try
            {
                Application.Instance.AsyncInvoke( () =>
                {
                    MessageBox.Show(Application.Instance.MainForm, $"Wallet creation failed.\r\nError Code: {error.Code}\r\n{error.Message}", "Create Wallet",
                    MessageBoxButtons.OK, MessageBoxType.Error, MessageBoxDefaultButton.OK);
                });
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("MF.CE", ex, false);
            }
        }

        private void OpenError(RequestError error)
        {
            try
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
            catch (Exception ex)
            {
                ErrorHandler.HandleException("MF.OE", ex, false);
            }
        }

        protected void wallet_Store_Clicked(object sender, EventArgs e)
        {
            try
            {
                Helpers.TaskFactory.Instance.RunTask("store", $"Saving wallet information", () =>
                {
                    Logger.LogDebug("MF.WSeC", "Saving wallet");
                    WalletRpc.Store();

                    Application.Instance.AsyncInvoke(() =>
                    {
                        MessageBox.Show(this, "Wallet Save Complete", "NERVA Wallet", MessageBoxButtons.OK,
                            MessageBoxType.Information, MessageBoxDefaultButton.OK);
                    });
                });
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("MF.WSeC", ex, true);
            }
        }

        protected void wallet_Stop_Clicked(object sender, EventArgs e)
        {
            try
            {
                Helpers.TaskFactory.Instance.RunTask("closewallet", "Closing the wallet", () => 
                {
                    Logger.LogDebug("MF.WSpC","Closing wallet");
                    lastTxHeight = 0;

                    WalletRpc.CloseWallet(null, null);

                    Application.Instance.AsyncInvoke( () =>
                    {
                        balancesPage.Update(null);
                        transfersPage.Update(null);
                        lblWalletStatus.Text = "Wallet Offline";
                    });
                });
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("MF.WSpC", ex, true);
            }
        }

        protected void wallet_RescanSpent_Clicked(object sender, EventArgs e)
        {
            try
            {
                Helpers.TaskFactory.Instance.RunTask("rescanspent", $"Rescanning spent outputs", () =>
                {
                    Logger.LogDebug("MF.WRSC", "Rescanning spent outputs");
                    if (!WalletRpc.RescanSpent())
                    {
                        Logger.LogDebug("MF.WRSC", "Rescanning spent outputs failed");
                    }
                    else
                    {
                        Logger.LogDebug("MF.WRSC", "Rescanning spent outputs success");
                    }

                    Application.Instance.AsyncInvoke(() =>
                    {
                        MessageBox.Show(this, "Rescanning spent outputs complete", "Rescan Spent",
                            MessageBoxButtons.OK, MessageBoxType.Information, MessageBoxDefaultButton.OK);
                    });
                });
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("MF.WRSC", ex, true);
            }
        }

        protected void wallet_RescanBlockchain_Clicked(object sender, EventArgs e)
        {
            try
            {
                Helpers.TaskFactory.Instance.RunTask("rescanchain", $"Rescanning the blockchain", () =>
                {
                    Logger.LogDebug("MF.WRBC", "Rescanning blockchain");
                    if (!WalletRpc.RescanBlockchain())
                    {
                        Logger.LogError("MF.WRBC", "Rescanning blockchain failed");
                    }
                    else
                    {
                        Logger.LogDebug("MF.WRBC", "Rescanning blockchain success");
                    }

                    Application.Instance.AsyncInvoke(() =>
                    {
                        MessageBox.Show(this, "Rescanning blockchain complete", "Rescan Blockchain",
                            MessageBoxButtons.OK, MessageBoxType.Information, MessageBoxDefaultButton.OK);
                    });
                });
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("MF.WRBC", ex, true);
            }
        }

        protected void wallet_Keys_View_Clicked(object sender, EventArgs e)
        {
            try
            {
                new DisplayKeysDialog().ShowModal();
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("MF.WKVC", ex, true);
            }
        }

        protected void wallet_Account_Create_Clicked(object sender, EventArgs e)
        {
            try
            {
                TextDialog d = new TextDialog("Enter Account Name", false);
                if (d.ShowModal() == DialogResult.Ok)
                {
                    Helpers.TaskFactory.Instance.RunTask("createwallet", "Creating new wallet", () =>
                    {
                        WalletRpc.CreateAccount(d.Text, (CreateAccountResponseData r) =>
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
            catch (Exception ex)
            {
                ErrorHandler.HandleException("MF.WACC", ex, true);
            }
        }

        protected void about_Clicked(object sender, EventArgs e)
        {
            try
            {
                AboutDialog ad = new AboutDialog();

                ad.ProgramName = "NERVA Desktop Wallet and Miner";
                ad.ProgramDescription = "NERVA Desktop Wallet and Miner is a one-step solution for all your NERVA needs.\r\n\r\nManage your NERVA funds - create/open wallet, check your balance, transfer funds and check history.\r\n\r\nMine on NERVA network - mine new coins and help protect NERVA network with your spare resources.\r\n\r\n1 CPU = 1 VOTE";
                string[] names = Assembly.GetExecutingAssembly().GetManifestResourceNames();
                ad.Title = "About NERVA Desktop Wallet and Miner";
                ad.License = "Copyright © 2017 - 2021 NERVA Project";
                ad.Version = $"GUI: {Version.VERSION}\r\nCLI: {daemonPage.version}";
                ad.Logo = new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream("NERVA-Logo.png"));
                ad.Website = new Uri("https://nerva.one");
                ad.WebsiteLabel = "https://nerva.one";
                
                ad.ShowDialog(this);
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("MF.AC", ex, true);
            }
        }

        protected void debugFolderCommand_Clicked(object sender, EventArgs e)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = Configuration.StorageDirectory,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("MF.DFC", ex, true);
            }
        }

        protected void discord_Clicked(object sender, EventArgs e)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "https://discord.gg/ufysfvcFwe",
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("MF.DC", ex, true);
            }
        }

        protected void twitter_Clicked(object sender, EventArgs e)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "https://twitter.com/nervacurrency",
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("MF.TC", ex, true);
            }
        }

        protected void reddit_Clicked(object sender, EventArgs e)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "https://www.reddit.com/r/Nerva/",
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("MF.RC", ex, true);
            }
        }

        protected void file_Preferences_Clicked(object sender, EventArgs e)
        {
            try
            {
                PreferencesDialog d = new PreferencesDialog();
                if (d.ShowModal() == DialogResult.Ok)
                {
                    Configuration.Save();

                    if (d.RestartCliRequired)
                    {
                        //if thge daemon has to be restarted, there is a good chance the wallet has to be restarted, so just do it
                        MessageBox.Show(this, "NERVA backend will now restart to apply your changes", "NERVA Preferences",
                            MessageBoxButtons.OK, MessageBoxType.Information, MessageBoxDefaultButton.OK);

                        Logger.LogDebug("MF.FPC", "Restarting CLI");

                        Helpers.TaskFactory.Instance.RunTask("restartcli", "Restarting the CLI", () =>
                        {
                            DaemonProcess.ForceClose();
                            WalletProcess.ForceClose();

                            Application.Instance.AsyncInvoke( () =>
                            {
                                daemonPage.UpdateInfo(null);
                                daemonPage.UpdateConnections(null);
                                daemonPage.UpdateMinerStatus(null);
                                balancesPage.Update(null);
                                transfersPage.Update(null);
                            });
                        });
                    }
                    else
                    {
                        if (d.RestartMinerRequired)
                        {
                            DaemonRpc.StopMining();
                            DaemonRpc.StartMining();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("MF.FPC", ex, true);
            }
        }

        protected void file_UpdateCheck_Clicked(object sender, EventArgs e)
        {
            try
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
                            if (MessageBox.Show(Application.Instance.MainForm, "New version is available:\n\r\n\r" + cliMsg, "NERVA Updater", 
                                MessageBoxButtons.OK, MessageBoxType.Information, MessageBoxDefaultButton.OK) == DialogResult.Yes)
                            {
                                // TODO: Doesn't work. Disable for now
                                /*
                                if (UpdateManager.CliUpdateInfo != null && UpdateManager.CliUpdateInfo.UpdateStatus == Update_Status_Code.NewVersionAvailable)
                                    Helpers.TaskFactory.Instance.RunTask("dlcliupdate", $"Downloading CLI update", () =>
                                    {
                                        UpdateManager.DownloadUpdate(Update_Type.CLI, UpdateManager.CliUpdateInfo.DownloadLink, null, (b, s) =>
                                        {
                                            DisplayUpdateResult(b, s);
                                        });
                                    });
                                */
                            }
                        }
                        else
                            MessageBox.Show(Application.Instance.MainForm, $"You are up to date", "NERVA Updater", 
                                MessageBoxButtons.OK, MessageBoxType.Information);
                    });
                    
                });
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("MF.FUCC", ex, true);
            }
        }

        private void DisplayUpdateResult(bool ok, string file)
        {
            try
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
            catch (Exception ex)
            {
                ErrorHandler.HandleException("MF.DUR", ex, false);
            }
        }

        protected void quit_Clicked(object sender, EventArgs e)
        {
            try
            {
                Application.Instance.Quit();
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("MF.QC", ex, true);
            }
        }
    }
}