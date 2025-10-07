using Microsoft.EntityFrameworkCore;
using Engitrack.Projects.Domain.Entities;
using Engitrack.Projects.Domain.Enums;
using Engitrack.Workers.Domain.Entities;
using Engitrack.Workers.Domain.Enums;
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
    
    // Workers entities
    public DbSet<Worker> Workers => Set<Worker>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Attendance> Attendances => Set<Attendance>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("projects");

        // User Configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWSEQUENTIALID()").ValueGeneratedOnAdd();
            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.Property(e => e.FullName).HasMaxLength(120).IsRequired();
            entity.Property(e => e.Phone).HasMaxLength(32).IsRequired();
            entity.Property(e => e.Role).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(100).IsRequired();
            entity.Property<byte[]>("RowVersion").IsRowVersion();
            
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Project Configuration
        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWSEQUENTIALID()").ValueGeneratedOnAdd();
            entity.Property(e => e.Name).HasMaxLength(160).IsRequired();
            entity.Property(e => e.StartDate).IsRequired();
            entity.Property(e => e.EndDate);
            entity.Property(e => e.Budget).HasPrecision(14, 2);
            entity.Property(e => e.Status).HasConversion<string>().IsRequired();
            entity.Property(e => e.OwnerUserId).IsRequired();
            entity.Property<byte[]>("RowVersion").IsRowVersion();

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

        // Worker Configuration
        modelBuilder.Entity<Worker>(entity =>
        {
            entity.ToTable("Workers", "workers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWSEQUENTIALID()").ValueGeneratedOnAdd();
            entity.Property(e => e.FullName).HasMaxLength(120).IsRequired();
            entity.Property(e => e.DocumentNumber).HasMaxLength(32).IsRequired();
            entity.Property(e => e.Phone).HasMaxLength(32);
            entity.Property(e => e.Position).HasMaxLength(80);
            entity.Property(e => e.HourlyRate).HasPrecision(10, 2);
            entity.Property<byte[]>("RowVersion").IsRowVersion();

            entity.HasIndex(e => e.DocumentNumber).IsUnique();
            entity.Ignore(e => e.DomainEvents);
        });

        // Assignment Configuration
        modelBuilder.Entity<Assignment>(entity =>
        {
            entity.ToTable("Assignments", "workers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWSEQUENTIALID()").ValueGeneratedOnAdd();
            entity.Property(e => e.WorkerId).IsRequired();
            entity.Property(e => e.ProjectId).IsRequired();
            entity.Property(e => e.StartDate).IsRequired();
            entity.Property(e => e.EndDate);

            entity.HasIndex(e => e.WorkerId);
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => new { e.WorkerId, e.ProjectId });
            entity.Ignore(e => e.DomainEvents);
        });

        // Attendance Configuration
        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.ToTable("Attendances", "workers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWSEQUENTIALID()").ValueGeneratedOnAdd();
            entity.Property(e => e.WorkerId).IsRequired();
            entity.Property(e => e.ProjectId).IsRequired();
            entity.Property(e => e.Day).IsRequired();
            entity.Property(e => e.CheckIn);
            entity.Property(e => e.CheckOut);
            entity.Property(e => e.Status).HasConversion<string>().IsRequired();
            entity.Property(e => e.Notes).HasMaxLength(400);

            entity.HasIndex(e => e.WorkerId);
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => new { e.WorkerId, e.Day }).IsUnique();
            entity.Ignore(e => e.DomainEvents);
        });

        base.OnModelCreating(modelBuilder);
    }
}