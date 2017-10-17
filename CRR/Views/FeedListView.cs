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
    private Viewport _mainView;
    dynamic headerFormat;
    dynamic footerFormat;

    private static string FormatFeedView(string format)
    {
      return format
        .Replace("%v", Configuration.VERSION)
        .Replace("%V", Configuration.MAJOR_VERSION);
    }
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

    public void Show(bool refresh, IList<RssFeed> feeds)
    {
      var rssFeeds = feeds
              .Where(item => item.Hidden == false)
              .Select((item, index) => {
                item.Index = index;
                item.DisplayText = item.DisplayLine;
                return item;
              }).ToList();

      var feedListHeader = _mainView.Controls.Where(x => x.GetType() == typeof(Header)).FirstOrDefault() as Header;
      if (feedListHeader != null)
      {
        feedListHeader.DisplayText = FormatFeedView(headerFormat);
      }

      var feedListFooter = _mainView.Controls.Where(x => x.GetType() == typeof(Footer)).FirstOrDefault() as Footer;
      if (feedListFooter != null)
      {
        feedListFooter.DisplayText = FormatFeedView(footerFormat);
      }

      var list = _mainView.Controls.Where(x => x.GetType() == typeof(Picklist<RssFeed>)).FirstOrDefault() as Picklist<RssFeed>;
      if (list == null) { throw new InvalidOperationException("Missing list config."); }
      list.UpdateList(rssFeeds);
      list.OnItemKeyHandler += FeedList_OnItemKeyHandler;

      ReloadAll(list, refresh);
      rssFeeds = null;

      _mainView.Show();
    }

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

    private void ReloadAll(Picklist<RssFeed> parent, bool online) {
      new Thread(() =>
      {
        Thread.CurrentThread.IsBackground = true;
        /* first load online feeds */
        Parallel.ForEach(parent.ListItems.Where(i => ((RssFeed)i).IsDynamic == false), (item) => {
          ReloadOne(item, online);
        });

        /* then load dynamic feeds */
        Parallel.ForEach(parent.ListItems.Where(i => ((RssFeed)i)?.IsDynamic == true), (item) => {
          ((RssFeed)item).Load(false);
        });

      }).Start();
    }
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

          Parallel.ForEach(parent.ListItems.Where(i => ((RssFeed)i).IsDynamic == true), (item) => {
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
            if(input.InputText == Config.Global.UI.Strings.PromptAnswerYes)
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

    internal void RefreshConfig()
    {
      //TODO: implement configuration refresh without restarting app.
    }
  }
}
