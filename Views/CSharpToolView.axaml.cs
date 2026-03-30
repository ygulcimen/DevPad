#nullable enable

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

public partial class CSharpToolView : UserControl
{
    // VS Code Dark+ theme colors for C#
    private static readonly SolidColorBrush KeywordBrush      = new(Color.Parse("#569CD6")); // Blue
    private static readonly SolidColorBrush StringBrush       = new(Color.Parse("#CE9178")); // Orange
    private static readonly SolidColorBrush NumberBrush       = new(Color.Parse("#B5CEA8")); // Light green
    private static readonly SolidColorBrush CommentBrush      = new(Color.Parse("#6A9955")); // Green
    private static readonly SolidColorBrush PreprocessorBrush = new(Color.Parse("#569CD6")); // Blue
    private static readonly SolidColorBrush IdentifierBrush   = new(Color.Parse("#9CDCFE")); // Light blue
    private static readonly SolidColorBrush PunctuationBrush  = new(Color.Parse("#D4D4D4")); // Light gray
    private static readonly SolidColorBrush LineNumBrush      = new(Color.Parse("#404040")); // Dim gray
    private static readonly SolidColorBrush TypeBrush         = new(Color.Parse("#4EC9B0")); // Teal (classes/types)

    private static readonly HashSet<string> TypeKeywords = new(StringComparer.Ordinal)
    {
        "bool", "byte", "char", "decimal", "double", "float", "int", "long", 
        "object", "sbyte", "short", "string", "uint", "ulong", "ushort", "void",
        "var", "dynamic", "nint", "nuint"
    };

    private static readonly FontFamily MonoFont =
        new FontFamily("Cascadia Code, Consolas, Courier New, monospace");

    public CSharpToolView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is CSharpToolViewModel vm)
        {
            vm.PropertyChanged -= OnViewModelPropertyChanged;
            vm.PropertyChanged += OnViewModelPropertyChanged;
            
            if (!string.IsNullOrEmpty(vm.FormattedText))
                RebuildOutput(vm.FormattedText);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CSharpToolViewModel.FormattedText)
            && DataContext is CSharpToolViewModel vm)
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
            OutputScroller.IsVisible = false;
            return;
        }

        var lines = CSharpSyntaxHighlighter.TokenizeByLine(formattedText);
        int padWidth = lines.Count.ToString().Length;

        foreach (var (lineTokens, lineIndex) in lines.Select((t, i) => (t, i + 1)))
        {
            var block = new SelectableTextBlock
            {
                FontFamily = MonoFont,
                FontSize = 13,
                TextWrapping = Avalonia.Media.TextWrapping.NoWrap,
            };

            // Line number gutter: right-padded grey number + separator
            block.Inlines!.Add(new Run
            {
                Text = lineIndex.ToString().PadLeft(padWidth) + " │ ",
                Foreground = LineNumBrush
            });

            // Syntax-highlighted tokens for this line
            foreach (var tok in lineTokens)
            {
                block.Inlines.Add(new Run
                {
                    Text = tok.Text,
                    Foreground = GetBrush(tok)
                });
            }

            OutputLines.Children.Add(block);
        }

        FormattedEmptyHint.IsVisible = false;
        OutputScroller.IsVisible = true;
    }

    private static IBrush GetBrush(CSharpToken tok)
    {
        // Check if identifier is actually a type
        if (tok.Type == CSharpTokenType.Identifier && TypeKeywords.Contains(tok.Text))
            return TypeBrush;

        return tok.Type switch
        {
            CSharpTokenType.Keyword       => KeywordBrush,
            CSharpTokenType.String        => StringBrush,
            CSharpTokenType.Number        => NumberBrush,
            CSharpTokenType.Comment       => CommentBrush,
            CSharpTokenType.Preprocessor  => PreprocessorBrush,
            CSharpTokenType.Identifier    => IdentifierBrush,
            CSharpTokenType.Punctuation   => PunctuationBrush,
            _                             => PunctuationBrush
        };
    }
}