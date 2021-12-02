using System;
using System.IO;
using System.Net;
using AngryWasp.Logger;
using Nerva.Rpc;
using Nerva.Rpc.Daemon;
using Log = AngryWasp.Logger.Log;
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
            UpdateInfo ui = new UpdateInfo
            {
                version = Nerva.Desktop.Version.DEFAULT_CLI_VERSION,
                codeName = string.Empty,
                notice = string.Empty,
                downloadLink = Nerva.Desktop.Version.DEFAULT_CLI_DOWNLOAD_URL
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
                    Log.Instance.Write("UM.UpdateInfo: cliUpdateInfo is null");
                    GetRemoteVersion();
                }
                
                Log.Instance.Write("UM.UpdateInfo: Returning download link: " + cliUpdateInfo.DownloadLink);
                return cliUpdateInfo;
            }
        }

        public static void CheckUpdate()
        {
            Log.Instance.Write("UM.CheckUpdate: Checking for updates...");

            GetLocalVersion();
            GetRemoteVersion();

            uint localCliInt = Conversions.VersionStringToInt(localCliVersion);
            uint remoteCliInt = Conversions.VersionStringToInt(cliUpdateInfo.Version);

            if (remoteCliInt > localCliInt)
                cliUpdateInfo.UpdateStatus = Update_Status_Code.NewVersionAvailable;
            else
                cliUpdateInfo.UpdateStatus = Update_Status_Code.UpToDate;
        }

        private static void GetLocalVersion()
        {
            DaemonRpc.GetInfo((GetInfoResponseData r) => {
                localCliVersion = r.Version;
            }, (RequestError e) => {
                Log.Instance.Write(Log_Severity.Error, $"GetInfo RPC call returned error {e.Code}, {e.Message}");
            });

            if (string.IsNullOrEmpty(localCliVersion))
                Log.Instance.Write(Log_Severity.Error, "Could not determine if a CLI update is required");
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
                        Log.Instance.Write("UM.GetRemoteVersion: Node: " + node.UpdateUrl);

                        // Only consider TXT records
                        if(record is DnsClient.Protocol.TxtRecord)
                        {
                            string txt = ((DnsClient.Protocol.TxtRecord)record).Text.ToArray()[0];

                            Log.Instance.Write("UM.GetRemoteVersion: TXT: " + txt);

                            if (txt.StartsWith("nerva-cli:"))
                            {
                                Log.Instance.Write("UM.GetRemoteVersion: Found DNS update record: " + record);
                                cliUpdateInfo = UpdateInfo.Create(txt, GetDownloadLink(node.DownloadUrl));

                                // Found good record, break inner loop
                                foundGoodRecord = true;
                                break;
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
                    Log.Instance.Write("UM.GetRemoteVersion: Update Info or Download link NULL. Creating from DEFAULTS");
                    cliUpdateInfo = UpdateInfo.CreateDefault();
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleException("UM.GetRemoteVersion", ex, false);
            }
        }
        
        public static void DownloadUpdate(Update_Type ut, string file, Action<DownloadProgressChangedEventArgs> onProgress, Action<bool, string> onComplete)
        {
            TaskFactory.Instance.RunTask("downloadupdate", "Downloading update", () =>
            {
                string url = cliUpdateInfo.DownloadLink;
                string destDir = Configuration.Instance.ToolsPath;
                
                if (string.IsNullOrEmpty(destDir))
                    destDir = Environment.CurrentDirectory;

                if (!Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);

                string destFile = Path.Combine(destDir, file);

                if (File.Exists(destFile))
                {
                    Log.Instance.Write($"Binary package found @ {destFile}");
                    onComplete(true, destFile);
                }
                else
                {
                    Log.Instance.Write("Downloading binary package.");
                    using (var client = new WebClient())
                    {
                        client.DownloadFileAsync(new Uri(url + ".sig"),  destFile);
                    }
                    
                    using (var client = new WebClient())
                    {
                        client.DownloadProgressChanged += (s, e) =>
                        {
                            onProgress(e);
                        };

                        client.DownloadFileCompleted += (s, e) =>
                        {
                            if (e.Error == null)
                                onComplete(true, destFile);
                            else
                            {
                                AngryWasp.Logger.Log.Instance.Write(Log_Severity.Error, $".NET Exception, {e.Error.Message}");
                                onComplete(false, destFile);
                            }
                        };

                        client.DownloadFileAsync(new Uri(url),  destFile);
                    }
                }
            });   
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
                    Log.Instance.Write($"Found DNS download record: {r}");
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
                    Directory.CreateDirectory(destDir);

                // Check if we already downloaded the CLI package
                string destFile = Path.Combine(destDir, Path.GetFileName(file));

                if (File.Exists(destFile))
                {
                    Log.Instance.Write($"CLI tools found @ {destFile}");
                    ExtractFile(destDir, destFile, onComplete);
                }
                else
                {
                    Log.Instance.Write("Downloading CLI tools.");
                    AngryWasp.Logger.Log.Instance.Write(url);
                    using (var client = new WebClient())
                    {
                        client.DownloadProgressChanged += (s, e) =>
                        {
                            onProgress(e);
                        };

                        client.DownloadFileCompleted += (s, e) =>
                        {
                            if (e.Error == null)
                                ExtractFile(destDir, destFile, onComplete);
                            else
                            {
                                AngryWasp.Logger.Log.Instance.Write(Log_Severity.Error, $".NET Exception, {e.Error.Message}");
                                AngryWasp.Logger.Log.Instance.WriteNonFatalException(e.Error);
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
                DaemonProcess.ForceClose();
                WalletProcess.ForceClose();

                Log.Instance.Write("Extracting CLI tools");
                
                ZipArchive archive = ZipFile.Open(destFile, ZipArchiveMode.Read);
                foreach (var a in archive.Entries)
                {
                    Log.Instance.Write($"Extracting {a.FullName}");
                    string extFile = Path.Combine(destDir, a.FullName);
                    a.ExtractToFile(extFile, true);
#if UNIX
                    UnixNative.Chmod(extFile, 33261);
#endif
                }

                string installDir = Configuration.Instance.ToolsPath;

                if (!Directory.Exists(installDir))
                    Directory.CreateDirectory(installDir);

                try
                {
                    foreach (var f in new DirectoryInfo(destDir).GetFiles())
                        File.Copy(f.FullName, Path.Combine(installDir, f.Name), true);
                }
                catch (Exception ex)
                {
                    AngryWasp.Logger.Log.Instance.Write(Log_Severity.Error, $".NET Exception, {ex.Message}");
                }

                //todo: add ~/.local/bin to mac $PATH

                destDir = installDir; 
            }
            catch (Exception ex)
            {
                AngryWasp.Logger.Log.Instance.Write(Log_Severity.Error, $".NET Exception, {ex.Message}");
                onComplete(false, null);
                return;
            }
                         
            onComplete(true, destDir);
        }
    }
}