using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cFeed.Entities;
using cFeed.Util;
using CGui.Gui;
using CGui.Gui.Primitives;
using JsonConfig;
using LiteDB;

namespace cFeed
{
  public class ArticleView : IDisposable
  {
    private ListItem<FeedItem> selectedArticle;
    private ListItem<RssFeed> selectedFeed;
    private Picklist<FeedItem> parentArticleList;
    private ListItem<FeedItem> nextArticle;
    private string[] _filters;
    private TextArea _articleContent;
    private bool _displayNext = false;

    Header articleHeader = new Header("")
    {
      BackgroundColor = Configuration.GetColor(Config.Global.UI.Colors.ArticleHeaderBackground),
      ForegroundColor = Configuration.GetColor(Config.Global.UI.Colors.ArticleHeaderForeground),
      PadChar = '-'
    };
    Footer articleFooter = new Footer(Config.Global.UI.Strings.ArticleFooter)
    {
      AutoRefresh = false,
      BackgroundColor = Configuration.GetColor(Config.Global.UI.Colors.ArticleFooterBackground),
      ForegroundColor = Configuration.GetColor(Config.Global.UI.Colors.ArticleFooterForeground),
      PadChar = '-'
    };

    public ArticleView()
    {
    }

    private void PrepareArticle()
    {

      if (selectedFeed != null)
      {
        //selectedFeed.Value.UnreadItems--;
        if (selectedFeed.Value.Filters != null)
        {
          _filters = selectedFeed.Value.Filters;
        }
      }

      StringBuilder sb = new StringBuilder();
      sb.AppendLine(Configuration.ArticleTextHighlight + Configuration.ArticleTextFeedUrlLabel + Configuration.ColorReset + selectedArticle.Value.FeedUrl);
      sb.AppendLine(Configuration.ArticleTextHighlight + Configuration.ArticleTextTitleLabel + Configuration.ColorReset + selectedArticle.Value.Title);
      sb.AppendLine(Configuration.ArticleTextHighlight + Configuration.ArticleTextAuthorsLabel + Configuration.ColorReset + String.Join(", ", selectedArticle.Value.Authors.Select(x => x.Name).ToArray()));
      sb.AppendLine(Configuration.ArticleTextHighlight + Configuration.ArticleTextLinkLabel + Configuration.ColorReset + selectedArticle.Value.Links?[0].Uri.GetLeftPart(UriPartial.Path));
      sb.AppendLine(Configuration.ArticleTextHighlight + Configuration.ArticleTextPublishDateLabel + Configuration.ColorReset + selectedArticle.Value.PublishDate.ToString());
      sb.AppendLine();

      Console.Clear();
      if (articleHeader != null)
      {
        articleHeader.DisplayText = selectedArticle.Value.DisplayTitle;
        articleHeader.Refresh();
      }
      if (articleFooter != null) { articleFooter.Show(); }

      var textArea = new TextArea(sb.ToString());
      sb = null;
      textArea.Top = 2;
      textArea.Left = 2;
      textArea.Width = Console.WindowWidth - 6;
      textArea.Height = textArea.LinesCount + 1;
      textArea.WaitForInput = false;
      _articleContent = textArea;

      void onContentLoaded(string content)
      {
        var article = new TextArea(content);
        article.Top = 9;
        article.Left = 2;
        article.Height = Console.WindowHeight - 10;
        article.Width = Console.WindowWidth - 3;
        article.WaitForInput = true;
        article.OnItemKeyHandler += Article_OnItemKeyHandler;
        article.ShowScrollbar = true;

        selectedArticle.Value.MarkAsRead();

        article.Show();
      }

      selectedArticle.Value.OnContentLoaded = new Action<string>(s => { onContentLoaded(s); });
    }

    public void DisplayArticle(ListItem<FeedItem> article, ListItem<RssFeed> feed, Picklist<FeedItem> parent)
    {
      if (article != null)
      {
        this.selectedArticle = article;
        this.selectedFeed = feed;
        parentArticleList = parent;

        PrepareArticle();

        Parallel.Invoke(
            new Action(() => this.selectedArticle.Value.LoadArticle(_filters)),
            new Action(() => _articleContent.Show())
            );

        this.selectedArticle.DisplayText = this.selectedArticle.Value.DisplayText;
        //Given lack of inspiration and a late hour, i commit this code for next article in hope that one day I will rewrite it
        //and provide this functionality with better design.
        while (_displayNext)
        {
          _displayNext = false;
          this.selectedArticle = nextArticle;
          PrepareArticle();
          Parallel.Invoke(
              new Action(() => this.selectedArticle.Value.LoadArticle(_filters)),
              new Action(() => _articleContent.Show())
              );
        }
      }
    }

    private bool CanShowNext
    {
      get { return selectedFeed != null && selectedArticle != null; }
    }

    private bool Article_OnItemKeyHandler(ConsoleKeyInfo key)
    {
      nextArticle = null;

      //Next
      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.Next))
      {
        if (CanShowNext)
        {
          nextArticle = parentArticleList.ListItems
              .OrderByDescending(x => x.Index)
              .Where(x => x.Index < selectedArticle.Index)
              .FirstOrDefault();
        }
      }
      //Next unread
      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.NextUnread))
      {
        if (CanShowNext)
        {
          nextArticle = parentArticleList.ListItems
              .OrderByDescending(x => x.Index)
              .Where(x => x.Value.IsNew == true && x.Index < selectedArticle.Index)
              .FirstOrDefault();
        }
      }
      //Prev
      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.Prev))
      {
        if (CanShowNext)
        {
          if (selectedArticle.Index < selectedFeed.Value.TotalItems - 1)
          {
            nextArticle = parentArticleList.ListItems
                .OrderBy(x => x.Index)
                .Where(x => x.Index > selectedArticle.Index)
                .FirstOrDefault();
          }
        }
      }
      //Prev unread
      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.PrevUnread))
      {
        if (CanShowNext)
        {
          if (selectedArticle.Index < selectedFeed.Value.TotalItems - 1)
          {
            nextArticle = parentArticleList.ListItems
                .OrderBy(x => x.Index)
                .Where(x => x.Value.IsNew == true && x.Index > selectedArticle.Index)
                .FirstOrDefault();
          }
        }
      }
      if (nextArticle != null)
      {
        _displayNext = true;
        return false;
      }

      //Step back
      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.StepBack))
      {
        return false;
      }

      //Open article
      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.OpenBrowser))
      {
        if (selectedArticle != null &&
            selectedArticle.Value.Links.Count > 0)
        {
          if (!string.IsNullOrEmpty(Config.Global.Browser)
              && File.Exists(Config.Global.Browser))
          {
            //Open article url with configured browser
            Process.Start(Config.Global.Browser, selectedArticle.Value.Links[0].Uri.ToString());
          }
          else
          {
            //Open article url with default system browser
            Process.Start(selectedArticle.Value.Links[0].Uri.ToString());
          }
        }
      }

      //Save article
      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.SaveArticle))
      {
        if (selectedArticle != null)
        {
          //selectedArticle.Value.LoadOnlineArticle(_filters, _db);
          Parallel.Invoke(
              new Action(() => selectedArticle.Value.LoadOnlineArticle(_filters)),
              new Action(() => _articleContent.Show())
              );
        }
      }

      //Open numbered link
      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.OpenLink))
      {
        if (selectedArticle != null && selectedArticle.Value != null && selectedArticle.Value.IsLoaded)
        {
          var input = new Input("Link #:")
          {
            Top = Console.WindowHeight - 2,
            ForegroundColor = Configuration.GetColor(Config.Global.UI.Colors.LinkInputForeground),
            BackgroundColor = Configuration.GetColor(Config.Global.UI.Colors.LinkInputBackground),
          };

          int linkNumber;
          if (int.TryParse(input.InputText, out linkNumber))
          {
            if (selectedArticle.Value.ExternalLinks != null
                && selectedArticle.Value.ExternalLinks.Count >= linkNumber
                && linkNumber > 0)
            {
              try
              {
                if (!String.IsNullOrWhiteSpace(Config.Global.Browser))
                {
                  Process.Start(Config.Global.Browser, selectedArticle.Value.ExternalLinks[linkNumber - 1].ToString());
                }
                else
                {
                  Process.Start(selectedArticle.Value.ExternalLinks[linkNumber - 1].ToString());
                }
              }
              catch (Win32Exception ex)
              {
                Logging.Logger.Log(ex);
              }
            }
          }
        }
      }

      //Open numbered image
      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.OpenImage))
      {
        if (selectedArticle != null && selectedArticle.Value != null && selectedArticle.Value.IsLoaded)
        {
          var input = new Input("Image #:")
          {
            Top = Console.WindowHeight - 2,
            ForegroundColor = Configuration.GetColor(Config.Global.UI.Colors.LinkInputForeground),
            BackgroundColor = Configuration.GetColor(Config.Global.UI.Colors.LinkInputBackground),
          };

          int linkNumber;
          if (int.TryParse(input.InputText, out linkNumber))
          {
            if (selectedArticle.Value.ImageLinks != null
                && selectedArticle.Value.ImageLinks.Count >= linkNumber
                && linkNumber > 0)
            {
              try
              {
                if (!String.IsNullOrWhiteSpace(Config.Global.Browser))
                {
                  Process.Start(Config.Global.Browser, selectedArticle.Value.ImageLinks[linkNumber - 1].ToString());
                }
                else
                {
                  Process.Start(selectedArticle.Value.ImageLinks[linkNumber - 1].ToString());
                }
              }
              catch (Win32Exception ex)
              {
                Logging.Logger.Log(ex);
              }
            }
          }
        }
      }

      //Mark article for deletion
      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.Delete))
      {
        if (selectedArticle != null)
        {
          selectedArticle.Value.MarkDeleted();
          selectedArticle.DisplayText = selectedArticle.Value.DisplayText;
        }
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
          // TODO: dispose managed state (managed objects).
          if (_articleContent != null)
          {
            _articleContent.Dispose();
            _articleContent = null;
          }
        }

        // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
        // TODO: set large fields to null.
        selectedArticle = null;
        selectedFeed = null;
        nextArticle = null;
        _filters = null;

        disposedValue = true;
      }
    }

    // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    ~ArticleView() {
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
