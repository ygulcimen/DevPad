using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevPad.Models;
using DevPad.Services;

namespace DevPad.ViewModels;

public partial class XmlToolViewModel : ToolViewModelBase
{
    private static readonly XmlFormatterService _service = new();

    private static readonly SolidColorBrush ValidBrush   = new(Color.Parse("#4CAF50"));
    private static readonly SolidColorBrush ErrorBrush   = new(Color.Parse("#F48771"));
    private static readonly SolidColorBrush NeutralBrush = new(Color.Parse("#8E8E8E"));

    public override string ToolName => "XML";

    // Angle brackets icon representing XML/HTML tags
    public override string IconPath => "M14.5 4L9.5 20M7 7L2 12L7 17M17 7L22 12L17 17";

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private string _statusText = "Paste XML to begin";

    [ObservableProperty]
    private IBrush _statusForeground = NeutralBrush;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsInvalid))]
    private bool _isValid;

    [ObservableProperty]
    private string _nodeCountText = string.Empty;

    [ObservableProperty]
    private string _fileSizeText = string.Empty;

    [ObservableProperty]
    private string _xPathQuery = string.Empty;

    [ObservableProperty]
    private string _xPathResultText = string.Empty;

    [ObservableProperty]
    private bool _hasXPathResult;

    /// <summary>Pretty-printed XML; drives the syntax-highlighted output panel via code-behind.</summary>
    [ObservableProperty]
    private string _formattedText = string.Empty;

    /// <summary>When true the right pane shows the tree; false shows the formatted text panel.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowFormatted))]
    private bool _showTree = false;

    public bool IsInvalid     => !IsValid;
    public bool ShowFormatted => !ShowTree;

    public ObservableCollection<XmlTreeNode> TreeNodes { get; } = new();

    // ── View-toggle commands ──────────────────────────────────────────────

    [RelayCommand]
    private void SwitchToText() => ShowTree = false;

    [RelayCommand]
    private void SwitchToTree() => ShowTree = true;

    // ── Input change handler ──────────────────────────────────────────────

    partial void OnInputTextChanged(string value)
    {
        HasXPathResult  = false;
        XPathResultText = string.Empty;

        if (string.IsNullOrWhiteSpace(value))
        {
            IsValid          = false;
            StatusText       = "Paste XML to begin";
            StatusForeground = NeutralBrush;
            NodeCountText = FileSizeText = FormattedText = string.Empty;
            TreeNodes.Clear();
            return;
        }

        // Single parse: format + validate + build tree in one go
        var (formatted, nodes, count, error) = _service.FormatAndBuildTree(value);
        bool valid = error == null;
        IsValid = valid;

        if (valid)
        {
            StatusText       = "Valid XML";
            StatusForeground = ValidBrush;
            FormattedText    = formatted ?? value;
            TreeNodes.Clear();
            foreach (var node in nodes) TreeNodes.Add(node);
            NodeCountText = $"{count:N0} nodes";
            FileSizeText  = $"{Encoding.UTF8.GetByteCount(value):N0} bytes";
        }
        else
        {
            StatusText       = error ?? "Invalid XML";
            StatusForeground = ErrorBrush;
            FormattedText    = string.Empty;
            TreeNodes.Clear();
            NodeCountText = FileSizeText = string.Empty;
        }
    }

    [RelayCommand]
    private void Format()
    {
        if (string.IsNullOrWhiteSpace(InputText)) return;
        var result = _service.Format(InputText, out _);
        if (result != null) InputText = result;
    }

    [RelayCommand]
    private async Task CopyAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText)) return;
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            await (desktop.MainWindow?.Clipboard?.SetTextAsync(InputText) ?? Task.CompletedTask);
    }

    [RelayCommand]
    private void Clear()
    {
        InputText       = string.Empty;
        XPathQuery      = string.Empty;
        XPathResultText = string.Empty;
        HasXPathResult  = false;
    }

    [RelayCommand]
    private void RunXPath()
    {
        if (string.IsNullOrWhiteSpace(InputText) || string.IsNullOrWhiteSpace(XPathQuery)) return;
        var (results, error) = _service.ExecuteXPath(InputText, XPathQuery);
        HasXPathResult = true;

        if (error != null)
        {
            XPathResultText = $"Error: {error}";
        }
        else if (results.Count == 0)
        {
            XPathResultText = "(no matches)";
        }
        else
        {
            XPathResultText = $"{results.Count} match(es):\n\n" + string.Join("\n\n", results);
        }
    }

    public override Task PasteAndFormat(string clipboardText)
    {
        InputText = clipboardText;  // OnInputTextChanged validates + formats + builds tree
        return Task.CompletedTask;
    }
}
