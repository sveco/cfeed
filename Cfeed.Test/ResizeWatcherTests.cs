using System;
using System.IO;
using cFeed.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cfeed.Test
{
  /// <summary>
  /// Drives the watcher with Tick() and a fake console size, no timer and no console involved.
  /// </summary>
  [TestClass]
  public class ResizeWatcherTests
  {
    private int _width;
    private int _height;
    private int _resizes;
    private ResizeWatcher _watcher;

    [TestInitialize]
    public void Setup()
    {
      _width = 120;
      _height = 40;
      _resizes = 0;
      ResizeWatcher.Current = null;
      _watcher = new ResizeWatcher(() => _width, () => _height, () => _resizes++, stablePolls: 2);
    }

    [TestCleanup]
    public void Cleanup()
    {
      ResizeWatcher.Current = null;
    }

    private void Ticks(int count)
    {
      for (int i = 0; i < count; i++) { _watcher.Tick(); }
    }

    [TestMethod]
    public void NoResize_NothingHappens()
    {
      Ticks(10);

      Assert.AreEqual(0, _resizes);
    }

    [TestMethod]
    public void Resize_IsAppliedOnce_AfterItSettled()
    {
      _width = 90;
      _height = 25;

      Ticks(1);
      Assert.AreEqual(0, _resizes, "still might be dragged");
      Ticks(5);
      Assert.AreEqual(1, _resizes);
    }

    [TestMethod]
    public void ResizeWhilePaused_IsAppliedAfterResume()
    {
      var pause = _watcher.Pause();
      Assert.IsTrue(_watcher.IsPaused);

      _width = 90;
      Ticks(10);
      Assert.AreEqual(0, _resizes, "a prompt is open, redrawing would wipe it");

      pause.Dispose();
      Assert.IsFalse(_watcher.IsPaused);
      Ticks(2);
      Assert.AreEqual(1, _resizes);
    }

    [TestMethod]
    public void NestedPauses_AreCounted()
    {
      var outer = _watcher.Pause();
      var inner = _watcher.Pause();

      inner.Dispose();
      Assert.IsTrue(_watcher.IsPaused, "outer pause is still open");
      inner.Dispose();
      Assert.IsTrue(_watcher.IsPaused, "disposing twice must not release the outer pause");

      outer.Dispose();
      Assert.IsFalse(_watcher.IsPaused);
    }

    [TestMethod]
    public void TooSmallWindow_IsNotDrawn_ButRedrawnWhenLargeAgainEvenAtTheSameSize()
    {
      _width = 10;
      _height = 3;
      Ticks(5);
      Assert.AreEqual(0, _resizes, "too small to draw anything");

      // back to the size the screen was drawn for, but the screen content is garbage now
      _width = 120;
      _height = 40;
      Ticks(3);

      Assert.AreEqual(1, _resizes);
    }

    [TestMethod]
    public void MinimumSize_IsStillDrawn()
    {
      _width = ResizeWatcher.MinWidth;
      _height = ResizeWatcher.MinHeight;

      Ticks(3);

      Assert.AreEqual(1, _resizes);
    }

    [TestMethod]
    public void FailingRedraw_DoesNotThrow_AndTheNextResizeStillWorks()
    {
      int calls = 0;
      var watcher = new ResizeWatcher(() => _width, () => _height, () => { calls++; if (calls == 1) throw new InvalidOperationException("boom"); }, 2);

      _width = 90;
      watcher.Tick();
      watcher.Tick();
      Assert.AreEqual(1, calls);

      _width = 80;
      watcher.Tick();
      watcher.Tick();
      Assert.AreEqual(2, calls);
    }

    [TestMethod]
    public void NoConsole_DoesNotThrow()
    {
      var watcher = new ResizeWatcher(() => _width, () => { if (_height == 40) return _height; throw new IOException("The handle is invalid."); }, () => _resizes++, 2);

      _height = 0;
      watcher.Tick();
      watcher.Tick();

      Assert.AreEqual(0, _resizes);
    }

    [TestMethod]
    public void Suspend_WithoutAWatcher_IsHarmless()
    {
      ResizeWatcher.Current = null;

      using (ResizeWatcher.Suspend())
      {
      }
    }

    [TestMethod]
    public void Suspend_PausesTheCurrentWatcher()
    {
      ResizeWatcher.Current = _watcher;

      var scope = ResizeWatcher.Suspend();
      Assert.IsTrue(_watcher.IsPaused);

      scope.Dispose();
      Assert.IsFalse(_watcher.IsPaused);
    }

    [TestMethod]
    public void Dispose_ForgetsItselfAsTheCurrentWatcher()
    {
      ResizeWatcher.Current = _watcher;

      _watcher.Dispose();

      Assert.IsNull(ResizeWatcher.Current);
    }
  }
}
