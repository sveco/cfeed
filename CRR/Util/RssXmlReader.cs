using System;
using System.Globalization;
using System.IO;
using System.Xml;

namespace cFeed.Util
{
  class RssXmlReader : XmlTextReader
  {
    private bool readingDate;
    //const string CustomUtcDateTimeFormat = "ddd MMM dd HH:mm:ss Z yyyy"; // Wed Oct 07 08:00:07 GMT 2009
    const string CustomUtcDateTimeFormat = "ddd MMM dd HH:mm:ss Z"; //Thu, 05 Oct 2017 05:09:42 PDT

    public RssXmlReader(Stream s) : base(s) { }

    public RssXmlReader(string inputUri) : base(inputUri) { }

    public override void ReadStartElement()
    {
      readingDate |= (string.Equals(base.NamespaceURI, string.Empty, StringComparison.InvariantCultureIgnoreCase) &&
          (string.Equals(base.LocalName, "lastBuildDate", StringComparison.InvariantCultureIgnoreCase) ||
           string.Equals(base.LocalName, "pubDate", StringComparison.InvariantCultureIgnoreCase)));
      base.ReadStartElement();
    }

    public override void ReadEndElement()
    {
      if (readingDate)
      {
        readingDate = false;
      }
      base.ReadEndElement();
    }

    public override string ReadString()
    {
      if (readingDate)
      {
        string dateString = base.ReadString();
        //this is one ugly hack
        string correctedDateString = dateString
          .Replace("EDT", "-0400")
          .Replace("EST", "-0500")
          .Replace("PDT", "-0700")
          .Replace("PST", "-0800");


        if (!DateTime.TryParse(correctedDateString, out DateTime dt))
        {
          dt = DateTime.ParseExact(correctedDateString, CustomUtcDateTimeFormat, CultureInfo.InvariantCulture);
        }
        return dt.ToUniversalTime().ToString("R", CultureInfo.InvariantCulture);
      }
      else
      {
        return base.ReadString();
      }
    }
  }

}
