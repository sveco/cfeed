namespace cFeed.Entities
{
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
  using NLog;

  /// <summary>
  /// Defines the <see cref="RssFeed" />
  /// </summary>
  public class RssFeed : ListItem, IDisposable
  {
    internal bool _isProcessing;

    string _customTitle;
    Uri _feedUrl;

    /// <summary>
    /// Time when feed was loaded.
    /// </summary>
    DateTime lastLoadtime;

    NLog.Logger logger = Log.Instance.Logger;

    /// <summary>
    /// Timer used to automatically reload feed.
    /// </summary>
    Timer timer;

    /// <summary>
    /// Auto reload feed.
    /// </summary>
    public bool AutoReload { get; set; }

    /// <summary>
    /// Custom title to override title defined by feed xml
    /// </summary>
    public string CustomTitle
    {
      get { return _customTitle; }
      set
      {
        if (_customTitle != value)
        {
          _customTitle = value;
          this.DisplayText = this.DisplayLine;
        }
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

    /// <summary>
    /// List of articles in current feed.
    /// </summary>
    public IList<FeedItem> FeedItems { get; set; }

    /// <summary>
    /// Query for dynamic feeds.
    /// </summary>
    public string FeedQuery { get; set; }

    public string FeedTitle
    {
      get { return this.Feed?.Title.Text; }
    }

    /// <summary>
    /// Url of the RSS/Atom feed.
    /// </summary>
    public Uri FeedUrl
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
    /// Marks classes and id's from html that will be ignored when converting html article content to plain text
    /// </summary>
    public string[] Filters { get; set; }

    /// <summary>
    /// Whether feed should be hidden on list of feeds and only available via dynamic query.
    /// </summary>
    public bool Hidden { get; set; }

    /// <summary>
    /// Is feed dynamic (e.g no external feed sources). Used to load dynamic feeds last.
    /// </summary>
    public bool IsDynamic
    {
      get { return FeedUrl == null && !string.IsNullOrEmpty(FeedQuery); }
    }

    /// <summary>
    /// Whether feed is loading
    /// </summary>
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
    /// Interval, in seconds after which the feed will be reloaded if AutoReload is set to true.
    /// </summary>
    public int ReloadInterval { get; set; }

    /// <summary>
    /// Tags can be used to filter feeds via FeedQuery
    /// </summary>
    public string[] Tags { get; set; }

    /// <summary>
    /// Feed title.
    /// </summary>
    public string Title
    {
      get { return CustomTitle ?? FeedTitle ?? FeedUrl?.ToString(); }
    }

    /// <summary>
    /// Total number of feed items (articles) not marked for deletion.
    /// </summary>
    public int TotalItems
    {
      get { return FeedItems != null ? FeedItems.Count(x => x.Deleted == false) : 0; }
    }

    /// <summary>
    /// Total number of items not marked read.
    /// </summary>
    public int UnreadItems
    {
      get { return FeedItems != null ? FeedItems.Count(x => x.IsNew == true && x.Deleted == false) : 0; }
    }

    /// <summary>
    /// Gets or sets the Feed
    /// </summary>
    private SyndicationFeed Feed { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RssFeed"/> class.
    /// </summary>
    /// <param name="url">The <see cref="string"/></param>
    /// <param name="query">The <see cref="string"/></param>
    /// <param name="index">The <see cref="int"/></param>
    /// <param name="customTitle">The <see cref="string"/></param>
    /// <param name="autoReload">The <see cref="bool"/></param>
    /// <param name="reloadInterval">The <see cref="int"/></param>
    public RssFeed(string url, string query, int index, string customTitle = "",
                   bool autoReload = false, int reloadInterval = 30)
    {
      if (!string.IsNullOrEmpty(url))
      {
        FeedUrl = new Uri(url);
      }
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

    public static Stream Flush(string s)
    {
      MemoryStream stream = new MemoryStream();
      StreamWriter writer = new StreamWriter(stream);
      writer.Write(s);
      writer.Flush();
      stream.Position = 0;
      return stream;
    }

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
        { "l", FeedUrl?.ToString() },
        { "n", Configuration.Instance.GetReadState(UnreadItems > 0) },
        { "U", UnreadItems.ToString() },
        { "T", TotalItems.ToString() },
        { "u", (UnreadItems.ToString() + "/" + TotalItems.ToString()).PadLeft(8) },
        { "t", Title },
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
    /// The Load
    /// </summary>
    /// <param name="refresh">The <see cref="bool"/></param>
    public void Load(bool refresh)
    {
      this.GetFeed(refresh);
    }

    /// <summary>
    /// Supports RSS 1, 2 and ATOM 1.0 feed standards
    /// </summary>
    /// <param name="url"></param>
    /// <param name="timeout"></param>
    /// <returns></returns>
    internal SyndicationFeed GetFeed(Uri url, int timeout = 10000)
    {
      SyndicationFeed feed = null;
      HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
      request.UserAgent = Configuration.UserAgent;
      request.Timeout = timeout;
      try
      {
        using (WebResponse response = request.GetResponse())
        {
          XmlSanitizingStream stream = new XmlSanitizingStream(response.GetResponseStream());
          var xml = stream.ReadToEnd();
          using (RssXmlReader reader = new RssXmlReader(Flush(xml)))
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
                logger.Error(ex);
                throw;
              }
            }
          }
        }
      }
      catch (WebException ex)
      {
        logger.Error(ex);
      }
      catch (SocketException ex)
      {
        logger.Error(ex);
      }
      catch (IOException ex)
      {
        logger.Error(ex);
      }
      return feed;
    }

    /// <summary>
    /// Marks all articles for deletion
    /// </summary>
    internal void MarkAllDeleted()
    {
      foreach (var item in FeedItems)
      {
        item.MarkDeleted();
      }
    }

    /// <summary>
    /// The MarkAllRead
    /// </summary>
    internal void MarkAllRead()
    {
      foreach (var item in FeedItems)
      {
        item.MarkAsRead();
      }
    }

    /// <summary>
    /// Purges all articles marked for deletion
    /// </summary>
    internal void Purge()
    {
      DbWrapper.Instance.Purge(FeedUrl);
    }

    internal void RefreshTitle()
    {
      this.DisplayText = this.DisplayLine;
    }

    /// <summary>
    /// The GetFeed
    /// </summary>
    /// <param name="refresh">The <see cref="bool"/></param>
    private void GetFeed(bool refresh)
    {
      IsProcessing = true;
      FeedItems.Clear();

      LoadFeedFromStore();

      //if refresh is on, get feed from web
      if (refresh && FeedUrl != null)
      {
        LoadFeedFromWeb();
      }
      if (timer != null)
      {
        timer.Change(this.ReloadInterval * 1000, this.ReloadInterval * 1000);
      }
      IsProcessing = false;
    }

    /// <summary>
    /// Joins downloaded feed items to FeedList and reindexes them
    /// </summary>
    private void JoinReindexFeed()
    {
      int index = 0;
      foreach (var i in this.Feed.Items)
      {
        var result = DbWrapper.Instance.Find(x => x.SyndicationItemId == i.Id || x.SyndicationItemId == i.Links[0].Uri.ToString()).SingleOrDefault();
        if (result != null)
        {
          result.Item = i;
          result.Index = index + 1;
          result.Tags = Tags;
          result.LastUpdated = DateTime.Now;
          DbWrapper.Instance.Update(result);
        }
        else
        {
          var newItem = new FeedItem(FeedUrl, i)
          {
            FeedUrl = FeedUrl,
            Index = index + 1,
            Tags = Tags,
            LastUpdated = DateTime.Now
          };
          try
          {
            DbWrapper.Instance.Insert(newItem);
          }
          catch (InvalidOperationException ex)
          {
            logger.Log(LogLevel.Error, ex);
          }

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
              logger.Error("Syntax error in FeedQuery:" + FeedQuery);
              logger.Error(x);
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

    /// <summary>
    /// Loads feed from local store (LiteDB)
    /// </summary>
    private void LoadFeedFromStore()
    {
      //load items from db
      if (FeedUrl != null &&
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
          logger.Error("Syntax error in FeedQuery:" + FeedQuery);
          logger.Error(x);
        }
      }
      else
        if (FeedUrl != null &&
            string.IsNullOrEmpty(FeedQuery))
      {
        //Only feed, no filtering
        this.FeedItems =
          DbWrapper.Instance.Find(x => x.FeedUrl == FeedUrl)
          .OrderByDescending(x => x.PublishDate)
        .Select((item, x) => { item.Index = x; return item; })
        .ToList();
      }
      else
          if (!string.IsNullOrEmpty(FeedQuery))
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
          logger.Error("Syntax error in FeedQuery:" + FeedQuery);
          logger.Error(x);
        }
        catch (ArgumentNullException x)
        {
          logger.Error("Missing field in db, skipping feed:" + FeedQuery);
          logger.Error(x);
        }
      }
    }

    /// <summary>
    /// Downloads feed contents from web
    /// </summary>
    private void LoadFeedFromWeb()
    {
      logger.Trace(FeedUrl + " start loading.");

      try
      {
        this.Feed = GetFeed(FeedUrl);
      }
      catch (Exception ex)
      {
        logger.Error("Error loading " + FeedUrl);
        logger.Error(ex);
        this.CustomTitle = Configuration.GetForegroundColor("Red") + this.Title + " - ERROR" +
                           Configuration.ColorReset;
      }

      this.lastLoadtime = DateTime.Now;

      if (Feed == null)
      {
        IsProcessing = false;
        return;
      }

      logger.Trace(FeedUrl + " loaded.");

      JoinReindexFeed();
    }

    /// <summary>
    /// The OnTimer
    /// </summary>
    /// <param name="state">The <see cref="object"/></param>
    private void OnTimer(object state)
    {
      GetFeed(true);
    }

    private bool disposedValue; // To detect redundant calls

    ~RssFeed()
    {
      // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
      Dispose(false);
    }

    // This code added to correctly implement the disposable pattern.
    public void Dispose()
    {
      // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
      Dispose(true);
      // TODO: uncomment the following line if the finalizer is overridden above.
      // GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (!disposedValue)
      {
        if (disposing)
        {
          if (timer != null)
          {
            timer.Dispose();
            timer = null;
          }
        }

        Filters = null;
        Tags = null;
        Feed = null;
        if (FeedItems != null)
        { FeedItems = null; }

        disposedValue = true;
      }
    }
  }
}