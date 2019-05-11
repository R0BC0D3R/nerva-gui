using System;
using System.IO;
using System.Net;
using AngryWasp.Logger;
using Nerva.Rpc;
using Nerva.Rpc.Daemon;
using Nerva.Toolkit.CLI;
using Log = AngryWasp.Logger.Log;
using DnsClient;
using System.Diagnostics;
using System.IO.Compression;
using Nerva.Toolkit.Helpers.Native;
using Nerva.Toolkit.Config;
using System.Linq;

namespace Nerva.Toolkit.Helpers
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

        public Update_Status_Code UpdateStatus { get; set; } = Update_Status_Code.Undefined;
        public string Version => version;
        public string CodeName => codeName;
        public string Notice => notice;

        public string GetDownloadFile()
        {   
            if (OS.Type == OS_Type.NotSet || OS.Type == OS_Type.Unsupported)
            {
                Log.Instance.Write(Log_Severity.Error, $"Could not generate CLI download link for OS type {OS.Type}");
                return null;
            }

            if (string.IsNullOrEmpty(version))
            {
                Log.Instance.Write(Log_Severity.Error, $"Could not check remote software version");
                return null;
            }

            return $"nerva-v{version}_{OS.Type.ToString().ToLower()}.zip";
        }

        public static UpdateInfo Create(string dnsRecord)
        {
            string[] recordParts = dnsRecord.Split(':');
            UpdateInfo ui = new UpdateInfo
            {
                version = recordParts[1],
                codeName = recordParts[2],
                notice = recordParts[3]
            };

            return ui;
        }

        public override string ToString()
        {
            return $"{version}:{codeName}\r\n{Constants.DOWNLOAD_LINK}/{GetDownloadFile()}\r\n{notice}";
        }
    }

	public class UpdateManager
	{
        private static string localCliVersion = null;
        private static string localGuiVersion = null;

        private static UpdateInfo cliUpdateInfo;
        private static UpdateInfo guiUpdateInfo;

        public static UpdateInfo CliUpdateInfo
        {
            get
            {
                if (cliUpdateInfo == null)
                    GetRemoteVersion();
                
                return cliUpdateInfo;
            }
        }

        public static UpdateInfo GuiUpdateInfo
        {
            get
            {
                if (guiUpdateInfo == null)
                    GetRemoteVersion();
                
                return guiUpdateInfo;
            }
        }

        public static void CheckUpdate()
        {
            Log.Instance.Write("Checking for updates...");

            GetLocalVersion();
            GetRemoteVersion();

            uint localCliInt = Conversions.VersionStringToInt(localCliVersion);
            uint remoteCliInt = Conversions.VersionStringToInt(cliUpdateInfo.Version);

            if (remoteCliInt > localCliInt)
                cliUpdateInfo.UpdateStatus = Update_Status_Code.NewVersionAvailable;
            else
                cliUpdateInfo.UpdateStatus = Update_Status_Code.UpToDate;

            uint localGuiInt = Conversions.VersionStringToInt(guiUpdateInfo.Version);
            uint remoteGuiInt = Conversions.VersionStringToInt(guiUpdateInfo.Version);

            if (remoteGuiInt > localGuiInt)
                guiUpdateInfo.UpdateStatus = Update_Status_Code.NewVersionAvailable;
            else
                guiUpdateInfo.UpdateStatus = Update_Status_Code.UpToDate;
        }

        private static void GetLocalVersion()
        {
            Cli.Instance.Daemon.Interface.GetInfo((GetInfoResponseData r) => {
                localCliVersion = r.Version;
            }, (RequestError e) => {
                Log.Instance.Write(Log_Severity.Error, $"GetInfo RPC call returned error {e.Code}, {e.Message}");
            });

            if (string.IsNullOrEmpty(localCliVersion))
                Log.Instance.Write(Log_Severity.Error, "Could not determine if a CLI update is required");

            localGuiVersion = Constants.VERSION;
        }

        private static void GetRemoteVersion()
        {
            var client = new LookupClient();
            var records = client.Query("update.getnerva.org", QueryType.TXT).Answers;
            
            foreach (var r in records)
            {
                string txt = ((DnsClient.Protocol.TxtRecord)r).Text.ToArray()[0];
                
                if (txt.StartsWith("nerva-cli:"))
                {
                    Log.Instance.Write($"Found DNS update record: {r}");
                    cliUpdateInfo = UpdateInfo.Create(txt);
                }       
                else if (txt.StartsWith("nerva-gui:"))
                {
                    Log.Instance.Write($"Found DNS update record: {r}");
                    guiUpdateInfo = UpdateInfo.Create(txt);
                } 
            }
        }
        
        public static void DownloadUpdate(Update_Type ut, string file, Action<DownloadProgressChangedEventArgs> onProgress, Action<bool, string> onComplete)
        {
            TaskFactory.Instance.RunTask("downloadupdate", "Downloading update", () =>
            {
                string url = $"{Constants.DOWNLOAD_LINK}/{file}";
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

        public static void DownloadCLI(string file, Action<DownloadProgressChangedEventArgs> onProgress, Action<bool, string> onComplete)
        {
            TaskFactory.Instance.RunTask("downloadcli", "Downloading the CLI tools", () =>
            {
                string url = $"{Constants.DOWNLOAD_LINK}/{file}";
                string destDir = Configuration.Instance.ToolsPath;
                
                if (string.IsNullOrEmpty(destDir))
                {
                    if (OS.IsWindows())
                        destDir = Path.Combine(Environment.CurrentDirectory, "CLI");
                    else
                        destDir = Path.Combine(Path.GetTempPath(), "nerva-cli");
                }

                if (!Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);

                string destFile = Path.Combine(destDir, file);

                if (File.Exists(destFile))
                {
                    Log.Instance.Write($"CLI tools found @ {destFile}");
                    ExtractFile(destDir, destFile, onComplete);
                }
                else
                {
                    Log.Instance.Write("Downloading CLI tools.");
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
                Cli.Instance.KillCliProcesses(FileNames.NERVAD);
                Cli.Instance.KillCliProcesses(FileNames.RPC_WALLET);

                Log.Instance.Write("Extracting CLI tools");
                
                ZipArchive archive = ZipFile.Open(destFile, ZipArchiveMode.Read);
                foreach (var a in archive.Entries)
                {
                    Log.Instance.Write($"Extracting {a.FullName}");
                    string extFile = Path.Combine(destDir, a.FullName);
                    a.ExtractToFile(extFile, true);

                    // ZipFile does not maintain linux permissions, so we have to set them
                    if (OS.IsUnix())
                        UnixNative.Chmod(extFile, 33261);
                }

                if (OS.IsUnix())
                {
                    string installerFile = Path.Combine(destDir, "install");
                    string installDir = null;

                    if (OS.IsLinux())
                        installDir = Path.Combine(Environment.GetEnvironmentVariable("HOME"), ".local/bin");
                    else if (OS.IsMac())
                        installDir = "/usr/local/bin";

                    if (File.Exists(installerFile))
                        Process.Start(installerFile);
                    else
                    {
                        Log.Instance.Write(Log_Severity.Warning, "Package does not contain an installer. Copying to install directory");

                        try
                        {
                            foreach (var f in new DirectoryInfo(destDir).GetFiles())
                                File.Copy(f.FullName, Path.Combine(installDir, f.Name), true);
                        }
                        catch (Exception ex)
                        {
                            AngryWasp.Logger.Log.Instance.Write(Log_Severity.Error, $".NET Exception, {ex.Message}");
                        }
                    }

                    destDir = installDir;
                }
                
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