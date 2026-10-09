using cFeed.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cfeed.Test
{
  [TestClass]
  public class ScrollMathTests
  {
    private static void Keep(int total, int rows, int offset, int selected, out int newOffset, out int newSelected, out int position)
    {
      newOffset = offset;
      newSelected = selected;
      ScrollMath.KeepSelectionVisible(total, rows, ref newOffset, ref newSelected, out position);
    }

    [TestMethod]
    public void SelectionStillVisible_IsLeftAlone()
    {
      Keep(100, 30, 10, 15, out int offset, out int selected, out int position);

      Assert.AreEqual(10, offset);
      Assert.AreEqual(15, selected);
      Assert.AreEqual(5, position);
    }

    [TestMethod]
    public void WindowGetsSmaller_SelectionBelowTheNewBottom_ScrollsDown()
    {
      // 30 rows shown, item 35 selected on row 25. Now only 10 rows fit.
      Keep(100, 10, 10, 35, out int offset, out int selected, out int position);

      Assert.AreEqual(26, offset);
      Assert.AreEqual(35, selected);
      Assert.AreEqual(9, position, "selected item is on the last row");
    }

    [TestMethod]
    public void WindowGetsBigger_NoEmptyRowsAtTheBottom()
    {
      // 10 rows shown from item 90 of 100. Now 30 rows fit, so the view moves up to fill them.
      Keep(100, 30, 90, 95, out int offset, out int selected, out int position);

      Assert.AreEqual(70, offset);
      Assert.AreEqual(95, selected);
      Assert.AreEqual(25, position);
    }

    [TestMethod]
    public void ListShorterThanWindow_StartsAtTheTop()
    {
      Keep(5, 30, 3, 4, out int offset, out int selected, out int position);

      Assert.AreEqual(0, offset);
      Assert.AreEqual(4, selected);
      Assert.AreEqual(4, position);
    }

    [TestMethod]
    public void EmptyList_ResetsEverything()
    {
      Keep(0, 20, 7, 7, out int offset, out int selected, out int position);

      Assert.AreEqual(0, offset);
      Assert.AreEqual(0, selected);
      Assert.AreEqual(0, position);
    }

    [TestMethod]
    public void SelectionBeyondTheEnd_IsBroughtBack()
    {
      Keep(10, 5, 0, 50, out int offset, out int selected, out int position);

      Assert.AreEqual(9, selected);
      Assert.AreEqual(5, offset);
      Assert.AreEqual(4, position);
    }

    [TestMethod]
    public void ZeroRows_IsTreatedAsOne()
    {
      Keep(10, 0, 0, 3, out int offset, out int selected, out int position);

      Assert.AreEqual(3, offset);
      Assert.AreEqual(3, selected);
      Assert.AreEqual(0, position);
    }

    [TestMethod]
    public void EveryCombination_KeepsTheSelectionInsideTheVisibleRows()
    {
      for (int total = 0; total <= 25; total++)
      {
        for (int rows = 1; rows <= 30; rows++)
        {
          for (int offset = 0; offset <= total; offset++)
          {
            for (int selected = 0; selected <= total; selected++)
            {
              Keep(total, rows, offset, selected, out int newOffset, out int newSelected, out int position);

              string where = string.Format("total={0} rows={1} offset={2} selected={3}", total, rows, offset, selected);
              if (total == 0)
              {
                Assert.AreEqual(0, position, where);
                continue;
              }

              Assert.IsTrue(newSelected >= 0 && newSelected < total, "selected in range, " + where);
              Assert.IsTrue(position >= 0 && position < rows, "position inside the rows, " + where);
              Assert.AreEqual(newSelected, newOffset + position, "index is offset plus row, " + where);
              Assert.IsTrue(newOffset >= 0 && newOffset <= System.Math.Max(0, total - rows), "offset in range, " + where);
            }
          }
        }
      }
    }

    [TestMethod]
    public void ClampOffset_LimitsToWhatStillFillsTheWindow()
    {
      Assert.AreEqual(0, ScrollMath.ClampOffset(5, 10, 20), "everything fits");
      Assert.AreEqual(70, ScrollMath.ClampOffset(99, 100, 30), "last page");
      Assert.AreEqual(12, ScrollMath.ClampOffset(12, 100, 30), "valid offset is kept");
      Assert.AreEqual(0, ScrollMath.ClampOffset(-4, 100, 30));
      Assert.AreEqual(9, ScrollMath.ClampOffset(50, 10, 0), "zero rows counts as one");
    }
  }
}
