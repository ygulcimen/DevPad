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

public partial class XmlToolView : UserControl
{
    // VS Code-inspired colours for XML token types
    private static readonly SolidColorBrush TagPunctBrush  = new(Color.Parse("#808080")); // gray
    private static readonly SolidColorBrush TagNameBrush   = new(Color.Parse("#4EC9B0")); // teal
    private static readonly SolidColorBrush AttrNameBrush  = new(Color.Parse("#9CDCFE")); // light blue
    private static readonly SolidColorBrush AttrValueBrush = new(Color.Parse("#CE9178")); // orange
    private static readonly SolidColorBrush TextBrush      = new(Color.Parse("#D4D4D4")); // neutral
    private static readonly SolidColorBrush CommentBrush   = new(Color.Parse("#6A9955")); // green
    private static readonly SolidColorBrush CDataBrush     = new(Color.Parse("#CE9178")); // orange
    private static readonly SolidColorBrush DeclBrush      = new(Color.Parse("#C586C0")); // purple
    private static readonly SolidColorBrush LineNumBrush   = new(Color.Parse("#404040")); // dim gray

    private static readonly FontFamily MonoFont =
        new FontFamily("Cascadia Code, Consolas, Courier New, monospace");

    public XmlToolView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is XmlToolViewModel vm)
        {
            vm.PropertyChanged -= OnViewModelPropertyChanged;   // prevent double-subscription
            vm.PropertyChanged += OnViewModelPropertyChanged;
            if (!string.IsNullOrEmpty(vm.FormattedText))
                RebuildOutput(vm.FormattedText);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(XmlToolViewModel.FormattedText)
            && DataContext is XmlToolViewModel vm)
            RebuildOutput(vm.FormattedText);
    }

    private void RebuildOutput(string formattedText)
    {
        OutputLines.Children.Clear();

        if (string.IsNullOrEmpty(formattedText))
        {
            FormattedEmptyHint.IsVisible = true;
            OutputScroller.IsVisible     = false;
            return;
        }

        var lines = XmlSyntaxHighlighter.TokenizeByLine(formattedText);
        int padWidth = lines.Count.ToString().Length;

        foreach (var (lineTokens, lineIndex) in lines.Select((t, i) => (t, i + 1)))
        {
            var block = new TextBlock
            {
                FontFamily   = MonoFont,
                FontSize     = 13,
                TextWrapping = Avalonia.Media.TextWrapping.NoWrap,
            };

            block.Inlines!.Add(new Run
            {
                Text       = lineIndex.ToString().PadLeft(padWidth) + " │ ",
                Foreground = LineNumBrush
            });

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

    private static IBrush GetBrush(XmlTokenType type) => type switch
    {
        XmlTokenType.TagPunct      => TagPunctBrush,
        XmlTokenType.TagName       => TagNameBrush,
        XmlTokenType.AttributeName => AttrNameBrush,
        XmlTokenType.AttributeValue => AttrValueBrush,
        XmlTokenType.Comment       => CommentBrush,
        XmlTokenType.CData         => CDataBrush,
        XmlTokenType.Declaration   => DeclBrush,
        _                          => TextBrush
    };
}
