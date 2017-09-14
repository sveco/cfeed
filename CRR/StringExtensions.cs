using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CRR
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

		public static int VisibleLength(this string str)
		{
			//var stripedAnsi = Regex.Replace(str, @"\e\[(\d+;)*(\d+)?[ABCDHJKfmsu]", "");
			var stripedAnsi = Regex.Replace(str, @"\x1b\[(\d+;)*(\d+)?[ABCDHJKfmsu]", "");
			var stripedControl = Regex.Replace(stripedAnsi, @"[^\x20-\x7F]", "");
			return stripedControl.Length;
		}
	}
}
