using Nerva.Desktop.Helpers;

namespace Nerva.Desktop.Config
{
    public class Daemon
    {
        public RpcDetails Rpc { get; set; }

        public bool StopOnExit { get; set; }

        public bool AutoStartMining { get; set; }

        public string MiningAddress { get; set; }

        public int MiningThreads { get; set; }

        public string AdditionalArguments { get; set; }

        public static Daemon New(bool testnet)
        {
            return new Daemon
            {
                StopOnExit = false,
                AutoStartMining = false,
                MiningAddress = string.Empty,
                MiningThreads = 2,
                AdditionalArguments = string.Empty,

                Rpc = RpcDetails.New(testnet ? Constants.NERVAD_RPC_PORT_TESTNET : Constants.NERVAD_RPC_PORT_MAINNET)
            };
        }
    }
}