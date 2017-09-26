/*
             __               _ 
   ___      / _| ___  ___  __| |
  / __|____| |_ / _ \/ _ \/ _` |
 | (_|_____|  _|  __/  __/ (_| |
  \___|    |_|  \___|\___|\__,_| 
  Console Feed Reader

  Big thanks to awesome newsbeuter team for inspiration. This app is built from scratch, and do not use any portion
  of newsbeuter code. This is open source project to provide windows users with purely textual Atom and RSS feed reader.

  Copyright (c) 2017 Sveco

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.

 */

using CGui.Gui;
using JsonConfig;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using cFeed.Util;
using cFeed.Entities;
using cFeed.Native;
using System.IO;
using cFeed.Logging;
using System.Text;

namespace cFeed
{
    class Program
    {
        const int STD_OUTPUT_HANDLE = -11;
        const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
        const uint ENABLE_EXTENDED_FLAGS = 0x0080;
        const uint ENABLE_QUICK_EDIT = 0x0040;
        const uint DISABLE_NEWLINE_AUTO_RETURN = 0x0008;

        //private static bool ConsoleCtrlCheck(CtrlTypes ctrlType)
        //{
        //    // Put your own handler here
        //    return true;
        //}
        private static FeedListView feedList;

        static void Main(string[] args)
        {
            Logging.Logger.Log(LogLevel.Info, "App started.");

            var arguments = new ArgumentParser(args);

            var handle = NativeMethods.GetStdHandle(STD_OUTPUT_HANDLE);
            if (NativeMethods.GetConsoleMode(handle, out uint mode))
            {
                mode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;
                mode |= DISABLE_NEWLINE_AUTO_RETURN;
                //mode |= ENABLE_EXTENDED_FLAGS;
                NativeMethods.SetConsoleMode(handle, mode);
            }
            Console.OutputEncoding = Encoding.UTF8;
            //SetConsoleCtrlHandler(new HandlerRoutine(ConsoleCtrlCheck), true);

            //arguments
            var database = arguments.GetArgValue<string>("d", Config.Global.Database);
            var refresh = arguments.GetArgValue<bool>("r", Config.Global.Refresh);

            //show help?
            if (arguments.GetArgValue<bool>("h"))
            {
                ShowHelp();
            }

            FileInfo conf = new FileInfo("settings.conf");
            Config.WatchUserConfig(conf);
            Config.OnUserConfigFileChanged += Config_OnUserConfigFileChanged;

            using (var db = new LiteDatabase(database))
            {
                List<RssFeed> feeds = new List<RssFeed>();
        
                var configFeeds = Enumerable.ToList(Config.Global.Feeds);
                int i = 0;
                foreach (var feed in configFeeds)
                {
                    feeds.Add(new RssFeed(feed.FeedUrl, feed.FeedQuery, i, db, feed.Title) {
                        Filters = feed.Filters,
                        Hidden = feed.Hidden
                    });
                    i++;
                }

                feedList = new FeedListView(feeds, db);
                feedList.Show(refresh);
            }
        }

        private static void Config_OnUserConfigFileChanged()
        {
            if (feedList != null)
            {
                feedList.RefreshConfig();
            }
        }

        private static void ShowHelp()
        {
          var content = "TBD: Help. Esc to continue to app.";
          var help = new TextArea(content)
          {
            Top = 0,
            Left = 0,
            Width = Console.WindowWidth,
            Height = Console.WindowHeight,
            WaitForInput = true
          };
          help.Show();
        }
    }
}
