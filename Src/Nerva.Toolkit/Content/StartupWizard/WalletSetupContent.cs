using System;
using System.Text.RegularExpressions;
using AngryWasp.Logger;
using Eto.Drawing;
using Eto.Forms;
using Nerva.Rpc;
using Nerva.Rpc.Wallet;
using Nerva.Toolkit.CLI;
using Nerva.Toolkit.Content.Dialogs;
using Configuration = Nerva.Toolkit.Config.Configuration;

namespace Nerva.Toolkit.Content.Wizard
{
    public class WalletSetupContent : WizardContent
    {
        private Control content;

        public override string Title => "Set up your account";

        Button btnCreateAccount = new Button { Text = "Create" };
        Button btnImportAccount = new Button { Text = "Import" };
        Label lblImport = new Label { Text = "Importing an old wallet can take time" };
        Label lblImport2 = new Label { Text = "Now might be a good time for coffee" };
        public override Control Content
        {
            get
            {
                if (content == null)
                    content = CreateContent();

                return content;
            }
        }

        public override Control CreateContent()
        {
            btnCreateAccount.Click += (s, e) =>
            {
                Parent.EnableNextButton(false);
                lblImport.Visible = lblImport2.Visible = false;
                NewWalletDialog d = new NewWalletDialog();
                if (d.ShowModal() == DialogResult.Ok)
                {
                    Helpers.TaskFactory.Instance.RunTask("createwallet", "Creating wallet", () =>
                    {
                        if (d.HwWallet)
                        {
                            WalletRpc.CreateHwWallet(d.Name, d.Password,
                                (CreateHwWalletResponseData result) =>
                            {
                                CreateSuccess(d.Name, d.Password, result.Address);
                            }, CreateError);
                        }
                        else
                        {
                            WalletRpc.CreateWallet(d.Name, d.Password, 
                                (CreateWalletResponseData result) =>
                            {
                                CreateSuccess(d.Name, d.Password, result.Address);
                            }, CreateError);
                        }
                    });
                }
                else
                    Parent.EnableNextButton(true); 
            };

            btnImportAccount.Click += (s, e) =>
            {
                Parent.EnableNextButton(false);
                lblImport.Visible = lblImport2.Visible = false;
                ImportWalletDialog d = new ImportWalletDialog();
                if (d.ShowModal() == DialogResult.Ok)
                {
                    Helpers.TaskFactory.Instance.RunTask("importwallet", "Importing wallet", () =>
                    {
                        switch (d.ImportType)
                        {
                            case Import_Type.Key:
                                WalletRpc.RestoreWalletFromKeys(d.Name, d.Address, d.ViewKey, d.SpendKey, d.Password, d.Language,
                                (RestoreWalletFromKeysResponseData result) => {
                                    CreateSuccess(d.Name, d.Password, result.Address);
                                }, CreateError);
                            break;
                            case Import_Type.Seed:
                                WalletRpc.RestoreWalletFromSeed(d.Name, d.Seed, d.SeedOffset, d.Password, d.Language,
                                (RestoreWalletFromSeedResponseData result) => {
                                    CreateSuccess(d.Name, d.Password, result.Address);
                                }, CreateError);
                            break;
                        } 
                    });
                }
                else
                    Parent.EnableNextButton(true); 
            };

            return new StackLayout
            {
                Orientation = Orientation.Vertical,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                Items = 
                {
                    new Label { Text = "Create your account" },
                    new Label { Text = "   " },
                    new Label { Text = "You must create an account in order" },
                    new Label { Text = "to mine, send or receive NERVA" },
                    new Label { Text = "You can create a new account, or" },
                    new Label { Text = "import an existing one" },
                    new Label { Text = "   " },
                    lblImport,
                    lblImport2,
                    new StackLayoutItem(null, true),
                    new StackLayoutItem(new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalContentAlignment = HorizontalAlignment.Stretch,
                        VerticalContentAlignment = VerticalAlignment.Stretch,
                        Padding = new Padding(10, 10, 0, 0),
                        Spacing = 10,
                        Items =
                        {
                            new StackLayoutItem(null, true),
                            new StackLayoutItem(btnCreateAccount, false),
                            new StackLayoutItem(btnImportAccount, false)
                        }   
                    }, false), 
                }
            };
        }

        private void CreateSuccess(string name, string password, string address)
        {
            
            Application.Instance.Invoke( () =>
            {
                lblImport.Text = "Wallet creation complete";
                lblImport2.Text = "Press 'Next' to continue";
                lblImport.Visible = lblImport2.Visible = true;
                Parent.EnableNextButton(true);  
                Configuration.Instance.Wallet.LastOpenedWallet = name;
                Configuration.Instance.Daemon.MiningAddress = address;                                    
            });
            
            WalletRpc.CloseWallet(null, null);
        }

        private void CreateError(RequestError error)
        {
            WalletRpc.CloseWallet(null, null);
            Application.Instance.Invoke( () =>
            {
                lblImport.Text = "Wallet creation failed";
                lblImport2.Text = $"Error {error.Code}: {error.Message}";
                lblImport.Visible = lblImport2.Visible = true;
                Parent.EnableNextButton(true);     
            });
        }

        public override void OnAssignContent()
        {
            Parent.EnableNextButton(true);
        }
    }

    public class LabelWriter : ILogWriter
    {
        private Label lbl;
        private ProgressBar pb;

        public LabelWriter(Label lbl, ProgressBar pb)
        {
            this.lbl = lbl;
            this.pb = pb;
        }

        public void Close()
        {
            return;
        }

        public void Flush()
        {
            return;
        }

        public void SetColor(ConsoleColor color)
        {
            //todo: implement color change of label text
        }

        public void Write(Log_Severity severity, string value)
        {
            Application.Instance.AsyncInvoke( () =>
            {
                string stripped = Regex.Match(value, @"\d+(\.\d+)?[ ]\/[ ]\d+(\.\d+)?").Value;
                lbl.Text = stripped;
                if (stripped != null)
                {
                    string[] split = stripped.Split('/');
                    if (split.Length != 2)
                        return;

                    int val = int.Parse(split[0]);
                    int max = int.Parse(split[1]);

                    pb.MaxValue = max;
                    pb.Value = val;
                }
            });
        }
    }
}