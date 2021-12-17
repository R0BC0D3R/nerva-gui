using System.Diagnostics;
using Nerva.Desktop.Config;
using Nerva.Desktop.Helpers;

namespace Nerva.Desktop.CLI
{
    public static class DaemonProcess
    {
        public static void ForceClose()
        {
            ProcessManager.Kill(FileNames.NERVAD);
        }

        public static bool IsRunning()
        {
            Process process = null;
            ProcessManager.IsRunning(FileNames.NERVAD, out process);

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
            return GenerateCommandLine(string.Empty);
        }

        public static string GenerateCommandLine(string extraParams)
        {
            string parameters = ProcessManager.GenerateCommandLine(FileNames.DaemonPath, Configuration.Instance.Daemon.Rpc);

            if (Configuration.Instance.Daemon.AutoStartMining)
            {
                string ma = Configuration.Instance.Daemon.MiningAddress;

                Logger.LogDebug("DP.GCL", $"Enabling startup mining @ {ma}");
                parameters += $" --start-mining {ma} --mining-threads {Configuration.Instance.Daemon.MiningThreads}";
            }
            
#if UNIX
            parameters += " --detach";
#endif

            parameters += $" {Configuration.Instance.Daemon.AdditionalArguments}";

            if(!string.IsNullOrEmpty(extraParams))
            {
                parameters += " " + extraParams;
            }

            return parameters;
        }
    }
}