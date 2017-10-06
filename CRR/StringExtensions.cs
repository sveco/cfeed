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
