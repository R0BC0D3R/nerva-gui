using System;
using System.Diagnostics;
using System.Threading;
using AngryWasp.Logger;
using Nerva.Toolkit.Config;
using Nerva.Toolkit.Helpers;

namespace Nerva.Toolkit.CLI
{
    public static class WalletProcess
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

                        if (!ProcessManager.IsRunning(FileNames.RPC_WALLET, out p))
                        {
                            ForceClose();
                            Log.Instance.Write("Starting wallet process");
                            ProcessManager.StartExternalProcess(FileNames.RpcWalletPath, GenerateCommandLine());
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
            ProcessManager.Kill(FileNames.RPC_WALLET);
        }

        public static string GenerateCommandLine()
        {
            string a = ProcessManager.GenerateCommandLine(FileNames.RpcWalletPath, Configuration.Instance.Wallet.Rpc);
            a +=  " --disable-rpc-login";
            a += $" --wallet-dir \"{Configuration.Instance.Wallet.WalletDir}\"";
            a += $" --daemon-address 127.0.0.1:{Configuration.Instance.Daemon.Rpc.Port}";
            
            // TODO: Uncomment to enable rpc user:pass.
            // string ip = d.IsPublic ? $" --rpc-bind-ip 0.0.0.0 --confirm-external-bind" : $" --rpc-bind-ip 127.0.0.1";
            // a += $"{ip} --rpc-login {d.Login}:{d.Pass}";

            return a;
        }
    }
}