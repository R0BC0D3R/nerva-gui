using System;
using System.IO;
using AngryWasp.Helpers;
using AngryWasp.Logger;
using AngryWasp.Serializer;
using AngryWasp.Serializer.Serializers;
using Eto;
using Eto.Forms;
using Nerva.Toolkit.CLI;
using Nerva.Toolkit.Config;
using Nerva.Toolkit.Helpers;

namespace Nerva.Toolkit
{
    public class Program
	{
		/// <summary>
		/// Program entry point
		/// Available command line arguments
		/// --log-file: Location to write a log file to
		/// --config-file: Location to load a config file from
		/// --new-daemon: Kill any running daemon instances and restart them.
		/// --rpc-log-level: Log level fo the RPC library
		/// </summary>
		[STAThread]
		public static void Main(string[] args)
		{
            //Workaround for debugging on Mac
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            CommandLineParser cmd = CommandLineParser.Parse(args);

			string logFile, configFile;

			ParseFileArguments(cmd, out logFile, out configFile);

			InitializeLog(logFile);

			Serializer.Initialize();

			AddressBook.Load();

			bool newFile;

			Configuration.Load(configFile, out newFile);
			Nerva.Rpc.Configuration.ErrorLogVerbosity = Configuration.Instance.LogRpcErrors ? 
				Nerva.Rpc.Error_Log_Verbosity.Normal : Nerva.Rpc.Error_Log_Verbosity.None;
			
			Cli.Instance.KillCliProcesses(FileNames.RPC_WALLET);

			Configuration.Instance.NewDaemonOnStartup = cmd["new-daemon"] != null;

			if (cmd["rpc-log-level"] != null)
				Nerva.Rpc.Configuration.ErrorLogVerbosity = (Rpc.Error_Log_Verbosity)new UintSerializer().Deserialize(cmd["rpc-log-level"].Value);

			if (Nerva.Rpc.Configuration.ErrorLogVerbosity != Rpc.Error_Log_Verbosity.None)
			{
				Log.Instance.Write(Log_Severity.Warning, "RPC logging != NONE. Sensitive information may be written to the terminal or log file");
				Nerva.Rpc.Configuration.TraceRpcData = true;
			}

			try
			{
				Platform platform = Eto.Platform.Detect;
				Log.Instance.Write($"Platform detected as {platform.ToString()}");
				new Application(platform).Run(new MainForm(newFile));
			}
			catch (Exception ex)
			{
				Log.Instance.WriteNonFatalException(ex);

				Cli.Instance.Daemon.StopCrashCheck();
				Cli.Instance.Wallet.StopCrashCheck();

				Cli.Instance.Wallet.ForceClose();
				Cli.Instance.Daemon.ForceClose();

				Configuration.Save();
				Log.Instance.Write(Log_Severity.Fatal, "PROGRAM TERMINATED");
			}
			
			Shutdown();
		}

		public static void Shutdown()
		{
			//Prevent the daemon restarting automatically before telling it to stop
			if (Configuration.Instance.Daemon.StopOnExit)
			{
				Cli.Instance.Daemon.StopCrashCheck();
				Cli.Instance.Daemon.Interface.StopDaemon();
			}

			//be agressive and make sure it is dead
			Cli.Instance.Wallet.StopCrashCheck();
			Cli.Instance.Wallet.ForceClose();

			Configuration.Save();
			Log.Instance.Shutdown();

			Environment.Exit(0);
		}

		private static void ParseFileArguments(CommandLineParser cmd, out string logFile, out string configFile)
		{
			logFile = Path.Combine(Environment.CurrentDirectory, Constants.DEFAULT_LOG_FILENAME);
			configFile = Path.Combine(Environment.CurrentDirectory, Constants.DEFAULT_CONFIG_FILENAME);

			var lf = cmd["log-file"];
			var cf = cmd["config-file"];

			if (lf != null && !string.IsNullOrEmpty(lf.Value))
			{
				string newLf = Path.GetFullPath(lf.Value);
				if (Directory.Exists(Path.GetDirectoryName(newLf)))
					logFile = newLf;
			}

			if (cf != null && !string.IsNullOrEmpty(cf.Value))
			{
				string newCf = Path.GetFullPath(cf.Value);
				if (Directory.Exists(Path.GetDirectoryName(newCf)))
					configFile = newCf;
			}
		}	

		private static void InitializeLog(string logPath)
		{
			Log.CreateInstance(true, logPath);
			Log.Instance.Write($"NERVA Unified Toolkit. Version {Constants.LONG_VERSION}");

			//Crash the program if not 64-bit
			if (!Environment.Is64BitOperatingSystem)
				Log.Instance.Write(Log_Severity.Fatal, "The NERVA Unified Toolkit is only available for 64-bit platforms");

			Log.Instance.Write(Log_Severity.None, "System Information:");
			Log.Instance.Write(Log_Severity.None, $"OS: {Environment.OSVersion.Platform} {Environment.OSVersion.Version}");
			Log.Instance.Write(Log_Severity.None, $"CPU Count: {Environment.ProcessorCount}");
			
			if (logPath != null)
				Log.Instance.Write($"Writing log to file '{logPath}'");
		}
	}
}
