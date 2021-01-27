using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using AngryWasp.Helpers;
using AngryWasp.Logger;
using Nerva.Toolkit.Config;

#if UNIX
using Mono.Unix.Native;
using Nerva.Toolkit.Helpers.Native;
#endif

namespace Nerva.Toolkit.Helpers
{
    public static class ProcessManager
    {
        private static string ExeNameToProcessName(string exe)
        {
            exe = Path.GetFileNameWithoutExtension(exe);
#if OSX
            if (exe.Length > 15) // Mac truncates process names to 15 characters
                exe = exe.Substring(0, 15);
#endif

            return exe;
        }

        public static void Kill(string exe)
        {
            try
            {
                exe = ExeNameToProcessName(exe);
                var pl = GetRunningByName(exe);

                if (pl.Count == 0)
                {
                    Log.Instance.Write(Log_Severity.Info, $"No instances of {exe} to kill");
                    return;
                }

                foreach (Process p in pl)
                {
                    Log.Instance.Write(Log_Severity.Warning, $"Killing running instance of {exe} with id {p.Id}");
#if UNIX
                    UnixNative.Kill(p.Id, Signum.SIGABRT);
#else
                    p.Kill();
#endif
                    Log.Instance.Write($"Process {p.Id} killed");
                }
            }
            catch (Exception ex)
            {
                Log.Instance.WriteNonFatalException(ex, "Could not kill process");
            }
        }

#if UNIX

        public static Process[] PsFindByName(string fileName)
        {
            // Mac has a fucked up launchd process with the same name which messes up a simple search by name
            // So we need this mostrosity of a command to get the pid of the actual process and not the launchd process
            Process proc = Process.Start(new ProcessStartInfo(Path.Combine(Configuration.StorageDirectory, "FindProcesses.sh"), fileName)
            {
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            });

            proc.WaitForExit();

            var result = proc.StandardOutput.ReadToEnd().Trim();

            if (string.IsNullOrEmpty(result))
                return new Process[0];

            string[] split = result.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

            List<Process> returnValue = new List<Process>();

            foreach (var s in split)
            {
                int pid;
                if (int.TryParse(s, out pid))
                    returnValue.Add(Process.GetProcessById(pid));
                else
                    Debugger.Break();
            }

            return returnValue.ToArray();
        }

#endif

        public static bool IsRunning(string exe, out Process p)
        {
            exe = ExeNameToProcessName(exe);
            p = null;

            try
            {
                var pl = GetRunningByName(exe);

                if (pl.Count == 0)
                    return false;

                p = pl[0];

                if (p == null || p.HasExited)
                {
                    Log.Instance.Write(Log_Severity.Warning, $"CLI tool {exe} exited unexpectedly. Restarting");
                    p = null;
                    return false;
                }
                else
                    return true;
            }
            catch (Exception ex)
            {
                Log.Instance.Write(Log_Severity.Warning, ex.Message);
                return false;
            }
        }

        public static List<Process> GetRunningByName(string exe)
        { 
            exe = ExeNameToProcessName(exe);
            List<Process> r = new  List<Process>();
#if UNIX
            foreach (var p in PsFindByName(exe))
                if (!p.HasExited)
                    r.Add(p);
#else
            foreach (var p in Process.GetProcessesByName(exe))
                if (!p.HasExited)
                    r.Add(p);
#endif
            return r;
        }

        public static void StartExternalProcess(string exePath, string args)
        {
            Log.Instance.Write($"Starting process {ExeNameToProcessName(exePath)} {args}");

            Process proc = Process.Start(new ProcessStartInfo(exePath, args)
            {
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            });
        }

        public static string CycleLogFile(string path)
        {
            string logFile = path + ".log";
            string oldLogFile = logFile + ".old";

            try
            {
                if (File.Exists(oldLogFile))
                    File.Delete(oldLogFile);

                if (File.Exists(logFile))
                    File.Move(logFile, oldLogFile);
            }
            catch (Exception)
            {
                logFile = FileHelper.RenameDuplicateFile(logFile);
                Log.Instance.Write(Log_Severity.Warning, $"Cannot cycle log file. New log will be written to {logFile}");
                return logFile;
            }

            return logFile;
        }

        public static string GenerateCommandLine(string exePath, RpcDetails d)
        {
            string arg = $"--log-file \"{CycleLogFile(exePath)}\"";

            if (Configuration.Instance.Testnet)
            {
                Log.Instance.Write("Connecting to testnet");
                arg += " --testnet";
            }

            arg += $" --rpc-bind-port {d.Port}";
            arg += " --log-level 1";

            return arg;
        }
    }
}