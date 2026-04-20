using System.Windows;

namespace TodoApp.Views;

public partial class ConfirmDeleteDialog : Window
{
    public ConfirmDeleteDialog(string taskTitle)
    {
        InitializeComponent();
        TaskNameText.Text = $"\"{taskTitle}\"";
    }

    private void DeleteBtn_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void CancelBtn_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void CloseBtn_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
