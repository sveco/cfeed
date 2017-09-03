using CGui.Gui;
using CGui.Gui.Primitives;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRR
{
    public class FeedHandler
    {
        private IList<string> _feedUrls = new System.Collections.Generic.List<string>();
        private LiteDatabase _db;
        private CFeedItem _selectedArticle;
        private RssFeed _selectedFeed;
        private Viewport _mainview;

        Header feedListHeader => new Header(" cfeed v0.2 - console feed reader ")
        {
            BackgroundColor = Config.FeedListHeaderBackground,
            ForegroundColor = Config.FeedListHeaderForeground,
            PadChar = '-'
        };
        Footer feedListFooter => new Footer(" Q:Quit ENTER/Space:List articles R:Reload Ctrl+R:Reload all")
        {
            BackgroundColor = Config.FeedListFooterBackground,
            ForegroundColor = Config.FeedListFooterForeground,
            PadChar = '-'
        };
        Header articleListHeader = new Header("") {
            BackgroundColor = Config.ArticleListHeaderBackground,
            ForegroundColor = Config.ArticleListHeaderForeground,
            PadChar = '-'
        };
        Footer articleListFooter = new Footer(" ESC/Backspace:Back R:Reload")
        {
            BackgroundColor = Config.ArticleListFooterBackground,
            ForegroundColor = Config.ArticleListFooterForeground,
            PadChar = '-'
        };
        Header articleHeader = new Header("")
        {
            BackgroundColor = Config.ArticleHeaderBackground,
            ForegroundColor = Config.ArticleHeaderForeground,
            PadChar = '-'
        };
        Footer articleFooter = new Footer(" ESC/Backspace:Back O:Open in browser N:Next") {
            AutoRefresh = false,
            BackgroundColor = Config.ArticleFooterBackground,
            ForegroundColor = Config.ArticleFooterForeground,
            PadChar = '-'
        };

        public FeedHandler(IList<string> urls, LiteDatabase db)
        {
            _feedUrls = urls;
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

            var rssFeeds = _feedUrls
                    .Select((item, index) => new ListItem<RssFeed>()
                    {
                        Index = index,
                        DisplayText = $"{index + 1} - {item} - Loading...",
                        Value = new RssFeed(item, index, _db)
                    }).ToList();


            //GUI
            _mainview = new Viewport();
            _mainview.Height = Console.WindowHeight;

            var list = new CGui.Gui.Picklist<RssFeed>(rssFeeds, processItem);

            list.Top = Config.FeedListTop;
            list.Left = Config.FeedListLeft;
            list.Height = Config.FeedMaxItems;
            list.Width = Console.WindowWidth - Config.FeedListLeft-1;
            list.OnItemKeyHandler += FeedList_OnItemKeyHandler;
            list.ShowScrollbar = true;

            _mainview.Controls.Add(feedListHeader);
            _mainview.Controls.Add(feedListFooter);
            _mainview.Controls.Add(list);

            _mainview.Show();
        }

        private bool FeedList_OnItemKeyHandler(ConsoleKeyInfo key, ListItem<RssFeed> selectedItem, Picklist<RssFeed> parent)
        {
            switch (key.Key)
            {
                case ConsoleKey.Enter:
                case ConsoleKey.Spacebar:
                    if (selectedItem != null)
                    {
                        if (selectedItem.Value.Isloaded)
                        {
                            DisplayArticleList(selectedItem.Value);
                            if (feedListHeader != null) { feedListHeader.Show(); }
                            if (feedListFooter != null) { feedListFooter.Show(); }
                            parent.Refresh();
                        }
                    }
                    break;
                case ConsoleKey.R:
                    if (key.Modifiers == ConsoleModifiers.Control)
                    {
                        Parallel.ForEach(parent.ListItems, (item) => {
                            item.Value.Load();
                            parent.Refresh();
                        });
                    }
                    else
                    {
                        if (selectedItem != null)
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
                    }
                    break;
                case ConsoleKey.F:
                    _mainview.Show();
                    break;

                case ConsoleKey.Q:
                    return false;
            }
            return true;
        }

        private void DisplayArticleList(RssFeed feed) {
            var items = feed.FeedItems
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
                articleListHeader.DisplayText = feed.TitleLine;
                articleListHeader.Show();
            }
            if (articleListFooter != null) { articleListFooter.Show();  }

            var list = new CGui.Gui.Picklist<CFeedItem>(items.ToList());
            if (Config.ArticleListHeight > 0)
            {
                list.Height = Config.ArticleListHeight;
            }
            else if (Config.ArticleListHeight < 0 && _mainview != null)
            {
                list.Height = _mainview.Height + Config.ArticleListHeight;
            }
            else {
                list.Height = 10;
            }
            list.Width = Console.WindowWidth - Config.ArticleListLeft;
            list.Top = Config.ArticleListTop;
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
                        _selectedFeed.Load();
                        parent.Refresh();
                        if (articleListHeader != null)
                        {
                            articleListHeader.DisplayText = _selectedFeed.TitleLine;
                            articleListHeader.Refresh();
                        }
                    }
                    break;
            }
            return true;
        }

        private void DisplayArticle(ListItem<CFeedItem> selectedItem, Picklist<CFeedItem> parent)
        {
            if (selectedItem != null)
            {
                _selectedArticle = selectedItem.Value;
                selectedItem.Value.MarkAsRead(this._db);
                selectedItem.DisplayText = $"{(selectedItem.Index + 1).ToString().PadLeft(3)} {selectedItem.Value.DisplayText}";
                if (_selectedFeed != null)
                {
                    _selectedFeed.UnreadItems--;
                }
                if (articleListHeader != null)
                {
                    articleListHeader.DisplayText = _selectedFeed.TitleLine;
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Feed: " + selectedItem.Value.Title);
                sb.AppendLine("Title: " + selectedItem.Value.Title);
                sb.AppendLine("Author(s): ");
                foreach (var a in selectedItem.Value.Authors)
                {
                    sb.AppendLine(a.Name);
                }
                sb.AppendLine("Links: ");
                foreach (var a in selectedItem.Value.Links)
                {
                    sb.AppendLine(a.Uri.ToString());
                }
                sb.AppendLine("Date: " + selectedItem.Value.PublishDate.ToString());
                sb.AppendLine();

                Console.Clear();
                if (articleHeader != null)
                {
                    articleHeader.DisplayText = selectedItem.Value.FormatLine(Config.ArticleTitleFormat);
                    articleHeader.Refresh();
                }
                if (articleFooter != null) { articleFooter.Show(); }


                var textArea = new TextArea(sb.ToString());
                textArea.Top = 2;
                textArea.Left = 2;
                textArea.Height = textArea.LinesCount + 1;
                textArea.Width = Console.WindowWidth - 6;
                textArea.WaitForInput = false;

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

                selectedItem.Value.OnContentLoaded = new Action<string>(s => { onContentLoaded(s); });
                Parallel.Invoke(
                    new Action(() => selectedItem.Value.LoadOnlineArticle()),
                    new Action(() => textArea.Show())
                    );

                Console.Clear();
                if (articleListHeader != null) { articleListHeader.Show(); }
                if (articleListFooter!= null) { articleListFooter.Show(); }
                parent.Refresh();
            }
        }

        private bool Article_OnItemKeyHandler(ConsoleKeyInfo key)
        {
            switch (key.Key)
            {
                case ConsoleKey.Escape:
                case ConsoleKey.Backspace:
                    return false;

                case ConsoleKey.O:
                    if (_selectedArticle != null &&
                        _selectedArticle.Links.Count > 0)
                    {
                        Process.Start(_selectedArticle.Links[0].Uri.ToString());
                    }
                    break;
            }
            return true;
        }
    }
}
