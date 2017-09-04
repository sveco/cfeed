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

namespace CRR
{
    public class FeedHandler
    {
        private IList<RssFeed> _feeds = new List<RssFeed>();
        private LiteDatabase _db;
        private ListItem<CFeedItem> _selectedArticle;
        private ListItem<CFeedItem> _nextUnreadArticle;
        private TextArea _articleContent;
        private bool _displayNext = false;
        private ListItem<RssFeed> _selectedFeed;
        private Picklist<CFeedItem> _parentArticleList;
        private string[] _filters = new string[] { };

        private Viewport _mainView;
        private Viewport _articelListView;
        private Viewport _articleView;

        Header feedListHeader = new Header(Config.Global.UI.Strings.ApplicationTitle)
        {
            BackgroundColor = Configuration.getColor(Config.Global.UI.Colors.FeedListHeaderBackground),
            ForegroundColor = Configuration.getColor(Config.Global.UI.Colors.FeedListHeaderForeground),
            PadChar = '-'
        };
        Footer feedListFooter = new Footer(" Q:Quit ENTER/Space:List articles R:Reload Ctrl+R:Reload all")
        {
            BackgroundColor = Configuration.getColor(Config.Global.UI.Colors.FeedListFooterBackground),
            ForegroundColor = Configuration.getColor(Config.Global.UI.Colors.FeedListFooterForeground),
            PadChar = '-'
        };
        Header articleListHeader = new Header("") {
            BackgroundColor = Configuration.getColor(Config.Global.UI.Colors.ArticleListHeaderBackground),
            ForegroundColor = Configuration.getColor(Config.Global.UI.Colors.ArticleListHeaderForeground),
            PadChar = '-'
        };
        Footer articleListFooter = new Footer(" ESC/Backspace:Back R:Reload")
        {
            BackgroundColor = Configuration.getColor(Config.Global.UI.Colors.ArticleListFooterBackground),
            ForegroundColor = Configuration.getColor(Config.Global.UI.Colors.ArticleListFooterForeground),
            PadChar = '-'
        };
        Header articleHeader = new Header("")
        {
            BackgroundColor = Configuration.getColor(Config.Global.UI.Colors.ArticleHeaderBackground),
            ForegroundColor = Configuration.getColor(Config.Global.UI.Colors.ArticleHeaderForeground),
            PadChar = '-'
        };
        Footer articleFooter = new Footer(" ESC/Backspace:Back O:Open in browser N:Next") {
            AutoRefresh = false,
            BackgroundColor = Configuration.getColor(Config.Global.UI.Colors.ArticleFooterBackground),
            ForegroundColor = Configuration.getColor(Config.Global.UI.Colors.ArticleFooterForeground),
            PadChar = '-'
        };

        public FeedHandler(IList<RssFeed> feeds, LiteDatabase db)
        {
            _feeds = feeds;
            _db = db;
        }

        public void DisplayFeedList()
        {

            void processItem(ListItem<RssFeed> i, CGui.Gui.Picklist<RssFeed> parent)
            {
                i.Value.Load();
                i.DisplayText = i.Value.DisplayLine;
                parent.UpdateItem(i.Index);
            }

            var rssFeeds = _feeds
                    .Select((item, index) => new ListItem<RssFeed>()
                    {
                        Index = index,
                        DisplayText = $"{index + 1} - {item.FeedUrl} - Loading...",
                        Value = item
                    }).ToList();


            //Initialize mainview
            _mainView = new Viewport();
            _mainView.Height = Console.WindowHeight;

            var list = new CGui.Gui.Picklist<RssFeed>(rssFeeds, processItem);

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
            if (Configuration.verifyKey(key, Config.Global.Shortcuts.QuitApp.Key, Config.Global.Shortcuts.QuitApp.Modifiers))
            {
                return false;
            }

            //Reload all
            if (Configuration.verifyKey(key, Config.Global.Shortcuts.ReloadAll.Key, Config.Global.Shortcuts.ReloadAll.Modifiers))
            {
                Parallel.ForEach(parent.ListItems, (item) =>
                {
                    item.Value.Load();
                    parent.Refresh();
                });
            }

            //Reload
            if (Configuration.verifyKey(key, Config.Global.Shortcuts.Reload.Key, Config.Global.Shortcuts.Reload.Modifiers))
            {
                if (selectedItem.Value.Isloaded)
                {
                    selectedItem.DisplayText += " - Loading...";
                    parent.Refresh();
                    selectedItem.Value.Load();
                    selectedItem.DisplayText = selectedItem.Value.DisplayLine;
                    parent.Refresh();
                }
            }

            //Open
            if (Configuration.verifyKey(key, Config.Global.Shortcuts.OpenArticle.Key, Config.Global.Shortcuts.OpenArticle.Modifiers))
            {
                if (selectedItem != null)
                {
                    if (selectedItem.Value.Isloaded)
                    {
                        DisplayArticleList(selectedItem);
                        if (feedListHeader != null) { feedListHeader.Show(); }
                        if (feedListFooter != null) { feedListFooter.Show(); }
                        parent.Refresh();
                    }
                }
            }

            //Redraw view
            if (Configuration.verifyKey(key, Config.Global.Shortcuts.Refresh.Key, Config.Global.Shortcuts.Refresh.Modifiers))
            {
                _mainView.Show();
            }
            return true;
        }

        private void DisplayArticleList(ListItem<RssFeed> feed) {
            var items = feed.Value.FeedItems
                .OrderByDescending(x => x.PublishDate)
                .Select((item, index) => new ListItem<CFeedItem>()
                {
                    Index = index,
                    DisplayText = $"{(index + 1).ToString().PadLeft(3)} {item.DisplayText}",
                    Value = item
                });
            _selectedFeed = feed;

            Console.Clear();

            if (articleListHeader != null) {
                articleListHeader.DisplayText = feed.Value.TitleLine;
                articleListHeader.Show();
            }
            if (articleListFooter != null) { articleListFooter.Show();  }

            var list = new CGui.Gui.Picklist<CFeedItem>(items.ToList());
            if (Config.Global.UI.Layout.ArticleListHeight > 0)
            {
                list.Height = Config.Global.UI.Layout.ArticleListHeight;
            }
            else if (Config.Global.UI.Layout.ArticleListHeight < 0 && _mainView != null)
            {
                list.Height = _mainView.Height + Config.Global.UI.Layout.ArticleListHeight;
            }
            else {
                list.Height = 10;
            }
            list.Width = Console.WindowWidth - Config.Global.UI.Layout.ArticleListLeft;
            list.Top = Config.Global.UI.Layout.ArticleListTop;
            list.OnItemKeyHandler += ArticleList_OnItemKeyHandler;
            list.ShowScrollbar = true;
            list.Show();

            Console.Clear();
        }

        private bool ArticleList_OnItemKeyHandler(ConsoleKeyInfo key, ListItem<CFeedItem> selectedItem, Picklist<CFeedItem> parent)
        {
            switch (key.Key)
            {
                case ConsoleKey.Escape:
                case ConsoleKey.Backspace:
                    return false;

                case ConsoleKey.Enter:
                case ConsoleKey.Spacebar:
                    DisplayArticle(selectedItem, parent);
                    break;

                case ConsoleKey.R:
                    if (_selectedFeed != null)
                    {
                        if (articleListHeader != null)
                        {
                            articleListHeader.DisplayText += "- Loading...";
                            articleListHeader.Refresh();
                        }
                        _selectedFeed.Value.Load();
                        parent.Refresh();
                        if (articleListHeader != null)
                        {
                            articleListHeader.DisplayText = _selectedFeed.Value.TitleLine;
                            articleListHeader.Refresh();
                        }
                    }
                    break;
            }
            return true;
        }

        private void PrepareArticle() {
            
            if (_selectedArticle.Index < _selectedFeed.Value.TotalItems - 1)
            {
                _nextUnreadArticle = _parentArticleList.ListItems
                    .OrderBy(x => x.Index)
                    .Where(x => x.Value.IsNew == true && x.Index > _selectedArticle.Index)
                    .FirstOrDefault();
            }

            _selectedArticle.Value.MarkAsRead(this._db);
            _selectedArticle.DisplayText = $"{(_selectedArticle.Index + 1).ToString().PadLeft(3)} {_selectedArticle.Value.DisplayText}";
            if (_selectedFeed != null)
            {
                _selectedFeed.Value.UnreadItems--;
                if (_selectedFeed.Value.Filters != null)
                {
                    _filters = _selectedFeed.Value.Filters;
                }
            }
            if (articleListHeader != null)
            {
                articleListHeader.DisplayText = _selectedFeed.Value.TitleLine;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Feed: " + _selectedArticle.Value.Title);
            sb.AppendLine("Title: " + _selectedArticle.Value.Title);
            sb.AppendLine("Author(s): ");
            foreach (var a in _selectedArticle.Value.Authors)
            {
                sb.AppendLine(a.Name);
            }
            sb.AppendLine("Links: ");
            foreach (var a in _selectedArticle.Value.Links)
            {
                sb.AppendLine(a.Uri.ToString());
            }
            sb.AppendLine("Date: " + _selectedArticle.Value.PublishDate.ToString());
            sb.AppendLine();

            Console.Clear();
            if (articleHeader != null)
            {
                articleHeader.DisplayText = _selectedArticle.Value.FormatLine(Config.Global.UI.Strings.ArticleTitleFormat);
                articleHeader.Refresh();
            }
            if (articleFooter != null) { articleFooter.Show(); }

            var textArea = new TextArea(sb.ToString());
            textArea.Top = 2;
            textArea.Left = 2;
            textArea.Height = textArea.LinesCount + 1;
            textArea.Width = Console.WindowWidth - 6;
            textArea.WaitForInput = false;
            _articleContent = textArea;

            void onContentLoaded(string content)
            {
                var article = new TextArea(content);
                article.Top = textArea.LinesCount + 2;
                article.Left = 2;
                article.Height = Console.WindowHeight - 11;
                article.Width = Console.WindowWidth - 6;
                article.WaitForInput = true;
                article.OnItemKeyHandler += Article_OnItemKeyHandler;
                article.Show();
            }

            _selectedArticle.Value.OnContentLoaded = new Action<string>(s => { onContentLoaded(s); });
        }

        private void DisplayArticle(ListItem<CFeedItem> selectedItem, Picklist<CFeedItem> parent)
        {
            if (selectedItem != null)
            {
                _selectedArticle = selectedItem;
                _parentArticleList = parent;

                PrepareArticle();

                Parallel.Invoke(
                    new Action(() => _selectedArticle.Value.LoadOnlineArticle(_filters)),
                    new Action(() => _articleContent.Show())
                    );
                //Given lack of inspiration and a late hour, i commit this code for next article in hope that one day I will rewrite it
                //and provide this functionality with better design.
                while (_displayNext)
                {
                    _displayNext = false;
                    _selectedArticle = _nextUnreadArticle;
                    PrepareArticle();
                    Parallel.Invoke(
                        new Action(() => _selectedArticle.Value.LoadOnlineArticle(_filters)),
                        new Action(() => _articleContent.Show())
                        );
                }

                Console.Clear();
                if (articleListHeader != null) { articleListHeader.Show(); }
                if (articleListFooter!= null) { articleListFooter.Show(); }
                parent.Refresh();
            }
        }

        private bool Article_OnItemKeyHandler(ConsoleKeyInfo key)
        {
            //Next unread
            if (Configuration.verifyKey(key, Config.Global.Shortcuts.NextUnread.Key, Config.Global.Shortcuts.NextUnread.Modifiers))
            {
                if (_nextUnreadArticle != null && _selectedFeed != null)
                {
                    _displayNext = true;
                    return false;
                }
            }

            //Step back
            if (Configuration.verifyKey(key, Config.Global.Shortcuts.StepBack.Key, Config.Global.Shortcuts.StepBack.Modifiers))
            {
                return false;
            }

            //Open article
            if (Configuration.verifyKey(key, Config.Global.Shortcuts.OpenBrowser.Key, Config.Global.Shortcuts.OpenBrowser.Modifiers))
            {
                if (_selectedArticle != null &&
                    _selectedArticle.Value.Links.Count > 0)
                {
                    if (!string.IsNullOrEmpty(Config.Global.Browser)
                        && File.Exists(Config.Global.Browser))
                    {
                        //Open article url with configured browser
                        Process.Start(Config.Global.Browser, _selectedArticle.Value.Links[0].Uri.ToString());
                    }
                    else
                    {
                        //Open article url with default system browser
                        Process.Start(_selectedArticle.Value.Links[0].Uri.ToString());
                    }
                }
            }
            return true;
        }
    }
}
