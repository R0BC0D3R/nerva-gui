using Eto.Forms;

namespace Nerva.Toolkit.Content.Dialogs
{
    public class TextDialog : DialogBase<DialogResult>
	{
        private string text;
        public string Text => text;

        TextBox txtText = new TextBox();

        public TextDialog(string title, bool readOnly, string text = null) : base(title)
        {
            this.text = text;
            txtText.Text = text;
            txtText.ReadOnly = readOnly;
        }

        protected override void OnOk()
        {
            this.text = txtText.Text;
            this.Close(DialogResult.Ok);
        }

        protected override void OnCancel()
        {
            this.text = null;
            this.Close(DialogResult.Cancel);
        }

        protected override void ConstructContent()
        {
            base.ConstructContent();
            txtText.Focus();
        }

        protected override Control ConstructChildContent()
        {
            return new TableLayout
            {
                Padding = 10,
				Spacing = new Eto.Drawing.Size(10, 10),
                Rows = {
                    txtText,
                    new TableRow { ScaleHeight = true }
                }
            };
        }
    }
}