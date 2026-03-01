using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DevPad.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    /// <summary>Collection of available tools shown in the sidebar.</summary>
    public ObservableCollection<ToolViewModelBase> Tools { get; }

    /// <summary>Currently selected tool, displayed in the main content area.</summary>
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

        // Select JSON tool by default
        SelectedTool = Tools[0];
    }

    /// <summary>
    /// Global Ctrl+Shift+V handler.
    /// Reads the clipboard then delegates paste+format to the active tool.
    /// </summary>
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
}
