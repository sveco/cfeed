using JsonConfig;
using LiteDB;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Xml;

namespace CRR
{
    public class RssFeed
    {
        public string FeedUrl { get; set; }
        [BsonIgnore]
        public string[] Filters { get; set; }

        public string Title { get; private set; }

        public string CustomTitle { get; set; }

        [BsonIgnore]
        public bool Isloaded { get; private set; } = false;
        public bool IsProcessing { get; private set; } = false;
        public int Index { get; private set; }
        private SyndicationFeed Feed { get; set; }

        public int TotalItems { get; private set; }
        public int UnreadItems { get; set; }
        public IList<CFeedItem> FeedItems { get; set; }

        [BsonIgnore]
        private string FormatLine(string Format) {
            return Format
                .Replace("%i", (Index + 1).ToString())
                .Replace("%l", FeedUrl)
                .Replace("%n", Configuration.GetReadState(UnreadItems > 0))
                .Replace("%U", UnreadItems.ToString())
                .Replace("%T", TotalItems.ToString())
                .Replace("%u", (UnreadItems.ToString() + "/" + TotalItems.ToString()).PadLeft(8))
                .Replace("%t", CustomTitle ?? Title ?? FeedUrl)
                .Replace("%V", Configuration.MAJOR_VERSION)
                .Replace("%v", Configuration.VERSION);
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

        public RssFeed(string url, int index, LiteDatabase db, string customTitle = "")
        {
            FeedUrl = url;
            Index = index;
            _db = db;
            FeedItems = new List<CFeedItem>();
            CustomTitle = customTitle;
        }

        private void GetFeed(bool refresh)
        {
            IsProcessing = true;
            UnreadItems = 0;
            FeedItems.Clear();
            int index = 0;

            //load items from db
            var items = _db.GetCollection<CFeedItem>("items");

            this.FeedItems = items.Find(x => x.FeedUrl == FeedUrl)
                .OrderByDescending(x => x.PublishDate)
                .Select((item, x) => { item.Index = x + 1; return item; })
                .ToList();

            //if refresh is on, get feed from web
            if (refresh)
            {
                XmlReaderSettings settings = new XmlReaderSettings()
                {
                    DtdProcessing = DtdProcessing.Parse
                };
                XmlReader reader = XmlReader.Create(FeedUrl, settings);
                this.Feed = SyndicationFeed.Load(reader);
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
                        var newItem = new CFeedItem(FeedUrl, i)
                        {
                            FeedUrl = FeedUrl,
                            Index = index + 1
                        };
                        items.Insert(newItem);
                        this.FeedItems.Add(newItem);
                    }
                    index++;
                }
            }

            UnreadItems = this.FeedItems.Where(x => x.IsNew == true).Count();
            TotalItems = this.FeedItems.Count();
            IsProcessing = false;
        }

        public void Load(bool refresh)
        {
            this.GetFeed(refresh);
        }
    }
}
