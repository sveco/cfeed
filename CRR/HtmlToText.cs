using CRR;
using JsonConfig;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace HtmlAgilityPack.Samples
{
    public class HtmlToText
    {
        Collection<Uri> Links = new Collection<Uri>();
        Uri BaseUri;

        char[] trimChars = { ' ', '\t', '\n' };
 
        public List<string> Filters { get; internal set; }

        public HtmlToText()
        {
        }

        public string Convert(string path)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.Load(path);

            StringWriter sw = new StringWriter();
            ConvertTo(doc.DocumentNode, sw);
            sw.Flush();
            return sw.ToString();
        }

        public string ConvertHtml(string html, Uri baseUri, out Collection<Uri> links)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            BaseUri = baseUri;
            StringWriter sw = new StringWriter();
            ConvertTo(doc.DocumentNode, sw);
            sw.Flush();
            links = this.Links;
            return sw.ToString();
        }

        private void ConvertContentTo(HtmlNode node, TextWriter outText)
        {
            foreach (HtmlNode subnode in node.ChildNodes)
            {
                ConvertTo(subnode, outText);
            }
        }

        public void ConvertTo(HtmlNode node, TextWriter outText)
        {
            if (Filters.Select(x => x.TrimStart('#')).Contains(node.Id.Trim()))
                return;
            if (node.Attributes.Contains("class") &&
                Filters.Select(x => x.TrimStart('.')).Contains(node.Attributes["class"].Value.Trim()))
                return;


            string html;
            switch (node.NodeType)
            {
                case HtmlNodeType.Comment:
                    // don't output comments
                    break;

                case HtmlNodeType.Document:
                    ConvertContentTo(node, outText);
                    break;

                case HtmlNodeType.Text:
                    // script and style must not be output
                    string parentName = node.ParentNode.Name;
                    if ((parentName == "script") || (parentName == "style"))
                        break;

                    // get text
                    html = ((HtmlTextNode)node).Text;

                    // is it in fact a special closing node output as text?
                    if (HtmlNode.IsOverlappedClosingElement(html))
                        break;

                    // check the text is meaningful and not a bunch of whitespaces
                    if (html.Trim().Length > 0)
                    {
                        outText.Write(HtmlEntity.DeEntitize(html).Trim(trimChars));
                    }
                    break;

                case HtmlNodeType.Element:
                    bool skip = false;
                    switch (node.Name)
                    {
                        case "p":
                        case "ul":
                        case "ol":
                            // treat paragraphs as crlf
                            outText.Write(Environment.NewLine);
                            break;
                        case "li":
                            outText.Write(Environment.NewLine + "* ");
                            break;
                        case "img":
                            outText.Write(Environment.NewLine + "[img:" + node.Attributes["alt"]?.Value + "]");
                            break;
                        case "a":
                            outText.Write(" [Link:");
                            if (node.HasChildNodes)
                            {
                                ConvertContentTo(node, outText);
                            }
                            outText.Write("]");
                            if (node.Attributes.Contains("href"))
                            {
                                var uriName = node.Attributes["href"].Value;
                                var linkHighlight = Configuration.TextColor.ForegroundColor(Config.Global.UI.Colors.LinkHighlight);
                                var resetColor = Configuration.ColorReset;

                                Uri uriResult;
                                if (Uri.TryCreate(uriName, UriKind.Absolute, out uriResult)
                                    && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
                                {
                                    Links.Add(uriResult);
                                    outText.Write(linkHighlight + " [" + Links.Count + "] " + resetColor);
                                } else if (Uri.TryCreate(BaseUri, uriName, out uriResult)
                                    && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
                                {
                                    Links.Add(uriResult);
                                    outText.Write(linkHighlight + " [" + Links.Count + "] " + resetColor);
                                }

                            }
                            skip = true;
                            break;
                        case "i":
                            outText.Write(" ");
                            if (node.HasChildNodes)
                            {
                                ConvertContentTo(node, outText);
                            }
                            outText.Write(" ");
                            skip = true;
                            break;
                    }
                    if (!skip)
                    {
                        if (node.HasChildNodes)
                        {
                            ConvertContentTo(node, outText);
                        }
                    }
                    break;
            }
        }
    }
}