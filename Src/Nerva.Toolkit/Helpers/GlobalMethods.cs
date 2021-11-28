using System;
using Eto.Forms;
using Nerva.Rpc.Daemon;
using Nerva.Toolkit.CLI;
using Configuration = Nerva.Toolkit.Config.Configuration;
using Log = AngryWasp.Logger.Log;

namespace Nerva.Toolkit.Helpers
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
                        Log.Instance.Write("Mining stopped");                        
                    }
                    else
                    {
                        if (DaemonRpc.StartMining())
                        {
                            Log.Instance.Write($"Mining started for @ {Conversions.WalletAddressShortForm(Configuration.Instance.Daemon.MiningAddress)} on {Configuration.Instance.Daemon.MiningThreads} threads");
                        }
                        else
                        {
                            Application.Instance.AsyncInvoke( () =>
                            {
                                MessageBox.Show(Application.Instance.MainForm, $"Failed to start mining.\r\nMake sure you are synced and check your mining address", "Toggle Miner",
                                MessageBoxButtons.OK, MessageBoxType.Error, MessageBoxDefaultButton.OK);
                            });
                        }
                    }
                }
                , null);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleException("GM.StartStopMining", ex, false);
            }
        }
    }
}