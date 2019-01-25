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
            Log.Instance.Write("Starting process {0} {1}", exe, args);

            Process proc = Process.Start(new ProcessStartInfo(exe, args)
            {
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            });

            proc.WaitForExit();
        
            string n = Path.GetFileNameWithoutExtension(exe);
            var p = CliInterface.GetRunningProcesses(n);

            if (p.Count == 1)
                controller.DoProcessStarted(exe, p[0]);
            else
                Log.Instance.Write(Log_Severity.Fatal, "Error creating CLI process {0}", exe);
        }

        public override string GenerateCommandLine()
        {
            string a = GetBaseCommandLine(BaseExeName, Configuration.Instance.Wallet.Rpc);
            a +=  " --disable-rpc-login";
            a += $" --wallet-dir {Configuration.Instance.Wallet.WalletDir}";
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