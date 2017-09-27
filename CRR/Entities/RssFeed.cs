using JsonConfig;
using LiteDB;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
using System.ServiceModel.Syndication;
using System.Xml;
using System;
using System.Reflection.Emit;
using System.Linq.Expressions;
using System.Diagnostics;
using cFeed.Util;
using System.Text.RegularExpressions;
using cFeed.Logging;

namespace cFeed.Entities
{
  public class RssFeed
  {
    public string FeedUrl { get; set; }
    public string FeedQuery { get; set; }
    public bool Hidden { get; set; }

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

    public int TotalItems { get { return FeedItems.Count(); } }
    public int UnreadItems { get { return FeedItems.Where(x => x.IsNew == true).Count(); } }
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
        { "v", Configuration.VERSION }
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

    private LiteDatabase _db;

    public RssFeed(string url, string query, int index, LiteDatabase db, string customTitle = "")
    {
      FeedUrl = url;
      FeedQuery = query;
      Index = index;
      _db = db;
      FeedItems = new List<FeedItem>();
      CustomTitle = customTitle;
    }

    private void GetFeed(bool refresh)
    {
      IsProcessing = true;
      FeedItems.Clear();
      int index = 0;

      //load items from db
      var items = _db.GetCollection<FeedItem>("items");

      if (!string.IsNullOrEmpty(FeedUrl) &&
          !string.IsNullOrEmpty(FeedQuery))
      {
        //Filtered real feed
        try
        {
          this.FeedItems =
          items.Find(x => x.FeedUrl == FeedUrl)
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
            items.Find(x => x.FeedUrl == FeedUrl)
            .OrderByDescending(x => x.PublishDate)
            .Select((item, x) => { item.Index = x; return item; })
            .ToList();
      }
      else if (!string.IsNullOrEmpty(FeedQuery))
      {
        //Dynamic feed
        try
        {
          this.FeedItems = items.FindAll().Where(this.FeedQuery)
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
        XmlReaderSettings settings = new XmlReaderSettings()
        {
          DtdProcessing = DtdProcessing.Parse,
          Async = true
        };
        //XmlReader reader = XmlReader.Create(FeedUrl, settings);
        RssXmlReader reader = new RssXmlReader(FeedUrl);

        this.Feed = SyndicationFeed.Load(reader);
        Logging.Logger.Log(FeedUrl + " loaded");
        this.Title = Feed.Title.Text;
        reader.Close();
        Isloaded = true;
        foreach (var i in this.Feed.Items)
        {
          var result = items.Find(x => x.SyndicationItemId == i.Id).FirstOrDefault();
          if (result != null)
          {
            //UnreadItems += result.IsNew ? 1 : 0;
            result.Item = i;
            result.Index = index + 1;
            items.Update(result);
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
            items.Insert(newItem);

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

      //UnreadItems = this.FeedItems.Where(x => x.IsNew == true).Count();
      //TotalItems = this.FeedItems.Count();
      IsProcessing = false;
    }

    internal void MarkAllRead(LiteDatabase db)
    {
      foreach (var feed in FeedItems)
      {
        feed.MarkAsRead(db);
      }
    }

    public void Load(bool refresh)
    {
      this.GetFeed(refresh);
    }
  }
}
