using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.ServiceModel.Syndication;
using cFeed.LiteDb;
using cFeed.Util;
using JsonConfig;
using LiteDB;

namespace cFeed.Entities
{
  public class FeedItem : IDisposable
  {
    //private IList<Uri> externalLinks;
    //public IList<Uri> ExternalLinks { get => externalLinks; set => externalLinks = value; }
    [BsonIgnore]
    public bool IsLoaded { get; private set; }

    private static string displayFormat = Config.Global.UI.Strings.ArticleListFormat as string;
    private static string titleFormat = Config.Global.UI.Strings.ArticleTitleFormat as string;
    private static string dateFormat = Config.Global.UI.Strings.ArticleListDateFormat as string;
    private static string fileNameFormat = Config.Global.SavedFileName;

    public Guid Id { get; set; }
    public string SyndicationItemId { get; set; }
    public string FeedUrl { get; set; }
    public string[] Tags { get; set; }
    public DateTime PublishDate { get; set; }
    public string Summary { get; set; }
    public Collection<SyndicationLink> Links { get; set; }
    public Collection<SyndicationPerson> Authors { get; set; }

    public Collection<Uri> ExternalLinks { get; set; }
    public Collection<Uri> ImageLinks { get; set; }

    public string Title { get; set; }

    [BsonIgnore]
    public Action<string> OnContentLoaded;
    public bool Matched { get; set; }

    [BsonIgnore]
    public int Index { get; set; }

    private bool _isNew = true;
    public bool IsNew
    {
      get { return _isNew; }
      private set { _isNew = value; }
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

    [BsonIgnore]
    private string FormatLine(string format)
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
      return Formatter.FormatLine(format, replacementTable);
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
    public string DisplayText
    {
      get
      {
        return FormatLine(displayFormat);
      }
    }

    [BsonIgnore]
    public string DisplayTitle
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
    public FeedItem()
    {

    }

    public FeedItem(string feedUrl)
    {
      FeedUrl = feedUrl;
    }

    public FeedItem(string feedUrl, SyndicationItem i)
    {
      FeedUrl = feedUrl;
      Item = i;
    }

    private void SetValues(SyndicationItem i)
    {
      SyndicationItemId = i.Id;
      PublishDate = i.PublishDate.DateTime > i.LastUpdatedTime.DateTime ? i.PublishDate.DateTime : i.LastUpdatedTime.DateTime;
      Summary = i.Summary.Text;
      Links = i.Links;
      Authors = i.Authors;
      Title = i.Title.Text;
    }

    public void LoadOnlineArticle(string[] filters)
    {
      if (OnContentLoaded == null) throw new ArgumentNullException("OnContentLoaded");

      if (Links.Count > 0)
      {
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

      var s = conv.ConvertHtml(doc.DocumentNode.OuterHtml, Links[0].Uri, out links, out images);
      ExternalLinks = links;
      ImageLinks = images;
      ArticleContent = s;

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
      _isNew = false;
      var result = DbWrapper.Instance.Find(x => x.Id == this.Id).FirstOrDefault();
      if (result != null)
      {
        result.IsNew = false;
        DbWrapper.Instance.Update(result);
      }
    }

    internal void MarkUnread()
    {
      _isNew = true;
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

    // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    ~FeedItem()
    {
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
