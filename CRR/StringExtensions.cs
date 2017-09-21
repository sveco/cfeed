using System.IO;
using System.Text.RegularExpressions;

namespace cFeed
{
  public static class StringExtensions
	{
		public static string SanitizeFileName(this string filename)
		{
			string regexSearch = new string(Path.GetInvalidFileNameChars());
			Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
			return r.Replace(filename, "");
		}

		public static string SanitizePath(this string path)
		{
			string regexSearch = new string(Path.GetInvalidPathChars());
			Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
			return r.Replace(path, "");
		}
	}
}
