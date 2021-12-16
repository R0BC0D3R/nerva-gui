using System;
using Eto.Forms;

namespace Nerva.Desktop.Helpers
{
    public static class ErrorHandler
    {
        public static void HandleException(string origin, Exception exception, bool showMessage)
        {
            HandleException(origin, exception, string.Empty, showMessage);
        }

        public static void HandleException(string origin, Exception exception, string message, bool showMessage)
        {
            try
            {                
                if (string.IsNullOrEmpty(message))
                {
                    Logger.LogException(origin, exception);
                    if (showMessage)
                    {
                        Application.Instance.AsyncInvoke( () =>
                        {
                            MessageBox.Show(exception.Message, MessageBoxButtons.OK, MessageBoxType.Error, MessageBoxDefaultButton.OK);
                        });
                    }
                }
                else
                {
                    Logger.LogException(origin, message, exception);
                    if (showMessage)
                    {
                        Application.Instance.AsyncInvoke( () =>
                        {
                            MessageBox.Show(message + " : " + exception.Message, MessageBoxButtons.OK, MessageBoxType.Error, MessageBoxDefaultButton.OK);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException("EH.HE", ex);
                // Exception handling failed.  Not much you can do.  Just try to continue.
                ex.Data.Clear();
            }
        }
    }
}