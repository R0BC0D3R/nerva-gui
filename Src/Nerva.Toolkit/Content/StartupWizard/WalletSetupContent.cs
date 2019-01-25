using System.Text.RegularExpressions;
using AngryWasp.Helpers;
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
        Label lblImport = new Label { TextAlignment = TextAlignment.Right, Visible = false };
        Label lblImport2 = new Label { TextAlignment = TextAlignment.Right, Visible = false };
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
                NewWalletDialog d2 = new NewWalletDialog();
                if (d2.ShowModal() == DialogResult.Ok)
                {
                    Helpers.TaskFactory.Instance.RunTask("createwallet", "Creating wallet", () =>
                    {
                        Cli.Instance.Wallet.Interface.CreateWallet(d2.Name, d2.Password,
                        (CreateWalletResponseData result) => {
                            CreateSuccess(d2.Name, d2.Password, result.Address);
                        }, CreateError);
                    });
                    
                }
                else
                    Parent.EnableNextButton(true); 
            };

            btnImportAccount.Click += (s, e) =>
            {
                Parent.EnableNextButton(false);
                lblImport.Visible = lblImport2.Visible = false;
                ImportWalletDialog d2 = new ImportWalletDialog();
                DialogResult dr = d2.ShowModal();
                if (dr == DialogResult.Ok)
                {
                    Helpers.TaskFactory.Instance.RunTask("importwallet", "Importing wallet", () =>
                    {
                        switch (d2.ImportType)
                        {
                            case Import_Type.Key:
                                Cli.Instance.Wallet.Interface.RestoreWalletFromKeys(d2.Name, d2.Address, d2.ViewKey, d2.SpendKey, d2.Password, d2.Language,
                                (RestoreWalletFromKeysResponseData result) => {
                                    CreateSuccess(d2.Name, d2.Password, result.Address);
                                }, CreateError);
                            break;
                            case Import_Type.Seed:
                                Cli.Instance.Wallet.Interface.RestoreWalletFromSeed(d2.Name, d2.Seed, d2.SeedOffset, d2.Password, d2.Language,
                                (RestoreWalletFromSeedResponseData result) => {
                                    CreateSuccess(d2.Name, d2.Password, result.Address);
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
                    new StackLayoutItem(null, true),
                    new StackLayoutItem(new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalContentAlignment = HorizontalAlignment.Stretch,
                        VerticalContentAlignment = VerticalAlignment.Stretch,
                        Padding = new Padding(0, 0, 0, 10),
                        Spacing = 10,
                        Items =
                        {
                            new StackLayoutItem(null, true),
                            new StackLayoutItem(btnCreateAccount, false),
                            new StackLayoutItem(btnImportAccount, false)
                        }   
                    }, false),
                    lblImport,
                    lblImport2
                }
            };
        }

        private void CreateSuccess(string name, string password, string address)
        {
            Application.Instance.Invoke( () =>
            {
                lblImport.Text = "Wallet creation complete";
                lblImport2.Text = "Press >> to continue";
                lblImport.Visible = lblImport2.Visible = true;
                Parent.EnableNextButton(true);  
                SaveWalletLogin(name, password);
                Configuration.Instance.Daemon.MiningAddress = address;                                          
            });
        }

        private void CreateError(RequestError error)
        {
            Application.Instance.Invoke( () =>
            {
                lblImport.Text = "Wallet creation failed";
                lblImport2.Text = $"Error {error.Code}: {error.Message}";
                lblImport.Visible = lblImport2.Visible = true;
                Parent.EnableNextButton(true);     
            });
        }

        public static void SaveWalletLogin(string walletFile, string password)
        {
            string formattedPassword = string.IsNullOrEmpty(password) ? string.Empty : password.EncodeBase64();
            Configuration.Instance.Wallet.LastOpenedWallet = walletFile;
            Configuration.Instance.Wallet.LastWalletPassword = formattedPassword;
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