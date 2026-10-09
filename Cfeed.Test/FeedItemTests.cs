using System;
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

    [TestMethod]
    public void Constructor_ItemWithoutTitle_DoesNotThrow()
    {
      var item = new SyndicationItem { Id = "item-2" };

      var feedItem = new FeedItem(FeedUrl, item);

      Assert.AreEqual(string.Empty, feedItem.Title);
    }
  }
}
