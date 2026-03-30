using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevPad.Services;

namespace DevPad.ViewModels;

public partial class CSharpToolViewModel : ToolViewModelBase
{
    private static readonly CSharpFormatterService _formatterService = new();

    private static readonly SolidColorBrush ValidBrush   = new(Color.Parse("#4CAF50"));
    private static readonly SolidColorBrush ErrorBrush   = new(Color.Parse("#F48771"));
    private static readonly SolidColorBrush NeutralBrush = new(Color.Parse("#8E8E8E"));

    public override string ToolName => "C#";
    
    // C# icon path (curly braces style)
    public override string IconPath => "M12.5 4L7.5 20M5 7L0 12L5 17M15 7L20 12L15 17";

    [ObservableProperty] private string _inputText       = string.Empty;
    [ObservableProperty] private string _statusText      = "Paste C# code to begin";
    [ObservableProperty] private IBrush _statusForeground = NeutralBrush;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsInvalid))]
    private bool _isValid;

    [ObservableProperty] private string _lineCountText  = string.Empty;
    [ObservableProperty] private string _fileSizeText   = string.Empty;
    [ObservableProperty] private string _formattedText  = string.Empty;

    public bool IsInvalid => !IsValid;

    private CancellationTokenSource _cts = new();

    partial void OnInputTextChanged(string value)
    {
        _cts.Cancel();
        _cts = new CancellationTokenSource();
        _ = FormatAsync(value, _cts.Token);
    }

    private async Task FormatAsync(string value, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                IsValid = false;
                StatusText = "Paste C# code to begin";
                StatusForeground = NeutralBrush;
                LineCountText = FileSizeText = FormattedText = string.Empty;
                return;
            }

            StatusText = "Formatting…";
            StatusForeground = NeutralBrush;

            var result = await Task.Run(() => _formatterService.Format(value, out var error), ct);
            ct.ThrowIfCancellationRequested();

            int byteCount = await Task.Run(() => Encoding.UTF8.GetByteCount(value), ct);
            int lineCount = await Task.Run(() => value.Split('\n').Length, ct);
            ct.ThrowIfCancellationRequested();

            if (result != null)
            {
                IsValid = true;
                StatusText = "Formatted";
                StatusForeground = ValidBrush;
                FormattedText = result;
                LineCountText = $"{lineCount:N0} lines";
                FileSizeText = $"{byteCount:N0} bytes";
            }
            else
            {
                IsValid = false;
                StatusText = "Format error";
                StatusForeground = ErrorBrush;
                FormattedText = string.Empty;
                LineCountText = FileSizeText = string.Empty;
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            IsValid = false;
            StatusText = $"Error: {ex.Message}";
            StatusForeground = ErrorBrush;
            FormattedText = string.Empty;
        }
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

    public override void ExecuteClear()
    {
        InputText = string.Empty;
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
            Title             = "Save C#",
            SuggestedFileName = "output.cs",
            FileTypeChoices   = new[] { new FilePickerFileType("C#") { Patterns = new[] { "*.cs" } } }
        });
        if (file is null) return;

        await using var stream = await file.OpenWriteAsync();
        await using var writer = new StreamWriter(stream, Encoding.UTF8);
        await writer.WriteAsync(FormattedText);
    }

    public override Task PasteAndFormat(string clipboardText)
    {
        _cts.Cancel();
        _cts = new CancellationTokenSource();
        InputText = clipboardText;
        return Task.CompletedTask;
    }
}