using System;
using AngryWasp.Helpers;
using AngryWasp.Logger;
using Nerva.Toolkit.Config;
using Nerva.Toolkit.Helpers;
using Newtonsoft.Json.Linq;

namespace Nerva.Toolkit.CLI
{
    public class DaemonInterface
    {
        /// <summary>
        /// gets node information
        /// </summary>
        /// <returns>A JObject containing the node information</returns>
        public JObject GetInfo()
        {
            string result = null;

            if (!NetHelper.MakeJsonRpcRequest("get_info", out result))
            {
                Log.Instance.Write(Log_Severity.Error, "Could not complete JSON RPC call: get_info");
                return null;
            }

            return JObject.Parse(result);
        }

        /// <summary>
        /// Stops the CLI daemon
        /// </summary>
        /// <returns>Returns a bool value indicating if the request was successful</returns>
        public bool StopDaemon()
        {
            string result = null;

            if (!NetHelper.MakeRpcRequest("stop_daemon", null, out result))
            {
                Log.Instance.Write(Log_Severity.Error, "Could not complete RPC call: stop_daemon");
                return false;
            }

            var json = JObject.Parse(result);
            bool ok = json["status"].Value<string>().ToLower() == "ok";

            return ok;
        }

        public bool StartMining(int miningThreads)
        {
            int threads = MathHelper.Clamp(miningThreads, 1, Environment.ProcessorCount - 1);

            //To simplify things we set
            //do_background_mining = false
            //ignore_battery = true
            string postDataString = $"{{\"do_background_mining\":false,\"ignore_battery\":true,\"miner_address\":\"{Configuration.Instance.WalletAddress}\",\"threads_count\":{threads}}}";

            string result = null;

            if (!NetHelper.MakeRpcRequest("start_mining", postDataString, out result))
            {
                Log.Instance.Write(Log_Severity.Error, "Could not complete RPC call: start_mining");
                return false;
            }

            var json = JObject.Parse(result);
            bool ok = json["status"].Value<string>().ToLower() == "ok";

            return ok;
        }
    }
}