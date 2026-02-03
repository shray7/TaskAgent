using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TaskAgent.Contracts.Dtos;

namespace TaskAgent.Api.Services;

public class BoardRealtimeNotifier : IBoardRealtimeNotifier
{
    private readonly HttpClient _http;
    private readonly string? _baseUrl;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public BoardRealtimeNotifier(HttpClient http, IOptions<RealtimeOptions> options)
    {
        _http = http;
        _baseUrl = options.Value.ServerUrl?.TrimEnd('/');
    }

    public async Task NotifyTaskCreatedAsync(TaskItemDto task, CancellationToken ct = default)
    {
        await BroadcastAsync("TaskCreated", task.ProjectId, task.SprintId, task, ct).ConfigureAwait(false);
    }

    public async Task NotifyTaskUpdatedAsync(TaskItemDto task, CancellationToken ct = default)
    {
        await BroadcastAsync("TaskUpdated", task.ProjectId, task.SprintId, task, ct).ConfigureAwait(false);
    }

    public async Task NotifyTaskDeletedAsync(int projectId, int? sprintId, int taskId, CancellationToken ct = default)
    {
        await BroadcastAsync("TaskDeleted", projectId, sprintId, new { taskId }, ct).ConfigureAwait(false);
    }

    private async Task BroadcastAsync(string eventName, int projectId, int? sprintId, object data, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_baseUrl)) return;

        var payload = new Dictionary<string, object?>
        {
            ["projectId"] = projectId,
            ["sprintId"] = sprintId,
            ["event"] = eventName,
            ["data"] = data
        };
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        try
        {
            var response = await _http.PostAsync($"{_baseUrl}/broadcast", content, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception)
        {
            // Fire-and-forget: do not fail the API request if realtime server is down
        }
    }
}

public class RealtimeOptions
{
    public const string SectionName = "Realtime";
    public string? ServerUrl { get; set; }
}
