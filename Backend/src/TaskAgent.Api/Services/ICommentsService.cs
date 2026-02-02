using TaskAgent.Contracts.Dtos;
using TaskAgent.Contracts.Requests;

namespace TaskAgent.Api.Services;

public interface ICommentsService
{
    Task<IEnumerable<CommentDto>> GetByProjectAsync(int projectId, CancellationToken ct = default);
    Task<IEnumerable<CommentDto>> GetByTaskAsync(int taskId, CancellationToken ct = default);
    Task<(CommentDto? Dto, string? Error)> CreateAsync(CreateCommentRequest req, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
