using System;
using AngryWasp.Logger;

namespace Nerva.Desktop.Helpers
{
    public static class Logging 
    {
        public static void LogException(string origin, Exception exception)
        {
            LogException(origin, "", exception);
        }

        public static void LogException(string origin, string message, Exception exception)
        {
            try
            {
                if (string.IsNullOrEmpty(message))
                {
                    Log.Instance.Write(Log_Severity.Error, origin + ": Message: " + exception.Message + " | Trace: " + exception.StackTrace);
                }
                else
                {
                    Log.Instance.Write(Log_Severity.Error, origin + ":" + message + ", Message: " + exception.Message + " | Trace: " + exception.StackTrace);
                }
            }
            catch (Exception ex)
            {
                // Logging failed.  Not much you can do.  Just try to continue.
                ex.Data.Clear();
            }
        }
    }
}