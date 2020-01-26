using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using AngryWasp.Logger;
using Eto.Drawing;
using Eto.Forms;
using Nerva.Toolkit.CLI;
using Nerva.Toolkit.Config;
using Nerva.Toolkit.Helpers;
using Nerva.Toolkit.Helpers.Native;

namespace Nerva.Toolkit.Content.Wizard
{
    public class GetCliContent : WizardContent
    {
        private Control content;
        private bool existingCli = false;

        public override string Title => "Download NERVA";

        public override Control Content
        {
            get
            {
                if (content == null)
                    content = CreateContent();

                return content;
            }
        }

        Button btnDownload = new Button { Text = "Download", Enabled = false };
        ProgressBar pbDownload = new ProgressBar();

        public override Control CreateContent()
        {
            switch (OS.Type)
            {
                case OS_Type.Unsupported:
                    return CreateNotSupportedContent();
                default:
                    {
                        string link = UpdateManager.CliUpdateInfo.DownloadLink;
                        string defPath = FileNames.GetCliExePath(FileNames.NERVAD);
                        bool defPathExists = File.Exists(defPath);
                        switch (OS.Type)
                        {
                            case OS_Type.Windows:
                                existingCli = defPathExists;
                                break;
                            case OS_Type.Linux:
                                existingCli = defPathExists || File.Exists(Path.Combine(Environment.GetEnvironmentVariable("HOME"), ".local/bin/nervad"));
                                break;
                            case OS_Type.Osx:
                                existingCli = defPathExists || File.Exists("/usr/local/bin/nervad");
                                break;
                        }

                        return CreateContent(link);
                    }
            }
        }

        private Control CreateContent(string link)
        {
            btnDownload.Click += (s, e) =>
            {
                HandleDownloadClick(link);
            };

            if (existingCli)
            {
                GetExistingCliPath();
                return new StackLayout
                {
                    Orientation = Orientation.Vertical,
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    VerticalContentAlignment = VerticalAlignment.Stretch,
                    Items =
                    {
                        new Label { Text = $"It appears you already have the" },
                        new Label { Text = $"{OS.Type} CLI tools installed." },
                        new Label { Text = $"Press 'Next' to continue." },
                        new Label { Text = "   " },
                        new StackLayoutItem(null, true),
                    }
                };
            }

            return new StackLayout
            {
                Orientation = Orientation.Vertical,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                Items =
                {
                    new Label { Text = $"It appears you are running {OS.Type}" },
                    new Label { Text = "Click download to get the CLI tools" },
                    new Label { Text = "   " },
                    new StackLayoutItem(null, true),
                    new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalContentAlignment = HorizontalAlignment.Stretch,
                        VerticalContentAlignment = VerticalAlignment.Stretch,
                        Padding = new Padding(10, 0, 0, 0),
                        Spacing = 10,
                        Items =
                        {
                            new StackLayoutItem(null, true),
                            new StackLayoutItem(btnDownload, false)
                        }
                    },
                    new Label { Text = "   " },
                    pbDownload
                }
            };
        }

        private void GetExistingCliPath()
        {
            string dest = FileNames.GetCliExePath(FileNames.NERVAD);
            if (File.Exists(dest))
            {
                dest = Path.GetDirectoryName(dest);
                Configuration.Instance.ToolsPath = dest;
                return;
            }
            
            if (OS.IsMac())
                dest = "/usr/local/bin";
            else if (OS.IsLinux())
                dest = Path.Combine(Environment.GetEnvironmentVariable("HOME"), ".local/bin");
            else //should never happen
                Log.Instance.Write(Log_Severity.Fatal, "Attempt to use existing CLI path on unsupported system");

            Configuration.Instance.ToolsPath = dest;
        }

        private Control CreateNotSupportedContent()
        {
            return new StackLayout
            {
                Orientation = Orientation.Vertical,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                Padding = new Padding(0, 0, 0, 10),
                Spacing = 0,
                Items =
                {
                    new Label { Text = "This platform is not supported" },
                    new Label { Text = "   " },
                    new Label { Text = "Only Linux, Windows & Mac are supported" }
                }
            };
        }

        public override void OnAssignContent()
        {
            btnDownload.Enabled = OS.Type != OS_Type.Unsupported;
            Parent.EnableNextButton(File.Exists(FileNames.GetCliExePath(FileNames.NERVAD)));
        }

        public override void OnNext()
        {
            Cli.Instance.StartDaemon();
            Cli.Instance.StartWallet();
        }

        private void HandleDownloadClick(string link)
        {
            btnDownload.Enabled = false;

            UpdateManager.DownloadCLI(link, (DownloadProgressChangedEventArgs ea) =>
            {
                Application.Instance.AsyncInvoke(() =>
                {
                    btnDownload.Enabled = false;
                    pbDownload.MaxValue = (int)ea.TotalBytesToReceive;
                    pbDownload.Value = (int)ea.BytesReceived;
                });

            }, (bool success, string dest) =>
            {
                Application.Instance.AsyncInvoke(() =>
                {
                    btnDownload.Enabled = true;

                    if (success)
                    { 
                        Parent.EnableNextButton(true);
                        Configuration.Instance.ToolsPath = dest;
                        Log.Instance.Write($"Setting Config.ToolsPath: {dest}");
                    }
                    else
                    {
                        if (File.Exists(dest))
                            File.Delete(dest);
                        MessageBox.Show(Application.Instance.MainForm, "An error occured while downloading/extracting the NERVA CLI tools.\r\n" +
                        "Please refer to the log file and try again later", "Request Failed", MessageBoxButtons.OK, MessageBoxType.Error, MessageBoxDefaultButton.OK);
                    }
                });
            });
        }
    }
}