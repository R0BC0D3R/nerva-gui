using System;
using Nerva.Toolkit.Helpers.Native;

namespace Nerva.Toolkit.Helpers
{	
	public static class Constants
	{
        public const string VERSION = "2.8.0";
        public const string CODE_NAME = "Beta-8";
        public static readonly string LONG_VERSION = $"{VERSION}: {CODE_NAME}";

        public const string DEFAULT_CONFIG_FILENAME = "app.config";
        public const string DEFAULT_LOG_FILENAME = "app.log";
        public const string DEV_WALLET_ADDRESS = "NV1r8P6THPASAQX77re6hXTMJ1ykXXvtYXFXgMv4vFAQNYo3YatUvZ8LFNRu4dPQBjTwqJbMvqoeiipywmREPHpD2AgWnmG7Q";
        public const int ONE_SECOND = 1000;
        public const int FIVE_SECONDS = 5000;
        public const int BAN_TIME = 6000;

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
                            var uname = UnixNative.Sysname();
                            if (uname == "linux")
                                type = OS_Type.Linux;
                            else if (uname == "darwin")
                                type = OS_Type.Osx;
                            else
                                type = OS_Type.Unsupported;
                        }
                        break;
                    default:
                        type = OS_Type.Unsupported;
                        break;
                }

                if (type == OS_Type.Unsupported)
                    throw new NotSupportedException("The OS type could not be determined");

                return type;
            }
        }

        public static bool IsWindows() => type == OS_Type.Windows;

        public static bool IsLinux() => type == OS_Type.Linux;

        public static bool IsMac() => type == OS_Type.Osx;

        public static bool IsUnix() => type == OS_Type.Linux || type == OS_Type.Osx;
    }
}
