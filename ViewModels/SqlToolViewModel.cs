using System.Collections.Generic;
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

public partial class SqlToolViewModel : ToolViewModelBase
{
    private static readonly SqlFormatterService _service = new();

    public override string ToolName => "SQL";

    public override string IconPath =>
        "M12 2C6.47715 2 2 4.01472 2 6.5V17.5C2 19.9853 6.47715 22 12 22C17.5228 22 22 19.9853 22 17.5V6.5C22 4.01472 17.5228 2 12 2ZM12 4C16.4183 4 20 5.56701 20 6.5C20 7.43299 16.4183 9 12 9C7.58172 9 4 7.43299 4 6.5C4 5.56701 7.58172 4 12 4ZM4 9.22857C5.78667 10.3524 8.70375 11 12 11C15.2962 11 18.2133 10.3524 20 9.22857V12.5C20 13.433 16.4183 15 12 15C7.58172 15 4 13.433 4 12.5V9.22857ZM4 15.2286C5.78667 16.3524 8.70375 17 12 17C15.2962 17 18.2133 16.3524 20 15.2286V17.5C20 18.433 16.4183 20 12 20C7.58172 20 4 18.433 4 17.5V15.2286Z";

    [ObservableProperty] private string _inputText  = string.Empty;
    [ObservableProperty] private string _statusText = "Paste SQL to format";

    private IReadOnlyList<SqlToken> _segments = [];
    public IReadOnlyList<SqlToken> Segments
    {
        get => _segments;
        private set { _segments = value; OnPropertyChanged(); }
    }

    partial void OnInputTextChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) { Segments = []; StatusText = "Paste SQL to format"; return; }
        var formatted = _service.Format(value);
        Segments   = _service.Tokenize(formatted);
        StatusText = $"Formatted · {formatted.Length:N0} chars";
    }

    private string BuildPlainText()
    {
        var sb = new StringBuilder();
        foreach (var seg in Segments) sb.Append(seg.Text);
        return sb.ToString();
    }

    [RelayCommand]
    private async Task CopyAsync()
    {
        if (Segments.Count == 0) return;
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime d)
            await (d.MainWindow?.Clipboard?.SetTextAsync(BuildPlainText()) ?? Task.CompletedTask);
        await FlashCopyAsync();
    }

    [RelayCommand]
    private void Clear() => ExecuteClear();

    public override void ExecuteClear()
    {
        InputText = string.Empty; Segments = []; StatusText = "Paste SQL to format";
    }

    public override async Task ExecuteCopyOutputAsync()
    {
        if (Segments.Count == 0) return;
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime d)
            await (d.MainWindow?.Clipboard?.SetTextAsync(BuildPlainText()) ?? Task.CompletedTask);
        await FlashCopyAsync();
    }

    public override async Task SaveAsAsync()
    {
        if (Segments.Count == 0) return;
        var sp = GetStorageProvider(); if (sp is null) return;

        var file = await sp.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title             = "Save SQL",
            SuggestedFileName = "output.sql",
            FileTypeChoices   = new[] { new FilePickerFileType("SQL") { Patterns = new[] { "*.sql" } } }
        });
        if (file is null) return;

        await using var stream = await file.OpenWriteAsync();
        await using var writer = new StreamWriter(stream, Encoding.UTF8);
        await writer.WriteAsync(BuildPlainText());
    }

    public override Task PasteAndFormat(string clipboardText)
    {
        InputText = clipboardText; return Task.CompletedTask;
    }
}
