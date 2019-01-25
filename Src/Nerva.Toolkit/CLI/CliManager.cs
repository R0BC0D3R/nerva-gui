using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using AngryWasp.Logger;
using Nerva.Toolkit.Config;
using Nerva.Toolkit.Helpers;
using AngryWasp.Helpers;
using Nerva.Toolkit.Content.Dialogs;
using Eto.Forms;
using System.Threading.Tasks;
using Nerva.Toolkit.Content.Wizard;

namespace Nerva.Toolkit.CLI
{
    public abstract class CliTool<T> where T : CliInterface, new()
    {
        protected bool doCrashCheck = true;
        protected T cliInterface;
        protected Cli controller = null;

        public T Interface => cliInterface;

        public abstract string FullExeName { get; }

        public string BaseExeName => Path.GetFileNameWithoutExtension(FullExeName);

        public CliTool(Cli controller, T cliInterface)
        {
            this.controller = controller;
            this.cliInterface = cliInterface;
        }

        protected string GetBaseCommandLine(string exe, RpcDetails d)
        {
            string e = FileNames.GetCliExePath(exe);

            string arg = $"--log-file {controller.CycleLogFile(e)}";

            if (Configuration.Instance.Testnet)
            {
                Log.Instance.Write("Connecting to testnet");
                arg += " --testnet";
            }

            arg += $" --rpc-bind-port {d.Port}";

            // TODO: Uncomment to enable rpc user:pass.
            // string ip = d.IsPublic ? $" --rpc-bind-ip 0.0.0.0 --confirm-external-bind" : $" --rpc-bind-ip 127.0.0.1";
            // arg += $"{ip} --rpc-login {d.Login}:{d.Pass}";

            return arg;
        }

        public virtual void ForceClose()
        {
            controller.KillCliProcesses(BaseExeName);
        }

        public abstract void Create(string exe, string args);
        public abstract void ManageCliProcess();
        public abstract string GenerateCommandLine();
    }

    public class Cli
    {
        private static Cli instance;

        public static Cli Instance
        {
            get
            {
                if (instance == null)
                    instance = new Cli();

                return instance;
            }
        }

        private DaemonCliTool daemon;
        private WalletCliTool wallet;

        public DaemonCliTool Daemon => daemon;

        public WalletCliTool Wallet => wallet;

        public void StartDaemon()
        {
            daemon = new DaemonCliTool(this);
            daemon.StartCrashCheck();
        }

        public void StartWallet()
        {
            wallet = new WalletCliTool(this);
            wallet.StartCrashCheck();
        }

        public string CycleLogFile(string path)
        {
            string logFile = path + ".log";
            string oldLogFile = logFile + ".old";

            try
            {
                if (File.Exists(oldLogFile))
                    File.Delete(oldLogFile);

                if (File.Exists(logFile))
                    File.Move(logFile, oldLogFile);
            }
            catch (Exception)
            {
                logFile = FileHelper.RenameDuplicateFile(logFile);
                Log.Instance.Write(Log_Severity.Warning, "Cannot cycle log file. New log will be written to {0}", logFile);
                return logFile;
            }

            return logFile;
        }

        public bool IsReady(string exe)
        {
            var pl = CliInterface.GetRunningProcesses(exe);
            if (pl.Count == 0)
                return false;

            Process dp = pl[0];
            if (dp == null || dp.HasExited)
                return false;

            double ms = (DateTime.Now - dp.StartTime).TotalMilliseconds;
            if (ms < Constants.FIVE_SECONDS)
                return false;

            return true;
        }

        public bool IsAlreadyRunning(string exe, out Process p)
        {
            p = null;

            try
            {
                var pl = CliInterface.GetRunningProcesses(exe);
                if (pl.Count == 0)
                    return false;

                p = pl[0];

                if (p == null || p.HasExited)
                {
                    Log.Instance.Write(Log_Severity.Warning, "CLI tool {0} exited unexpectedly. Restarting", exe);
                    p = null;
                    return false;
                }
                else
                    return true;
            }
            catch (Exception ex)
            {
                Log.Instance.Write(Log_Severity.Warning, ex.Message);
                return false;
            }
        }

        public void ManageCliProcesses(string exe, bool reconnect, ref bool createNew)
        {
            var p = CliInterface.GetRunningProcesses(exe);

            if (p.Count > 0)
            {
                //Reconnect to an existing process if only one is running
                //If more than 1, we have to start again, because we cannot be sure which one to connec to
                if (p.Count > 1 || !reconnect || createNew)
                    KillCliProcesses(exe);
                    
                createNew = false;
            }
        }

        public void KillCliProcesses(string exe)
        {
            var pl = CliInterface.GetRunningProcesses(exe);

            if (pl.Count > 0)
            {
                foreach (Process p in pl)
                {
                    Log.Instance.Write(Log_Severity.Warning, "Killing running instance of {0} with id {1}", p.ProcessName, p.Id);
                    p.Kill();
                    p.WaitForExit();
                }

                pl = CliInterface.GetRunningProcesses(exe);

                if (pl.Count > 0)
                {
                    Log.Instance.Write(Log_Severity.Fatal, "There are unknown proceses running. Please kill all processes");
                    return;
                }
            }
            else
                Log.Instance.Write("No processes to kill");
        }

        public void DoProcessStarted(string exe, Process proc)
        {
            //Don't worry about it if running the startup wizard
            if (!SetupWizard.IsRunning)
                DoCliStartup(exe);
        }

        #region Startup events

        private void DoCliStartup(string exe)
        {
            switch (FileNames.GetCliExeBaseName(exe))
            {
                case FileNames.NERVAD:
                    UpdateCheck();
                    break;
            }
        }

        private void UpdateCheck()
        {
            if (!Configuration.Instance.CheckForUpdateOnStartup)
                return;

            Helpers.TaskFactory.Instance.RunTask("updatecheck", "Checking for updates", () =>
            {
                while (!IsReady(daemon.BaseExeName))
                    Thread.Sleep(Constants.FIVE_SECONDS);

                UpdateManager.CheckForCliUpdates();

                switch (UpdateManager.UpdateStatus)
                {
                    case Update_Status_Code.UpToDate:
                        Log.Instance.Write("NERVA CLI tools are up to date");
                        break;
                    case Update_Status_Code.NewVersionAvailable:
                        Log.Instance.Write("A new version of the NERVA CLI tools are available");
                        break;
                    default:
                        Log.Instance.Write(Log_Severity.Error, "An error occurred checking for updates.");
                        break;
                }

                Log.Instance.Write("Update check complete");
            });
        }

        #endregion
    }
}