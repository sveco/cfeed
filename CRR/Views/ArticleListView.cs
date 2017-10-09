using System;
using System.Linq;
using cFeed.Entities;
using cFeed.Util;
using CGui.Gui;
using CGui.Gui.Primitives;
using JsonConfig;
using LiteDB;

namespace cFeed
{
  public class ArticleListView : IDisposable
  {
    private ListItem<RssFeed> selectedFeed;
    //private ArticleView article;

    Header articleListHeader = new Header("")
    {
      BackgroundColor = Configuration.GetColor(Config.Global.UI.Colors.ArticleListHeaderBackground),
      ForegroundColor = Configuration.GetColor(Config.Global.UI.Colors.ArticleListHeaderForeground),
      PadChar = '-'
    };
    Footer articleListFooter = new Footer(Config.Global.UI.Strings.ArticleListFooter)
    {
      BackgroundColor = Configuration.GetColor(Config.Global.UI.Colors.ArticleListFooterBackground),
      ForegroundColor = Configuration.GetColor(Config.Global.UI.Colors.ArticleListFooterForeground),
      PadChar = '-'
    };

    private Viewport view;

    public ArticleListView()
    {
      //article = new ArticleView();

      view = new Viewport();
      view.Controls.Add(articleListHeader);
      view.Controls.Add(articleListFooter);
    }

    public void DisplayArticleList(ListItem<RssFeed> feed)
    {
      selectedFeed = feed;

      var items = feed.Value.FeedItems
          .OrderByDescending(x => x.PublishDate)
          .Where(x => x.Deleted == false)
          .Select((item, index) => new ListItem<FeedItem>()
          {
            Index = index,
            DisplayText = item.DisplayText,
            Value = item
          });

      //Console.Clear();

      if (articleListHeader != null)
      {
        articleListHeader.DisplayText = feed.Value.TitleLine;
        //articleListHeader.Show();
      }
      //if (articleListFooter != null) { articleListFooter.Show(); }

      var articleList = new Picklist<FeedItem>(items.ToList());
     // articleList.ListItems = items.ToList();

      if (Config.Global.UI.Layout.ArticleListHeight > 0)
      {
        articleList.Height = Config.Global.UI.Layout.ArticleListHeight;
      }
      else if (Config.Global.UI.Layout.ArticleListHeight < 0 && view != null)
      {
        articleList.Height = view.Height + Config.Global.UI.Layout.ArticleListHeight;
      }
      else
      {
        articleList.Height = 10;
      }
      articleList.Width = Console.WindowWidth - Config.Global.UI.Layout.ArticleListLeft;
      articleList.Top = Config.Global.UI.Layout.ArticleListTop;
      articleList.OnItemKeyHandler += ArticleList_OnItemKeyHandler;
      articleList.ShowScrollBar = true;

      view.Show();

      articleList.Show();

      selectedFeed.DisplayText = selectedFeed.Value.DisplayLine;
    }

    private bool ArticleList_OnItemKeyHandler(ConsoleKeyInfo key, ListItem<FeedItem> selectedItem, Picklist<FeedItem> parent)
    {
      //Open article
      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.OpenArticle))
      {
        using (ArticleView article = new ArticleView())
        {
          article.DisplayArticle(selectedItem, selectedFeed, parent);
        }
        view.Refresh();
        parent.Refresh();
        return true;
      }
      //Mark selected item as read
      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.MarkRead))
      {
        if (selectedItem != null)
        {
          selectedItem.Value.MarkAsRead();
          selectedItem.DisplayText = selectedItem.Value.DisplayText;
        }
      }
      //Mark all read
      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.MarkAllRead))
      {
        if (parent != null)
        {
          var input = new Input("Mark all articles as read [Y/N]:")
          {
            Top = Console.WindowHeight - 2,
            ForegroundColor = Configuration.GetColor(Config.Global.UI.Colors.LinkInputForeground),
            BackgroundColor = Configuration.GetColor(Config.Global.UI.Colors.LinkInputBackground),
          };
          if (input.InputText == "Y")
          {
            foreach (var item in parent.ListItems)
            {
              if (item.Value.IsNew == true)
              {
                item.Value.MarkAsRead();
                item.DisplayText = item.Value.DisplayText;
              }
            }
          }
        }
      }

      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.MarkUnread))
      {
        if (selectedItem != null)
        {
          selectedItem.Value.MarkUnread();
          selectedItem.DisplayText = selectedItem.Value.DisplayText;
        }
      }

      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.Delete))
      {
        if (selectedItem != null)
        {
          selectedItem.Value.MarkDeleted();
          selectedItem.DisplayText = selectedItem.Value.DisplayText;
        }
      }

      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.StepBack))
      {
        return false;
      }

      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.Reload))
      {
        if (selectedFeed != null)
        {
          if (articleListHeader != null)
          {
            articleListHeader.DisplayText = Configuration.LoadingPrefix + articleListHeader.DisplayText + Configuration.LoadingSuffix;
            articleListHeader.Refresh();
          }
          selectedFeed.Value.Load(true);

          var items = selectedFeed.Value.FeedItems
              .OrderByDescending(x => x.PublishDate)
              .Where(x => x.Deleted == false)
              .Select((item, index) =>
                {
                  item.Index = index + 1;
                  return new ListItem<FeedItem>()
                  {
                    Index = index,
                    DisplayText = item.DisplayText,
                    Value = item
                  };
                }
              );
          parent.UpdateList(items);
          parent.Refresh();

          if (articleListHeader != null)
          {
            articleListHeader.DisplayText = selectedFeed.Value.TitleLine;
            articleListHeader.Refresh();
          }
        }
      }

      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.Download))
      {
        if (selectedItem != null && selectedFeed != null && selectedItem.Value.IsDownloaded == false)
        {
          selectedItem.DisplayText = Configuration.LoadingPrefix +  selectedItem.Value.DisplayText + Configuration.LoadingSuffix;
          selectedItem.Value.DownloadArticleContent(selectedFeed.Value.Filters);
          selectedItem.DisplayText = selectedItem.Value.DisplayText;
        }
        return true;
      }
      return true;
    }

    #region IDisposable Support
    private bool disposedValue = false; // To detect redundant calls

    protected virtual void Dispose(bool disposing)
    {
      if (!disposedValue)
      {
        if (disposing)
        {
          if (articleListHeader != null)
          {
            articleListHeader.Dispose();
            articleListHeader = null;
          }
          if (articleListFooter != null)
          {
            articleListFooter.Dispose();
            articleListFooter = null;
          }
          if (view != null)
          {
            view.Dispose();
            view = null;
          }
        }

        // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
        // TODO: set large fields to null.
        selectedFeed = null;

        disposedValue = true;
      }
    }

    // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    ~ArticleListView() {
      // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
      Dispose(false);
    }

    // This code added to correctly implement the disposable pattern.
    void IDisposable.Dispose()
    {
      // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
      Dispose(true);
      // TODO: uncomment the following line if the finalizer is overridden above.
      // GC.SuppressFinalize(this);
    }
    #endregion
  }
}
