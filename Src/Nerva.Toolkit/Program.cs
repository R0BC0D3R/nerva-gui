﻿using System;
using System.IO;
using System.Reflection;
using AngryWasp.Cli.Args;
using AngryWasp.Logger;
using AngryWasp.Serializer;
using Eto;
using Nerva.Toolkit.CLI;
using Nerva.Toolkit.Config;
using Nerva.Toolkit.Helpers;
using Application = Eto.Forms.Application;
using Log = AngryWasp.Logger.Log;

#if UNIX
using Nerva.Toolkit.Helpers.Native;
#endif

namespace Nerva.Toolkit
{
    public class Program
	{
		[STAThread]
		public static void Main(string[] rawArgs)
		{
            //Workaround for debugging on Mac
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            Arguments args = Arguments.Parse(rawArgs);

#if DEBUG
			args.Push(new Argument("log-cli-wallet", null));
#endif

			if (!Directory.Exists(Configuration.StorageDirectory))
                Directory.CreateDirectory(Configuration.StorageDirectory);

			string logFile = ProcessManager.CycleLogFile(Path.Combine(Configuration.StorageDirectory, "nerva-gui"));
			string configFile = Path.Combine(Configuration.StorageDirectory, "app.config");

#if UNIX
			string[] unixScripts = new string[] {
				"FindProcesses.sh"
			};

			foreach (var us in unixScripts)
			{
				var p = Path.Combine(Configuration.StorageDirectory, us);
				if (!File.Exists(p))
				{
					var rs = Assembly.GetExecutingAssembly().GetManifestResourceStream(us);
					using (var sr = new StreamReader(rs))
					{
						File.WriteAllText(p, sr.ReadToEnd());
						UnixNative.Chmod(p, 33261);
					}
				}
			}
#endif

			InitializeLog(logFile);
			Serializer.Initialize();
			AddressBook.Load();

			bool newFile;
			Configuration.Load(configFile, out newFile);

			var w = Configuration.Instance.Wallet;
			var d = Configuration.Instance.Daemon;
			
			WalletProcess.ForceClose();

			if (args["cli-path"] != null)
			{
				string p = args["cli-path"].Value;
				Log.Instance.Write($"CLI path manually set to {p}");
				Configuration.Instance.ToolsPath = p;
			}

			if (args["wallet-path"] != null)
			{
				string p = args["wallet-path"].Value;
				Log.Instance.Write($"Wallet path manually set to {p}");
				Configuration.Instance.Wallet.WalletDir = p;
			}

			try
			{
				Platform platform = Eto.Platform.Detect;
				Log.Instance.Write($"Platform detected as {platform.ToString()}");
				new Application(platform).Run(new MainForm(newFile));
			}
			catch (Exception ex)
			{
				AngryWasp.Logger.Log.Instance.WriteNonFatalException(ex);
				Shutdown(true);
			}
			
			Shutdown(false);
		}

		public static void Shutdown(bool forceDaemonShutdown)
		{
			//Prevent the daemon restarting automatically before telling it to stop
			if (Configuration.Instance.Daemon.StopOnExit || forceDaemonShutdown)
			{
				DaemonRpc.StopDaemon();
				DaemonProcess.ForceClose();
			}

			WalletProcess.ForceClose();

			Configuration.Save();
			Log.Instance.Write(Log_Severity.Fatal, "PROGRAM TERMINATED");
			Log.Instance.Shutdown();

			Environment.Exit(0);
		}	

		private static void InitializeLog(string logPath)
		{
			Log.CreateInstance(true, logPath);
			Log.Instance.Write($"NERVA Unified Toolkit. Version {Version.LONG_VERSION}");

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
