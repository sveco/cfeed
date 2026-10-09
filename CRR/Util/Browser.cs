namespace cFeed.Util
{
  using System;
  using System.Diagnostics;
  using System.IO;
  using cFeed.Logging;
  using JsonConfig;

  /// <summary>
  /// Handles integration with default or configured browser
  /// </summary>
  public class Browser
  {
    /// <summary>
    /// Only web links are opened. Feed content is untrusted, and passing other schemes
    /// (file:, ms-msdt:, custom protocol handlers) to the shell could launch programs.
    /// </summary>
    public static bool IsSafeToOpen(Uri address)
    {
      return address != null
             && address.IsAbsoluteUri
             && (address.Scheme == Uri.UriSchemeHttp || address.Scheme == Uri.UriSchemeHttps);
    }

    public static void Open(Uri adress)
    {
      if (!IsSafeToOpen(adress))
      {
        Log.Instance.Logger.Warn("Refusing to open non-http(s) link: " + adress);
        return;
      }

      if (!string.IsNullOrEmpty(Config.Global.Browser)
           && File.Exists(Config.Global.Browser))
      {
        //Open article url with configured browser
        Process.Start(Config.Global.Browser, adress.AbsoluteUri);
      }
      else
      {
        //Open article url with default system browser
        Process.Start(adress.AbsoluteUri);
      }
    }
  }
}
