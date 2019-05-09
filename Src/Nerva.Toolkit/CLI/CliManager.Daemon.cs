using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using AngryWasp.Logger;
using Nerva.Toolkit.Config;
using Nerva.Toolkit.Helpers;

namespace Nerva.Toolkit.CLI
{
    public class DaemonCliTool : CliTool<DaemonInterface>
    {
        public override string FullExeName => FileNames.GetFormattedCliExeName(FileNames.NERVAD);

        public DaemonCliTool(Cli controller) : base(controller, new DaemonInterface()) { }

        public override void Create(string exe, string args)
        {
            Log.Instance.Write($"Starting process {exe} {args}");

            switch (OS.Type)
            {
                case OS_Type.Linux:
                case OS_Type.Osx:
                    {
                    //On Linux we have to use the --detach option to keep
                    //the daemon running after the wallet closes
                    //Using --detach spawns the daemon in a new process, different to the one we originally
                    //spawned on the next line. So we have to wait for that first one to exit, then
                    //do a search for the new nervad process and link to that. 

                    Process proc = Process.Start(new ProcessStartInfo(exe, args)
                    {
                        UseShellExecute = false,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    });

                    proc.WaitForExit();
        
                    string n = Path.GetFileNameWithoutExtension(exe);
                    var p = CliInterface.GetRunningProcesses(n);

                    if (p.Count == 1)
                        controller.DoProcessStarted(exe, p[0]);
                    else
                        Log.Instance.Write(Log_Severity.Fatal, $"Error creating CLI process {exe}");
                }
                break;
                case OS_Type.Windows:
                {
                    //The --detach option is not available on Windows. So we just start the daemon.
                    Process proc = Process.Start(new ProcessStartInfo(exe, args)
                    {
                        UseShellExecute = false,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    });
                    
                    controller.DoProcessStarted(exe, proc);
                }
                break;
            }
        }

        bool threadRunning = false;

        public void StartCrashCheck()
        {
            Thread t = new Thread(new ThreadStart(() =>
            {
                threadRunning = true;
                
                while (doCrashCheck)
                {
                    try
                    {
                        Process p = null;

                        if (!doCrashCheck)
                            break;

                        if (!controller.IsAlreadyRunning(BaseExeName, out p))
                        {
                            if (!doCrashCheck)
                                break;

                            ManageCliProcess();

                            if (!doCrashCheck)
                                break;

                            Create(FileNames.GetCliExePath(BaseExeName), GenerateCommandLine());
                            Log.Instance.Write($"Connecting to process {BaseExeName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Instance.WriteFatalException(ex);
                    }

                    Thread.Sleep(Constants.ONE_SECOND);
                }

                threadRunning = false;
            }));

            t.Start();
        }

        public void ResumeCrashCheck()
        {
            doCrashCheck = true;

            if (!threadRunning)
                StartCrashCheck();
        }

        public void StopCrashCheck()
        {
            doCrashCheck = false;
        }

        public override string GenerateCommandLine()
        {
            string a = GetBaseCommandLine(BaseExeName, Configuration.Instance.Daemon.Rpc);

            if (Configuration.Instance.Daemon.AutoStartMining)
            {
                string ma = Configuration.Instance.Daemon.MiningAddress;

                Log.Instance.Write($"Enabling startup mining @ {ma}");
                a += $" --start-mining {ma} --mining-threads {Configuration.Instance.Daemon.MiningThreads}";
            }
            
            if (OS.IsUnix())
                a += " --detach";

            return a;
        }

        public override void ManageCliProcess()
        {
            bool reconnect = Configuration.Instance.ReconnectToDaemonProcess;
            bool createNew = Configuration.Instance.NewDaemonOnStartup;

            controller.ManageCliProcesses(BaseExeName, reconnect, ref createNew);

            Configuration.Instance.NewDaemonOnStartup = createNew;
        }
    }
}