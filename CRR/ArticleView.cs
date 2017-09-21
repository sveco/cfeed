using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CGui.Gui;
using CGui.Gui.Primitives;
using JsonConfig;
using LiteDB;

namespace CRR
{
    public class ArticleView
    {
        private ListItem<CFeedItem> selectedArticle;
        private ListItem<RssFeed> selectedFeed;
        private Picklist<CFeedItem> parentArticleList;
        private LiteDatabase db;
        private ListItem<CFeedItem> _nextUnreadArticle;
        private string[] _filters;
        private TextArea _articleContent;
        private bool _displayNext = false;

        Header articleHeader = new Header("")
        {
            BackgroundColor = Configuration.GetColor(Config.Global.UI.Colors.ArticleHeaderBackground),
            ForegroundColor = Configuration.GetColor(Config.Global.UI.Colors.ArticleHeaderForeground),
            PadChar = '-'
        };
        Footer articleFooter = new Footer(Config.Global.UI.Strings.ArticleFooter)
        {
            AutoRefresh = false,
            BackgroundColor = Configuration.GetColor(Config.Global.UI.Colors.ArticleFooterBackground),
            ForegroundColor = Configuration.GetColor(Config.Global.UI.Colors.ArticleFooterForeground),
            PadChar = '-'
        };

        public ArticleView(LiteDatabase Db)
        {
            db = Db;
        }

        private void PrepareArticle()
        {

            if (selectedArticle.Index < selectedFeed.Value.TotalItems - 1)
            {
                _nextUnreadArticle = parentArticleList.ListItems
                    .OrderBy(x => x.Index)
                    .Where(x => x.Value.IsNew == true && x.Index > selectedArticle.Index)
                    .FirstOrDefault();
            }

            selectedArticle.Value.MarkAsRead(db);
            selectedArticle.DisplayText = $"{selectedArticle.Value.DisplayText}";
            if (selectedFeed != null)
            {
                selectedFeed.Value.UnreadItems--;
                if (selectedFeed.Value.Filters != null)
                {
                    _filters = selectedFeed.Value.Filters;
                }
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(Configuration.ArticleTextHighlight + "Feed: " + Configuration.ColorReset + selectedArticle.Value.FeedUrl);
            sb.AppendLine(Configuration.ArticleTextHighlight + "Title: " + Configuration.ColorReset + selectedArticle.Value.Title);
            sb.AppendLine(Configuration.ArticleTextHighlight + "Author(s): " + Configuration.ColorReset + String.Join(", ", selectedArticle.Value.Authors.Select(x => x.Name).ToArray()));
            sb.AppendLine(Configuration.ArticleTextHighlight + "Link: " + Configuration.ColorReset + selectedArticle.Value.Links?[0].Uri.GetLeftPart(UriPartial.Path));
            sb.AppendLine(Configuration.ArticleTextHighlight + "Date: " + Configuration.ColorReset + selectedArticle.Value.PublishDate.ToString());
            sb.AppendLine();

            Console.Clear();
            if (articleHeader != null)
            {
                articleHeader.DisplayText = selectedArticle.Value.DisplayTitle;
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

            selectedArticle.Value.OnContentLoaded = new Action<string>(s => { onContentLoaded(s); });
        }

        public void DisplayArticle(ListItem<CFeedItem> SelectedArticle, ListItem<RssFeed> SelectedFeed, Picklist<CFeedItem> Parent)
        {
            if (SelectedArticle != null)
            {
                selectedArticle = SelectedArticle;
                selectedFeed = SelectedFeed;
                parentArticleList = Parent;

                PrepareArticle();

                Parallel.Invoke(
                    new Action(() => selectedArticle.Value.LoadArticle(_filters, db)),
                    new Action(() => _articleContent.Show())
                    );
                //Given lack of inspiration and a late hour, i commit this code for next article in hope that one day I will rewrite it
                //and provide this functionality with better design.
                while (_displayNext)
                {
                    _displayNext = false;
                    selectedArticle = _nextUnreadArticle;
                    PrepareArticle();
                    Parallel.Invoke(
                        new Action(() => selectedArticle.Value.LoadArticle(_filters, db)),
                        new Action(() => _articleContent.Show())
                        );
                }

                Console.Clear();
                //if (articleListHeader != null) { articleListHeader.Show(); }
                //if (articleListFooter != null) { articleListFooter.Show(); }
                Parent.Refresh();
            }
        }

        private bool Article_OnItemKeyHandler(ConsoleKeyInfo key)
        {
            //Next unread
            if (Configuration.VerifyKey(key, Config.Global.Shortcuts.NextUnread.Key, Config.Global.Shortcuts.NextUnread.Modifiers))
            {
                if (_nextUnreadArticle != null && selectedFeed != null)
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
                if (selectedArticle != null &&
                    selectedArticle.Value.Links.Count > 0)
                {
                    if (!string.IsNullOrEmpty(Config.Global.Browser)
                        && File.Exists(Config.Global.Browser))
                    {
                        //Open article url with configured browser
                        Process.Start(Config.Global.Browser, selectedArticle.Value.Links[0].Uri.ToString());
                    }
                    else
                    {
                        //Open article url with default system browser
                        Process.Start(selectedArticle.Value.Links[0].Uri.ToString());
                    }
                }
                return true;
            }

            //Save article
            if (Configuration.VerifyKey(key, Config.Global.Shortcuts.SaveArticle.Key, Config.Global.Shortcuts.SaveArticle.Modifiers))
            {
                if (selectedArticle != null)
                {
                    //selectedArticle.Value.LoadOnlineArticle(_filters, _db);
                    Parallel.Invoke(
                        new Action(() => selectedArticle.Value.LoadOnlineArticle(_filters, db)),
                        new Action(() => _articleContent.Show())
                        );
                }
                return false;
            }

            if (Configuration.VerifyKey(key, Config.Global.Shortcuts.OpenLink.Key, Config.Global.Shortcuts.OpenLink.Modifiers))
            {
                if (selectedArticle != null && selectedArticle.Value != null && selectedArticle.Value.IsLoaded)
                {
                    var input = new Input("Link #:")
                    {
                        Top = Console.WindowHeight - 2
                    };

                    int linkNumber;
                    if (int.TryParse(input.InputText, out linkNumber))
                    {
                        if (selectedArticle.Value.ExternalLinks != null
                            && selectedArticle.Value.ExternalLinks.Count >= linkNumber
                            && linkNumber > 0)
                        {
                            try
                            {
                                Process.Start(Config.Global.Browser, selectedArticle.Value.ExternalLinks[linkNumber - 1].ToString());
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
