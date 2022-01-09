using System;
using System.IO;
using System.Reflection;		// Do not remove. Needed by Linux/macOS to compile
using AngryWasp.Cli.Args;
using AngryWasp.Serializer;
using Eto;
using Eto.Forms;
using Nerva.Desktop.CLI;
using Nerva.Desktop.Config;
using Nerva.Desktop.Helpers;
using Application = Eto.Forms.Application;

#if UNIX
using Nerva.Desktop.Helpers.Native;
#endif

namespace Nerva.Desktop
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
			{
				try
				{
					 Directory.CreateDirectory(Configuration.StorageDirectory);
				}
				catch (Exception exDir)
				{
					MessageBox.Show(Application.Instance.MainForm, "Storage DIR could not be created:\r\n" + Configuration.StorageDirectory + "\r\n" + exDir.Message, 
							MessageBoxButtons.OK, MessageBoxType.Error, MessageBoxDefaultButton.OK);
				}                
			}

			string logFile = ProcessManager.CycleLogFile(Path.Combine(Configuration.StorageDirectory, "nerva-gui"));
			string configFile = Path.Combine(Configuration.StorageDirectory, "app.config");

			Logger.InitializeLog(logFile);
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
				Logger.LogDebug("PROG.M", $"CLI path manually set to {p}");
				Configuration.Instance.ToolsPath = p;
			}

			if (args["wallet-path"] != null)
			{
				string p = args["wallet-path"].Value;
				Logger.LogDebug("PROG.M", $"Wallet path manually set to {p}");
				Configuration.Instance.Wallet.WalletDir = p;
			}

			try
			{
				Platform platform = Eto.Platform.Detect;
				Logger.LogDebug("PROG.M", $"Platform detected as {platform.ToString()}");
				new Application(platform).Run(new MainForm(newFile));
			}
			catch (Exception ex)
			{
				ErrorHandler.HandleException("P.M", ex, true);
				Shutdown(false);
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
			Logger.LogError("PROG.S", "PROGRAM TERMINATED");
			Logger.ShutdownLog();

			Environment.Exit(0);
		}
	}
}
