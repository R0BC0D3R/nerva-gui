using AngryWasp.Helpers;

namespace Nerva.Desktop.Config
{	
	public class RpcDetails
    {
        public bool IsPublic { get; set; } = false;

        public string Host { get; set; } = "127.0.0.1";
        public uint Port { get; set; } = 0;

        public string Login { get; set; } = "";

        public string Pass { get; set; } = "";

        public uint LogLevel { get; set; } = 1;

        public static RpcDetails New(uint port)
        {
            return new RpcDetails
            {
                IsPublic = false,
                Port = port,
                Login = StringHelper.GenerateRandomString(24),
                Pass = StringHelper.GenerateRandomString(24)
            };
        }
    }
}