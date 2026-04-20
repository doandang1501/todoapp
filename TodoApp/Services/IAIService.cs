using TodoApp.Core.Enums;

namespace TodoApp.Services;

public record AITagSuggestion(
    List<string> Tags,
    bool         Success,
    string?      Error = null);

public record AITaskParseResult(
    string       Title,
    DateTime?    Deadline,
    Priority?    Priority,
    List<string> Tags,
    bool         Success,
    string?      Error = null);

public interface IAIService
{
    Task<bool>              IsAvailableAsync();
    Task<AITagSuggestion>   SuggestTagsAsync(string title, string description, CancellationToken ct = default);
    Task<AITaskParseResult> ParseTaskAsync(string input, CancellationToken ct = default);
}
