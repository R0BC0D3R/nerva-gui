using System;
using System.IO;
using AngryWasp.Logger;
using Eto.Drawing;
using Eto.Forms;
using Nerva.Rpc;
using Nerva.Rpc.Wallet;
using Nerva.Toolkit.CLI;
using Nerva.Toolkit.Config;
using Nerva.Toolkit.Helpers;
using Configuration = Nerva.Toolkit.Config.Configuration;

namespace Nerva.Toolkit.Content.Dialogs
{
    public enum Open_Wallet_Dialog_Result
    {
        Cancel,
        New,
        Import,
        Open
    }

    public class MainWalletDialog : Dialog<Open_Wallet_Dialog_Result>
	{
        Button btnOpenWallet = new Button { Text = "Open" };
        Button btnImportWallet = new Button { Text = "Import" };
        Button btnNewWallet = new Button { Text = "New" };
        Button btnCancel = new Button { Text = "Cancel" };

        public MainWalletDialog()
        {
            this.Title = "Open/Import Wallet";
            Topmost = true;
            this.Resizable = true;
            var scr = Screen.PrimaryScreen;
            Location = new Point((int)(scr.WorkingArea.Width - Size.Width) / 2, (int)(scr.WorkingArea.Height - Size.Height) / 2);

            CreateLayout();

            this.AbortButton = btnCancel;

            btnNewWallet.Click += NewWallet_Clicked;
            btnOpenWallet.Click += OpenWallet_Clicked;
            btnImportWallet.Click += ImportWallet_Clicked;
            btnCancel.Click += CancelWallet_Clicked;

            if (WalletHelper.GetWalletFileCount() == 0)
            {
                btnOpenWallet.Enabled = false;
                this.DefaultButton = btnNewWallet;
            }
            else
                this.DefaultButton = btnOpenWallet;

            this.Focus();
        }

        public void CreateLayout()
        {
            Content = new StackLayout
            {
                Padding = 10,
                Spacing = 10,
                Orientation = Orientation.Horizontal,
				HorizontalContentAlignment = HorizontalAlignment.Center,
				VerticalContentAlignment = VerticalAlignment.Center,
                Items = 
                {
                    new StackLayoutItem(null, true),
                    new StackLayoutItem(btnOpenWallet),
                    new StackLayoutItem(btnNewWallet),
                    new StackLayoutItem(btnImportWallet),
                    new StackLayoutItem(btnCancel),
                    new StackLayoutItem(null, true),
                }
            };
        }

        private void NewWallet_Clicked(object sender, EventArgs e)
        {
            NewWalletDialog d = new NewWalletDialog();
            if (d.ShowModal() == DialogResult.Ok)
            {
                Helpers.TaskFactory.Instance.RunTask("createwallet", $"Creating wallet", () =>
                {
                    Cli.Instance.Wallet.Interface.CreateWallet(d.Name, d.Password,
                        (CreateWalletResponseData result) =>
                    {
                        WalletHelper.SaveWalletLogin(d.Name, d.Password);
                        WalletHelper.OpenWallet(d.Name, d.Password);
                        CreateSuccess(result.Address);
                        Close(Open_Wallet_Dialog_Result.New);
                    }, CreateError);
                });
            }
        }

        private void OpenWallet_Clicked(object sender, EventArgs e)
        {
            OpenWalletDialog d = new OpenWalletDialog();
            if (d.ShowModal() == DialogResult.Ok)
            {
                WalletHelper.SaveWalletLogin(d.Name, d.Password);
                WalletHelper.OpenWallet(d.Name, d.Password);
                Close(Open_Wallet_Dialog_Result.Open);
            }
        }

        private void ImportWallet_Clicked(object sender, EventArgs e)
        {
            ImportWalletDialog d = new ImportWalletDialog();
            if (d.ShowModal() == DialogResult.Ok)
            {
                Helpers.TaskFactory.Instance.RunTask("importwallet", $"Importing wallet", () =>
                {
                    switch (d.ImportType)
                    {
                        case Import_Type.Key:
                            Cli.Instance.Wallet.Interface.RestoreWalletFromKeys(d.Name, d.Address, d.ViewKey, d.SpendKey, d.Password, d.Language,
                                (RestoreWalletFromKeysResponseData result) =>
                            {
                                WalletHelper.SaveWalletLogin(d.Name, d.Password);
                                WalletHelper.OpenWallet(d.Name, d.Password);
                                CreateSuccess(result.Address);
                                Close(Open_Wallet_Dialog_Result.Import);
                            }, CreateError);
                        break;
                        case Import_Type.Seed:
                            Cli.Instance.Wallet.Interface.RestoreWalletFromSeed(d.Name, d.Seed, d.SeedOffset, d.Password, d.Language,
                            (RestoreWalletFromSeedResponseData result) =>
                            {
                                WalletHelper.SaveWalletLogin(d.Name, d.Password);
                                WalletHelper.OpenWallet(d.Name, d.Password);
                                CreateSuccess(result.Address);
                                Close(Open_Wallet_Dialog_Result.Import);
                            }, CreateError);
                        break;
                    }
                });
            }
        }

        private void CancelWallet_Clicked(object sender, EventArgs e)
        {
            Close(Open_Wallet_Dialog_Result.Cancel);
        }
        
        private void CreateSuccess(string address)
        {
            Application.Instance.AsyncInvoke( () =>
            {
                if (MessageBox.Show(Application.Instance.MainForm, "Wallet creation complete.\nWould you like to use this as the mining address?", "Create Wallet",
                    MessageBoxButtons.YesNo, MessageBoxType.Question, MessageBoxDefaultButton.Yes) == DialogResult.Yes)
                {
                    Configuration.Instance.Daemon.MiningAddress = address;
                    Configuration.Save();
                }
            });
        }

        private void CreateError(RequestError error)
        {
            Application.Instance.AsyncInvoke( () =>
            {
                MessageBox.Show(Application.Instance.MainForm, $"Wallet creation failed.\r\nError Code: {error.Code}\r\n{error.Message}", "Create Wallet",
                MessageBoxButtons.OK, MessageBoxType.Information, MessageBoxDefaultButton.OK);
            });
        }
    }
}