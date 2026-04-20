using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace TodoApp.Controls;

/// <summary>
/// A TextBlock that renders a small subset of Markdown inline.
/// Supported: **bold**, *italic*, `code`, # headings, - bullet lists,
/// [link text](url).  Newlines become LineBreaks.
/// </summary>
public sealed class MarkdownTextBlock : TextBlock
{
    public static readonly DependencyProperty MarkdownTextProperty =
        DependencyProperty.Register(
            nameof(MarkdownText),
            typeof(string),
            typeof(MarkdownTextBlock),
            new FrameworkPropertyMetadata(null, OnMarkdownTextChanged));

    public string? MarkdownText
    {
        get => (string?)GetValue(MarkdownTextProperty);
        set => SetValue(MarkdownTextProperty, value);
    }

    private static void OnMarkdownTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MarkdownTextBlock tb)
            tb.Rebuild(e.NewValue as string);
    }

    // ── Parser ────────────────────────────────────────────────────────────────

    private void Rebuild(string? text)
    {
        Inlines.Clear();
        if (string.IsNullOrEmpty(text)) return;

        var lines = text.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            if (i > 0) Inlines.Add(new LineBreak());
            ParseLine(lines[i]);
        }
    }

    private void ParseLine(string line)
    {
        // Bullet list items  (- or *)
        if (line.Length > 2 && (line.StartsWith("- ") || line.StartsWith("* ")))
        {
            Inlines.Add(new Run("• "));
            ParseInlines(line[2..]);
            return;
        }

        // Headings  (#, ##, ###)
        if (line.StartsWith("### "))
        {
            Inlines.Add(HeadingRun(line[4..], 13));
            return;
        }
        if (line.StartsWith("## "))
        {
            Inlines.Add(HeadingRun(line[3..], 15));
            return;
        }
        if (line.StartsWith("# "))
        {
            Inlines.Add(HeadingRun(line[2..], 17));
            return;
        }

        ParseInlines(line);
    }

    private static Run HeadingRun(string text, double size) =>
        new(text) { FontSize = size, FontWeight = FontWeights.Bold };

    // ── Inline parser (state machine) ────────────────────────────────────────

    private void ParseInlines(string text)
    {
        var sb = new StringBuilder();
        var i  = 0;

        void FlushPlain()
        {
            if (sb.Length == 0) return;
            Inlines.Add(new Run(sb.ToString()));
            sb.Clear();
        }

        while (i < text.Length)
        {
            var c = text[i];

            // Underline: __...__
            if (c == '_' && i + 1 < text.Length && text[i + 1] == '_')
            {
                var end = text.IndexOf("__", i + 2, StringComparison.Ordinal);
                if (end > i + 2)
                {
                    FlushPlain();
                    var uRun = new Run(text[(i + 2)..end]);
                    uRun.TextDecorations = System.Windows.TextDecorations.Underline;
                    Inlines.Add(new Span(uRun));
                    i = end + 2;
                    continue;
                }
            }

            // Bold: **...**
            if (c == '*' && i + 1 < text.Length && text[i + 1] == '*')
            {
                var end = text.IndexOf("**", i + 2, StringComparison.Ordinal);
                if (end > i + 2)
                {
                    FlushPlain();
                    Inlines.Add(new Bold(new Run(text[(i + 2)..end])));
                    i = end + 2;
                    continue;
                }
            }

            // Italic: *...* (single star, not double)
            if (c == '*' && (i + 1 >= text.Length || text[i + 1] != '*'))
            {
                var end = text.IndexOf('*', i + 1);
                if (end > i + 1)
                {
                    FlushPlain();
                    Inlines.Add(new Italic(new Run(text[(i + 1)..end])));
                    i = end + 1;
                    continue;
                }
            }

            // Code: `...`
            if (c == '`')
            {
                var end = text.IndexOf('`', i + 1);
                if (end > i + 1)
                {
                    FlushPlain();
                    var codeRun = new Run(text[(i + 1)..end])
                    {
                        FontFamily = new FontFamily("Consolas,Courier New"),
                        Background = new SolidColorBrush(Color.FromArgb(0x22, 0x80, 0x80, 0x80))
                    };
                    Inlines.Add(new Span(codeRun));
                    i = end + 1;
                    continue;
                }
            }

            // Link: [label](url)
            if (c == '[')
            {
                var closeBracket = text.IndexOf(']', i + 1);
                if (closeBracket > i &&
                    closeBracket + 1 < text.Length &&
                    text[closeBracket + 1] == '(')
                {
                    var closeParen = text.IndexOf(')', closeBracket + 2);
                    if (closeParen > closeBracket + 2)
                    {
                        FlushPlain();
                        var label = text[(i + 1)..closeBracket];
                        var url   = text[(closeBracket + 2)..closeParen];
                        Inlines.Add(BuildLink(label, url));
                        i = closeParen + 1;
                        continue;
                    }
                }
            }

            sb.Append(c);
            i++;
        }

        FlushPlain();
    }

    private static Inline BuildLink(string label, string url)
    {
        try
        {
            var uri  = new Uri(url, UriKind.Absolute);
            var link = new Hyperlink(new Run(label)) { NavigateUri = uri };
            link.RequestNavigate += (_, e) =>
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            return link;
        }
        catch
        {
            return new Run(label);
        }
    }
}
