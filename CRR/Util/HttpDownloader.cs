namespace cFeed.Util
{
  using System;
  using System.IO;
  using System.IO.Compression;
  using System.Net;
  using System.Text;
  using System.Text.RegularExpressions;

  /// <summary>
  /// Wrapped download class which supports gzip and checks encoding header and meta tags in order to decode it correctly.
  /// See https://stackoverflow.com/questions/2700638/characters-in-string-changed-after-downloading-html-from-the-internet
  /// </summary>
  public class HttpDownloader
  {
    private readonly string _referer;
    private readonly string _userAgent;

    public Encoding Encoding { get; set; }
    public WebHeaderCollection Headers { get; set; }
    public Uri Url { get; set; }

    public HttpDownloader(string url, string referer, string userAgent)
    {
      Encoding = Encoding.GetEncoding("ISO-8859-1");
      Url = new Uri(url); // verify the uri
      _userAgent = userAgent;
      _referer = referer;
    }

    public string GetPage()
    {
      HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
      if (!string.IsNullOrEmpty(_referer))
        request.Referer = _referer;
      if (!string.IsNullOrEmpty(_userAgent))
        request.UserAgent = _userAgent;

      request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");

      using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
      {
        Headers = response.Headers;
        Url = response.ResponseUri;
        return ProcessContent(response);
      }

    }

    private string ProcessContent(HttpWebResponse response)
    {
      Stream s = response.GetResponseStream();
      if (response.ContentEncoding.ToLower().Contains("gzip"))
        s = new GZipStream(s, CompressionMode.Decompress);
      else if (response.ContentEncoding.ToLower().Contains("deflate"))
        s = new DeflateStream(s, CompressionMode.Decompress);

      MemoryStream memStream = new MemoryStream();
      int bytesRead;
      byte[] buffer = new byte[0x1000];
      for (bytesRead = s.Read(buffer, 0, buffer.Length); bytesRead > 0; bytesRead = s.Read(buffer, 0, buffer.Length))
      {
        memStream.Write(buffer, 0, bytesRead);
      }
      s.Close();

      return DecodeContent(memStream.ToArray(), response.ContentType);
    }

    /// <summary>
    /// Decodes downloaded html. The charset sent in the Content-Type header wins, then the one declared
    /// in a meta tag, otherwise ISO-8859-1 is used.
    /// </summary>
    /// <param name="data">Response body, already decompressed</param>
    /// <param name="contentType">Value of the Content-Type header</param>
    public string DecodeContent(byte[] data, string contentType)
    {
      SetEncodingFromHeader(contentType);

      string html;
      var memStream = new MemoryStream(data);
      using (StreamReader r = new StreamReader(memStream, Encoding))
      {
        html = r.ReadToEnd().Trim();
        html = CheckMetaCharSetAndReEncode(memStream, html);
      }

      return html;
    }

    private void SetEncodingFromHeader(string contentType)
    {
      // HttpWebResponse.CharacterSet reports ISO-8859-1 when the server sends no charset at all. That would
      // override the charset declared inside the page, so only trust a charset that is really in the header.
      if (string.IsNullOrEmpty(contentType))
        return;

      Match m = Regex.Match(contentType, @";\s*charset\s*=\s*(?<charset>[^;]*)", RegexOptions.IgnoreCase);
      if (!m.Success)
        return;

      string charset = m.Groups["charset"].Value.Trim().Trim('\'', '"'); // Sometimes encoding is enclosed in additional quotes.
      if (charset.Length == 0)
        return;

      try
      {
        Encoding = Encoding.GetEncoding(charset);
      }
      catch (ArgumentException)
      {
      }
    }

    private string CheckMetaCharSetAndReEncode(Stream memStream, string html)
    {
      // Matches <meta charset=utf-8>, <meta charSet="utf-8" /> and <meta http-equiv=... content="text/html; charset=utf-8">.
      // The search stays inside one tag, so a later "charset=" in a script is not picked up.
      Match m = new Regex(@"<meta\s[^>]*?charset\s*=\s*[""']?(?<charset>[A-Za-z0-9_-]+)", RegexOptions.IgnoreCase).Match(html);
      if (m.Success)
      {
        string charset = m.Groups["charset"].Value.ToLower() ?? "iso-8859-1";
        if ((charset == "unicode") || (charset == "utf-16"))
        {
          charset = "utf-8";
        }

        try
        {
          Encoding metaEncoding = Encoding.GetEncoding(charset);
          if (Encoding.CodePage != metaEncoding.CodePage)
          {
            memStream.Position = 0L;
            StreamReader recodeReader = new StreamReader(memStream, metaEncoding);
            html = recodeReader.ReadToEnd().Trim();
            recodeReader.Close();
          }
        }
        catch (ArgumentException)
        {
        }
      }

      return html;
    }
  }
}
