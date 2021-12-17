using System;
using System.IO;
using Eto.Forms;
using Nerva.Rpc.Daemon;
using Nerva.Desktop.CLI;
using Configuration = Nerva.Desktop.Config.Configuration;

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
                    }
                    else
                    {
                        if (DaemonRpc.StartMining())
                        {
                            Logger.LogInfo("GM.SSM",$"Mining started for @ {Conversions.WalletAddressShortForm(Configuration.Instance.Daemon.MiningAddress)} on {Configuration.Instance.Daemon.MiningThreads} threads");
                        }
                        else
                        {
                            Application.Instance.AsyncInvoke( () =>
                            {
                                MessageBox.Show(Application.Instance.MainForm, $"Failed to start mining.\r\nMake sure you are synchronized and check your mining address", "Start/Stop Miner",
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
    }
}