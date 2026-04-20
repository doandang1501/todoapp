using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TodoApp.ViewModels;

namespace TodoApp.Views;

public partial class TaskDetailDialog : Window
{
    public TaskDetailDialog(TaskDetailViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.CloseRequested += result =>
        {
            DialogResult = result;
            Close();
        };
        _ = vm.LoadLabelsAsync();
    }

    // ── Title bar drag ────────────────────────────────────────────────────────

    private void OnTitleBarMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    // ── Keyboard shortcuts (Ctrl+B / I / U) ──────────────────────────────────

    private void DescriptionBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyboardDevice.Modifiers != ModifierKeys.Control) return;
        switch (e.Key)
        {
            case Key.B: WrapSelection("**", "**"); e.Handled = true; break;
            case Key.I: WrapSelection("*",  "*");  e.Handled = true; break;
            case Key.U: WrapSelection("__", "__"); e.Handled = true; break;
        }
    }

    // ── Markdown format toolbar ───────────────────────────────────────────────

    private void OnFmtBold(object sender, RoutedEventArgs e)   => WrapSelection("**", "**");
    private void OnFmtItalic(object sender, RoutedEventArgs e) => WrapSelection("*",  "*");
    private void OnFmtCode(object sender, RoutedEventArgs e)   => WrapSelection("`",  "`");
    private void OnFmtH1(object sender, RoutedEventArgs e)     => PrependLine("# ");
    private void OnFmtList(object sender, RoutedEventArgs e)   => PrependLine("- ");

    private void OnFmtLink(object sender, RoutedEventArgs e)
    {
        var    tb  = DescriptionBox;
        int    pos = tb.SelectionStart;
        string sel = tb.SelectedText;

        // Try to get a URL from the clipboard
        string clipUrl = "url";
        try
        {
            var clip = Clipboard.GetText();
            if (!string.IsNullOrWhiteSpace(clip) &&
                (clip.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                 clip.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
            {
                clipUrl = clip.Trim();
            }
        }
        catch { /* clipboard unavailable — use placeholder */ }

        if (sel.Length > 0)
        {
            // Wrap selected text
            string replacement = $"[{sel}]({clipUrl})";
            tb.Text = tb.Text.Remove(pos, sel.Length).Insert(pos, replacement);
            int urlStart = pos + sel.Length + 3;
            tb.Select(urlStart, clipUrl.Length);
        }
        else
        {
            // No selection — replace the entire current line with [lineText](url)
            string txt = tb.Text;
            int lineStart = txt.LastIndexOf('\n', Math.Max(0, pos - 1));
            lineStart = lineStart < 0 ? 0 : lineStart + 1;
            int lineEnd = txt.IndexOf('\n', pos);
            if (lineEnd < 0) lineEnd = txt.Length;
            string lineContent = txt[lineStart..lineEnd].Trim();
            string linkText = lineContent.Length > 0 ? lineContent : "link text";
            string replacement = $"[{linkText}]({clipUrl})";
            tb.Text = txt.Remove(lineStart, lineEnd - lineStart).Insert(lineStart, replacement);
            // Select the URL placeholder so user can type over it
            int urlStart = lineStart + linkText.Length + 3; // after "[linkText]("
            tb.Select(urlStart, clipUrl.Length);
        }
        tb.Focus();
    }

    private static string GetCurrentLineText(TextBox tb)
    {
        int    caret = tb.CaretIndex;
        string txt   = tb.Text;
        if (txt.Length == 0) return string.Empty;

        int lineStart = txt.LastIndexOf('\n', Math.Max(0, caret - 1));
        lineStart = lineStart < 0 ? 0 : lineStart + 1;

        int lineEnd = txt.IndexOf('\n', caret);
        if (lineEnd < 0) lineEnd = txt.Length;

        return txt[lineStart..lineEnd].Trim();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Wraps the current selection (or inserts empty markers at the caret)
    /// with <paramref name="before"/> and <paramref name="after"/>.
    /// </summary>
    private void WrapSelection(string before, string after)
    {
        var    tb    = DescriptionBox;
        int    start = tb.SelectionStart;
        int    len   = tb.SelectionLength;
        string sel   = tb.SelectedText;

        string insert = before + sel + after;
        tb.Text = tb.Text.Remove(start, len).Insert(start, insert);

        // Place caret: if nothing was selected, put cursor between markers
        // so the user can start typing immediately.
        tb.CaretIndex = len == 0 ? start + before.Length : start + insert.Length;
        tb.Focus();
    }

    /// <summary>
    /// Inserts <paramref name="prefix"/> at the beginning of the line that
    /// currently contains the caret.
    /// </summary>
    private void PrependLine(string prefix)
    {
        var    tb    = DescriptionBox;
        int    caret = tb.CaretIndex;
        string txt   = tb.Text;

        // Find the start of the current line
        int lineStart = txt.LastIndexOf('\n', Math.Max(0, caret - 1));
        lineStart = lineStart < 0 ? 0 : lineStart + 1;

        tb.Text       = txt.Insert(lineStart, prefix);
        tb.CaretIndex = caret + prefix.Length;
        tb.Focus();
    }
}
