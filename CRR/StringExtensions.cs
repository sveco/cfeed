using System.IO;
using System.Text.RegularExpressions;

namespace cFeed
{
  public static class StringExtensions
	{
		public static string SanitizeFileName(this string fileName)
		{
			string regexSearch = new string(Path.GetInvalidFileNameChars());
			Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
			return r.Replace(fileName, "");
		}

		public static string SanitizePath(this string path)
		{
			string regexSearch = new string(Path.GetInvalidPathChars());
			Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
			return r.Replace(path, "");
		}

    private static int VisibleLength(this string str)
    {
      var stripedControl = Regex.Replace(str, @"\p{C}\[([fb]?)\:?(\w+)\]", "");
      return stripedControl.Length;
    }

    /// <summary>
    /// Shortens the text to at most <paramref name="maxVisible"/> visible characters. Colour tags do not count
    /// and are always kept, so the colours after the cut stay balanced.
    /// </summary>
    public static string TruncateVisible(this string str, int maxVisible)
    {
      if (str == null || maxVisible < 0 || str.VisibleLength() <= maxVisible) return str;

      var tag = new Regex(@"\G\p{C}\[([fb]?)\:?(\w+)\]");
      var result = new System.Text.StringBuilder();
      int visible = 0;
      int i = 0;
      while (i < str.Length)
      {
        var match = tag.Match(str, i);
        if (match.Success)
        {
          result.Append(match.Value);
          i += match.Length;
          continue;
        }
        if (visible < maxVisible)
        {
          result.Append(str[i]);
          visible++;
        }
        i++;
      }
      return result.ToString();
    }

    public static string PadLeftVisible(this string str, int pad)
    {
      var lengthv = str.VisibleLength();
      if (lengthv > pad) return str;
      else return str.PadLeft(pad + (str.Length - lengthv));
    }

    public static string PadRightVisible(this string str, int pad)
    {
      var lengthv = str.VisibleLength();
      if (lengthv > pad) return str;
      else return str.PadRight(pad + (str.Length - lengthv));
    }
  }
}
