using System;
using System.Collections.Generic;
using AngryWasp.Helpers;
using Nerva.Rpc;
using Nerva.Rpc.Daemon;
using Configuration = Nerva.Toolkit.Config.Configuration;

namespace Nerva.Toolkit.CLI
{
    public static class DaemonRpc
    {
        public static bool GetBlockCount(Action<uint> successAction, Action<RequestError> errorAction) =>
            new GetBlockCount(successAction, errorAction, Configuration.Instance.Daemon.Rpc.Host, Configuration.Instance.Daemon.Rpc.Port).Run();

        public static bool GetInfo(Action<GetInfoResponseData> successAction, Action<RequestError> errorAction) =>
            new GetInfo(successAction, errorAction, Configuration.Instance.Daemon.Rpc.Host, Configuration.Instance.Daemon.Rpc.Port).Run();

        public static bool GetConnections(Action<List<GetConnectionsResponseData>> successAction, Action<RequestError> errorAction) =>
            new GetConnections(successAction, errorAction, Configuration.Instance.Daemon.Rpc.Host, Configuration.Instance.Daemon.Rpc.Port).Run();

        public static bool StopDaemon() => 
            new StopDaemon(null, null, Configuration.Instance.Daemon.Rpc.Host, Configuration.Instance.Daemon.Rpc.Port).Run();

        public static bool StartMining() =>
            new StartMining(new StartMiningRequestData {
                MinerAddress = Configuration.Instance.Daemon.MiningAddress,
                MiningThreads = (uint)MathHelper.Clamp(Configuration.Instance.Daemon.MiningThreads, 1, Environment.ProcessorCount)
            }, null, null, Configuration.Instance.Daemon.Rpc.Host, Configuration.Instance.Daemon.Rpc.Port).Run();

        public static bool StopMining() =>
            new StopMining(null, null, Configuration.Instance.Daemon.Rpc.Host, Configuration.Instance.Daemon.Rpc.Port).Run();

        public static bool MiningStatus(Action<MiningStatusResponseData> successAction, Action<RequestError> errorAction) =>
            new MiningStatus(successAction, errorAction, Configuration.Instance.Daemon.Rpc.Host, Configuration.Instance.Daemon.Rpc.Port).Run();

        public static bool BanPeer(string ip) =>
            new SetBans(new SetBansRequestData {
                Bans = new List<SetBansItem> {
                    new SetBansItem {
                        Host = ip
                    }
                }
            }, null, null, Configuration.Instance.Daemon.Rpc.Host, Configuration.Instance.Daemon.Rpc.Port).Run();
    }
}