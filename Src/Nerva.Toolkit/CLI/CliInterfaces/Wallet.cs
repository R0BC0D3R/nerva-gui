using System;
using Nerva.Toolkit.Helpers;
using Nerva.Rpc.Wallet;
using System.Collections.Generic;
using Configuration = Nerva.Toolkit.Config.Configuration;
using Nerva.Rpc;
using AngryWasp.Logger;

namespace Nerva.Toolkit.CLI
{
    public partial class WalletInterface : CliInterface
    {
        public WalletInterface() : base(Configuration.Instance.Wallet.Rpc) { }

        public bool GetAccounts(Action<GetAccountsResponseData> successAction, Action<RequestError> errorAction)
        {
            return new GetAccounts(successAction, errorAction, r.Port).Run();
        }

        public bool CloseWallet(Action successAction, Action<RequestError> errorAction)
        {
            return new CloseWallet((string e) =>
            {
                successAction();
            }, errorAction, r.Port).Run(); 
        }

        public bool StopWallet()
        {
            return new StopWallet(null, null, r.Port).Run(); 
        }

        public bool CreateWallet(string walletName, string password,
            Action<CreateWalletResponseData> successAction, Action<RequestError> errorAction)
        {
            return new CreateWallet(new CreateWalletRequestData {
                FileName = walletName,
                Password = password
            }, successAction, errorAction, r.Port).Run();
        } 

        public bool RestoreWalletFromSeed(string walletName, string seed, string seedOffset, string password, 
            string language, Action<RestoreWalletFromSeedResponseData> successAction, Action<RequestError> errorAction)
        {
            return new RestoreWalletFromSeed(new RestoreWalletFromSeedRequestData {
                FileName = walletName, 
                Seed = seed,
                SeedOffset = seedOffset,
                Password = password
            }, successAction, errorAction, r.Port).Run();
        }

        public bool RestoreWalletFromKeys(string walletName, string address, string viewKey, string spendKey, string password, 
            string language, Action<RestoreWalletFromKeysResponseData> successAction, Action<RequestError> errorAction)
        {
            return new RestoreWalletFromKeys(new RestoreWalletFromKeysRequestData {
                FileName = walletName, 
                Address = address,
                ViewKey = viewKey,
                SpendKey = spendKey,
                Password = password
            }, successAction, errorAction, r.Port).Run();
        }

        public bool OpenWallet(string walletName, string password, Action successAction, Action<RequestError> errorAction)
        {
            return new OpenWallet(new OpenWalletRequestData {
                FileName = walletName,
                Password = password
            }, (string result) => {
                successAction();
            }, errorAction, r.Port).Run();
        }

        public bool QueryKey(string keyType, Action<QueryKeyResponseData> successAction)
        {
            return new QueryKey(new QueryKeyRequestData {
                KeyType = keyType
            }, successAction, null, r.Port).Run();
        }

        public bool GetTransfers(uint scanFromHeight, Action<GetTransfersResponseData> successAction, Action<RequestError> errorAction)//uint scanFromHeight, out uint lastTxHeight)
        {
            return new GetTransfers(new GetTransfersRequestData {
                ScanFromHeight = scanFromHeight
            }, successAction, errorAction, r.Port).Run();
        }

        public bool RescanSpent()
        {
            return new RescanSpent(null, null, r.Port).Run();
        }

        public bool RescanBlockchain()
        {
            return new RescanBlockchain(null, null, r.Port).Run();
        }

        public bool Store()
        {
            return new Store(null, null, r.Port).Run();
        }

        public CreateAccountResponseData CreateAccount(string label)
        {
            CreateAccountResponseData data = null;

            new CreateAccount(new CreateAccountRequestData {
                Label = label
            }, (CreateAccountResponseData result) => {
                data = result;
            }, null, r.Port).Run();

            return data;
        }

        public bool LabelAccount(uint index, string label)
        {
            return new LabelAccount(new LabelAccountRequestData {
                Index = index,
                Label = label
            }, null, null, r.Port).Run();
        }

        public GetTransferByTxIDResponseData GetTransferByTxID(string txid)
        {
            GetTransferByTxIDResponseData data = null;

            new GetTransferByTxID(new GetTransferByTxIDRequestData {
                TxID = txid
            }, (GetTransferByTxIDResponseData result) => {
                data = result;
            }, null, r.Port).Run();

            return data;
        }

        public TransferResponseData TransferFunds(SubAddressAccount acc, string address, string paymentId, double amount, Send_Priority priority)
        {
            TransferResponseData data = null;

            new Transfer(new TransferRequestData {
                AccountIndex = acc.Index,
                Priority = (uint)priority,
                PaymentId = paymentId,
                Destinations = new List<TransferDestination> {
                    new TransferDestination {
                        Address = address,
                        Amount = Conversions.ToAtomicUnits(amount)
                    }
                }
            }, (TransferResponseData result) => {
                data = result;
            }, null, r.Port).Run();

            return data;
        }
    }
}
