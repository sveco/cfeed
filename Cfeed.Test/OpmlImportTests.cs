using System.IO;
using System.Linq;
using cFeed.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cfeed.Test
{
  [TestClass]
  public class OpmlImportTests
  {
    private static string WriteOpml(string body)
    {
      var path = Path.Combine(Path.GetTempPath(), "cfeed-test-" + System.Guid.NewGuid().ToString("N") + ".opml");
      File.WriteAllText(path, "<?xml version=\"1.0\"?><opml version=\"1.0\"><body>" + body + "</body></opml>");
      return path;
    }

    private static System.Collections.Generic.IList<cFeed.Entities.Outline> Import(string body)
    {
      var path = WriteOpml(body);
      try { return OpmlImport.Import(path); }
      finally { File.Delete(path); }
    }

    [TestMethod]
    public void Import_UsesTitle_WhenPresent()
    {
      var result = Import("<outline type=\"rss\" title=\"My Title\" text=\"Other\" xmlUrl=\"http://a.test/feed\"/>");

      Assert.AreEqual(1, result.Count);
      Assert.AreEqual("My Title", result[0].Title);
      Assert.AreEqual("http://a.test/feed", result[0].FeedUrl);
    }

    [TestMethod]
    public void Import_FallsBackToText_WhenTitleMissing()
    {
      var result = Import("<outline type=\"rss\" text=\"Text Only\" xmlUrl=\"http://a.test/feed\"/>");

      Assert.AreEqual(1, result.Count);
      Assert.AreEqual("Text Only", result[0].Title);
    }

    [TestMethod]
    public void Import_FallsBackToUrl_WhenNoTitleOrText()
    {
      var result = Import("<outline type=\"rss\" xmlUrl=\"http://a.test/feed\"/>");

      Assert.AreEqual(1, result.Count);
      Assert.AreEqual("http://a.test/feed", result[0].Title);
    }

    [TestMethod]
    public void Import_AcceptsAtomAndMissingType()
    {
      var result = Import("<outline type=\"atom\" title=\"A\" xmlUrl=\"http://a.test/atom\"/>" +
                          "<outline title=\"B\" xmlUrl=\"http://b.test/feed\"/>");

      Assert.AreEqual(2, result.Count);
    }

    [TestMethod]
    public void Import_SkipsFoldersAndOtherTypes()
    {
      var result = Import("<outline title=\"Folder\"><outline type=\"rss\" title=\"In\" xmlUrl=\"http://a.test/feed\"/></outline>" +
                          "<outline type=\"link\" title=\"Not a feed\" xmlUrl=\"http://c.test/\"/>");

      Assert.AreEqual(1, result.Count);
      Assert.AreEqual("In", result[0].Title);
    }

    [TestMethod]
    public void Import_UsesFolderTitleAsTag()
    {
      var result = Import("<outline title=\"News\"><outline type=\"rss\" title=\"In\" xmlUrl=\"http://a.test/feed\"/></outline>" +
                          "<outline type=\"rss\" title=\"Top\" xmlUrl=\"http://b.test/feed\"/>");

      Assert.AreEqual(2, result.Count);
      CollectionAssert.AreEqual(new[] { "News" }, result.First(r => r.Title == "In").Tags);
      Assert.IsNull(result.First(r => r.Title == "Top").Tags ?? null,
                    "Top level outline lives under <body>, which has no title, so it should have no tag.");
    }
  }
}
