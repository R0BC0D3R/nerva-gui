using System.Diagnostics;
using Nerva.Desktop.Config;
using Nerva.Desktop.Helpers;

namespace Nerva.Desktop.CLI
{
    public static class WalletProcess
    {
        public static void ForceClose()
        {
            ProcessManager.Kill(FileNames.RPC_WALLET);
        }

        public static bool IsRunning()
        {
            Process process = null;
            ProcessManager.IsRunning(FileNames.RPC_WALLET, out process);

            if(process != null)
            {
                return true;
            }
            else 
            {
                return false;
            }
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