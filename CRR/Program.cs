/*
             __               _ 
   ___      / _| ___  ___  __| |
  / __|____| |_ / _ \/ _ \/ _` |
 | (_|_____|  _|  __/  __/ (_| |
  \___|    |_|  \___|\___|\__,_| 
  Console Feed Reader

  Big thanks to awesome newsbeuter team for inspiration. This app is built from scratch, and do not use any portion
  of newsbeuter code. This is open source project to provide windows users with purely textual Atom and RSS feed reader.
 */

using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Runtime.InteropServices;
using JsonConfig;

namespace CRR
{
    class Program
    {
        const int STD_OUTPUT_HANDLE = -11;
        const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 4;

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        static void Main(string[] args)
        {
            var handle = GetStdHandle(STD_OUTPUT_HANDLE);
            uint mode;
            GetConsoleMode(handle, out mode);
            mode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;
            SetConsoleMode(handle, mode);


            using (var db = new LiteDatabase(@"cfeed.db"))
            {
                List<RssFeed> feeds = new List<RssFeed>();
                var configFeeds = Enumerable.ToList(JsonConfig.Config.Global.Feeds);
                int i = 0;
                foreach (var feed in configFeeds)
                {
                    feeds.Add(new RssFeed(feed.FeedUrl, i, db) {
                        Filters = feed.Filters
                    });
                    i++;
                }

                FeedHandler feedHandler = new FeedHandler(feeds, db);
                feedHandler.DisplayFeedList();
            }
        }
    }
}
