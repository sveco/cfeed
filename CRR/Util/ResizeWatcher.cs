namespace cFeed.Util
{
  using System;
  using System.Threading;
  using cFeed.Logging;

  /// <summary>
  /// Notices when the console window is resized and asks the active view to redraw itself.
  /// The views wait for keys in a blocking loop, so nothing else would notice.
  /// </summary>
  public sealed class ResizeWatcher : IDisposable
  {
    /// <summary>
    /// Below this size the views cannot be drawn, the redraw waits until the window is larger again.
    /// </summary>
    public const int MinWidth = 20;
    public const int MinHeight = 6;

    private readonly Func<int> _width;
    private readonly Func<int> _height;
    private readonly Action _onResize;
    private readonly SizeChangeDetector _detector;
    private Timer _timer;
    private int _pauses;
    private int _ticking;
    private bool _wasTooSmall;

    /// <summary>
    /// The watcher of the running application. Null when the application is not watching the console.
    /// </summary>
    public static ResizeWatcher Current { get; set; }

    /// <param name="width">Gets current console width</param>
    /// <param name="height">Gets current console height</param>
    /// <param name="onResize">Called when a new size has settled</param>
    /// <param name="stablePolls">How many polls in a row must see the same new size</param>
    public ResizeWatcher(Func<int> width, Func<int> height, Action onResize, int stablePolls = 2)
    {
      _width = width;
      _height = height;
      _onResize = onResize;
      _detector = new SizeChangeDetector(width(), height(), stablePolls);
    }

    /// <summary>
    /// Pauses the current watcher, if there is one. Use while a prompt or dialog is open, a redraw
    /// would wipe it. A resize that happens meanwhile is applied after the scope is disposed.
    /// </summary>
    public static IDisposable Suspend()
    {
      var current = Current;
      return current != null ? current.Pause() : new NoScope();
    }

    public void Start(int intervalMilliseconds = 100)
    {
      _timer = new Timer(_ => Tick(), null, intervalMilliseconds, intervalMilliseconds);
    }

    public bool IsPaused
    {
      get { return Volatile.Read(ref _pauses) > 0; }
    }

    public IDisposable Pause()
    {
      Interlocked.Increment(ref _pauses);
      return new PauseScope(this);
    }

    /// <summary>
    /// Polls the console size once. Called by the timer, public so it can be driven by tests.
    /// </summary>
    public void Tick()
    {
      // a redraw can take longer than the polling interval
      if (Interlocked.Exchange(ref _ticking, 1) == 1)
      {
        return;
      }

      try
      {
        if (IsPaused)
        {
          return;
        }

        int width;
        int height;
        try
        {
          width = _width();
          height = _height();
        }
        catch (Exception ex)
        {
          // no console to watch (output redirected), stop polling instead of failing every 100 ms
          Log.Instance.Logger.Warn("Cannot read console size, resize watching stopped. " + ex.Message);
          Stop();
          return;
        }

        if (width < MinWidth || height < MinHeight)
        {
          if (!_wasTooSmall)
          {
            _wasTooSmall = true;
            _detector.Invalidate();
          }
          return;
        }
        _wasTooSmall = false;

        if (_detector.Poll(width, height))
        {
          _onResize();
        }
      }
      catch (Exception ex)
      {
        // a failed redraw must not take the application down, the next resize or key press draws again
        Log.Instance.Logger.Error("Redraw after resize failed.");
        Log.Instance.Logger.Error(ex);
      }
      finally
      {
        Interlocked.Exchange(ref _ticking, 0);
      }
    }

    public void Stop()
    {
      var timer = Interlocked.Exchange(ref _timer, null);
      if (timer != null)
      {
        timer.Dispose();
      }
    }

    public void Dispose()
    {
      Stop();
      if (ReferenceEquals(Current, this))
      {
        Current = null;
      }
    }

    private sealed class PauseScope : IDisposable
    {
      private ResizeWatcher _owner;

      public PauseScope(ResizeWatcher owner)
      {
        _owner = owner;
      }

      public void Dispose()
      {
        var owner = Interlocked.Exchange(ref _owner, null);
        if (owner != null)
        {
          Interlocked.Decrement(ref owner._pauses);
        }
      }
    }

    private sealed class NoScope : IDisposable
    {
      public void Dispose()
      {
      }
    }
  }
}
