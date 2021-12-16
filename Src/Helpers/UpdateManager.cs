using System;
using System.IO;
using System.Net;
using System.Threading;
using Nerva.Rpc;
using Nerva.Rpc.Daemon;
using DnsClient;
using System.IO.Compression;
using Nerva.Desktop.Config;
using System.Linq;
using Nerva.Desktop.CLI;
using System.Collections.Generic;
using Nerva.Desktop.Objects;

#if UNIX
using Nerva.Desktop.Helpers.Native;
#endif

namespace Nerva.Desktop.Helpers
{
    public enum Update_Status_Code
    {
        Undefined,
        NewVersionAvailable,
        UpToDate,
    }

    public enum Update_Type
    {
        Undefined,
        CLI,
        GUI
    }

    public class UpdateInfo
    {
        private string version = null;
        private string codeName = null;
        private string notice = null;

        private string downloadLink = null;

        public Update_Status_Code UpdateStatus { get; set; } = Update_Status_Code.Undefined;
        public string Version => version;
        public string CodeName => codeName;
        public string Notice => notice;
        public string DownloadLink => downloadLink;

        public static UpdateInfo Create(string dnsRecord, string dlLink)
        {
            string[] recordParts = dnsRecord.Split(':');
            UpdateInfo ui = new UpdateInfo
            {
                version = recordParts[1],
                codeName = recordParts[2],
                notice = recordParts[3],
                downloadLink = dlLink
            };

            return ui;
        }

        public static UpdateInfo CreateDefault()
        {
            string cliDownloadUrl;

            switch(OS.Type)
            {
                case OS_Type.Linux:
                    cliDownloadUrl = Nerva.Desktop.Version.DEFAULT_CLI_DOWNLOAD_URL_LINUX;
                    break;
                case OS_Type.Osx:
                    cliDownloadUrl = Nerva.Desktop.Version.DEFAULT_CLI_DOWNLOAD_URL_OSX;
                    break;
                default:
                    // Default to Windows
                    cliDownloadUrl = Nerva.Desktop.Version.DEFAULT_CLI_DOWNLOAD_URL_WINDOWS;
                    break;
            }

            UpdateInfo ui = new UpdateInfo
            {
                version = Nerva.Desktop.Version.DEFAULT_CLI_VERSION,
                codeName = string.Empty,
                notice = string.Empty,
                downloadLink = cliDownloadUrl
            };

            return ui;
        }

        public override string ToString()
        {
            return $"{version}:{codeName}\r\n{downloadLink}\r\n{notice}";
        }
    }

	public class UpdateManager
	{
        private static string localCliVersion = null;
        private static UpdateInfo cliUpdateInfo;

        public static UpdateInfo CliUpdateInfo
        {
            get
            {
                if (cliUpdateInfo == null)
                {
                    Logger.LogDebug("UM.UI", "cliUpdateInfo is null");
                    GetRemoteVersion();
                }
                
                Logger.LogDebug("UM.UI", "Returning download link: " + cliUpdateInfo.DownloadLink);
                return cliUpdateInfo;
            }
        }

        public static void CheckUpdate()
        {
            Logger.LogDebug("UM.CU", "Checking for updates...");

            GetLocalVersion();
            GetRemoteVersion();

            int localCliInt = Conversions.VersionStringToInt(localCliVersion);
            int remoteCliInt = Conversions.VersionStringToInt(cliUpdateInfo.Version);

            if (remoteCliInt > localCliInt)
            {
                cliUpdateInfo.UpdateStatus = Update_Status_Code.NewVersionAvailable;
            }
            else
            {
                cliUpdateInfo.UpdateStatus = Update_Status_Code.UpToDate;
            }
        }

        private static void GetLocalVersion()
        {
            DaemonRpc.GetInfo((GetInfoResponseData r) => {
                localCliVersion = r.Version;
            }, (RequestError e) => {
                Logger.LogError("UM.GLV", $"GetInfo RPC call returned error {e.Code}, {e.Message}");
            });

            if (string.IsNullOrEmpty(localCliVersion))
            {
                Logger.LogError("UM.GLV", "Could not determine if a CLI update is required");
            }
        }

        private static void GetRemoteVersion()
        {
            try
            {
                var client = new LookupClient();

                IReadOnlyList<DnsClient.Protocol.DnsResourceRecord> records = new List<DnsClient.Protocol.DnsResourceRecord>();

                bool foundGoodRecord = false;
                foreach(DnsNode node in Version.REMOTE_NODES)
                {
                    records = client.Query(node.UpdateUrl, QueryType.TXT).Answers;
                    
                    foreach (var record in records)
                    {
                        Logger.LogDebug("UM.GRV", "Node: " + node.UpdateUrl);

                        // Only consider TXT records
                        if(record is DnsClient.Protocol.TxtRecord)
                        {
                            string txt = ((DnsClient.Protocol.TxtRecord)record).Text.ToArray()[0];

                            Logger.LogDebug("UM.GRV", "TXT: " + txt);

                            if (txt.StartsWith("nerva-cli:"))
                            {
                                Logger.LogDebug("UM.GRV", "Found DNS update record: " + record);
                                string downloadLink = GetDownloadLink(node.DownloadUrl);
                                if(!string.IsNullOrEmpty(downloadLink))
                                {
                                    cliUpdateInfo = UpdateInfo.Create(txt, downloadLink);
                                }

                                if(cliUpdateInfo != null)
                                {
                                    // Found good record, break inner loop
                                    foundGoodRecord = true;
                                    break;
                                }
                            }
                        }
                    }

                    if(foundGoodRecord)
                    {
                        // Since we found good record, break outer loop
                        break;
                    }
                }

                // If DNS records could not be retrieved for some reason, use defaults
                if(cliUpdateInfo == null || string.IsNullOrEmpty(cliUpdateInfo.DownloadLink))
                {
                    Logger.LogDebug("UM.GRV", "Update Info or Download link NULL. Creating from DEFAULTS");
                    cliUpdateInfo = UpdateInfo.CreateDefault();
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("UM.GRV", ex, false);
            }
        }
        
        public static string GetDownloadLink(string downloadRecordUrl)
        {
            var client = new LookupClient();
            var records = client.Query(downloadRecordUrl, QueryType.TXT).Answers;

            string os = OS.Type.ToString().ToLower();
            string prefix = $"{os}:";

            foreach (var r in records)
            {
                string txt = ((DnsClient.Protocol.TxtRecord)r).Text.ToArray()[0];
                if (txt.StartsWith(prefix))
                {
                    Logger.LogDebug("UM.GDL", $"Found DNS download record: {r}");
                    return txt.Substring(prefix.Length);
                }       
            }

            return null;
        }

        public static void DownloadCLI(string file, Action<DownloadProgressChangedEventArgs> onProgress, Action<bool, string> onComplete)
        {

            TaskFactory.Instance.RunTask("downloadcli", "Downloading the CLI tools", () =>
            {
                string url = cliUpdateInfo.DownloadLink;

                // Set up the CLI tool path
                if (string.IsNullOrEmpty(Configuration.Instance.ToolsPath))
                {
                    Configuration.Instance.ToolsPath = Path.Combine(Configuration.StorageDirectory, "cli");
                    Configuration.Save();
                }

                string destDir = Configuration.Instance.ToolsPath;

                if (!Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }

                // Check if we already downloaded the CLI package
                string destFile = Path.Combine(destDir, Path.GetFileName(file));

                if (File.Exists(destFile))
                {
                    Logger.LogDebug("UM.DC", $"CLI tools found @ {destFile}");

                    ExtractFile(destDir, destFile, onComplete);
                }
                else
                {
                    Logger.LogDebug("UM.DC", "Downloading CLI tools. URL: " + url);
                    using (var client = new WebClient())
                    {
                        client.DownloadProgressChanged += (s, e) =>
                        {
                            onProgress(e);
                        };

                        client.DownloadFileCompleted += (s, e) =>
                        {
                            if (e.Error == null)
                            {
                                ExtractFile(destDir, destFile, onComplete);
                            }
                            else
                            {
                                ErrorHandler.HandleException("UM.DC", e.Error, false);
                                onComplete(false, destFile);
                            }
                        };

                        client.DownloadFileAsync(new Uri(url),  destFile);
                    }
                }
            });   
        }

        private static void ExtractFile(string destDir, string destFile, Action<bool, string> onComplete)
        {
            try
            {
                Logger.LogDebug("UM.EF", "Closing Daemon and Wallet processes");
                while (DaemonProcess.IsRunning())
                {
                    DaemonProcess.ForceClose();
                    Thread.Sleep(1000);
                }

                while (WalletProcess.IsRunning())
                {
                    WalletProcess.ForceClose();
                    Thread.Sleep(1000);
                }
                
                Logger.LogDebug("UM.EF", "Extracting CLI tools");
                
                ZipArchive archive = ZipFile.Open(destFile, ZipArchiveMode.Read);
                foreach (var a in archive.Entries)
                {
                    Logger.LogDebug("UM.EF", $"Extracting {a.FullName}");
                    string extFile = Path.Combine(destDir, a.FullName);
                    a.ExtractToFile(extFile, true);
#if UNIX
                    UnixNative.Chmod(extFile, 33261);
#endif
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("UM.EF", ex, false);
                onComplete(false, null);
                return;
            }
                         
            onComplete(true, destDir);
        }
    }
}