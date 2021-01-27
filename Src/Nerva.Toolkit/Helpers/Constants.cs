using System;

#if UNIX
using Nerva.Toolkit.Helpers.Native;
#endif

namespace Nerva.Toolkit.Helpers
{	
	public static class Constants
	{
        public const int ONE_SECOND = 1000;

        public const uint NERVAD_RPC_PORT_MAINNET = 17566;
        public const uint NERVAD_RPC_PORT_TESTNET = 18566;

        public static readonly string[] Languages = new string[]
        {
            "Deutsch",
            "English",
            "Español",
            "Français",
            "Italiano",
            "Nederlands",
            "Português",
            "русский язык",
            "日本語",
            "简体中文 (中国)",
            "Esperanto",
            "Lojban"
        };
    }

    public enum OS_Type
    {
        NotSet,
        Linux,
        Osx,
        Windows,
        Unsupported,
    }

    public static class OS
    {
        private static OS_Type type = OS_Type.NotSet;
        public static OS_Type Type
        {
            get
            {
                if (type != OS_Type.NotSet)
                    return type;

                var p = Environment.OSVersion.Platform;

                switch (p)
                {
                    case PlatformID.Win32NT:
                    case PlatformID.Win32S:
                    case PlatformID.Win32Windows:
                        type = OS_Type.Windows;
                        break;
                    case PlatformID.Unix:
                        {
#if UNIX
                            var uname = UnixNative.Sysname();
                            if (uname == "linux")
                                type = OS_Type.Linux;
                            else if (uname == "darwin")
                                type = OS_Type.Osx;
                            else
#endif
                                type = OS_Type.Unsupported;
                        }
                        break;
                    default:
                        type = OS_Type.Unsupported;
                        break;
                }

                if (type == OS_Type.Unsupported)
                    throw new PlatformNotSupportedException("The OS type could not be determined");

                return type;
            }
        }

#if WINDOWS
        public static string HomeDirectory => Environment.GetEnvironmentVariable("%HOMEDRIVE%%HOMEPATH%");
#else
        public static string HomeDirectory => Environment.GetEnvironmentVariable("HOME");
#endif

    }
}
