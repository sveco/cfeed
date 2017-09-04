using JsonConfig;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRR
{
    public static class Configuration
    {
        public static readonly string UNDERLINE = "\x1B[4m";
        public static readonly string UNDERLINE_OFF = "\x1B[24m";
        public static readonly string BOLD = "\x1B[1m";
        public static readonly string BOLD_OFF = "\x1B[21m";
        public static readonly string RESET = "\x1B[0m";
        public static readonly string ITALIC = "\x1B[1m";
        public static readonly string ITALIC_OFF = "\x1B[21m";

        public static string getReadState(bool IsNew)
        {
            var width = Math.Max(Config.Global.UI.Strings.ReadStateRead.Length, Config.Global.UI.Strings.ReadStateNew.Length) + 1;
            var result = IsNew ? Config.Global.UI.Strings.ReadStateNew : Config.Global.UI.Strings.ReadStateRead;
            return result.PadRight(width);
        }

        public static ConsoleColor getColor(string color) {
            ConsoleColor result = new ConsoleColor();
            if (Enum.TryParse<ConsoleColor>(color, out result))
            {
                return result;
            }
            else {
                throw new ArgumentException("Unknow color name, see https://msdn.microsoft.com/en-us/library/system.consolecolor(v=vs.110).aspx for valid color names.");
            }

        }

        public static List<ConsoleKey> getKeys(string[] keys) {
            List<ConsoleKey> result = new List<ConsoleKey>();
            foreach (var key in keys)
            {
                ConsoleKey k;
                if (Enum.TryParse(key, out k))
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

        public static ConsoleModifiers getModifier(string modifier)
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

        public static ConsoleModifiers getBitviseModifiers(string[] modifiers)
        {
            return modifiers.Select(x => getModifier(x)).Aggregate<ConsoleModifiers>((running, next) => (running | next));
        }

        public static bool verifyKey(ConsoleKeyInfo key, string[] keys, string[] modifiers)
        {
            if (getKeys(keys).Contains(key.Key))
            {
                if (Config.Global.Shortcuts.QuitApp.Modifiers.Length > 0)
                {
                    if ((key.Modifiers & getBitviseModifiers(modifiers)) != 0)
                    {
                        return true;
                    }
                    else { return false; }
                }
                return true;
            }
            return false;
        }
    }
}
