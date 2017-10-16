using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cFeed.Entities;
using CGui.Gui;
using CGui.Gui.Primitives;
using JsonConfig;

namespace cFeed.Util
{
  public static class ControlFactory
  {
    private static string FormatFeedView(string format)
    {
      return format
        .Replace("%v", Configuration.VERSION)
        .Replace("%V", Configuration.MAJOR_VERSION);
    }

    private static int AbsWidth (int width) {
      if (width < 0) { return Console.WindowWidth + width; }
      return width;
    }

    private static int AbsHeight(int height)
    {
      if (height < 0) { return Console.WindowHeight + height; }
      return height;
    }

    public static GuiElement Get(dynamic control)
    {
      if (control is NullExceptionPreventer) { return null; }

      GuiElement guiElement = null;

      if (!(control.Header is NullExceptionPreventer))
      {
        guiElement = new Header(FormatFeedView(control.Header.Text))
        {
          ForegroundColor = Configuration.GetColor(control.Header.Foreground),
          BackgroundColor = Configuration.GetColor(control.Header.Background),
          PadChar = Convert.ToChar(control.Header.Padchar)
        };
      }

      if (!(control.Footer is NullExceptionPreventer))
      {
        guiElement = new Footer(FormatFeedView(control.Footer.Text))
        {
          ForegroundColor = Configuration.GetColor(control.Footer.Foreground),
          BackgroundColor = Configuration.GetColor(control.Footer.Background),
          PadChar = Convert.ToChar(control.Footer.Padchar)
        };
      }

      if (!(control.FeedList is NullExceptionPreventer))
      {
        guiElement = new Picklist<RssFeed>(new List<RssFeed>())
        {
          ForegroundColor = Configuration.GetColor(control.FeedList.Foreground),
          BackgroundColor = Configuration.GetColor(control.FeedList.Background),
          Top = control.FeedList.Top,
          Left = control.FeedList.Left,
          Width = AbsWidth(control.FeedList.Width),
          Height = AbsHeight(control.FeedList.Height),
          ShowScrollBar = control.FeedList.ShowScrollBar
        };
      }

      if (!(control.ArticleList is NullExceptionPreventer))
      {
        guiElement = new Picklist<FeedItem>(new List<FeedItem>())
        {
          ForegroundColor = Configuration.GetColor(control.ArticleList.Foreground),
          BackgroundColor = Configuration.GetColor(control.ArticleList.Background),
          Top = control.ArticleList.Top,
          Left = control.ArticleList.Left,
          Width = AbsWidth(control.ArticleList.Width),
          Height = AbsHeight(control.ArticleList.Height),
          ShowScrollBar = control.ArticleList.ShowScrollBar
        };
      }
      return guiElement;
    }
  }
}
