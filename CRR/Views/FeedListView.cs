namespace cFeed.Views
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Net;
  using System.Threading;
  using System.Threading.Tasks;
  using cFeed.Entities;
  using cFeed.Util;
  using CGui.Gui;
  using CSharpFunctionalExtensions;
  using JsonConfig;

  /// <summary>
  /// Defines the <see cref="FeedListView" />
  /// </summary>
  public class FeedListView : BaseView
  {
    internal dynamic footerFormat;
    internal dynamic headerFormat;

    private bool markAllread;

    private bool purge;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedListView"/> class.
    /// </summary>
    /// <param name="layout">Feed list layout JSON from configuration</param>
    public FeedListView(dynamic layout) : base((ConfigObject)layout)
    {
      headerFormat = Config.Global.UI.Strings.FeedListHeaderFormat;
      footerFormat = Config.Global.UI.Strings.FeedListFooterFormat;

      Configuration.Instance.OnConfigurationChangedHandler += Instance_OnConfigurationChangedHandler;
    }

    public Result<Picklist<RssFeed>> GetPicklist()
    {
      var result = _mainView.Controls.FirstOrDefault(x => x.GetType() == typeof(Picklist<RssFeed>)) as Picklist<RssFeed>;
      if (result == null)
      {
        return Result.Fail<Picklist<RssFeed>>("Missing list config.");
      }
      else
      {
        return Result.Ok<Picklist<RssFeed>>(result);
      }
    }

    /// <summary>
    /// Displays current view
    /// </summary>
    /// <param name="refresh">The <see cref="bool"/></param>
    /// <param name="feeds">The <see cref="IList{RssFeed}"/></param>
    public void Show(bool refresh, IList<RssFeed> feeds)
    {
      Result<IList<RssFeed>> getFeedListResult = GetFeedList(feeds);

      getFeedListResult
        .OnSuccess((items) => {
          ShowHeader(Format(headerFormat));
          ShowFooter(Format(footerFormat));
        })
        .OnSuccess(items => GetPicklist().OnSuccess(picklist =>
        {
          picklist.UpdateList(items);
          picklist.OnItemKeyHandler += FeedList_OnItemKeyHandler;
        }))
        .OnSuccess(list => ReloadAll(list, refresh))
        .OnSuccess(list => _mainView.Show());
    }

    /// <summary>
    /// Formats displayed string
    /// </summary>
    /// <param name="format">The <see cref="string"/></param>
    /// <returns>The <see cref="string"/></returns>
    private string Format(string format)
    {
      return format
        .Replace("%v", Configuration.VERSION)
        .Replace("%V", Configuration.MAJOR_VERSION);
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
        return PurgeDeletedArticles(parent);
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

      //search
      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.Search))
      {
        parent.IsDisplayed = false;
        var result = GlobalMethods.Search(_mainView);
        parent.IsDisplayed = true;
        return result;
      }
      return true;
    }

    private Result<IList<RssFeed>> GetFeedList(IList<RssFeed> feeds)
    {
      var rssFeeds = feeds
              .Where(item => item.Hidden == false)
              .Select((item, index) =>
              {
                item.Index = index;
                item.DisplayText = item.DisplayLine;
                return item;
              }).ToList();
      return Result.Ok<IList<RssFeed>>(rssFeeds);
    }

    private void Instance_OnConfigurationChangedHandler()
    {
      if (_mainView != null && _mainView.IsDisplayed)
      {
        _mainView.Refresh();
      }
    }

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
          Dictionary<string, object> choices = new Dictionary<string, object>
          {
            { Config.Global.UI.Strings.PromptAnswerNo, 1 },
            { Config.Global.UI.Strings.PromptAnswerYes, 2 }
          };

          var dialog = new Dialog(Config.Global.UI.Strings.PromptMarkAll, choices);
          dialog.ItemSelected += MarkAllDialog_ItemSelected;
          dialog.Show();

          _mainView?.Refresh();
          if (markAllread)
          {
            selectedItem?.MarkAllRead();
            markAllread = false;
          }

          return true;
        }
      }
      return true;
    }

    private void MarkAllDialog_ItemSelected(object sender, DialogChoice e)
    {
      markAllread |= e.DisplayText == Config.Global.UI.Strings.PromptAnswerYes as string;
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
    /// Shows selected feed articles
    /// </summary>
    /// <param name="selectedItem">Selected <see cref="RssFeed"/> item</param>
    /// <param name="parent">Parent <see cref="Picklist{RssFeed}"/></param>
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

    private void Purge_ItemSelected(object sender, DialogChoice e)
    {
      purge |= e.DisplayText == Config.Global.UI.Strings.PromptAnswerYes as string;
    }

    /// <summary>
    /// Purges articles marked for deletion in selected <see cref="RssFeed"/>.
    /// </summary>
    /// <param name="parent"></param>
    /// <returns></returns>
    private bool PurgeDeletedArticles(Picklist<RssFeed> parent)
    {
      Dictionary<string, object> choices = new Dictionary<string, object>
      {
        { Config.Global.UI.Strings.PromptAnswerYes, 1 },
        { Config.Global.UI.Strings.PromptAnswerNo, 2 }
      };

      var dialog = new Dialog(Config.Global.UI.Strings.PromptPurge, choices);
      dialog.ItemSelected += Purge_ItemSelected;
      dialog.Show();

      _mainView?.Refresh();
      if (purge)
      {
        foreach (var feed in parent.ListItems.Where(x => !x.IsDynamic))
        {
          if (!feed.IsProcessing)
          {
            feed.Purge();
          }
        }
        purge = false;
      }
      return true;
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
        logger.Error(x);
        Feed.DisplayText = Feed.DisplayLine + " ERROR:" + x.Message;
      }
      catch (Exception x)
      {
        logger.Error("Error loading " + Feed.FeedUrl);
        logger.Error(x);
        Feed.DisplayText = Feed.DisplayLine + " ERROR!";
      }
    }
  }
}