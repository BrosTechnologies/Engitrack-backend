using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Engitrack.Projects.Infrastructure.Persistence;
using Engitrack.Projects.Application.Projects.Dtos;
using Engitrack.Projects.Application.Projects.Validators;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ProjectsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")));

// Add FluentValidation
builder.Services.AddScoped<IValidator<CreateProjectRequest>, CreateProjectRequestValidator>();

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
    var projects = await context.Projects
        .Include(p => p.Tasks)
        .ToListAsync();
    
    var response = projects.Select(p => new ProjectResponse(
        p.Id,
        p.Name,
        p.StartDate,
        p.EndDate,
        p.Budget,
        p.Status.ToString(),
        p.OwnerUserId,
        p.Tasks.Select(t => new ProjectTaskDto(t.Id, t.Title, t.Status.ToString(), t.DueDate))
    ));
    
    return Results.Ok(response);
})
.WithName("GetProjects")
.WithTags("Projects")
.Produces<IEnumerable<ProjectResponse>>(200);

app.MapPost("/api/projects", async (CreateProjectRequest request, IValidator<CreateProjectRequest> validator, ProjectsDbContext context) =>
{
    // Validate request
    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
    {
        return Results.BadRequest(validationResult.Errors.Select(e => new { 
            Property = e.PropertyName, 
            Error = e.ErrorMessage 
        }));
    }

    // Create project
    var project = new Engitrack.Projects.Domain.Entities.Project(
        request.Name,
        request.StartDate,
        request.OwnerUserId,
        request.Budget);

    // Set EndDate if provided and valid
    if (request.EndDate.HasValue)
    {
        project.SetEndDate(request.EndDate);
    }

    // Add tasks if provided
    if (request.Tasks != null)
    {
        foreach (var taskDto in request.Tasks)
        {
            project.AddTask(taskDto.Title, taskDto.DueDate);
        }
    }

    context.Projects.Add(project);
    await context.SaveChangesAsync();

    // Map to response
    var response = new ProjectResponse(
        project.Id,
        project.Name,
        project.StartDate,
        project.EndDate,
        project.Budget,
        project.Status.ToString(),
        project.OwnerUserId,
        project.Tasks.Select(t => new ProjectTaskDto(t.Id, t.Title, t.Status.ToString(), t.DueDate))
    );

    return Results.Created($"/api/projects/{project.Id}", response);
})
.WithName("CreateProject")
.WithTags("Projects")
.Accepts<CreateProjectRequest>("application/json")
.Produces<ProjectResponse>(201)
.Produces(400);

app.Run();
