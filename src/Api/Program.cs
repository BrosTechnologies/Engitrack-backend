using Microsoft.EntityFrameworkCore;
using Engitrack.Projects.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ProjectsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")));

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Engitrack API", Version = "v1" });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Engitrack API v1");
        c.RoutePrefix = string.Empty; // Swagger en la raíz
    });
}

app.UseCors();
app.UseHttpsRedirection();

// Basic endpoints
app.MapGet("/api/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }))
   .WithName("HealthCheck")
   .WithTags("System");

// Projects endpoints
app.MapGet("/api/projects", async (ProjectsDbContext context) =>
{
    var projects = await context.Projects.ToListAsync();
    return Results.Ok(projects);
})
.WithName("GetProjects")
.WithTags("Projects");

app.MapPost("/api/projects", async (CreateProjectRequest request, ProjectsDbContext context) =>
{
    // Simple validation
    if (string.IsNullOrWhiteSpace(request.Name))
        return Results.BadRequest("Name is required");

    var project = new Engitrack.Projects.Domain.Entities.Project(
        request.Name,
        DateOnly.FromDateTime(request.StartDate),
        request.OwnerUserId,
        request.Budget);

    context.Projects.Add(project);
    await context.SaveChangesAsync();

    return Results.Created($"/api/projects/{project.Id}", project);
})
.WithName("CreateProject")
.WithTags("Projects");

app.Run();

// DTOs
public record CreateProjectRequest(string Name, DateTime StartDate, Guid OwnerUserId, decimal? Budget);
