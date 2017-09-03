using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace CRR
{
    public class RssFeed
    {
        public string FeedUrl { get; private set; }
        public string Title { get; private set; }
        public bool Isloaded { get; private set; } = false;
        public int Index { get; private set; }
        private SyndicationFeed Feed { get; set; }

        public int TotalItems { get; private set; }
        public int UnreadItems { get; set; }
        public IList<CFeedItem> FeedItems { get; set; }

        private string FormatLine(string Format) {
            return Format
                .Replace("%i", Index.ToString())
                .Replace("%n", Config.getReadState(UnreadItems > 0))
                .Replace("%u", (UnreadItems.ToString() + "/" + TotalItems.ToString()).PadLeft(8))
                .Replace("%t", Title);
        }

        public string DisplayLine
        {
            get
            {
                return FormatLine(Config.FeedListFormat);
            }
        }

        public string TitleLine
        {
            get
            {
                return FormatLine(Config.FeedTitleFormat);
            }
        }

        private LiteDatabase _db;
        public RssFeed(string url, int index, LiteDatabase db)
        {
            FeedUrl = url;
            Index = index;
            _db = db;
            FeedItems = new List<CFeedItem>();
        }

        private void getFeed()
        {
            UnreadItems = 0;
            FeedItems.Clear();

            XmlReaderSettings settings = new XmlReaderSettings() {
                DtdProcessing = DtdProcessing.Parse
            };
            XmlReader reader = XmlReader.Create(FeedUrl, settings);
            this.Feed = SyndicationFeed.Load(reader);
            this.Title = Feed.Title.Text;
            reader.Close();
            Isloaded = true;

            var items = _db.GetCollection<CFeedItem>("items");
            var currentFeedItems = items.Find(x => x.FeedUrl == FeedUrl);

            foreach (var i in this.Feed.Items)
            {
                var result = items.Find(x => x.Id == i.Id).FirstOrDefault();
                //var savedFeedItem = currentFeedItems.Where(x => x.Id == i.Id).FirstOrDefault();
                if (result != null)
                {
                    UnreadItems += result.IsNew ? 1 : 0;
                    result.Item = i;
                    items.Update(result);
                    this.FeedItems.Add(result);
                    result.Matched = true;
                }
                else
                {
                    UnreadItems++;
                    var newItem = new CFeedItem {Id = i.Id,  FeedUrl = FeedUrl, Item = i };
                    items.Insert(newItem);
                    this.FeedItems.Add(newItem);
                }
            }

            foreach (var saved in currentFeedItems)
            {
                if (this.FeedItems.Count(x => x.Id == saved.Id) == 0)
                {
                    this.FeedItems.Add(saved);
                }
            }


            TotalItems = this.FeedItems.Count();
        }

        public void Load()
        {
            this.getFeed();
        }
    }
}
