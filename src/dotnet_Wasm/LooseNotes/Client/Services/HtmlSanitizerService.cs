using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace Xcaciv.LooseNotes.Client.Services;

/// <summary>
/// Service to sanitize HTML content to prevent XSS attacks
/// </summary>
public class HtmlSanitizerService : IHtmlSanitizerService
{
    private static readonly HashSet<string> AllowedTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "p", "div", "span", "br", "hr", "h1", "h2", "h3", "h4", "h5", "h6",
        "ul", "ol", "li", "blockquote", "pre", "code", "em", "strong", "i", "b",
        "a", "img", "table", "tr", "td", "th", "thead", "tbody", "caption"
    };

    private static readonly HashSet<string> AllowedAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        "href", "src", "alt", "title", "class", "style", "width", "height"
    };

    private static readonly HashSet<string> UriAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        "href", "src"
    };

    private static readonly HashSet<string> AllowedProtocols = new(StringComparer.OrdinalIgnoreCase)
    {
        "http", "https", "mailto", "tel"
    };

    public string Sanitize(string html)
    {
        if (string.IsNullOrEmpty(html))
        {
            return string.Empty;
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Process all nodes recursively
        ProcessNodes(doc.DocumentNode);

        // Get the sanitized HTML
        return doc.DocumentNode.InnerHtml;
    }

    private void ProcessNodes(HtmlNode node)
    {
        // Make a copy of the children nodes to avoid collection modification issues
        var childNodes = node.ChildNodes.ToList();

        foreach (var child in childNodes)
        {
            if (child.NodeType == HtmlNodeType.Element)
            {
                // Check if the tag is allowed
                if (!AllowedTags.Contains(child.Name))
                {
                    // Replace the node with its inner content
                    child.ParentNode.ReplaceChild(HtmlNode.CreateNode(child.InnerHtml), child);
                }
                else
                {
                    // Process attributes
                    ProcessAttributes(child);
                    
                    // Process this node's children
                    ProcessNodes(child);
                }
            }
            else if (child.NodeType == HtmlNodeType.Text)
            {
                // Leave text nodes as they are
            }
            else
            {
                // Remove other types of nodes (comments, CDATA, etc.)
                child.Remove();
            }
        }
    }

    private void ProcessAttributes(HtmlNode node)
    {
        // Make a copy of the attributes to avoid collection modification issues
        var attributes = node.Attributes.ToList();

        foreach (var attribute in attributes)
        {
            // Check if the attribute is allowed
            if (!AllowedAttributes.Contains(attribute.Name))
            {
                node.Attributes.Remove(attribute);
                continue;
            }

            // Special handling for URI attributes
            if (UriAttributes.Contains(attribute.Name))
            {
                string value = attribute.Value.Trim();
                
                // Check if the URI has a safe protocol
                bool safeProtocol = false;
                foreach (var protocol in AllowedProtocols)
                {
                    if (value.StartsWith($"{protocol}:", StringComparison.OrdinalIgnoreCase))
                    {
                        safeProtocol = true;
                        break;
                    }
                }

                if (!safeProtocol && !value.StartsWith("/") && !value.StartsWith("#"))
                {
                    // If the protocol is not allowed, remove the attribute
                    node.Attributes.Remove(attribute);
                }
            }
            
            // Sanitize CSS in style attributes
            if (attribute.Name.Equals("style", StringComparison.OrdinalIgnoreCase))
            {
                attribute.Value = SanitizeStyle(attribute.Value);
            }
        }
    }

    private string SanitizeStyle(string style)
    {
        if (string.IsNullOrEmpty(style))
        {
            return string.Empty;
        }

        // Remove potentially dangerous CSS
        var sanitized = Regex.Replace(style, @"expression\s*\(|javascript:|behavior:|vbscript:|mocha:", "", RegexOptions.IgnoreCase);
        
        // Remove url(...) values from CSS to prevent loading external resources
        sanitized = Regex.Replace(sanitized, @"url\s*\(\s*['""]?\s*(?!data:image/).*?['""]?\s*\)", "", RegexOptions.IgnoreCase);
        
        return sanitized;
    }
}