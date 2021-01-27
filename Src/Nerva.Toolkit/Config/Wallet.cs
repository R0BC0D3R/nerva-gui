using System;
using System.IO;
using AngryWasp.Helpers;
using Nerva.Toolkit.Helpers;

namespace Nerva.Toolkit.Config
{
    public class Wallet
	{
        private string walletDir;

		public RpcDetails Rpc { get; set; }

        //Windows seems to not like it wehen the RPC wallet directory is missing the trailing slash
        //So we make sure it is there when getting and setting
        public string WalletDir
        {
            get { return walletDir; }

            set
            {
                if (!Directory.Exists(value))
                    Directory.CreateDirectory(value);

                walletDir = value;
            }
        }

        public int NumTransfersToDisplay { get; set; } = 25;

		public static Wallet New()
        {
            return new Wallet
            {
                WalletDir = Path.Combine(Configuration.StorageDirectory, "wallets"),
                Rpc = RpcDetails.New((uint)MathHelper.Random.NextInt(10000, 50000))
            };
        }
    }
}