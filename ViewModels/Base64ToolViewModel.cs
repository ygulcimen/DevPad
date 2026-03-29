using System.IO;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevPad.Services;

namespace DevPad.ViewModels;

public partial class Base64ToolViewModel : ToolViewModelBase
{
    private static readonly Base64Service _service = new();

    public override string ToolName => "Base64";
    public override string IconPath => "M4 6H20M4 12H20M4 18H14M17 16L19 18L23 14";

    [ObservableProperty] private string _inputText  = string.Empty;
    [ObservableProperty] private string _outputText = string.Empty;
    [ObservableProperty] private string _statusText = "Enter text and click Encode, or paste Base64 and click Decode";
    [ObservableProperty] private bool   _isUrlSafe;

    [RelayCommand]
    private void Encode()
    {
        if (string.IsNullOrEmpty(InputText)) { OutputText = string.Empty; StatusText = "Nothing to encode"; return; }
        OutputText = _service.Encode(InputText, IsUrlSafe);
        var inBytes = Encoding.UTF8.GetByteCount(InputText);
        StatusText = $"Encoded ({(IsUrlSafe ? "URL-safe" : "Standard")}) · Input: {inBytes:N0} bytes · Output: {OutputText.Length:N0} chars";
    }

    [RelayCommand]
    private void Decode()
    {
        if (string.IsNullOrEmpty(InputText)) { OutputText = string.Empty; StatusText = "Nothing to decode"; return; }
        var (result, error) = _service.Decode(InputText, IsUrlSafe);
        if (error != null) { OutputText = string.Empty; StatusText = $"Decode error: {error}"; return; }
        OutputText = result!;
        StatusText = $"Decoded · Input: {InputText.Length:N0} chars · Output: {Encoding.UTF8.GetByteCount(result!):N0} bytes";
    }

    [RelayCommand]
    private void Swap()
    {
        (InputText, OutputText) = (OutputText, InputText);
        StatusText = "Swapped input and output";
    }

    [RelayCommand]
    private async Task CopyAsync()
    {
        if (string.IsNullOrEmpty(OutputText)) return;
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime d)
            await (d.MainWindow?.Clipboard?.SetTextAsync(OutputText) ?? Task.CompletedTask);
        await FlashCopyAsync();
    }

    [RelayCommand]
    private void Clear() => ExecuteClear();

    public override void ExecuteClear()
    {
        InputText = OutputText = string.Empty;
        StatusText = "Enter text and click Encode, or paste Base64 and click Decode";
    }

    public override async Task ExecuteCopyOutputAsync()
    {
        if (string.IsNullOrEmpty(OutputText)) return;
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime d)
            await (d.MainWindow?.Clipboard?.SetTextAsync(OutputText) ?? Task.CompletedTask);
        await FlashCopyAsync();
    }

    public override async Task SaveAsAsync()
    {
        if (string.IsNullOrEmpty(OutputText)) return;
        var sp = GetStorageProvider(); if (sp is null) return;

        var file = await sp.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title             = "Save Output",
            SuggestedFileName = "output.txt",
            FileTypeChoices   = new[] { new FilePickerFileType("Text") { Patterns = new[] { "*.txt" } } }
        });
        if (file is null) return;

        await using var stream = await file.OpenWriteAsync();
        await using var writer = new StreamWriter(stream, Encoding.UTF8);
        await writer.WriteAsync(OutputText);
    }

    public override Task PasteAndFormat(string clipboardText)
    {
        InputText = clipboardText;
        var (result, _) = _service.Decode(clipboardText, IsUrlSafe);
        if (result != null) Decode();
        return Task.CompletedTask;
    }
}
