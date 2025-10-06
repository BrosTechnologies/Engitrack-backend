using Microsoft.EntityFrameworkCore;
using Engitrack.Projects.Domain.Entities;
using Engitrack.Projects.Domain.Repositories;
using Engitrack.Projects.Domain.Enums;
using Engitrack.Projects.Infrastructure.Persistence;

namespace Engitrack.Projects.Infrastructure.Repositories;

public class ProjectRepository : IProjectRepository
{
    private readonly ProjectsDbContext _context;

    public ProjectRepository(ProjectsDbContext context)
    {
        _context = context;
    }

    public async Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Projects
            .Include(p => p.Tasks)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Project>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Projects
            .Include(p => p.Tasks)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Project>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        return await _context.Projects
            .Where(p => p.OwnerUserId == ownerId)
            .Include(p => p.Tasks)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Project>> GetByStatusAsync(ProjectStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Projects
            .Where(p => p.Status == status)
            .Include(p => p.Tasks)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Project>> GetByOwnerAndStatusAsync(Guid ownerId, ProjectStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Projects
            .Where(p => p.OwnerUserId == ownerId && p.Status == status)
            .Include(p => p.Tasks)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Project project, CancellationToken cancellationToken = default)
    {
        await _context.Projects.AddAsync(project, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Project project, CancellationToken cancellationToken = default)
    {
        _context.Projects.Update(project);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Project project, CancellationToken cancellationToken = default)
    {
        _context.Projects.Remove(project);
        await _context.SaveChangesAsync(cancellationToken);
    }
}