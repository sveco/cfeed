/*
             __               _ 
   ___      / _| ___  ___  __| |
  / __|____| |_ / _ \/ _ \/ _` |
 | (_|_____|  _|  __/  __/ (_| |
  \___|    |_|  \___|\___|\__,_| 
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

            var urls = readUrls();

            using (var db = new LiteDatabase(@"cfeed.db"))
            {
                FeedHandler feedHandler = new FeedHandler(urls, db);
                feedHandler.DisplayFeedList();
            }
        }

        private static IList<string> readUrls()
        {
            string line;
            List<string> result = new List<string>();

            // Read the file and display it line by line.
            System.IO.StreamReader file =
               new System.IO.StreamReader("urls.txt");
            while ((line = file.ReadLine()) != null)
            {
                result.Add(line);
            }
            file.Close();
            return result;
        }
    }
}
