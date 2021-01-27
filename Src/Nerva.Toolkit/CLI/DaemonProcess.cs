using System;
using System.Diagnostics;
using System.Threading;
using AngryWasp.Logger;
using Nerva.Toolkit.Config;
using Nerva.Toolkit.Helpers;

namespace Nerva.Toolkit.CLI
{
    public static class DaemonProcess
    {
        private static bool threadRunning = false;
        private static bool doCrashCheck = true;

        public static void StartCrashCheck()
        {
            if (threadRunning)
            {
                Log.Instance.Write(Log_Severity.Warning, "Attempt to start wallet crash check when already running");
                return;
            }

            Thread t = new Thread(new ThreadStart(() =>
            {
                threadRunning = true;
                
                while (doCrashCheck)
                {
                    try
                    {
                        Process p = null;

                        if (!ProcessManager.IsRunning(FileNames.NERVAD, out p))
                        {
                            ForceClose();
                            Log.Instance.Write("Starting daemon process");
                            ProcessManager.StartExternalProcess(FileNames.DaemonPath, GenerateCommandLine());
                            Thread.Sleep(Constants.ONE_SECOND * 30);
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

        public static void ResumeCrashCheck()
        {
            doCrashCheck = true;

            if (!threadRunning)
                StartCrashCheck();
        }

        public static void StopCrashCheck()
        {
            doCrashCheck = false;
        }

        public static void ForceClose()
        {
            ProcessManager.Kill(FileNames.NERVAD);
        }

        public static string GenerateCommandLine()
        {
            string a = ProcessManager.GenerateCommandLine(FileNames.DaemonPath, Configuration.Instance.Daemon.Rpc);

            if (Configuration.Instance.Daemon.AutoStartMining)
            {
                string ma = Configuration.Instance.Daemon.MiningAddress;

                Log.Instance.Write($"Enabling startup mining @ {ma}");
                a += $" --start-mining {ma} --mining-threads {Configuration.Instance.Daemon.MiningThreads}";
            }
            
#if UNIX
            a += " --detach";
#endif

            a += $" {Configuration.Instance.Daemon.AdditionalArguments}";

            return a;
        }
    }
}