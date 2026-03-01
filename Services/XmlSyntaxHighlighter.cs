using System;
using System.Collections.Generic;

namespace DevPad.Services;

/// <summary>
/// Character-level tokenizer for formatted XML.
/// Produces tokens grouped by line, ready for syntax-highlighted rendering.
/// </summary>
public static class XmlSyntaxHighlighter
{
    public static List<List<XmlToken>> TokenizeByLine(string xml)
    {
        xml = xml.Replace("\r\n", "\n").Replace("\r", "\n");
        return GroupByLine(Tokenize(xml));
    }

    // ── Tokeniser ─────────────────────────────────────────────────────────

    private static List<XmlToken> Tokenize(string xml)
    {
        var tokens = new List<XmlToken>();
        int i = 0, n = xml.Length;

        while (i < n)
        {
            // ── Whitespace outside tags ───────────────────────────────────
            if (xml[i] is ' ' or '\t' or '\r' or '\n')
            {
                int s = i;
                while (i < n && xml[i] is ' ' or '\t' or '\r' or '\n') i++;
                tokens.Add(new(xml[s..i], XmlTokenType.Whitespace));
                continue;
            }

            // ── Tag or special markup ─────────────────────────────────────
            if (xml[i] == '<')
            {
                // Comment: <!-- ... -->
                if (StartsWith(xml, i, "<!--"))
                {
                    int end = xml.IndexOf("-->", i + 4, StringComparison.Ordinal);
                    end = end < 0 ? n - 3 : end;
                    tokens.Add(new(xml[i..(end + 3)], XmlTokenType.Comment));
                    i = end + 3;
                }
                // CDATA: <![CDATA[ ... ]]>
                else if (StartsWith(xml, i, "<![CDATA["))
                {
                    int end = xml.IndexOf("]]>", i + 9, StringComparison.Ordinal);
                    end = end < 0 ? n - 3 : end;
                    tokens.Add(new(xml[i..(end + 3)], XmlTokenType.CData));
                    i = end + 3;
                }
                // Declaration / processing instruction: <?...?>
                else if (StartsWith(xml, i, "<?"))
                {
                    int end = xml.IndexOf("?>", i + 2, StringComparison.Ordinal);
                    end = end < 0 ? n - 2 : end;
                    tokens.Add(new(xml[i..(end + 2)], XmlTokenType.Declaration));
                    i = end + 2;
                }
                // Closing tag: </name>
                else if (StartsWith(xml, i, "</"))
                {
                    tokens.Add(new("</", XmlTokenType.TagPunct));
                    i += 2;
                    int s = i;
                    while (i < n && xml[i] != '>') i++;
                    tokens.Add(new(xml[s..i].Trim(), XmlTokenType.TagName));
                    if (i < n) { tokens.Add(new(">", XmlTokenType.TagPunct)); i++; }
                }
                // Opening tag: <name ...>
                else
                {
                    tokens.Add(new("<", XmlTokenType.TagPunct));
                    i++;

                    // Tag name
                    int s = i;
                    while (i < n && xml[i] is not (' ' or '\t' or '\r' or '\n' or '>' or '/')) i++;
                    tokens.Add(new(xml[s..i], XmlTokenType.TagName));

                    // Attributes until > or />
                    while (i < n && !(xml[i] == '>' || (xml[i] == '/' && i + 1 < n && xml[i + 1] == '>')))
                    {
                        char a = xml[i];
                        if (a is ' ' or '\t' or '\r' or '\n')
                        {
                            int ws = i;
                            while (i < n && xml[i] is ' ' or '\t' or '\r' or '\n') i++;
                            tokens.Add(new(xml[ws..i], XmlTokenType.Whitespace));
                        }
                        else if (a == '=')
                        {
                            tokens.Add(new("=", XmlTokenType.TagPunct)); i++;
                        }
                        else if (a == '"' || a == '\'')
                        {
                            char q = a;
                            int vs = i++;
                            while (i < n && xml[i] != q) i++;
                            if (i < n) i++;
                            tokens.Add(new(xml[vs..i], XmlTokenType.AttributeValue));
                        }
                        else
                        {
                            int ns = i;
                            while (i < n && xml[i] is not ('=' or ' ' or '\t' or '>' or '/')) i++;
                            tokens.Add(new(xml[ns..i], XmlTokenType.AttributeName));
                        }
                    }

                    if (i < n && xml[i] == '/')
                    {
                        tokens.Add(new("/>", XmlTokenType.TagPunct)); i += 2;
                    }
                    else if (i < n && xml[i] == '>')
                    {
                        tokens.Add(new(">", XmlTokenType.TagPunct)); i++;
                    }
                }
            }
            else
            {
                // ── Text content ──────────────────────────────────────────
                int s = i;
                while (i < n && xml[i] != '<') i++;
                tokens.Add(new(xml[s..i], XmlTokenType.TextContent));
            }
        }

        return tokens;
    }

    private static bool StartsWith(string s, int pos, string prefix)
    {
        if (pos + prefix.Length > s.Length) return false;
        return s.AsSpan(pos, prefix.Length).SequenceEqual(prefix.AsSpan());
    }

    // ── Line grouper ──────────────────────────────────────────────────────

    private static List<List<XmlToken>> GroupByLine(List<XmlToken> flat)
    {
        var lines   = new List<List<XmlToken>>();
        var current = new List<XmlToken>();

        foreach (var tok in flat)
        {
            bool hasNewline = tok.Text.Contains('\n') &&
                              tok.Type is XmlTokenType.Whitespace
                                       or XmlTokenType.Comment
                                       or XmlTokenType.CData
                                       or XmlTokenType.Declaration
                                       or XmlTokenType.TextContent;

            if (!hasNewline)
            {
                current.Add(tok);
                continue;
            }

            // For tokens that span multiple lines (comments, CDATA, long text)
            // emit each sub-line as its own TextContent/Comment/etc. token
            string[] parts = tok.Text.Split('\n');
            for (int p = 0; p < parts.Length; p++)
            {
                if (parts[p].Length > 0)
                    current.Add(new(parts[p], tok.Type));

                if (p < parts.Length - 1)
                {
                    lines.Add(current);
                    current = new List<XmlToken>();
                }
            }
        }

        lines.Add(current);
        return lines;
    }
}
