using System.Collections.Generic;
using Nerva.Desktop.Objects;

namespace Nerva.Desktop
{	
	public static class Version
	{
        public const string VERSION = "0.3.2.0";
        public const string CODE_NAME = "";
        public static readonly string LONG_VERSION = VERSION + (string.IsNullOrEmpty(CODE_NAME) ? "" : ": " + CODE_NAME);


        // NERVA nodes used to get latest version info and download links. Those nodes need to have TXT records set up like this:
        /*
            update.nerva.one	TXT	"nerva-cli:0.1.7.4:NoWasp:On latest version"

            download.nerva.one	TXT	"linux:https://github.com/nerva-project/nerva/releases/download/v0.1.7.4/nerva-v0.1.7.4_linux_minimal.zip"
            download.nerva.one	TXT	"osx:https://github.com/nerva-project/nerva/releases/download/v0.1.7.4/nerva-v0.1.7.4_osx_minimal.zip"
            download.nerva.one	TXT	"windows:https://github.com/nerva-project/nerva/releases/download/v0.1.7.4/nerva-v0.1.7.4_windows_minimal.zip"
            download.nerva.one	TXT	"quicksync:https://github.com/nerva-project/nerva/releases/download/v0.1.7.5/quicksync.raw"
        */
        public static IList<DnsNode> REMOTE_NODES = new List<DnsNode>() {
            new DnsNode() { UpdateUrl = "update.nerva.one", DownloadUrl = "download.nerva.one" },
            new DnsNode() { UpdateUrl = "update.nerva.info", DownloadUrl = "download.nerva.info" },
            new DnsNode() { UpdateUrl = "update.nerva.tools", DownloadUrl = "download.nerva.tools" }
        };

        // Default values to use when TXT records are missing or cannot be retrieved
        public const string DEFAULT_CLI_VERSION = "0.7.1.5";
        public const string DEFAULT_CLI_DOWNLOAD_URL_WINDOWS = "https://github.com/nerva-project/nerva/releases/download/v0.1.7.5/nerva-v0.1.7.5_windows_minimal.zip";
        public const string DEFAULT_CLI_DOWNLOAD_URL_LINUX = "https://github.com/nerva-project/nerva/releases/download/v0.1.7.5/nerva-v0.1.7.5_linux_minimal.zip";
        public const string DEFAULT_CLI_DOWNLOAD_URL_OSX = "https://github.com/nerva-project/nerva/releases/download/v0.1.7.5/nerva-v0.1.7.5_osx_minimal.zip";

        public const string DEFAULT_DOWNLOAD_URL_QUICKSYNC = "https://github.com/nerva-project/nerva/releases/download/v0.1.7.5/quicksync.raw";
    }
}