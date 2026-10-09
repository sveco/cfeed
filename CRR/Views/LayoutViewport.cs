namespace cFeed.Views
{
  using System;
  using CGui.Gui;

  /// <summary>
  /// CGui's Viewport resizes the console window and its buffer to the width and height it is given, and its
  /// constructor does it too. Every view created that way would put the window back to the size from the
  /// layout (120x40), also for a user who resized it. This viewport only remembers the size it is given.
  /// The console is resized with <see cref="ApplyWindowSize"/>, which is used once at startup.
  /// </summary>
  internal sealed class LayoutViewport : Viewport
  {
    private int _width;
    private int _height;

    public override int Width
    {
      get { return _width > 0 ? _width : Console.WindowWidth; }
      set { _width = value; }
    }

    public override int Height
    {
      get { return _height > 0 ? _height : Console.WindowHeight; }
      set { _height = value; }
    }

    /// <summary>
    /// Sets the size of the console window and its buffer, like CGui's Viewport does when it is given a size.
    /// </summary>
    public void ApplyWindowSize(int width, int height)
    {
      base.Width = width;
      _width = width;
      base.Height = height;
      _height = height;
    }
  }
}
