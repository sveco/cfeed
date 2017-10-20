namespace cFeed.Entities
{
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

  /// <summary>
  /// Defines the <see cref="FeedItem" />
  /// </summary>
  public class FeedItem : ListItem, IDisposable
  {
    /// <summary>
    /// Gets or sets a value indicating whether feed item is loaded
    /// </summary>
    [BsonIgnore]
    public bool IsLoaded { get; private set; }

    /// <summary>
    /// Defines the displayFormat
    /// </summary>
    private static string displayFormat = Config.Global.UI.Strings.ArticleListItemFormat as string;

    /// <summary>
    /// Defines the titleFormat
    /// </summary>
    private static string titleFormat = Config.Global.UI.Strings.ArticleHeaderFormat as string;

    /// <summary>
    /// Defines the dateFormat
    /// </summary>
    private static string dateFormat = Config.Global.UI.Strings.ArticleListDateFormat as string;

    /// <summary>
    /// Defines the fileNameFormat
    /// </summary>
    private static string fileNameFormat = Config.Global.SavedFileName;

    /// <summary>
    /// Gets or sets the Id
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the SyndicationItemId
    /// </summary>
    public string SyndicationItemId { get; set; }

    /// <summary>
    /// Defines the _feedUrl
    /// </summary>
    private string _feedUrl;

    /// <summary>
    /// Gets or sets the FeedUrl
    /// </summary>
    public string FeedUrl
    {
      get { return _feedUrl; }
      set
      {
        if (_feedUrl != value)
        {
          _feedUrl = value;
          this.DisplayText = this.DisplayLine;
        }
      }
    }

    /// <summary>
    /// Gets or sets the Tags
    /// </summary>
    public string[] Tags { get; set; }

    /// <summary>
    /// Defines the _publishDate
    /// </summary>
    private DateTime _publishDate;

    /// <summary>
    /// Gets or sets the PublishDate
    /// </summary>
    public DateTime PublishDate
    {
      get { return _publishDate; }
      set
      {
        if (_publishDate != value)
        {
          _publishDate = value;
          this.DisplayText = this.DisplayLine;
        }
      }
    }

    /// <summary>
    /// Defines the _summary
    /// </summary>
    private string _summary;

    /// <summary>
    /// Gets or sets the Summary
    /// </summary>
    public string Summary
    {
      get { return _summary; }
      set
      {
        if (_summary != value)
        {
          _summary = value;
          this.DisplayText = this.DisplayLine;
        }
      }
    }

    /// <summary>
    /// Gets or sets the Links
    /// </summary>
    public Collection<SyndicationLink> Links { get; set; }

    /// <summary>
    /// Gets or sets the Authors
    /// </summary>
    public Collection<SyndicationPerson> Authors { get; set; }

    /// <summary>
    /// Gets or sets the ExternalLinks
    /// </summary>
    public Collection<Uri> ExternalLinks { get; set; }

    /// <summary>
    /// Gets or sets the ImageLinks
    /// </summary>
    public Collection<Uri> ImageLinks { get; set; }

    /// <summary>
    /// Defines the _title
    /// </summary>
    private string _title;

    /// <summary>
    /// Gets or sets the Title
    /// </summary>
    public string Title
    {
      get { return _title; }
      set
      {
        if (_title != value)
        {
          _title = value;
          this.DisplayText = DisplayLine;
        }
      }
    }

    /// <summary>
    /// Defines the _isProcessing
    /// </summary>
    private bool _isProcessing;

    /// <summary>
    /// Gets or sets a value indicating whether IsProcessing
    /// </summary>
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

    /// <summary>
    /// Defines the OnContentLoaded
    /// </summary>
    [BsonIgnore]
    public Action<string> OnContentLoaded;

    //public bool Matched { get; set; }
    //public bool Matched { get; set; }        /// <summary>
    /// Defines the _isNew
    /// </summary>
    private bool _isNew = true;

    /// <summary>
    /// Gets or sets a value indicating whether IsNew
    /// </summary>
    public bool IsNew
    {
      get { return _isNew; }
      private set
      {
        if (_isNew != value)
        {
          _isNew = value;
          this.DisplayText = this.DisplayLine;
        }
      }
    }

    /// <summary>
    /// Gets a value indicating whether IsDownloaded
    /// </summary>
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

    /// <summary>
    /// Gets or sets a value indicating whether Deleted
    /// </summary>
    public bool Deleted { get; set; }

    /// <summary>
    /// The FormatLine
    /// </summary>
    /// <param name="Format">The <see cref="string"/></param>
    /// <returns>The <see cref="string"/></returns>
    public string FormatLine(string Format)
    {
      Dictionary<string, string> replacementTable = new Dictionary<string, string>
      {
        { "i", (Index + 1).ToString()},
        { "n", Configuration.Instance.GetReadState(this.IsNew)},
        { "D", Configuration.Instance.GetDownloadState(this.IsDownloaded)},
        { "x", Configuration.Instance.GetDeletedState(this.Deleted)},  //only shown when article is marked as deleted, afterwards filtered out
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
        return Configuration.Instance.LoadingPrefix + line + Configuration.Instance.LoadingSuffix;
      }
      else
      {
        return line;
      }
    }

    /// <summary>
    /// The FormatFileName
    /// </summary>
    /// <param name="format">The <see cref="string"/></param>
    /// <returns>The <see cref="string"/></returns>
    [BsonIgnore]
    private string FormatFileName(string format)
    {
      var fullPath = format
          .Replace("%i", Index.ToString().PadLeft(3))
          .Replace("%n", Configuration.Instance.GetReadState(this.IsNew))
          .Replace("%d", PublishDate.ToString(dateFormat))
          .Replace("%t", Title)
          .Replace("%l", FeedUrl);

      var pathEndsAt = fullPath.LastIndexOf('\\');
      string result;
      if (pathEndsAt > 0)
      {
        var pathOnly = fullPath.Substring(0, pathEndsAt).SanitizePath();
        var fileNameOnly = fullPath.Substring(pathEndsAt, fullPath.Length - pathEndsAt).SanitizeFileName();

        //Limit full path length to 260 chars to avoid System.IO.PathTooLongException
        if (System.IO.Path.GetFullPath(pathOnly).Length + fileNameOnly.Length > 260)
        {
          int dotPosition = fileNameOnly.LastIndexOf(".");
          string fileName = fileNameOnly.Substring(0, dotPosition);
          string extension = fileNameOnly.Substring(dotPosition, fileNameOnly.Length - dotPosition);
          fileNameOnly = fileName.Substring(0, fileName.Length - System.IO.Path.GetFullPath(pathOnly).Length - extension.Length) + extension;
        }
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

    /// <summary>
    /// Gets the DisplayLine
    /// </summary>
    [BsonIgnore]
    public string DisplayLine
    {
      get
      {
        return FormatLine(displayFormat);
      }
    }

    /// <summary>
    /// Gets the TitleLine
    /// </summary>
    [BsonIgnore]
    public string TitleLine
    {
      get
      {
        return FormatLine(titleFormat);
      }
    }

    /// <summary>
    /// Gets the ArticleFileName
    /// </summary>
    [BsonIgnore]
    public string ArticleFileName
    {
      get
      {
        return FormatFileName(fileNameFormat);
      }
    }

    /// <summary>
    /// Gets or sets the ArticleContent
    /// </summary>
    [BsonIgnore]
    public string ArticleContent { get; private set; }

    /// <summary>
    /// Sets the Item
    /// </summary>
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
    public FeedItem()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedItem"/> class.
    /// </summary>
    /// <param name="feedUrl">The <see cref="string"/></param>
    public FeedItem(string feedUrl)
    {
      FeedUrl = feedUrl;
      this.PropertyChanged += FeedItem_PropertyChanged;
    }

    /// <summary>
    /// The FeedItem_PropertyChanged
    /// </summary>
    /// <param name="sender">The <see cref="object"/></param>
    /// <param name="e">The <see cref="System.ComponentModel.PropertyChangedEventArgs"/></param>
    private void FeedItem_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
      this.DisplayText = this.DisplayLine;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedItem"/> class.
    /// </summary>
    /// <param name="feedUrl">The <see cref="string"/></param>
    /// <param name="i">The <see cref="SyndicationItem"/></param>
    public FeedItem(string feedUrl, SyndicationItem i)
    {
      FeedUrl = feedUrl;
      Item = i;
      this.PropertyChanged += FeedItem_PropertyChanged;
    }

    /// <summary>
    /// The SetValues
    /// </summary>
    /// <param name="i">The <see cref="SyndicationItem"/></param>
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

    /// <summary>
    /// The LoadOnlineArticle
    /// </summary>
    /// <param name="filters">The <see cref="string[]"/></param>
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

    /// <summary>
    /// The DownloadArticleContent
    /// </summary>
    /// <param name="filters">The <see cref="string[]"/></param>
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

    /// <summary>
    /// The MarkAsRead
    /// </summary>
    public void MarkAsRead()
    {
      IsNew = false;
      var result = DbWrapper.Instance.Find(x => x.Id == this.Id).FirstOrDefault();
      if (result != null)
      {
        result.IsNew = false;
        DbWrapper.Instance.Update(result);
      }
    }

    /// <summary>
    /// The MarkUnread
    /// </summary>
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

    /// <summary>
    /// The MarkDeleted
    /// </summary>
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

    /// <summary>
    /// The Save
    /// </summary>
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

    /// <summary>
    /// Defines the disposedValue
    /// </summary>
    private bool disposedValue = false;// To detect redundant calls

    /// <summary>
    /// The Dispose
    /// </summary>
    /// <param name="disposing">The <see cref="bool"/></param>
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
    /// <summary>
    /// The Dispose
    /// </summary>
    void IDisposable.Dispose()
    {
      // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
      Dispose(true);
    }
  }
}
