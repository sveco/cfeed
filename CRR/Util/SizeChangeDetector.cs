namespace cFeed.Util
{
  /// <summary>
  /// Decides when a console resize is finished. Dragging a window edge changes the size many times a second,
  /// so a new size is reported only after it was seen unchanged on a few polls in a row.
  /// </summary>
  public sealed class SizeChangeDetector
  {
    private readonly int _stablePolls;
    private int _appliedWidth;
    private int _appliedHeight;
    private int _candidateWidth;
    private int _candidateHeight;
    private int _stableCount;

    /// <param name="width">Size the screen is currently drawn for</param>
    /// <param name="height">Size the screen is currently drawn for</param>
    /// <param name="stablePolls">How many polls in a row must see the same new size before it is reported</param>
    public SizeChangeDetector(int width, int height, int stablePolls = 2)
    {
      _stablePolls = stablePolls < 1 ? 1 : stablePolls;
      _appliedWidth = _candidateWidth = width;
      _appliedHeight = _candidateHeight = height;
    }

    /// <summary>
    /// Call on every poll with the current size.
    /// Returns true once, when a size different from the last reported one has settled.
    /// </summary>
    public bool Poll(int width, int height)
    {
      if (width == _appliedWidth && height == _appliedHeight)
      {
        _candidateWidth = width;
        _candidateHeight = height;
        _stableCount = 0;
        return false;
      }

      if (width == _candidateWidth && height == _candidateHeight)
      {
        _stableCount++;
      }
      else
      {
        _candidateWidth = width;
        _candidateHeight = height;
        _stableCount = 1;
      }

      if (_stableCount < _stablePolls)
      {
        return false;
      }

      _appliedWidth = width;
      _appliedHeight = height;
      _stableCount = 0;
      return true;
    }

    /// <summary>
    /// Forget the size the screen is drawn for, so the next settled size is reported even if it is the
    /// same as before. Used when a redraw was skipped, the screen can show anything.
    /// </summary>
    public void Invalidate()
    {
      _appliedWidth = -1;
      _appliedHeight = -1;
      _stableCount = 0;
    }
  }
}
