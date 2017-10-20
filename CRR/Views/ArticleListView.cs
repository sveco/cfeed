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
    private RssFeed selectedFeed;
    private Viewport _mainView;
    dynamic headerFormat;
    dynamic footerFormat;

    public ArticleListView(dynamic articleListLayout)
    {
      //region controls
      _mainView = new Viewport();
      _mainView.Width = articleListLayout.Width;
      _mainView.Height = articleListLayout.Height;

      foreach (var control in articleListLayout.Controls)
      {
        var guiElement = ControlFactory.Get(control);
        if (guiElement != null) { _mainView.Controls.Add(guiElement); }
      }

      headerFormat = Config.Global.UI.Strings.ArticleListHeaderFormat;
      footerFormat = Config.Global.UI.Strings.ArticleListFooterFormat;
    }

    public void Show(RssFeed feed)
    {
      selectedFeed = feed;

      var items = feed.FeedItems
          .OrderByDescending(x => x.PublishDate)
          .Where(x => x.Deleted == false)
          .Select((item, index) => {
            item.Index = index;
            item.DisplayText = item.DisplayLine;
            return item;
          }).ToList();

      var articleListHeader = _mainView.Controls.Where(x => x.GetType() == typeof(Header)).FirstOrDefault() as Header;
      if (articleListHeader != null)
      {
        articleListHeader.DisplayText = feed.FormatLine(headerFormat);
      }

      var articleListFooter= _mainView.Controls.Where(x => x.GetType() == typeof(Footer)).FirstOrDefault() as Footer;
      if (articleListFooter != null)
      {
        articleListFooter.DisplayText = feed.FormatLine(footerFormat);
      }

      var list = _mainView.Controls.Where(x => x.GetType() == typeof(Picklist<FeedItem>)).FirstOrDefault() as Picklist<FeedItem>;
      if (list == null) { throw new InvalidOperationException("Missing list config."); }
      list.UpdateList(items);
      list.OnItemKeyHandler += ArticleList_OnItemKeyHandler;

      _mainView.Show();

      selectedFeed.DisplayText = selectedFeed.DisplayLine;
    }

    private bool ArticleList_OnItemKeyHandler(ConsoleKeyInfo key, FeedItem selectedItem, Picklist<FeedItem> parent)
    {
      //Open article
      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.OpenArticle))
      {
        parent.IsDisplayed = false;
        parent.Clear();
        using (ArticleView article = new ArticleView(Config.Global.UI.Layout.Article))
        {
          article.Show(selectedItem, selectedFeed, parent);
        }
        _mainView.Refresh();
        //parent.Refresh();
        return true;
      }
      //Mark selected item as read
      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.MarkRead))
      {
        if (selectedItem != null)
        {
          selectedItem.MarkAsRead();
          selectedItem.DisplayText = selectedItem.DisplayLine;
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
              if (((FeedItem)item).IsNew == true)
              {
                ((FeedItem)item).MarkAsRead();
                item.DisplayText = item.DisplayLine;
              }
            }
          }
        }
      }
      //Mark selected item as unread
      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.MarkUnread))
      {
        if (selectedItem != null && !selectedItem.IsNew)
        {
          selectedItem.MarkUnread();
          selectedItem.DisplayText = selectedItem.DisplayLine;
        }
      }
      //Mark current item for deletion
      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.Delete))
      {
        if (selectedItem != null)
        {
          selectedItem.MarkDeleted();
          selectedItem.DisplayText = selectedItem.DisplayLine;
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
          selectedFeed.Load(true);

          var articleListHeader = _mainView.Controls.Where(x => x.GetType() == typeof(Header)).FirstOrDefault() as Header;
          if (articleListHeader != null)
          {
            articleListHeader.DisplayText = selectedFeed.FormatLine(headerFormat);
            articleListHeader.Refresh();
          }

          var items = selectedFeed.FeedItems
              .OrderByDescending(x => x.PublishDate)
              .Where(x => x.Deleted == false)
              .Select((item, index) =>
                {
                  item.Index = index;
                  item.DisplayText = item.DisplayLine;
                  return item;
                }
              );
          parent.UpdateList(items);
          parent.Refresh();

          if (articleListHeader != null)
          {
            articleListHeader.DisplayText = selectedFeed.FormatLine(headerFormat);
            articleListHeader.Refresh();
          }
        }
      }
      //Download selected item content to local storage
      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.Download))
      {
        if (selectedItem != null && selectedFeed != null && selectedItem.IsDownloaded == false)
        {
          selectedItem.DisplayText = Configuration.Instance.LoadingPrefix +  selectedItem.DisplayText + Configuration.Instance.LoadingSuffix;
          selectedItem.DownloadArticleContent(selectedFeed.Filters);
          selectedItem.DisplayText = selectedItem.DisplayLine;
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

        }

        // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
        // TODO: set large fields to null.
        selectedFeed = null;

        disposedValue = true;
      }
    }

    //// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    //~ArticleListView() {
    //  // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
    //  Dispose(false);
    //}

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
