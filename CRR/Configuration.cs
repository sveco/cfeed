using System;
using System.Reflection;
using CGui.Gui.Primitives;
using JsonConfig;

namespace cFeed
{
  public class Configuration
  {
    #region Singleton implementation
    private static readonly Configuration instance = new Configuration();

    // Explicit static constructor to tell C# compiler
    // not to mark type as beforefieldinit
    static Configuration()
    {
    }

    private Configuration()
    {
      LoadConfig();
    }

    public static Configuration Instance
    {
      get { return instance; }
    }
    #endregion

    public delegate void OnConfigurationChanged();
    public event OnConfigurationChanged OnConfigurationChangedHandler;

    private static Version version = Assembly.GetExecutingAssembly().GetName().Version;

    public string ArticleRootPath { get; private set; }
    public string LoadingSuffix { get; private set; }
    public string LoadingPrefix { get; private set; }
    public string ArticleTextHighlight { get; private set; }

    public string ArticleTextFeedUrlLabel { get; private set; }
    public string ArticleTextTitleLabel { get; private set; }
    public string ArticleTextAuthorsLabel { get; private set; }
    public string ArticleTextLinkLabel { get; private set; }
    public string ArticleTextPublishDateLabel { get; private set; }

    private string readStateRead { get; set; }
    private string readStateNew { get; set; }
    private string downloadStateDownloaded { get; set; }
    private string downloadStatePending { get; set; }
    private string deletedState { get; set; }
    private string notDeletedState { get; set; }

    public static readonly string ReplacementPattern = @"\%([a-zA-Z]):?([\d]*)?([rl]?)?";

    public static string Database;

    /// <summary>
    /// Special tag that tells CGui to reset color to default
    /// </summary>
    public static readonly string ColorReset = "\x1b[Reset]";

    public static readonly string VERSION = version.ToString();
    public static readonly string MAJOR_VERSION = version.Major.ToString() + "." + version.Minor.ToString();

    private void LoadConfig()
    {
      ArticleRootPath = Config.Global.SavedFileRoot;
      LoadingSuffix = Config.Global.UI.Strings.LoadingSuffix;
      LoadingPrefix = Config.Global.UI.Strings.LoadingPrefix;
      ArticleTextHighlight = GetForegroundColor(Config.Global.UI.Colors.ArticleTextHighlight);

      ArticleTextFeedUrlLabel = Config.Global.UI.Strings.ArticleTextFeedUrlLabel;
      ArticleTextTitleLabel = Config.Global.UI.Strings.ArticleTextTitleLabel;
      ArticleTextAuthorsLabel = Config.Global.UI.Strings.ArticleTextAuthorsLabel;
      ArticleTextLinkLabel = Config.Global.UI.Strings.ArticleTextLinkLabel;
      ArticleTextPublishDateLabel = Config.Global.UI.Strings.ArticleTextPublishDateLabel;

      readStateRead = Config.Global.UI.Strings.ReadStateRead as string;
      readStateNew = Config.Global.UI.Strings.ReadStateNew as string;
      downloadStateDownloaded = Config.Global.UI.Strings.DownloadStateDownloaded as string;
      downloadStatePending = Config.Global.UI.Strings.DownloadStatePending as string;
      deletedState = Config.Global.UI.Strings.DeleteStateDeleted as string;
      notDeletedState = Config.Global.UI.Strings.DeleteStateNotDeleted as string;
    }

    public static string GetForegroundColor(string colorName)
    {
      return "\x1b[f:" + colorName + "]";
    }
    public static string GetBackgroundColor(string colorName)
    {
      return "\x1b[b:" + colorName + "]";
    }
    
    internal string GetDeletedState(bool deleted)
    {
      var width = Math.Max(deletedState.Length, notDeletedState.Length);
      var result = deleted ? deletedState : notDeletedState;
      return result.PadRightVisible(width);
    }

    public string GetReadState(bool isNew)
    {
      var width = Math.Max(readStateRead.Length, readStateNew.Length);
      var result = isNew ? readStateNew : readStateRead;
      return result.PadRightVisible(width);
    }

    public string GetDownloadState(bool isDownloaded)
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

    internal void RefreshConfig()
    {
      LoadConfig();
      OnConfigurationChangedHandler?.Invoke();
    }
  }
}
