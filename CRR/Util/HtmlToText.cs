

namespace cFeed.Util
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.IO;
	using System.Linq;
	using cFeed.Logging;
	using HtmlAgilityPack;
	using JsonConfig;

	public class HtmlToText
	{
		static NLog.Logger logger = Log.Instance.Logger;

		private Uri BaseUri;
		private string imageLinkHighlight = Configuration.GetForegroundColor(Config.Global.UI.Colors.ImageLinkHighlight);
		private string imageLinkTextHighlight = Configuration.GetForegroundColor(Config.Global.UI.Colors.ImageLinkTextHighlight);
		private Collection<Uri> Images = new Collection<Uri>();
		private string linkHighlight = Configuration.GetForegroundColor(Config.Global.UI.Colors.LinkHighlight);
		private Collection<Uri> Links = new Collection<Uri>();
		private string linkTextHighlight = Configuration.GetForegroundColor(Config.Global.UI.Colors.LinkTextHighlight);
		private string resetColor = Configuration.ColorReset;

		private char[] trimChars = { ' ', '\t', '\n' };

		public List<string> Filters { get; internal set; }

		public string Select { get; internal set; }
		public int LinkStartFrom { get; set; }

		/// <summary>
		/// Render links as plain text, without link markers and numbers. Links are not collected either.
		/// </summary>
		public bool StripLinks { get; internal set; }

		/// <summary>
		/// Nodes matched by the XPath entries of <see cref="Filters"/>. Skipped when converting.
		/// </summary>
		private HashSet<HtmlNode> excludedNodes = new HashSet<HtmlNode>();

		public HtmlToText()
		{
		}

		public string Convert(string path)
		{
			HtmlDocument doc = new HtmlDocument();
			doc.Load(path);
			ResolveXPathFilters(doc);

			StringWriter sw = new StringWriter();
			ConvertTo(doc.DocumentNode, sw);
			sw.Flush();
			return sw.ToString();
		}

		public string ConvertHtml(string html, Uri baseUri, out Collection<Uri> links, out Collection<Uri> images)
		{
			HtmlDocument doc = new HtmlDocument();
			doc.LoadHtml(html);
			ResolveXPathFilters(doc);
			BaseUri = baseUri;
			StringWriter sw = new StringWriter();
			HtmlNode rootNode = doc.DocumentNode;
			if (!string.IsNullOrWhiteSpace(Select)) {
				rootNode = rootNode.SelectSingleNode(Select);
			}
			if (rootNode != null) {
				ConvertTo(rootNode, sw);
			} else {
				logger.Warn(string.Format("Root document node empty, check selector. {0}", baseUri));
			}
			sw.Flush();
			links = this.Links;
			images = this.Images;
			return sw.ToString();
		}

		public void ConvertTo(HtmlNode node, TextWriter outText)
		{
			if (excludedNodes.Contains(node))
				return;

			if (Filters != null)
			{
				if (Filters.Select(x => x.TrimStart('#')).Contains(node.Id.Trim()))
					return;
				if (node.Attributes.Contains("class") &&
					Filters.Select(x => x.TrimStart('.')).Contains(node.Attributes["class"].Value.Trim()))
					return;
			}

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
						foreach (var c in trimChars)
						{
							html = html.Replace(c, ' ');
						}

						outText.Write(HtmlEntity.DeEntitize(html).Trim(trimChars));
					}
					break;

				case HtmlNodeType.Element:
					bool skip = false;
					switch (node.Name.ToLower())
					{
						case "title":
							if (node.HasChildNodes)
							{
								ConvertContentTo(node, outText);
							}
							skip = true;
							break;

						case "meta":
							//extract description
							if (node.GetAttributeValue("name", "") == "description")
							{
								outText.Write(Environment.NewLine);
								outText.Write(node.GetAttributeValue("content", ""));
								outText.Write(Environment.NewLine);
							}
							break;

						//handle headers
						case "h1":
						case "h2":
						case "h3":
							outText.Write(Environment.NewLine);
							outText.Write(Environment.NewLine);
							if (node.HasChildNodes)
							{
								ConvertContentTo(node, outText);
							}
							outText.Write(Environment.NewLine);
							skip = true;
							break;

						case "p":
						case "ul":
						case "ol":
						case "div":
						case "br":
							// treat paragraphs as crlf
							outText.Write(Environment.NewLine);
							break;

						case "li":
							outText.Write(Environment.NewLine + "* ");
							break;

						case "img":
							outText.Write(Environment.NewLine + imageLinkTextHighlight + "[img:" + node.Attributes["alt"]?.Value + "]" + resetColor);
							if (node.Attributes.Contains("src"))
							{
								var uriName = node.Attributes["src"].Value;
								Uri uriResult;
								if (Uri.TryCreate(uriName, UriKind.Absolute, out uriResult)
									&& (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
								{
									Images.Add(uriResult);
									outText.Write(imageLinkHighlight + "[" + Images.Count + "] " + resetColor);
								}
								else if (Uri.TryCreate(BaseUri, uriName, out uriResult)
								  && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
								{
									Images.Add(uriResult);
									outText.Write(imageLinkHighlight + "[" + Images.Count + "] " + resetColor);
								}
							}
							break;

						case "strong":
							outText.Write(" " + linkTextHighlight);
							if (node.HasChildNodes)
							{
								ConvertContentTo(node, outText);
							}
							outText.Write(resetColor + " ");
							skip = true;
							break;

						case "a":
							if (StripLinks)
							{
								// Keep the link text, drop the marker and the number. Text nodes are trimmed, so
								// the spaces that separated the link from its neighbours have to be written here.
								if (EndsWithWhitespace(node.PreviousSibling))
								{
									outText.Write(" ");
								}
								if (node.HasChildNodes)
								{
									ConvertContentTo(node, outText);
								}
								if (StartsWithWhitespace(node.NextSibling))
								{
									outText.Write(" ");
								}
								skip = true;
								break;
							}

							outText.Write(linkTextHighlight + " [Link:");
							if (node.HasChildNodes)
							{
								ConvertContentTo(node, outText);
							}
							outText.Write("]" + resetColor);
							if (node.Attributes.Contains("href"))
							{
								var uriName = node.Attributes["href"].Value;
								Uri uriResult;
								if (Uri.TryCreate(uriName, UriKind.Absolute, out uriResult)
									&& (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
								{
									Links.Add(uriResult);
									outText.Write(linkHighlight + "[" + (Links.Count + LinkStartFrom) + "] " + resetColor);
								}
								else if (Uri.TryCreate(BaseUri, uriName, out uriResult)
								  && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
								{
									Links.Add(uriResult);
									outText.Write(linkHighlight + "[" + (Links.Count + LinkStartFrom) + "] " + resetColor);
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

		/// <summary>
		/// Filters that start with / or ( are XPath expressions, for example "//*[@data-block='promoList']".
		/// Class and id filters cannot match sites that generate class names, so those pages need XPath.
		/// </summary>
		private void ResolveXPathFilters(HtmlDocument doc)
		{
			excludedNodes.Clear();
			if (Filters == null) return;

			foreach (var filter in Filters.Where(f => !string.IsNullOrWhiteSpace(f) && (f.TrimStart().StartsWith("/") || f.TrimStart().StartsWith("("))))
			{
				try
				{
					var matches = doc.DocumentNode.SelectNodes(filter.Trim());
					if (matches != null)
					{
						foreach (var match in matches)
						{
							excludedNodes.Add(match);
						}
					}
				}
				catch (Exception ex)
				{
					// a typo in one filter must not break the article
					logger.Warn(string.Format("Ignoring invalid XPath filter '{0}': {1}", filter, ex.Message));
				}
			}
		}

		private static bool EndsWithWhitespace(HtmlNode sibling)
		{
			var text = sibling as HtmlTextNode;
			return text != null && text.Text.Length > 0 && char.IsWhiteSpace(text.Text[text.Text.Length - 1]);
		}

		private static bool StartsWithWhitespace(HtmlNode sibling)
		{
			var text = sibling as HtmlTextNode;
			return text != null && text.Text.Length > 0 && char.IsWhiteSpace(text.Text[0]);
		}

		private void ConvertContentTo(HtmlNode node, TextWriter outText)
		{
			foreach (HtmlNode subnode in node.ChildNodes)
			{
				ConvertTo(subnode, outText);
			}
		}
	}
}