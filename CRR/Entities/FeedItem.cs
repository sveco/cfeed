using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;
using cFeed.LiteDb;
using cFeed.Util;
using CGui.Gui.Primitives;
using JsonConfig;
using LiteDB;

namespace cFeed.Entities
{
  public class FeedItem : ListItem, IDisposable
  {
    //private IList<Uri> externalLinks;
    //public IList<Uri> ExternalLinks { get => externalLinks; set => externalLinks = value; }
    [BsonIgnore]
    public bool IsLoaded { get; private set; }

    private static string displayFormat = Config.Global.UI.Strings.ArticleListItemFormat as string;
    private static string titleFormat = Config.Global.UI.Strings.ArticleHeaderFormat as string;
    private static string dateFormat = Config.Global.UI.Strings.ArticleListDateFormat as string;
    private static string fileNameFormat = Config.Global.SavedFileName;

    public Guid Id { get; set; }
    public string SyndicationItemId { get; set; }
    private string _feedUrl;
    public string FeedUrl {
      get { return _feedUrl; }
      set {
        if(_feedUrl != value)
        {
          _feedUrl = value;
          this.DisplayText = this.DisplayLine;
        }
      }
    }
    public string[] Tags { get; set; }
    private DateTime _publishDate;
    public DateTime PublishDate {
      get { return _publishDate; }
      set {
        if (_publishDate != value)
        {
          _publishDate = value;
          this.DisplayText = this.DisplayLine;
        }
      }
    }
    private string _summary;
    public string Summary {
      get { return _summary; }
      set {
        if (_summary != value)
        {
          _summary = value;
          this.DisplayText = this.DisplayLine;
        }
      }
    }
    public Collection<SyndicationLink> Links { get; set; }
    public Collection<SyndicationPerson> Authors { get; set; }

    public Collection<Uri> ExternalLinks { get; set; }
    public Collection<Uri> ImageLinks { get; set; }

    private string _title;
    public string Title {
      get { return _title; }
      set {
        if (_title != value)
        {
          _title = value;
          this.DisplayText = DisplayLine;
        }
      }
    }
    private bool _isProcessing;
    [BsonIgnore]
    public bool IsProcessing
    {
      get { return _isProcessing; }
      private set
      {
        if (_isProcessing != value)
        {
          _isProcessing = value;
          this.DisplayText = this.DisplayLine;
        }
      }
    }

    [BsonIgnore]
    public Action<string> OnContentLoaded;
    //public bool Matched { get; set; }

    private bool _isNew = true;
    public bool IsNew
    {
      get { return _isNew; }
      private set {
        if (_isNew != value)
        {
          _isNew = value;
          this.DisplayText = this.DisplayLine;
        }
      }
    }
    [BsonIgnore]
    public bool IsDownloaded
    {
      get
      {
        try
        {
          return File.Exists(ArticleFileName);
        }
        catch (Exception ex)
        {
          if (ex is UnauthorizedAccessException ||
              ex is DirectoryNotFoundException)
          {
            Logging.Logger.Log(ex);
            return false;
          }
          else
          {
            throw;
          }
        }
      }
    }

    public bool Deleted { get; set; }

    public string FormatLine(string Format)
    {
      Dictionary<string, string> replacementTable = new Dictionary<string, string>
      {
        { "i", (Index + 1).ToString()},
        { "n", Configuration.GetReadState(this.IsNew)},
        { "D", Configuration.GetDownloadState(this.IsDownloaded)},
        { "x", Configuration.GetDeletedState(this.Deleted)},  //only shown when article is marked as deleted, afterwards filtered out
        { "d", PublishDate.ToString(dateFormat)},
        { "t", Title},
        { "s", Summary},
        { "l", FeedUrl},
        { "V", Configuration.VERSION},
        { "v", Configuration.VERSION}
      };
      var line = Formatter.FormatLine(Format, replacementTable);
      if (this.IsProcessing)
      {
        return Configuration.LoadingPrefix + line + Configuration.LoadingSuffix;
      }
      else
      {
        return line;
      }
    }

    [BsonIgnore]
    private string FormatFileName(string format)
    {
      var fullPath = format
          .Replace("%i", Index.ToString().PadLeft(3))
          .Replace("%n", Configuration.GetReadState(this.IsNew))
          .Replace("%d", PublishDate.ToString(dateFormat))
          .Replace("%t", Title)
          .Replace("%l", FeedUrl);

      var pathEndsAt = fullPath.LastIndexOf('\\');
      string result;
      if (pathEndsAt > 0)
      {
        var pathOnly = fullPath.Substring(0, pathEndsAt).SanitizePath();
        var fileNameOnly = fullPath.Substring(pathEndsAt, fullPath.Length - pathEndsAt).SanitizeFileName();
        result = pathOnly + "\\" + fileNameOnly;
      }
      else
      {
        result = fullPath.SanitizeFileName();
      }

      if (!Path.IsPathRooted(result))
      {
        result = System.IO.Path.GetFullPath(result);
      }

      return result;
    }

    [BsonIgnore]
    public string DisplayLine
    {
      get
      {
        return FormatLine(displayFormat);
      }
    }

    [BsonIgnore]
    public string TitleLine
    {
      get
      {
        return FormatLine(titleFormat);
      }
    }

    [BsonIgnore]
    public string ArticleFileName
    {
      get
      {
        return FormatFileName(fileNameFormat);
      }
    }

    [BsonIgnore]
    public string ArticleContent { get; private set; }

    [BsonIgnore]
    public SyndicationItem Item
    {
      set
      {
        SetValues(value);
      }
    }


    /// <summary>
    /// Only for serialization. DO NOT USE!
    /// </summary>
    public FeedItem() {}

    public FeedItem(string feedUrl)
    {
      FeedUrl = feedUrl;
      this.PropertyChanged += FeedItem_PropertyChanged;
    }

    private void FeedItem_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
      this.DisplayText = this.DisplayLine;
    }

    public FeedItem(string feedUrl, SyndicationItem i)
    {
      FeedUrl = feedUrl;
      Item = i;
      this.PropertyChanged += FeedItem_PropertyChanged;
    }

    private void SetValues(SyndicationItem i)
    {
      SyndicationItemId = i.Id;
      PublishDate = i.PublishDate.DateTime > i.LastUpdatedTime.DateTime ? i.PublishDate.DateTime : i.LastUpdatedTime.DateTime;
      Summary = i.Summary.Text;
      Links = i.Links;
      Authors = i.Authors;
      Title = i.Title.Text;
      this.PropertyChanged += FeedItem_PropertyChanged;
    }

    public void LoadOnlineArticle(string[] filters)
    {
      if (OnContentLoaded == null) throw new ArgumentNullException("OnContentLoaded");

      if (Links.Count > 0)
      {
        this.IsProcessing = true;
        DownloadArticleContent(filters);
        //var items = db.GetCollection<FeedItem>("items");
        var result = DbWrapper.Instance.Find(x => x.Id == this.Id).FirstOrDefault();
        if (result != null)
        {
          result.ExternalLinks = ExternalLinks;
          result.ImageLinks = ImageLinks;
          result.Tags = Tags;
          DbWrapper.Instance.Update(result);
        }
        this.IsProcessing = false;
        OnContentLoaded.Invoke(ArticleContent);
      }
    }

    public void DownloadArticleContent(string[] filters)
    {
      var w = new HtmlAgilityPack.HtmlWeb();
      var doc = w.Load(Links[0].Uri);
      HtmlToText conv = new HtmlToText() { Filters = filters?.ToList() };
      Collection<Uri> links = new Collection<Uri>();
      Collection<Uri> images = new Collection<Uri>();

      var resultString = conv.ConvertHtml(doc.DocumentNode.OuterHtml, Links[0].Uri, out links, out images);
      //remove multiple lines from article content. It makes text more condensed.
      var cleanedContent = Regex.Replace(resultString, @"^\s+$[\r\n]*", "\r\n", RegexOptions.Multiline);

      ExternalLinks = links;
      ImageLinks = images;
      ArticleContent = cleanedContent;

      IsLoaded = true;
      Save();
    }

    /// <summary>
    /// Loads article content from local storage or web. Supressing Code Analysis warning as we are handling errors by type inside catch block.
    /// </summary>
    /// <param name="filters"></param>
    /// <param name="db"></param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
    public void LoadArticle(string[] filters)
    {
      if (this.IsDownloaded)
      {
        try
        {
          ArticleContent = File.ReadAllText(ArticleFileName);
          IsLoaded = true;
          OnContentLoaded.Invoke(ArticleContent);
        }
        catch (Exception x)
        {
          if (x is FileNotFoundException ||
              x is UnauthorizedAccessException ||
              x is FileNotFoundException)
          {
            //For all purposes, file is not accessible to us
            Logging.Logger.Log(x);
            IsLoaded = false;
          }
          else { throw; }
        }
      }
      else
      {
        LoadOnlineArticle(filters);
      }
    }

    public void MarkAsRead()
    {
      IsNew  = false;
      var result = DbWrapper.Instance.Find(x => x.Id == this.Id).FirstOrDefault();
      if (result != null)
      {
        result.IsNew = false;
        DbWrapper.Instance.Update(result);
      }
    }

    internal void MarkUnread()
    {
      IsNew = true;
      var result = DbWrapper.Instance.Find(x => x.Id == this.Id).FirstOrDefault();
      if (result != null)
      {
        result.IsNew = true;
        DbWrapper.Instance.Update(result);
      }
    }
    internal void MarkDeleted()
    {
      Deleted = true;
      var result = DbWrapper.Instance.Find(x => x.Id == this.Id).FirstOrDefault();
      if (result != null)
      {
        result.Deleted = true;
        DbWrapper.Instance.Update(result);
      }
    }

    internal void Save()
    {
      var filename = ArticleFileName;

      FileInfo i = new FileInfo(filename);

      if (!i.Directory.Exists)
      {
        i.Directory.Create();
      }

      using (var sw = File.CreateText(filename))
      {
        sw.Write(ArticleContent);
      }
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
        }

        // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
        // TODO: set large fields to null.
        Summary = null;
        FeedUrl = null;
        Tags = null;
        Links = null;
        Authors = null;
        ExternalLinks = null;
        ImageLinks = null;

        disposedValue = true;
      }
    }

    //// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    //~FeedItem()
    //{
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
