using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using JsonConfig;

namespace cFeed.Logging
{
  public sealed class Logger
  {
    private static readonly LogLevel DefaultLogLevel = LogLevel.Info;
    public static LogLevel ConfiguredLogLevel
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
          Enum.TryParse<LogLevel>(Config.Global.Debug, out result);
          return result;
        }
      }
    }

    private static readonly Logger instance = new Logger();
    private static ConcurrentQueue<LogData> logQueue = new ConcurrentQueue<LogData>();
    private static int queueSize = 1;
    private static int maxLogAge = 10;
    private static string logDir = "";
    private static string logFile = "log.txt";
    private static DateTime LastFlushed = DateTime.Now;
    private static LogLevel _defaultLevel = LogLevel.Info;

    // Explicit static constructor to tell C# compiler
    // not to mark type as beforefieldinit
    static Logger()
    {
    }

    private Logger()
    {
    }

    public static Logger Instance
    {
      get
      {
        return instance;
      }
    }

    ~Logger()
    {
      FlushLog();
    }

    private static void FlushLog()
    {
      System.Threading.ThreadPool.QueueUserWorkItem(q =>
      {
        while (logQueue.Count > 0)
        {
          LogData entry;
          logQueue.TryDequeue(out entry);
          string logPath = logDir + entry.LogDate + "_" + logFile;
          try
          {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(logPath, true, System.Text.Encoding.UTF8))
            {
              file.WriteLine(string.Format("{0}\t{1}", entry.LogTime, entry.Message));
            }
          }
          catch (IOException)
          {
            Thread.Sleep(100);
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(logPath, true, System.Text.Encoding.UTF8))
            {
              file.WriteLine(string.Format("{0}\t{1}", entry.LogTime, entry.Message));
            }
          }
        }
      });
    }

    private static bool DoPeriodicFlush()
    {
      TimeSpan logAge = DateTime.Now - LastFlushed;
      if (logAge.TotalSeconds >= maxLogAge)
      {
        LastFlushed = DateTime.Now;
        return true;
      }
      else
      {
        return false;
      }
    }

    public static void Log(string message)
    {
      Log(_defaultLevel, message);
    }

    public static void Log(LogLevel level, string message)
    {
      if (ConfiguredLogLevel > level)
        return;

      lock (logQueue)
      {
        LogData logEntry = new LogData(level, message);
        logQueue.Enqueue(logEntry);

        if (logQueue.Count >= queueSize || DoPeriodicFlush())
        {
          FlushLog();
        }
      }
    }

    public static void Log(Exception ex)
    {
      if (ConfiguredLogLevel > LogLevel.Error)
        return;

      Log(LogLevel.Error, ex);
    }

    public static void Log(LogLevel level, Exception ex)
    {
      lock (logQueue)
      {
        LogData logEntry = new LogData(level, ex);
        logQueue.Enqueue(logEntry);

        if (logQueue.Count >= queueSize || DoPeriodicFlush())
        {
          FlushLog();
        }
      }
    }
  }
}
