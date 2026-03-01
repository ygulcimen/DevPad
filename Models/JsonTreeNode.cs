using System.Collections.ObjectModel;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DevPad.Models;

public enum JsonNodeType { Object, Array, String, Number, Boolean, Null }

public partial class JsonTreeNode : ObservableObject
{
    public string? Key { get; init; }
    public string? Value { get; init; }
    public JsonNodeType TokenType { get; init; }
    public ObservableCollection<JsonTreeNode> Children { get; } = new();

    [ObservableProperty]
    private bool _isExpanded = true;

    // Key label shown in the tree (e.g. "name: " or empty for root/array items without string keys)
    public string KeyDisplay => Key != null ? $"{Key}: " : string.Empty;

    // Value portion shown in the tree
    public string ValueDisplay => TokenType switch
    {
        JsonNodeType.Object => $"{{...}} ({Children.Count} items)",
        JsonNodeType.Array  => $"[...] ({Children.Count} items)",
        JsonNodeType.String => $"\"{Value}\"",
        JsonNodeType.Null   => "null",
        _                   => Value ?? string.Empty
    };

    // VS Code-style syntax colors per value type
    private static readonly SolidColorBrush ObjectBrush  = new(Color.Parse("#DCDCAA"));
    private static readonly SolidColorBrush StringBrush  = new(Color.Parse("#CE9178"));
    private static readonly SolidColorBrush NumberBrush  = new(Color.Parse("#B5CEA8"));
    private static readonly SolidColorBrush BoolBrush    = new(Color.Parse("#569CD6"));
    private static readonly SolidColorBrush NullBrush    = new(Color.Parse("#808080"));
    private static readonly SolidColorBrush DefaultBrush = new(Color.Parse("#CCCCCC"));

    public IBrush ValueForeground => TokenType switch
    {
        JsonNodeType.Object or JsonNodeType.Array => ObjectBrush,
        JsonNodeType.String                       => StringBrush,
        JsonNodeType.Number                       => NumberBrush,
        JsonNodeType.Boolean                      => BoolBrush,
        JsonNodeType.Null                         => NullBrush,
        _                                         => DefaultBrush
    };
}
