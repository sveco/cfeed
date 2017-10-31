namespace cFeed.Util
{
  using System;
  using System.Diagnostics;
  using System.IO;
  using JsonConfig;

  /// <summary>
  /// Handles integration with default or configured browser
  /// </summary>
  public class Browser
  {
    public static void Open(Uri adress)
    {
      if (!string.IsNullOrEmpty(Config.Global.Browser)
           && File.Exists(Config.Global.Browser))
      {
        //Open article url with configured browser
        Process.Start(Config.Global.Browser, adress.ToString());
      }
      else
      {
        //Open article url with default system browser
        Process.Start(adress.ToString());
      }
    }
  }
}
