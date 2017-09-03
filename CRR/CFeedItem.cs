using HtmlAgilityPack.Samples;
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
        public string Id { get; set; }
        public string FeedUrl { get; set; }

        private bool _isNew = true;
        public Action<string> OnContentLoaded;
        public bool Matched { get; set; }
        public bool IsNew {
            get { return _isNew; }
            private set { _isNew = value; }
        }


        public string DisplayText {
            get {
                return $" {Config.getReadState(this.IsNew)} { this.PublishDate.ToString("MMM dd")} { this.Title}";
            }
        }
        public SyndicationItem Item { set {
                setValues(value);
            } }

        public DateTime PublishDate { get; set; }
        public string Summary { get; set; }
        public Collection<SyndicationLink> Links { get; set; }
        public Collection<SyndicationPerson> Authors { get; set; }
        public string Title { get; set; }
        public string FormatLine(string Format)
        {
            return Format
                .Replace("%t", Title);
        }

        public CFeedItem() { }

        public CFeedItem(SyndicationItem i)
        {
            setValues(i);
        }
        public CFeedItem(SyndicationItem i, Action<string> a) {
            setValues(i);
            OnContentLoaded = a;
        }

        private void setValues(SyndicationItem i) {
            PublishDate = i.PublishDate.DateTime;
            Summary = i.Summary.Text;
            Links = i.Links;
            Authors = i.Authors;
            Title = i.Title.Text;
        }

        public void LoadOnlineArticle() {
            if (OnContentLoaded == null) throw new ArgumentNullException("onLoaded");
            if (Links.Count > 0)
            {
                var w = new HtmlAgilityPack.HtmlWeb();
                var doc = w.Load(Links[0].Uri.ToString());
                HtmlToText conv = new HtmlToText() { Exclude = new List<string> { "orb-banner", "main-nav", "topbar", "bottombar", "fancybox-tmp", "fancybox-loading", "subscription-barrier", "footer", "nav-top", "primary-nav", "related-urls"  } };
                var s = conv.ConvertHtml(doc.DocumentNode.OuterHtml);
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
