using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using AngryWasp.Logger;
using Nerva.Toolkit.Config;
using Nerva.Toolkit.Helpers;

namespace Nerva.Toolkit.CLI
{
    public class DaemonCliTool : CliTool<DaemonInterface>
    {
        public override string FullExeName => FileNames.GetFormattedCliExeName(FileNames.NERVAD);

        public DaemonCliTool(Cli controller) : base(controller, new DaemonInterface()) { }

        public override void Create(string exe, string args)
        {
            Log.Instance.Write($"Starting process {exe} {args}");

            Process proc = Process.Start(new ProcessStartInfo(exe, args)
            {
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            });

            controller.DoProcessStarted(exe, proc);
        }

        bool threadRunning = false;

        public void StartCrashCheck()
        {
            Thread t = new Thread(new ThreadStart(() =>
            {
                threadRunning = true;
                
                while (doCrashCheck)
                {
                    try
                    {
                        Process p = null;

                        if (!doCrashCheck)
                            break;

                        if (!controller.IsAlreadyRunning(BaseExeName, out p))
                        {
                            if (!doCrashCheck)
                                break;

                            ManageCliProcess();

                            if (!doCrashCheck)
                                break;

                            Create(FileNames.GetCliExePath(BaseExeName), GenerateCommandLine());
                            Log.Instance.Write($"Connecting to process {BaseExeName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Instance.WriteFatalException(ex);
                    }

                    Thread.Sleep(Constants.ONE_SECOND);
                }

                threadRunning = false;
            }));

            t.Start();
        }

        public void ResumeCrashCheck()
        {
            doCrashCheck = true;

            if (!threadRunning)
                StartCrashCheck();
        }

        public void StopCrashCheck()
        {
            doCrashCheck = false;
        }

        public override string GenerateCommandLine()
        {
            string a = GetBaseCommandLine(BaseExeName, Configuration.Instance.Daemon.Rpc);

            if (Configuration.Instance.Daemon.AutoStartMining)
            {
                string ma = Configuration.Instance.Daemon.MiningAddress;

                Log.Instance.Write($"Enabling startup mining @ {ma}");
                a += $" --start-mining {ma} --mining-threads {Configuration.Instance.Daemon.MiningThreads}";
            }
            
            if (OS.IsUnix())
                a += " --detach";

            a += " --log-level 2";
            a += $" {Configuration.Instance.Daemon.AdditionalArguments}";

            return a;
        }

        public override void ManageCliProcess()
        {
            bool reconnect = Configuration.Instance.ReconnectToDaemonProcess;
            bool createNew = Configuration.Instance.NewDaemonOnStartup;

            controller.ManageCliProcesses(BaseExeName, reconnect, ref createNew);

            Configuration.Instance.NewDaemonOnStartup = createNew;
        }
    }
}