using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CGui.Gui;
using CGui.Gui.Primitives;
using JsonConfig;
using LiteDB;

namespace CRR
{
    public class ArticleListView
    {
        private Viewport mainView;
        private ListItem<RssFeed> selectedFeed;
        private LiteDatabase db;
        private ArticleView article;

        Header articleListHeader = new Header("")
        {
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

        private Viewport _view;

        public ArticleListView(Viewport MainView,  LiteDatabase Db)
        {
            mainView = MainView;
            db = Db;
            article = new ArticleView(db);

            //_view = new Viewport();
            //_view.Controls.Add(articleListHeader);
            //_view.Controls.Add(articleListFooter);

        }

        public void DisplayArticleList(ListItem<RssFeed> feed)
        {
            selectedFeed = feed;

            var items = feed.Value.FeedItems
                .OrderByDescending(x => x.PublishDate)
                .Select((item, index) => new ListItem<CFeedItem>()
                {
                    Index = index,
                    DisplayText = $"{item.DisplayText}",
                    Value = item
                });

            //Console.Clear();

            if (articleListHeader != null)
            {
                articleListHeader.DisplayText = feed.Value.TitleLine;
                articleListHeader.Show();
            }
            if (articleListFooter != null) { articleListFooter.Show(); }

            var list = new Picklist<CFeedItem>(items.ToList());
            if (Config.Global.UI.Layout.ArticleListHeight > 0)
            {
                list.Height = Config.Global.UI.Layout.ArticleListHeight;
            }
            else if (Config.Global.UI.Layout.ArticleListHeight < 0 && mainView != null)
            {
                list.Height = mainView.Height + Config.Global.UI.Layout.ArticleListHeight;
            }
            else
            {
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
                    selectedItem.Value.MarkAsRead(db);
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
                    article.DisplayArticle(selectedItem, selectedFeed, parent);



                    break;

                case ConsoleKey.R:
                    if (selectedFeed != null)
                    {
                        if (articleListHeader != null)
                        {
                            articleListHeader.DisplayText = Configuration.LoadingPrefix + articleListHeader.DisplayText + Configuration.LoadingSuffix;
                            articleListHeader.Refresh();
                        }
                        selectedFeed.Value.Load(true);

                        var items = selectedFeed.Value.FeedItems
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
                            articleListHeader.DisplayText = selectedFeed.Value.TitleLine;
                            articleListHeader.Refresh();
                        }
                    }
                    break;
            }
            return true;
        }
    }
}
