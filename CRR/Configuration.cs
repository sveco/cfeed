using JsonConfig;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace CRR
{
    public static class Configuration
    {
        private static Version version = Assembly.GetExecutingAssembly().GetName().Version;

        public static readonly string ArticleRootPath = Config.Global.SavedFileRoot;
        public static readonly string LoadingSuffix = Config.Global.UI.Strings.LoadingSuffix;
        public static readonly string LoadingPrefix = Config.Global.UI.Strings.LoadingPrefix;
        public static readonly string ArticleTextHighlight = Configuration.TextColor.ForegroundColor(Config.Global.UI.Colors.ArticleTextHighlight);
        public static readonly string ColorReset = "\x1b[Reset]";

        public static readonly string VERSION = version.ToString();
        public static readonly string MAJOR_VERSION = version.Major.ToString() + "." + version.Minor.ToString();

        /// <summary>
        /// Converts color name to "flag" that CGUI textbox renderer understands
        /// </summary>
        public static class TextColor {


            public static string ForegroundColor(string c) {
                return "\x1b[f:" + c + "]";
            }
            public static string BackgroundColor(string c)
            {
                return "\x1b[b:" + c + "]";
            }
        }

        private static string readStateRead = Config.Global.UI.Strings.ReadStateRead as string;
        private static string readStateNew = Config.Global.UI.Strings.ReadStateNew as string;
    
        public static string GetReadState(bool IsNew)
        {
            var width = Math.Max(readStateRead.Length, readStateNew.Length) + 1;
            var result = IsNew ? readStateNew : readStateRead;
            return result.PadRight(width);
        }

        public static ConsoleColor GetColor(string color) {
            ConsoleColor result = new ConsoleColor();
            if (Enum.TryParse<ConsoleColor>(color, out result))
            {
                return result;
            }
            else {
                throw new ArgumentException("Unknow color name, see https://msdn.microsoft.com/en-us/library/system.consolecolor(v=vs.110).aspx for valid color names.");
            }

        }

        public static List<ConsoleKey> GetKeys(string[] keys) {
            List<ConsoleKey> result = new List<ConsoleKey>();
            foreach (var key in keys)
            {
                if (Enum.TryParse(key, out ConsoleKey k))
                {
                    result.Add(k);
                }
                else
                {
                    throw new ArgumentException("Unknow color name, see https://msdn.microsoft.com/en-us/library/system.consolekey(v=vs.110).aspx for valid key names.");
                }
            }
            return result;
        }

        public static ConsoleModifiers GetModifier(string modifier)
        {
            ConsoleModifiers result = new ConsoleModifiers();
            if (Enum.TryParse(modifier, out result))
            {
                return result;
            }
            else
            {
                throw new ArgumentException("Unknow color name, see https://msdn.microsoft.com/en-us/library/system.consolekey(v=vs.110).aspx for valid key names.");
            }
        }

        public static ConsoleModifiers GetBitviseModifiers(string[] modifiers)
        {
            return modifiers.Select(x => GetModifier(x)).Aggregate<ConsoleModifiers>((running, next) => (running | next));
        }

        public static bool VerifyKey(ConsoleKeyInfo key, string[] keys, string[] modifiers)
        {
            if (GetKeys(keys).Contains(key.Key))
            {
                if (modifiers.Length > 0)
                {
                    if ((key.Modifiers == GetBitviseModifiers(modifiers)))
                    {
                        return true;
                    }
                    else { return false; }
                }
                if (key.Modifiers != 0)
                {
                    return false;
                }
                return true;
            }
            return false;
        }
    }
}
