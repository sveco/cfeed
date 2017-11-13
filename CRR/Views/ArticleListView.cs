namespace cFeed
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using cFeed.Entities;
  using cFeed.Util;
  using CGui.Gui;
  using JsonConfig;

  /// <summary>
  /// Displays list of articles for selected feed
  /// </summary>
  public class ArticleListView : IDisposable
  {
    private Viewport _mainView;
    private dynamic footerFormat;
    private dynamic headerFormat;
    private RssFeed selectedFeed;

    public ArticleListView(dynamic articleListLayout)
    {
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
          .Select((item, index) =>
          {
            item.Index = index;
            item.DisplayText = item.DisplayLine;
            return item;
          }).ToList();

      var articleListHeader = _mainView.Controls.Where(x => x.GetType() == typeof(Header)).FirstOrDefault() as Header;
      if (articleListHeader != null)
      {
        articleListHeader.DisplayText = feed.FormatLine(headerFormat);
      }

      var articleListFooter = _mainView.Controls.Where(x => x.GetType() == typeof(Footer)).FirstOrDefault() as Footer;
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
        return OpenArticle(selectedItem, parent);
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
        return MarkAllRead();
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

      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.DeleteAll))
      {
        return DeleteAllArticles();
      }

      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.StepBack))
      {
        return false;
      }

      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.Reload))
      {
        return Reload(parent);
      }
      //Download selected item content to local storage
      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.Download))
      {
        return Download(selectedItem);
      }
      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.OpenBrowser))
      {
        return OpenArticleInBrowser(selectedItem);
      }
      return true;
    }

    private bool markAllDeleted;
    private bool DeleteAllArticles()
    {
      if (selectedFeed != null && !selectedFeed.IsProcessing)
      {
        Dictionary<string, object> choices = new Dictionary<string, object>();
        choices.Add(Config.Global.UI.Strings.PromptAnswerYes, 1);
        choices.Add(Config.Global.UI.Strings.PromptAnswerNo, 2);

        var dialog = new Dialog(Config.Global.UI.Strings.PromptDeleteAll, choices);
        dialog.ItemSelected += DeleteAll_ItemSelected;
        dialog.Show();

        _mainView?.Refresh();
        if (markAllDeleted)
        {
          selectedFeed?.MarkAllDeleted();
          markAllDeleted = false;
        }
        return true;
      }
      return true;
    }

    private void DeleteAll_ItemSelected(object sender, DialogChoice e)
    {
      if (e.DisplayText == Config.Global.UI.Strings.PromptAnswerYes as string)
      {
        markAllDeleted = true;
      }
    }

    private bool OpenArticleInBrowser(FeedItem selectedItem)
    {
      if (selectedItem != null && selectedItem.Links.Count > 0)
      {
        Browser.Open(selectedItem.Links[0].Uri);
      }
      return true;
    }

    private bool Download(FeedItem selectedItem)
    {
      if (selectedItem != null && selectedFeed != null && selectedItem.IsDownloaded == false)
      {
        selectedItem.DisplayText = Configuration.Instance.LoadingPrefix + selectedItem.DisplayText + Configuration.Instance.LoadingSuffix;
        selectedItem.DownloadArticleContent(selectedFeed.Filters);
        selectedItem.DisplayText = selectedItem.DisplayLine;
      }
      return true;
    }

    private bool Reload(Picklist<FeedItem> parent)
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
      return true;
    }

    private bool MarkAllRead()
    {
      Dictionary<string, object> choices = new Dictionary<string, object>();
      choices.Add(Config.Global.UI.Strings.PromptAnswerYes, 1);
      choices.Add(Config.Global.UI.Strings.PromptAnswerNo, 2);

      var dialog = new Dialog(Config.Global.UI.Strings.PromptMarkAll, choices);
      dialog.ItemSelected += MarkAllDialog_ItemSelected;
      dialog.Show();
      return true;
    }

    private bool OpenArticle(FeedItem selectedItem, Picklist<FeedItem> parent)
    {
      parent.IsDisplayed = false;
      parent.Clear();
      using (ArticleView article = new ArticleView(Config.Global.UI.Layout.Article))
      {
        article.Show(selectedItem, selectedFeed, parent);
      }
      _mainView.Refresh();
      return true;
    }

    private void MarkAllDialog_ItemSelected(object sender, DialogChoice e)
    {
      if (e.DisplayText == Config.Global.UI.Strings.PromptAnswerYes as string)
      {
        selectedFeed?.MarkAllRead();
      }
      _mainView?.Refresh();
    }

    private bool disposedValue = false; // To detect redundant calls

    // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    ~ArticleListView()
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

    protected virtual void Dispose(bool disposing)
    {
      if (disposedValue)
        return;

      if (disposing)
      {
        if (_mainView != null)
        {
          _mainView.Dispose();
          _mainView = null;
        }
      }

      disposedValue = true;
    }
  }
}