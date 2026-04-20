namespace TodoApp.Core.Models;

public class Goal
{
    public Guid      Id              { get; set; } = Guid.NewGuid();
    public string    Title           { get; set; } = string.Empty;
    public string    Description     { get; set; } = string.Empty;
    public int       TotalDays       { get; set; } = 30;
    public int       CompletedDays   { get; set; } = 0;
    public DateTime  CreatedAt       { get; set; } = DateTime.Now;
    public DateTime? LastCheckedDate { get; set; }
    public bool      IsCompleted     { get; set; } = false;
    public DateTime? CompletedAt     { get; set; }
}
