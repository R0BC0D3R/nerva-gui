#if UNIX

using Mono.Unix.Native;
using Nerva.Desktop.Helpers;

namespace Nerva.Desktop.Helpers.Native
{
    public static class UnixNative
    {
        public static void Chmod(string path, uint mode)
        {
            if(Syscall.chmod(path, (FilePermissions)mode) != 0)
            {
                Logger.LogError("UN.UN", "Syscall 'chmod' failed.");
            }
        }

        public static string Sysname()
        {
            Utsname results;

            if(Syscall.uname(out results) != 0)
            {
                Logger.LogError("UN.SYS", "Syscall 'uname' failed.");
                return null;
            }

            return results.sysname.ToLower();
        }

        public static void Symlink(string source, string target)
        {
            if(Syscall.symlink(source, target) != 0)
            {
                Logger.LogError("UN.SYM", "Syscall 'symlink' failed. Possibly already exists.");
            }
        }

        public static void Kill(int pid, Signum sig)
        {
            if (Syscall.kill(pid, sig) != 0)
            {
                Logger.LogError("UN.KIL", $"Syscall 'kill' failed to kill process {pid}.");
            }
        }
    }
}

#endif