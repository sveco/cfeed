namespace cFeed.Views
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using cFeed.Logging;
  using cFeed.Util;
  using CGui.Gui;
  using JsonConfig;

  public class BaseView : IDisposable
  {
    internal NLog.Logger logger = Log.Instance.Logger;
    internal Viewport _mainView;

    /// <summary>
    /// The Width and Height of the layout are the size of the console window when the application starts.
    /// Views that are opened later keep the window as it is, the user may have resized it. Setting the size
    /// of a viewport changes the size of the console window.
    /// </summary>
    private static bool windowSizeApplied;

    public BaseView (ConfigObject layout)
    {
      _mainView = new Viewport();

      if (!windowSizeApplied)
      {
        windowSizeApplied = true;
        try
        {
          _mainView.Width = (int)layout["Width"];
          _mainView.Height = (int)layout["Height"];
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
        if (guiElement != null) { _mainView.Controls.Add(guiElement); }
      }
    }

    internal void ShowHeader(string displayText)
    {
      if (_mainView.Controls.FirstOrDefault(x => x.GetType() == typeof(Header)) is Header header)
      {
        header.DisplayText = displayText;
      }
    }

    internal void ShowFooter(string displayText)
    {
      if (_mainView.Controls.FirstOrDefault(x => x.GetType() == typeof(Footer)) is Footer footer)
      {
        footer.DisplayText = displayText;
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
