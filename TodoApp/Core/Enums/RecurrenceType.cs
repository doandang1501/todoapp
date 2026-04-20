namespace TodoApp.Core.Enums;

/// <summary>Determines how often a recurring task template generates new instances.</summary>
public enum RecurrenceType
{
    None    = 0,
    Daily   = 1,
    Weekly  = 2,
    Monthly = 3,
    Custom  = 4   // Custom interval in days
}
