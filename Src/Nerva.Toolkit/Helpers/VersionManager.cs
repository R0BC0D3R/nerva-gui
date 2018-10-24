using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using AngryWasp.Logger;
using Nerva.Toolkit.CLI;
using Nerva.Toolkit.Helpers.Native;
using Newtonsoft.Json;

namespace Nerva.Toolkit.Helpers
{
    public static class VersionManager
    {
        private static VersionInfo versionInfo;

        public static VersionInfo VersionInfo => versionInfo;

        public static void GetVersionInfo(ulong currentVersion, Action completeAction)
        {
            TaskFactory.Instance.RunTask("checkversions", "Checking for available versions", () =>
            {
                Log.Instance.Write("Checking for new versions");

                string versionString = null;
                UpdateManager.MakeHttpRequest("http://getnerva.org/getbinaries.php", out versionString);

                if (versionString == null)
                {
                    //todo: Show a message box
                    Log.Instance.Write(Log_Severity.Error, "Could not retrieve update information");
                    completeAction();
                    return;
                }

                versionInfo = JsonConvert.DeserializeObject<VersionInfo>(versionString);
                ulong newCliVersion = Conversions.OctetSetToInt(versionInfo.CliVersionNumber.TrimStart('v'));
                ulong newGuiVersion = Conversions.OctetSetToInt(versionInfo.GuiVersionNumber.TrimStart('v'));

                if (newCliVersion > currentVersion)
                {
                    Log.Instance.Write("New CLI version available: '{0}'", versionInfo.CliVersion);

                    TaskFactory.Instance.RunTask("killcli", "Killing old CLI processes", () =>
                    {
                        //new CLI version is available. kill old version
                        Log.Instance.Write("Killing outdated CLI processes");
                        if (Cli.Instance.Daemon != null)
                        {
                            Cli.Instance.Daemon.StopCrashCheck();
                            Cli.Instance.Wallet.ForceClose();
                        }
                        else
                            Cli.Instance.KillCliProcesses(FileNames.NERVAD);

                        if (Cli.Instance.Wallet != null)
                        {
                            Cli.Instance.Wallet.StopCrashCheck();
                            Cli.Instance.Daemon.ForceClose();
                        }
                        else
                            Cli.Instance.KillCliProcesses(FileNames.RPC_WALLET);                     
                    });
                }
                else
                    Log.Instance.Write("CLI tools up to date");

                ulong currentGuiVersion = Conversions.OctetSetToInt(Constants.VERSION);
                if (newGuiVersion > currentGuiVersion)
                    Log.Instance.Write("New GUI version available: '{0}'", versionInfo.GuiVersion);
                else
                    Log.Instance.Write("GUI up to date");

                completeAction();
            });
        }

        public static void DownloadFile(string file, Action<DownloadProgressChangedEventArgs> onProgress, Action<bool, string> onComplete)
        {
            TaskFactory.Instance.RunTask("downloadcli", "Downloading the CLI tools", () =>
            {
                string url = $"{VersionInfo.BinaryUrl}binaries/{file}";
                string destDir = Path.Combine(Environment.CurrentDirectory, "CLI");

                if (!Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);

                string destFile = Path.Combine(destDir, file);

                if (File.Exists(destFile))
                {
                    Log.Instance.Write("Local copy of CLI tools found.");
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
                                Log.Instance.WriteNonFatalException(e.Error);
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
                Cli.Instance.KillCliProcesses(FileNames.CLI_WALLET);
                Cli.Instance.KillCliProcesses(FileNames.RPC_WALLET);

                Log.Instance.Write("Extracting CLI tools");
                ZipArchive archive = ZipFile.Open(destFile, ZipArchiveMode.Read);
                foreach (var a in archive.Entries)
                {
                    Log.Instance.Write("Extracting {0}", a.FullName);
                    string newName = Path.GetFileNameWithoutExtension(a.FullName) + "-gui";

                    if (OS.Type == OS_Type.Windows)
                        newName += ".exe";

                    string extFile = Path.Combine(destDir, newName);

                    a.ExtractToFile(extFile, true);

                    // ZipFile does not maintain linux permissions, so we have to set them
                    if (Environment.OSVersion.Platform == PlatformID.Unix)
                        LinuxNative.Chmod(extFile, 33261);
                }
            }
            catch (Exception ex)
            {
                Log.Instance.WriteNonFatalException(ex);
                onComplete(false, null);
                return;
            }
                         
            onComplete(true, destDir);
        }
    }
}