using System;
using cFeed.Entities;
using cFeed.Views;
using CGui.Gui;
using JsonConfig;

namespace cFeed.Util
{
  public static class GlobalMethods
  {
    /// <summary>
    /// Searches for given string in article title and description and displays search result.
    /// </summary>
    /// <param name="view"></param>
    /// <returns></returns>
    public static bool Search(Viewport view, RssFeed feed = null)
    {

      var searchInput = new Input("Search for: ")
      {
        Top = Console.WindowHeight - 2,
        ForegroundColor = Configuration.GetColor(Config.Global.UI.Colors.LinkInputForeground),
        BackgroundColor = Configuration.GetColor(Config.Global.UI.Colors.LinkInputBackground),
      };
      var searchString = searchInput.InputText.Replace("\\\"", string.Empty);
      if (String.IsNullOrWhiteSpace(searchString))
      {
        return true;
      }
      var searchQuery = getSearchQuery(searchString, feed);
      var feedResult = new RssFeed(null, searchQuery, 0, "Search Results");
      feedResult.Load(false);
      if (feedResult.FeedItems.Count > 0)
      {
        using (ArticleListView articleList = new ArticleListView(Config.Global.UI.Layout.ArticleList))
        {
          articleList.Show(feedResult);
        }
        view.Refresh();
      }
      feedResult.Dispose();
      feedResult = null;
      return true;
    }

    private static string getSearchQuery(string searchString, RssFeed feed = null)
    {
      var s = $"(Culture.CompareInfo.IndexOf(Summary, \"{searchString}\", IgnoreCase) >= 0";
      s = s + $" || Culture.CompareInfo.IndexOf(Title, \"{searchString}\", IgnoreCase) >= 0)";
      if (feed != null)
      {
        s = s + $" && FeedUrl == \"{ feed.FeedUrl }\"";
      }
      return s;
    }
  }
}
