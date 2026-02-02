namespace TaskAgent.Contracts.Requests;

public record CreateCommentRequest(string Content, int AuthorId, int? ProjectId, int? TaskId);
