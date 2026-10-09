using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using cFeed.Entities;

namespace cFeed.Util
{
  public static class OpmlImport
  {
    public static IList<Outline> Import(string uri)
    {
      var results = XDocument.Load(uri)
                           .Descendants("outline")
                           // Subscription outlines carry xmlUrl. Category outlines (folders) do not.
                           .Where(o => !string.IsNullOrWhiteSpace((string)o.Attribute("xmlUrl"))
                                       && IsFeedType((string)o.Attribute("type")))
                           .Select(o => new Outline
                           {
                             // Many exporters write only "text", and some write neither, so fall back to the url.
                             Title = FirstNonEmpty((string)o.Attribute("title"),
                                                   (string)o.Attribute("text"),
                                                   (string)o.Attribute("xmlUrl")),
                             FeedUrl = ((string)o.Attribute("xmlUrl")).Trim(),
                             Tags = o.Parent != null
                                    && !string.IsNullOrWhiteSpace(FirstNonEmpty((string)o.Parent.Attribute("title"), (string)o.Parent.Attribute("text")))
                                  ? new string[] { FirstNonEmpty((string)o.Parent.Attribute("title"), (string)o.Parent.Attribute("text")) }
                                  : null
                           });
      return results.ToList();
    }

    private static bool IsFeedType(string type)
    {
      // type is optional, rss is the usual value, atom is used by some exporters
      return string.IsNullOrEmpty(type)
             || type.Equals("rss", StringComparison.OrdinalIgnoreCase)
             || type.Equals("atom", StringComparison.OrdinalIgnoreCase);
    }

    private static string FirstNonEmpty(params string[] values)
    {
      return values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
    }
  }
}
