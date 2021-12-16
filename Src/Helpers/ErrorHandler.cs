using System;
using Eto.Forms;

namespace Nerva.Desktop.Helpers
{
    public static class ErrorHandler
    {
        public static void HandleException(string origin, Exception ex, bool showMessage)
        {
            HandleException(origin, ex, string.Empty, showMessage);
        }

        public static void HandleException(string origin, Exception ex, string message, bool showMessage)
        {
            if (string.IsNullOrEmpty(message))
            {
                Logger.LogException(origin, ex);
                if (showMessage)
                {
                    MessageBox.Show(Application.Instance.MainForm, ex.Message, MessageBoxButtons.OK, MessageBoxType.Error, MessageBoxDefaultButton.OK);
                }
            }
            else
            {
                Logger.LogException(origin, message, ex);
                if (showMessage)
                {
                    MessageBox.Show(Application.Instance.MainForm, message + " : " + ex.Message, MessageBoxButtons.OK, MessageBoxType.Error, MessageBoxDefaultButton.OK);
                }
            }
        }
    }
}