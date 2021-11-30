#if UNIX

using Mono.Unix.Native;
using AngryWasp.Logger;

namespace Nerva.Desktop.Helpers.Native
{
    public static class UnixNative
    {
        public static void Chmod(string path, uint mode)
        {
            if(Syscall.chmod(path, (FilePermissions)mode) != 0)
                Log.Instance.Write(Log_Severity.Fatal, "Syscall 'chmod' failed.");
        }

        public static string Sysname()
        {
            Utsname results;

            if(Syscall.uname(out results) != 0)
            {
                Log.Instance.Write(Log_Severity.Fatal, "Syscall 'uname' failed.");
                return null;
            }

            return results.sysname.ToLower();
        }

        public static void Symlink(string source, string target)
        {
            if(Syscall.symlink(source, target) != 0)
                Log.Instance.Write(Log_Severity.Warning, "Syscall 'symlink' failed. Possibly already exists.");
        }

        public static void Kill(int pid, Signum sig)
        {
            if (Syscall.kill(pid, sig) != 0)
                Log.Instance.Write(Log_Severity.Warning, $"Syscall 'kill' failed to kill process {pid}.");
        }
    }
}

#endif