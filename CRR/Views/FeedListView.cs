namespace cFeed
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Net;
  using System.Threading;
  using System.Threading.Tasks;
  using cFeed.Entities;
  using cFeed.Logging;
  using cFeed.Util;
  using CGui.Gui;
  using JsonConfig;

  /// <summary>
  /// Defines the <see cref="FeedListView" />
  /// </summary>
  public class FeedListView : IDisposable
  {
    private Viewport _mainView;
    internal dynamic headerFormat;
    internal dynamic footerFormat;

    /// <summary>
    /// Formats displayed string
    /// </summary>
    /// <param name="format">The <see cref="string"/></param>
    /// <returns>The <see cref="string"/></returns>
    private static string Format(string format)
    {
      return format
        .Replace("%v", Configuration.VERSION)
        .Replace("%V", Configuration.MAJOR_VERSION);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedListView"/> class.
    /// </summary>
    /// <param name="feedListLayout">Feed list layout JSON from configuration</param>
    public FeedListView(dynamic feedListLayout)
    {
      //region controls
      _mainView = new Viewport();
      _mainView.Width = feedListLayout.Width;
      _mainView.Height = feedListLayout.Height;

      foreach (var control in feedListLayout.Controls)
      {
        var guiElement = ControlFactory.Get(control);
        if (guiElement != null) { _mainView.Controls.Add(guiElement); }
      }

      headerFormat = Config.Global.UI.Strings.FeedListHeaderFormat;
      footerFormat = Config.Global.UI.Strings.FeedListFooterFormat;

      Configuration.Instance.OnConfigurationChangedHandler += Instance_OnConfigurationChangedHandler;
    }

    private void Instance_OnConfigurationChangedHandler()
    {
      if (_mainView != null && _mainView.IsDisplayed)
      {
        _mainView.Refresh();
      }
    }

    /// <summary>
    /// Displays current view
    /// </summary>
    /// <param name="refresh">The <see cref="bool"/></param>
    /// <param name="feeds">The <see cref="IList{RssFeed}"/></param>
    public void Show(bool refresh, IList<RssFeed> feeds)
    {
      var rssFeeds = feeds
              .Where(item => item.Hidden == false)
              .Select((item, index) =>
              {
                item.Index = index;
                item.DisplayText = item.DisplayLine;
                return item;
              }).ToList();

      if (_mainView.Controls.Where(x => x.GetType() == typeof(Header)).FirstOrDefault() is Header feedListHeader)
      {
        feedListHeader.DisplayText = Format(headerFormat);
      }

      if (_mainView.Controls.Where(x => x.GetType() == typeof(Footer)).FirstOrDefault() is Footer feedListFooter)
      {
        feedListFooter.DisplayText = Format(footerFormat);
      }

      var list = _mainView.Controls.Where(x => x.GetType() == typeof(Picklist<RssFeed>)).FirstOrDefault() as Picklist<RssFeed>;
      if (list == null) { throw new InvalidOperationException("Missing list config."); }
      list.UpdateList(rssFeeds);
      list.OnItemKeyHandler += FeedList_OnItemKeyHandler;

      ReloadAll(list, refresh);
      rssFeeds = null;

      _mainView.Show();
    }

    /// <summary>
    /// Reloads selected <see cref="RssFeed"/>
    /// </summary>
    /// <param name="Feed">The <see cref="RssFeed"/></param>
    /// <param name="Refresh">The <see cref="bool"/></param>
    private void ReloadOne(RssFeed Feed, bool Refresh)
    {
      try
      {
        if (Feed.IsDynamic)
        {
          Feed.Load(false);
        }
        else
        {
          Feed.Load(Refresh);
        }
      }
      catch (WebException x)
      {
        cFeed.Logging.Logger.Log(x);
        Feed.DisplayText = Feed.DisplayLine + " ERROR:" + x.Message;
      }
      catch (Exception x)
      {
        cFeed.Logging.Logger.Log(LogLevel.Error, "Error loading " + Feed.FeedUrl);
        cFeed.Logging.Logger.Log(x);
        Feed.DisplayText = Feed.DisplayLine + " ERROR!";
      }
    }

    /// <summary>
    /// Reloads all feeds
    /// </summary>
    /// <param name="parent">The parent <see cref="Picklist{RssFeed}"/></param>
    /// <param name="online">Should it download feed? <see cref="bool"/></param>
    private void ReloadAll(Picklist<RssFeed> parent, bool online)
    {
      new Thread(() =>
      {
        Thread.CurrentThread.IsBackground = true;
        /* first load online feeds */
        Parallel.ForEach(parent.ListItems.Where(i => ((RssFeed)i).IsDynamic == false), (item) =>
        {
          ReloadOne(item, online);
        });

        /* then load dynamic feeds */
        Parallel.ForEach(parent.ListItems.Where(i => ((RssFeed)i)?.IsDynamic == true), (item) =>
        {
          ((RssFeed)item).Load(false);
        });
      }).Start();
    }

    /// <summary>
    /// The handles key press in feed list
    /// </summary>
    /// <param name="key">The <see cref="ConsoleKeyInfo"/></param>
    /// <param name="selectedItem">The <see cref="RssFeed"/></param>
    /// <param name="parent">The <see cref="Picklist{RssFeed}"/></param>
    /// <returns>The <see cref="bool"/></returns>
    private bool FeedList_OnItemKeyHandler(ConsoleKeyInfo key, RssFeed selectedItem, Picklist<RssFeed> parent)
    {
      //Open
      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.OpenFeed))
      {
        return OpenSelectedFeed(selectedItem, parent);
      }

      //Exit app
      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.QuitApp))
      {
        return false;
      }

      //Reload all
      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.ReloadAll))
      {
        ReloadAll(parent, true);
        parent.Refresh();
        return true;
      }

      //Reload
      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.Reload))
      {
        if (!selectedItem.IsProcessing)
        {
          ReloadOne(selectedItem, true);

          Parallel.ForEach(parent.ListItems.Where(i => ((RssFeed)i).IsDynamic == true), (item) =>
          {
            ((RssFeed)item).Load(false);
          });
        }
        return true;
      }

      //Mark all read
      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.MarkAllRead))
      {
        return MarkAllArticlesRead(selectedItem);
      }

      //Purge deleted
      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.Purge))
      {
        return PurgeDeletedArticles(selectedItem);
      }

      //Redraw view
      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.RefreshView))
      {
        _mainView.Refresh();
      }

      //open feed URL
      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.OpenBrowser))
      {
        return OpenFeedInBrowser(selectedItem);
      }
      return true;
    }

    private bool OpenFeedInBrowser(RssFeed selectedItem)
    {
      if (selectedItem != null && selectedItem.FeedUrl != null)
      {
        Browser.Open(selectedItem.FeedUrl);
      }
      return true;
    }

    /// <summary>
    /// Purges articles marked for deletion in selected <see cref="RssFeed"/>.
    /// </summary>
    /// <param name="selectedItem"></param>
    /// <returns></returns>
    private bool PurgeDeletedArticles(RssFeed selectedItem)
    {
      if (selectedItem != null)
      {
        if (!selectedItem.IsProcessing)
        {
          var input = new Input(Config.Global.UI.Strings.PromptPurge)
          {
            Top = Console.WindowHeight - 2,
            ForegroundColor = Configuration.GetColor(Config.Global.UI.Colors.LinkInputForeground),
            BackgroundColor = Configuration.GetColor(Config.Global.UI.Colors.LinkInputBackground),
          };
          if (input.InputText == Config.Global.UI.Strings.PromptAnswerYes)
          {
            selectedItem.Purge();
            selectedItem.DisplayText = selectedItem.DisplayLine;
          }
        }
      }
      return true;
    }

    private bool markAllread = false;
    /// <summary>
    /// Marks all articles in <see cref="RssFeed"/> as read.
    /// </summary>
    /// <param name="selectedItem"></param>
    /// <returns></returns>
    private bool MarkAllArticlesRead(RssFeed selectedItem)
    {
      if (selectedItem != null)
      {
        if (!selectedItem.IsProcessing)
        {
          Dictionary<string, object> choices = new Dictionary<string, object>();
          choices.Add(Config.Global.UI.Strings.PromptAnswerNo, 1);
          choices.Add(Config.Global.UI.Strings.PromptAnswerYes, 2);

          var dialog = new Dialog(Config.Global.UI.Strings.PromptMarkAll, choices);
          dialog.ItemSelected += MarkAllDialog_ItemSelected;
          dialog.Show();

          _mainView?.Refresh();
          if (markAllread)
          {
            selectedItem?.MarkAllRead();
          }

          return true;
        }
      }
      return true;
    }

    private void MarkAllDialog_ItemSelected(object sender, DialogChoice e)
    {
      if (e.DisplayText == Config.Global.UI.Strings.PromptAnswerYes as string)
      {
        markAllread = true;
      }
    }

    /// <summary>
    /// Shows selected feed articles
    /// </summary>
    /// <param name="selectedItem">Selected <see cref="RssFeed"/> item</param>
    /// <param name="parent">Parent <see cref="Picklist<RssFeed>"/></param>
    /// <returns></returns>
    private bool OpenSelectedFeed(RssFeed selectedItem, Picklist<RssFeed> parent)
    {
      if (selectedItem != null)
      {
        if (!selectedItem.IsProcessing)
        {
          parent.IsDisplayed = false;

          using (ArticleListView articleList = new ArticleListView(Config.Global.UI.Layout.ArticleList))
          {
            articleList.Show(selectedItem);
          }
          _mainView.Refresh();
        }
      }
      return true;
    }

    #region IDisposable Support
    private bool disposedValue = false; // To detect redundant calls

    protected virtual void Dispose(bool disposing)
    {
      if (disposedValue)
        return;
      
      if (disposing)
      {
        _mainView.Dispose();
      }

      // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
      // TODO: set large fields to null.

      disposedValue = true;
    }

    ~FeedListView()
    {
      // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
      Dispose(false);
    }

    // This code added to correctly implement the disposable pattern.
    public void Dispose()
    {
      // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
      Dispose(true);
      // TODO: uncomment the following line if the finalizer is overridden above.
      GC.SuppressFinalize(this);
    }
    #endregion
  }
}