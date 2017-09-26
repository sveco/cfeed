using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using cFeed.Entities;
using CGui.Gui;
using CGui.Gui.Primitives;
using JsonConfig;
using LiteDB;

namespace cFeed
{
    public class FeedListView
    {
        private IList<RssFeed> feeds = new List<RssFeed>();
        private LiteDatabase db;
        private ArticleListView articleList;

        private string[] _filters = new string[] { };

        private Viewport _mainView;

        Header feedListHeader = new Header(Format(Config.Global.UI.Strings.ApplicationTitleFormat))
        {
            BackgroundColor = Configuration.GetColor(Config.Global.UI.Colors.FeedListHeaderBackground),
            ForegroundColor = Configuration.GetColor(Config.Global.UI.Colors.FeedListHeaderForeground),
            PadChar = '-'
        };

        private static string Format(string ApplicationTitle)
        {
            return ApplicationTitle
                .Replace("%v", Configuration.VERSION)
                .Replace("%V", Configuration.MAJOR_VERSION);
        }

        Footer feedListFooter = new Footer(Config.Global.UI.Strings.FeedListFooter)
        {
            BackgroundColor = Configuration.GetColor(Config.Global.UI.Colors.FeedListFooterBackground),
            ForegroundColor = Configuration.GetColor(Config.Global.UI.Colors.FeedListFooterForeground),
            PadChar = '-'
        };

        public FeedListView(IList<RssFeed> Feeds, LiteDatabase Db)
        {
            feeds = Feeds;
            db = Db;

            articleList = new ArticleListView(db);
        }

        public void Show(bool refresh)
        {
            string prefix = (refresh ? "" : Configuration.LoadingPrefix);
            string suffix = (refresh ? "" : Configuration.LoadingSuffix);

            void processItem(ListItem<RssFeed> i, CGui.Gui.Picklist<RssFeed> parent)
            {
                new Thread(delegate () {
                    i.DisplayText = prefix + i.DisplayText + suffix;
                    i.Value.Load(refresh);
                    i.DisplayText = i.Value.DisplayLine;
                }).Start();
            }

            var rssFeeds = feeds
                    .Where(item => item.Hidden == false)
                    .Select((item, index) => new ListItem<RssFeed>()
                    {
                        Index = index,
                        DisplayText = $"{index + 1} - {prefix}{item.FeedUrl}{suffix}",
                        Value = item
                    }).ToList();


            //Initialize mainview
            _mainView = new Viewport();
            if (!(Config.Global.UI.Layout.WindowWidth is NullExceptionPreventer))
            {
                _mainView.Width = Config.Global.UI.Layout.WindowWidth;
            }
            if (!(Config.Global.UI.Layout.WindowHeight is NullExceptionPreventer))
            {
                _mainView.Height = Config.Global.UI.Layout.WindowHeight;
            }

            var list = new Picklist<RssFeed>(rssFeeds, processItem);

            list.Top = Config.Global.UI.Layout.FeedListTop;
            list.Left = Config.Global.UI.Layout.FeedListLeft;
            list.Height = Config.Global.UI.Layout.FeedMaxItems;
            list.Width = Console.WindowWidth - Config.Global.UI.Layout.FeedListLeft-1;
            list.OnItemKeyHandler += FeedList_OnItemKeyHandler;
            list.ShowScrollbar = true;

            _mainView.Controls.Add(feedListHeader);
            _mainView.Controls.Add(feedListFooter);
            _mainView.Controls.Add(list);

            _mainView.Show();
        }

        private bool FeedList_OnItemKeyHandler(ConsoleKeyInfo key, ListItem<RssFeed> selectedItem, Picklist<RssFeed> parent)
        {
            //Exit app
            if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.QuitApp))
            {
                return false;
            }

            //Reload all
            if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.ReloadAll))
            {
                new Thread(() =>
                {
                    Thread.CurrentThread.IsBackground = true;
                    /* run your code here */
                    Parallel.ForEach(parent.ListItems, (item) =>
                    {
                        item.DisplayText = Configuration.LoadingPrefix + item.DisplayText + Configuration.LoadingSuffix;
                        item.Value.Load(true);
                        item.DisplayText = item.Value.DisplayLine;
                    });
                }).Start();


                parent.Refresh();
            }

            //Reload
            if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.Reload))
            {
                if (!selectedItem.Value.IsProcessing)
                {
                    new Thread(delegate () {
                        selectedItem.DisplayText = Configuration.LoadingPrefix + selectedItem.DisplayText + Configuration.LoadingSuffix;
                        selectedItem.Value.Load(true);
                        selectedItem.DisplayText = selectedItem.Value.DisplayLine;
                    }).Start();
                }
            }

            //Open
            if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.OpenArticle))
            {
                if (selectedItem != null)
                {
                    if (!selectedItem.Value.IsProcessing)
                    {
                        articleList.DisplayArticleList(selectedItem);
                        _mainView.Refresh();
                    }
                }
            }

            //Redraw view
            if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.RefreshView))
            {
                _mainView.Refresh();
            }
            return true;
        }

        internal void RefreshConfig()
        {
            if (feedListHeader != null)
            {
                feedListHeader.DisplayText = Format(Config.Global.UI.Strings.ApplicationTitleFormat);
                feedListHeader.BackgroundColor = Configuration.GetColor(Config.Global.UI.Colors.FeedListHeaderBackground);
                feedListHeader.ForegroundColor = Configuration.GetColor(Config.Global.UI.Colors.FeedListHeaderForeground);
                feedListHeader.Refresh();
            }
            if (feedListFooter != null)
            {
                feedListFooter.DisplayText = Config.Global.UI.Strings.FeedListFooter;
                feedListFooter.BackgroundColor = Configuration.GetColor(Config.Global.UI.Colors.FeedListFooterBackground);
                feedListFooter.ForegroundColor = Configuration.GetColor(Config.Global.UI.Colors.FeedListFooterForeground);
                feedListFooter.Refresh();
            }
        }
    }
}
