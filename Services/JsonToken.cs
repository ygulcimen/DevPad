namespace DevPad.Services;

public enum JsonTokenType
{
    Whitespace,
    Punctuation,  // { } [ ] : ,
    Key,          // object property names (quoted)
    StringValue,  // string values (quoted)
    Number,       // integers and floats
    Boolean,      // true / false
    Null          // null
}

public record JsonToken(string Text, JsonTokenType Type);
