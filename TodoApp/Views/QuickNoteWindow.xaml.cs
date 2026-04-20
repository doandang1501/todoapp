using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TodoApp.ViewModels;

namespace TodoApp.Views;

public partial class QuickNoteWindow : Window
{
    private readonly QuickNoteViewModel _vm;

    public QuickNoteWindow(QuickNoteViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        _vm.Reset();
        DataContext = vm;
        vm.CloseRequested += _ => Close();

        Loaded += (_, _) => NoteBox.Focus();

        // Close on Escape anywhere in the window
        InputBindings.Add(new KeyBinding(vm.CancelCommand, new KeyGesture(Key.Escape)));
    }

    // ── Drag window by header ──────────────────────────────────────────────────

    private void OnHeaderMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    // ── Keyboard shortcuts in NoteBox ─────────────────────────────────────────

    private void NoteBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyboardDevice.Modifiers != ModifierKeys.Control) return;
        switch (e.Key)
        {
            case Key.B: WrapSelection("**", "**"); e.Handled = true; break;
            case Key.I: WrapSelection("*",  "*");  e.Handled = true; break;
            case Key.U: WrapSelection("__", "__"); e.Handled = true; break;
        }
    }

    // ── Format toolbar ────────────────────────────────────────────────────────

    private void OnFmtBold(object sender, RoutedEventArgs e)      => WrapSelection("**", "**");
    private void OnFmtItalic(object sender, RoutedEventArgs e)    => WrapSelection("*",  "*");
    private void OnFmtUnderline(object sender, RoutedEventArgs e) => WrapSelection("__", "__");
    private void OnFmtCode(object sender, RoutedEventArgs e)      => WrapSelection("`",  "`");
    private void OnFmtList(object sender, RoutedEventArgs e)      => PrependLine("- ");

    private void OnFmtLink(object sender, RoutedEventArgs e)
    {
        var    tb  = NoteBox;
        int    pos = tb.SelectionStart;
        string sel = tb.SelectedText;

        string clipUrl = "url";
        try
        {
            var clip = Clipboard.GetText();
            if (!string.IsNullOrWhiteSpace(clip) &&
                (clip.StartsWith("http://",  StringComparison.OrdinalIgnoreCase) ||
                 clip.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
            {
                clipUrl = clip.Trim();
            }
        }
        catch { }

        if (sel.Length > 0)
        {
            string replacement = $"[{sel}]({clipUrl})";
            tb.Text = tb.Text.Remove(pos, sel.Length).Insert(pos, replacement);
            tb.Select(pos + sel.Length + 3, clipUrl.Length);
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
            int urlStart = lineStart + linkText.Length + 3;
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

    private void WrapSelection(string before, string after)
    {
        var    tb    = NoteBox;
        int    start = tb.SelectionStart;
        int    len   = tb.SelectionLength;
        string sel   = tb.SelectedText;

        string insert = before + sel + after;
        tb.Text = tb.Text.Remove(start, len).Insert(start, insert);
        tb.CaretIndex = len == 0 ? start + before.Length : start + insert.Length;
        tb.Focus();
    }

    private void PrependLine(string prefix)
    {
        var    tb    = NoteBox;
        int    caret = tb.CaretIndex;
        string txt   = tb.Text;

        int lineStart = txt.LastIndexOf('\n', Math.Max(0, caret - 1));
        lineStart = lineStart < 0 ? 0 : lineStart + 1;

        tb.Text       = txt.Insert(lineStart, prefix);
        tb.CaretIndex = caret + prefix.Length;
        tb.Focus();
    }
}
