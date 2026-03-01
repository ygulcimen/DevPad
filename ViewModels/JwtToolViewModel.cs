using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevPad.Models;
using DevPad.Services;

namespace DevPad.ViewModels;

public partial class JwtToolViewModel : ToolViewModelBase
{
    private static readonly JwtDecoderService _service = new();

    private static readonly SolidColorBrush ValidBrush    = new(Color.Parse("#4CAF50"));
    private static readonly SolidColorBrush ExpiredBrush  = new(Color.Parse("#F48771"));
    private static readonly SolidColorBrush WarnBrush     = new(Color.Parse("#CCA700"));
    private static readonly SolidColorBrush NeutralBrush  = new(Color.Parse("#8E8E8E"));

    public override string ToolName => "JWT";

    // Key/token icon
    public override string IconPath =>
        "M21 2L19 4M11.3891 11.6109C12.3844 12.6062 12.3844 14.2073 11.3891 15.2026C10.3938 16.1979 8.79271 16.1979 7.79741 15.2026C6.80211 14.2073 6.80211 12.6062 7.79741 11.6109C8.79271 10.6156 10.3938 10.6156 11.3891 11.6109ZM11.3891 11.6109L15.5 7.5M15.5 7.5L18 10L21 7L18.5 4.5M15.5 7.5L18.5 4.5M6.5 21L3 17.5L6.5 14M6.5 14L10 17.5L6.5 21";

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private string _statusText = "Paste a JWT to decode";

    // Decoded output fields
    [ObservableProperty]
    private string _headerText = string.Empty;

    [ObservableProperty]
    private string _payloadText = string.Empty;

    [ObservableProperty]
    private string _algorithmText = string.Empty;

    [ObservableProperty]
    private string _tokenTypeText = string.Empty;

    [ObservableProperty]
    private string _expiryText = string.Empty;

    [ObservableProperty]
    private IBrush _expiryForeground = NeutralBrush;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoResult))]
    private bool _hasResult;

    public bool HasNoResult => !HasResult;

    // Stored for copy commands
    private string _rawHeader  = string.Empty;
    private string _rawPayload = string.Empty;

    /// <summary>Auto-decode whenever the input changes.</summary>
    partial void OnInputTextChanged(string value) => TryDecode(value);

    [RelayCommand]
    private void Decode() => TryDecode(InputText);

    private void TryDecode(string jwt)
    {
        if (string.IsNullOrWhiteSpace(jwt))
        {
            HasResult = false;
            StatusText = "Paste a JWT to decode";
            ClearFields();
            return;
        }

        var (token, error) = _service.Decode(jwt);

        if (token == null)
        {
            HasResult = false;
            StatusText = error ?? "Invalid JWT";
            ClearFields();
            return;
        }

        HasResult      = true;
        StatusText     = "Decoded successfully";
        HeaderText     = token.FormattedHeader;
        PayloadText    = token.FormattedPayload;
        AlgorithmText  = $"alg: {token.Algorithm}";
        TokenTypeText  = $"typ: {token.TokenType}";
        ExpiryText     = token.ExpiryMessage;
        ExpiryForeground = token.ExpiryStatus switch
        {
            JwtExpiryStatus.Valid      => ValidBrush,
            JwtExpiryStatus.Expired    => ExpiredBrush,
            JwtExpiryStatus.NotYetValid => WarnBrush,
            _                          => NeutralBrush
        };

        // Keep raw parts for copy commands
        _rawHeader  = token.FormattedHeader;
        _rawPayload = token.FormattedPayload;
    }

    private void ClearFields()
    {
        HeaderText = PayloadText = AlgorithmText = TokenTypeText = ExpiryText = string.Empty;
        ExpiryForeground = NeutralBrush;
        _rawHeader = _rawPayload = string.Empty;
    }

    [RelayCommand]
    private async Task CopyPayloadAsync()
    {
        if (string.IsNullOrWhiteSpace(_rawPayload)) return;
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            await (desktop.MainWindow?.Clipboard?.SetTextAsync(_rawPayload) ?? Task.CompletedTask);
    }

    [RelayCommand]
    private async Task CopyHeaderAsync()
    {
        if (string.IsNullOrWhiteSpace(_rawHeader)) return;
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            await (desktop.MainWindow?.Clipboard?.SetTextAsync(_rawHeader) ?? Task.CompletedTask);
    }

    [RelayCommand]
    private void Clear()
    {
        InputText = string.Empty;
    }

    // OnInputTextChanged already auto-decodes; setting InputText is all that's needed.
    public override Task PasteAndFormat(string clipboardText)
    {
        InputText = clipboardText;
        return Task.CompletedTask;
    }
}
