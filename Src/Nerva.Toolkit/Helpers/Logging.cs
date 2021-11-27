using System;
using AngryWasp.Logger;

namespace Nerva.Toolkit.Helpers
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
                    Log.Instance.Write(Log_Severity.Error, origin + ":" + exception.Message);
                }
                else
                {
                    Log.Instance.Write(Log_Severity.Error, origin + ":" + message + ", " + exception.Message);
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