using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace cFeed.Util
{
  public static class Formatter
  {
    public static string FormatLine(string Format, Dictionary<string, string> replacementTable) {
      MatchCollection matches = Regex.Matches(Format, Configuration.ReplacementPattern);
      foreach (Match match in matches)
      {
        var token = match.Groups[1].Value;

        if (!replacementTable.ContainsKey(token)) { continue; }

        int pad;
        int.TryParse(match.Groups[2].Value, out pad);
        string direction = match.Groups[3].Value;

        if (pad != 0)
        {
          if (direction != null)
          {
            if (direction == "r")
            { Format = Format.Replace(match.Value, replacementTable[token].PadLeft(pad)); }
            else if (direction == "l")
            { Format = Format.Replace(match.Value, replacementTable[token].PadRight(pad)); }
            else
            {
              Format = Format.Replace(match.Value, replacementTable[token]);
            }
          }
        }
        else
        {
          Format = Format.Replace(match.Value, replacementTable[token]);
        }
      }
      return Format;
    }
  }
}
