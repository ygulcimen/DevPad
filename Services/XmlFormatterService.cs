using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using DevPad.Models;

namespace DevPad.Services;

public class XmlFormatterService
{
    /// <summary>Pretty-prints XML with 2-space indentation. Returns null on parse error.</summary>
    public string? Format(string xml, out string? error)
    {
        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var sb = new StringBuilder();
            using var writer = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true, IndentChars = "  " });
            doc.Save(writer);
            error = null;
            return sb.ToString();
        }
        catch (XmlException ex)
        {
            error = $"Line {ex.LineNumber}, col {ex.LinePosition}: {ex.Message}";
            return null;
        }
    }

    /// <summary>
    /// Single-parse: formats and builds the tree in one go.
    /// Returns the formatted string, tree root list, node count, and any error.
    /// A non-null Error means the input is invalid XML.
    /// </summary>
    public (string? Formatted, List<XmlTreeNode> Tree, int NodeCount, string? Error)
        FormatAndBuildTree(string xml)
    {
        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);

            var sb = new StringBuilder();
            using (var writer = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true, IndentChars = "  " }))
                doc.Save(writer);
            string formatted = sb.ToString();

            if (doc.DocumentElement == null)
                return (formatted, new List<XmlTreeNode>(), 0, null);

            int count = 0;
            var root  = BuildNode(doc.DocumentElement, ref count);
            return (formatted, new List<XmlTreeNode> { root }, count, null);
        }
        catch (XmlException ex)
        {
            return (null, new List<XmlTreeNode>(), 0,
                    $"Line {ex.LineNumber}, col {ex.LinePosition}: {ex.Message}");
        }
    }

    /// <summary>Returns whether the input is valid XML and any parse error message.</summary>
    public (bool IsValid, string? Error) Validate(string xml)
    {
        try
        {
            new XmlDocument().LoadXml(xml);
            return (true, null);
        }
        catch (XmlException ex)
        {
            return (false, $"Line {ex.LineNumber}, col {ex.LinePosition}: {ex.Message}");
        }
    }

    /// <summary>Parses XML into a tree of XmlTreeNode and returns the root plus total node count.</summary>
    public (List<XmlTreeNode> Tree, int NodeCount) BuildTree(string xml)
    {
        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            if (doc.DocumentElement == null) return (new List<XmlTreeNode>(), 0);
            int count = 0;
            var root = BuildNode(doc.DocumentElement, ref count);
            return (new List<XmlTreeNode> { root }, count);
        }
        catch
        {
            return (new List<XmlTreeNode>(), 0);
        }
    }

    /// <summary>
    /// Runs an XPath query against the XML and returns the outer XML of each matching node.
    /// Returns an error message if the query is invalid.
    /// </summary>
    public (List<string> Results, string? Error) ExecuteXPath(string xml, string xpath)
    {
        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var nodes = doc.SelectNodes(xpath);
            var results = new List<string>();
            if (nodes != null)
                foreach (XmlNode node in nodes)
                    results.Add(node.NodeType == XmlNodeType.Attribute
                        ? $"@{node.Name}=\"{node.Value}\""
                        : node.OuterXml ?? node.Value ?? string.Empty);
            return (results, null);
        }
        catch (Exception ex)
        {
            return (new List<string>(), ex.Message);
        }
    }

    private static XmlTreeNode BuildNode(XmlNode node, ref int count)
    {
        count++;
        var treeNode = new XmlTreeNode { Name = node.LocalName, NodeKind = XmlNodeKind.Element };

        // Attributes shown as child nodes prefixed with @
        if (node.Attributes != null)
            foreach (XmlAttribute attr in node.Attributes)
            {
                count++;
                treeNode.Children.Add(new XmlTreeNode
                {
                    Name     = attr.LocalName,
                    Value    = attr.Value,
                    NodeKind = XmlNodeKind.Attribute
                });
            }

        // Child elements, non-whitespace text, and comments
        foreach (XmlNode child in node.ChildNodes)
        {
            switch (child.NodeType)
            {
                case XmlNodeType.Element:
                    treeNode.Children.Add(BuildNode(child, ref count));
                    break;

                case XmlNodeType.Text when !string.IsNullOrWhiteSpace(child.Value):
                    count++;
                    var text = child.Value!.Trim();
                    treeNode.Children.Add(new XmlTreeNode
                    {
                        // Truncate very long text nodes so the tree stays readable
                        Name     = text.Length > 120 ? text[..120] + "…" : text,
                        NodeKind = XmlNodeKind.Text
                    });
                    break;

                case XmlNodeType.Comment:
                    count++;
                    treeNode.Children.Add(new XmlTreeNode
                    {
                        Name     = child.Value?.Trim() ?? string.Empty,
                        NodeKind = XmlNodeKind.Comment
                    });
                    break;
            }
        }

        return treeNode;
    }
}
