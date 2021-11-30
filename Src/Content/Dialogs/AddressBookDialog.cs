using System;
using Eto.Forms;
using Nerva.Desktop.Helpers;

namespace Nerva.Desktop.Content.Dialogs
{
    public class AddressBookDialog : DialogBase<DialogResult>
	{
        private GridView grid;
        private Button btnAdd = new Button { Text = "Add" };
        private Button btnEdit = new Button { Text = "Edit" };
        private Button btnRemove = new Button { Text = "Remove" };

        private AddressBookEntry selectedEntry;

        public AddressBookEntry SelectedEntry => selectedEntry;

        public AddressBookDialog() : base("Address Book")
        {
            this.DefaultButton = btnCancel;

#if UNIX
            this.Height = 400;
#endif

            btnAdd.Click += (s, e) =>
            {
                AddressBookAddDialog dlg = new AddressBookAddDialog(null);

                if (dlg.ShowModal() == DialogResult.Ok)
                    AddressBook.Instance.Entries.Add(dlg.Entry);

                grid.DataStore = AddressBook.Instance.Entries;
            };

            btnEdit.Click += (s, e) =>
            {
                if (grid.SelectedRow == -1)
                {
                    MessageBox.Show(this, $"Please select an address to edit", "Address Book",
                        MessageBoxButtons.OK, MessageBoxType.Error, MessageBoxDefaultButton.OK);

                    return;
                }
                
                AddressBookAddDialog dlg = new AddressBookAddDialog(AddressBook.Instance.Entries[grid.SelectedRow]);

                if (dlg.ShowModal() == DialogResult.Ok)
                    AddressBook.Instance.Entries[grid.SelectedRow] = dlg.Entry;

                grid.DataStore = AddressBook.Instance.Entries;
            };

            btnRemove.Click += (s, e) =>
            {
                if (grid.SelectedRow == -1)
                    return;

                AddressBook.Instance.Entries.RemoveAt(grid.SelectedRow);
                grid.DataStore = AddressBook.Instance.Entries;
            };
        }

        protected override void OnShown(EventArgs e)
        {
#if WINDOWS
                this.Height = 400;
#endif
        }

        protected override Control ConstructChildContent()
        {
            grid = new GridView
			{
				GridLines = GridLines.Horizontal,
				Columns = 
				{
					new GridColumn { DataCell = new TextBoxCell { Binding = Binding.Property<AddressBookEntry, string>(r => r.Name)}, HeaderText = "Name" },
					new GridColumn { DataCell = new TextBoxCell { Binding = Binding.Property<AddressBookEntry, string>(r => r.Description)}, HeaderText = "Description" },
					new GridColumn { DataCell = new TextBoxCell { Binding = Binding.Property<AddressBookEntry, string>(r => 
                        Conversions.WalletAddressShortForm(r.Address))}, HeaderText = "Address" },
                    new GridColumn { DataCell = new TextBoxCell { Binding = Binding.Property<AddressBookEntry, string>(r => 
                        Conversions.WalletAddressShortForm(r.PaymentId))}, HeaderText = "Pay ID" }    
				},
                DataStore = AddressBook.Instance.Entries
			};

            grid.SelectedRowsChanged += (s, e) =>
            {
                btnRemove.Enabled = grid.SelectedRow != -1;
                selectedEntry = grid.SelectedRow != -1 ? AddressBook.Instance.Entries[grid.SelectedRow] : null;
            };

            return new StackLayout
			{
				Orientation = Orientation.Vertical,
				HorizontalContentAlignment = HorizontalAlignment.Stretch,
				VerticalContentAlignment = VerticalAlignment.Stretch,
                Padding = 10,
                Spacing = 10,
				Items = 
                {
                    new StackLayoutItem(new Scrollable
					{
						Content = grid
					}, true),
                    new StackLayoutItem(new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalContentAlignment = HorizontalAlignment.Right,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Padding = new Eto.Drawing.Padding(10, 0, 0, 0),
                        Spacing = 10,
                        Items =
                        {
                            new StackLayoutItem(null, true),
                            btnAdd,
                            btnEdit,
                            btnRemove
                        }
                    }, false)
                }
            };
        }

        protected override void OnOk()
        {
            AddressBook.Save();
            this.Close(DialogResult.Ok);
        }

        protected override void OnCancel()
        {
            this.Close(DialogResult.Cancel);
        }
    }
}