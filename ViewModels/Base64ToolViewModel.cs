using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevPad.Services;

namespace DevPad.ViewModels;

public partial class Base64ToolViewModel : ToolViewModelBase
{
    private static readonly Base64Service _service = new();

    public override string ToolName => "Base64";

    // Binary/encode icon
    public override string IconPath => "M4 6H20M4 12H20M4 18H14M17 16L19 18L23 14";

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private string _outputText = string.Empty;

    // Status bar text (idle hint, size info, or error)
    [ObservableProperty]
    private string _statusText = "Enter text and click Encode, or paste Base64 and click Decode";

    // When true, Base64 output uses - _ instead of + / and no padding
    [ObservableProperty]
    private bool _isUrlSafe;

    [RelayCommand]
    private void Encode()
    {
        if (string.IsNullOrEmpty(InputText))
        {
            OutputText = string.Empty;
            StatusText = "Nothing to encode";
            return;
        }

        OutputText = _service.Encode(InputText, IsUrlSafe);

        var inBytes  = Encoding.UTF8.GetByteCount(InputText);
        var outChars = OutputText.Length;
        var mode     = IsUrlSafe ? "URL-safe" : "Standard";
        StatusText   = $"Encoded ({mode}) · Input: {inBytes:N0} bytes · Output: {outChars:N0} chars";
    }

    [RelayCommand]
    private void Decode()
    {
        if (string.IsNullOrEmpty(InputText))
        {
            OutputText = string.Empty;
            StatusText = "Nothing to decode";
            return;
        }

        var (result, error) = _service.Decode(InputText, IsUrlSafe);

        if (error != null)
        {
            OutputText = string.Empty;
            StatusText = $"Decode error: {error}";
            return;
        }

        OutputText = result!;
        var inChars  = InputText.Length;
        var outBytes = Encoding.UTF8.GetByteCount(result!);
        StatusText   = $"Decoded · Input: {inChars:N0} chars · Output: {outBytes:N0} bytes";
    }

    /// <summary>Moves current output to input so the user can round-trip.</summary>
    [RelayCommand]
    private void Swap()
    {
        (InputText, OutputText) = (OutputText, InputText);
        StatusText = "Swapped input and output";
    }

    /// <summary>Copies the output to clipboard.</summary>
    [RelayCommand]
    private async Task CopyAsync()
    {
        if (string.IsNullOrEmpty(OutputText)) return;
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            await (desktop.MainWindow?.Clipboard?.SetTextAsync(OutputText) ?? Task.CompletedTask);
    }

    [RelayCommand]
    private void Clear()
    {
        InputText  = string.Empty;
        OutputText = string.Empty;
        StatusText = "Enter text and click Encode, or paste Base64 and click Decode";
    }

    /// <summary>
    /// Pastes content and tries to decode it as Base64.
    /// If decoding fails (plain text), the content is left in the input for manual Encode.
    /// </summary>
    public override Task PasteAndFormat(string clipboardText)
    {
        InputText = clipboardText;
        var (result, _) = _service.Decode(clipboardText, IsUrlSafe);
        if (result != null)
            Decode();   // looks like Base64 — decode it
        // else: plain text — user can click Encode manually
        return Task.CompletedTask;
    }
}
