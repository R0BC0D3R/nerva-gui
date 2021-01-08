using Nerva.Toolkit.Helpers;

namespace Nerva.Toolkit.Config
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
                MiningAddress = Constants.DEV_WALLET_ADDRESS,
                MiningThreads = 2,

                Rpc = RpcDetails.New(testnet ? Constants.NERVAD_RPC_PORT_TESTNET : Constants.NERVAD_RPC_PORT_MAINNET)
            };
        }
    }
}