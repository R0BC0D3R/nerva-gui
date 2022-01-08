using System;
using System.IO;
using Eto.Forms;
using Nerva.Rpc.Daemon;
using Nerva.Desktop.CLI;
using Configuration = Nerva.Desktop.Config.Configuration;
using Nerva.Rpc.Wallet;
using Nerva.Desktop.Content.Dialogs;
using System.Text;
using Nerva.Rpc;

namespace Nerva.Desktop.Helpers
{
    public static class GlobalMethods
    {
        public static void StartStopMining()
        {
            try 
            {
                DaemonRpc.MiningStatus(( MiningStatusResponseData r) => 
                {
                    if (r.Active)
                    {
                        DaemonRpc.StopMining();
                        Logger.LogInfo("GM.SSM", "Mining stopped");
                        MessageBox.Show(Application.Instance.MainForm, "Mining stopped", MessageBoxButtons.OK, MessageBoxType.Information);
                    }
                    else
                    {
                        if (DaemonRpc.StartMining())
                        {
                            Logger.LogInfo("GM.SSM",$"Mining started to {Conversions.WalletAddressShortForm(Configuration.Instance.Daemon.MiningAddress)} on {Configuration.Instance.Daemon.MiningThreads} threads");
                            MessageBox.Show(Application.Instance.MainForm, "Mining started", MessageBoxButtons.OK, MessageBoxType.Information);
                        }
                        else
                        {
                            Application.Instance.AsyncInvoke( () =>
                            {
                                MessageBox.Show(Application.Instance.MainForm, 
                                    $"Failed to start mining\r\n\r\nMake sure you are synchronized and that you have mining address set under File > Preferences > Daemon", "Start/Stop Miner",
                                    MessageBoxButtons.OK, MessageBoxType.Error, MessageBoxDefaultButton.OK);
                            });
                        }
                    }
                }
                , null);
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("GM.SSM", ex, false);
            }
        }

        public static string GetAppIcon()
        {
            string iconFile = string.Empty;

            try
            {
                string icon = "nerva.ico";

                if(File.Exists(icon))
                {
                    iconFile = icon;
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("GM.GAI", ex, false);
            }

            return iconFile;
        }

        public static void TransferFundsUsingSplit(TransferDialog transferDialog)
		{
			try
			{
				WalletRpc.TransferSplitFunds(transferDialog.SelectedAccount, transferDialog.Address, transferDialog.PaymentId, transferDialog.Amount, transferDialog.Priority,
				(TransferSplitResponseData response) =>
				{
					Application.Instance.AsyncInvoke(() =>
					{
						Logger.LogInfo("BP.TFUS", "TransferSplitFunds was successful");

						StringBuilder transferMassage = new StringBuilder();
						transferMassage.AppendLine("Transfer Split Successful! See log for more details.");
						
						for(int ct = 0; ct < response.AmountList.Count; ct++)
						{
							Logger.LogInfo("BP.TFUS", "#" + ct + ", sent: " + Conversions.FromAtomicUnits4Places(response.AmountList[ct]) + ", fees: " + Conversions.FromAtomicUnits4Places(response.FeeList[ct]) + ", hash: " + response.TxHashList[ct] + ", key: " + response.TxKeyList[ct]);
							transferMassage.AppendLine("#" + ct + ", sent: " + Conversions.FromAtomicUnits4Places(response.AmountList[ct]) + ", fees: " + Conversions.FromAtomicUnits4Places(response.FeeList[ct]));
						}								

						MessageBox.Show(Application.Instance.MainForm, transferMassage.ToString(), MessageBoxType.Information);
					});
				}, (RequestError err) =>
				{
					Application.Instance.AsyncInvoke(() =>
					{
						Logger.LogError("BP.TFUS", "TransferSplitFunds request failed: " + err.Message);
						MessageBox.Show(Application.Instance.MainForm, "The transfer request failed\r\n" + err.Message, MessageBoxType.Error);
					});
				});
			}
			catch (Exception ex)
			{
				ErrorHandler.HandleException("BP.TUS", ex, false);
			}	
		}

		public static void TransferFundsNoSplit(TransferDialog transferDialog)
		{
			try
			{
				WalletRpc.TransferFunds(transferDialog.SelectedAccount, transferDialog.Address, transferDialog.PaymentId, transferDialog.Amount, transferDialog.Priority,
				(TransferResponseData response) =>
				{
					Application.Instance.AsyncInvoke(() =>
					{
						Logger.LogInfo("BP.TFNS", "TransferFunds was successful");
						Logger.LogInfo("BP.TFNS", "Sent: " + Conversions.FromAtomicUnits4Places(response.Amount) + ", fees: " + Conversions.FromAtomicUnits4Places(response.Fee) + ", hash: " + response.TxHash + ", txKey: " + response.TxKey);

						string transferMassage = "Transfer Successful! See log for more details.\r\nSent: " + Conversions.FromAtomicUnits4Places(response.Amount) + "\r\nFees: " + Conversions.FromAtomicUnits4Places(response.Fee);								
						MessageBox.Show(Application.Instance.MainForm, transferMassage, MessageBoxType.Information);
					});
				}, (RequestError err) =>
				{
					Application.Instance.AsyncInvoke(() =>
					{
						Logger.LogError("BP.TFNS", "TransferFunds request failed: " + err.Message);

						if(err.Message.ToLower().Contains("transfer_split"))
						{
							string boxMessage = "Your transfer failed because transaction would be too large.\r\n\r\nWould you like to retry by splitting into multiple transactions?";

							if (MessageBox.Show(Application.Instance.MainForm, boxMessage, MessageBoxButtons.YesNo, MessageBoxType.Question, MessageBoxDefaultButton.Yes) == DialogResult.Yes)
							{
								TransferFundsUsingSplit(transferDialog);
							}
						}
						else 
						{
							MessageBox.Show(Application.Instance.MainForm, "The transfer request failed\r\n" + err.Message, MessageBoxType.Error);
						}						
					});
				});
			}
			catch (Exception ex)
			{
				ErrorHandler.HandleException("BP.TFNS", ex, false);
			}	
		}
    }
}