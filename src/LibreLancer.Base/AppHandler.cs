using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using LibreLancer.Dialogs;

namespace LibreLancer
{
    public static class AppHandler
    {
        [DllImport("kernel32.dll")]
        static extern bool SetDllDirectory(string directory);
        public static void Run(Action action, Action onCrash = null)
        {
            if (Platform.RunningOS == OS.Windows)
            {
                string bindir = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);
                var fullpath = Path.Combine(bindir, IntPtr.Size == 8 ? "x64" : "x86");
                SetDllDirectory(fullpath);
            }
#if !DEBUG
            var domain = AppDomain.CurrentDomain;
            domain.UnhandledException += (object sender, UnhandledExceptionEventArgs e) => {
                var ex = (Exception)(e.ExceptionObject);
                CrashWindow.Run("Uh-oh!", "Librelancer has crashed. See the log for more information.",
                FormatException(ex));
            };
            try
            {
#endif
            if (!Platform.CheckDependencies()) return;
            action();
#if !DEBUG
            }
            catch (Exception ex)
            {
                try { onCrash?.Invoke(); } catch { }
                CrashWindow.Run("Uh-oh!", "Librelancer has crashed. See the log for more information.", FormatException(ex));
            }

#endif
        }

        static string FormatException(Exception ex)
        {
            var builder = new StringBuilder();
            builder.AppendLine(ex.Message);
            builder.AppendLine(ex.StackTrace);
            Exception ex2 = ex;
            while ((ex2 = ex2.InnerException) != null)
            {
                builder.AppendLine($"Inner: {ex2.Message}");
                builder.AppendLine(ex2.StackTrace);
            }
            return builder.ToString();
        }
    }
}
