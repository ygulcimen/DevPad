using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using DevPad.Services;
using DevPad.ViewModels;

namespace DevPad.Views;

public partial class JsonToolView : UserControl
{
    // VS Code-inspired colours for JSON token types
    private static readonly SolidColorBrush KeyBrush     = new(Color.Parse("#9CDCFE")); // light blue
    private static readonly SolidColorBrush StringBrush  = new(Color.Parse("#CE9178")); // orange
    private static readonly SolidColorBrush NumberBrush  = new(Color.Parse("#B5CEA8")); // light green
    private static readonly SolidColorBrush BoolBrush    = new(Color.Parse("#569CD6")); // blue
    private static readonly SolidColorBrush NullBrush    = new(Color.Parse("#569CD6")); // blue
    private static readonly SolidColorBrush PunctBrush   = new(Color.Parse("#D4D4D4")); // neutral
    private static readonly SolidColorBrush LineNumBrush = new(Color.Parse("#404040")); // dim gray

    private static readonly FontFamily MonoFont =
        new FontFamily("Cascadia Code, Consolas, Courier New, monospace");

    public JsonToolView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is JsonToolViewModel vm)
        {
            vm.PropertyChanged -= OnViewModelPropertyChanged;   // prevent double-subscription
            vm.PropertyChanged += OnViewModelPropertyChanged;
            // Show any text that was already formatted before this view was created
            if (!string.IsNullOrEmpty(vm.FormattedText))
                RebuildOutput(vm.FormattedText);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(JsonToolViewModel.FormattedText)
            && DataContext is JsonToolViewModel vm)
            RebuildOutput(vm.FormattedText);
    }

    /// <summary>
    /// Clears and rebuilds the syntax-highlighted output lines.
    /// Each line becomes a TextBlock with a grey line-number prefix and coloured code spans.
    /// </summary>
    private void RebuildOutput(string formattedText)
    {
        OutputLines.Children.Clear();

        if (string.IsNullOrEmpty(formattedText))
        {
            FormattedEmptyHint.IsVisible = true;
            OutputScroller.IsVisible     = false;
            return;
        }

        var lines = JsonSyntaxHighlighter.TokenizeByLine(formattedText);
        int padWidth = lines.Count.ToString().Length;

        foreach (var (lineTokens, lineIndex) in lines.Select((t, i) => (t, i + 1)))
        {
            var block = new SelectableTextBlock
            {
                FontFamily  = MonoFont,
                FontSize    = 13,
                TextWrapping = Avalonia.Media.TextWrapping.NoWrap,
            };

            // Line number gutter: right-padded grey number + separator
            block.Inlines!.Add(new Run
            {
                Text       = lineIndex.ToString().PadLeft(padWidth) + " │ ",
                Foreground = LineNumBrush
            });

            // Syntax-highlighted tokens for this line
            foreach (var tok in lineTokens)
            {
                block.Inlines.Add(new Run
                {
                    Text       = tok.Text,
                    Foreground = GetBrush(tok.Type)
                });
            }

            OutputLines.Children.Add(block);
        }

        FormattedEmptyHint.IsVisible = false;
        OutputScroller.IsVisible     = true;
    }

    private static IBrush GetBrush(JsonTokenType type) => type switch
    {
        JsonTokenType.Key         => KeyBrush,
        JsonTokenType.StringValue => StringBrush,
        JsonTokenType.Number      => NumberBrush,
        JsonTokenType.Boolean     => BoolBrush,
        JsonTokenType.Null        => NullBrush,
        _                         => PunctBrush
    };
}
