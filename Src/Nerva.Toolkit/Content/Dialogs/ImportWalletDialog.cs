using System.Text;
using Eto.Forms;
using Nerva.Toolkit.Helpers;

namespace Nerva.Toolkit.Content.Dialogs
{
    public enum Import_Type
    {
        Key,
        Seed
    }

    public class ImportWalletDialog : PasswordDialog
    {
        protected string name;
        protected string viewKey;
        protected string spendKey;
        protected string seed;
        protected string seedOffset;
        protected string address;

        protected Import_Type importType;
        protected string lang;

        public string Name => name;
        public string ViewKey => viewKey;
        public string SpendKey => spendKey;
        public string Seed => seed;
        public string SeedOffset => seedOffset;
        public string Address => address;
        public Import_Type ImportType => importType;

        public string Language => lang;

        TextBox txtName = new TextBox();
        TextBox txtAddress = new TextBox();
        TextBox txtViewKey = new TextBox();
        TextBox txtSpendKey = new TextBox();
        TextArea txtSeed = new TextArea();
        TextBox txtSeedOffset = new TextBox();

        TabControl tc = new TabControl();
        ComboBox cbxLang = new ComboBox();

        public ImportWalletDialog() : base("Import Wallet") { }

        bool importStarted = false;
        protected override void OnOk()
        {
            if (IsImportStarted())
                return;

            base.OnOk();

            importType = (Import_Type)tc.SelectedIndex;
            lang = cbxLang.SelectedValue.ToString();

            name = txtName.Text;
            address = txtAddress.Text;
            viewKey = txtViewKey.Text;
            spendKey = txtSpendKey.Text;
            seed = txtSeed.Text;

            StringBuilder errors = new StringBuilder();

            if (string.IsNullOrEmpty(name))
                errors.AppendLine("Wallet name not provided");

            switch (importType)
            {
                case Import_Type.Key:
                {
                    if (string.IsNullOrEmpty(viewKey))
                        errors.AppendLine("View key not provided");

                    if (string.IsNullOrEmpty(spendKey))
                        errors.AppendLine("Spend key not provided");
                }
                break;
                case Import_Type.Seed:
                {
                    if (string.IsNullOrEmpty(seed))
                        errors.AppendLine("Seed not provided");
                }
                break;
            }
            
            string errorString = errors.ToString();
            if (!string.IsNullOrEmpty(errorString))
            {
                MessageBox.Show(this, $"Wallet import failed:\r\n{errorString}", "Wallet Import",
                    MessageBoxButtons.OK, MessageBoxType.Error, MessageBoxDefaultButton.OK);
                return;
            }

            this.Close(DialogResult.Ok);
        }

        protected override void OnCancel()
        {
            if (IsImportStarted())
                return;

            base.OnCancel();
            name = null;
            this.Close(DialogResult.Cancel);
        }

        private bool IsImportStarted()
        {
            if (importStarted)
            {
                MessageBox.Show(this, "Please wait for the import to complete", "Wallet Import",
                    MessageBoxButtons.OK, MessageBoxType.Information, MessageBoxDefaultButton.OK);
                return true;
            }

            return false;
        }

        protected override Control ConstructChildContent()
        {
            cbxLang.Items.Clear();

            foreach (var l in Constants.Languages)
                cbxLang.Items.Add(l);

            cbxLang.SelectedIndex = 1; //default to english

            tc = new TabControl
            {
                Pages =
                {
                    new TabPage
                    {
                        Text = "Keys",
                        Content = new StackLayout
                        {
                            Padding = 10,
                            Spacing = 10,
                            Orientation = Orientation.Vertical,
                            HorizontalContentAlignment = HorizontalAlignment.Stretch,
                            VerticalContentAlignment = VerticalAlignment.Stretch,
                            Items =
                            {
                                new StackLayoutItem(new Label { Text = "Address" }),
                                new StackLayoutItem(txtAddress, true),
                                new StackLayoutItem(new Label { Text = "View Key" }),
                                new StackLayoutItem(txtViewKey, true),
                                new StackLayoutItem(new Label { Text = "Spend Key" }),
                                new StackLayoutItem(txtSpendKey, true)
                            }
                        }
                    },
                    new TabPage
                    {
                        Text = "Seed",
                        Content = new StackLayout
                        {
                            Padding = 10,
                            Spacing = 10,
                            Orientation = Orientation.Vertical,
                            HorizontalContentAlignment = HorizontalAlignment.Stretch,
                            VerticalContentAlignment = VerticalAlignment.Stretch,
                            Items =
                            {
                                new StackLayoutItem(new Label { Text = "Seed" }),
                                new StackLayoutItem(txtSeed, true),
                                new StackLayoutItem(new Label { Text = "Seed Offset" }),
                                new StackLayoutItem(txtSeedOffset, false),
                            }
                        }
                    }
                }
            };

            return new StackLayout
            {
                Padding = 10,
                Spacing = 10,
                Orientation = Orientation.Vertical,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                Items =
                {
                    new StackLayoutItem(tc),
                    new StackLayoutItem(new Label { Text = "Wallet Name" }),
                    new StackLayoutItem(txtName),
                    new StackLayoutItem(new Label { Text = "Language" }),
                    new StackLayoutItem(cbxLang),
                    new StackLayoutItem(new Label { Text = "Password" }),
                    ConstructPasswordControls()
                }
            };
        }
    }
}