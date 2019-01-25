using System.Diagnostics;
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

            controller.DoProcessStarted(exe,  proc);
            proc.WaitForExit();
        }

        public override string GenerateCommandLine()
        {
            string a = GetBaseCommandLine(BaseExeName, Configuration.Instance.Wallet.Rpc);
            a += " --disable-rpc-login";
            a += $" --wallet-dir {Configuration.Instance.Wallet.WalletDir}";
            if (!string.IsNullOrEmpty(Configuration.Instance.Wallet.LastOpenedWallet))
                a += $" --wallet-file {Configuration.Instance.Wallet.LastOpenedWallet}";
    
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