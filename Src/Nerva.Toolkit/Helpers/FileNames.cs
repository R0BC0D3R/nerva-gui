using System.IO;
using AngryWasp.Logger;
using Nerva.Toolkit.Config;

namespace Nerva.Toolkit.Helpers
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
                    Log.Instance.Write(Log_Severity.Fatal, "ToolPath is null");

                if (string.IsNullOrEmpty(NERVAD))
                    Log.Instance.Write(Log_Severity.Fatal, "NERVAD is null");

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