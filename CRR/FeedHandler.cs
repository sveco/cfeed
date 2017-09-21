using CGui.Gui;
using CGui.Gui.Primitives;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JsonConfig;
using System.IO;
using System.Threading;

namespace CRR
{
    public class FeedHandler
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


        public FeedHandler(IList<RssFeed> Feeds, LiteDatabase Db)
        {
            feeds = Feeds;
            db = Db;

            articleList = new ArticleListView(_mainView, db);
        }

        public void DisplayFeedList(bool refresh)
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
                    .Select((item, index) => new ListItem<RssFeed>()
                    {
                        Index = index,
                        DisplayText = $"{index + 1} - {item.FeedUrl}{Configuration.LoadingSuffix}",
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
            if (Configuration.VerifyKey(key, Config.Global.Shortcuts.QuitApp.Key, Config.Global.Shortcuts.QuitApp.Modifiers))
            {
                return false;
            }

            //Reload all
            if (Configuration.VerifyKey(key, Config.Global.Shortcuts.ReloadAll.Key, Config.Global.Shortcuts.ReloadAll.Modifiers))
            {
                Parallel.ForEach(parent.ListItems, (item) =>
                {
                    item.DisplayText = Configuration.LoadingPrefix + item.DisplayText + Configuration.LoadingSuffix;
                    item.Value.Load(true);
                    item.DisplayText = item.Value.DisplayLine;
                });
                parent.Refresh();
            }

            //Reload
            if (Configuration.VerifyKey(key, Config.Global.Shortcuts.Reload.Key, Config.Global.Shortcuts.Reload.Modifiers))
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
            if (Configuration.VerifyKey(key, Config.Global.Shortcuts.OpenArticle.Key, Config.Global.Shortcuts.OpenArticle.Modifiers))
            {
                if (selectedItem != null)
                {
                    if (!selectedItem.Value.IsProcessing)
                    {
                        articleList.DisplayArticleList(selectedItem);
                        if (feedListHeader != null) { feedListHeader.Show(); }
                        if (feedListFooter != null) { feedListFooter.Show(); }
                        parent.Refresh();
                    }
                }
            }

            //Redraw view
            if (Configuration.VerifyKey(key, Config.Global.Shortcuts.RefreshView.Key, Config.Global.Shortcuts.RefreshView.Modifiers))
            {
                _mainView.Show();
            }
            return true;
        }
    }
}
