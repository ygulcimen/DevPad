using System;
using System.Collections.Generic;

namespace DevPad.Services;

/// <summary>
/// Character-level tokenizer for formatted JSON.
/// Produces tokens grouped by line, ready for syntax-highlighted rendering.
/// </summary>
public static class JsonSyntaxHighlighter
{
    /// <summary>
    /// Tokenizes <paramref name="json"/> and groups the result into lines.
    /// Each inner list represents one line's worth of tokens (no newline characters included).
    /// </summary>
    public static List<List<JsonToken>> TokenizeByLine(string json)
    {
        // Normalise line endings so the splitter only needs to handle \n
        json = json.Replace("\r\n", "\n").Replace("\r", "\n");
        return GroupByLine(Tokenize(json));
    }

    // ── Tokeniser ─────────────────────────────────────────────────────────

    private static List<JsonToken> Tokenize(string json)
    {
        var tokens = new List<JsonToken>();
        var ctx = new System.Collections.Generic.Stack<bool>(); // true = in object
        bool nextIsKey = false;

        int i = 0, n = json.Length;
        while (i < n)
        {
            char c = json[i];

            // ── Whitespace ────────────────────────────────────────────────
            if (c is ' ' or '\t' or '\r' or '\n')
            {
                int s = i;
                while (i < n && json[i] is ' ' or '\t' or '\r' or '\n') i++;
                tokens.Add(new(json[s..i], JsonTokenType.Whitespace));
                continue;
            }

            // ── Quoted string ─────────────────────────────────────────────
            if (c == '"')
            {
                int s = i++;
                while (i < n)
                {
                    if (json[i] == '\\') { i += 2; continue; }
                    if (json[i] == '"')  { i++; break; }
                    i++;
                }
                var type = nextIsKey ? JsonTokenType.Key : JsonTokenType.StringValue;
                if (nextIsKey) nextIsKey = false;
                tokens.Add(new(json[s..i], type));
                continue;
            }

            // ── Number ────────────────────────────────────────────────────
            if (c == '-' || (c >= '0' && c <= '9'))
            {
                int s = i;
                if (c == '-') i++;
                while (i < n && IsNumeric(json[i])) i++;
                tokens.Add(new(json[s..i], JsonTokenType.Number));
                continue;
            }

            // ── Keywords ──────────────────────────────────────────────────
            if (Match(json, i, "true"))  { tokens.Add(new("true",  JsonTokenType.Boolean)); i += 4; continue; }
            if (Match(json, i, "false")) { tokens.Add(new("false", JsonTokenType.Boolean)); i += 5; continue; }
            if (Match(json, i, "null"))  { tokens.Add(new("null",  JsonTokenType.Null));    i += 4; continue; }

            // ── Structural characters ─────────────────────────────────────
            switch (c)
            {
                case '{':
                    ctx.Push(true); nextIsKey = true;
                    tokens.Add(new("{", JsonTokenType.Punctuation)); break;
                case '}':
                    if (ctx.Count > 0) ctx.Pop();
                    nextIsKey = ctx.Count > 0 && ctx.Peek();
                    tokens.Add(new("}", JsonTokenType.Punctuation)); break;
                case '[':
                    ctx.Push(false); nextIsKey = false;
                    tokens.Add(new("[", JsonTokenType.Punctuation)); break;
                case ']':
                    if (ctx.Count > 0) ctx.Pop();
                    nextIsKey = ctx.Count > 0 && ctx.Peek();
                    tokens.Add(new("]", JsonTokenType.Punctuation)); break;
                case ':':
                    nextIsKey = false;
                    tokens.Add(new(":", JsonTokenType.Punctuation)); break;
                case ',':
                    nextIsKey = ctx.Count > 0 && ctx.Peek();
                    tokens.Add(new(",", JsonTokenType.Punctuation)); break;
                default:
                    tokens.Add(new(c.ToString(), JsonTokenType.Punctuation)); break;
            }
            i++;
        }

        return tokens;
    }

    private static bool IsNumeric(char c) =>
        c >= '0' && c <= '9' || c == '.' || c == 'e' || c == 'E' || c == '+' || c == '-';

    private static bool Match(string s, int pos, string word)
    {
        if (pos + word.Length > s.Length) return false;
        return s.AsSpan(pos, word.Length).SequenceEqual(word.AsSpan());
    }

    // ── Line grouper ──────────────────────────────────────────────────────

    private static List<List<JsonToken>> GroupByLine(List<JsonToken> flat)
    {
        var lines   = new List<List<JsonToken>>();
        var current = new List<JsonToken>();

        foreach (var tok in flat)
        {
            if (tok.Type == JsonTokenType.Whitespace && tok.Text.Contains('\n'))
            {
                string[] parts = tok.Text.Split('\n');

                // Remainder of the current line (before first \n)
                if (parts[0].Length > 0)
                    current.Add(new(parts[0], JsonTokenType.Whitespace));
                lines.Add(current);

                // Whole lines in the middle
                for (int m = 1; m < parts.Length - 1; m++)
                {
                    var mid = new List<JsonToken>();
                    if (parts[m].Length > 0)
                        mid.Add(new(parts[m], JsonTokenType.Whitespace));
                    lines.Add(mid);
                }

                // Start of the next line (after last \n)
                current = new List<JsonToken>();
                if (parts[^1].Length > 0)
                    current.Add(new(parts[^1], JsonTokenType.Whitespace));
            }
            else
            {
                current.Add(tok);
            }
        }

        lines.Add(current);
        return lines;
    }
}
