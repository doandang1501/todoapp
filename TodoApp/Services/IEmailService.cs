using TodoApp.Core.Models;
using TodoApp.Core.Models.Settings;

namespace TodoApp.Services;

public interface IEmailService
{
    /// <summary>Send a task-due notification email.</summary>
    Task<bool> SendNotificationAsync(TodoItem task, NotificationEventArgs args, EmailSettings settings);

    /// <summary>Send a test email to verify configuration.</summary>
    Task<bool> SendTestEmailAsync(EmailSettings settings);
}
