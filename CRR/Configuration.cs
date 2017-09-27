﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JsonConfig;
using cFeed.Logging;

namespace cFeed
{
  public static class Configuration
  {
    private static Version version = Assembly.GetExecutingAssembly().GetName().Version;

		private static LogLevel DefaultLogLevel = LogLevel.Info;
		public static LogLevel ConfiguredLogLevel {
			get {
				if (Config.Global.Debug is NullExceptionPreventer)
				{
					return DefaultLogLevel;
				}
				else {
					LogLevel result = DefaultLogLevel;
					Enum.TryParse<LogLevel>(Config.Global.Debug, out result);
					return result;
				}
			}
		}

    public static readonly string ArticleRootPath = Config.Global.SavedFileRoot;
    public static readonly string LoadingSuffix = Config.Global.UI.Strings.LoadingSuffix;
    public static readonly string LoadingPrefix = Config.Global.UI.Strings.LoadingPrefix;
    public static readonly string ArticleTextHighlight = Configuration.TextColor.ForegroundColor(Config.Global.UI.Colors.ArticleTextHighlight);

    public static readonly string ArticleTextFeedUrlLabel = Config.Global.UI.Strings.ArticleTextFeedUrlLabel;
    public static readonly string ArticleTextTitleLabel = Config.Global.UI.Strings.ArticleTextTitleLabel;
    public static readonly string ArticleTextAuthorsLabel = Config.Global.UI.Strings.ArticleTextAuthorsLabel;
    public static readonly string ArticleTextLinkLabel = Config.Global.UI.Strings.ArticleTextLinkLabel;
    public static readonly string ArticleTextPublishDatelLabel = Config.Global.UI.Strings.ArticleTextPublishDatelLabel;

    public static readonly string ReplacementPattern = @"\%([a-zA-Z]):?([\d*])?([rl]?)?";

    /// <summary>
    /// Special tag that tells CGui to reset color to default
    /// </summary>
    public static readonly string ColorReset = "\x1b[Reset]";

    public static readonly string VERSION = version.ToString();
    public static readonly string MAJOR_VERSION = version.Major.ToString() + "." + version.Minor.ToString();

    /// <summary>
    /// Converts color name to "flag" that CGUI textbox renderer understands
    /// </summary>
    public static class TextColor
    {


      public static string ForegroundColor(string c)
      {
        return "\x1b[f:" + c + "]";
      }
      public static string BackgroundColor(string c)
      {
        return "\x1b[b:" + c + "]";
      }
    }

    private static string readStateRead = Config.Global.UI.Strings.ReadStateRead as string;
    private static string readStateNew = Config.Global.UI.Strings.ReadStateNew as string;
    private static string downloadStateDownloaded = Config.Global.UI.Strings.DownloadStateDownloaded as string;
    private static string downloadStatePending = Config.Global.UI.Strings.DownloadStatePending as string;
    private static string deletedState = Config.Global.UI.Strings.DeleteStateDeleted as string;
    private static string notDeletedState = Config.Global.UI.Strings.DeleteStateNotDeleted as string;

    internal static string GetDeletedState(bool deleted)
    {
      var width = Math.Max(deletedState.Length, notDeletedState.Length);
      var result = deleted ? deletedState : notDeletedState;
      return result.PadRight(width);
    }

    public static string GetReadState(bool IsNew)
    {
      var width = Math.Max(readStateRead.Length, readStateNew.Length);
      var result = IsNew ? readStateNew : readStateRead;
      return result.PadRight(width);
    }

    public static string GetDownloadState(bool IsDownloaded)
    {
      var width = Math.Max(downloadStateDownloaded.Length, downloadStatePending.Length);
      var result = IsDownloaded ? downloadStateDownloaded : downloadStatePending;
      return result.PadRight(width);
    }

    public static ConsoleColor GetColor(string color)
    {
      ConsoleColor result = new ConsoleColor();
      if (Enum.TryParse<ConsoleColor>(color, out result))
      {
        return result;
      }
      else
      {
        throw new ArgumentException("Unknow color name, see https://msdn.microsoft.com/en-us/library/system.consolecolor(v=vs.110).aspx for valid color names.");
      }

    }

    public static List<ConsoleKey> GetKeys(string[] keys)
    {
      List<ConsoleKey> result = new List<ConsoleKey>();
      foreach (var key in keys)
      {
        if (Enum.TryParse(key, out ConsoleKey k))
        {
          result.Add(k);
        }
        else
        {
          throw new ArgumentException("Unknow color name, see https://msdn.microsoft.com/en-us/library/system.consolekey(v=vs.110).aspx for valid key names.");
        }
      }
      return result;
    }

    public static ConsoleModifiers GetModifier(string modifier)
    {
      ConsoleModifiers result = new ConsoleModifiers();
      if (Enum.TryParse(modifier, out result))
      {
        return result;
      }
      else
      {
        throw new ArgumentException("Unknow color name, see https://msdn.microsoft.com/en-us/library/system.consolekey(v=vs.110).aspx for valid key names.");
      }
    }

    public static ConsoleModifiers GetBitviseModifiers(string[] modifiers)
    {
      return modifiers.Select(x => GetModifier(x)).Aggregate<ConsoleModifiers>((running, next) => (running | next));
    }

    public static bool VerifyKey(this ConsoleKeyInfo Key, dynamic KeyConfig)
    {
      string[] keys = KeyConfig.Key;
      string[] modifiers = KeyConfig.Modifiers;
      if (GetKeys(keys).Contains(Key.Key))
      {
        if (modifiers.Length > 0)
        {
          if ((Key.Modifiers == GetBitviseModifiers(modifiers)))
          {
            return true;
          }
          else { return false; }
        }
        if (Key.Modifiers != 0)
        {
          return false;
        }
        return true;
      }
      return false;
    }
  }
}
