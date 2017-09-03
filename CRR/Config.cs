using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRR
{
    public static class Config
    {
        public static string ReadStateNew = "[N]";
        public static string ReadStateRead = "[ ]";
        public static string FeedListFormat = "%i %n [%u] %t";
        public static string ArticleListFormat = "%i %n %d %t";
        public static string FeedTitleFormat = "cFeed v0.2 - Articles in \'%t\' %u";
        public static string ArticleTitleFormat = "cFeed v0.2 - Article:%t";

        public static ConsoleColor DefaultForeground = ConsoleColor.White;
        public static ConsoleColor DefaultBackground = ConsoleColor.Black;

        public static ConsoleColor DefaultSelectedForeground = ConsoleColor.Black;
        public static ConsoleColor DefaultSelectedBackground = ConsoleColor.DarkYellow;

        public static ConsoleColor FeedListHeaderBackground = ConsoleColor.DarkCyan;
        public static ConsoleColor FeedListHeaderForeground = ConsoleColor.Yellow;
        public static ConsoleColor FeedListFooterBackground = ConsoleColor.DarkCyan;
        public static ConsoleColor FeedListFooterForeground = ConsoleColor.Yellow;
        public static ConsoleColor ArticleListFooterBackground = ConsoleColor.DarkCyan;
        public static ConsoleColor ArticleListFooterForeground = ConsoleColor.Yellow;
        public static ConsoleColor ArticleListHeaderBackground = ConsoleColor.DarkCyan;
        public static ConsoleColor ArticleListHeaderForeground = ConsoleColor.Yellow;
        public static ConsoleColor ArticleHeaderBackground = ConsoleColor.DarkCyan;
        public static ConsoleColor ArticleHeaderForeground = ConsoleColor.Yellow;
        public static ConsoleColor ArticleFooterBackground = ConsoleColor.DarkCyan;
        public static ConsoleColor ArticleFooterForeground = ConsoleColor.Yellow;

        public static int FeedListLeft = 2;
        public static int FeedListTop = 1;
        public static int FeedMaxItems = 20;
        public static int ArticleListLeft = 2;
        public static int ArticleListTop = 1;
        public static int ArticleListHeight = -3;

        public static readonly string UNDERLINE = "\x1B[4m";
        public static readonly string UNDERLINE_OFF = "\x1B[24m";
        public static readonly string BOLD = "\x1B[1m";
        public static readonly string BOLD_OFF = "\x1B[21m";
        public static readonly string RESET = "\x1B[0m";

        public static string getReadState(bool IsNew)
        {
            var width = Math.Max(Config.ReadStateRead.Length, Config.ReadStateNew.Length) + 1;
            var result = IsNew ? Config.ReadStateNew : Config.ReadStateRead;
            return result.PadRight(width);
        }
    }
}
