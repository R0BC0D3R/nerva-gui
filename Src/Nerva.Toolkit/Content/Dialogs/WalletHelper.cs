using System;
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

    public static class WalletHelper
    {
        public static int GetWalletFileCount()
        {
            FileInfo[] files;
            if (!GetWalletFiles(out files))
                return 0;

            return files.Length;
        }

        public static bool GetWalletFiles(out FileInfo[] files)
        {
            files = null;

            if (!WalletDirExists())
                return false;

            DirectoryInfo dir = new DirectoryInfo(Configuration.Instance.Wallet.WalletDir);
            files = dir.GetFiles("*.cache", SearchOption.TopDirectoryOnly);

            if (files.Length == 0)
                return false;

            return true;
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

        public static void SaveWalletLogin(string walletFile)
        {
            Configuration.Instance.Wallet.LastOpenedWallet = walletFile;
            Configuration.Save();
        }
    }
}