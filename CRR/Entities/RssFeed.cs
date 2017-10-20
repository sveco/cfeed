using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Dynamic;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel.Syndication;
using System.Threading;
using System.Xml;
using cFeed.LiteDb;
using cFeed.Logging;
using cFeed.Util;
using CGui.Gui.Primitives;
using JsonConfig;

namespace cFeed.Entities
{
  public class RssFeed : ListItem, IDisposable
  {
    private string _feedUrl;
    /// <summary>
    /// Url of the RSS/Atom feed.
    /// </summary>
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
    /// <summary>
    /// Query for dynamic feeds.
    /// </summary>
    public string FeedQuery { get; set; }
    /// <summary>
    /// Whether feed should be hidden on list of feeds and only available via dynamic query.
    /// </summary>
    public bool Hidden { get; set; }
    /// <summary>
    /// Auto reload feed
    /// </summary>
    public bool AutoReload { get; set; }
    /// <summary>
    /// Interval, in seconds after which the feed will be reloaded if AutoReload is set to true.
    /// </summary>
    public int ReloadInterval { get; set; }

    private DateTime lastLoadtime;
    private Timer timer;

    /// <summary>
    /// Is feed dynamic (e.g no external feed sources). Used to load dynamic feeds last.
    /// </summary>
    public bool IsDynamic {
      get { return string.IsNullOrEmpty(FeedUrl) && !string.IsNullOrEmpty(FeedQuery); }
    }

    /// <summary>
    /// Marks classes and id's from html that will be ignored when converting html article content to plain text
    /// </summary>
    public string[] Filters { get; set; }

    /// <summary>
    /// Tags can be used to filter feeds via FeedQuery
    /// </summary>
    public string[] Tags { get; set; }

    private string _title;
    /// <summary>
    /// Feed title.
    /// </summary>
    public string Title {
      get { return _title; }
      private set {
        if (_title != value)
        {
          _title = value;
          this.DisplayText = this.DisplayLine;
        }
      }
    }

    private string _customTitle;
    /// <summary>
    /// Custom title to override title defined by feed xml
    /// </summary>
    public string CustomTitle {
      get { return _customTitle; }
      set {
        if (_customTitle != value)
        {
          _customTitle = value;
          this.DisplayText = this.DisplayLine;
        }
      }
    }
    /// <summary>
    /// Is feed loaded or not
    /// </summary>
    public bool Isloaded { get; private set; } = false;
    bool _isProcessing = false;
    /// <summary>
    /// Whether feed is loading
    /// </summary>
    public bool IsProcessing {
      get { return _isProcessing; }
      private set {
        if (_isProcessing != value)
        {
          _isProcessing = value;
          this.DisplayText = this.DisplayLine;
        }
      }
    }

    private SyndicationFeed Feed { get; set; }
    /// <summary>
    /// Total number of feed items (articles) not marked for deletion.
    /// </summary>
    public int TotalItems { get { return FeedItems != null ? FeedItems.Where(x => x.Deleted == false).Count() : 0; } }
    /// <summary>
    /// Total number of items not marked read.
    /// </summary>
    public int UnreadItems { get { return FeedItems != null ? FeedItems.Where(x => x.IsNew == true && x.Deleted == false).Count() : 0; } }
    /// <summary>
    /// List of articles in current feed.
    /// </summary>
    public IList<FeedItem> FeedItems { get; set; }
    /// <summary>
    /// Formats string and replaces placeholders with actual values
    /// </summary>
    /// <param name="Format"></param>
    /// <returns></returns>
    public string FormatLine(string Format)
    {
      Dictionary<string, string> replacementTable = new Dictionary<string, string>
      {
        { "i", (Index + 1).ToString() },
        { "l", FeedUrl },
        { "n", Configuration.Instance.GetReadState(UnreadItems > 0) },
        { "U", UnreadItems.ToString() },
        { "T", TotalItems.ToString() },
        { "u", (UnreadItems.ToString() + "/" + TotalItems.ToString()).PadLeft(8) },
        { "t", CustomTitle ?? Title ?? FeedUrl },
        { "V", Configuration.MAJOR_VERSION },
        { "v", Configuration.VERSION },
        { "g", ( Tags != null ? string.Join(" ", Tags) : "")}
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
    /// Formats string using FeedListItemFormat string.
    /// </summary>
    public string DisplayLine
    {
      get
      {
        return FormatLine(Config.Global.UI.Strings.FeedListItemFormat);
      }
    }

    public RssFeed(string url, string query, int index, string customTitle = "", bool autoReload = false, int reloadInterval = 30)
    {
      FeedUrl = url;
      FeedQuery = query;
      Index = index;
      FeedItems = new List<FeedItem>();
      CustomTitle = customTitle;
      AutoReload = autoReload;
      ReloadInterval = reloadInterval;

      if (this.AutoReload)
      {
        if (timer == null)
        {
          timer = new Timer(OnTimer, null, 0, this.ReloadInterval * 1000);
        }
      }
    }

    private void OnTimer(object state)
    {
      GetFeed(true);
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
      try
      {
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
            }
            catch (XmlException ex)
            {
              Logging.Logger.Log(ex);
            }
          }
        }
      }
      catch (WebException ex)
      {
        Logging.Logger.Log(ex);
      }
      catch (SocketException ex)
      {
        Logging.Logger.Log(ex);
      }
      catch (IOException ex)
      {
        Logging.Logger.Log(ex);
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
        try
        {
          this.Feed = GetFeed(FeedUrl);
        }
        catch (WebException ex)
        {
          Logging.Logger.Log(ex);
          this.Title += " - " + ex.Message;
        }

        this.lastLoadtime = DateTime.Now;

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
        Filters = null;
        Tags = null;
        Title = null;
        Feed = null;
        if (FeedItems != null)
          FeedItems = null;

        disposedValue = true;
      }
    }

    //// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    //~RssFeed() {
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
