using AngryWasp.Logger;
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