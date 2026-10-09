namespace cFeed.Util
{
  /// <summary>
  /// Keeps scroll positions valid when the number of visible rows changes, for example after the console
  /// was resized.
  /// </summary>
  public static class ScrollMath
  {
    /// <summary>
    /// Adjusts a list so that the selected item is inside the visible rows and no empty rows are left at the
    /// bottom while there are items above the first visible one.
    /// </summary>
    /// <param name="total">Number of items in the list</param>
    /// <param name="visibleRows">Number of rows the list can show</param>
    /// <param name="offset">Index of the first visible item</param>
    /// <param name="selectedIndex">Index of the selected item</param>
    /// <param name="selectionPosition">Row of the selected item inside the visible rows</param>
    public static void KeepSelectionVisible(int total, int visibleRows, ref int offset, ref int selectedIndex, out int selectionPosition)
    {
      if (total <= 0)
      {
        offset = 0;
        selectedIndex = 0;
        selectionPosition = 0;
        return;
      }

      if (visibleRows < 1)
      {
        visibleRows = 1;
      }

      selectedIndex = Clamp(selectedIndex, 0, total - 1);

      if (selectedIndex < offset)
      {
        offset = selectedIndex;
      }
      else if (selectedIndex > offset + visibleRows - 1)
      {
        offset = selectedIndex - visibleRows + 1;
      }

      offset = ClampOffset(offset, total, visibleRows);
      selectionPosition = selectedIndex - offset;
    }

    /// <summary>
    /// Largest valid offset is the one that shows the last item in the last visible row.
    /// </summary>
    public static int ClampOffset(int offset, int total, int visibleRows)
    {
      if (visibleRows < 1)
      {
        visibleRows = 1;
      }
      return Clamp(offset, 0, total > visibleRows ? total - visibleRows : 0);
    }

    private static int Clamp(int value, int min, int max)
    {
      return value < min ? min : (value > max ? max : value);
    }
  }
}
