using System.Diagnostics;
using System.IO;
using AngryWasp.Helpers;
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
    public abstract class DialogBase<T> : Dialog<T>
    {
        protected Button btnOk = new Button { Text = "OK" };
        protected Button btnCancel = new Button { Text = "Cancel" };

        public DialogBase(string title)
        {
            this.Title = title;

            if (OS.IsUnix())
                this.Width = 400;

            this.Resizable = true;
            //Topmost = true;
            var scr = Screen.PrimaryScreen;
            Location = new Point((int)(scr.WorkingArea.Width - Size.Width) / 2, (int)(scr.WorkingArea.Height - Size.Height) / 2);

            this.AbortButton = btnCancel;
            this.DefaultButton = btnOk;

            ConstructContent();

            btnOk.Click += (s, e) => OnOk();
            btnCancel.Click += (s, e) => OnCancel();
        }

        protected override void OnShown(System.EventArgs e)
        {
            //HACK On Windows, setting the width in the constructor automatically changes the height
            //So we set the width here and it seems to work
            if (OS.IsWindows())
                this.Width = 400;
        }

        protected virtual void ConstructContent()
        {
            Content = new StackLayout
            {
                Orientation = Orientation.Vertical,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                Items =
                {
                    new StackLayoutItem(ConstructChildContent(), true),
                    new StackLayoutItem(new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalContentAlignment = HorizontalAlignment.Right,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Padding = 10,
                        Spacing = 10,
                        Items =
                        {
                            new StackLayoutItem(null, true),
                            btnOk,
                            btnCancel
                        }
                    })
                }
            };
        }

        protected abstract Control ConstructChildContent();

        protected abstract void OnOk();
        protected abstract void OnCancel();
    }

    public abstract class PasswordDialog : DialogBase<DialogResult>
    {
        protected bool isShown = false;

        protected string password;

        public string Password => password;

        protected PasswordBox txtPass = new PasswordBox { PasswordChar = '*' };
        protected TextBox txtPlain = new TextBox();
        protected TableRow tr = new TableRow();

        protected TextControl txtCtrl;

        protected Button btnShow = new Button { Text = "Show" };

        public PasswordDialog(string title) : base(title)
        {
            btnShow.Click += (s, e) => OnShow();
        }

        protected override void OnOk()
        {
            password = isShown ? txtPlain.Text : txtPass.Text;
        }

        protected override void OnCancel()
        {
            password = null;
        }

        protected virtual void OnShow()
        {
            isShown = !isShown;
            if (isShown)
                txtPlain.Text = txtPass.Text;
            else
                txtPass.Text = txtPlain.Text;

            ConstructContent();
        }

        protected override void ConstructContent()
        {
            txtCtrl = isShown ? (TextControl)txtPlain : (TextControl)txtPass;
            base.ConstructContent();
            txtCtrl.Focus();
        }

        protected StackLayout ConstructPasswordControls()
        {
            return new StackLayout
            {
                Orientation = Orientation.Horizontal,
                HorizontalContentAlignment = HorizontalAlignment.Right,
                VerticalContentAlignment = VerticalAlignment.Center,
                Spacing = 10,
                Items =
                {
                    new StackLayoutItem(txtCtrl, true),
                    btnShow
                }
            };
        }
    }

    public class WalletHelper
    {
        public delegate void WalletWizardEventHandler(Open_Wallet_Dialog_Result result, object additionalData);
        public event WalletWizardEventHandler WalletWizardEvent;
        private static WalletHelper instance = new WalletHelper();

        public static WalletHelper Instance => instance;

        public static void SaveWalletLogin(string walletFile, string password)
        {
            Configuration.Instance.Wallet.LastOpenedWallet = walletFile;

            string formattedPassword = string.IsNullOrEmpty(password) ? string.Empty : password.EncodeBase64();

            if (Configuration.Instance.Wallet.SaveWalletPassword)
                Configuration.Instance.Wallet.LastWalletPassword = formattedPassword;
            else
                Configuration.Instance.Wallet.LastWalletPassword = null;

            Configuration.Save();
        }

        public static bool OpenSavedWallet()
        {
            var w = Configuration.Instance.Wallet;

            if (!WalletFileExists(w.LastOpenedWallet))
                return false;

            //Wallet file is saved in config and exists on disk.
            //Load from the saved password if that exists
            if (w.LastWalletPassword != null)
            {
                string pass = w.LastWalletPassword == string.Empty ? string.Empty : w.LastWalletPassword.DecodeBase64();
                if (Cli.Instance.Wallet.Interface.OpenWallet(w.LastOpenedWallet, pass))
                {
                    Log.Instance.Write("Wallet file '{0}' opened", w.LastOpenedWallet);
                    return true;
                }
                else
                {
                    Log.Instance.Write(Log_Severity.Warning, "Wallet cannot be opened from saved information");
                    return false;
                }
            }
            else
            {
                //Keep looping until the right password is entered or cancelled
                while (true)
                {
                    EnterPasswordDialog d = new EnterPasswordDialog();
                    switch (d.ShowModal())
                    {
                        case DialogResult.Ok:
                            {
                                if (Cli.Instance.Wallet.Interface.OpenWallet(w.LastOpenedWallet, d.Password))
                                    return true;
                            }
                            break;
                        default:
                            return false;
                    }
                }
            }
        }

        public static bool WalletFileExists(string file)
        {
            if (!WalletDirExists())
                return false;

            if (string.IsNullOrEmpty(file))
                return false;

            string walletFile = Path.Combine(Configuration.Instance.Wallet.WalletDir, file);

            return File.Exists(walletFile);
        }

        public static bool WalletDirExists()
        {
            if (string.IsNullOrEmpty(Configuration.Instance.Wallet.WalletDir))
                return false;

            return Directory.Exists(Configuration.Instance.Wallet.WalletDir);
        }

        public static int GetWalletFileCount()
        {
            var f = GetWalletFiles();

            if (f == null)
                return 0;

            return f.Length;
        }

        public static FileInfo[] GetWalletFiles()
        {
            if (!WalletDirExists())
                return null;

            DirectoryInfo dir = new DirectoryInfo(Configuration.Instance.Wallet.WalletDir);
            return dir.GetFiles("*.keys", SearchOption.TopDirectoryOnly);
        }

        private static bool wizardRunning = false;

        public static bool WizardRunning => wizardRunning;

        /// <summary>
        /// Shows the open wallet wizard
        /// </summary>
        public void ShowWalletWizard()
        {
            //prevent other threads starting the wizard if it is already running
            if (wizardRunning)
                return;

            wizardRunning = true;

            //We only break this loop if cancel is pressed from the main form
            while (true)
            {
                MainWalletDialog d = new MainWalletDialog();
                switch (d.ShowModal())
                {
                    case Open_Wallet_Dialog_Result.Open:
                        {
                            while (true)
                            {
                                OpenWalletDialog d2 = new OpenWalletDialog();
                                if (d2.ShowModal() == DialogResult.Ok)
                                {
                                    if (d2.Name == Configuration.Instance.Wallet.LastOpenedWallet)
                                    {
                                        wizardRunning = false;
                                        WalletWizardEvent?.Invoke(Open_Wallet_Dialog_Result.Open, null);
                                        return;
                                    }

                                    SaveWalletLogin(d2.Name, d2.Password);
                                    wizardRunning = false;
                                    WalletWizardEvent?.Invoke(Open_Wallet_Dialog_Result.Open, null);
                                    return;
                                }
                                else //break the loop if cancelled
                                    break;
                            }
                        }
                        break;
                    case Open_Wallet_Dialog_Result.New:
                        {
                            while (true)
                            {
                                NewWalletDialog d2 = new NewWalletDialog();
                                if (d2.ShowModal() == DialogResult.Ok)
                                {
                                    Helpers.TaskFactory.Instance.RunTask("createwallet", $"Creating wallet", () =>
                                    {
                                        Cli.Instance.Wallet.Interface.CreateWallet(d2.Name, d2.Password,
                                        (CreateWalletResponseData result) =>
                                        {
                                            SaveWalletLogin(d2.Name, d2.Password);
                                            wizardRunning = false;
                                            WalletWizardEvent?.Invoke(Open_Wallet_Dialog_Result.New, result);
                                            CreateSuccess(result.Address);
                                        }, CreateError);
                                    });

                                    wizardRunning = false;
                                    return;
                                }
                                else //break the loop if cancelled
                                    break;
                            }
                        }
                        break;
                    case Open_Wallet_Dialog_Result.Import:
                        {
                            while (true)
                            {
                                ImportWalletDialog d2 = new ImportWalletDialog();
                                DialogResult dr = d2.ShowModal();
                                if (dr == DialogResult.Ok)
                                {
                                    Helpers.TaskFactory.Instance.RunTask("importwallet", $"Importing wallet", () =>
                                    {
                                        switch (d2.ImportType)
                                        {
                                            case Import_Type.Key:
                                                Cli.Instance.Wallet.Interface.RestoreWalletFromKeys(d2.Name, d2.Address, d2.ViewKey, d2.SpendKey, d2.Password, d2.Language,
                                                    (RestoreWalletFromKeysResponseData result) =>
                                                {
                                                    wizardRunning = false;
                                                    SaveWalletLogin(d2.Name, d2.Password);
                                                    WalletWizardEvent?.Invoke(Open_Wallet_Dialog_Result.Import, result);
                                                    CreateSuccess(result.Address);
                                                }, CreateError);
                                            break;
                                            case Import_Type.Seed:
                                                Cli.Instance.Wallet.Interface.RestoreWalletFromSeed(d2.Name, d2.Seed, d2.SeedOffset, d2.Password, d2.Language,
                                                (RestoreWalletFromSeedResponseData result) =>
                                                {
                                                    wizardRunning = false;
                                                    SaveWalletLogin(d2.Name, d2.Password);
                                                    WalletWizardEvent?.Invoke(Open_Wallet_Dialog_Result.Import, result);
                                                    CreateSuccess(result.Address);
                                                }, CreateError);
                                            break;
                                        }
                                    });
                                    
                                    wizardRunning = false;
                                    return;
                                }
                                else
                                    break;
                            }
                        }
                        break;
                    default:
                        {
                            wizardRunning = false;
                            return;
                        }
                }
            }
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