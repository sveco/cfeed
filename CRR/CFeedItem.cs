using HtmlAgilityPack.Samples;
using JsonConfig;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.ServiceModel.Syndication;

namespace CRR
{
    public class CFeedItem
    {
        private IList<Uri> externalLinks;
        public IList<Uri> ExternalLinks { get => externalLinks; set => externalLinks = value; }

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
        public string Title { get; set; }

        [BsonIgnore]
        public Action<string> OnContentLoaded;
        public bool Matched { get; set; }

        [BsonIgnore]
        public int Index { get; set; }

        private bool _isNew = true;
        public bool IsNew {
            get { return _isNew; }
            private set { _isNew = value; }
        }
        [BsonIgnore]
        private string FormatLine(string Format)
        {
            return Format
                .Replace("%i", Index.ToString().PadLeft(3))
                .Replace("%n", Configuration.GetReadState(this.IsNew))
                .Replace("%d", PublishDate.ToString(dateFormat))
                .Replace("%t", Title);
        }

        [BsonIgnore]
        public string DisplayText {
            get {
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
        public string ArticleFileName {
            get {
                return FormatLine(fileNameFormat);
            }
        }

        [BsonIgnore]
        public string ArticleContent { get; private set;}

        [BsonIgnore]
        public SyndicationItem Item {
            set {
                SetValues(value);
            }
        }


        /// <summary>
        /// Only for serialization. DO NOT USE!
        /// </summary>
        public CFeedItem() {

        }

        public CFeedItem(string feedUrl) {
            FeedUrl = feedUrl;
        }

        public CFeedItem(string feedUrl, SyndicationItem i)
        {
            FeedUrl = feedUrl;
            Item = i;
        }

        private void SetValues(SyndicationItem i) {
            SyndicationItemId = i.Id;
            PublishDate = i.PublishDate.DateTime;
            Summary = i.Summary.Text;
            Links = i.Links;
            Authors = i.Authors;
            Title = i.Title.Text;
        }

        public void LoadOnlineArticle(string[] filters) {
            if (OnContentLoaded == null) throw new ArgumentNullException("onLoaded");
            if (Links.Count > 0)
            {
                var w = new HtmlAgilityPack.HtmlWeb();
                var doc = w.Load(Links[0].Uri.ToString());
                HtmlToText conv = new HtmlToText() { Filters = filters.ToList() };
                var s = conv.ConvertHtml(doc.DocumentNode.OuterHtml, Links[0].Uri, out externalLinks);
                ArticleContent = s;
                IsLoaded = true;
                OnContentLoaded.Invoke(s);
            }
        }

        public void MarkAsRead(LiteDatabase db)
        {
            _isNew = false;
            var items = db.GetCollection<CFeedItem>("items");
            var result = items.Find(x => x.Id == this.Id).FirstOrDefault();
            if (result != null)
            {
                result.IsNew = false;
                items.Update(result);
            }
        }
    }
}
