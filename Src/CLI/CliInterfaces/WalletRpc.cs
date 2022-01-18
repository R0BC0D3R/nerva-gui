using System;
using Nerva.Desktop.Helpers;
using Nerva.Rpc.Wallet;
using System.Collections.Generic;
using Nerva.Rpc;
using Nerva.Rpc.Wallet.Helpers;
using Nerva.Desktop.Config;
using Configuration = Nerva.Desktop.Config.Configuration;

namespace Nerva.Desktop.CLI
{
    public static class WalletRpc
    {
        public static bool GetAccounts(Action<GetAccountsResponseData> successAction, Action<RequestError> errorAction)
        {
            //suppress error code -13: No wallet file
            var l = Nerva.Rpc.Log.Presets.Normal;
            l.SuppressRpcCodes.Add(-13);
            return new GetAccounts(null, successAction, errorAction, Configuration.Instance.Wallet.Rpc.Host, Configuration.Instance.Wallet.Rpc.Port, l).Run();
        }
            
        public static bool CloseWallet(Action successAction, Action<RequestError> errorAction) =>
            new CloseWallet((string s) => {
                if (successAction != null)
                    successAction();
            }, errorAction, Configuration.Instance.Wallet.Rpc.Host, Configuration.Instance.Wallet.Rpc.Port).Run(); 

        public static bool StopWallet() =>
            new StopWallet(null, null, Configuration.Instance.Wallet.Rpc.Host, Configuration.Instance.Wallet.Rpc.Port).Run(); 

        public static bool CreateWallet(string walletName, string password,
            Action<CreateWalletResponseData> successAction, Action<RequestError> errorAction) =>
            new CreateWallet(new CreateWalletRequestData {
                FileName = walletName,
                Password = password
            }, successAction, errorAction, Configuration.Instance.Wallet.Rpc.Host, Configuration.Instance.Wallet.Rpc.Port).Run();

        public static bool CreateHwWallet(string walletName, string password,
            Action<CreateHwWalletResponseData> successAction, Action<RequestError> errorAction) =>
            new CreateHwWallet(new CreateHwWalletRequestData {
                FileName = walletName,
                Password = password
            }, successAction, errorAction, Configuration.Instance.Wallet.Rpc.Host, Configuration.Instance.Wallet.Rpc.Port).Run();

        public static bool RestoreWalletFromSeed(string walletName, string seed, string seedOffset, string password, string language, 
            Action<RestoreWalletFromSeedResponseData> successAction, Action<RequestError> errorAction) =>
            new RestoreWalletFromSeed(new RestoreWalletFromSeedRequestData {
                FileName = walletName, 
                Seed = seed,
                SeedOffset = seedOffset,
                Password = password
            }, successAction, errorAction, Configuration.Instance.Wallet.Rpc.Host, Configuration.Instance.Wallet.Rpc.Port).Run();

        public static bool RestoreWalletFromKeys(string walletName, string address, string viewKey, string spendKey, string password, string language, 
            Action<RestoreWalletFromKeysResponseData> successAction, Action<RequestError> errorAction) =>
            new RestoreWalletFromKeys(new RestoreWalletFromKeysRequestData {
                FileName = walletName, 
                Address = address,
                ViewKey = viewKey,
                SpendKey = spendKey,
                Password = password
            }, successAction, errorAction, Configuration.Instance.Wallet.Rpc.Host, Configuration.Instance.Wallet.Rpc.Port).Run();

        public static bool OpenWallet(string walletName, string password, Action successAction, Action<RequestError> errorAction) =>
            new OpenWallet(new OpenWalletRequestData {
                FileName = walletName,
                Password = password
            }, (string s) => {
                if (successAction != null)
                    successAction();
            }, errorAction, Configuration.Instance.Wallet.Rpc.Host, Configuration.Instance.Wallet.Rpc.Port).Run();

        public static bool QueryKey(string keyType, Action<QueryKeyResponseData> successAction, Action<RequestError> errorAction) =>
            new QueryKey(new QueryKeyRequestData {
                KeyType = keyType
            }, successAction, errorAction, Configuration.Instance.Wallet.Rpc.Host, Configuration.Instance.Wallet.Rpc.Port).Run();

        public static bool GetTransfers(ulong scanFromHeight, uint accountIndex, bool allAccounts, Action<GetTransfersResponseData> successAction, Action<RequestError> errorAction)
        {
            //suppress error code -13: No wallet file
            var l = Nerva.Rpc.Log.Presets.Normal;
            l.SuppressRpcCodes.Add(-13);

            return new GetTransfers(new GetTransfersRequestData {
                ScanFromHeight = scanFromHeight,
                AccountIndex = accountIndex,
                AllAccounts = allAccounts
            }, successAction, errorAction, Configuration.Instance.Wallet.Rpc.Host, Configuration.Instance.Wallet.Rpc.Port, l).Run();
        }

        public static bool RescanSpent() =>
            new RescanSpent(null, null, Configuration.Instance.Wallet.Rpc.Host, Configuration.Instance.Wallet.Rpc.Port).Run();

        public static bool RescanBlockchain() =>
            new RescanBlockchain(null, null, Configuration.Instance.Wallet.Rpc.Host, Configuration.Instance.Wallet.Rpc.Port).Run();

        public static bool Store() =>
            new Store(null, null, Configuration.Instance.Wallet.Rpc.Host, Configuration.Instance.Wallet.Rpc.Port).Run();

        public static bool CreateAccount(string label, Action<CreateAccountResponseData> successAction, Action<RequestError> errorAction) =>
            new CreateAccount(new CreateAccountRequestData {
                Label = label
            }, successAction, errorAction, Configuration.Instance.Wallet.Rpc.Host, Configuration.Instance.Wallet.Rpc.Port).Run();

        public static bool LabelAccount(uint index, string label) =>
            new LabelAccount(new LabelAccountRequestData {
                Index = index,
                Label = label
            }, null, null, Configuration.Instance.Wallet.Rpc.Host, Configuration.Instance.Wallet.Rpc.Port).Run();

        public static bool GetTransferByTxID(string txid, uint accountIndex, Action<GetTransferByTxIDResponseData> successAction, Action<RequestError> errorAction) =>
            new GetTransferByTxID(new GetTransferByTxIDRequestData {
                TxID = txid,
                AccountIndex = accountIndex
            }, successAction, errorAction, Configuration.Instance.Wallet.Rpc.Host, Configuration.Instance.Wallet.Rpc.Port).Run();

        public static bool TransferFunds(SubAddressAccount acc, string address, string paymentId, double amount, SendPriority priority, 
            Action<TransferResponseData> successAction, Action<RequestError> errorAction) =>
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
            }, successAction, errorAction, Configuration.Instance.Wallet.Rpc.Host, Configuration.Instance.Wallet.Rpc.Port).Run();

            public static bool TransferSplitFunds(SubAddressAccount acc, string address, string paymentId, double amount, SendPriority priority, 
            Action<TransferSplitResponseData> successAction, Action<RequestError> errorAction) =>
            new TransferSplit(new TransferSplitRequestData {
                AccountIndex = acc.Index,
                Priority = (uint)priority,
                PaymentId = paymentId,
                Destinations = new List<TransferDestination> {
                    new TransferDestination {
                        Address = address,
                        Amount = Conversions.ToAtomicUnits(amount)
                    }
                }
            }, successAction, errorAction, Configuration.Instance.Wallet.Rpc.Host, Configuration.Instance.Wallet.Rpc.Port).Run();

        public static bool MakeIntegratedAddress(string address, Action<MakeIntegratedAddressResponseData> successAction, Action<RequestError> errorAction) =>
            new MakeIntegratedAddress(new MakeIntegratedAddressRequestData {
                StandardAddress = address
            }, successAction, errorAction, Configuration.Instance.Wallet.Rpc.Host, Configuration.Instance.Wallet.Rpc.Port).Run();
    }
}
