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
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<Company> Companies => Set<Company>();

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

        modelBuilder.Entity<Job>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Title).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Company).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Location).HasMaxLength(128);
            entity.Property(e => e.Address).HasMaxLength(512);
            
            // Indexes for common queries
            entity.HasIndex(e => e.Company);
            entity.HasIndex(e => e.Location);
            entity.HasIndex(e => e.ScheduleDate);
        });

        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Industry).HasMaxLength(100);
            entity.Property(e => e.ContactEmail).HasMaxLength(256);
            entity.Property(e => e.ContactPhone).HasMaxLength(50);
            entity.Property(e => e.Website).HasMaxLength(512);
            entity.Property(e => e.City).HasMaxLength(128);
            entity.Property(e => e.State).HasMaxLength(50);
            entity.Property(e => e.LicenseNumber).HasMaxLength(100);
            entity.Property(e => e.Rating).HasPrecision(3, 2);

            // Indexes for common queries
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.IsAllowed);
            entity.HasIndex(e => e.Industry);
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
