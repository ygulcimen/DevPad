using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DevPad.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public ObservableCollection<ToolViewModelBase> Tools { get; }

    [ObservableProperty]
    private ToolViewModelBase? _selectedTool;

    public MainWindowViewModel()
    {
        Tools = new ObservableCollection<ToolViewModelBase>
        {
            new JsonToolViewModel(),
            new XmlToolViewModel(),
            new JwtToolViewModel(),
            new Base64ToolViewModel(),
            new SqlToolViewModel()
        };

        SelectedTool = Tools[0];
    }

    // ── Ctrl+Shift+V — paste & format in active tool ─────────────────────
    [RelayCommand]
    private async Task GlobalPasteAndFormatAsync()
    {
        if (SelectedTool == null) return;

        string? text = null;
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            text = await (desktop.MainWindow?.Clipboard?.GetTextAsync() ?? Task.FromResult<string?>(null));

        if (!string.IsNullOrEmpty(text))
            await SelectedTool.PasteAndFormat(text);
    }

    // ── Ctrl+1–5 — switch tools ──────────────────────────────────────────
    [RelayCommand] private void SelectTool1() => SelectedTool = Tools[0];
    [RelayCommand] private void SelectTool2() => SelectedTool = Tools[1];
    [RelayCommand] private void SelectTool3() => SelectedTool = Tools[2];
    [RelayCommand] private void SelectTool4() => SelectedTool = Tools[3];
    [RelayCommand] private void SelectTool5() => SelectedTool = Tools[4];

    // ── Ctrl+L — clear active tool ───────────────────────────────────────
    [RelayCommand]
    private void GlobalClear() => SelectedTool?.ExecuteClear();

    // ── Ctrl+Shift+C — copy output of active tool ────────────────────────
    [RelayCommand]
    private async Task GlobalCopyOutputAsync()
    {
        if (SelectedTool != null)
            await SelectedTool.ExecuteCopyOutputAsync();
    }
}
