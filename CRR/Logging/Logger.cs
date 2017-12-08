using System;
using System.Collections.Concurrent;
using JsonConfig;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace cFeed.Logging
{
  public sealed class Log
  {
    LoggingConfiguration config = new LoggingConfiguration();
    private static string logDir = "";
    private static string logFile = "log.txt";
    public readonly Logger Logger = LogManager.GetLogger("Log");


    private static readonly LogLevel DefaultLogLevel = LogLevel.Info;
    public LogLevel ConfiguredLogLevel
    {
      get
      {
        if (Config.Global.Debug is NullExceptionPreventer)
        {
          return DefaultLogLevel;
        }
        else
        {
          LogLevel result = DefaultLogLevel;
          try
          {
            result = LogLevel.FromString(Config.Global.Debug);
          }
          catch (Exception) {
            //No point logging error, logger is not ready
            //Logger.Warn("Unable to parse log level, using default.");
          }
          return result;
        }
      }
    }

    private static readonly Log instance = new Log();
    private static LogLevel _defaultLevel = LogLevel.Info;

    // Explicit static constructor to tell C# compiler
    // not to mark type as beforefieldinit
    static Log()
    {
    }

    private Log()
    {
      var fileTarget = new FileTarget();
      config.AddTarget("file", fileTarget);
      string logPath = logDir + DateTime.Now.Date.ToString("yyyyMMdd") + "_" + logFile;
      fileTarget.FileName =  logPath;
      fileTarget.Layout = "${longdate}|${level:uppercase=true}|${logger}|${message}";
      var rule2 = new LoggingRule("*", ConfiguredLogLevel, fileTarget);
      config.LoggingRules.Add(rule2);
      LogManager.Configuration = config;
    }

    public static Log Instance
    {
      get
      {
        return instance;
      }
    }
  }
}
