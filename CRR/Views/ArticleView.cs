namespace cFeed.Views
{
	using System;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using System.Timers;
	using cFeed.Entities;
	using cFeed.Logging;
	using cFeed.Util;
	using CGui.Gui;
	using CGui.Gui.Primitives;
	using CSharpFunctionalExtensions;
	using JsonConfig;

	public class ArticleView : BaseView
	{
		TextArea _articleContent;
		bool _displayNext;
		string[] _filters;
		string _select;
		dynamic footerFormat;
		dynamic headerFormat;

		FeedItem _nextArticle;
		public FeedItem NextArticle { get => _nextArticle; set => _nextArticle = value; }

		Picklist<FeedItem> parentArticleList;
		FeedItem selectedArticle;
		RssFeed selectedFeed;
		Timer timer = new Timer();
		String timerElipsis = string.Empty;

		private bool CanShowNext
		{
			get { return selectedFeed != null && selectedArticle != null; }
		}

		public ArticleView(dynamic layout) : base((ConfigObject)layout)
		{
			headerFormat = Config.Global.UI.Strings.ArticleHeaderFormat;
			footerFormat = Config.Global.UI.Strings.ArticleFooterFormat;
		}

		public void Show(RssFeed feed, Picklist<FeedItem> parent)
		{
			
			if (_nextArticle != null)
			{
				this.selectedArticle = _nextArticle;
				_nextArticle = null;
				this.selectedFeed = feed;
				parentArticleList = parent;

				PrepareArticle();

				Parallel.Invoke(
					new Action(() => this.selectedArticle.LoadArticle(_select, _filters)),
					new Action(_articleContent.Show)
					);

				this.selectedArticle.DisplayText = this.selectedArticle.DisplayText;
			}
		}

		private bool Article_OnItemKeyHandler(ConsoleKeyInfo key)
		{
			_nextArticle = null;

			//Next
			if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.Next))
			{
				return ShowNext();
			}
			//Next unread
			if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.NextUnread))
			{
				return ShowNextUnread();
			}
			//Prev
			if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.Prev))
			{
				return ShowPrevious();
			}
			//Prev unread
			if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.PrevUnread))
			{
				return ShowPreviousUnread();
			}

			//Step back
			if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.StepBack))
			{
				return false;
			}

			//Open article
			if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.OpenBrowser))
			{
				return OpenArticle();
			}

			//Save article
			if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.SaveArticle))
			{
				return SaveArticle();
			}

			//Open numbered link
			if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.OpenLink))
			{
				return OpenLink();
			}

			//Open numbered image
			if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.OpenImage))
			{
				return OpenImage();
			}

			//Mark article for deletion
			if (key.VerifyKey((ConfigObject)Config.Global.Shortcuts.Delete))
			{
				return DeleteArticle();
			}

			return true;
		}

		private bool DeleteArticle()
		{
			if (selectedArticle != null)
			{
				selectedArticle.MarkDeleted();
				selectedArticle.DisplayText = selectedArticle.DisplayText;
			}
			return true;
		}

		private bool DisplayNext()
		{
			_displayNext = _nextArticle != null;
			return !_displayNext;
		}

		private void HideLoadingText()
		{
			if (_mainView.Controls.FirstOrDefault(x => x.Name == "Loading") is TextArea loadingText)
			{
				loadingText.Clear();
			}
		}

		private bool OpenArticle()
		{
			if (selectedArticle != null &&
				selectedArticle.Links.Count > 0)
			{
				Browser.Open(selectedArticle.Links[0].Uri);
			}
			return true;
		}

		private bool OpenImage()
		{
			if (selectedArticle != null && selectedArticle != null && selectedArticle.IsLoaded)
			{
				var input = new Input("Image #:")
				{
					Top = Console.WindowHeight - 2,
					ForegroundColor = Configuration.GetColor(Config.Global.UI.Colors.LinkInputForeground),
					BackgroundColor = Configuration.GetColor(Config.Global.UI.Colors.LinkInputBackground),
				};

				if (int.TryParse(input.InputText, out int linkNumber))
				{
					if (selectedArticle.ImageLinks != null
						&& selectedArticle.ImageLinks.Count >= linkNumber
						&& linkNumber > 0)
					{
						try
						{
							if (!String.IsNullOrWhiteSpace(Config.Global.Browser))
							{
								Process.Start(Config.Global.Browser, selectedArticle.ImageLinks[linkNumber - 1].ToString());
							}
							else
							{
								Process.Start(selectedArticle.ImageLinks[linkNumber - 1].ToString());
							}
						}
						catch (Win32Exception ex)
						{
							logger.Error(ex);
						}
					}
				}
			}
			return true;
		}

		private bool OpenLink()
		{
			if (selectedArticle != null && selectedArticle != null && selectedArticle.IsLoaded)
			{
				var input = new Input("Link #:")
				{
					Top = Console.WindowHeight - 2,
					ForegroundColor = Configuration.GetColor(Config.Global.UI.Colors.LinkInputForeground),
					BackgroundColor = Configuration.GetColor(Config.Global.UI.Colors.LinkInputBackground),
				};

				if (int.TryParse(input.InputText, out int linkNumber))
				{
					if (selectedArticle.ExternalLinks != null
						&& selectedArticle.ExternalLinks.Count + selectedArticle.Links.Count >= linkNumber
						&& linkNumber > 0)
					{
						string link;
						if (linkNumber <= selectedArticle.Links.Count)
						{
							link = selectedArticle.Links[linkNumber - 1].Uri.ToString();
						}
						else
						{
							link = selectedArticle.ExternalLinks[linkNumber - 1 - selectedArticle.Links.Count].ToString();
						}

						try
						{
							if (!String.IsNullOrWhiteSpace(Config.Global.Browser))
							{
								Process.Start(Config.Global.Browser, link);
							}
							else
							{
								Process.Start(link);
							}
						}
						catch (Win32Exception ex)
						{
							logger.Error(ex);
						}
					}
				}
			}
			return true;
		}

		private void PrepareArticle()
		{
			if (selectedFeed != null)
			{
				if (selectedFeed.Filters != null)
				{
					_filters = selectedFeed.Filters;
					_select = selectedFeed.Select;
				}
			}

			void onContentLoaded(string content)
			{
				if (_mainView.Controls.FirstOrDefault(x => x.Name == "ArticleContent") is TextArea article)
				{
					article.Content = content;
					article.OnItemKeyHandler += Article_OnItemKeyHandler;
					article.WaitForInput = true;
					selectedArticle.MarkAsRead();
					timer.Stop();
					HideLoadingText();
					article.Show();
				}
				else
				{
					logger.Warn("Missing \"ArticleContent\" text area, cannot display article content.");
				}
			}

			showArticleHeader()
			  .OnSuccess(() =>
			  {
				  ShowHeader(selectedArticle.TitleLine);
				  ShowFooter(selectedArticle.FormatLine(footerFormat));
				  selectedArticle.OnContentLoaded = new Action<string>(onContentLoaded);
			  })
			  .OnSuccess(() => ShowLoadingText())
			  .OnSuccess(() => StartTimer())
			  .OnSuccess(() => _mainView.Show());
		}

		private bool SaveArticle()
		{
			if (selectedArticle != null)
			{
				Parallel.Invoke(
					new Action(() => selectedArticle.LoadOnlineArticle(_select, _filters)),
					new Action(_articleContent.Show)
					);
			}
			return false;
		}

		private Result showArticleHeader()
		{
			if (selectedArticle == null)
			{
				return Result.Fail("Article not selected.");
			}

			string linkHighlight = Configuration.GetForegroundColor(Config.Global.UI.Colors.LinkHighlight);
			string linkTextHighlight = Configuration.GetForegroundColor(Config.Global.UI.Colors.LinkTextHighlight);
			string resetColor = Configuration.ColorReset;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine(Configuration.Instance.ArticleTextHighlight + Configuration.Instance.ArticleTextFeedUrlLabel + Configuration.ColorReset + selectedArticle.FeedUrl);
			sb.AppendLine(Configuration.Instance.ArticleTextHighlight + Configuration.Instance.ArticleTextTitleLabel + Configuration.ColorReset + selectedArticle.Title);
			sb.AppendLine(Configuration.Instance.ArticleTextHighlight + Configuration.Instance.ArticleTextAuthorsLabel + Configuration.ColorReset + String.Join(", ", selectedArticle.Authors.Select(x => x.Name).ToArray()));
			sb.AppendLine(Configuration.Instance.ArticleTextHighlight + Configuration.Instance.ArticleTextLinkLabel + Configuration.ColorReset);
			for (int i = 0; i < selectedArticle.Links.Count; i++)
			{
				sb.AppendLine(linkHighlight + "[" + (i + 1).ToString() + "] " + resetColor + Configuration.Instance.ArticleTextHighlight + Configuration.ColorReset + selectedArticle.Links?[i].Uri.GetLeftPart(UriPartial.Path));
			}
			sb.AppendLine(Configuration.Instance.ArticleTextHighlight + Configuration.Instance.ArticleTextPublishDateLabel + Configuration.ColorReset + selectedArticle.PublishDate.ToString());
			sb.AppendLine();

			var textArea = new TextArea(sb.ToString());
			sb = null;
			textArea.Top = 2;
			textArea.Left = 2;
			textArea.Width = Console.WindowWidth - 6;
			textArea.Height = textArea.TotalItems + 1;
			textArea.WaitForInput = false;
			_articleContent = textArea;

			return Result.Ok();
		}

		private void ShowLoadingText()
		{
			if (_mainView.Controls.FirstOrDefault(x => x.Name == "Loading") is TextArea loadingText)
			{
				loadingText.Content = Configuration.Instance.LoadingText;
				loadingText.TextAlignment = TextAlignment.Center;
			}
		}

		private bool ShowNext()
		{
			if (CanShowNext)
			{
				_nextArticle = (FeedItem)parentArticleList.ListItems
				  .OrderByDescending(x => x.Index)
				  .FirstOrDefault(x => x.Index < selectedArticle.Index);
			}
			return DisplayNext();
		}

		private bool ShowNextUnread()
		{
			if (CanShowNext)
			{
				_nextArticle = (FeedItem)parentArticleList.ListItems
				  .OrderByDescending(x => x.Index)
				  .FirstOrDefault(i => ((FeedItem)i).IsNew == true && i.Index < selectedArticle.Index);
			}
			return DisplayNext();
		}

		private bool ShowPrevious()
		{
			if (CanShowNext)
			{
				if (selectedArticle.Index < selectedFeed.TotalItems - 1)
				{
					_nextArticle = (FeedItem)parentArticleList.ListItems
					  .OrderBy(x => x.Index)
					  .FirstOrDefault(x => x.Index > selectedArticle.Index);
				}
			}
			return DisplayNext();
		}

		private bool ShowPreviousUnread()
		{
			if (CanShowNext)
			{
				if (selectedArticle.Index < selectedFeed.TotalItems - 1)
				{
					_nextArticle = parentArticleList.ListItems
					  .OrderBy(x => x.Index)
					  .FirstOrDefault(i => ((FeedItem)i).IsNew == true && i.Index > selectedArticle.Index);
				}
			}
			return DisplayNext();
		}

		private void StartTimer()
		{
			timer.Interval = 500;
			timer.Elapsed += Timer_Elapsed;
			timer.Start();
		}

		private void Timer_Elapsed(object sender, ElapsedEventArgs e)
		{
			if (timerElipsis.Length < 3) { timerElipsis += "."; } else { timerElipsis = string.Empty; }
			if (_mainView != null && _mainView.Controls.FirstOrDefault(x => x.Name == "Loading") is TextArea loadingText && loadingText != null && loadingText.IsDisplayed)
			{
				loadingText.Content = timerElipsis + Configuration.Instance.LoadingText + timerElipsis;
				loadingText.Refresh();
			}
		}
	}
}