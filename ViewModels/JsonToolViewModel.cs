using System;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevPad.Models;
using DevPad.Services;

namespace DevPad.ViewModels;

public partial class JsonToolViewModel : ToolViewModelBase
{
    private static readonly JsonFormatterService _service = new();

    private static readonly SolidColorBrush ValidBrush   = new(Color.Parse("#4CAF50"));
    private static readonly SolidColorBrush ErrorBrush   = new(Color.Parse("#F48771"));
    private static readonly SolidColorBrush NeutralBrush = new(Color.Parse("#8E8E8E"));

    public override string ToolName => "JSON";

    // Curly-braces icon representing JSON
    public override string IconPath =>
        "M4 2C2.89543 2 2 2.89543 2 4V8C2 9.10457 2.89543 10 4 10H4.5C5.05228 10 5.5 10.4477 5.5 11V12C5.5 12.5523 5.05228 13 4.5 13H4C2.89543 13 2 13.8954 2 15V19C2 20.1046 2.89543 21 4 21H5V19H4V15H4.5C6.15685 15 7.5 13.6569 7.5 12V11C7.5 9.34315 6.15685 8 4.5 8H4V4H5V2H4ZM20 2C21.1046 2 22 2.89543 22 4V8C22 9.10457 21.1046 10 20 10H19.5C18.9477 10 18.5 10.4477 18.5 11V12C18.5 12.5523 18.9477 13 19.5 13H20C21.1046 13 22 13.8954 22 15V19C22 20.1046 21.1046 21 20 21H19V19H20V15H19.5C17.8431 15 16.5 13.6569 16.5 12V11C16.5 9.34315 17.8431 8 19.5 8H20V4H19V2H20Z";

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private string _statusText = "Paste JSON to begin";

    [ObservableProperty]
    private IBrush _statusForeground = NeutralBrush;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsInvalid))]
    private bool _isValid;

    [ObservableProperty]
    private string _nodeCountText = string.Empty;

    [ObservableProperty]
    private string _fileSizeText = string.Empty;

    /// <summary>Pretty-printed JSON; drives the syntax-highlighted output panel via code-behind.</summary>
    [ObservableProperty]
    private string _formattedText = string.Empty;

    /// <summary>When true the right pane shows the tree; false shows the formatted text panel.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowFormatted))]
    private bool _showTree = false;

    public bool IsInvalid     => !IsValid;
    public bool ShowFormatted => !ShowTree;

    public ObservableCollection<JsonTreeNode> TreeNodes { get; } = new();

    private CancellationTokenSource _cts = new();

    // ── View-toggle commands ──────────────────────────────────────────────

    [RelayCommand]
    private void SwitchToText() => ShowTree = false;

    [RelayCommand]
    private void SwitchToTree() => ShowTree = true;

    // ── Input change handler ──────────────────────────────────────────────

    partial void OnInputTextChanged(string value)
    {
        _cts.Cancel();
        _cts = new CancellationTokenSource();
        _ = AnalyseAsync(value, _cts.Token);
    }

    private async Task AnalyseAsync(string value, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                IsValid          = false;
                StatusText       = "Paste JSON to begin";
                StatusForeground = NeutralBrush;
                NodeCountText = FileSizeText = FormattedText = string.Empty;
                TreeNodes.Clear();
                return;
            }

            StatusText       = "Analysing…";
            StatusForeground = NeutralBrush;

            // Single parse: format + build tree off the UI thread
            var (formatted, nodes, nodeCount, truncated, error) =
                await Task.Run(() => _service.BuildTreeAndFormat(value), ct);

            int byteCount = await Task.Run(() => Encoding.UTF8.GetByteCount(value), ct);

            ct.ThrowIfCancellationRequested();

            bool isValid = error == null;
            IsValid = isValid;

            if (isValid)
            {
                StatusText = truncated
                    ? $"Valid JSON · tree limited to {JsonFormatterService.MaxTreeNodes:N0} nodes"
                    : "Valid JSON";
                StatusForeground = ValidBrush;
                FormattedText    = formatted;
                TreeNodes.Clear();
                foreach (var n in nodes) TreeNodes.Add(n);
                NodeCountText = truncated ? $"{nodeCount:N0}+ nodes" : $"{nodeCount:N0} nodes";
                FileSizeText  = $"{byteCount:N0} bytes";
            }
            else
            {
                StatusText       = error ?? "Invalid JSON";
                StatusForeground = ErrorBrush;
                FormattedText    = string.Empty;
                TreeNodes.Clear();
                NodeCountText = FileSizeText = string.Empty;
            }
        }
        catch (OperationCanceledException)
        {
            // Superseded by newer input — normal.
        }
        catch (Exception ex)
        {
            IsValid          = false;
            StatusText       = ex.Message;
            StatusForeground = ErrorBrush;
            FormattedText    = string.Empty;
        }
    }

    [RelayCommand]
    private async Task MinifyAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText)) return;
        var input = InputText;
        StatusText = "Minifying…";
        var result = await Task.Run(() => _service.Minify(input, out _));
        if (result != null) InputText = result;
    }

    [RelayCommand]
    private async Task CopyAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText)) return;
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            await (desktop.MainWindow?.Clipboard?.SetTextAsync(InputText) ?? Task.CompletedTask);
    }

    /// <summary>Copies the formatted (pretty-printed) text from the output panel.</summary>
    [RelayCommand]
    private async Task CopyFormattedAsync()
    {
        if (string.IsNullOrEmpty(FormattedText)) return;
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            await (desktop.MainWindow?.Clipboard?.SetTextAsync(FormattedText) ?? Task.CompletedTask);
    }

    [RelayCommand]
    private void Clear()
    {
        InputText = string.Empty;
    }

    public override Task PasteAndFormat(string clipboardText)
    {
        // Cancel any pending analysis and paste the raw text.
        // OnInputTextChanged → AnalyseAsync will produce the formatted output automatically.
        _cts.Cancel();
        _cts     = new CancellationTokenSource();
        InputText = clipboardText;
        return Task.CompletedTask;
    }
}
