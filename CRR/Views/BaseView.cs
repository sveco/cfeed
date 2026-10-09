namespace cFeed.Views
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using cFeed.Logging;
  using cFeed.Util;
  using CGui.Gui;
  using CGui.Gui.Primitives;
  using JsonConfig;

  public class BaseView : IDisposable
  {
    internal NLog.Logger logger = Log.Instance.Logger;
    internal Viewport _mainView;

    /// <summary>
    /// Views that are showing, the last one is on top. Views are nested: feeds, articles of a feed, one article.
    /// </summary>
    private static readonly List<BaseView> ActiveViews = new List<BaseView>();

    private readonly object _layoutLock = new object();

    /// <summary>
    /// A width or height configured relative to the console (negative, -3 is the console size minus 3) is turned
    /// into a fixed number when it is set. Text areas wrap their text at that width too. After a resize the
    /// configured values are set again.
    /// </summary>
    private readonly List<RelativeSize> _relativeSizes = new List<RelativeSize>();

    private sealed class RelativeSize
    {
      public GuiElement Element;
      public System.Reflection.PropertyInfo Property;
      public int Value;
    }

    private string _headerText;
    private string _footerText;
    private int _laidOutWidth;
    private int _laidOutHeight;

    public BaseView (ConfigObject layout)
    {
      var viewport = new LayoutViewport();
      _mainView = viewport;

      // The configured size is the size of the console window when the application starts. Views that are
      // opened later keep the window as it is, the user may have resized it.
      bool firstView;
      lock (ActiveViews)
      {
        firstView = ActiveViews.Count == 0;
      }
      if (firstView)
      {
        try
        {
          viewport.ApplyWindowSize((int)layout["Width"], (int)layout["Height"]);
        }
        catch (Exception ex)
        {
          // the terminal does not allow its size to be set, the application works with the size it has
          logger.Warn("Could not set the console size from the layout: " + ex.Message);
        }
      }

      foreach (var control in (IEnumerable<dynamic>)layout["Controls"])
      {
        var guiElement = ControlFactory.Get(control);
        if (guiElement != null)
        {
          _mainView.Controls.Add(guiElement);

          foreach (var name in new[] { "Width", "Height" })
          {
            // the indexer throws for a setting that is not in the layout
            object configured = ((ConfigObject)control).ContainsKey(name) ? ((ConfigObject)control)[name] : null;
            if ((configured is int || configured is long) && Convert.ToInt32(configured) < 0)
            {
              var property = guiElement.GetType().GetProperty(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
              if (property != null && property.CanWrite)
              {
                _relativeSizes.Add(new RelativeSize { Element = guiElement, Property = property, Value = Convert.ToInt32(configured) });
              }
            }
          }
        }
      }

      _laidOutWidth = ConsoleWidth();
      _laidOutHeight = ConsoleHeight();
    }

    internal void ShowHeader(string displayText)
    {
      _headerText = displayText;
      if (_mainView.Controls.FirstOrDefault(x => x.GetType() == typeof(Header)) is Header header)
      {
        header.DisplayText = FitToWidth(displayText);
      }
    }

    internal void ShowFooter(string displayText)
    {
      _footerText = displayText;
      if (_mainView.Controls.FirstOrDefault(x => x.GetType() == typeof(Footer)) is Footer footer)
      {
        footer.DisplayText = FitToWidth(displayText);
      }
    }

    /// <summary>
    /// A row pads its text to the width of the console but does not shorten it. Text that is longer wraps
    /// to the next line, which scrolls the screen when it is the last line.
    /// </summary>
    private static string FitToWidth(string text)
    {
      int width = ConsoleWidth();
      return width > 1 ? text.TruncateVisible(width - 1) : text;
    }

    /// <summary>
    /// Shortens the header and footer again for the current width, they keep the full text.
    /// </summary>
    private void FitRows()
    {
      if (_headerText != null && _mainView.Controls.FirstOrDefault(x => x.GetType() == typeof(Header)) is Header header)
      {
        header.DisplayText = FitToWidth(_headerText);
      }
      if (_footerText != null && _mainView.Controls.FirstOrDefault(x => x.GetType() == typeof(Footer)) is Footer footer)
      {
        footer.DisplayText = FitToWidth(_footerText);
      }
    }

    /// <summary>
    /// Registers the view as the one that is on screen. Dispose the result when the view closes, around the
    /// call that blocks until the user leaves the view.
    /// </summary>
    internal IDisposable Activate()
    {
      lock (ActiveViews)
      {
        ActiveViews.Add(this);
      }
      ApplyLayoutIfNeeded();
      return new ActiveScope(this);
    }

    /// <summary>
    /// Redraws the view that is on top. Called when the console was resized.
    /// </summary>
    internal static void ResizeActive()
    {
      BaseView top;
      lock (ActiveViews)
      {
        top = ActiveViews.Count > 0 ? ActiveViews[ActiveViews.Count - 1] : null;
      }
      top?.OnResize();
    }

    /// <summary>
    /// Fits the view to the current console size and draws it again.
    /// </summary>
    internal virtual void OnResize()
    {
      lock (_layoutLock)
      {
        if (_mainView == null)
        {
          return;
        }

        _laidOutWidth = ConsoleWidth();
        _laidOutHeight = ConsoleHeight();

        foreach (var item in _relativeSizes)
        {
          item.Property.SetValue(item.Element, item.Value, null);
        }

        FitRows();

        foreach (var control in _mainView.Controls.ToList())
        {
          KeepScrollPositionValid(control);
        }

        _mainView.Refresh();
      }
    }

    /// <summary>
    /// A view that was covered by another one did not get the resize, catch up when it is on top again.
    /// </summary>
    private void ApplyLayoutIfNeeded()
    {
      if (_mainView != null && (ConsoleWidth() != _laidOutWidth || ConsoleHeight() != _laidOutHeight))
      {
        OnResize();
      }
    }

    private static void KeepScrollPositionValid(CGui.Gui.Primitives.GuiElement control)
    {
      var type = control.GetType();
      if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Picklist<>))
      {
        // Picklist<T> for the different item types, only reachable through dynamic
        dynamic list = control;
        int offset = list.Offset;
        int selectedIndex = list.SelectedItemIndex;
        int selectionPosition;
        // Picklist.ListHeight is protected, this is how it is calculated
        int visibleRows = list.Height - list.BorderWidth * 2;
        ScrollMath.KeepSelectionVisible(list.TotalItems, visibleRows, ref offset, ref selectedIndex, out selectionPosition);
        list.Offset = offset;
        list.SelectedItemIndex = selectedIndex;
        list.SelectionPosition = selectionPosition;
      }
      else if (control is TextArea textArea)
      {
        textArea.Offset = ScrollMath.ClampOffset(textArea.Offset, textArea.TotalItems, textArea.Height - textArea.BorderWidth * 2);
      }
    }

    private static int ConsoleWidth()
    {
      try { return Console.WindowWidth; } catch (System.IO.IOException) { return 0; }
    }

    private static int ConsoleHeight()
    {
      try { return Console.WindowHeight; } catch (System.IO.IOException) { return 0; }
    }

    private sealed class ActiveScope : IDisposable
    {
      private BaseView _view;

      public ActiveScope(BaseView view)
      {
        _view = view;
      }

      public void Dispose()
      {
        var view = System.Threading.Interlocked.Exchange(ref _view, null);
        if (view == null)
        {
          return;
        }

        BaseView top;
        lock (ActiveViews)
        {
          ActiveViews.Remove(view);
          top = ActiveViews.Count > 0 ? ActiveViews[ActiveViews.Count - 1] : null;
        }
        top?.ApplyLayoutIfNeeded();
      }
    }

    private bool disposedValue; // To detect redundant calls

    // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    ~BaseView()
    {
      // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
      Dispose(false);
    }

    // This code added to correctly implement the disposable pattern.
    public void Dispose()
    {
      // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
      Dispose(true);
      // TODO: uncomment the following line if the finalizer is overridden above.
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (disposedValue)
        return;

      if (disposing)
      {
        if (_mainView != null)
        {
          _mainView.Dispose();
          _mainView = null;
        }
      }

      disposedValue = true;
    }
  }
}
