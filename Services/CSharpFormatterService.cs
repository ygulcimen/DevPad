using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DevPad.Services;

/// <summary>
/// Formats C# into "Claude style":
///   • 4-space indentation, Allman braces
///   • if/for/foreach without braces: body on next line, indented
///   • Method chains split one .Call() per line (threshold: 60 chars)
///   • Long signatures: one parameter per line
///   • Object initializers: one property per line, = signs aligned
///   • NO spaces around dots, nullable ? sticks to type
///   • Blank lines between logical statement blocks
/// </summary>
public sealed class CSharpFormatterService
{
    // FIX 4: lowered from 90 to 60 so chains split more naturally
    private const int WrapWidth     = 90;
    private const int ChainWrap     = 60;

    // ── Public API ────────────────────────────────────────────────────────────

    public string? Format(string source, out string? error)
    {
        error = null;
        try
        {
            var tokens   = Lex(source.Replace("\r\n", "\n").Replace("\r", "\n"));
            var noWs     = tokens.Where(t => t.Kind is not (TK.Ws or TK.Nl)).ToList();
            var emitted  = Emit(noWs);
            var indented = Indent(emitted);
            var styled   = Style(indented);
            return Cleanup(styled);
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return null;
        }
    }

    // ── Token ─────────────────────────────────────────────────────────────────

    private enum TK { Kw, Id, Num, Str, Cmt, Pre, Op, Nl, Ws }

    private readonly record struct Tok(TK Kind, string Text);

    // ── Keywords ──────────────────────────────────────────────────────────────

    private static readonly HashSet<string> Kws = new(StringComparer.Ordinal)
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
        "async","await","var","dynamic","yield","partial","get","set","value",
        "add","remove","nameof","when","record","init","with","global","file",
        "required","scoped","not","and","or","nint","nuint","managed","unmanaged",
    };

    // ── Lexer ─────────────────────────────────────────────────────────────────

    private static List<Tok> Lex(string src)
    {
        var r = new List<Tok>();
        int i = 0, n = src.Length;

        while (i < n)
        {
            char c = src[i];

            // comments
            if (c == '/' && i+1 < n && src[i+1] == '/')
            { int b=i; while (i<n&&src[i]!='\n') i++; r.Add(new(TK.Cmt,src[b..i])); continue; }

            if (c == '/' && i+1 < n && src[i+1] == '*')
            { int b=i; i+=2; while(i+1<n&&!(src[i]=='*'&&src[i+1]=='/'))i++; if(i+1<n)i+=2; r.Add(new(TK.Cmt,src[b..i])); continue; }

            // strings
            if (c=='@' && i+1<n && src[i+1]=='"')
            { int b=i; i+=2; while(i<n){if(src[i]=='"'){i++;if(i<n&&src[i]=='"'){i++;continue;}break;}i++;} r.Add(new(TK.Str,src[b..i])); continue; }

            if (c=='$' && i+1<n && src[i+1]=='"')
            { int b=i,d=0; i+=2; while(i<n){if(src[i]=='\\'){i+=2;continue;}if(src[i]=='{'){d++;i++;continue;}if(src[i]=='}'&&d>0){d--;i++;continue;}if(src[i]=='"'&&d==0){i++;break;}i++;} r.Add(new(TK.Str,src[b..i])); continue; }

            if (c=='"' && i+2<n && src[i+1]=='"' && src[i+2]=='"')
            { int b=i; i+=3; while(i+2<n&&!(src[i]=='"'&&src[i+1]=='"'&&src[i+2]=='"'))i++; if(i+2<n)i+=3; r.Add(new(TK.Str,src[b..i])); continue; }

            if (c=='"')
            { int b=i++; while(i<n){if(src[i]=='\\'){i+=2;continue;}if(src[i]=='"'){i++;break;}if(src[i]=='\n')break;i++;} r.Add(new(TK.Str,src[b..i])); continue; }

            if (c=='\'')
            { int b=i++; while(i<n){if(src[i]=='\\'){i+=2;continue;}if(src[i]=='\''){i++;break;}if(src[i]=='\n')break;i++;} r.Add(new(TK.Str,src[b..i])); continue; }

            // number
            if (char.IsDigit(c)||(c=='.'&&i+1<n&&char.IsDigit(src[i+1])))
            {
                int b=i;
                if (c=='0'&&i+1<n&&(src[i+1]|32)=='x'){i+=2;while(i<n&&(IsHex(src[i])||src[i]=='_'))i++;}
                else if(c=='0'&&i+1<n&&(src[i+1]|32)=='b'){i+=2;while(i<n&&(src[i]=='0'||src[i]=='1'||src[i]=='_'))i++;}
                else{while(i<n&&(char.IsDigit(src[i])||src[i]=='.'||src[i]=='_'||src[i]=='e'||src[i]=='E'))i++;}
                while(i<n&&"fFdDmMlLuU".Contains(src[i]))i++;
                r.Add(new(TK.Num,src[b..i])); continue;
            }

            // preprocessor
            if (c=='#'){ int b=i; while(i<n&&src[i]!='\n')i++; r.Add(new(TK.Pre,src[b..i])); continue; }

            // whitespace
            if (c=='\n'){ r.Add(new(TK.Nl,"\n")); i++; continue; }
            if (c==' '||c=='\t'||c=='\r'){ int b=i; while(i<n&&(src[i]==' '||src[i]=='\t'||src[i]=='\r'))i++; r.Add(new(TK.Ws,src[b..i])); continue; }

            // multi-char operators
            if (TryOp(src,i,out var op)){ r.Add(new(TK.Op,op)); i+=op.Length; continue; }

            // identifier / keyword
            if (char.IsLetter(c)||c=='_')
            {
                int b=i; while(i<n&&(char.IsLetterOrDigit(src[i])||src[i]=='_'))i++;
                var t=src[b..i]; r.Add(new(Kws.Contains(t)?TK.Kw:TK.Id,t)); continue;
            }

            r.Add(new(TK.Op,c.ToString())); i++;
        }

        return r;
    }

    private static bool IsHex(char c) => (c>='0'&&c<='9')||(c>='a'&&c<='f')||(c>='A'&&c<='F');

    private static readonly string[] Ops3 = { "<<=",">>=","??=","..." };
    private static readonly string[] Ops2 = { "=>","??","?.","?[","++","--","+=","-=","*=","/=",
        "%=","&=","|=","^=","<<",">>","<=",">=","==","!=","&&","||","::","->",".." };

    private static bool TryOp(string s, int i, out string op)
    {
        foreach (var o in Ops3) if (i+o.Length<=s.Length && s[i..(i+o.Length)]==o) { op=o; return true; }
        foreach (var o in Ops2) if (i+o.Length<=s.Length && s[i..(i+o.Length)]==o) { op=o; return true; }
        op=""; return false;
    }

    // ── Emit ──────────────────────────────────────────────────────────────────

    private static readonly HashSet<string> BinOps = new(StringComparer.Ordinal)
    {
        "=","+=","-=","*=","/=","%=","&=","|=","^=","<<=",">>=",
        "==","!=","<=",">=","&&","||","??","??=","=>",
        "+","-","*","/","%","&","|","^","->","is","as",
    };

    private static readonly HashSet<string> UnaryOps = new(StringComparer.Ordinal)
    { "!", "~", "+", "-", "++", "--" };

    private static readonly HashSet<string> ControlKws = new(StringComparer.Ordinal)
    { "if","for","foreach","while","switch","catch","using","lock","fixed","when" };

    private static string Emit(List<Tok> toks)
    {
        var sb  = new StringBuilder();
        int n   = toks.Count;

        // FIX 2: replaced single bool flag with depth counter
        bool nextIsControlFlowBody  = false;
        int  controlFlowParenDepth  = 0;

        Tok? At(int j) => j >= 0 && j < n ? toks[j] : null;

        bool IsUnaryCtx(int j)
        {
            for (int k = j-1; k >= 0; k--)
            {
                var t = toks[k];
                if (t.Kind == TK.Ws) continue;
                if (t.Kind == TK.Kw) return true;
                if (t.Kind == TK.Op && t.Text is "(" or "[" or "," or "=" or "=>"
                    or ":" or "&&" or "||" or "!" or "~" or "return") return true;
                if (t.Kind == TK.Op && BinOps.Contains(t.Text)) return true;
                return false;
            }
            return true;
        }

        void Append(string s) => sb.Append(s);
        void Sp() { if (sb.Length > 0 && sb[^1] != ' ' && sb[^1] != '\n') sb.Append(' '); }
        void Nl() { if (sb.Length > 0 && sb[^1] != '\n') sb.Append('\n'); }
        char Last() { for (int k=sb.Length-1;k>=0;k--) { char c=sb[k]; if(c!=' '&&c!='\n')return c; } return '\0'; }

        for (int i = 0; i < n; i++)
        {
            var tok  = toks[i];
            var next = At(i+1);
            var prev = At(i-1);

            // ── Preprocessor ──────────────────────────────────────────────
            if (tok.Kind == TK.Pre)
            { Nl(); Append(tok.Text); Nl(); continue; }

            // ── Comment ───────────────────────────────────────────────────
            if (tok.Kind == TK.Cmt)
            { Nl(); Append(tok.Text); Nl(); continue; }

            // ── Opening brace { ───────────────────────────────────────────
            if (tok.Kind == TK.Op && tok.Text == "{")
            {
                Nl(); Append("{"); Nl();
                continue;
            }

            // ── Closing brace } ───────────────────────────────────────────
            if (tok.Kind == TK.Op && tok.Text == "}")
            {
                Nl(); Append("}");
                bool joins = next is { Kind: TK.Kw } nk
                             && nk.Text is "else" or "catch" or "finally";
                if (joins) Sp(); else Nl();
                continue;
            }

            // ── Semicolon ─────────────────────────────────────────────────
            if (tok.Kind == TK.Op && tok.Text == ";")
            { Append(";"); Nl(); continue; }

            // ── Comma ─────────────────────────────────────────────────────
            if (tok.Kind == TK.Op && tok.Text == ",")
            { Append(","); Sp(); continue; }

            // ── Dot / ?. / :: — NO spaces ─────────────────────────────────
            if (tok.Kind == TK.Op && tok.Text is "." or "?." or "::")
            { Append(tok.Text); continue; }

            // FIX 1: nullable ? — stick to type, no space before it
            // If next token is a type keyword or identifier and we're in a param/field context
            // treat ? as postfix on the type → no space before, no space after
            if (tok.Kind == TK.Op && tok.Text == "?" 
                && next is { Kind: TK.Id or TK.Kw } 
                && Last() is not '?' and not '(' and not '[')
            {
                // This is a nullable type qualifier e.g. int? or string?
                // Append directly with no spaces
                Append("?");
                continue;
            }

            // ── Opening paren ( ───────────────────────────────────────────
            if (tok.Kind == TK.Op && tok.Text == "(")
            {
                if (prev is { Kind: TK.Kw } pk && ControlKws.Contains(pk.Text))
                {
                    // FIX 2: start tracking depth
                    nextIsControlFlowBody = true;
                    controlFlowParenDepth = 1;
                    Sp();
                }
                else if (nextIsControlFlowBody)
                {
                    // FIX 2: nested paren inside control-flow condition
                    controlFlowParenDepth++;
                }
                Append("(");
                continue;
            }

            // ── Closing paren ) ───────────────────────────────────────────
            if (tok.Kind == TK.Op && tok.Text == ")")
            {
                Append(")");

                if (nextIsControlFlowBody)
                {
                    // FIX 2: only emit newline when we close the OUTERMOST paren
                    controlFlowParenDepth--;
                    if (controlFlowParenDepth == 0)
                    {
                        nextIsControlFlowBody = false;
                        bool nextIsBrace = next is { Kind: TK.Op } nb && nb.Text == "{";
                        if (!nextIsBrace)
                            Nl();
                    }
                }
                continue;
            }

            // ── [ ] ───────────────────────────────────────────────────────
            if (tok.Kind == TK.Op && tok.Text is "[" or "]")
            { Append(tok.Text); continue; }

            // ── Colon (but not ::) ────────────────────────────────────────
            if (tok.Kind == TK.Op && tok.Text == ":")
            { Sp(); Append(":"); Sp(); continue; }

            // ── Arrow => ──────────────────────────────────────────────────
            if (tok.Kind == TK.Op && tok.Text == "=>")
            { Sp(); Append("=>"); Sp(); continue; }

            // ── Binary / assignment operators ─────────────────────────────
            if (tok.Kind == TK.Op && BinOps.Contains(tok.Text) && tok.Text != "=>")
            {
                bool unary = UnaryOps.Contains(tok.Text) && IsUnaryCtx(i);
                if (!unary) Sp();
                Append(tok.Text);
                if (!unary) Sp();
                continue;
            }

            // ── < > (generic vs comparison) ───────────────────────────────
            if (tok.Kind == TK.Op && tok.Text is "<" or ">")
            {
                bool afterId  = prev is { Kind: TK.Id or TK.Kw };
                bool beforeId = next is { Kind: TK.Id or TK.Kw } ||
                                (next is { Kind: TK.Op } no && no.Text is "," or ">" or "<" or "[" or "]");
                if (afterId && beforeId) Append(tok.Text);
                else { Sp(); Append(tok.Text); Sp(); }
                continue;
            }

            // ── else / catch / finally ────────────────────────────────────
            if (tok.Kind == TK.Kw && tok.Text is "else" or "catch" or "finally")
            {
                Append(tok.Text);
                if (next is not null && !next.Value.Text.StartsWith("{") && !next.Value.Text.StartsWith("("))
                    Sp();
                continue;
            }

            // ── case / default ────────────────────────────────────────────
            if (tok.Kind == TK.Kw && tok.Text is "case" or "default")
            {
                Nl(); Append(tok.Text);
                if (tok.Text == "case") Sp();
                continue;
            }

            // ── return / throw / new / await / typeof / sizeof / nameof ──
            if (tok.Kind == TK.Kw && tok.Text is
                "return" or "throw" or "new" or "await" or "typeof" or "sizeof" or "nameof" or "stackalloc")
            {
                if (sb.Length > 0 && sb[^1] != '\n' && sb[^1] != ' ') Sp();
                Append(tok.Text);
                if (next is not null && next.Value.Text != ";") Sp();
                continue;
            }

            // ── General keyword ───────────────────────────────────────────
            if (tok.Kind == TK.Kw)
            {
                // FIX 3 (partial): don't add space after ! or ~
                if (sb.Length > 0 && sb[^1] != '\n' && sb[^1] != ' '
                    && sb[^1] != '!' && sb[^1] != '~') Sp();
                Append(tok.Text);
                if (next is not null && next.Value.Kind != TK.Op)
                    Sp();
                else if (next is { Kind: TK.Op } no2 && no2.Text is not (";" or "(" or "[" or ")" or "." or "?."))
                    Sp();
                continue;
            }

            // ── Identifiers, numbers, strings ─────────────────────────────
            if (tok.Kind is TK.Id or TK.Num or TK.Str)
            {
                char last = Last();
                if (last != '\0' && last != ' ' && last != '\n'
                    && last != '(' && last != '[' && last != '<'
                    && last != '.' && last != '!' && last != '~'
                    && last != '@' && last != '?')
                    Sp();
                Append(tok.Text);
                continue;
            }

            // ── Anything else ─────────────────────────────────────────────
            Append(tok.Text);
        }

        return sb.ToString();
    }

    // ── Indent ────────────────────────────────────────────────────────────────

    private static readonly HashSet<string> ControlFlowStarters = new(StringComparer.Ordinal)
    { "if ", "if(", "for ", "for(", "foreach ", "foreach(", "while ", "while(", "else" };

    private static string Indent(string code)
    {
        var sb    = new StringBuilder();
        int depth = 0;

        // FIX 3: track whether previous meaningful line was braceless control-flow
        bool prevWasBracelessControl = false;

        var rawLines = code.Split('\n');

        foreach (var raw in rawLines)
        {
            var line = raw.Trim();
            if (line.Length == 0)
            {
                sb.Append('\n');
                prevWasBracelessControl = false;
                continue;
            }

            // Dedent before }
            if (line[0] == '}') depth = Math.Max(0, depth - 1);

            // case / default one level below switch body
            bool isCase = (line.StartsWith("case ") || line == "default:" || line.StartsWith("default:"))
                          && !line.StartsWith("default(");
            int pad = isCase ? Math.Max(0, depth - 1) : depth;

            // FIX 3: if previous line was braceless control-flow, indent body +1
            if (prevWasBracelessControl && line[0] != '{' && line[0] != '}')
                pad = depth + 1;

            sb.Append(' ', pad * 4).Append(line).Append('\n');

            if (line.EndsWith('{'))
            {
                depth++;
                prevWasBracelessControl = false;
            }
            else
            {
                // FIX 3: detect braceless control-flow line
                // A control-flow line ends with ) and has no { anywhere
                bool isControlFlow = ControlFlowStarters.Any(s => line.StartsWith(s))
                                     && line.EndsWith(')')
                                     && !line.Contains('{');
                prevWasBracelessControl = isControlFlow;
            }
        }

        return sb.ToString();
    }

    // ── Style ─────────────────────────────────────────────────────────────────

    private static string Style(string code)
    {
        var lines  = code.Split('\n');
        var result = new List<string>();

        foreach (var raw in lines)
        {
            var line = raw.TrimEnd();
            if (line.Length == 0) { result.Add(""); continue; }

            string ind  = new string(' ', line.Length - line.TrimStart().Length);
            string body = line.TrimStart();
            // Split multiple inline property assignments: Id = u.Id, Name = u.Name...
            if (body.Contains(',') && LooksLikeMultiAssign(body))
            {
                var split = SplitMultiAssign(body, ind);
                if (split is not null) { result.AddRange(split); continue; }
            }
            // Long method signature: split params
            if (line.Length > WrapWidth && LooksLikeSig(body))
            {
                var split = SplitSig(body, ind);
                if (split is not null) { result.AddRange(split); continue; }
            }

            // Guard: never chain-split control-flow lines
            bool isControlFlow = body.StartsWith("if ") || body.StartsWith("if(")
                              || body.StartsWith("else") || body.StartsWith("for ")
                              || body.StartsWith("while ") || body.StartsWith("foreach ");

            // FIX 4: use ChainWrap (60) instead of WrapWidth (90) for chains
            if (line.Length > ChainWrap && LooksLikeChain(body) && !isControlFlow)
            {
                var split = SplitChain(body, ind);
                if (split is not null) { result.AddRange(split); continue; }
            }

            // Inline initializer: split props
            if (line.Length > WrapWidth && LooksLikeInit(body))
            {
                var split = SplitInit(body, ind);
                if (split is not null) { result.AddRange(split); continue; }
            }

            result.Add(raw.TrimEnd());
        }

        return string.Join('\n', result);
    }

    private static readonly Regex MultiAssignRx = new(
        @"^\w+\s*=\s*.+,\s*\w+\s*=\s*",
        RegexOptions.Compiled);

    private static bool LooksLikeMultiAssign(string s) =>
    MultiAssignRx.IsMatch(s);
    private static List<string>? SplitMultiAssign(string body, string ind)
    {
        var props = TopSplit(body, ',');
        if (props.Count <= 1) return null;

        // Verify all parts look like assignments
        bool allAssign = props.All(p => Regex.IsMatch(p.Trim(), @"^\w+\s*=\s*"));
        if (!allAssign) return null;

        // Align = signs
        int maxKey = props.Max(p =>
        {
            int e = p.IndexOf('=');
            return e > 0 ? p[..e].Trim().Length : 0;
        });

        var r = new List<string>();
        foreach (var p in props)
        {
            int e = p.IndexOf('=');
            string k = p[..e].Trim().PadRight(maxKey);
            string v = p[(e+1)..].Trim();
            r.Add(ind + k + " = " + v + ",");
        }

        // Remove trailing comma from last line
        r[^1] = r[^1].TrimEnd(',');

        return r;
    }
    // ── Signature split ───────────────────────────────────────────────────────

    private static readonly Regex SigRx = new(
        @"^([\w<>\[\]\?,\s\.]+)\s+(\w+)\s*(<[^>]*>)?\s*\((.+,.+)\)\s*(\{|;|where|=>|$)",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private static bool LooksLikeSig(string s) =>
        s.Contains('(') && s.Contains(',') && SigRx.IsMatch(s);

    private static List<string>? SplitSig(string body, string ind)
    {
        int po = body.IndexOf('(');
        if (po < 0) return null;
        int pc = MatchClose(body, po, '(', ')');
        if (pc < 0) return null;

        string head  = body[..po];
        string inner = body[(po+1)..pc];
        string tail  = body[(pc+1)..].TrimStart();

        var parms = TopSplit(inner, ',');
        if (parms.Count <= 1) return null;

        string ci = ind + "    ";
        var r = new List<string> { ind + head + "(" };
        for (int j = 0; j < parms.Count; j++)
            r.Add(ci + parms[j].Trim() + (j < parms.Count - 1 ? "," : ")"));

        if (tail == "{")          r[^1] += " {";
        else if (tail.Length > 0) r.Add(ind + tail);

        return r;
    }

    // ── Chain split ───────────────────────────────────────────────────────────

    private static readonly Regex ChainRx = new(
        @"\.\w+[\(<].*\.\w+", RegexOptions.Compiled | RegexOptions.Singleline);

    private static bool LooksLikeChain(string s) => ChainRx.IsMatch(s);

    private static List<string>? SplitChain(string body, string ind)
    {
        string prefix = "";
        string rest   = body;

        foreach (var pk in new[] { "return await ", "return ", "await " })
        {
            if (rest.StartsWith(pk, StringComparison.Ordinal))
            { prefix = pk.TrimEnd(); rest = rest[pk.Length..]; break; }
        }

        int eq = rest.IndexOf('=');
        if (eq > 0 && eq < rest.IndexOf('.') && rest[eq+1] != '=')
        {
            prefix = (prefix.Length > 0 ? prefix + " " : "") + rest[..(eq+1)].TrimEnd();
            rest   = rest[(eq+1)..].TrimStart();
        }

        var parts = TopDotSplit(rest);
        if (parts.Count <= 2) return null;

        string ci = ind + "    ";
        var r     = new List<string>();

        r.Add(ind + (prefix.Length > 0 ? prefix + " " + parts[0] : parts[0]));
        for (int j = 1; j < parts.Count; j++)
            r.Add(ci + "." + parts[j].Trim());

        return r;
    }

    private static List<string> TopDotSplit(string s)
    {
        var parts = new List<string>();
        int d = 0, start = 0;
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c is '(' or '[' or '<') d++;
            else if (c is ')' or ']' or '>') d--;
            else if (c == '.' && d == 0 && i > 0)
            { parts.Add(s[start..i]); start = i+1; }
        }
        parts.Add(s[start..]);
        return parts;
    }

    // ── Initializer split ─────────────────────────────────────────────────────

    private static readonly Regex InitRx = new(
        @"\bnew\b[^{]*\{[^{}]+\}", RegexOptions.Compiled | RegexOptions.Singleline);

    private static bool LooksLikeInit(string s) => InitRx.IsMatch(s) && s.Contains(',');

    private static List<string>? SplitInit(string body, string ind)
    {
        int bo = body.IndexOf('{');
        if (bo < 0) return null;
        int bc = MatchClose(body, bo, '{', '}');
        if (bc < 0) return null;

        string before = body[..bo].TrimEnd();
        string inner  = body[(bo+1)..bc].Trim();
        string after  = body[(bc+1)..].TrimStart();

        var props = TopSplit(inner, ',');
        if (props.Count <= 1) return null;

        bool allAssign = props.All(p => Regex.IsMatch(p.Trim(), @"^\w+\s*=\s*"));
        int maxKey = allAssign
            ? props.Max(p => { int e=p.IndexOf('='); return e>0?p[..e].Trim().Length:0; })
            : 0;

        string ci = ind + "    ";
        var r = new List<string>();
        r.Add(ind + before);
        r.Add(ind + "{");

        for (int j = 0; j < props.Count; j++)
        {
            var p = props[j].Trim();
            if (allAssign && p.Contains('='))
            {
                int e = p.IndexOf('=');
                string k = p[..e].Trim().PadRight(maxKey);
                string v = p[(e+1)..].Trim();
                r.Add(ci + k + " = " + v + ",");
            }
            else r.Add(ci + p + ",");
        }

        r.Add(ind + "}" + after);
        return r;
    }

    // ── Utilities ─────────────────────────────────────────────────────────────

    private static int MatchClose(string s, int open, char o, char c)
    {
        int d = 0;
        for (int i=open; i<s.Length; i++)
        { if(s[i]==o)d++; else if(s[i]==c){d--;if(d==0)return i;} }
        return -1;
    }

    private static List<string> TopSplit(string s, char sep)
    {
        var parts = new List<string>();
        int d=0, start=0; bool inStr=false;
        for (int i=0; i<s.Length; i++)
        {
            char c=s[i];
            if ((c=='"'||c=='\'')&&(i==0||s[i-1]!='\\')) inStr=!inStr;
            if (inStr) continue;
            if (c is '('or'['or'{'or'<') d++;
            else if (c is ')'or']'or'}'or'>') d--;
            else if (c==sep&&d==0){parts.Add(s[start..i].Trim());start=i+1;}
        }
        var last=s[start..].Trim();
        if(last.Length>0)parts.Add(last);
        return parts;
    }

    // ── Cleanup ───────────────────────────────────────────────────────────────

    // FIX 5: blank lines before these starters inside method bodies
    private static readonly string[] BlankBefore =
    {
        "if ", "if(", "for ", "for(", "foreach ", "foreach(",
        "while ", "while(", "return ", "return;", "throw ",
        "var ", "var\t"
    };

    private static string Cleanup(string code)
    {
        var lines  = code.Split('\n').ToList();
        var result = new List<string>();
        int blanks = 0;

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i].TrimEnd();

            if (line.Trim().Length == 0)
            { blanks++; if (blanks == 1) result.Add(""); continue; }

            blanks = 0;

            // Blank line before member declarations (indent >= 4 spaces, class-level)
            if (NeedsBlank(line, result))
                if (result.Count > 0 && result[^1] != "") result.Add("");

            // FIX 5: blank line before statement starters inside method bodies
            // Only when indented (inside a method) and prev line is not { or blank
            string trimmed = line.TrimStart();
            int    lineInd = line.Length - trimmed.Length;

            if (lineInd >= 4 && BlankBefore.Any(b => trimmed.StartsWith(b)))
            {
                if (result.Count > 0
                    && result[^1] != ""
                    && result[^1].TrimEnd() != "{"
                    && !result[^1].TrimStart().StartsWith("//"))
                {
                    result.Add("");
                }
            }

            result.Add(line);
        }

        while (result.Count > 0 && result[0]  == "") result.RemoveAt(0);
        while (result.Count > 0 && result[^1] == "") result.RemoveAt(result.Count - 1);

        return string.Join('\n', result) + "\n";
    }

    private static readonly string[] MemberPfx =
    { "public ","private ","protected ","internal ","override ","static ","async ","/// ","[" };

    private static bool NeedsBlank(string line, List<string> prev)
    {
        string body = line.TrimStart();
        int ind     = line.Length - body.Length;
        if (ind < 4) return false;
        if (!MemberPfx.Any(body.StartsWith)) return false;
        for (int k=prev.Count-1; k>=0; k--)
        {
            string p = prev[k].TrimEnd();
            if (p == "") return false;
            if (p.TrimStart() == "{") return false;
            return true;
        }
        return false;
    }
}