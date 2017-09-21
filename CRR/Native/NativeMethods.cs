using System;
using System.Runtime.InteropServices;

namespace cFeed.Native
{
    public static class NativeMethods
    {
        public enum CtrlTypes
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        internal static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        internal static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        [DllImport("Kernel32")]
        internal static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);

        internal delegate bool HandlerRoutine(CtrlTypes CtrlType);
    }
}
