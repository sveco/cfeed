namespace cFeed.Util.Atom0_3
{
  using System;
  using System.Collections.Generic;
  using System.Collections.ObjectModel;
  using System.ServiceModel.Syndication;
  using System.Xml;
  using cFeed.Logging;

  public class Atom03FeedFormatter : SyndicationFeedFormatter
  {
    static NLog.Logger logger = Log.Instance.Logger;
    public override string Version => "Atom03";
    public static string LocalName { get { return "feed"; } }
    public static string RdfNamespaceUri { get { return "http://purl.org/atom/ns#"; } }

    public override bool CanRead(XmlReader reader)
    {
      return CanReadFrom(reader);
    }

    public static bool CanReadFrom(XmlReader reader)
    {
      return reader.IsStartElement(LocalName, RdfNamespaceUri);
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

    public override void WriteTo(XmlWriter writer)
    {
      throw new NotImplementedException();
    }

    protected override SyndicationFeed CreateFeedInstance()
    {
      return new SyndicationFeed();
    }

    static IEnumerable<SyndicationItem> ReadItems(XmlReader reader)
    {
      var items = new Collection<SyndicationItem>();
      while (reader.IsStartElement("item") || reader.IsStartElement("entry"))
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
            item.Links.Add(new SyndicationLink(new Uri(reader.GetAttribute("href"))));
            reader.ReadElementString();
          }
          else if (reader.IsStartElement("description") || reader.IsStartElement("summary"))
          {
            item.Summary = new TextSyndicationContent(reader.ReadElementString());
          }
          else if (reader.IsStartElement("pubDate") || reader.IsStartElement("issued"))
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

    internal void ReadXml(XmlReader reader, SyndicationFeed result)
    {
      reader.ReadStartElement("feed");
      while (reader.IsStartElement())
      {
        if (reader.IsStartElement("title"))
        {
          result.Title = new TextSyndicationContent(reader.ReadElementString());
        }
        else if (reader.IsStartElement("link"))
        {
          result.Links.Add(new SyndicationLink(new Uri(reader.GetAttribute("href"))));
          reader.ReadElementString();
        }
        else if (reader.IsStartElement("description") || reader.IsStartElement("tagline"))
        {
          result.Description = new TextSyndicationContent(reader.ReadElementString());
        }
        else if (reader.IsStartElement("pubDate") || reader.IsStartElement("modified"))
        {
          result.LastUpdatedTime = DateTime.Parse(reader.ReadElementString());
        }
        else if (reader.IsStartElement("item") || reader.IsStartElement("entry"))
        {
          break;
        }
        else
        {
          reader.Skip();
        }
      }
      result.Items = ReadItems(reader);
      logger.Info("Done");
    }
  }
}
