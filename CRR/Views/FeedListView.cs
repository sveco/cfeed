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
using CGui.Gui.Primitives;
using JsonConfig;

namespace cFeed
{
  public class FeedListView
  {
    private IList<RssFeed> feeds = new List<RssFeed>();
    private ArticleListView articleList;

    private Viewport _mainView;

    Header feedListHeader = new Header(Format(Config.Global.UI.Strings.ApplicationTitleFormat))
    {
      BackgroundColor = Configuration.GetColor(Config.Global.UI.Colors.FeedListHeaderBackground),
      ForegroundColor = Configuration.GetColor(Config.Global.UI.Colors.FeedListHeaderForeground),
      PadChar = '═'
    };

    private static string Format(string ApplicationTitle)
    {
      return ApplicationTitle
          .Replace("%v", Configuration.VERSION)
          .Replace("%V", Configuration.MAJOR_VERSION);
    }

    Footer feedListFooter = new Footer(Config.Global.UI.Strings.FeedListFooter)
    {
      BackgroundColor = Configuration.GetColor(Config.Global.UI.Colors.FeedListFooterBackground),
      ForegroundColor = Configuration.GetColor(Config.Global.UI.Colors.FeedListFooterForeground),
      PadChar = '═'
    };

    public FeedListView(IList<RssFeed> feeds)
    {
      this.feeds = feeds;
      articleList = new ArticleListView();
    }

    public void Show(bool refresh)
    {
      string prefix = (refresh ? "" : Configuration.LoadingPrefix);
      string suffix = (refresh ? "" : Configuration.LoadingSuffix);

      var rssFeeds = feeds
              .Where(item => item.Hidden == false)
              .Select((item, index) => new ListItem<RssFeed>()
              {
                Index = index,
                DisplayText = prefix + item.DisplayLine + suffix,
                Value = item
              }).ToList();


      //Initialize mainview
      _mainView = new Viewport();
      if (!(Config.Global.UI.Layout.WindowWidth is NullExceptionPreventer))
      {
        _mainView.Width = Config.Global.UI.Layout.WindowWidth;
      }
      if (!(Config.Global.UI.Layout.WindowHeight is NullExceptionPreventer))
      {
        _mainView.Height = Config.Global.UI.Layout.WindowHeight;
      }

      var list = new Picklist<RssFeed>(rssFeeds, null);

      list.Top = Config.Global.UI.Layout.FeedListTop;
      list.Left = Config.Global.UI.Layout.FeedListLeft;
      list.Height = Config.Global.UI.Layout.FeedMaxItems;
      list.Width = Console.WindowWidth - Config.Global.UI.Layout.FeedListLeft - 1;
      list.OnItemKeyHandler += FeedList_OnItemKeyHandler;
      list.ShowScrollBar = true;

      _mainView.Controls.Add(feedListHeader);
      _mainView.Controls.Add(feedListFooter);
      _mainView.Controls.Add(list);

      ReloadAll(list, refresh);

      _mainView.Show();
    }

    private void ReloadAll(Picklist<RssFeed> parent, bool online) {
      new Thread(() =>
      {
        Thread.CurrentThread.IsBackground = true;
        /* first load online feeds */
        Parallel.ForEach(parent.ListItems.Where(i => i.Value.IsDynamic == false), (item) => {
        item.DisplayText = Configuration.LoadingPrefix + item.DisplayText + Configuration.LoadingSuffix;
          try
          {
            item.Value.Load(online);
            item.DisplayText = item.Value.DisplayLine;
          }
          catch (WebException x)
          {
            cFeed.Logging.Logger.Log(x);
            item.DisplayText = item.Value.DisplayLine + " ERROR:" + x.Message;
          }
          catch (Exception x)
          {
            cFeed.Logging.Logger.Log(LogLevel.Error, "Error loading " + item.Value.FeedUrl);
            cFeed.Logging.Logger.Log(x);
            item.DisplayText = item.Value.DisplayLine + " ERROR!";
          }

        });

        /* then load dynamic feeds */
        Parallel.ForEach(parent.ListItems.Where(i => i?.Value?.IsDynamic == true), (item) => {
          item.DisplayText = Configuration.LoadingPrefix + item.DisplayText + Configuration.LoadingSuffix;
          item.Value.Load(false);
          item.DisplayText = item.Value.DisplayLine;
        });

      }).Start();
    }

    private bool FeedList_OnItemKeyHandler(ConsoleKeyInfo key, ListItem<RssFeed> selectedItem, Picklist<RssFeed> parent)
    {
      //Open
      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.OpenFeed))
      {
        if (selectedItem != null)
        {
          if (!selectedItem.Value.IsProcessing)
          {
            parent.IsDisplayed = false;
            articleList.DisplayArticleList(selectedItem);
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
        if (!selectedItem.Value.IsProcessing)
        {
          /* just update dynamic feed */
          if (selectedItem.Value.IsDynamic == true)
          {
            selectedItem.Value.Load(false);
            selectedItem.DisplayText = selectedItem.Value.DisplayLine;
          }
          else
          {
            /* refresh current online feed */
            new Thread(delegate ()
            {
              selectedItem.DisplayText = Configuration.LoadingPrefix + selectedItem.DisplayText + Configuration.LoadingSuffix;
              selectedItem.Value.Load(true);
              selectedItem.DisplayText = selectedItem.Value.DisplayLine;
            }).Start();
          }
          /* then load dynamic feeds */
          Parallel.ForEach(parent.ListItems.Where(i => i.Value.IsDynamic == true), (item) => {
            item.DisplayText = Configuration.LoadingPrefix + item.DisplayText + Configuration.LoadingSuffix;
            item.Value.Load(false);
            item.DisplayText = item.Value.DisplayLine;
          });
        }
        return true;
      }

      //Mark all read
      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.MarkAllRead))
      {
        if (selectedItem != null)
        {
          if (!selectedItem.Value.IsProcessing)
          {
            var input = new Input(Config.Global.UI.Strings.PromptMarkAll)
            {
              Top = Console.WindowHeight - 2,
              ForegroundColor = Configuration.GetColor(Config.Global.UI.Colors.LinkInputForeground),
              BackgroundColor = Configuration.GetColor(Config.Global.UI.Colors.LinkInputBackground),
            };
            if(input.InputText == Config.Global.UI.Strings.PromptAnswerYes)
            {
              selectedItem.Value.MarkAllRead();
              selectedItem.DisplayText = selectedItem.Value.DisplayLine;
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
          if (!selectedItem.Value.IsProcessing)
          {
            var input = new Input(Config.Global.UI.Strings.PromptPurge)
            {
              Top = Console.WindowHeight - 2,
              ForegroundColor = Configuration.GetColor(Config.Global.UI.Colors.LinkInputForeground),
              BackgroundColor = Configuration.GetColor(Config.Global.UI.Colors.LinkInputBackground),
            };
            if (input.InputText == Config.Global.UI.Strings.PromptAnswerYes)
            {
              selectedItem.Value.Purge();
              selectedItem.DisplayText = selectedItem.Value.DisplayLine;
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

    internal void RefreshConfig()
    {
      if (feedListHeader != null && feedListHeader.IsDisplayed)
      {
        feedListHeader.DisplayText = Format(Config.Global.UI.Strings.ApplicationTitleFormat);
        feedListHeader.BackgroundColor = Configuration.GetColor(Config.Global.UI.Colors.FeedListHeaderBackground);
        feedListHeader.ForegroundColor = Configuration.GetColor(Config.Global.UI.Colors.FeedListHeaderForeground);
        feedListHeader.Refresh();
      }
      if (feedListFooter != null && feedListFooter.IsDisplayed)
      {
        feedListFooter.DisplayText = Config.Global.UI.Strings.FeedListFooter;
        feedListFooter.BackgroundColor = Configuration.GetColor(Config.Global.UI.Colors.FeedListFooterBackground);
        feedListFooter.ForegroundColor = Configuration.GetColor(Config.Global.UI.Colors.FeedListFooterForeground);
        feedListFooter.Refresh();
      }
    }
  }
}
