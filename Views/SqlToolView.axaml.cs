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

public partial class SqlToolView : UserControl
{
    // VS Code-style colours for each SQL token type
    private static readonly SolidColorBrush KeywordBrush = new(Color.Parse("#569CD6")); // blue
    private static readonly SolidColorBrush StringBrush  = new(Color.Parse("#CE9178")); // orange
    private static readonly SolidColorBrush CommentBrush = new(Color.Parse("#6A9955")); // green
    private static readonly SolidColorBrush NumberBrush  = new(Color.Parse("#B5CEA8")); // light green
    private static readonly SolidColorBrush DefaultBrush = new(Color.Parse("#D4D4D4")); // neutral
    private static readonly SolidColorBrush LineNumBrush = new(Color.Parse("#404040")); // dim gray

    private static readonly FontFamily MonoFont =
        new FontFamily("Cascadia Code, Consolas, Courier New, monospace");

    public SqlToolView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is SqlToolViewModel vm)
        {
            vm.PropertyChanged -= OnViewModelPropertyChanged;   // prevent double-subscription
            vm.PropertyChanged += OnViewModelPropertyChanged;
            if (vm.Segments.Count > 0)
                RebuildOutput(vm.Segments);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SqlToolViewModel.Segments)
            && DataContext is SqlToolViewModel vm)
            RebuildOutput(vm.Segments);
    }

    /// <summary>
    /// Groups tokens by line and renders each line as a TextBlock with a
    /// grey line-number gutter, matching the JSON and XML output panels.
    /// </summary>
    private void RebuildOutput(IReadOnlyList<SqlToken> segments)
    {
        OutputLines.Children.Clear();

        if (segments.Count == 0)
        {
            EmptyHint.IsVisible      = true;
            OutputScroller.IsVisible = false;
            return;
        }

        var lines    = GroupByLine(segments);
        int padWidth = lines.Count.ToString().Length;

        foreach (var (lineTokens, lineIndex) in lines.Select((t, i) => (t, i + 1)))
        {
            var block = new TextBlock
            {
                FontFamily   = MonoFont,
                FontSize     = 13,
                TextWrapping = Avalonia.Media.TextWrapping.NoWrap,
            };

            // Line-number gutter
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

        EmptyHint.IsVisible      = false;
        OutputScroller.IsVisible = true;
    }

    /// <summary>Groups a flat token list into per-line lists, splitting on embedded newlines.</summary>
    private static List<List<SqlToken>> GroupByLine(IReadOnlyList<SqlToken> segments)
    {
        var lines   = new List<List<SqlToken>>();
        var current = new List<SqlToken>();

        foreach (var seg in segments)
        {
            if (!seg.Text.Contains('\n'))
            {
                current.Add(seg);
                continue;
            }

            string[] parts = seg.Text.Split('\n');

            if (parts[0].Length > 0)
                current.Add(new SqlToken(parts[0], seg.Type));
            lines.Add(current);

            for (int m = 1; m < parts.Length - 1; m++)
            {
                var mid = new List<SqlToken>();
                if (parts[m].Length > 0)
                    mid.Add(new SqlToken(parts[m], seg.Type));
                lines.Add(mid);
            }

            current = new List<SqlToken>();
            if (parts[^1].Length > 0)
                current.Add(new SqlToken(parts[^1], seg.Type));
        }

        if (current.Count > 0 || lines.Count == 0)
            lines.Add(current);

        return lines;
    }

    private static IBrush GetBrush(SqlTokenType type) => type switch
    {
        SqlTokenType.Keyword         => KeywordBrush,
        SqlTokenType.StringLiteral   => StringBrush,
        SqlTokenType.LineComment
        or SqlTokenType.BlockComment => CommentBrush,
        SqlTokenType.Number          => NumberBrush,
        _                            => DefaultBrush
    };
}
