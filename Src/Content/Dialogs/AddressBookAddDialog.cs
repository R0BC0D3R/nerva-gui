using System.Text;
using Eto.Forms;
using Eto.Drawing;
using Nerva.Desktop.Helpers;

namespace Nerva.Desktop.Content.Dialogs
{
    public class AddressBookAddDialog : DialogBase<DialogResult>
    {
        private AddressBookEntry entry;
        public AddressBookEntry Entry => entry;

        TextBox txtName = new TextBox();
        TextBox txtDescription = new TextBox();
        TextBox txtAddress = new TextBox();
        TextBox txtPayID = new TextBox();

        public AddressBookAddDialog(AddressBookEntry e) : base(e == null ? "Add To Address Book" : "Edit Address Book")
        {
            this.MinimumSize = new Size(300, 330);

            if (e == null)
            {
                return;
            }

            txtName.Text = e.Name;
            txtAddress.Text = e.Address;
            txtDescription.Text = e.Description;
            txtPayID.Text = e.PaymentId;
        }

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
                    new Label { Text = "Name"},
                    txtName,
                    new Label { Text = "Description" },
                    txtDescription,
                    new Label { Text = "Address" },
                    txtAddress,
                    new Label { Text = "Payment ID" },
                    txtPayID
                }
            };
        }

        protected override void OnOk()
        {
            StringBuilder errors = new StringBuilder();

            //At a minimum we require a name and address
            //Description and payment id are not required

            if (string.IsNullOrEmpty(txtName.Text))
                errors.AppendLine("Name is not provided");

            if (string.IsNullOrEmpty(txtAddress.Text))
                errors.AppendLine("Address is not provided");

            string errorString = errors.ToString();
            if (!string.IsNullOrEmpty(errorString))
            {
                MessageBox.Show(this, $"Failed to add address:\r\n{errorString}", "Address Book",
                    MessageBoxButtons.OK, MessageBoxType.Error, MessageBoxDefaultButton.OK);
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
    }
}
