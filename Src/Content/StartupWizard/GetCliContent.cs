using System;
using System.IO;
using System.Net;
using AngryWasp.Logger;
using Eto.Drawing;
using Eto.Forms;
using Nerva.Desktop.Config;
using Nerva.Desktop.Helpers;

namespace Nerva.Desktop.Content.Wizard
{
    public class GetCliContent : WizardContent
    {
        private Control content;

        public override string Title => "NERVA Desktop Setup Wizard - Download";

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
            Log.Instance.Write("GCC.CreateContent: OS Type: " + OS.Type);
            switch (OS.Type)
            {
                case OS_Type.Unsupported:
                {
                    return CreateNotSupportedContent();
                }
                default:
                {
                    return CreateContent(UpdateManager.CliUpdateInfo.DownloadLink);
                }
            }
        }

        private Control CreateContent(string link)
        {
            StackLayout layout = null;

            try
            {
                btnDownload.Click += (s, e) =>
                {
                    HandleDownloadClick(link);
                };

                if (FileNames.DirectoryContainsCliTools(Configuration.Instance.ToolsPath))
                {
                    layout = new StackLayout
                    {
                        Orientation = Orientation.Vertical,
                        HorizontalContentAlignment = HorizontalAlignment.Stretch,
                        VerticalContentAlignment = VerticalAlignment.Stretch,
                        Items =
                        {
                            new Label { Text = $"It appears you already have the {OS.Type} CLI tools installed." },
                            new Label { Text = "   " },
                            new Label { Text = $"Press 'Next' to continue." },
                            new Label { Text = "   " },
                            new StackLayoutItem(null, true),
                        }
                    };
                }
                else
                {
                    layout = new StackLayout
                    {
                        Orientation = Orientation.Vertical,
                        HorizontalContentAlignment = HorizontalAlignment.Stretch,
                        VerticalContentAlignment = VerticalAlignment.Stretch,
                        Items =
                        {
                            new Label { Text = $"It appears you are running {OS.Type}" },
                            new Label { Text = "   " },
                            new Label { Text = "Click 'Download' to get the CLI tools" },
                            new Label { Text = "   " },
                            new Label { Text = "After download is complete, click 'Next' to continue" },
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
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleException("GCC.CreateContent", ex, true);
            }

            return layout;
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
            Log.Instance.Write("GCC.OnAssignContent: OS Type: " + OS.Type);
            Log.Instance.Write("GCC.OnAssignContent: Daemon Path: " + FileNames.DaemonPath);
            btnDownload.Enabled = OS.Type != OS_Type.Unsupported;
            Parent.EnableNextButton(File.Exists(FileNames.DaemonPath));
        }

        public override void OnNext()
        {
            //DaemonProcess.StartCrashCheck();
            //WalletProcess.StartCrashCheck();
        }

        private void HandleDownloadClick(string link)
        {
            try
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
            catch (Exception ex)
            {
                ErrorHandling.HandleException("GetCliContent.HandleDownloadClick", ex, true);
            }
        }
    }
}