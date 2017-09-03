using CRR;
using System;
using System.Collections.Generic;
using System.IO;

namespace HtmlAgilityPack.Samples
{
    public class HtmlToText
    {
        char[] trimChars = { ' ', '\t', '\n' };
 
        public List<string> Exclude { get; internal set; }

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

        public string ConvertHtml(string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            StringWriter sw = new StringWriter();
            ConvertTo(doc.DocumentNode, sw);
            sw.Flush();
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
            if (Exclude.Contains(node.Id.Trim()))
                return;
            if (node.Attributes.Contains("class") &&
                Exclude.Contains(node.Attributes["class"].Value.Trim()))
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
                            outText.Write(Environment.NewLine + "• ");
                            break;
                        case "img":
                            outText.Write(Environment.NewLine + "[img:" + node.Attributes["alt"]?.Value + "]");
                            break;
                        case "a":
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