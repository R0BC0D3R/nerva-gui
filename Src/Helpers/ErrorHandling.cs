using System;
using Eto.Forms;

namespace Nerva.Desktop.Helpers
{
    public static class ErrorHandling
    {
        public static void HandleException(string origin, Exception ex, bool showMessage)
        {
            HandleException(origin, "", ex, showMessage);
        }

        public static void HandleException(string origin, string message, Exception ex, bool showMessage)
        {
            if (string.IsNullOrEmpty(message))
            {
                Logging.LogException(origin, ex);
                if (showMessage)
                {
                    MessageBox.Show(Application.Instance.MainForm, ex.Message, MessageBoxButtons.OK, MessageBoxType.Error, MessageBoxDefaultButton.OK);
                }
            }
            else
            {
                Logging.LogException(origin, message, ex);
                if (showMessage)
                {
                    MessageBox.Show(Application.Instance.MainForm, message + " : " + ex.Message, MessageBoxButtons.OK, MessageBoxType.Error, MessageBoxDefaultButton.OK);
                }
            }
        }
    }
}