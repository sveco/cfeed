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
      var invalidChars = Path.GetInvalidFileNameChars();

      var pathString = "Test" + new String(invalidChars) + ".txt";
      var sanitizedPath = pathString.SanitizeFileName();
      bool possiblePath = sanitizedPath.IndexOfAny(invalidChars) == -1;

      Assert.AreEqual<string>(sanitizedPath, "Test.txt");
      Assert.IsTrue(possiblePath);
    }

    [TestMethod]
    public void SanitizePathTest()
    {
      var invalidChars = Path.GetInvalidPathChars();

      var pathString = "c:\\Test" + new String(invalidChars) + "\\";
      var sanitizedPath = pathString.SanitizePath();
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
      var output = input.PadLeftVisible(10);

      Assert.AreEqual<string>(output, expected);
      Assert.AreNotEqual<string>(output, notexpected);
    }

    [TestMethod]
    public void TruncateVisible_ShortTextIsUnchanged()
    {
      Assert.AreEqual("Hello", "Hello".TruncateVisible(5));
      Assert.AreEqual("Hello", "Hello".TruncateVisible(50));
      Assert.AreEqual("", "".TruncateVisible(3));
      Assert.IsNull(((string)null).TruncateVisible(3));
    }

    [TestMethod]
    public void TruncateVisible_CutsLongText()
    {
      Assert.AreEqual("Hello", "Hello world".TruncateVisible(5));
      Assert.AreEqual("", "Hello".TruncateVisible(0));
    }

    [TestMethod]
    public void TruncateVisible_ColourTagsDoNotCountAndAreKept()
    {
      var text = "\x1b[f:Red]Hello\x1b[Reset] world";

      var result = text.TruncateVisible(7);

      // 7 visible characters, "Hello w", with both tags still there
      Assert.AreEqual("\x1b[f:Red]Hello\x1b[Reset] w", result);
    }

    [TestMethod]
    public void TruncateVisible_KeepsATrailingResetTag()
    {
      var text = "\x1b[f:Red]Hello world\x1b[Reset]";

      var result = text.TruncateVisible(5);

      Assert.AreEqual("\x1b[f:Red]Hello\x1b[Reset]", result);
    }

    [TestMethod]
    public void TruncateVisible_TextWithOnlyTagsIsUnchanged()
    {
      var text = "\x1b[f:Red]\x1b[Reset]";

      Assert.AreEqual(text, text.TruncateVisible(0));
    }

    [TestMethod]
    public void TruncateVisible_ArticleFooterAtANarrowWidth()
    {
      var footer = " ESC/Backspace:Back O:Open N:Next L:Link I:Image S:Download <:Prev >:Next [:Prev Unread ]:Next Unread ";

      var result = footer.TruncateVisible(69);

      Assert.AreEqual(69, result.Length);
      Assert.IsTrue(footer.StartsWith(result));
    }

    [TestMethod]
    public void PadRightVisibleTest()
    {
      var input = "\x1b[Test]Test";
      var expected = "\x1b[Test]Test      ";
      var notexpected = "\x1b[Test]Test          ";
      var output = input.PadRightVisible(10);

      Assert.AreEqual<string>(output, expected);
      Assert.AreNotEqual<string>(output, notexpected);
    }
  }
}
