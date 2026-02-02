namespace TaskAgent.Contracts.Dtos;

public record CommentDto(int Id, string Content, int AuthorId, string AuthorName, string AuthorAvatar, DateTime CreatedAt, int? ProjectId, int? TaskId);
