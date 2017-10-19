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
  public class FeedListView
  {
    /// <summary>
    /// Defines the _mainView
    /// </summary>
    private Viewport _mainView;

    /// <summary>
    /// Defines the headerFormat
    /// </summary>
    internal dynamic headerFormat;

    /// <summary>
    /// Defines the footerFormat
    /// </summary>
    internal dynamic footerFormat;

    /// <summary>
    /// The FormatFeedView
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
    /// <param name="feedListLayout">The <see cref="dynamic"/></param>
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
    /// The ReloadOne
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
        if (selectedItem != null)
        {
          if (!selectedItem.IsProcessing)
          {
            var input = new Input(Config.Global.UI.Strings.PromptMarkAll)
            {
              Top = Console.WindowHeight - 2,
              ForegroundColor = Configuration.GetColor(Config.Global.UI.Colors.LinkInputForeground),
              BackgroundColor = Configuration.GetColor(Config.Global.UI.Colors.LinkInputBackground),
            };
            if (input.InputText == Config.Global.UI.Strings.PromptAnswerYes)
            {
              selectedItem.MarkAllRead();
              selectedItem.DisplayText = selectedItem.DisplayLine;
            }
            return true;
          }
        }
      }

      //Purge deleted
      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.Purge))
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
            return true;
          }
        }
      }

      //Redraw view
      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.RefreshView))
      {
        _mainView.Refresh();
      }
      return true;
    }
  }
}
