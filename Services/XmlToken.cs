namespace DevPad.Services;

public enum XmlTokenType
{
    Whitespace,
    TagPunct,       // <  >  </  />  <?  ?>
    TagName,        // element and closing-tag names
    AttributeName,
    AttributeValue, // includes surrounding quotes
    TextContent,    // character data between tags
    Comment,        // <!-- ... -->  (entire span)
    CData,          // <![CDATA[ ... ]]>  (entire span)
    Declaration     // <?xml ... ?>  (entire span)
}

public record XmlToken(string Text, XmlTokenType Type);
