using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Platform.Storage;
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
    public override string IconPath => "M14.5 4L9.5 20M7 7L2 12L7 17M17 7L22 12L17 17";

    [ObservableProperty] private string _inputText       = string.Empty;
    [ObservableProperty] private string _statusText      = "Paste XML to begin";
    [ObservableProperty] private IBrush _statusForeground = NeutralBrush;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsInvalid))]
    private bool _isValid;

    [ObservableProperty] private string _nodeCountText  = string.Empty;
    [ObservableProperty] private string _fileSizeText   = string.Empty;
    [ObservableProperty] private string _xPathQuery     = string.Empty;
    [ObservableProperty] private string _xPathResultText = string.Empty;
    [ObservableProperty] private bool   _hasXPathResult;
    [ObservableProperty] private string _formattedText  = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowFormatted))]
    private bool _showTree = false;

    public bool IsInvalid     => !IsValid;
    public bool ShowFormatted => !ShowTree;

    public ObservableCollection<XmlTreeNode> TreeNodes { get; } = new();

    [RelayCommand] private void SwitchToText() => ShowTree = false;
    [RelayCommand] private void SwitchToTree() => ShowTree = true;

    partial void OnInputTextChanged(string value)
    {
        HasXPathResult = false; XPathResultText = string.Empty;

        if (string.IsNullOrWhiteSpace(value))
        {
            IsValid = false; StatusText = "Paste XML to begin"; StatusForeground = NeutralBrush;
            NodeCountText = FileSizeText = FormattedText = string.Empty;
            TreeNodes.Clear(); return;
        }

        var (formatted, nodes, count, error) = _service.FormatAndBuildTree(value);
        IsValid = error == null;

        if (IsValid)
        {
            StatusText = "Valid XML"; StatusForeground = ValidBrush;
            FormattedText = formatted ?? value;
            TreeNodes.Clear(); foreach (var node in nodes) TreeNodes.Add(node);
            NodeCountText = $"{count:N0} nodes";
            FileSizeText  = $"{Encoding.UTF8.GetByteCount(value):N0} bytes";
        }
        else
        {
            StatusText = error ?? "Invalid XML"; StatusForeground = ErrorBrush;
            FormattedText = string.Empty; TreeNodes.Clear();
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
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime d)
            await (d.MainWindow?.Clipboard?.SetTextAsync(InputText) ?? Task.CompletedTask);
        await FlashCopyAsync();
    }

    [RelayCommand]
    private async Task CopyFormattedAsync()
    {
        if (string.IsNullOrEmpty(FormattedText)) return;
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime d)
            await (d.MainWindow?.Clipboard?.SetTextAsync(FormattedText) ?? Task.CompletedTask);
        await FlashCopyOutputAsync();
    }

    [RelayCommand]
    private void Clear() => ExecuteClear();

    [RelayCommand]
    private void RunXPath()
    {
        if (string.IsNullOrWhiteSpace(InputText) || string.IsNullOrWhiteSpace(XPathQuery)) return;
        var (results, error) = _service.ExecuteXPath(InputText, XPathQuery);
        HasXPathResult = true;

        if (error != null) XPathResultText = $"Error: {error}";
        else if (results.Count == 0) XPathResultText = "(no matches)";
        else XPathResultText = $"{results.Count} match(es):\n\n" + string.Join("\n\n", results);
    }

    public override void ExecuteClear()
    {
        InputText = XPathQuery = XPathResultText = string.Empty;
        HasXPathResult = false;
    }

    public override async Task ExecuteCopyOutputAsync()
    {
        if (string.IsNullOrEmpty(FormattedText)) return;
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime d)
            await (d.MainWindow?.Clipboard?.SetTextAsync(FormattedText) ?? Task.CompletedTask);
        await FlashCopyOutputAsync();
    }

    public override async Task SaveAsAsync()
    {
        if (string.IsNullOrEmpty(FormattedText)) return;
        var sp = GetStorageProvider(); if (sp is null) return;

        var file = await sp.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title             = "Save XML",
            SuggestedFileName = "output.xml",
            FileTypeChoices   = new[] { new FilePickerFileType("XML") { Patterns = new[] { "*.xml" } } }
        });
        if (file is null) return;

        await using var stream = await file.OpenWriteAsync();
        await using var writer = new StreamWriter(stream, Encoding.UTF8);
        await writer.WriteAsync(FormattedText);
    }

    public override Task PasteAndFormat(string clipboardText)
    {
        InputText = clipboardText;
        return Task.CompletedTask;
    }
}
