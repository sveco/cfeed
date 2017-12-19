namespace cFeed.Views
{
  using System;
  using System.ComponentModel;
  using System.Diagnostics;
  using System.Linq;
  using System.Text;
  using System.Threading.Tasks;
  using System.Timers;
  using cFeed.Entities;
  using cFeed.Util;
  using CGui.Gui;
  using CGui.Gui.Primitives;
  using CSharpFunctionalExtensions;
  using JsonConfig;

  public class ArticleView : BaseView
  {
    dynamic headerFormat;
    dynamic footerFormat;

    private FeedItem selectedArticle;
    private RssFeed selectedFeed;
    private Picklist<FeedItem> parentArticleList;
    private FeedItem nextArticle;
    private string[] _filters;
    private TextArea _articleContent;
    private bool _displayNext;

    Timer timer = new Timer();

    public ArticleView(dynamic layout) : base((ConfigObject)layout)
    {
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

      void onContentLoaded(string content)
      {
        var article = new TextArea(content)
        {
          Top = 9,
          Left = 2,
          Height = Console.WindowHeight - 10,
          Width = Console.WindowWidth - 3,
          WaitForInput = true
        };
        article.OnItemKeyHandler += Article_OnItemKeyHandler;
        article.ShowScrollBar = true;

        selectedArticle.MarkAsRead();
        timer.Stop();
        article.Show();
      }

      showArticleHeader()
        .OnSuccess(() =>
        {
          ShowHeader(selectedArticle.TitleLine);
          ShowFooter(selectedArticle.FormatLine(footerFormat));
          selectedArticle.OnContentLoaded = new Action<string>(onContentLoaded);
        })
        .OnSuccess(() => ShowLoadingText())
        .OnSuccess(() => StartTimer())
        .OnSuccess(() => _mainView.Show());
    }

    private void ShowLoadingText()
    {
      if (_mainView.Controls.FirstOrDefault(x => x.Name == "Loading") is TextArea loadingText)
      {
        loadingText.Content = Configuration.Instance.LoadingText;
        loadingText.TextAlignment = TextAlignment.Center;
      }
    }

    private void StartTimer()
    {
      timer.Interval = 500;
      timer.Elapsed += Timer_Elapsed;
      timer.Start();
    }

    private Result showArticleHeader()
    {
      StringBuilder sb = new StringBuilder();
      sb.AppendLine(Configuration.Instance.ArticleTextHighlight + Configuration.Instance.ArticleTextFeedUrlLabel + Configuration.ColorReset + selectedArticle.FeedUrl);
      sb.AppendLine(Configuration.Instance.ArticleTextHighlight + Configuration.Instance.ArticleTextTitleLabel + Configuration.ColorReset + selectedArticle.Title);
      sb.AppendLine(Configuration.Instance.ArticleTextHighlight + Configuration.Instance.ArticleTextAuthorsLabel + Configuration.ColorReset + String.Join(", ", selectedArticle.Authors.Select(x => x.Name).ToArray()));
      sb.AppendLine(Configuration.Instance.ArticleTextHighlight + Configuration.Instance.ArticleTextLinkLabel + Configuration.ColorReset + selectedArticle.Links?[0].Uri.GetLeftPart(UriPartial.Path));
      sb.AppendLine(Configuration.Instance.ArticleTextHighlight + Configuration.Instance.ArticleTextPublishDateLabel + Configuration.ColorReset + selectedArticle.PublishDate.ToString());
      sb.AppendLine();

      var textArea = new TextArea(sb.ToString());
      sb = null;
      textArea.Top = 2;
      textArea.Left = 2;
      textArea.Width = Console.WindowWidth - 6;
      textArea.Height = textArea.TotalItems + 1;
      textArea.WaitForInput = false;
      _articleContent = textArea;

      return Result.Ok();
    }

    String timerElipsis = string.Empty;
    private void Timer_Elapsed(object sender, ElapsedEventArgs e)
    {
      if (timerElipsis.Length < 3) { timerElipsis += "."; } else { timerElipsis = string.Empty; }
      if (_mainView.Controls.FirstOrDefault(x => x.Name == "Loading") is TextArea loadingText && loadingText != null && loadingText.IsDisplayed)
      {
        loadingText.Content = timerElipsis + Configuration.Instance.LoadingText + timerElipsis;
        loadingText.Refresh();
      }
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
            new Action(_articleContent.Show)
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
              new Action(_articleContent.Show)
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
        return ShowNext();
      }
      //Next unread
      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.NextUnread))
      {
        return ShowNextUnread();
      }
      //Prev
      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.Prev))
      {
        return ShowPrevious();
      }
      //Prev unread
      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.PrevUnread))
      {
        return ShowPreviousUnread();
      }

      //Step back
      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.StepBack))
      {
        return false;
      }

      //Open article
      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.OpenBrowser))
      {
        return OpenArticle();
      }

      //Save article
      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.SaveArticle))
      {
        return SaveArticle();
      }

      //Open numbered link
      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.OpenLink))
      {
        return OpenLink();
      }

      //Open numbered image
      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.OpenImage))
      {
        return OpenImage();
      }

      //Mark article for deletion
      if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.Delete))
      {
        return DeleteArticle();
      }

      return true;
    }

    private bool ShowPreviousUnread()
    {
      if (CanShowNext)
      {
        if (selectedArticle.Index < selectedFeed.TotalItems - 1)
        {
          nextArticle = parentArticleList.ListItems
            .OrderBy(x => x.Index)
            .FirstOrDefault(i => ((FeedItem)i).IsNew == true && i.Index > selectedArticle.Index);
        }
      }
      return DisplayNext();
    }

    private bool ShowPrevious()
    {
      if (CanShowNext)
      {
        if (selectedArticle.Index < selectedFeed.TotalItems - 1)
        {
          nextArticle = (FeedItem)parentArticleList.ListItems
            .OrderBy(x => x.Index)
            .FirstOrDefault(x => x.Index > selectedArticle.Index);
        }
      }
      return DisplayNext();
    }

    private bool DisplayNext() {
      _displayNext = nextArticle != null;
      return !_displayNext;
    }

    private bool ShowNextUnread()
    {
      if (CanShowNext)
      {
        nextArticle = (FeedItem)parentArticleList.ListItems
          .OrderByDescending(x => x.Index)
          .FirstOrDefault(i => ((FeedItem)i).IsNew == true && i.Index < selectedArticle.Index);
      }
      return DisplayNext();
    }

    private bool ShowNext()
    {
      if (CanShowNext)
      {
        nextArticle = (FeedItem)parentArticleList.ListItems
          .OrderByDescending(x => x.Index)
          .FirstOrDefault(x => x.Index < selectedArticle.Index);
      }
      return DisplayNext();
    }

    private bool DeleteArticle()
    {
      if (selectedArticle != null)
      {
        selectedArticle.MarkDeleted();
        selectedArticle.DisplayText = selectedArticle.DisplayText;
      }
      return true;
    }

    private bool OpenImage()
    {
      if (selectedArticle != null && selectedArticle != null && selectedArticle.IsLoaded)
      {
        var input = new Input("Image #:")
        {
          Top = Console.WindowHeight - 2,
          ForegroundColor = Configuration.GetColor(Config.Global.UI.Colors.LinkInputForeground),
          BackgroundColor = Configuration.GetColor(Config.Global.UI.Colors.LinkInputBackground),
        };

        if (int.TryParse(input.InputText, out int linkNumber))
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
              logger.Error(ex);
            }
          }
        }
      }
      return true;
    }

    private bool SaveArticle()
    {
      if (selectedArticle != null)
      {
        Parallel.Invoke(
            new Action(() => selectedArticle.LoadOnlineArticle(_filters)),
            new Action(_articleContent.Show)
            );
      }
      return true;
    }

    private bool OpenArticle()
    {
      if (selectedArticle != null &&
          selectedArticle.Links.Count > 0)
      {
        Browser.Open(selectedArticle.Links[0].Uri);
      }
      return true;
    }

    private bool OpenLink()
    {
      if (selectedArticle != null && selectedArticle != null && selectedArticle.IsLoaded)
      {
        var input = new Input("Link #:")
        {
          Top = Console.WindowHeight - 2,
          ForegroundColor = Configuration.GetColor(Config.Global.UI.Colors.LinkInputForeground),
          BackgroundColor = Configuration.GetColor(Config.Global.UI.Colors.LinkInputBackground),
        };

        if (int.TryParse(input.InputText, out int linkNumber))
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
              logger.Error(ex);
            }
          }
        }
      }
      return true;
    }
  }
}
