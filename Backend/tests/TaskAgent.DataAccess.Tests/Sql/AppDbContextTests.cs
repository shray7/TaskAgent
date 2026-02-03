using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TaskAgent.DataAccess.Entities;
using TaskAgent.DataAccess.Sql;
using Xunit;

namespace TaskAgent.DataAccess.Tests.Sql;

public class AppDbContextTests
{
    private static AppDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Projects_SoftDelete_ExcludedFromQuery()
    {
        var dbName = nameof(Projects_SoftDelete_ExcludedFromQuery) + Guid.NewGuid();
        await using (var db = CreateContext(dbName))
        {
            var owner = new AppUser { Name = "Owner", Email = "o@test.com" };
            db.AppUsers.Add(owner);
            await db.SaveChangesAsync();

            var p1 = new ProjectEntity { Name = "P1", Description = "", Color = "#fff", OwnerId = owner.Id };
            var p2 = new ProjectEntity { Name = "P2", Description = "", Color = "#fff", OwnerId = owner.Id, DeletedAt = DateTime.UtcNow };
            db.Projects.AddRange(p1, p2);
            await db.SaveChangesAsync();
        }

        await using (var db = CreateContext(dbName))
        {
            var active = await db.Projects.Where(p => p.DeletedAt == null).ToListAsync();
            active.Should().HaveCount(1);
            active[0].Name.Should().Be("P1");
        }
    }

    [Fact]
    public async Task TaskItems_FilterByProjectIdAndDeletedAt()
    {
        var dbName = nameof(TaskItems_FilterByProjectIdAndDeletedAt) + Guid.NewGuid();
        await using (var db = CreateContext(dbName))
        {
            var owner = new AppUser { Name = "O", Email = "o@t.com" };
            db.AppUsers.Add(owner);
            await db.SaveChangesAsync();
            var project = new ProjectEntity { Name = "P", Description = "", Color = "#fff", OwnerId = owner.Id };
            db.Projects.Add(project);
            await db.SaveChangesAsync();

            var t1 = new TaskItem { Title = "T1", Description = "", ProjectId = project.Id, AssigneeId = owner.Id, CreatedBy = owner.Id };
            var t2 = new TaskItem { Title = "T2", Description = "", ProjectId = project.Id, AssigneeId = owner.Id, CreatedBy = owner.Id, DeletedAt = DateTime.UtcNow };
            db.TaskItems.AddRange(t1, t2);
            await db.SaveChangesAsync();
        }

        await using (var db = CreateContext(dbName))
        {
            var active = await db.TaskItems
                .Where(t => t.ProjectId == 1 && t.DeletedAt == null)
                .OrderBy(t => t.CreatedAt)
                .ToListAsync();
            active.Should().HaveCount(1);
            active[0].Title.Should().Be("T1");
        }
    }

    [Fact]
    public async Task TaskItems_IncludeProject_Succeeds()
    {
        var dbName = nameof(TaskItems_IncludeProject_Succeeds) + Guid.NewGuid();
        await using (var db = CreateContext(dbName))
        {
            var owner = new AppUser { Name = "O", Email = "o@t.com" };
            db.AppUsers.Add(owner);
            await db.SaveChangesAsync();
            var project = new ProjectEntity { Name = "MyProject", Description = "", Color = "#fff", OwnerId = owner.Id };
            db.Projects.Add(project);
            await db.SaveChangesAsync();
            var task = new TaskItem { Title = "T", Description = "", ProjectId = project.Id, AssigneeId = owner.Id, CreatedBy = owner.Id };
            db.TaskItems.Add(task);
            await db.SaveChangesAsync();
        }

        await using (var db = CreateContext(dbName))
        {
            var task = await db.TaskItems.Include(t => t.Project).FirstOrDefaultAsync(t => t.Id == 1);
            task.Should().NotBeNull();
            task!.Project.Should().NotBeNull();
            task.Project.Name.Should().Be("MyProject");
        }
    }

    [Fact]
    public async Task Sprints_FilterByProjectId()
    {
        var dbName = nameof(Sprints_FilterByProjectId) + Guid.NewGuid();
        await using (var db = CreateContext(dbName))
        {
            var owner = new AppUser { Name = "O", Email = "o@t.com" };
            db.AppUsers.Add(owner);
            await db.SaveChangesAsync();
            var project = new ProjectEntity { Name = "P", Description = "", Color = "#fff", OwnerId = owner.Id };
            db.Projects.Add(project);
            await db.SaveChangesAsync();
            var start = DateTime.UtcNow;
            var end = start.AddDays(14);
            var sprint = new SprintEntity { ProjectId = project.Id, Name = "S1", Goal = "", StartDate = start, EndDate = end };
            db.Sprints.Add(sprint);
            await db.SaveChangesAsync();
        }

        await using (var db = CreateContext(dbName))
        {
            var byProject = await db.Sprints.Where(s => s.ProjectId == 1).ToListAsync();
            byProject.Should().HaveCount(1);
            byProject[0].Name.Should().Be("S1");
        }
    }

    [Fact]
    public async Task Comments_QueryByTaskId()
    {
        var dbName = nameof(Comments_QueryByTaskId) + Guid.NewGuid();
        await using (var db = CreateContext(dbName))
        {
            var author = new AppUser { Name = "A", Email = "a@t.com" };
            db.AppUsers.Add(author);
            await db.SaveChangesAsync();
            var owner = new AppUser { Name = "O", Email = "o@t.com" };
            db.AppUsers.Add(owner);
            await db.SaveChangesAsync();
            var project = new ProjectEntity { Name = "P", Description = "", Color = "#fff", OwnerId = owner.Id };
            db.Projects.Add(project);
            await db.SaveChangesAsync();
            var task = new TaskItem { Title = "T", Description = "", ProjectId = project.Id, AssigneeId = owner.Id, CreatedBy = owner.Id };
            db.TaskItems.Add(task);
            await db.SaveChangesAsync();
            var comment = new CommentEntity { Content = "C1", AuthorId = author.Id, TaskId = task.Id };
            db.Comments.Add(comment);
            await db.SaveChangesAsync();
        }

        await using (var db = CreateContext(dbName))
        {
            var comments = await db.Comments.Where(c => c.TaskId == 1).ToListAsync();
            comments.Should().HaveCount(1);
            comments[0].Content.Should().Be("C1");
        }
    }

    [Fact]
    public void AppUser_CanBeCreatedAndPersisted()
    {
        var dbName = nameof(AppUser_CanBeCreatedAndPersisted) + Guid.NewGuid();
        using (var db = CreateContext(dbName))
        {
            db.AppUsers.Add(new AppUser { Name = "Test", Email = "test@example.com" });
            db.SaveChanges();
        }
        using (var db = CreateContext(dbName))
        {
            var user = db.AppUsers.FirstOrDefault(u => u.Email == "test@example.com");
            user.Should().NotBeNull();
            user!.Name.Should().Be("Test");
        }
    }
}
