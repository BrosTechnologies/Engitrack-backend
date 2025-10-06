using Microsoft.EntityFrameworkCore;
using Engitrack.Projects.Domain.Entities;
using Engitrack.Projects.Domain.Enums;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Engitrack.Projects.Infrastructure.Persistence;

public class ProjectsDbContext : DbContext
{
    public ProjectsDbContext(DbContextOptions<ProjectsDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectTask> ProjectTasks => Set<ProjectTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("projects");

        // User Configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.Property(e => e.FullName).HasMaxLength(120).IsRequired();
            entity.Property(e => e.Phone).HasMaxLength(32);
            entity.Property(e => e.Role).HasConversion<string>().IsRequired();
            
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Project Configuration
        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(160).IsRequired();
            entity.Property(e => e.StartDate).IsRequired();
            entity.Property(e => e.EndDate);
            entity.Property(e => e.Budget).HasPrecision(14, 2);
            entity.Property(e => e.Status).HasConversion<string>().IsRequired();
            entity.Property(e => e.OwnerUserId).IsRequired();

            entity.HasIndex(e => e.OwnerUserId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.OwnerUserId, e.Status });

            // Relationship with ProjectTasks
            entity.HasMany(p => p.Tasks)
                  .WithOne()
                  .HasForeignKey(t => t.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Ignore domain events
            entity.Ignore(e => e.DomainEvents);
        });

        // ProjectTask Configuration
        modelBuilder.Entity<ProjectTask>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProjectId).IsRequired();
            entity.Property(e => e.Title).HasMaxLength(160).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().IsRequired();
            entity.Property(e => e.DueDate);

            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.ProjectId, e.Status });

            // Ignore domain events
            entity.Ignore(e => e.DomainEvents);
        });

        base.OnModelCreating(modelBuilder);
    }
}