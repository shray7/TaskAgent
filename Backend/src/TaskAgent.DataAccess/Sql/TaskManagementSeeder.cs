using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TaskAgent.DataAccess.Entities;

namespace TaskAgent.DataAccess.Sql;

/// <summary>
/// Seeds task management data for TaskAgent AI Support project.
/// </summary>
public static class TaskManagementSeeder
{
    // Demo password for all seeded users: "password123"
    private const string DemoPassword = "password123";

    // Project dates: Feb 2 - Apr 2, 2026 (2 months), 1-week sprints
    private static readonly DateTime ProjectStart = new(2026, 2, 2);
    private static readonly DateTime Sprint1Start = ProjectStart;
    private static readonly DateTime Sprint1End = ProjectStart.AddDays(6);  // Feb 2-8
    private static readonly DateTime Sprint2Start = ProjectStart.AddDays(7); // Feb 9
    private static readonly DateTime Sprint2End = ProjectStart.AddDays(13);  // Feb 9-15

    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (await db.AppUsers.AnyAsync(ct)) return; // Already seeded

        // Hash the demo password for all users
        var hashedPassword = HashPassword(DemoPassword);

        // Insert users first (let DB generate Ids to avoid IDENTITY_INSERT with managed identity)
        var users = new[]
        {
            new AppUser { Name = "Alice Johnson", Email = "alice@example.com", Avatar = "👩‍💼", PasswordHash = hashedPassword, CreatedAt = DateTime.UtcNow },
            new AppUser { Name = "Bob Smith", Email = "bob@example.com", Avatar = "👨‍💻", PasswordHash = hashedPassword, CreatedAt = DateTime.UtcNow },
            new AppUser { Name = "Carol Davis", Email = "carol@example.com", Avatar = "👩‍🎨", PasswordHash = hashedPassword, CreatedAt = DateTime.UtcNow },
            new AppUser { Name = "David Wilson", Email = "david@example.com", Avatar = "👨‍🔬", PasswordHash = hashedPassword, CreatedAt = DateTime.UtcNow }
        };
        db.AppUsers.AddRange(users);
        await db.SaveChangesAsync(ct);

        var userIds = users.Select(u => u.Id).ToArray();
        var defaultColumns = JsonSerializer.Serialize(new[] { "todo", "in-progress", "completed" });
        var allUserIds = JsonSerializer.Serialize(userIds);

        var project = new ProjectEntity
        {
            Name = "TaskAgent AI Support",
            Description = "AI-powered support features for TaskAgent - natural language task creation, smart suggestions, and intelligent prioritization",
            Color = "#6366f1",
            CreatedAt = ProjectStart.AddDays(-1),
            StartDate = ProjectStart,
            EndDate = ProjectStart.AddMonths(2), // Apr 2, 2026
            OwnerId = userIds[0],
            VisibleToUserIdsJson = allUserIds,
            SprintDurationDays = 7,
            VisibleColumnsJson = defaultColumns,
            TaskSizeUnit = "hours"
        };
        db.Projects.Add(project);
        await db.SaveChangesAsync(ct);

        var sprints = new[]
        {
            new SprintEntity { ProjectId = project.Id, Name = "Sprint 1", Goal = "AI integration foundation and prompt engineering setup", StartDate = Sprint1Start, EndDate = Sprint1End, Status = "active" },
            new SprintEntity { ProjectId = project.Id, Name = "Sprint 2", Goal = "Natural language task parsing and smart suggestions", StartDate = Sprint2Start, EndDate = Sprint2End, Status = "planning" }
        };
        db.Sprints.AddRange(sprints);
        await db.SaveChangesAsync(ct);

        // Tasks for each user across Sprint 1 and 2 (Alice=0, Bob=1, Carol=2, David=3 in userIds)
        var s1 = sprints[0].Id;
        var s2 = sprints[1].Id;
        var tasks = new[]
        {
            new TaskItem { Title = "Define AI prompt templates for task creation", Description = "Create structured prompts for parsing natural language into task fields", Status = "in-progress", Priority = "high", AssigneeId = userIds[0], CreatedBy = userIds[0], CreatedAt = Sprint1Start, DueDate = Sprint1End, TagsJson = JsonSerializer.Serialize(new[] { "ai", "prompts" }), ProjectId = project.Id, SprintId = s1, Size = 6 },
            new TaskItem { Title = "Integrate LLM API for task suggestions", Description = "Wire up OpenAI/Claude API for generating task recommendations", Status = "todo", Priority = "high", AssigneeId = userIds[0], CreatedBy = userIds[0], CreatedAt = Sprint1Start, DueDate = Sprint1End, TagsJson = JsonSerializer.Serialize(new[] { "ai", "backend" }), ProjectId = project.Id, SprintId = s1, Size = 8 },
            new TaskItem { Title = "Build smart priority suggestion model", Description = "Train or configure model to suggest task priority from description", Status = "todo", Priority = "medium", AssigneeId = userIds[0], CreatedBy = userIds[0], CreatedAt = Sprint2Start, DueDate = Sprint2End, TagsJson = JsonSerializer.Serialize(new[] { "ai", "ml" }), ProjectId = project.Id, SprintId = s2, Size = 12 },
            new TaskItem { Title = "Design AI feedback UI components", Description = "Mockups for showing AI suggestions and confidence scores", Status = "todo", Priority = "medium", AssigneeId = userIds[0], CreatedBy = userIds[1], CreatedAt = Sprint2Start, DueDate = Sprint2End, TagsJson = JsonSerializer.Serialize(new[] { "design", "frontend" }), ProjectId = project.Id, SprintId = s2, Size = 4 },
            new TaskItem { Title = "Implement natural language task parser", Description = "Parse user input like \"Fix login bug by Friday\" into structured task", Status = "in-progress", Priority = "high", AssigneeId = userIds[1], CreatedBy = userIds[0], CreatedAt = Sprint1Start, DueDate = Sprint1End, TagsJson = JsonSerializer.Serialize(new[] { "ai", "backend" }), ProjectId = project.Id, SprintId = s1, Size = 10 },
            new TaskItem { Title = "Add rate limiting for AI endpoints", Description = "Protect AI API from abuse with token-based rate limits", Status = "completed", Priority = "medium", AssigneeId = userIds[1], CreatedBy = userIds[1], CreatedAt = Sprint1Start, DueDate = Sprint1End, TagsJson = JsonSerializer.Serialize(new[] { "backend", "security" }), ProjectId = project.Id, SprintId = s1, Size = 3 },
            new TaskItem { Title = "Create task similarity service", Description = "Find related tasks when user creates new one using embeddings", Status = "todo", Priority = "medium", AssigneeId = userIds[1], CreatedBy = userIds[1], CreatedAt = Sprint2Start, DueDate = Sprint2End, TagsJson = JsonSerializer.Serialize(new[] { "ai", "backend" }), ProjectId = project.Id, SprintId = s2, Size = 8 },
            new TaskItem { Title = "Implement suggestion acceptance flow", Description = "API and logic for applying AI suggestions to tasks", Status = "todo", Priority = "high", AssigneeId = userIds[1], CreatedBy = userIds[0], CreatedAt = Sprint2Start, DueDate = Sprint2End, TagsJson = JsonSerializer.Serialize(new[] { "backend", "api" }), ProjectId = project.Id, SprintId = s2, Size = 5 },
            new TaskItem { Title = "Design AI assistant chat interface", Description = "UI for conversational task creation and refinement", Status = "in-progress", Priority = "high", AssigneeId = userIds[2], CreatedBy = userIds[1], CreatedAt = Sprint1Start, DueDate = Sprint1End, TagsJson = JsonSerializer.Serialize(new[] { "design", "ux" }), ProjectId = project.Id, SprintId = s1, Size = 6 },
            new TaskItem { Title = "Create loading and streaming states for AI", Description = "Skeleton and streaming text components for AI responses", Status = "todo", Priority = "medium", AssigneeId = userIds[2], CreatedBy = userIds[2], CreatedAt = Sprint1Start, DueDate = Sprint1End, TagsJson = JsonSerializer.Serialize(new[] { "frontend", "ux" }), ProjectId = project.Id, SprintId = s1, Size = 4 },
            new TaskItem { Title = "Implement inline AI suggestions in task form", Description = "Show AI-suggested title, description, assignee as user types", Status = "todo", Priority = "high", AssigneeId = userIds[2], CreatedBy = userIds[0], CreatedAt = Sprint2Start, DueDate = Sprint2End, TagsJson = JsonSerializer.Serialize(new[] { "frontend", "ai" }), ProjectId = project.Id, SprintId = s2, Size = 8 },
            new TaskItem { Title = "Add analytics for AI feature usage", Description = "Track which AI suggestions are accepted vs dismissed", Status = "todo", Priority = "low", AssigneeId = userIds[2], CreatedBy = userIds[1], CreatedAt = Sprint2Start, DueDate = Sprint2End, TagsJson = JsonSerializer.Serialize(new[] { "analytics", "frontend" }), ProjectId = project.Id, SprintId = s2, Size = 4 },
            new TaskItem { Title = "Set up vector store for task embeddings", Description = "Configure Pinecone/Supabase for semantic task search", Status = "completed", Priority = "high", AssigneeId = userIds[3], CreatedBy = userIds[1], CreatedAt = Sprint1Start, DueDate = Sprint1End, TagsJson = JsonSerializer.Serialize(new[] { "ai", "infra" }), ProjectId = project.Id, SprintId = s1, Size = 6 },
            new TaskItem { Title = "Write AI integration tests", Description = "Unit and integration tests for task parsing and suggestions", Status = "in-progress", Priority = "medium", AssigneeId = userIds[3], CreatedBy = userIds[3], CreatedAt = Sprint1Start, DueDate = Sprint1End, TagsJson = JsonSerializer.Serialize(new[] { "testing", "qa" }), ProjectId = project.Id, SprintId = s1, Size = 5 },
            new TaskItem { Title = "Document AI API and prompt guidelines", Description = "Developer docs for extending and tuning AI features", Status = "todo", Priority = "medium", AssigneeId = userIds[3], CreatedBy = userIds[0], CreatedAt = Sprint2Start, DueDate = Sprint2End, TagsJson = JsonSerializer.Serialize(new[] { "documentation" }), ProjectId = project.Id, SprintId = s2, Size = 4 },
            new TaskItem { Title = "Implement fallback when AI unavailable", Description = "Graceful degradation when API is down or rate limited", Status = "todo", Priority = "medium", AssigneeId = userIds[3], CreatedBy = userIds[1], CreatedAt = Sprint2Start, DueDate = Sprint2End, TagsJson = JsonSerializer.Serialize(new[] { "backend", "resilience" }), ProjectId = project.Id, SprintId = s2, Size = 3 }
        };
        db.TaskItems.AddRange(tasks);
        await db.SaveChangesAsync(ct);

        // Project-level comments
        var projectComments = new[]
        {
            new CommentEntity { Content = "Excited to kick off the AI Support project! Let's make task creation smarter.", AuthorId = userIds[0], ProjectId = project.Id, TaskId = null, CreatedAt = ProjectStart.AddHours(-2) },
            new CommentEntity { Content = "Reminder: we're using 1-week sprints. Plan your capacity accordingly.", AuthorId = userIds[1], ProjectId = project.Id, TaskId = null, CreatedAt = ProjectStart.AddHours(4) },
            new CommentEntity { Content = "I'll set up the design system for AI-related components in Sprint 1.", AuthorId = userIds[2], ProjectId = project.Id, TaskId = null, CreatedAt = ProjectStart.AddDays(1) },
            new CommentEntity { Content = "Vector store is ready. Bob - you can start on the similarity service when Sprint 2 kicks off.", AuthorId = userIds[3], ProjectId = project.Id, TaskId = null, CreatedAt = ProjectStart.AddDays(2) },
            new CommentEntity { Content = "Great progress everyone! Sprint 1 is looking solid.", AuthorId = userIds[0], ProjectId = project.Id, TaskId = null, CreatedAt = ProjectStart.AddDays(3) }
        };
        db.Comments.AddRange(projectComments);

        // Task-level comments (tasks 0,1,4,5,8,10,12,13)
        var taskComments = new[]
        {
            new CommentEntity { Content = "Using a few-shot approach for the prompts - will share examples in the doc.", AuthorId = userIds[0], ProjectId = null, TaskId = tasks[0].Id, CreatedAt = Sprint1Start.AddHours(5) },
            new CommentEntity { Content = "API key is in env. Use the gpt-4 model for best results.", AuthorId = userIds[1], ProjectId = null, TaskId = tasks[1].Id, CreatedAt = Sprint1Start.AddHours(8) },
            new CommentEntity { Content = "Parser is working for simple cases. Edge cases (dates, assignees) next.", AuthorId = userIds[1], ProjectId = null, TaskId = tasks[4].Id, CreatedAt = Sprint1Start.AddDays(1) },
            new CommentEntity { Content = "Rate limit: 100 req/min per user. Can adjust if needed.", AuthorId = userIds[1], ProjectId = null, TaskId = tasks[5].Id, CreatedAt = Sprint1Start.AddDays(1).AddHours(2) },
            new CommentEntity { Content = "Chat UI mockups in Figma - link in task description.", AuthorId = userIds[2], ProjectId = null, TaskId = tasks[8].Id, CreatedAt = Sprint1Start.AddDays(2) },
            new CommentEntity { Content = "We could show suggestions in a dropdown below the title field.", AuthorId = userIds[2], ProjectId = null, TaskId = tasks[10].Id, CreatedAt = Sprint2Start.AddHours(1) },
            new CommentEntity { Content = "Pinecone index is live. 1536 dimensions to match OpenAI embeddings.", AuthorId = userIds[3], ProjectId = null, TaskId = tasks[12].Id, CreatedAt = Sprint1Start.AddDays(1).AddHours(6) },
            new CommentEntity { Content = "Adding mocks for AI responses in tests. Should be done by EOD.", AuthorId = userIds[3], ProjectId = null, TaskId = tasks[13].Id, CreatedAt = Sprint1Start.AddDays(2).AddHours(3) }
        };
        db.Comments.AddRange(taskComments);

        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Hash a password using PBKDF2 (same algorithm as PasswordHasher service).
    /// </summary>
    private static string HashPassword(string password)
    {
        const int saltSize = 16;
        const int hashSize = 32;
        const int iterations = 100_000;

        byte[] salt = RandomNumberGenerator.GetBytes(saltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            hashSize
        );

        return $"{iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }
}
