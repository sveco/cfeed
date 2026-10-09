using System;
using System.Linq;
using System.ServiceModel.Syndication;
using cFeed.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cfeed.Test
{
  [TestClass]
  public class FeedItemTests
  {
    private static readonly Uri FeedUrl = new Uri("http://feed.test/rss");

    [TestMethod]
    public void Constructor_ItemWithIdButNoLinks_DoesNotThrow()
    {
      var item = new SyndicationItem { Id = "item-1", Title = new TextSyndicationContent("Hello") };

      var feedItem = new FeedItem(FeedUrl, item);

      Assert.AreEqual("item-1", feedItem.SyndicationItemId);
      Assert.AreEqual("Hello", feedItem.Title);
    }

    [TestMethod]
    public void Constructor_ItemWithoutIdUsesFirstLink()
    {
      var item = new SyndicationItem { Title = new TextSyndicationContent("Hello") };
      item.Links.Add(SyndicationLink.CreateAlternateLink(new Uri("http://site.test/post/1")));

      var feedItem = new FeedItem(FeedUrl, item);

      Assert.AreEqual("http://site.test/post/1", feedItem.SyndicationItemId);
    }

    [TestMethod]
    public void Constructor_ItemWithoutIdOrLinks_DoesNotThrow()
    {
      var item = new SyndicationItem { Title = new TextSyndicationContent("Hello") };

      var feedItem = new FeedItem(FeedUrl, item);

      Assert.IsNull(feedItem.SyndicationItemId);
    }

    // Shape of a reddit link post as published in the Atom content element.
    private const string RedditContent =
      "<table><tr><td><div class=\"md\"><p>Body of the post</p></div></td></tr>" +
      "<tr><td>submitted by <a href=\"https://www.reddit.com/user/someone\">/u/someone</a> " +
      "<span><a href=\"https://news.example.com/story/1\">[link]</a></span> " +
      "<span><a href=\"https://www.reddit.com/r/news/comments/abc/post/\">[comments]</a></span></td></tr></table>";

    [TestMethod]
    public void GetFeedContent_ReturnsEntryContent()
    {
      var item = new SyndicationItem { Id = "1", Content = new TextSyndicationContent(RedditContent, TextSyndicationContentKind.Html) };

      Assert.AreEqual(RedditContent, FeedItem.GetFeedContent(item));
    }

    [TestMethod]
    public void GetFeedContent_NoContent_ReturnsNull()
    {
      Assert.IsNull(FeedItem.GetFeedContent(new SyndicationItem { Id = "1" }));
      Assert.IsNull(FeedItem.GetFeedContent(null));
    }

    [TestMethod]
    public void ConvertContent_BuildsTextAndCollectsLinks()
    {
      var item = new SyndicationItem { Id = "1", Title = new TextSyndicationContent("Post") };
      item.Links.Add(SyndicationLink.CreateAlternateLink(new Uri("https://www.reddit.com/r/news/comments/abc/post/")));
      var feedItem = new FeedItem(FeedUrl, item);

      feedItem.ConvertContent(RedditContent, item.Links[0].Uri, null, null);

      Assert.IsTrue(feedItem.IsLoaded);
      StringAssert.Contains(feedItem.ArticleContent, "Body of the post");
      CollectionAssert.Contains(feedItem.ExternalLinks.ToList(), new Uri("https://news.example.com/story/1"));
    }

    [TestMethod]
    public void ConvertContent_NumbersLinksAfterTheItemsOwnLinks()
    {
      var item = new SyndicationItem { Id = "1", Title = new TextSyndicationContent("Post") };
      item.Links.Add(SyndicationLink.CreateAlternateLink(new Uri("https://www.reddit.com/r/news/comments/abc/post/")));
      var feedItem = new FeedItem(FeedUrl, item);

      feedItem.ConvertContent("<a href=\"https://one.example.com/\">one</a>", item.Links[0].Uri, null, null);

      // The item has one link of its own, so the first link in the text is number 2 (see OpenLink).
      StringAssert.Contains(feedItem.ArticleContent, "[2]");
    }

    [TestMethod]
    public void ConvertContent_ItemWithoutLinks_DoesNotThrow()
    {
      var feedItem = new FeedItem(FeedUrl, new SyndicationItem { Id = "1", Title = new TextSyndicationContent("Post") });

      feedItem.ConvertContent("<p>Hello</p>", FeedUrl, null, null);

      StringAssert.Contains(feedItem.ArticleContent, "Hello");
    }

    [TestMethod]
    public void Constructor_ItemWithoutTitle_DoesNotThrow()
    {
      var item = new SyndicationItem { Id = "item-2" };

      var feedItem = new FeedItem(FeedUrl, item);

      Assert.AreEqual(string.Empty, feedItem.Title);
    }
  }
}
