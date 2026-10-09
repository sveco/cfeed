using System.Text;
using cFeed.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cfeed.Test
{
  [TestClass]
  public class HttpDownloaderTests
  {
    private static HttpDownloader NewDownloader()
    {
      return new HttpDownloader("http://example.com/", null, null);
    }

    private static string Decode(byte[] data, string contentType)
    {
      return NewDownloader().DecodeContent(data, contentType);
    }

    [TestMethod]
    public void NoCharsetInHeader_MetaCharSetWithQuotes_IsUsed()
    {
      // This is how bbc.co.uk serves its pages: no charset in the header, <meta charSet="utf-8" /> in the page.
      var html = "<html><head><meta charSet=\"utf-8\" /></head><body>Jørgen and María</body></html>";

      var result = Decode(Encoding.UTF8.GetBytes(html), "text/html");

      StringAssert.Contains(result, "Jørgen and María");
    }

    [TestMethod]
    public void NoCharsetInHeader_MetaCharSetWithoutQuotes_IsUsed()
    {
      var html = "<html><head><meta charset=utf-8></head><body>Jørgen</body></html>";

      StringAssert.Contains(Decode(Encoding.UTF8.GetBytes(html), "text/html"), "Jørgen");
    }

    [TestMethod]
    public void NoCharsetInHeader_HttpEquivMeta_IsUsed()
    {
      var html = "<html><head><meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\"></head><body>Jørgen</body></html>";

      StringAssert.Contains(Decode(Encoding.UTF8.GetBytes(html), "text/html"), "Jørgen");
    }

    [TestMethod]
    public void CharsetInHeader_IsUsed()
    {
      var html = "<html><body>Jørgen</body></html>";

      var result = Decode(Encoding.UTF8.GetBytes(html), "text/html; charset=UTF-8");

      StringAssert.Contains(result, "Jørgen");
    }

    [TestMethod]
    public void QuotedCharsetInHeader_IsUsed()
    {
      var html = "<html><body>Jørgen</body></html>";

      StringAssert.Contains(Decode(Encoding.UTF8.GetBytes(html), "text/html; charset=\"utf-8\""), "Jørgen");
    }

    [TestMethod]
    public void LatinPageWithLatinHeader_IsDecodedAsLatin()
    {
      var html = "<html><body>café</body></html>";

      var result = Decode(Encoding.GetEncoding("ISO-8859-1").GetBytes(html), "text/html; charset=ISO-8859-1");

      StringAssert.Contains(result, "café");
    }

    [TestMethod]
    public void MetaCharSet_IsSearchedInsideTheTagOnly()
    {
      // A later "charset=" in a script must not be taken for the page encoding.
      var html = "<html><head><meta name=\"viewport\" content=\"width=device-width\"></head>" +
                 "<body>café<script>var s = 'charset=windows-1250';</script></body></html>";

      var result = Decode(Encoding.GetEncoding("ISO-8859-1").GetBytes(html), "text/html");

      StringAssert.Contains(result, "café");
    }

    [TestMethod]
    public void NothingDeclared_FallsBackToLatin1()
    {
      var result = Decode(new byte[] { 0x63, 0x61, 0x66, 0xE9 }, "text/html");

      Assert.AreEqual("café", result);
    }

    [TestMethod]
    public void UnknownCharset_IsIgnored()
    {
      var html = "<html><body>plain ascii</body></html>";

      StringAssert.Contains(Decode(Encoding.ASCII.GetBytes(html), "text/html; charset=no-such-charset"), "plain ascii");
    }
  }
}
