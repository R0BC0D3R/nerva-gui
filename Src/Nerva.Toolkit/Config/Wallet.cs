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
            get
            {
                if (OS.IsWindows())
                    if (!walletDir.EndsWith(Path.DirectorySeparatorChar.ToString()))
                        walletDir = walletDir + Path.DirectorySeparatorChar;

                return walletDir;
            }

            set
            {
                if (OS.IsWindows())
                    if (!value.EndsWith(Path.DirectorySeparatorChar.ToString()))
                        value = value + Path.DirectorySeparatorChar;
                
                if (!Directory.Exists(value))
                    Directory.CreateDirectory(value);

                walletDir = value;
            }
        }

        public string LastOpenedWallet { get; set; }

        public int NumTransfersToDisplay { get; set; } = 25;

		public static Wallet New()
        {
            return new Wallet
            {
                WalletDir = Path.Combine(Environment.CurrentDirectory, "Wallets"),
                LastOpenedWallet = null,
                Rpc = RpcDetails.New((uint)MathHelper.Random.NextInt(10000, 50000))
            };
        }
    }
}