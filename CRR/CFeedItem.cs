using HtmlAgilityPack.Samples;
using JsonConfig;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;

namespace CRR
{
    public class CFeedItem
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
        private string FormatFileName(string Format)
        {
            var fullPath = Format
                .Replace("%i", Index.ToString().PadLeft(3))
                .Replace("%n", Configuration.GetReadState(this.IsNew))
                .Replace("%d", PublishDate.ToString(dateFormat))
                .Replace("%t", Title);

            var pathEndsAt = fullPath.LastIndexOf('\\');
            string result;
            if (pathEndsAt > 0)
            {
                var pathOnly = fullPath.Substring(0, pathEndsAt).SanitizePath();
                var fileNameOnly = fullPath.Substring(pathEndsAt, fullPath.Length - pathEndsAt).SanitizeFileName();
                result = pathOnly + "\\" + fileNameOnly;
            }
            else {
                result = fullPath.SanitizeFileName();
            }

            if (!Path.IsPathRooted(result))
            {
                result = System.IO.Path.GetFullPath(result);
            }

            return result;
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
                return FormatFileName(fileNameFormat);
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

        public void LoadOnlineArticle(string[] filters, LiteDatabase db) {
            if (OnContentLoaded == null) throw new ArgumentNullException("onLoaded");
            if (Links.Count > 0)
            {
                var w = new HtmlAgilityPack.HtmlWeb();
                var doc = w.Load(Links[0].Uri.ToString());
                HtmlToText conv = new HtmlToText() { Filters = filters.ToList() };
                Collection<Uri> links = new Collection<Uri>();
                var s = conv.ConvertHtml(doc.DocumentNode.OuterHtml, Links[0].Uri, out links);
                ExternalLinks = links;

                var items = db.GetCollection<CFeedItem>("items");
                var result = items.Find(x => x.Id == this.Id).FirstOrDefault();
                if (result != null)
                {
                    result.ExternalLinks = links;
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
            if (File.Exists(ArticleFileName)) {
                ArticleContent = File.ReadAllText(ArticleFileName);
                IsLoaded = true;
                OnContentLoaded.Invoke(ArticleContent);
            }
            else {
                LoadOnlineArticle(filters, db);
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
