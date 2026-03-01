using System.Collections.ObjectModel;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DevPad.Models;

public enum XmlNodeKind { Element, Attribute, Text, Comment }

public partial class XmlTreeNode : ObservableObject
{
    // Element/attribute/tag name, or raw text/comment content
    public string Name { get; init; } = string.Empty;
    // Attribute value (Attribute nodes only)
    public string? Value { get; init; }
    public XmlNodeKind NodeKind { get; init; }
    public ObservableCollection<XmlTreeNode> Children { get; } = new();

    [ObservableProperty]
    private bool _isExpanded = true;

    public string DisplayText => NodeKind switch
    {
        XmlNodeKind.Element   => Name,
        XmlNodeKind.Attribute => $"@{Name}=\"{Value}\"",
        XmlNodeKind.Text      => Name,
        XmlNodeKind.Comment   => $"<!-- {Name} -->",
        _                     => Name
    };

    private static readonly SolidColorBrush ElementBrush   = new(Color.Parse("#4EC9B0"));
    private static readonly SolidColorBrush AttributeBrush = new(Color.Parse("#9CDCFE"));
    private static readonly SolidColorBrush TextBrush      = new(Color.Parse("#D4D4D4"));
    private static readonly SolidColorBrush CommentBrush   = new(Color.Parse("#6A9955"));

    public IBrush NodeForeground => NodeKind switch
    {
        XmlNodeKind.Element   => ElementBrush,
        XmlNodeKind.Attribute => AttributeBrush,
        XmlNodeKind.Text      => TextBrush,
        XmlNodeKind.Comment   => CommentBrush,
        _                     => TextBrush
    };
}
