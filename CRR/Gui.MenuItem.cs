using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRR.Gui
{
    public class MenuItem
    {
        public string DisplayText;
        public string Id;
        public Uri Link;

        public MenuItem(string displayText, string id, Uri link)
        {
            DisplayText = displayText;
            Id = id;
            Link = link;
        }
    }
}
