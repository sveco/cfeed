using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JsonConfig;
using cFeed.Logging;
using System.Collections.ObjectModel;

namespace cFeed
{
  public static class Configuration
  {
    private static Version version = Assembly.GetExecutingAssembly().GetName().Version;

    public static readonly string ArticleRootPath = Config.Global.SavedFileRoot;
    public static readonly string LoadingSuffix = Config.Global.UI.Strings.LoadingSuffix;
    public static readonly string LoadingPrefix = Config.Global.UI.Strings.LoadingPrefix;
    public static readonly string ArticleTextHighlight = GetForegroundColor(Config.Global.UI.Colors.ArticleTextHighlight);

    public static readonly string ArticleTextFeedUrlLabel = Config.Global.UI.Strings.ArticleTextFeedUrlLabel;
    public static readonly string ArticleTextTitleLabel = Config.Global.UI.Strings.ArticleTextTitleLabel;
    public static readonly string ArticleTextAuthorsLabel = Config.Global.UI.Strings.ArticleTextAuthorsLabel;
    public static readonly string ArticleTextLinkLabel = Config.Global.UI.Strings.ArticleTextLinkLabel;
    public static readonly string ArticleTextPublishDateLabel = Config.Global.UI.Strings.ArticleTextPublishDateLabel;

    private static readonly string readStateRead = Config.Global.UI.Strings.ReadStateRead as string;
    private static readonly string readStateNew = Config.Global.UI.Strings.ReadStateNew as string;
    private static readonly string downloadStateDownloaded = Config.Global.UI.Strings.DownloadStateDownloaded as string;
    private static readonly string downloadStatePending = Config.Global.UI.Strings.DownloadStatePending as string;
    private static readonly string deletedState = Config.Global.UI.Strings.DeleteStateDeleted as string;
    private static readonly string notDeletedState = Config.Global.UI.Strings.DeleteStateNotDeleted as string;

    public static readonly string ReplacementPattern = @"\%([a-zA-Z]):?([\d]*)?([rl]?)?";

    public static string Database;

    /// <summary>
    /// Special tag that tells CGui to reset color to default
    /// </summary>
    public static readonly string ColorReset = "\x1b[Reset]";

    public static readonly string VERSION = version.ToString();
    public static readonly string MAJOR_VERSION = version.Major.ToString() + "." + version.Minor.ToString();

    public static string GetForegroundColor(string colorName)
    {
      return "\x1b[f:" + colorName + "]";
    }
    public static string GetBackgroundColor(string colorName)
    {
      return "\x1b[b:" + colorName + "]";
    }
    
    internal static string GetDeletedState(bool deleted)
    {
      var width = Math.Max(deletedState.Length, notDeletedState.Length);
      var result = deleted ? deletedState : notDeletedState;
      return result.PadRightVisible(width);
    }

    public static string GetReadState(bool isNew)
    {
      var width = Math.Max(readStateRead.Length, readStateNew.Length);
      var result = isNew ? readStateNew : readStateRead;
      return result.PadRightVisible(width);
    }

    public static string GetDownloadState(bool isDownloaded)
    {
      var width = Math.Max(downloadStateDownloaded.Length, downloadStatePending.Length);
      var result = isDownloaded ? downloadStateDownloaded : downloadStatePending;
      return result.PadRightVisible(width);
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
  }
}
