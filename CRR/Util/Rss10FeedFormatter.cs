using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using cFeed.Logging;

namespace cFeed.Util
{
  // RSS 1.0 feed formatter (without writer).
  // Based on http://www.4guysfromrolla.com/articles/031809-1.aspx
  public class Rss10FeedFormatter : SyndicationFeedFormatter
  {
    static NLog.Logger logger = Log.Instance.Logger;
    public Rss10FeedFormatter() { }
    public Rss10FeedFormatter(SyndicationFeed feed) : base(feed) { }
    public override string Version { get { return "Rss10"; } }
    public static string LocalName { get { return "RDF"; } }
    public static string RdfNamespaceUri { get { return "http://www.w3.org/1999/02/22-rdf-syntax-ns#"; } }
    public static string NamespaceUri { get { return "http://purl.org/rss/1.0/"; } }
    public static bool CanReadFrom(XmlReader reader)
    {
      return reader.IsStartElement(LocalName, RdfNamespaceUri);
    }

    public override bool CanRead(XmlReader reader)
    {
      return CanReadFrom(reader);
    }

    protected override SyndicationFeed CreateFeedInstance()
    {
      return new SyndicationFeed();
    }

    public override void ReadFrom(XmlReader reader)
    {
      if (!CanRead(reader))
      {
        throw new XmlException("Unknown RSS 1.0 feed format.");
      }
      SetFeed(CreateFeedInstance());
      ReadXml(reader, base.Feed);
    }

    static void ReadXml(XmlReader reader, SyndicationFeed result)
    {
      reader.ReadStartElement();              // Read in <RDF>
      reader.ReadStartElement("channel");     // Read in <channel>
      result.LastUpdatedTime = DateTime.Now;
      while (reader.IsStartElement())         // Process <channel> children
      {
        if (reader.IsStartElement("title"))
        {
          result.Title = new TextSyndicationContent(reader.ReadElementString());
        }
        else if (reader.IsStartElement("link"))
        {
          result.Links.Add(new SyndicationLink(new Uri(reader.ReadElementString())));
        }
        else if (reader.IsStartElement("description"))
        {
          result.Description = new TextSyndicationContent(reader.ReadElementString());
        }
        else if (reader.IsStartElement("pubDate"))
        {
          result.LastUpdatedTime = DateTime.Parse(reader.ReadElementString());
        }
        else
        {
          reader.Skip();
        }
      }
      reader.ReadEndElement();                // Read in </channel>
      while (reader.IsStartElement())
      {
        if (reader.IsStartElement("item"))
        {
          result.Items = ReadItems(reader);
          break;
        }
        reader.Skip();
      }
    }

    static IEnumerable<SyndicationItem> ReadItems(XmlReader reader)
    {
      var items = new Collection<SyndicationItem>();
      while (reader.IsStartElement("item"))
      {
        var item = new SyndicationItem();
        reader.ReadStartElement();
        while (reader.IsStartElement())
        {
          if (reader.IsStartElement("title"))
          {
            item.Title = new TextSyndicationContent(reader.ReadElementString());
          }
          else if (reader.IsStartElement("link"))
          {
            item.Links.Add(new SyndicationLink(new Uri(reader.ReadElementString())));
          }
          else if (reader.IsStartElement("description"))
          {
            item.Summary = new TextSyndicationContent(reader.ReadElementString());
          }
          else if (reader.IsStartElement("pubDate"))
          {
            try
            {
              item.PublishDate = DateTime.Parse(reader.ReadElementString());
            }
            catch (FormatException ex)
            {
              logger.Error(ex, "Date format invalid");
            }
          }
          else
          {
            reader.Skip();
          }
        }
        reader.ReadEndElement();
        items.Add(item);
      }
      return items;
    }

    public override void WriteTo(XmlWriter writer)
    {
      throw new NotImplementedException();
    }
  }
}
