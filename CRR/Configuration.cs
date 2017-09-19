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
        public static readonly string VERSION = version.ToString();
        public static readonly string MAJOR_VERSION = version.Major.ToString() + "." + version.Minor.ToString();


        public static readonly string UNDERLINE = "\x1B[4m";
        public static readonly string UNDERLINE_OFF = "\x1B[24m";
        public static readonly string BOLD = "\x1B[1m";
        public static readonly string BOLD_OFF = "\x1B[21m";
        public static readonly string RESET = "\x1B[0m";
        public static readonly string ITALIC = "\x1B[1m";
        public static readonly string ITALIC_OFF = "\x1B[21m";

        public static class AnsiColor
        {
            public static readonly string Reset = "\x1b[0m";
            public static readonly string Cyan = "\x1b[36m";
        }

        public static class TextColor {
            public static readonly string Reset = "\x1b[Reset]";

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
                    if ((key.Modifiers & GetBitviseModifiers(modifiers)) != 0)
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
