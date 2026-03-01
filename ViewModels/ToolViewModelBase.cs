using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DevPad.ViewModels;

/// <summary>
/// Base class for all tool ViewModels.
/// Contains common properties like tool name and icon.
/// </summary>
public abstract partial class ToolViewModelBase : ViewModelBase
{
    /// <summary>
    /// Display name shown in the sidebar.
    /// </summary>
    public abstract string ToolName { get; }

    /// <summary>
    /// SVG path data for the tool icon.
    /// </summary>
    public abstract string IconPath { get; }

    /// <summary>
    /// Called by the global Ctrl+Shift+V handler.
    /// Sets the tool's input to <paramref name="clipboardText"/> and runs the appropriate auto-format.
    /// </summary>
    public abstract Task PasteAndFormat(string clipboardText);
}
