using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
using System.Net;
using System.ServiceModel.Syndication;
using System.Text;
using System.Xml;
using cFeed.LiteDb;
using cFeed.Logging;
using cFeed.Util;
using JsonConfig;
using LiteDB;

namespace cFeed.Entities
{
  public class RssFeed
  {
    public string FeedUrl { get; set; }
    public string FeedQuery { get; set; }
    public bool Hidden { get; set; }
    /// <summary>
    /// Is feed dynamic (e.g no external feed sources). Used to load dynamic feeds last.
    /// </summary>
    [BsonIgnore]
    public bool IsDynamic {
      get { return string.IsNullOrEmpty(FeedUrl) && !string.IsNullOrEmpty(FeedQuery); }
    }
    [BsonIgnore]
    public string[] Filters { get; set; }

    public string[] Tags { get; set; }

    public string Title { get; private set; }

    public string CustomTitle { get; set; }

    [BsonIgnore]
    public bool Isloaded { get; private set; } = false;
    public bool IsProcessing { get; private set; } = false;
    public int Index { get; private set; }
    private SyndicationFeed Feed { get; set; }

    public int TotalItems { get { return FeedItems.Where(x => x.Deleted == false).Count(); } }
    public int UnreadItems { get { return FeedItems.Where(x => x.IsNew == true && x.Deleted == false).Count(); } }
    public IList<FeedItem> FeedItems { get; set; }

    [BsonIgnore]
    private string FormatLine(string Format)
    {
      Dictionary<string, string> replacementTable = new Dictionary<string, string>
      {
        { "i", (Index + 1).ToString() },
        { "l", FeedUrl },
        { "n", Configuration.GetReadState(UnreadItems > 0) },
        { "U", UnreadItems.ToString() },
        { "T", TotalItems.ToString() },
        { "u", (UnreadItems.ToString() + "/" + TotalItems.ToString()).PadLeft(8) },
        { "t", CustomTitle ?? Title ?? FeedUrl },
        { "V", Configuration.MAJOR_VERSION },
        { "v", Configuration.VERSION },
        { "g", ( Tags != null ? string.Join(" ", Tags) : "")}
      };

      return Formatter.FormatLine(Format, replacementTable);
    }

    [BsonIgnore]
    public string DisplayLine
    {
      get
      {
        return FormatLine(Config.Global.UI.Strings.FeedListFormat);
      }
    }

    [BsonIgnore]
    public string TitleLine
    {
      get
      {
        return FormatLine(Config.Global.UI.Strings.FeedTitleFormat);
      }
    }

    public RssFeed(string url, string query, int index, string customTitle = "")
    {
      FeedUrl = url;
      FeedQuery = query;
      Index = index;
      FeedItems = new List<FeedItem>();
      CustomTitle = customTitle;
    }

    /// <summary>
    /// Supports RSS 1, 2 and ATOM 1.0 feed standards
    /// </summary>
    /// <param name="url"></param>
    /// <param name="timeout"></param>
    /// <returns></returns>
    static SyndicationFeed GetFeed(string url, int timeout = 10000)
    {
      SyndicationFeed feed = null;

      WebRequest request = WebRequest.Create(url);
      request.Timeout = timeout;
      using (WebResponse response = request.GetResponse())
      using (RssXmlReader reader = new RssXmlReader(response.GetResponseStream()))
      {
        if (Rss10FeedFormatter.CanReadFrom(reader))
        {
          // RSS 1.0
          var rff = new Rss10FeedFormatter();
          rff.ReadFrom(reader);
          feed = rff.Feed;
        }
        else
        {
          // RSS 2.0 or Atom 1.0
          try
          {
            feed = CustomSyndicationFeed.Load(reader);
            //SyndicationFeed sf = SyndicationFeed.Load(reader);
            //Rss20FeedFormatter rssFormatter = sf.GetRss20Formatter();
            //rssFormatter.ReadFrom(reader);
            //feed = rssFormatter.Feed;
          }
          catch (XmlException ex)
          {
            Logging.Logger.Log(ex);
          }
        }
      }
      return feed;
    }

    private void GetFeed(bool refresh)
    {
      IsProcessing = true;
      FeedItems.Clear();
      int index = 0;

      //load items from db
        if (!string.IsNullOrEmpty(FeedUrl) &&
            !string.IsNullOrEmpty(FeedQuery))
        {
          //Filtered real feed
          try
          {
            this.FeedItems =
            DbWrapper.Instance.Find(x => x.FeedUrl == FeedUrl)
            .Where(this.FeedQuery)
            .OrderByDescending(x => x.PublishDate)
            .Select((item, x) => { item.Index = x; return item; })
            .ToList();
          }
          catch (ParseException x)
          {
            Logging.Logger.Log("Syntax error in FeedQuery:" + FeedQuery);
            Logging.Logger.Log(x);
          }
        }
        else if (!string.IsNullOrEmpty(FeedUrl) &&
          string.IsNullOrEmpty(FeedQuery))
        {
          //Only feed, no filtering
          this.FeedItems =
              DbWrapper.Instance.Find(x => x.FeedUrl == FeedUrl)
              .OrderByDescending(x => x.PublishDate)
              .Select((item, x) => { item.Index = x; return item; })
              .ToList();
        }
        else if (!string.IsNullOrEmpty(FeedQuery))
        {
          //Dynamic feed
          try
          {
            this.FeedItems = DbWrapper.Instance.FindAll().Where(this.FeedQuery)
                .OrderByDescending(x => x.PublishDate)
                .Select((item, x) => { item.Index = x; return item; })
                .ToList();
          }
          catch (ParseException x)
          {
            Logging.Logger.Log(LogLevel.Error, "Syntax error in FeedQuery:" + FeedQuery);
            Logging.Logger.Log(x);
          }
          catch (ArgumentNullException x)
          {
            Logging.Logger.Log(LogLevel.Error, "Missing field in db, skipping feed:" + FeedQuery);
            Logging.Logger.Log(x);
          }
        }

      //if refresh is on, get feed from web
      if (refresh && !String.IsNullOrEmpty(FeedUrl))
      {
        this.Feed = GetFeed(FeedUrl);
        if (Feed == null) {
          IsProcessing = false;
          return;
        }

        Logging.Logger.Log(FeedUrl + " loaded");
        this.Title = Feed.Title.Text;
        Isloaded = true;
        foreach (var i in this.Feed.Items)
        {
          var result = DbWrapper.Instance.Find(x => x.SyndicationItemId == i.Id).FirstOrDefault();
          if (result != null)
          {
            result.Item = i;
            result.Index = index + 1;
            result.Tags = Tags;
            DbWrapper.Instance.Update(result);
          }
          else
          {
            //UnreadItems++;
            var newItem = new FeedItem(FeedUrl, i)
            {
              FeedUrl = FeedUrl,
              Index = index + 1,
              Tags = Tags
            };
            DbWrapper.Instance.Insert(newItem);

            if ((!string.IsNullOrEmpty(FeedQuery)))
            {
              try
              {
                var single = new List<FeedItem> { newItem };
                var filtered = single.Where(this.FeedQuery).FirstOrDefault();
                if (filtered != null)
                {
                  this.FeedItems.Add(newItem);
                }
              }
              catch (ParseException x)
              {
                Logging.Logger.Log("Syntax error in FeedQuery:" + FeedQuery);
                Logging.Logger.Log(x);
              }
            }
            else
            {
              this.FeedItems.Add(newItem);
            }
          }
          index++;
        }
      }
      IsProcessing = false;
    }

    internal void MarkAllRead()
    {
      foreach (var item in FeedItems)
      {
        item.MarkAsRead();
      }
    }

    public void Load(bool refresh)
    {
      this.GetFeed(refresh);
    }

    internal void Purge()
    {
      DbWrapper.Instance.Purge(FeedUrl);
    }
  }
}
