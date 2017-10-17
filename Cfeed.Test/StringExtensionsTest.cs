using System;
using System.IO;
using cFeed;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cfeed.Test
{
  [TestClass]
  public class StringExtensionTest
  {
    [TestMethod]
    public void SanitizeFileNameTest()
    {
      var invalidChars = new String(Path.GetInvalidPathChars());

      var pathString = "Test" + invalidChars + ".txt";
      var sanitizedPath = StringExtensions.SanitizeFileName(pathString);
      bool possiblePath = sanitizedPath.IndexOfAny(Path.GetInvalidPathChars()) == -1;

      Assert.AreEqual<string>(sanitizedPath, "Test.txt");
      Assert.IsTrue(possiblePath);
    }
  }
}
