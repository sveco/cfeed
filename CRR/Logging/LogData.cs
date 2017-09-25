using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;

namespace cFeed.Logging
{
    public enum LogLevel {
        None,
        Info,
        Debug,
        Warning,
        Error,
        Critical
    }

    public class LogData
    {
        public string Message { get; set; }
        public string LogTime { get; set; }
        public string LogDate { get; set; }

        public LogData(LogLevel level, string message)
        {
            Message = "[" + level.ToString() + "] " +  message;
            LogDate = DateTime.Now.ToString("yyyy-MM-dd");
            LogTime = DateTime.Now.ToString("HH:mm:ss.fff tt");
        }

        public LogData(LogLevel level, Exception ex)
        {
            var stackFrame = FindStackFrame();
            var methodBase = GetCallingMethodBase(stackFrame);
            var callingMethod = methodBase.Name;
            var callingClass = methodBase.ReflectedType.Name;
            var lineNumber = stackFrame.GetFileLineNumber();

            Message = "[" + level.ToString() + "] ";
            Message += callingMethod + "." + callingClass + " at line " + lineNumber.ToString() + Environment.NewLine;
            Message += ex.Message + Environment.NewLine;

            LogDate = DateTime.Now.ToString("yyyy-MM-dd");
            LogTime = DateTime.Now.ToString("HH:mm:ss.fff tt");
        }

        private static MethodBase GetCallingMethodBase(StackFrame stackFrame)
        {
            return stackFrame == null
                ? MethodBase.GetCurrentMethod() : stackFrame.GetMethod();
        }

        private static StackFrame FindStackFrame()
        {
            var stackTrace = new StackTrace();
            for (var i = 0; i < stackTrace.GetFrames().Count(); i++)
            {
                var methodBase = stackTrace.GetFrame(i).GetMethod();
                var name = MethodBase.GetCurrentMethod().Name;
                if (!methodBase.Name.Equals("Log") && !methodBase.Name.Equals(name))
                    return new StackFrame(i, true);
            }
            return null;
        }
    }
}
