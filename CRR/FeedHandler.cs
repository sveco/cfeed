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
        Header articleListHeader = new Header("") {
            BackgroundColor = Configuration.GetColor(Config.Global.UI.Colors.ArticleListHeaderBackground),
            ForegroundColor = Configuration.GetColor(Config.Global.UI.Colors.ArticleListHeaderForeground),
            PadChar = '-'
        };
        Footer articleListFooter = new Footer(Config.Global.UI.Strings.ArticleListFooter)
        {
            BackgroundColor = Configuration.GetColor(Config.Global.UI.Colors.ArticleListFooterBackground),
            ForegroundColor = Configuration.GetColor(Config.Global.UI.Colors.ArticleListFooterForeground),
            PadChar = '-'
        };
        Header articleHeader = new Header("")
        {
            BackgroundColor = Configuration.GetColor(Config.Global.UI.Colors.ArticleHeaderBackground),
            ForegroundColor = Configuration.GetColor(Config.Global.UI.Colors.ArticleHeaderForeground),
            PadChar = '-'
        };
        Footer articleFooter = new Footer(Config.Global.UI.Strings.ArticleFooter) {
            AutoRefresh = false,
            BackgroundColor = Configuration.GetColor(Config.Global.UI.Colors.ArticleFooterBackground),
            ForegroundColor = Configuration.GetColor(Config.Global.UI.Colors.ArticleFooterForeground),
            PadChar = '-'
        };

        public FeedHandler(IList<RssFeed> feeds, LiteDatabase db)
        {
            _feeds = feeds;
            _db = db;
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

            var rssFeeds = _feeds
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
                        DisplayArticleList(selectedItem);
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

        private void DisplayArticleList(ListItem<RssFeed> feed) {
            var items = feed.Value.FeedItems
                .OrderByDescending(x => x.PublishDate)
                .Select((item, index) => new ListItem<CFeedItem>()
                {
                    Index = index,
                    DisplayText = $"{item.DisplayText}",
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
            if (Configuration.VerifyKey(key, Config.Global.Shortcuts.MarkRead.Key, Config.Global.Shortcuts.MarkRead.Modifiers))
            {
                if (selectedItem != null)
                {
                    selectedItem.Value.MarkAsRead(this._db);
                    selectedItem.DisplayText = $"{(selectedItem.Index + 1).ToString().PadLeft(3)} {selectedItem.Value.DisplayText}";
                }
            }

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
                            articleListHeader.DisplayText = Configuration.LoadingPrefix + articleListHeader.DisplayText + Configuration.LoadingSuffix;
                            articleListHeader.Refresh();
                        }
                        _selectedFeed.Value.Load(true);

                        var items = _selectedFeed.Value.FeedItems
                            .OrderByDescending(x => x.PublishDate)
                            .Select((item, index) => {
                                    item.Index = index + 1;
                                    return new ListItem<CFeedItem>()
                                    {
                                        Index = index,
                                        DisplayText = $"{item.DisplayText}",
                                        Value = item
                                    };
                                }
                            );
                        parent.UpdateList(items);
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
            _selectedArticle.DisplayText = $"{_selectedArticle.Value.DisplayText}";
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
            sb.AppendLine(Configuration.ArticleTextHighlight + "Feed: " + Configuration.ColorReset + _selectedArticle.Value.FeedUrl);
            sb.AppendLine(Configuration.ArticleTextHighlight + "Title: " + Configuration.ColorReset + _selectedArticle.Value.Title);
            sb.AppendLine(Configuration.ArticleTextHighlight + "Author(s): " + Configuration.ColorReset + String.Join(", ", _selectedArticle.Value.Authors.Select(x => x.Name).ToArray()));
            sb.AppendLine(Configuration.ArticleTextHighlight + "Link: " + Configuration.ColorReset + _selectedArticle.Value.Links?[0].Uri.GetLeftPart(UriPartial.Path));
            sb.AppendLine(Configuration.ArticleTextHighlight + "Date: " + Configuration.ColorReset + _selectedArticle.Value.PublishDate.ToString());
            sb.AppendLine();

            Console.Clear();
            if (articleHeader != null)
            {
                articleHeader.DisplayText = _selectedArticle.Value.DisplayTitle;
                articleHeader.Refresh();
            }
            if (articleFooter != null) { articleFooter.Show(); }

            var textArea = new TextArea(sb.ToString());
            textArea.Top = 2;
            textArea.Left = 2;
            textArea.Width = Console.WindowWidth - 6;
            textArea.Height = textArea.LinesCount + 1;
            textArea.WaitForInput = false;
            _articleContent = textArea;

            void onContentLoaded(string content)
            {
                var article = new TextArea(content);
                article.Top = textArea.LinesCount + 3;
                article.Left = 2;
                article.Height = Console.WindowHeight - 12;
                article.Width = Console.WindowWidth - 6;
                article.WaitForInput = true;
                article.OnItemKeyHandler += Article_OnItemKeyHandler;
                article.ShowScrollbar = true;

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
                    new Action(() => _selectedArticle.Value.LoadArticle(_filters, _db)),
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
                        new Action(() => _selectedArticle.Value.LoadArticle(_filters, _db)),
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
            if (Configuration.VerifyKey(key, Config.Global.Shortcuts.NextUnread.Key, Config.Global.Shortcuts.NextUnread.Modifiers))
            {
                if (_nextUnreadArticle != null && _selectedFeed != null)
                {
                    _displayNext = true;
                    return false;
                }
            }

            //Step back
            if (Configuration.VerifyKey(key, Config.Global.Shortcuts.StepBack.Key, Config.Global.Shortcuts.StepBack.Modifiers))
            {
                return false;
            }

            //Open article
            if (Configuration.VerifyKey(key, Config.Global.Shortcuts.OpenBrowser.Key, Config.Global.Shortcuts.OpenBrowser.Modifiers))
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
                return true;
            }

            //Save article
            if (Configuration.VerifyKey(key, Config.Global.Shortcuts.SaveArticle.Key, Config.Global.Shortcuts.SaveArticle.Modifiers))
            {
                if (_selectedArticle != null)
                {
                    //_selectedArticle.Value.LoadOnlineArticle(_filters, _db);
                    Parallel.Invoke(
                        new Action(() => _selectedArticle.Value.LoadOnlineArticle(_filters, _db)),
                        new Action(() => _articleContent.Show())
                        );
                }
                return false;
            }

            if (Configuration.VerifyKey(key, Config.Global.Shortcuts.OpenLink.Key, Config.Global.Shortcuts.OpenLink.Modifiers))
            {
                if (_selectedArticle != null && _selectedArticle.Value != null && _selectedArticle.Value.IsLoaded)
                {
                    var input = new Input("Link #:")
                    {
                        Top = Console.WindowHeight - 2
                    };

                    int linkNumber;
                    if (int.TryParse(input.InputText, out linkNumber))
                    {
                        if (_selectedArticle.Value.ExternalLinks != null 
                            && _selectedArticle.Value.ExternalLinks.Count >= linkNumber
                            && linkNumber > 0)
                        {
                            try
                            {
                                Process.Start(Config.Global.Browser, _selectedArticle.Value.ExternalLinks[linkNumber - 1].ToString());
                            }
                            catch (System.ComponentModel.Win32Exception ex)
                            {
                                Debug.Write(ex.Message);
                                Debug.Write(ex.StackTrace);
                            }
                        }
                    }

                }
                return true;
            }

            return true;
        }
    }
}
