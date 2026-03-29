using System.Collections.Generic;

namespace DevPad.Services;

/// <summary>
/// Simple state-machine tokenizer for formatted C# code.
/// Produces tokens grouped by line for syntax-highlighted rendering.
/// </summary>
public static class CSharpSyntaxHighlighter
{
    private static readonly HashSet<string> Keywords = new(System.StringComparer.Ordinal)
    {
        "abstract","as","base","bool","break","byte","case","catch","char","checked",
        "class","const","continue","decimal","default","delegate","do","double","else",
        "enum","event","explicit","extern","false","finally","fixed","float","for",
        "foreach","goto","if","implicit","in","int","interface","internal","is","lock",
        "long","namespace","new","null","object","operator","out","override","params",
        "private","protected","public","readonly","ref","return","sbyte","sealed",
        "short","sizeof","stackalloc","static","string","struct","switch","this",
        "throw","true","try","typeof","uint","ulong","unchecked","unsafe","ushort",
        "using","virtual","void","volatile","while",
        // Contextual keywords
        "async","await","var","dynamic","yield","partial","get","set","value",
        "add","remove","nameof","when","record","init","with","global","file",
        "required","scoped","not","and","or","nint","nuint","managed","unmanaged"
    };

    public static List<List<CSharpToken>> TokenizeByLine(string code)
    {
        code = code.Replace("\r\n", "\n").Replace("\r", "\n");
        return GroupByLine(Tokenize(code));
    }

    private static List<CSharpToken> Tokenize(string code)
    {
        var tokens = new List<CSharpToken>();
        int i = 0, n = code.Length;

        while (i < n)
        {
            char c = code[i];

            // ── Single-line comment (// or ///) ──────────────────────────
            if (i + 1 < n && c == '/' && code[i + 1] == '/')
            {
                int s = i;
                while (i < n && code[i] != '\n') i++;
                tokens.Add(new(code[s..i], CSharpTokenType.Comment));
                continue;
            }

            // ── Block comment /* ... */ ───────────────────────────────────
            if (i + 1 < n && c == '/' && code[i + 1] == '*')
            {
                int s = i; i += 2;
                while (i + 1 < n && !(code[i] == '*' && code[i + 1] == '/')) i++;
                if (i + 1 < n) i += 2;
                tokens.Add(new(code[s..i], CSharpTokenType.Comment));
                continue;
            }

            // ── Verbatim string @"..." ────────────────────────────────────
            if (c == '@' && i + 1 < n && code[i + 1] == '"')
            {
                int s = i; i += 2;
                while (i < n)
                {
                    if (code[i] == '"')
                    {
                        i++;
                        if (i < n && code[i] == '"') { i++; continue; } // escaped ""
                        break;
                    }
                    i++;
                }
                tokens.Add(new(code[s..i], CSharpTokenType.String));
                continue;
            }

            // ── Interpolated / regular string "$..." or "..." ────────────
            if (c == '$' && i + 1 < n && code[i + 1] == '"')
            {
                int s = i; i += 2;
                int depth = 0;
                while (i < n)
                {
                    if (code[i] == '\\') { i += 2; continue; }
                    if (code[i] == '{') { depth++; i++; continue; }
                    if (code[i] == '}' && depth > 0) { depth--; i++; continue; }
                    if (code[i] == '"' && depth == 0) { i++; break; }
                    i++;
                }
                tokens.Add(new(code[s..i], CSharpTokenType.String));
                continue;
            }

            // ── Regular string "..." ──────────────────────────────────────
            if (c == '"')
            {
                int s = i++;
                while (i < n)
                {
                    if (code[i] == '\\') { i += 2; continue; }
                    if (code[i] == '"') { i++; break; }
                    if (code[i] == '\n') break; // unterminated
                    i++;
                }
                tokens.Add(new(code[s..i], CSharpTokenType.String));
                continue;
            }

            // ── Char literal '.' ──────────────────────────────────────────
            if (c == '\'')
            {
                int s = i++;
                while (i < n)
                {
                    if (code[i] == '\\') { i += 2; continue; }
                    if (code[i] == '\'') { i++; break; }
                    if (code[i] == '\n') break;
                    i++;
                }
                tokens.Add(new(code[s..i], CSharpTokenType.String));
                continue;
            }

            // ── Number ────────────────────────────────────────────────────
            if (char.IsDigit(c) || (c == '.' && i + 1 < n && char.IsDigit(code[i + 1])))
            {
                int s = i;
                if (c == '0' && i + 1 < n && (code[i + 1] == 'x' || code[i + 1] == 'X'))
                {
                    i += 2;
                    while (i < n && (IsHexDigit(code[i]) || code[i] == '_')) i++;
                }
                else if (c == '0' && i + 1 < n && (code[i + 1] == 'b' || code[i + 1] == 'B'))
                {
                    i += 2;
                    while (i < n && (code[i] == '0' || code[i] == '1' || code[i] == '_')) i++;
                }
                else
                {
                    while (i < n && (char.IsDigit(code[i]) || code[i] == '.' || code[i] == '_' ||
                                     code[i] == 'e' || code[i] == 'E')) i++;
                }
                // numeric suffix: f, d, m, l, u, ul, lu, etc.
                while (i < n && "fFdDmMlLuU".IndexOf(code[i]) >= 0) i++;
                tokens.Add(new(code[s..i], CSharpTokenType.Number));
                continue;
            }

            // ── Preprocessor directive ────────────────────────────────────
            if (c == '#')
            {
                int s = i;
                while (i < n && code[i] != '\n') i++;
                tokens.Add(new(code[s..i], CSharpTokenType.Preprocessor));
                continue;
            }

            // ── Whitespace ────────────────────────────────────────────────
            if (c == ' ' || c == '\t' || c == '\r' || c == '\n')
            {
                int s = i;
                while (i < n && (code[i] == ' ' || code[i] == '\t' || code[i] == '\r' || code[i] == '\n')) i++;
                tokens.Add(new(code[s..i], CSharpTokenType.Whitespace));
                continue;
            }

            // ── Identifier or keyword ─────────────────────────────────────
            if (char.IsLetter(c) || c == '_')
            {
                int s = i;
                while (i < n && (char.IsLetterOrDigit(code[i]) || code[i] == '_')) i++;
                var text = code[s..i];
                tokens.Add(new(text, Keywords.Contains(text) ? CSharpTokenType.Keyword : CSharpTokenType.Identifier));
                continue;
            }

            // ── Everything else: operators, punctuation ───────────────────
            tokens.Add(new(code[i..(i + 1)], CSharpTokenType.Punctuation));
            i++;
        }

        return tokens;
    }

    private static bool IsHexDigit(char c) =>
        (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');

    private static List<List<CSharpToken>> GroupByLine(List<CSharpToken> flat)
    {
        var lines   = new List<List<CSharpToken>>();
        var current = new List<CSharpToken>();

        foreach (var tok in flat)
        {
            bool hasNewline = tok.Type is CSharpTokenType.Whitespace
                                       or CSharpTokenType.Comment
                                       or CSharpTokenType.Preprocessor
                              && tok.Text.Contains('\n');

            if (!hasNewline)
            {
                current.Add(tok);
                continue;
            }

            string[] parts = tok.Text.Split('\n');
            if (parts[0].Length > 0) current.Add(new(parts[0], tok.Type));
            lines.Add(current);

            for (int m = 1; m < parts.Length - 1; m++)
            {
                var mid = new List<CSharpToken>();
                if (parts[m].Length > 0) mid.Add(new(parts[m], tok.Type));
                lines.Add(mid);
            }

            current = new List<CSharpToken>();
            if (parts[^1].Length > 0) current.Add(new(parts[^1], tok.Type));
        }

        if (current.Count > 0 || lines.Count == 0)
            lines.Add(current);

        return lines;
    }
}
