using System.IO;
using Nerva.Desktop.Config;

namespace Nerva.Desktop.Helpers
{
    public static class FileNames
    {
#if WINDOWS
        public const string NERVAD = "nervad.exe";
        public const string RPC_WALLET = "nerva-wallet-rpc.exe";
#else
        public const string NERVAD = "nervad";
        public const string RPC_WALLET = "nerva-wallet-rpc";
#endif

        public static string DaemonPath 
        {
            get {
                if (string.IsNullOrEmpty(Configuration.Instance.ToolsPath))
                {
                    Logger.LogDebug("FN.DP", "ToolPath is null. Trying to use default...");
                    Configuration.SetMissingElements();

                    if(string.IsNullOrEmpty(Configuration.Instance.ToolsPath))
                    {
                        Logger.LogDebug("FN.DP", "ToolPath is null. Could not recover");
                    }
                }

                if (string.IsNullOrEmpty(NERVAD))
                {
                    Logger.LogError("FN.DP", "NERVAD is null and it should never be!");
                }

                return Path.Combine(Configuration.Instance.ToolsPath, NERVAD);
            }
        }

        public static string RpcWalletPath => Path.Combine(Configuration.Instance.ToolsPath, RPC_WALLET);

        public static bool DirectoryContainsCliTools(string path)
        {
            if (!Directory.Exists(path))
                return false;

            bool hasDaemon = File.Exists(Path.Combine(path, NERVAD));
            bool hasRpcWallet = File.Exists(Path.Combine(path, RPC_WALLET));

            return (hasRpcWallet && hasDaemon);
        }
    }
}