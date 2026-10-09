using System;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;
using cFeed.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cfeed.Test
{
  /// <summary>
  /// Tests HtmlToText (Select, Filters, StripLinks) through FeedItem.ConvertContent.
  /// </summary>
  [TestClass]
  public class HtmlToTextTests
  {
    // Same structure as a BBC article: one main element, blocks marked with data-block, generated class names.
    private const string BbcLikePage =
      "<html><body>" +
      "<nav><a href=\"/\">Home</a><a href=\"/news\">News</a></nav>" +
      "<main id=\"main-content\"><article>" +
      "<div class=\"ssrcss-aaa\" data-block=\"headline\"><h1>The headline</h1></div>" +
      "<div class=\"ssrcss-bbb\" data-block=\"image\"><img src=\"https://img.example.com/a.jpg\" alt=\"A photo\"/>Image caption, A caption</div>" +
      "<div class=\"ssrcss-aaa\" data-block=\"byline\">By A Reporter</div>" +
      "<div class=\"ssrcss-aaa\" data-block=\"text\"><p>First paragraph of the story.</p></div>" +
      "<div class=\"ssrcss-aaa\" data-block=\"media\">To play this video you need to enable JavaScript</div>" +
      "<div class=\"ssrcss-aaa\" data-block=\"text\"><p>Second paragraph, see <a href=\"https://news.example.com/other\">the other story</a> for more.</p></div>" +
      "<div class=\"ssrcss-aaa\" data-block=\"topicList\">Related topics<ul><li><a href=\"/t/1\">Topic one</a></li></ul></div>" +
      "<div class=\"ssrcss-aaa\" data-block=\"promoList\">More on this story<a href=\"/s/2\">Another story</a></div>" +
      "</article></main>" +
      "<footer>Site footer</footer>" +
      "</body></html>";

    private static readonly string[] BbcFilters =
    {
      "//*[@data-block='headline']", "//*[@data-block='image']", "//*[@data-block='media']",
      "//*[@data-block='topicList']", "//*[@data-block='promoList']"
    };

    private static FeedItem Convert(string html, string select, string[] filters, bool stripLinks = false)
    {
      var item = new SyndicationItem { Id = "x", Title = new TextSyndicationContent("t") };
      item.Links.Add(SyndicationLink.CreateAlternateLink(new Uri("https://www.example.com/article")));
      var feedItem = new FeedItem(new Uri("http://feed.test/rss"), item);
      feedItem.ConvertContent(html, item.Links[0].Uri, select, filters, stripLinks);
      return feedItem;
    }

    /// <summary>Article text without the colour markers the UI library understands.</summary>
    private static string Text(FeedItem item)
    {
      return Regex.Replace(item.ArticleContent, @"\x1b\[[^\]]*\]", "");
    }

    [TestMethod]
    public void XPathFilters_RemoveMatchingBlocks_AndKeepTheStory()
    {
      var item = Convert(BbcLikePage, "//main[@id='main-content']", BbcFilters);
      var text = Text(item);

      StringAssert.Contains(text, "First paragraph of the story.");
      StringAssert.Contains(text, "Second paragraph");
      StringAssert.Contains(text, "By A Reporter");
      foreach (var removed in new[] { "The headline", "Image caption", "To play this video", "Related topics", "Topic one", "More on this story", "Another story" })
      {
        Assert.IsFalse(text.Contains(removed), "should have been filtered out: " + removed);
      }
    }

    [TestMethod]
    public void Select_WithoutFilters_LimitsOutputToTheSelectedNode()
    {
      var text = Text(Convert(BbcLikePage, "//main[@id='main-content']", null));

      StringAssert.Contains(text, "First paragraph of the story.");
      Assert.IsFalse(text.Contains("Site footer"));
      Assert.IsFalse(text.Contains("Home"));
    }

    [TestMethod]
    public void XPathFilters_WorkWithoutSelect()
    {
      var text = Text(Convert(BbcLikePage, null, new[] { "//nav", "//footer" }));

      StringAssert.Contains(text, "First paragraph of the story.");
      Assert.IsFalse(text.Contains("Site footer"));
      Assert.IsFalse(text.Contains("News"));
    }

    [TestMethod]
    public void InvalidXPathFilter_IsIgnored_AndOthersStillApply()
    {
      var text = Text(Convert(BbcLikePage, "//main[@id='main-content']", new[] { "//*[@data-block=", "//*[@data-block='promoList']" }));

      StringAssert.Contains(text, "First paragraph of the story.");
      Assert.IsFalse(text.Contains("More on this story"));
    }

    [TestMethod]
    public void IdAndClassFilters_StillWork()
    {
      var html = "<div id=\"nav\">NAVIGATION</div><div class=\"ad\">ADVERT</div><p>Body</p>";

      var text = Text(Convert(html, null, new[] { "#nav", ".ad" }));

      StringAssert.Contains(text, "Body");
      Assert.IsFalse(text.Contains("NAVIGATION"));
      Assert.IsFalse(text.Contains("ADVERT"));
    }

    [TestMethod]
    public void Links_AreMarkedAndCollected_ByDefault()
    {
      var item = Convert("<p>See <a href=\"https://one.example.com/\">the report</a> now</p>", null, null);

      StringAssert.Contains(item.ArticleContent, "[Link:the report]");
      CollectionAssert.AreEqual(new[] { new Uri("https://one.example.com/") }, item.ExternalLinks.ToList());
    }

    [TestMethod]
    public void StripLinks_KeepsTheLinkText_WithoutMarkersOrNumbers()
    {
      var item = Convert("<p>See <a href=\"https://one.example.com/\">the report</a> now.</p>", null, null, stripLinks: true);
      var text = Text(item);

      StringAssert.Contains(text, "See the report now.");
      Assert.IsFalse(item.ArticleContent.Contains("[Link:"));
      Assert.AreEqual(0, item.ExternalLinks.Count);
    }

    [TestMethod]
    public void StripLinks_LinkAtTheStartOrEnd_KeepsSurroundingText()
    {
      var text = Text(Convert("<p><a href=\"/a\">First</a> middle <a href=\"/b\">last</a></p>", null, null, stripLinks: true));

      StringAssert.Contains(text, "First middle last");
    }

    [TestMethod]
    public void StripLinks_OnBbcLikePage_LeavesNoLinkMarkers()
    {
      var item = Convert(BbcLikePage, "//main[@id='main-content']", BbcFilters, stripLinks: true);

      StringAssert.Contains(Text(item), "see the other story for more.");
      Assert.IsFalse(item.ArticleContent.Contains("[Link:"));
      Assert.AreEqual(0, item.ExternalLinks.Count);
    }
  }
}
