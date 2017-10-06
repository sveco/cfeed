using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel.Syndication;

namespace cFeed.Util
{
  class CustomSyndicationFeed: SyndicationFeed
  {
    protected override bool TryParseAttribute(string name, string ns, string value, string version)
    {
      if (name == "lastBuildDate")
      {
        return false;
      }
      else
      {
        return base.TryParseAttribute(name, ns, value, version);
      }
    }
  }
}
