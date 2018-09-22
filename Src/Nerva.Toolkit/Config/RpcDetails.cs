using AngryWasp.Helpers;

namespace Nerva.Toolkit.Config
{	
	public class RpcDetails
    {
        public bool IsPublic { get; set; } = false;

        public uint Port { get; set; } = 0;

        public string Login { get; set; } = "";

        public string Pass { get; set; } = "";

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