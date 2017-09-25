using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.ServiceModel.Syndication;
using cFeed.Util;
using JsonConfig;
using LiteDB;

namespace cFeed.Entities
{
  public class FeedItem
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

    private bool IsDownloaded
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

    [BsonIgnore]
    private string FormatLine(string Format)
    {
      return Format
          .Replace("%i", Index.ToString().PadLeft(3))
          .Replace("%n", Configuration.GetReadState(this.IsNew))
          .Replace("%D", Configuration.GetDownloadState(this.IsDownloaded))
          .Replace("%d", PublishDate.ToString(dateFormat))
          .Replace("%t", Title)
          .Replace("%s", Summary)
          .Replace("%l", FeedUrl)
          .Replace("%V", Configuration.MAJOR_VERSION)
          .Replace("%v", Configuration.VERSION);
    }

    [BsonIgnore]
    private string FormatFileName(string Format)
    {
      var fullPath = Format
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

    public void LoadOnlineArticle(string[] filters, LiteDatabase db)
    {
      if (OnContentLoaded == null) throw new ArgumentNullException("onLoaded");
      if (Links.Count > 0)
      {
        var w = new HtmlAgilityPack.HtmlWeb();
        var doc = w.Load(Links[0].Uri.ToString());
        HtmlToText conv = new HtmlToText() { Filters = filters.ToList() };
        Collection<Uri> links = new Collection<Uri>();
        Collection<Uri> images = new Collection<Uri>();

        var s = conv.ConvertHtml(doc.DocumentNode.OuterHtml, Links[0].Uri, out links, out images);
        ExternalLinks = links;
        ImageLinks = images;
        var items = db.GetCollection<FeedItem>("items");

        var result = items.Find(x => x.Id == this.Id).FirstOrDefault();
        if (result != null)
        {
          result.ExternalLinks = links;
          result.ImageLinks = images;
          items.Update(result);
        }

        ArticleContent = s;
        IsLoaded = true;
        Save();
        OnContentLoaded.Invoke(ArticleContent);
      }
    }

    public void LoadArticle(string[] filters, LiteDatabase db)
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
        }
      }
      else
      {
        LoadOnlineArticle(filters, db);
      }
    }

    public void MarkAsRead(LiteDatabase db)
    {
      _isNew = false;
      var items = db.GetCollection<FeedItem>("items");
      var result = items.Find(x => x.Id == this.Id).FirstOrDefault();
      if (result != null)
      {
        result.IsNew = false;
        items.Update(result);
      }
    }

    internal void MarkUnread(LiteDatabase db)
    {
      _isNew = true;
      var items = db.GetCollection<FeedItem>("items");
      var result = items.Find(x => x.Id == this.Id).FirstOrDefault();
      if (result != null)
      {
        result.IsNew = true;
        items.Update(result);
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
  }
}
