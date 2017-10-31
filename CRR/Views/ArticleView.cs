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
    private Viewport _mainView;
    dynamic headerFormat;
    dynamic footerFormat;

    private FeedItem selectedArticle;
    private RssFeed selectedFeed;
    private Picklist<FeedItem> parentArticleList;
    private FeedItem nextArticle;
    private string[] _filters;
    private TextArea _articleContent;
    private bool _displayNext = false;

    public ArticleView(dynamic articleLayout)
    {
      //region controls
      _mainView = new Viewport();
      _mainView.Width = articleLayout.Width;
      _mainView.Height = articleLayout.Height;

      foreach (var control in articleLayout.Controls)
      {
        var guiElement = ControlFactory.Get(control);
        if (guiElement != null) { _mainView.Controls.Add(guiElement); }
      }

      headerFormat = Config.Global.UI.Strings.ArticleHeaderFormat;
      footerFormat = Config.Global.UI.Strings.ArticleFooterFormat;
    }

    private void PrepareArticle()
    {

      if (selectedFeed != null)
      {
        if (selectedFeed.Filters != null)
        {
          _filters = selectedFeed.Filters;
        }
      }

      StringBuilder sb = new StringBuilder();
      sb.AppendLine(Configuration.Instance.ArticleTextHighlight + Configuration.Instance.ArticleTextFeedUrlLabel + Configuration.ColorReset + selectedArticle.FeedUrl);
      sb.AppendLine(Configuration.Instance.ArticleTextHighlight + Configuration.Instance.ArticleTextTitleLabel + Configuration.ColorReset + selectedArticle.Title);
      sb.AppendLine(Configuration.Instance.ArticleTextHighlight + Configuration.Instance.ArticleTextAuthorsLabel + Configuration.ColorReset + String.Join(", ", selectedArticle.Authors.Select(x => x.Name).ToArray()));
      sb.AppendLine(Configuration.Instance.ArticleTextHighlight + Configuration.Instance.ArticleTextLinkLabel + Configuration.ColorReset + selectedArticle.Links?[0].Uri.GetLeftPart(UriPartial.Path));
      sb.AppendLine(Configuration.Instance.ArticleTextHighlight + Configuration.Instance.ArticleTextPublishDateLabel + Configuration.ColorReset + selectedArticle.PublishDate.ToString());
      sb.AppendLine();

      var articleHeader = _mainView.Controls.Where(x => x.GetType() == typeof(Header)).FirstOrDefault() as Header;
      if (articleHeader != null)
      {
        articleHeader.DisplayText = selectedArticle.TitleLine;
      }

      var articleFooter = _mainView.Controls.Where(x => x.GetType() == typeof(Footer)).FirstOrDefault() as Footer;
      if (articleFooter != null)
      {
        articleFooter.DisplayText = selectedArticle.FormatLine(footerFormat);
      }

      var textArea = new TextArea(sb.ToString());
      sb = null;
      textArea.Top = 2;
      textArea.Left = 2;
      textArea.Width = Console.WindowWidth - 6;
      textArea.Height = textArea.TotalItems + 1;
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
        article.ShowScrollBar = true;

        selectedArticle.MarkAsRead();

        article.Show();
      }

      selectedArticle.OnContentLoaded = new Action<string>(s => { onContentLoaded(s); });

      _mainView.Show();
    }

    public void Show(FeedItem article, RssFeed feed, Picklist<FeedItem> parent)
    {
      if (article != null)
      {
        this.selectedArticle = article;
        this.selectedFeed = feed;
        parentArticleList = parent;

        PrepareArticle();

        Parallel.Invoke(
            new Action(() => this.selectedArticle.LoadArticle(_filters)),
            new Action(() => _articleContent.Show())
            );

        this.selectedArticle.DisplayText = this.selectedArticle.DisplayText;
        //Given lack of inspiration and a late hour, i commit this code for next article in hope that one day I will rewrite it
        //and provide this functionality with better design.
        while (_displayNext)
        {
          _displayNext = false;
          this.selectedArticle = nextArticle;
          PrepareArticle();
          Parallel.Invoke(
              new Action(() => this.selectedArticle.LoadArticle(_filters)),
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
          nextArticle = (FeedItem)parentArticleList.ListItems
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
          nextArticle = (FeedItem)parentArticleList.ListItems
              .OrderByDescending(x => x.Index)
              .Where(i => ((FeedItem)i).IsNew == true && i.Index < selectedArticle.Index)
              .FirstOrDefault();
        }
      }
      //Prev
      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.Prev))
      {
        if (CanShowNext)
        {
          if (selectedArticle.Index < selectedFeed.TotalItems - 1)
          {
            nextArticle = (FeedItem)parentArticleList.ListItems
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
          if (selectedArticle.Index < selectedFeed.TotalItems - 1)
          {
            nextArticle = (FeedItem)parentArticleList.ListItems
                .OrderBy(x => x.Index)
                .Where(i => ((FeedItem)i).IsNew == true && i.Index > selectedArticle.Index)
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
            selectedArticle.Links.Count > 0)
        {
          Browser.Open(selectedArticle.Links[0].Uri);
        }
      }

      //Save article
      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.SaveArticle))
      {
        if (selectedArticle != null)
        {
          Parallel.Invoke(
              new Action(() => selectedArticle.LoadOnlineArticle(_filters)),
              new Action(() => _articleContent.Show())
              );
        }
      }

      //Open numbered link
      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.OpenLink))
      {
        if (selectedArticle != null && selectedArticle!= null && selectedArticle.IsLoaded)
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
            if (selectedArticle.ExternalLinks != null
                && selectedArticle.ExternalLinks.Count >= linkNumber
                && linkNumber > 0)
            {
              try
              {
                if (!String.IsNullOrWhiteSpace(Config.Global.Browser))
                {
                  Process.Start(Config.Global.Browser, selectedArticle.ExternalLinks[linkNumber - 1].ToString());
                }
                else
                {
                  Process.Start(selectedArticle.ExternalLinks[linkNumber - 1].ToString());
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
        if (selectedArticle != null && selectedArticle != null && selectedArticle.IsLoaded)
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
            if (selectedArticle.ImageLinks != null
                && selectedArticle.ImageLinks.Count >= linkNumber
                && linkNumber > 0)
            {
              try
              {
                if (!String.IsNullOrWhiteSpace(Config.Global.Browser))
                {
                  Process.Start(Config.Global.Browser, selectedArticle.ImageLinks[linkNumber - 1].ToString());
                }
                else
                {
                  Process.Start(selectedArticle.ImageLinks[linkNumber - 1].ToString());
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
          selectedArticle.MarkDeleted();
          selectedArticle.DisplayText = selectedArticle.DisplayText;
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
          if(_mainView != null)
          {
            _mainView.Dispose();
            _mainView = null;
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
