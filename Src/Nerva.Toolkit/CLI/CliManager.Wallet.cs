using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using AngryWasp.Logger;
using Nerva.Toolkit.Config;
using Nerva.Toolkit.Helpers;

namespace Nerva.Toolkit.CLI
{
    public class WalletCliTool : CliTool<WalletInterface>
    {
        public override string FullExeName => FileNames.GetFormattedCliExeName(FileNames.RPC_WALLET);

        public WalletCliTool(Cli controller) : base(controller, new WalletInterface()) { }

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

            proc.WaitForExit();
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
                        if (!doCrashCheck)
                            break;

                        Process p = null;
                        if (!controller.IsAlreadyRunning(BaseExeName, out p))
                        {
                            ManageCliProcess();
                            Create(FileNames.GetCliExePath(BaseExeName), GenerateCommandLine());
                            if (!doCrashCheck)
                                break;
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
            string a = GetBaseCommandLine(BaseExeName, Configuration.Instance.Wallet.Rpc);
            a +=  " --disable-rpc-login";
            a += $" --wallet-dir \"{Configuration.Instance.Wallet.WalletDir}\"";
            a += $" --daemon-address 127.0.0.1:{Configuration.Instance.Daemon.Rpc.Port}";
            return a;
        }

        public override void ManageCliProcess()
        {
            bool createNew = true;
            controller.ManageCliProcesses(BaseExeName, false, ref createNew);
        }
    }
}