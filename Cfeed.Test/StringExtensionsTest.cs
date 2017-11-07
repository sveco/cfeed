﻿using System;
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
      var invalidChars = Path.GetInvalidFileNameChars();

      var pathString = "Test" + new String(invalidChars) + ".txt";
      var sanitizedPath = StringExtensions.SanitizeFileName(pathString);
      bool possiblePath = sanitizedPath.IndexOfAny(invalidChars) == -1;

      Assert.AreEqual<string>(sanitizedPath, "Test.txt");
      Assert.IsTrue(possiblePath);
    }

    [TestMethod]
    public void SanitizePathTest()
    {
      var invalidChars = Path.GetInvalidPathChars();

      var pathString = "c:\\Test" + new String(invalidChars) + "\\";
      var sanitizedPath = StringExtensions.SanitizePath(pathString);
      bool possiblePath = sanitizedPath.IndexOfAny(Path.GetInvalidPathChars()) == -1;

      Assert.AreEqual<string>(sanitizedPath, "c:\\Test\\");
      Assert.IsTrue(possiblePath);
    }

    [TestMethod]
    public void PadLeftVisibleTest()
    {
      var input = "\x1b[Test]Test";
      var expected = "      \x1b[Test]Test";
      var notexpected = "          \x1b[Test]Test";
      var output = StringExtensions.PadLeftVisible(input, 10);

      Assert.AreEqual<string>(output, expected);
      Assert.AreNotEqual<string>(output, notexpected);
    }

    [TestMethod]
    public void PadRightVisibleTest()
    {
      var input = "\x1b[Test]Test";
      var expected = "\x1b[Test]Test      ";
      var notexpected = "\x1b[Test]Test          ";
      var output = StringExtensions.PadRightVisible(input, 10);

      Assert.AreEqual<string>(output, expected);
      Assert.AreNotEqual<string>(output, notexpected);
    }
  }
}
