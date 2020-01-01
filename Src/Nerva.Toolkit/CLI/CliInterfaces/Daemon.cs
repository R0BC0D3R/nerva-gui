using System;
using System.Collections.Generic;
using AngryWasp.Helpers;
using AngryWasp.Logger;
using AngryWasp.Serializer;
using Nerva.Rpc;
using Nerva.Rpc.Daemon;
using Nerva.Toolkit.Config;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Configuration = Nerva.Toolkit.Config.Configuration;

namespace Nerva.Toolkit.CLI
{
    public class DaemonInterface : CliInterface
    {
        public DaemonInterface() : base(Configuration.Instance.Daemon.Rpc) { }
        
        public bool GetBlockCount(Action<uint> successAction, Action<RequestError> errorAction) =>
            new GetBlockCount(successAction, errorAction, r.Host, r.Port).Run();

        public bool GetInfo(Action<GetInfoResponseData> successAction, Action<RequestError> errorAction) =>
            new GetInfo(successAction, errorAction, r.Host, r.Port).Run();

        public bool GetConnections(Action<List<GetConnectionsResponseData>> successAction, Action<RequestError> errorAction) =>
            new GetConnections(successAction, errorAction, r.Host, r.Port).Run();

        public bool StopDaemon() => 
            new StopDaemon(null, null, r.Host, r.Port).Run();

        public bool StartMining() =>
            new StartMining(new StartMiningRequestData {
                MinerAddress = Configuration.Instance.Daemon.MiningAddress,
                MiningThreads = (uint)MathHelper.Clamp(Configuration.Instance.Daemon.MiningThreads, 1, Environment.ProcessorCount)
            }, null, null, r.Host, r.Port).Run();

        public bool StopMining() =>
            new StopMining(null, null, r.Host, r.Port).Run();

        public bool MiningStatus(Action<MiningStatusResponseData> successAction, Action<RequestError> errorAction) =>
            new MiningStatus(successAction, errorAction, r.Host, r.Port).Run();

        public bool BanPeer(string ip) =>
            new SetBans(new SetBansRequestData {
                Bans = new List<SetBansItem> {
                    new SetBansItem {
                        Host = ip
                    }
                }
            }, null, null, r.Host, r.Port).Run();
    }
}