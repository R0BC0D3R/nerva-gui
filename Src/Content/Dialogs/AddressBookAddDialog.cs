using System.Text;
using Eto.Forms;
using Eto.Drawing;
using Nerva.Desktop.Helpers;

namespace Nerva.Desktop.Content.Dialogs
{
    public class AddressBookAddDialog : DialogBase<DialogResult>
    {
        #region Local Variables
        private AddressBookEntry entry;
        public AddressBookEntry Entry => entry;

        TextBox txtName = new TextBox();
        TextBox txtDescription = new TextBox();
        TextBox txtAddress = new TextBox();
        TextBox txtPayID = new TextBox();
        Label lblRequiredMessage = new Label() { Text = "* Indicates required fields" };
        #endregion // Local Variables

        #region Constructor Methods
        public AddressBookAddDialog(AddressBookEntry entry) : base(entry == null ? "Add To Address Book" : "Edit Address Book")
        {
            this.MinimumSize = new Size(300, 330);
            this.btnOk.Text = "Save";

            if (entry == null)
            {
                return;
            }

            txtName.Text = entry.Name;
            txtAddress.Text = entry.Address;
            txtDescription.Text = entry.Description;
            txtPayID.Text = entry.PaymentId;
        }
        #endregion // Constructor Methods

        #region Base Class Methods
        protected override Control ConstructChildContent()
        {
            return new StackLayout
            {
                Padding = 10,
                Spacing = 10,
                Orientation = Orientation.Vertical,
				HorizontalContentAlignment = HorizontalAlignment.Stretch,
				VerticalContentAlignment = VerticalAlignment.Stretch,
                Items =
                {
                    new Label { Text = "* Name"},
                    txtName,
                    new Label { Text = "Description" },
                    txtDescription,
                    new Label { Text = "* Address" },
                    txtAddress,
                    new Label { Text = "Payment ID" },
                    txtPayID,
                    lblRequiredMessage
                }
            };
        }

        protected override void OnOk()
        {
            StringBuilder errors = new StringBuilder();

            //At a minimum we require a name and address
            //Description and payment id are not required

            if (string.IsNullOrEmpty(txtName.Text))
            {
                errors.AppendLine("Name is not provided");
            }

            if (string.IsNullOrEmpty(txtAddress.Text))
            {
                errors.AppendLine("Address is not provided");
            }
            else if(txtAddress.Text.Length < 30)
            {
                errors.AppendLine("Address is too short");
            }
            else if(txtAddress.Text.Contains(' '))
            {
                errors.AppendLine("Address cannot contain spaces");
            }

            string errorString = errors.ToString();
            if (!string.IsNullOrEmpty(errorString))
            {
                MessageBox.Show(this, $"Failed to add address:\r\n{errorString}", "Address Book", MessageBoxButtons.OK, MessageBoxType.Error, MessageBoxDefaultButton.OK);
                return;
            }

            entry = new AddressBookEntry
            {
                Name = txtName.Text,
                Address = txtAddress.Text,
                Description = txtDescription.Text,
                PaymentId = txtPayID.Text
            };

            this.Close(DialogResult.Ok);
        }

        protected override void OnCancel()
        {
            this.Close(DialogResult.Cancel);
        }
        #endregion // Base Class Methods
    }
}