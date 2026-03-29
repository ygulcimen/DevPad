using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DevPad.ViewModels;

public abstract partial class ToolViewModelBase : ViewModelBase
{
    public abstract string ToolName { get; }
    public abstract string IconPath { get; }

    // ── Copy micro-interaction labels ─────────────────────────────────────
    [ObservableProperty] private string _copyButtonText       = "Copy";
    [ObservableProperty] private string _copyOutputButtonText = "Copy Output";

    protected async Task FlashCopyAsync()
    {
        CopyButtonText = "✓ Copied!";
        await Task.Delay(1500);
        CopyButtonText = "Copy";
    }

    protected async Task FlashCopyOutputAsync()
    {
        CopyOutputButtonText = "✓ Copied!";
        await Task.Delay(1500);
        CopyOutputButtonText = "Copy Output";
    }

    // ── Abstract contract for global keyboard shortcuts ───────────────────
    public abstract void ExecuteClear();
    public abstract Task ExecuteCopyOutputAsync();
    public abstract Task SaveAsAsync();

    // ── Relay command wrapper so XAML can bind to SaveAsCommand ──────────
    [RelayCommand]
    private Task SaveAs() => SaveAsAsync();

    public abstract Task PasteAndFormat(string clipboardText);

    // ── Helper: get the app's StorageProvider ─────────────────────────────
    protected static IStorageProvider? GetStorageProvider()
    {
        var window = (Application.Current?.ApplicationLifetime
            as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        return window is null ? null : TopLevel.GetTopLevel(window)?.StorageProvider;
    }
}
