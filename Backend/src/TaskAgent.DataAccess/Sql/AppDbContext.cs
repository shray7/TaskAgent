using Microsoft.EntityFrameworkCore;
using TaskAgent.DataAccess.Entities;

namespace TaskAgent.DataAccess.Sql;

/// <summary>
/// Entity Framework Core DbContext for SQL database operations.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    // Task Management (Vue app)
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<ProjectEntity> Projects => Set<ProjectEntity>();
    public DbSet<SprintEntity> Sprints => Set<SprintEntity>();
    public DbSet<TaskItem> TaskItems => Set<TaskItem>();
    public DbSet<CommentEntity> Comments => Set<CommentEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.Property(e => e.DisplayName).HasMaxLength(128).IsRequired();
            entity.Property(e => e.PasswordHash).IsRequired();
        });

        // Task Management entities
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(128).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Avatar).HasMaxLength(50);
            entity.Property(e => e.PasswordHash).HasMaxLength(500);
        });

        modelBuilder.Entity<ProjectEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Color).HasMaxLength(20);
            entity.Property(e => e.TaskSizeUnit).HasMaxLength(10);
            entity.HasIndex(e => e.OwnerId);
            entity.HasOne(e => e.Owner).WithMany().HasForeignKey(e => e.OwnerId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SprintEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(128).IsRequired();
            entity.Property(e => e.Goal).HasMaxLength(500);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.HasIndex(e => e.ProjectId);
            entity.HasOne(e => e.Project).WithMany().HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.Priority).HasMaxLength(20);
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.SprintId);
            entity.HasIndex(e => e.AssigneeId);
            entity.HasOne(e => e.Project).WithMany().HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Sprint).WithMany().HasForeignKey(e => e.SprintId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CommentEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).HasMaxLength(2000).IsRequired();
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.TaskId);
            entity.HasOne(e => e.Author).WithMany().HasForeignKey(e => e.AuthorId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
