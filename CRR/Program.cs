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

using JsonConfig;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using static CRR.NativeMethods;

namespace CRR
{
    class Program
    {
        const int STD_OUTPUT_HANDLE = -11;
        const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
        const uint ENABLE_EXTENDED_FLAGS = 0x0080;
        const uint ENABLE_QUICK_EDIT = 0x0040;
        const uint DISABLE_NEWLINE_AUTO_RETURN = 0x0008;

        private static bool ConsoleCtrlCheck(CtrlTypes ctrlType)
        {
            // Put your own handler here
            return true;
        }

        static void Main(string[] args)
        {
            var handle = GetStdHandle(STD_OUTPUT_HANDLE);
            if (GetConsoleMode(handle, out uint mode))
            {
                mode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;
                mode |= DISABLE_NEWLINE_AUTO_RETURN;
                mode |= ENABLE_EXTENDED_FLAGS;
                SetConsoleMode(handle, mode);
            }
            SetConsoleCtrlHandler(new HandlerRoutine(ConsoleCtrlCheck), true);

            using (var db = new LiteDatabase(@"cfeed.db"))
            {
                List<RssFeed> feeds = new List<RssFeed>();
                var configFeeds = Enumerable.ToList(Config.Global.Feeds);
                int i = 0;
                foreach (var feed in configFeeds)
                {
                    feeds.Add(new RssFeed(feed.FeedUrl, i, db, feed.Title) {
                        Filters = feed.Filters
                    });
                    i++;
                }

                FeedHandler feedHandler = new FeedHandler(feeds, db);
                feedHandler.DisplayFeedList(false);
            }
        }
    }
}
