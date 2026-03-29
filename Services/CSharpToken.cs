namespace DevPad.Services;

public enum CSharpTokenType
{
    Whitespace,
    Comment,
    String,
    Number,
    Keyword,
    Preprocessor,
    Identifier,
    Punctuation
}

public record CSharpToken(string Text, CSharpTokenType Type);
