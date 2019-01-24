namespace cFeed.Views
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using cFeed.Entities;
	using cFeed.Util;
	using CGui.Gui;
	using CSharpFunctionalExtensions;
	using JsonConfig;

	/// <summary>
	/// Displays list of articles for selected feed
	/// </summary>
	public class ArticleListView : BaseView
	{
		dynamic footerFormat;
		dynamic headerFormat;
		bool markAllDeleted;
		RssFeed selectedFeed;

		public ArticleListView(dynamic layout) : base((ConfigObject)layout)
		{
			headerFormat = Config.Global.UI.Strings.ArticleListHeaderFormat;
			footerFormat = Config.Global.UI.Strings.ArticleListFooterFormat;
		}

		public Result<Picklist<FeedItem>> GetPicklist()
		{
			var result = _mainView.Controls.FirstOrDefault(x => x.GetType() == typeof(Picklist<FeedItem>)) as Picklist<FeedItem>;
			if (result == null)
			{
				return Result.Fail<Picklist<FeedItem>>("Missing list config.");
			}
			else
			{
				return Result.Ok<Picklist<FeedItem>>(result);
			}
		}

		public void Show(RssFeed feed)
		{
			selectedFeed = feed;
			Result<IList<FeedItem>> getFeedResult = GetFeed(feed);
			getFeedResult
			  .OnSuccess(items =>
				{
					ShowHeader(feed.FormatLine(headerFormat));
					ShowFooter(feed.FormatLine(footerFormat));
				})
			  .OnSuccess(items => GetPicklist().OnSuccess(list =>
			   {
				   list.UpdateList(items);
				   list.OnItemKeyHandler += ArticleList_OnItemKeyHandler;
			   }))
			  .OnSuccess((list) =>
			  {
				  _mainView.Show();
			  })
			  .OnSuccess(list => selectedFeed.RefreshTitle());
		}

		private bool ArticleList_OnItemKeyHandler(ConsoleKeyInfo key, FeedItem selectedItem, Picklist<FeedItem> parent)
		{
			//Open article
			if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.OpenArticle))
			{
				return OpenArticle(selectedItem, parent);
			}
			//Mark selected item as read
			if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.MarkRead))
			{
				if (selectedItem != null)
				{
					selectedItem.MarkAsRead();
					selectedItem.DisplayText = selectedItem.DisplayLine;
				}
			}
			//Mark all read
			if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.MarkAllRead))
			{
				return MarkAllRead();
			}
			//Mark selected item as unread
			if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.MarkUnread))
			{
				if (selectedItem != null && !selectedItem.IsNew)
				{
					selectedItem.MarkUnread();
					selectedItem.DisplayText = selectedItem.DisplayLine;
				}
			}
			//Mark current item for deletion
			if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.Delete))
			{
				if (selectedItem != null)
				{
					selectedItem.MarkDeleted();
					selectedItem.DisplayText = selectedItem.DisplayLine;
				}
			}

			if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.DeleteAll))
			{
				return DeleteAllArticles();
			}

			if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.StepBack))
			{
				return false;
			}

			if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.Reload))
			{
				return Reload(parent);
			}
			//Download selected item content to local storage
			if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.Download))
			{
				return Download(selectedItem);
			}
			//Delete locally stored content
			if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.DeleteContent))
			{
				return DeleteContent(selectedItem);
			}
			//open in browser
			if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.OpenBrowser))
			{
				return OpenArticleInBrowser(selectedItem);
			}
			//search
			if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.Search))
			{
				parent.IsDisplayed = false;
				var result = GlobalMethods.Search(_mainView);
				parent.IsDisplayed = true;
				return result;
			}
			return true;
		}

		private void DeleteAll_ItemSelected(object sender, DialogChoice e)
		{
			markAllDeleted |= e.DisplayText == Config.Global.UI.Strings.PromptAnswerYes as string;
		}

		private bool DeleteAllArticles()
		{
			if (selectedFeed != null && !selectedFeed.IsProcessing)
			{
				Dictionary<string, object> choices = new Dictionary<string, object>
		{
		  { Config.Global.UI.Strings.PromptAnswerYes, 1 },
		  { Config.Global.UI.Strings.PromptAnswerNo, 2 }
		};

				var dialog = new Dialog(Config.Global.UI.Strings.PromptDeleteAll, choices);
				dialog.ItemSelected += DeleteAll_ItemSelected;
				dialog.Show();

				_mainView?.Refresh();
				if (markAllDeleted)
				{
					selectedFeed?.MarkAllDeleted();
					markAllDeleted = false;
				}
				return true;
			}
			return true;
		}

		private bool DeleteContent(FeedItem selectedItem)
		{
			if (selectedItem != null && selectedFeed != null && selectedItem.IsDownloaded == true)
			{
				selectedItem.DeleteArticleContent();
				selectedItem.DisplayText = selectedItem.DisplayLine;
			}
			return true;
		}

		private bool Download(FeedItem selectedItem)
		{
			if (selectedItem != null && selectedFeed != null && selectedItem.IsDownloaded == false)
			{
				selectedItem.DisplayText = Configuration.Instance.LoadingPrefix + selectedItem.DisplayText + Configuration.Instance.LoadingSuffix;
				selectedItem.DownloadArticleContent(selectedFeed.Select, selectedFeed.Filters);
				selectedItem.DisplayText = selectedItem.DisplayLine;
			}
			return true;
		}

		private Result<IList<FeedItem>> GetFeed(RssFeed feed)
		{
			var feedItems = feed.FeedItems
				.OrderByDescending(x => x.PublishDate)
				.Where(x => x.Deleted == false)
				.Select((item, index) =>
				{
					item.Index = index;
					item.DisplayText = item.DisplayLine;
					return item;
				}).ToList();
			return Result.Ok<IList<FeedItem>>(feedItems);
		}

		private void MarkAllDialog_ItemSelected(object sender, DialogChoice e)
		{
			if (e.DisplayText == Config.Global.UI.Strings.PromptAnswerYes as string)
			{
				selectedFeed?.MarkAllRead();
			}
			_mainView?.Refresh();
		}

		private bool MarkAllRead()
		{
			Dictionary<string, object> choices = new Dictionary<string, object>
	  {
		{ Config.Global.UI.Strings.PromptAnswerYes, 1 },
		{ Config.Global.UI.Strings.PromptAnswerNo, 2 }
	  };

			var dialog = new Dialog(Config.Global.UI.Strings.PromptMarkAll, choices);
			dialog.ItemSelected += MarkAllDialog_ItemSelected;
			dialog.Show();
			return true;
		}

		private bool OpenArticle(FeedItem selectedItem, Picklist<FeedItem> parent)
		{
			parent.IsDisplayed = false;
			parent.Clear();
			while (selectedItem != null)
			{
				using (ArticleView article = new ArticleView(Config.Global.UI.Layout.Article))
				{
					article.NextArticle = selectedItem;
					article.Show(selectedFeed, parent);
					selectedItem = article.NextArticle;
				}
				_mainView.Refresh();
			}
			return true;
		}

		private bool OpenArticleInBrowser(FeedItem selectedItem)
		{
			if (selectedItem != null && selectedItem.Links.Count > 0)
			{
				Browser.Open(selectedItem.Links[0].Uri);
			}
			return true;
		}

		private bool Reload(Picklist<FeedItem> parent)
		{
			if (selectedFeed != null)
			{
				selectedFeed.Load(true);

				var articleListHeader = _mainView.Controls.FirstOrDefault(x => x.GetType() == typeof(Header)) as Header;
				if (articleListHeader != null)
				{
					articleListHeader.DisplayText = selectedFeed.FormatLine(headerFormat);
					articleListHeader.Refresh();
				}

				var items = selectedFeed.FeedItems
					.OrderByDescending(x => x.PublishDate)
					.Where(x => x.Deleted == false)
					.Select((item, index) =>
					{
						item.Index = index;
						item.DisplayText = item.DisplayLine;
						return item;
					}
					);
				parent.UpdateList(items);
				parent.Refresh();

				if (articleListHeader != null)
				{
					articleListHeader.DisplayText = selectedFeed.FormatLine(headerFormat);
					articleListHeader.Refresh();
				}
			}
			return true;
		}
	}
}