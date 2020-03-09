using Microsoft.VisualStudio.TestTools.UnitTesting;
using cFeed.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JsonConfig;
using Microsoft.QualityTools.Testing.Fakes;
using cFeed.Entities;
using cFeed.Views.Fakes;
using CSharpFunctionalExtensions;
using CGui.Gui;
using cFeed.Logging.Fakes;
using NLog;
using CGui.Gui.Primitives;

namespace cFeed.Views.Tests
{
  [TestClass()]
  public class FeedListViewTests
  {
    [TestMethod()]
    public void ShowTest()
    {
      using (ShimsContext.Create())
      {
        ShimLog.AllInstances.ConfiguredLogLevelGet = (a) => { return LogLevel.FromString("Debug"); };


        ShimFeedListView.AllInstances.GetFeedListIListOfRssFeed = (a, b) =>
        {
          var f = new List<RssFeed>();
          return Result.Ok<IList<RssFeed>>(f);
        };

        ShimBaseView.AllInstances.ShowHeaderString = (a,b) => { };
        ShimBaseView.AllInstances.ShowFooterString = (a, b) => { };

        ShimFeedListView.AllInstances.GetPicklist = (a) =>
        {
          var r = new Picklist<RssFeed>();
          return Result.Ok(r);
        };

        ConfigObject layout = new ConfigObject();
        var controls = new List<GuiElement>();
        layout.Add(new KeyValuePair<string, object>("Width", 100));
        layout.Add(new KeyValuePair<string, object>("Height", 101));
        layout.Add(new KeyValuePair<string, object>("Controls", controls));

        var view = new FeedListView(layout);
        var feeds = new List<RssFeed>();

        view.Show(false, feeds);

        Assert.AreEqual(view._mainView.Width, 100);
        Assert.AreEqual(view._mainView.Height, 101);
      }
    }
  }
}