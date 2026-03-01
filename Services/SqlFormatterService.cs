using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DevPad.Services;

public enum SqlTokenType { Keyword, StringLiteral, LineComment, BlockComment, Number, Default }

/// <summary>One contiguous chunk of SQL text with a token type for syntax colouring.</summary>
public record SqlToken(string Text, SqlTokenType Type);

public class SqlFormatterService
{
    // ──────────────────────────────────────────────────────────────
    // Formatting
    // ──────────────────────────────────────────────────────────────

    // Major clause keywords that start on their own line
    private static readonly string[] ClauseKeywords =
    [
        "SELECT", "DISTINCT",
        "FROM",
        "WHERE",
        "GROUP BY", "ORDER BY", "HAVING",
        "LIMIT", "TOP", "FETCH",
        "UNION ALL", "UNION", "INTERSECT", "EXCEPT",
        "INSERT INTO", "INSERT",
        "VALUES",
        "UPDATE",
        "DELETE FROM", "DELETE",
        "SET",
        "CREATE TABLE", "CREATE VIEW", "CREATE INDEX", "CREATE",
        "ALTER TABLE", "ALTER",
        "DROP TABLE", "DROP",
        "WITH",
    ];

    // JOIN variants also get their own line
    private static readonly string[] JoinKeywords =
    [
        "LEFT OUTER JOIN", "RIGHT OUTER JOIN", "FULL OUTER JOIN",
        "LEFT JOIN", "RIGHT JOIN", "FULL JOIN",
        "INNER JOIN", "CROSS JOIN",
        "JOIN",
    ];

    // Single compiled regex covering all clause + join keywords, sorted longest-first so that
    // "UNION ALL" wins over "UNION", "INNER JOIN" wins over "JOIN", "INSERT INTO" wins over "INSERT", etc.
    // This prevents the double-\n and split-JOIN bugs that the two separate loops caused.
    private static readonly Regex ClauseAndJoinRe = new(
        string.Join("|",
            ClauseKeywords.Concat(JoinKeywords)
                .OrderByDescending(k => k.Length)
                .Select(k => $@"\b{Regex.Escape(k)}\b")),
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Condition combiners get an indented new line inside WHERE/HAVING
    private static readonly Regex AndOrRe =
        new(@"\b(AND|OR)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex OnRe =
        new(@"\b(ON)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Placeholder to protect string literals during reformatting
    private const string LiteralPlaceholder = "\x02STR{0}\x03";
    private static readonly Regex LiteralRe =
        new(@"'(?:[^']|'')*'", RegexOptions.Compiled);

    /// <summary>Returns pretty-printed SQL with uppercase keywords.</summary>
    public string Format(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql)) return string.Empty;

        // Step 1 — protect string literals
        var literals = new List<string>();
        string work = LiteralRe.Replace(sql, m =>
        {
            literals.Add(m.Value);
            return string.Format(LiteralPlaceholder, literals.Count - 1);
        });

        // Step 2 — collapse whitespace to single spaces
        work = Regex.Replace(work.Trim(), @"\s+", " ");

        // Step 3 — insert newlines before all clause + join keywords in one pass.
        // The regex is ordered longest-first, so "UNION ALL" matches before "UNION",
        // "INNER JOIN" before "JOIN", "INSERT INTO" before "INSERT", etc.
        // This prevents both the blank-line bug and the split-keyword bug.
        work = ClauseAndJoinRe.Replace(work, m => "\n" + m.Value.ToUpperInvariant());

        // Step 4 — indent AND/OR and ON inside clauses
        work = AndOrRe.Replace(work, m => "\n  " + m.Value.ToUpperInvariant());
        work = OnRe.Replace(work,    m => "\n    " + m.Value.ToUpperInvariant());

        // Step 6 — uppercase remaining known keywords
        work = UppercaseKeywords(work);

        // Step 7 — restore string literals
        for (int i = 0; i < literals.Count; i++)
            work = work.Replace(string.Format(LiteralPlaceholder, i), literals[i]);

        // Clean up: trim each line, skip blank lines, use \n only (not \r\n).
        // Using \r\n would leave a \r at the end of every token, causing Avalonia TextBlocks
        // to render with extra height and a visible gap between output lines.
        var sb = new StringBuilder();
        foreach (var line in work.Split('\n'))
        {
            var t = line.TrimEnd();
            if (t.Length > 0)
                sb.Append(t).Append('\n');
        }
        return sb.ToString().TrimEnd();
    }

    private static readonly string[] AllKeywords =
    [
        "SELECT", "DISTINCT", "FROM", "WHERE", "AND", "OR", "NOT",
        "IN", "LIKE", "BETWEEN", "IS", "NULL", "EXISTS", "ANY", "ALL",
        "AS", "UNION", "INTERSECT", "EXCEPT",
        "JOIN", "LEFT", "RIGHT", "INNER", "OUTER", "CROSS", "FULL", "ON",
        "GROUP", "ORDER", "BY", "HAVING", "LIMIT", "TOP", "FETCH", "OFFSET",
        "INSERT", "INTO", "VALUES", "UPDATE", "SET", "DELETE",
        "CREATE", "ALTER", "DROP", "TABLE", "VIEW", "INDEX",
        "ASC", "DESC", "CASE", "WHEN", "THEN", "ELSE", "END",
        "WITH", "PRIMARY", "KEY", "FOREIGN", "REFERENCES", "UNIQUE",
        "DEFAULT", "CONSTRAINT", "CHECK",
    ];

    private static string UppercaseKeywords(string sql)
    {
        foreach (var kw in AllKeywords)
            sql = Regex.Replace(sql, $@"\b{kw}\b", kw, RegexOptions.IgnoreCase);
        return sql;
    }

    // ──────────────────────────────────────────────────────────────
    // Tokenising for syntax colouring
    // ──────────────────────────────────────────────────────────────

    private static readonly HashSet<string> KeywordSet = new(
        AllKeywords, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Splits already-formatted SQL into coloured tokens.
    /// Respects string literals, line comments, and block comments.
    /// </summary>
    public IReadOnlyList<SqlToken> Tokenize(string sql)
    {
        var tokens = new List<SqlToken>();
        int i = 0;

        while (i < sql.Length)
        {
            // Line comment  --
            if (i + 1 < sql.Length && sql[i] == '-' && sql[i + 1] == '-')
            {
                int end = sql.IndexOf('\n', i);
                if (end < 0) end = sql.Length;
                tokens.Add(new SqlToken(sql[i..end], SqlTokenType.LineComment));
                i = end;
                continue;
            }

            // Block comment  /* */
            if (i + 1 < sql.Length && sql[i] == '/' && sql[i + 1] == '*')
            {
                int end = sql.IndexOf("*/", i + 2, StringComparison.Ordinal);
                if (end < 0) end = sql.Length - 2;
                end += 2;
                tokens.Add(new SqlToken(sql[i..end], SqlTokenType.BlockComment));
                i = end;
                continue;
            }

            // String literal  '...'
            if (sql[i] == '\'')
            {
                int end = i + 1;
                while (end < sql.Length)
                {
                    if (sql[end] == '\'' && end + 1 < sql.Length && sql[end + 1] == '\'')
                    { end += 2; continue; }   // escaped quote ''
                    if (sql[end] == '\'') { end++; break; }
                    end++;
                }
                tokens.Add(new SqlToken(sql[i..end], SqlTokenType.StringLiteral));
                i = end;
                continue;
            }

            // Number literal
            if (char.IsDigit(sql[i]) || (sql[i] == '-' && i + 1 < sql.Length && char.IsDigit(sql[i + 1])))
            {
                int end = i + (sql[i] == '-' ? 1 : 0);
                while (end < sql.Length && (char.IsDigit(sql[end]) || sql[end] == '.')) end++;
                tokens.Add(new SqlToken(sql[i..end], SqlTokenType.Number));
                i = end;
                continue;
            }

            // Word (keyword or identifier)
            if (char.IsLetter(sql[i]) || sql[i] == '_')
            {
                int end = i;
                while (end < sql.Length && (char.IsLetterOrDigit(sql[end]) || sql[end] == '_')) end++;
                var word = sql[i..end];
                tokens.Add(new SqlToken(word,
                    KeywordSet.Contains(word) ? SqlTokenType.Keyword : SqlTokenType.Default));
                i = end;
                continue;
            }

            // Everything else: whitespace, punctuation, operators — emit char-by-char
            // Group consecutive non-special characters together for performance
            int defEnd = i + 1;
            while (defEnd < sql.Length
                   && sql[defEnd] != '\''
                   && sql[defEnd] != '-'
                   && sql[defEnd] != '/'
                   && !char.IsLetter(sql[defEnd])
                   && !char.IsDigit(sql[defEnd])
                   && sql[defEnd] != '_')
            {
                defEnd++;
            }
            tokens.Add(new SqlToken(sql[i..defEnd], SqlTokenType.Default));
            i = defEnd;
        }

        return tokens;
    }
}
