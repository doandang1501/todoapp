using System.IO;
using System.Windows;

namespace TodoApp;

/// <summary>
/// Custom entry point thay vì dùng Main() auto-generated từ App.xaml.
/// Bọc toàn bộ startup trong try/catch và ghi log ra file để debug.
/// </summary>
public static class Program
{
    private static readonly string LogFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TodoApp", "startup.log");

    [STAThread]
    public static void Main(string[] args)
    {
        // Đảm bảo thư mục log tồn tại TRƯỚC MỌI THỨ KHÁC
        Directory.CreateDirectory(Path.GetDirectoryName(LogFile)!);
        Log("=== TodoApp starting ===");
        Log($"OS: {Environment.OSVersion}");
        Log($"Runtime: {System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory()}");
        Log($"WorkDir: {Environment.CurrentDirectory}");

        try
        {
            Log("Creating App instance...");
            var app = new App();
            Log("App instance created OK.");

            // CRITICAL: must call InitializeComponent() to load App.xaml BAML
            // which sets up Application.Resources (merged dicts + inline resources).
            // Without this, ALL ResourceDictionary entries are never registered.
            Log("Calling app.InitializeComponent() to load App.xaml resources...");
            app.InitializeComponent();
            Log("InitializeComponent() done — resources registered.");

            Log("Calling App.Run()...");
            app.Run();
            Log("App.Run() returned — app exited normally.");
        }
        catch (Exception ex)
        {
            Log($"FATAL EXCEPTION: {ex.GetType().FullName}");
            Log($"Message: {ex.Message}");
            Log($"StackTrace:\n{ex.StackTrace}");

            if (ex.InnerException is not null)
            {
                Log($"InnerException: {ex.InnerException.GetType().FullName}");
                Log($"Inner Message: {ex.InnerException.Message}");
                Log($"Inner Stack:\n{ex.InnerException.StackTrace}");
            }

            MessageBox.Show(
                $"Lỗi nghiêm trọng khi khởi động:\n\n" +
                $"{ex.GetType().Name}: {ex.Message}\n\n" +
                $"Chi tiết đã ghi vào:\n{LogFile}",
                "TodoApp — Fatal Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    internal static void Log(string message)
    {
        try
        {
            File.AppendAllText(LogFile, $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n");
        }
        catch { /* Không làm crash nếu ghi log thất bại */ }
    }
}
