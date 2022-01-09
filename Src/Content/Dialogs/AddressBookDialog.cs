using System;
using Eto.Forms;
using Eto.Drawing;
using Nerva.Desktop.Helpers;

namespace Nerva.Desktop.Content.Dialogs
{
    public class AddressBookDialog : DialogBase<DialogResult>
	{
        #region Local Variables
        private GridView grid;

        private Button btnAdd = new Button { Text = "Add" };
        private Button btnEdit = new Button { Text = "Edit" };
        private Button btnRemove = new Button { Text = "Remove" };        

        private AddressBookEntry selectedEntry;
        public AddressBookEntry SelectedEntry => selectedEntry;
        #endregion // Local Variables

        #region Constructor Methods
        public AddressBookDialog() : base("Address Book")
        {
            this.Width = 540;
            this.MinimumSize = new Size(500, 260);
            this.btnOk.Text = "Select";

            this.DefaultButton = btnCancel;

            btnAdd.Click += new EventHandler<EventArgs>(btnAdd_Click);
            btnEdit.Click += new EventHandler<EventArgs>(btnEdit_Click);
            btnRemove.Click += new EventHandler<EventArgs>(btnRemove_Click);

        }
        #endregion // Constructor Methods
    
        #region Base Class Methods
        protected override Control ConstructChildContent()
        {
            grid = new GridView
			{
				GridLines = GridLines.Horizontal,
				Columns = 
				{
					new GridColumn { DataCell = new TextBoxCell { Binding = Binding.Property<AddressBookEntry, string>(r => r.Name)}, HeaderText = "Name", Width = 120 },
					new GridColumn { DataCell = new TextBoxCell { Binding = Binding.Property<AddressBookEntry, string>(r => r.Description)}, HeaderText = "Description", Width = 100 },
					new GridColumn { DataCell = new TextBoxCell { Binding = Binding.Property<AddressBookEntry, string>(r => Conversions.WalletAddressShortForm(r.Address))}, HeaderText = "Address", Width = 180 },
                    new GridColumn { DataCell = new TextBoxCell { Binding = Binding.Property<AddressBookEntry, string>(r => Conversions.WalletAddressShortForm(r.PaymentId))}, HeaderText = "Pay ID", Width = 100 }    
				},
                DataStore = AddressBook.Instance.Entries
			};

            grid.SelectRow(0);

            grid.SelectedRowsChanged += new EventHandler<EventArgs>(grid_SelectedRowsChanged);
            grid.CellDoubleClick += new EventHandler<GridCellMouseEventArgs>(grid_CellDoubleClick);

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
        #endregion // Base Class Methods

        #region Event Methods
        private void btnAdd_Click(object sender, EventArgs e)
        {
            try
            {
                AddressBookAddDialog dlg = new AddressBookAddDialog(null);

                if (dlg.ShowModal() == DialogResult.Ok)
                {
                    AddressBook.Instance.Entries.Add(dlg.Entry);
                }

                grid.DataStore = AddressBook.Instance.Entries;
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("ABD.BAC", ex, true);
            }
        }
        private void btnEdit_Click(object sender, EventArgs e)
        {
            try
            {
                if (grid.SelectedRow == -1)
                {
                    MessageBox.Show(this, $"Please select an address to edit", "Address Book", MessageBoxButtons.OK, MessageBoxType.Error, MessageBoxDefaultButton.OK);
                    return;
                }
                
                AddressBookAddDialog dlg = new AddressBookAddDialog(AddressBook.Instance.Entries[grid.SelectedRow]);

                if (dlg.ShowModal() == DialogResult.Ok)
                {
                    AddressBook.Instance.Entries[grid.SelectedRow] = dlg.Entry;
                }

                grid.DataStore = AddressBook.Instance.Entries;        
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("ABD.BEC", ex, true);
            }
        }
        private void btnRemove_Click(object sender, EventArgs e)
        {
            try
            {
                if (grid.SelectedRow == -1)
                {
                   return;
                }

                AddressBook.Instance.Entries.RemoveAt(grid.SelectedRow);
                grid.DataStore = AddressBook.Instance.Entries;
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("ABD.BRC", ex, true);
            }
        }

        private void grid_SelectedRowsChanged(object sender, EventArgs e)
        {
            try
            {            
                btnRemove.Enabled = grid.SelectedRow != -1;
                selectedEntry = grid.SelectedRow != -1 ? AddressBook.Instance.Entries[grid.SelectedRow] : null;            
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("ABD.GSRC", ex, true);
            }
        }

        private void grid_CellDoubleClick(object sender, EventArgs e)
        {
            try
            {
                OnOk();
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException("ABD.GSRC", ex, true);
            }
        }
        #endregion // Event Methods
    }
}