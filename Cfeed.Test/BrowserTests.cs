using System;
using cFeed.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cfeed.Test
{
  [TestClass]
  public class BrowserTests
  {
    [DataTestMethod]
    [DataRow("http://example.com/a")]
    [DataRow("https://example.com/a?b=c#d")]
    [DataRow("HTTPS://EXAMPLE.COM/")]
    public void IsSafeToOpen_WebLinks_AreAllowed(string url)
    {
      Assert.IsTrue(Browser.IsSafeToOpen(new Uri(url)));
    }

    [DataTestMethod]
    [DataRow("file:///C:/Windows/System32/calc.exe")]
    [DataRow("file://server/share/evil.exe")]
    [DataRow("ms-msdt:/id PCWDiagnostic")]
    [DataRow("javascript:alert(1)")]
    [DataRow("mailto:someone@example.com")]
    [DataRow("ftp://example.com/file")]
    public void IsSafeToOpen_OtherSchemes_AreRefused(string url)
    {
      Assert.IsFalse(Browser.IsSafeToOpen(new Uri(url)));
    }

    [TestMethod]
    public void IsSafeToOpen_Null_IsRefused()
    {
      Assert.IsFalse(Browser.IsSafeToOpen(null));
    }

    [TestMethod]
    public void IsSafeToOpen_RelativeUri_IsRefused()
    {
      Assert.IsFalse(Browser.IsSafeToOpen(new Uri("/relative/path", UriKind.Relative)));
    }
  }
}
