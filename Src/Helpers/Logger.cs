using System;
using AngryWasp.Logger;

namespace Nerva.Desktop.Helpers
{
    public static class Logger 
    {
        public static void InitializeLog(string logPath)
		{
			Log.CreateInstance(true, logPath);
			LogInfo("LOG.IL", $"NERVA Desktop. Version {Version.LONG_VERSION}");

			//Crash the program if not 64-bit
			if (!Environment.Is64BitOperatingSystem)
            {
                ErrorHandler.HandleException("LOG.IL", new Exception("NERVA Desktop is only available for 64-bit platforms"), true);
            }

			LogInfo("LOG.IL", "System Information:");
			LogInfo("LOG.IL", $"OS: {Environment.OSVersion.Platform} {Environment.OSVersion.Version}");
			LogInfo("LOG.IL", $"CPU Count: {Environment.ProcessorCount}");
			
			if (logPath != null)
            {
				LogInfo("LOG.IL", $"Writing log to file '{logPath}'");
            }
		}

        public static void ShutdownLog()
        {
            Log.Instance.Shutdown();
        }

        #region Exception Logging
        public static void LogException(string origin, Exception exception)
        {
            LogException(origin, string.Empty, exception);
        }

        public static void LogException(string origin, string message, Exception exception)
        {
            try
            {
                if (string.IsNullOrEmpty(message))
                {
                    Log.Instance.Write(Log_Severity.None, "ERROR\t" + DateTime.Now.ToString("u") + "\t" + origin + "\t Ex Msg: " + exception.Message + "\nTrace: " + exception.StackTrace);
                }
                else
                {
                    Log.Instance.Write(Log_Severity.None, "ERROR\t" + DateTime.Now.ToString("u") + "\t" + origin + "\t " + message + " | Ex Msg: " + exception.Message + "\nTrace: " + exception.StackTrace);
                }
            }
            catch (Exception ex)
            {
                // Logging failed.  Not much you can do.  Just try to continue.
                ex.Data.Clear();
            }
        }
        #endregion // Exception Logging

        #region Debug Logging
        public static void LogDebug(string message)
        {
            LogDebug(string.Empty, message);
        }

        public static void LogDebug(string origin, string message)
        {
            try
            {
                if(string.IsNullOrEmpty(origin))
                {
                    Log.Instance.Write(Log_Severity.None, "DEBUG\t" + DateTime.Now.ToString("u") + "\t" + message);
                }
                else
                {
                    Log.Instance.Write(Log_Severity.None, "DEBUG\t" + DateTime.Now.ToString("u") + "\t" + origin + "\t " + message);
                }
            }
            catch (Exception ex)
            {
                // Logging failed.  Not much you can do.  Just try to continue.
                ex.Data.Clear();
            }                    
        }
        #endregion // Debug Logging

        #region Info Logging
        public static void LogInfo(string message)
        {
            LogInfo(string.Empty, message);
        }

        public static void LogInfo(string origin, string message)
        {
            try
            {
                if(string.IsNullOrEmpty(origin))
                {
                    Log.Instance.Write(Log_Severity.None, "INFO\t" + DateTime.Now.ToString("u") + "\t" + message);
                }
                else
                {
                    Log.Instance.Write(Log_Severity.None, "INFO\t" + DateTime.Now.ToString("u") + "\t" + origin + "\t " + message);
                }
            }
            catch (Exception ex)
            {
                // Logging failed.  Not much you can do.  Just try to continue.
                ex.Data.Clear();
            }                    
        }
        #endregion // Info Logging

        #region Error Logging
        public static void LogError(string message)
        {
            LogError(string.Empty, message);
        }

        public static void LogError(string origin, string message)
        {
            try
            {
                if(string.IsNullOrEmpty(origin))
                {
                    Log.Instance.Write(Log_Severity.None, "ERROR\t" + DateTime.Now.ToString("u") + "\t" + message);
                }
                else
                {
                    Log.Instance.Write(Log_Severity.None, "ERROR\t" + DateTime.Now.ToString("u") + "\t" + origin + "\t " + message);
                }
            }
            catch (Exception ex)
            {
                // Logging failed.  Not much you can do.  Just try to continue.
                ex.Data.Clear();
            }                    
        }
        #endregion // Error Logging
    }
}