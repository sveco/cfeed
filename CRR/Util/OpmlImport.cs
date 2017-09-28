using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using cFeed.Entities;

namespace cFeed.Util
{
  public static class OpmlImport
  {
    public static List<Outline> Import(string uri)
    {
      var results = XDocument.Load(uri)
                           .Descendants("outline")
                           .Where(o => o.Attribute("type") != null && o.Attribute("type").Value == "rss")
                           //.Elements("outline")
                           .Select(o => new Outline
                           {
                             Title = o.Attribute("title").Value,
                             FeedUrl = o.Attribute("xmlUrl").Value,
                             Tags = (o.Parent != null && o.Parent.Attribute("title") != null) ? new string[] { o.Parent.Attribute("title").Value } : null
                           });
      return results.ToList();
    }
  }
}
