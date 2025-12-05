using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Engitrack.Projects.Infrastructure.Persistence;
using Engitrack.Inventory.Infrastructure.Persistence;
using Engitrack.Projects.Application.Projects.Dtos;
using Engitrack.Projects.Application.Projects.Validators;
using Engitrack.Inventory.Application.Dtos;
using Engitrack.Inventory.Application.Validators;
using Engitrack.Workers.Application.Dtos;
using Engitrack.Workers.Application.Validators;
using Engitrack.Api.Security;
using Engitrack.Api.Auth;
using Engitrack.Api.Contracts.Projects;
using Engitrack.Api.Validators;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Security.Claims;
using Engitrack.Projects.Domain.Entities;
using Engitrack.Projects.Domain.Enums;
using Engitrack.Workers.Domain.Entities;
using Engitrack.Inventory.Domain.Materials;
using Engitrack.Inventory.Domain.Suppliers;
using InventoryTx = Engitrack.Inventory.Domain.Transactions.InventoryTransaction;
using TxType = Engitrack.Inventory.Domain.Transactions.TxType;
using BCrypt.Net;
using Microsoft.Data.SqlClient;
using Engitrack.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<JwtHelper>();
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddDbContext<ProjectsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")));

builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")));

// Add Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ValidateIssuer = false, // Disabled in dev
            ValidateAudience = false, // Disabled in dev
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Add FluentValidation
builder.Services.AddScoped<IValidator<CreateProjectRequest>, CreateProjectRequestValidator>();
builder.Services.AddScoped<IValidator<CreateTaskRequest>, CreateTaskRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateTaskStatusRequest>, UpdateTaskStatusRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateProjectRequest>, UpdateProjectRequestValidator>();
builder.Services.AddScoped<IValidator<UpdatePriorityRequest>, UpdatePriorityRequestValidator>();
builder.Services.AddScoped<IValidator<UpdatePriorityStringRequest>, UpdatePriorityStringRequestValidator>();
builder.Services.AddScoped<IValidator<RegisterTransactionRequest>, RegisterTransactionRequestValidator>();
builder.Services.AddScoped<IValidator<CreateWorkerRequest>, CreateWorkerRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateWorkerRequest>, UpdateWorkerRequestValidator>();
builder.Services.AddScoped<IValidator<CreateAssignmentRequest>, CreateAssignmentRequestValidator>();
builder.Services.AddScoped<IValidator<AssignWorkerRequest>, AssignWorkerRequestValidator>();
builder.Services.AddScoped<IValidator<CreateAttendanceRequest>, CreateAttendanceRequestValidator>();
builder.Services.AddScoped<IValidator<CreateIncidentRequest>, CreateIncidentValidator>();
builder.Services.AddScoped<IValidator<UpdateIncidentRequest>, UpdateIncidentValidator>();
builder.Services.AddScoped<IValidator<CreateMachineRequest>, CreateMachineValidator>();
builder.Services.AddScoped<IValidator<UpdateMachineRequest>, UpdateMachineValidator>();
builder.Services.AddScoped<IValidator<CreateMaterialRequest>, CreateMaterialRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateMaterialRequest>, UpdateMaterialRequestValidator>();
builder.Services.AddScoped<IValidator<RegisterTransactionRequest>, RegisterTransactionRequestValidator>();
builder.Services.AddScoped<IValidator<CreateSupplierRequest>, CreateSupplierRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateSupplierRequest>, UpdateSupplierRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateUserProfileRequest>, UpdateUserProfileRequestValidator>();

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
    
    // Add Bearer authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' followed by a space and the JWT token"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
// Enable Swagger in both Development and Production for API testing
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Engitrack API v1");
    c.RoutePrefix = string.Empty; // Swagger en la raíz
});

app.UseCors();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Basic endpoints
app.MapGet("/", () => Results.Ok(new { 
    Message = "Engitrack API is running!", 
    Version = "v1",
    Timestamp = DateTime.UtcNow,
    SwaggerUI = "/swagger",
    HealthCheck = "/api/health"
}))
   .WithName("Root")
   .WithTags("System");

app.MapGet("/api/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }))
   .WithName("HealthCheck")
   .WithTags("System");

// Debug endpoint to check configuration
app.MapGet("/api/debug/config", (IConfiguration config) => Results.Ok(new {
    Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
    HasConnectionString = !string.IsNullOrEmpty(config.GetConnectionString("SqlServer")),
    ConnectionStringLength = config.GetConnectionString("SqlServer")?.Length ?? 0,
    HasJwtKey = !string.IsNullOrEmpty(config["Jwt:Key"]),
    JwtKeyLength = config["Jwt:Key"]?.Length ?? 0,
    JwtIssuer = config["Jwt:Issuer"],
    JwtAudience = config["Jwt:Audience"]
}))
   .WithName("DebugConfig")
   .WithTags("System");

// Debug endpoint to check database connectivity
app.MapGet("/api/debug/database", async (ProjectsDbContext context) => {
    try
    {
        // Test basic connectivity
        var canConnect = await context.Database.CanConnectAsync();
        
        // Check if Users table exists
        var usersTableExists = false;
        var userCount = 0;
        
        try
        {
            userCount = await context.Users.CountAsync();
            usersTableExists = true;
        }
        catch (Exception ex)
        {
            // Table doesn't exist or other error
        }
        
        return Results.Ok(new {
            CanConnect = canConnect,
            UsersTableExists = usersTableExists,
            UserCount = userCount,
            DatabaseName = context.Database.GetDbConnection().Database
        });
    }
    catch (Exception ex)
    {
        return Results.Ok(new {
            Error = ex.Message,
            CanConnect = false,
            UsersTableExists = false
        });
    }
})
   .WithName("DebugDatabase")
   .WithTags("System");

// Projects endpoints (authenticated)
app.MapGet("/api/projects", async (ProjectsDbContext context, ICurrentUser currentUser, string? status = null, string? q = null, int page = 1, int pageSize = 10) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    var query = context.Projects
        .AsNoTracking()
        .Include(p => p.Tasks)
        .AsQueryable();

    // SUPERVISOR and CONTRACTOR can see all projects, others only see their own projects
    if (currentUser.Role != "SUPERVISOR" && currentUser.Role != "CONTRACTOR")
    {
        query = query.Where(p => p.OwnerUserId == currentUser.Id);
    }

    // Apply filters
    if (!string.IsNullOrEmpty(status))
        query = query.Where(p => p.Status.ToString() == status);

    if (!string.IsNullOrEmpty(q))
        query = query.Where(p => p.Name.Contains(q));

    // Pagination
    var projects = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    var response = projects.Select(p => new ProjectResponse(
        p.Id,
        p.Name,
        p.StartDate,
        p.EndDate,
        p.Budget,
        p.Status.ToString(),
        p.Priority.ToString(),
        p.Description,
        p.OwnerUserId,
        p.Tasks.Select(t => new ProjectTaskDto(t.Id, p.Id, t.Title, t.Status.ToString(), t.DueDate))
    ));

    return Results.Ok(response);
})
.RequireAuthorization()
.WithName("GetProjects")
.WithTags("Projects")
.Produces<IEnumerable<ProjectResponse>>(200);

app.MapGet("/api/projects/{id:guid}", async (Guid id, ProjectsDbContext context, ICurrentUser currentUser) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    var project = await context.Projects
        .AsNoTracking()
        .Include(p => p.Tasks)
        .FirstOrDefaultAsync(p => p.Id == id);

    if (project == null)
        return Results.NotFound();

    // SUPERVISOR and CONTRACTOR can access any project, others only their own projects
    if (currentUser.Role != "SUPERVISOR" && currentUser.Role != "CONTRACTOR" && project.OwnerUserId != currentUser.Id)
        return Results.Forbid();

    var response = new ProjectResponse(
        project.Id,
        project.Name,
        project.StartDate,
        project.EndDate,
        project.Budget,
        project.Status.ToString(),
        project.Priority.ToString(),
        project.Description,
        project.OwnerUserId,
        project.Tasks.Select(t => new ProjectTaskDto(t.Id, project.Id, t.Title, t.Status.ToString(), t.DueDate))
    );

    return Results.Ok(response);
})
.RequireAuthorization()
.WithName("GetProject")
.WithTags("Projects")
.Produces<ProjectResponse>(200)
.Produces(404)
.Produces(403);

app.MapPost("/api/projects", async (CreateProjectRequest request, IValidator<CreateProjectRequest> validator, ProjectsDbContext context, ICurrentUser currentUser) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    // Validate request
    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
    {
        return Results.BadRequest(validationResult.Errors.Select(e => new { 
            Property = e.PropertyName, 
            Error = e.ErrorMessage 
        }));
    }

    // Use owner from JWT, ignore request.OwnerUserId
    var owner = currentUser.Id!.Value;

    // Create project
    var project = new Project(request.Name, request.StartDate, owner, request.Description, request.Budget, request.Priority ?? Priority.MEDIUM);

    // Set EndDate if provided and valid
    if (request.EndDate.HasValue && request.EndDate >= request.StartDate)
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
        project.Priority.ToString(),
        project.Description,
        project.OwnerUserId,
        project.Tasks.Select(t => new ProjectTaskDto(t.Id, project.Id, t.Title, t.Status.ToString(), t.DueDate))
    );

    return Results.Created($"/api/projects/{project.Id}", response);
})
.RequireAuthorization()
.WithName("CreateProject")
.WithTags("Projects")
.Accepts<CreateProjectRequest>("application/json")
.Produces<ProjectResponse>(201)
.Produces(400);

app.MapPost("/api/projects/{id:guid}/tasks", async (Guid id, CreateTaskRequest request, IValidator<CreateTaskRequest> validator, ProjectsDbContext context, ICurrentUser currentUser) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
        return Results.BadRequest(validationResult.Errors.Select(e => new { Property = e.PropertyName, Error = e.ErrorMessage }));

    // First verify project exists and user owns it
    var project = await context.Projects
        .AsNoTracking()
        .FirstOrDefaultAsync(p => p.Id == id);

    if (project == null)
        return Results.NotFound();

    // SUPERVISOR and CONTRACTOR can manage any project, others only their own projects
    if (currentUser.Role != "SUPERVISOR" && currentUser.Role != "CONTRACTOR" && project.OwnerUserId != currentUser.Id)
        return Results.Forbid();

    // Create task directly without loading the project into context
    var task = new ProjectTask(id, request.Title, request.DueDate);
    context.ProjectTasks.Add(task);
    
    try
    {
        await context.SaveChangesAsync();
        var response = new ProjectTaskDto(task.Id, task.ProjectId, task.Title, task.Status.ToString(), task.DueDate);
        return Results.Created($"/api/projects/{id}/tasks/{task.Id}", response);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error creating task: {ex.Message}");
    }
})
.RequireAuthorization()
.WithName("CreateTask")
.WithTags("Projects")
.Accepts<CreateTaskRequest>("application/json")
.Produces<ProjectTaskDto>(201)
.Produces(400)
.Produces(404)
.Produces(403);

app.MapPatch("/api/projects/{id:guid}/tasks/{taskId:guid}/status", async (Guid id, Guid taskId, UpdateTaskStatusRequest request, IValidator<UpdateTaskStatusRequest> validator, ProjectsDbContext context, ICurrentUser currentUser) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
        return Results.BadRequest(validationResult.Errors.Select(e => new { Property = e.PropertyName, Error = e.ErrorMessage }));

    var project = await context.Projects
        .Include(p => p.Tasks)
        .FirstOrDefaultAsync(p => p.Id == id);

    if (project == null)
        return Results.NotFound("Project not found");

    // SUPERVISOR and CONTRACTOR can manage any project, others only their own projects
    if (currentUser.Role != "SUPERVISOR" && currentUser.Role != "CONTRACTOR" && project.OwnerUserId != currentUser.Id)
        return Results.Forbid();

    var task = project.Tasks.FirstOrDefault(t => t.Id == taskId);
    if (task == null)
        return Results.NotFound("Task not found");

    // Update task status using domain method
    if (Enum.TryParse<Engitrack.Projects.Domain.Enums.TaskStatus>(request.Status, out var status))
    {
        task.UpdateStatus(status);
        await context.SaveChangesAsync();
        
        var response = new ProjectTaskDto(task.Id, task.ProjectId, task.Title, task.Status.ToString(), task.DueDate);
        return Results.Ok(response);
    }

    return Results.BadRequest(new { error = "Invalid status" });
})
.RequireAuthorization()
.WithName("UpdateTaskStatus")
.WithTags("Projects")
.Accepts<UpdateTaskStatusRequest>("application/json")
.Produces<ProjectTaskDto>(200)
.Produces(400)
.Produces(404)
.Produces(403);

app.MapDelete("/api/projects/{id:guid}/tasks/{taskId:guid}", async (Guid id, Guid taskId, ProjectsDbContext context, ICurrentUser currentUser) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    var project = await context.Projects
        .Include(p => p.Tasks)
        .FirstOrDefaultAsync(p => p.Id == id);

    if (project == null)
        return Results.NotFound("Project not found");

    // SUPERVISOR can manage any project, others only their own projects
    if (currentUser.Role != "SUPERVISOR" && project.OwnerUserId != currentUser.Id)
        return Results.Forbid();

    var task = project.Tasks.FirstOrDefault(t => t.Id == taskId);
    if (task == null)
        return Results.NotFound("Task not found");

    project.RemoveTask(taskId);
    await context.SaveChangesAsync();

    return Results.NoContent();
})
.RequireAuthorization()
.WithName("DeleteTask")
.WithTags("Projects")
.Produces(204)
.Produces(404)
.Produces(403);

app.MapDelete("/api/projects/{id:guid}", async (Guid id, ProjectsDbContext context, ICurrentUser currentUser) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    var project = await context.Projects
        .Include(p => p.Tasks)
        .FirstOrDefaultAsync(p => p.Id == id);

    if (project == null)
        return Results.NotFound("Project not found");

    // SUPERVISOR and CONTRACTOR can delete any project, others only their own projects
    if (currentUser.Role != "SUPERVISOR" && currentUser.Role != "CONTRACTOR" && project.OwnerUserId != currentUser.Id)
        return Results.Forbid();

    // Remove project and all related tasks (cascade delete is configured in EF)
    context.Projects.Remove(project);
    await context.SaveChangesAsync();

    return Results.NoContent();
})
.RequireAuthorization()
.WithName("DeleteProject")
.WithTags("Projects")
.Produces(204)
.Produces(404)
.Produces(403);

app.MapPatch("/api/projects/{id:guid}/complete", async (Guid id, ProjectsDbContext context, ICurrentUser currentUser) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    var project = await context.Projects
        .Include(p => p.Tasks)
        .FirstOrDefaultAsync(p => p.Id == id);

    if (project == null)
        return Results.NotFound();

    // SUPERVISOR and CONTRACTOR can manage any project, others only their own projects
    if (currentUser.Role != "SUPERVISOR" && currentUser.Role != "CONTRACTOR" && project.OwnerUserId != currentUser.Id)
        return Results.Forbid();

    try
    {
        project.Complete();
        await context.SaveChangesAsync();

        var response = new ProjectResponse(
            project.Id,
            project.Name,
            project.StartDate,
            project.EndDate,
            project.Budget,
            project.Status.ToString(),
            project.Priority.ToString(),
            project.Description,
            project.OwnerUserId,
            project.Tasks.Select(t => new ProjectTaskDto(t.Id, project.Id, t.Title, t.Status.ToString(), t.DueDate))
        );

        return Results.Ok(response);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.RequireAuthorization()
.WithName("CompleteProject")
.WithTags("Projects")
.Produces<ProjectResponse>(200)
.Produces(400)
.Produces(404)
.Produces(403);

app.MapPatch("/api/projects/{id:guid}", async (Guid id, UpdateProjectRequest request, IValidator<UpdateProjectRequest> validator, ProjectsDbContext context, ICurrentUser currentUser) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
        return Results.BadRequest(validationResult.Errors.Select(e => new { Property = e.PropertyName, Error = e.ErrorMessage }));

    var project = await context.Projects
        .Include(p => p.Tasks)
        .FirstOrDefaultAsync(p => p.Id == id);

    if (project == null)
        return Results.NotFound();

    // SUPERVISOR and CONTRACTOR can manage any project, others only their own projects
    if (currentUser.Role != "SUPERVISOR" && currentUser.Role != "CONTRACTOR" && project.OwnerUserId != currentUser.Id)
        return Results.Forbid();

    // Validate EndDate >= StartDate
    if (request.EndDate.HasValue && request.EndDate < project.StartDate)
        return Results.BadRequest(new { error = "EndDate must be >= StartDate" });

    // Update project
    project.UpdateDetails(request.Name, request.Budget, request.Priority, request.Description);
    if (request.EndDate.HasValue)
        project.SetEndDate(request.EndDate);

    await context.SaveChangesAsync();

    var response = new ProjectResponse(
        project.Id,
        project.Name,
        project.StartDate,
        project.EndDate,
        project.Budget,
        project.Status.ToString(),
        project.Priority.ToString(),
        project.Description,
        project.OwnerUserId,
        project.Tasks.Select(t => new ProjectTaskDto(t.Id, project.Id, t.Title, t.Status.ToString(), t.DueDate))
    );

    return Results.Ok(response);
})
.RequireAuthorization()
.WithName("UpdateProject")
.WithTags("Projects")
.Accepts<UpdateProjectRequest>("application/json")
.Produces<ProjectResponse>(200)
.Produces(400)
.Produces(404)
.Produces(403);

// PATCH /api/projects/{id}/priority - Update project priority specifically
app.MapPatch("/api/projects/{id:guid}/priority", async (Guid id, UpdatePriorityRequest request, ProjectsDbContext context, ICurrentUser currentUser) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    var project = await context.Projects
        .Include(p => p.Tasks)
        .FirstOrDefaultAsync(p => p.Id == id);

    if (project == null)
        return Results.NotFound();

    // SUPERVISOR and CONTRACTOR can manage any project, others only their own projects
    if (currentUser.Role != "SUPERVISOR" && currentUser.Role != "CONTRACTOR" && project.OwnerUserId != currentUser.Id)
        return Results.Forbid();

    // Update priority
    project.SetPriority(request.Priority);
    await context.SaveChangesAsync();

    var response = new ProjectResponse(
        project.Id,
        project.Name,
        project.StartDate,
        project.EndDate,
        project.Budget,
        project.Status.ToString(),
        project.Priority.ToString(),
        project.Description,
        project.OwnerUserId,
        project.Tasks.Select(t => new ProjectTaskDto(t.Id, project.Id, t.Title, t.Status.ToString(), t.DueDate))
    );

    return Results.Ok(response);
})
.RequireAuthorization()
.WithName("UpdateProjectPriority")
.WithTags("Projects")
.Accepts<UpdatePriorityRequest>("application/json")
.Produces<ProjectResponse>(200)
.Produces(404)
.Produces(403);

// PATCH /api/projects/{id}/priority/string - Update project priority using string values
app.MapPatch("/api/projects/{id:guid}/priority/string", async (Guid id, UpdatePriorityStringRequest request, IValidator<UpdatePriorityStringRequest> validator, ProjectsDbContext context, ICurrentUser currentUser) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
        return Results.BadRequest(validationResult.Errors.Select(e => new { Property = e.PropertyName, Error = e.ErrorMessage }));

    var project = await context.Projects
        .Include(p => p.Tasks)
        .FirstOrDefaultAsync(p => p.Id == id);

    if (project == null)
        return Results.NotFound();

    // SUPERVISOR and CONTRACTOR can manage any project, others only their own projects
    if (currentUser.Role != "SUPERVISOR" && currentUser.Role != "CONTRACTOR" && project.OwnerUserId != currentUser.Id)
        return Results.Forbid();

    // Convert string to enum
    var priority = request.Priority.ToUpper() switch
    {
        "LOW" => Priority.LOW,
        "MEDIUM" => Priority.MEDIUM,
        "HIGH" => Priority.HIGH,
        _ => throw new ArgumentException("Invalid priority value")
    };

    // Update priority
    project.SetPriority(priority);
    await context.SaveChangesAsync();

    var response = new ProjectResponse(
        project.Id,
        project.Name,
        project.StartDate,
        project.EndDate,
        project.Budget,
        project.Status.ToString(),
        project.Priority.ToString(),
        project.Description,
        project.OwnerUserId,
        project.Tasks.Select(t => new ProjectTaskDto(t.Id, project.Id, t.Title, t.Status.ToString(), t.DueDate))
    );

    return Results.Ok(response);
})
.RequireAuthorization()
.WithName("UpdateProjectPriorityString")
.WithTags("Projects")
.Accepts<UpdatePriorityStringRequest>("application/json")
.Produces<ProjectResponse>(200)
.Produces(400)
.Produces(404)
.Produces(403);

// ===== INVENTORY ENDPOINTS =====

// GET /api/inventory/materials - List materials with filters and pagination
app.MapGet("/api/inventory/materials", async (
    InventoryDbContext inventoryContext,
    ProjectsDbContext projectsContext,
    ClaimsPrincipal user,
    string? q = null,
    Guid? projectId = null,
    MaterialStatus? status = null,
    int page = 1,
    int pageSize = 20) =>
{
    var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    // Build query
    var query = inventoryContext.Materials.AsNoTracking();

    // Filter by ownership: only materials from projects owned by current user
    query = query.Where(m => projectsContext.Projects
        .Any(p => p.Id == m.ProjectId && p.OwnerUserId == userId));

    // Apply filters
    if (!string.IsNullOrEmpty(q))
        query = query.Where(m => m.Name.Contains(q));

    if (projectId.HasValue)
        query = query.Where(m => m.ProjectId == projectId.Value);

    if (status.HasValue)
        query = query.Where(m => m.Status == status.Value);

    // Get total count
    var total = await query.CountAsync();

    // Apply pagination
    var materials = await query
        .OrderBy(m => m.Name)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(m => new MaterialDto(
            m.MaterialId,
            m.ProjectId,
            m.Name,
            m.Unit,
            m.Stock,
            m.MinNivel,
            m.Status.ToString()
        ))
        .ToListAsync();

    return Results.Ok(new MaterialListResponse(materials, total, page, pageSize));
})
.RequireAuthorization()
.WithName("GetMaterials")
.WithTags("Inventory")
.Produces<MaterialListResponse>(200);

// POST /api/inventory/materials - Create material
app.MapPost("/api/inventory/materials", async (
    CreateMaterialRequest request,
    InventoryDbContext inventoryContext,
    ProjectsDbContext projectsContext,
    ClaimsPrincipal user,
    IValidator<CreateMaterialRequest> validator) =>
{
    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
        return Results.BadRequest(validationResult.ToDictionary());

    var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    // Verify project ownership
    var project = await projectsContext.Projects
        .AsNoTracking()
        .FirstOrDefaultAsync(p => p.Id == request.ProjectId && p.OwnerUserId == userId);

    if (project == null)
        return Results.NotFound("Project not found or access denied");

    // Check if material name already exists for this project
    var existingMaterial = await inventoryContext.Materials
        .AsNoTracking()
        .FirstOrDefaultAsync(m => m.ProjectId == request.ProjectId && m.Name == request.Name);

    if (existingMaterial != null)
        return Results.Conflict("A material with this name already exists in the project");

    try
    {
        var material = new Material(request.ProjectId, request.Name, request.Unit, request.MinNivel);
        inventoryContext.Materials.Add(material);
        await inventoryContext.SaveChangesAsync();

        var materialDto = new MaterialDto(
            material.MaterialId,
            material.ProjectId,
            material.Name,
            material.Unit,
            material.Stock,
            material.MinNivel,
            material.Status.ToString()
        );

        return Results.Created($"/api/inventory/materials/{material.MaterialId}", materialDto);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error creating material: {ex.Message}");
    }
})
.RequireAuthorization()
.WithName("CreateMaterial")
.WithTags("Inventory")
.Accepts<CreateMaterialRequest>("application/json")
.Produces<MaterialDto>(201)
.Produces(400)
.Produces(404)
.Produces(409);

// GET /api/inventory/materials/{id} - Get material by ID
app.MapGet("/api/inventory/materials/{id:guid}", async (
    Guid id,
    InventoryDbContext inventoryContext,
    ProjectsDbContext projectsContext,
    ClaimsPrincipal user) =>
{
    var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    var material = await inventoryContext.Materials
        .AsNoTracking()
        .FirstOrDefaultAsync(m => m.MaterialId == id);

    if (material == null)
        return Results.NotFound("Material not found");

    // Verify ownership through project
    var project = await projectsContext.Projects
        .AsNoTracking()
        .FirstOrDefaultAsync(p => p.Id == material.ProjectId && p.OwnerUserId == userId);

    if (project == null)
        return Results.NotFound("Material not found or access denied");

    var materialDto = new MaterialDto(
        material.MaterialId,
        material.ProjectId,
        material.Name,
        material.Unit,
        material.Stock,
        material.MinNivel,
        material.Status.ToString()
    );

    return Results.Ok(materialDto);
})
.RequireAuthorization()
.WithName("GetMaterialById")
.WithTags("Inventory")
.Produces<MaterialDto>(200)
.Produces(404);

// PATCH /api/inventory/materials/{id} - Update material
app.MapPatch("/api/inventory/materials/{id:guid}", async (
    Guid id,
    UpdateMaterialRequest request,
    InventoryDbContext inventoryContext,
    ProjectsDbContext projectsContext,
    ClaimsPrincipal user,
    IValidator<UpdateMaterialRequest> validator) =>
{
    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
        return Results.BadRequest(validationResult.ToDictionary());

    var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    var material = await inventoryContext.Materials
        .FirstOrDefaultAsync(m => m.MaterialId == id);

    if (material == null)
        return Results.NotFound("Material not found");

    // Verify ownership through project
    var project = await projectsContext.Projects
        .AsNoTracking()
        .FirstOrDefaultAsync(p => p.Id == material.ProjectId && p.OwnerUserId == userId);

    if (project == null)
        return Results.NotFound("Material not found or access denied");

    try
    {
        // Update fields
        if (!string.IsNullOrEmpty(request.Name))
            material.Rename(request.Name);

        if (!string.IsNullOrEmpty(request.Unit))
            material.UpdateUnit(request.Unit);

        if (request.MinNivel.HasValue)
            material.SetMinNivel(request.MinNivel.Value);

        if (request.Archive.HasValue)
        {
            if (request.Archive.Value)
                material.Archive();
            else
                material.Activate();
        }

        await inventoryContext.SaveChangesAsync();

        var materialDto = new MaterialDto(
            material.MaterialId,
            material.ProjectId,
            material.Name,
            material.Unit,
            material.Stock,
            material.MinNivel,
            material.Status.ToString()
        );

        return Results.Ok(materialDto);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error updating material: {ex.Message}");
    }
})
.RequireAuthorization()
.WithName("UpdateMaterial")
.WithTags("Inventory")
.Accepts<UpdateMaterialRequest>("application/json")
.Produces<MaterialDto>(200)
.Produces(400)
.Produces(404);

// POST /api/inventory/materials/{id}/transactions - Register transaction using SP
app.MapPost("/api/inventory/materials/{id:guid}/transactions", async (
    Guid id,
    RegisterTransactionRequest request,
    InventoryDbContext inventoryContext,
    ProjectsDbContext projectsContext,
    ClaimsPrincipal user,
    IValidator<RegisterTransactionRequest> validator) =>
{
    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
        return Results.BadRequest(validationResult.ToDictionary());

    var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    var material = await inventoryContext.Materials
        .FirstOrDefaultAsync(m => m.MaterialId == id);

    if (material == null)
        return Results.NotFound("Material not found");

    try
    {
        // Call stored procedure for atomic transaction
        var spResult = await inventoryContext.Database.SqlQueryRaw<SpTransactionResult>(
            @"EXEC [inventory].[usp_RegisterTransaction] 
                @MaterialId = {0}, 
                @ProjectId = {1}, 
                @TxType = {2}, 
                @Quantity = {3}, 
                @SupplierId = {4}, 
                @Notes = {5}, 
                @OwnerUserId = {6}",
            id,
            material.ProjectId,
            request.TxType,
            request.Quantity,
            (object?)request.SupplierId ?? DBNull.Value,
            (object?)request.Notes ?? DBNull.Value,
            userId
        ).FirstOrDefaultAsync();

        if (spResult == null || spResult.Result != "SUCCESS")
        {
            return Results.Problem("Transaction failed");
        }

        // Return transaction details
        var transactionDto = new TransactionDto(
            Guid.NewGuid(), // We'll get the actual TxId from a follow-up query if needed
            request.TxType,
            spResult.TransactionQuantity,
            DateTime.UtcNow,
            request.SupplierId,
            request.Notes
        );

        return Results.Created($"/api/inventory/materials/{id}/transactions", new
        {
            Transaction = transactionDto,
            Result = spResult.Result,
            NewStock = spResult.NewStock,
            PreviousStock = spResult.PreviousStock
        });
    }
    catch (SqlException sqlEx) when (sqlEx.Message.Contains("Material not found"))
    {
        return Results.NotFound("Material not found or access denied");
    }
    catch (SqlException sqlEx) when (sqlEx.Message.Contains("Insufficient stock"))
    {
        return Results.Conflict(sqlEx.Message);
    }
    catch (SqlException sqlEx) when (sqlEx.Message.Contains("Stock cannot be negative"))
    {
        return Results.Conflict(sqlEx.Message);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error registering transaction: {ex.Message}");
    }
})
.RequireAuthorization()
.WithName("RegisterTransactionWithSP")
.WithTags("Inventory")
.Accepts<RegisterTransactionRequest>("application/json")
.Produces(201)
.Produces(400)
.Produces(404)
.Produces(409);

// GET /api/inventory/materials/{id}/transactions - Get material transactions
app.MapGet("/api/inventory/materials/{id:guid}/transactions", async (
    Guid id,
    InventoryDbContext inventoryContext,
    ProjectsDbContext projectsContext,
    ClaimsPrincipal user,
    DateTime? from = null,
    DateTime? to = null,
    int page = 1,
    int pageSize = 20) =>
{
    var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    var material = await inventoryContext.Materials
        .AsNoTracking()
        .FirstOrDefaultAsync(m => m.MaterialId == id);

    if (material == null)
        return Results.NotFound("Material not found");

    // Verify ownership through project
    var project = await projectsContext.Projects
        .AsNoTracking()
        .FirstOrDefaultAsync(p => p.Id == material.ProjectId && p.OwnerUserId == userId);

    if (project == null)
        return Results.NotFound("Material not found or access denied");

    // Build query
    var query = inventoryContext.InventoryTransactions
        .AsNoTracking()
        .Where(t => t.MaterialId == id);

    // Apply date filters
    if (from.HasValue)
        query = query.Where(t => t.TxDate >= from.Value);

    if (to.HasValue)
        query = query.Where(t => t.TxDate <= to.Value);

    // Get total count
    var total = await query.CountAsync();

    // Apply pagination and ordering
    var transactions = await query
        .OrderByDescending(t => t.TxDate)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(t => new TransactionDto(
            t.TxId,
            t.TxType.ToString(),
            t.Quantity,
            t.TxDate,
            t.SupplierId,
            t.Notes
        ))
        .ToListAsync();

    return Results.Ok(new TransactionListResponse(transactions, total, page, pageSize));
})
.RequireAuthorization()
.WithName("GetMaterialTransactions")
.WithTags("Inventory")
.Produces<TransactionListResponse>(200)
.Produces(404);

// Workers endpoints
app.MapGet("/api/workers", async (ProjectsDbContext context, ICurrentUser currentUser, int page = 1, int pageSize = 10) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    // Devolver TODOS los workers sin filtrar por asignaciones o proyectos
    var workers = await context.Workers
        .AsNoTracking()
        .Include(w => w.Assignments)
        .OrderBy(w => w.FullName)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    var response = workers.Select(w => new WorkerResponse(
        w.Id,
        w.FullName,
        w.DocumentNumber,
        w.Phone,
        w.Position,
        w.HourlyRate,
        w.Assignments.Select(a => new AssignmentDto(a.Id, a.WorkerId, a.ProjectId, a.StartDate, a.EndDate))
    ));

    return Results.Ok(response);
})
.RequireAuthorization()
.WithName("GetWorkers")
.WithTags("Workers")
.Produces<IEnumerable<WorkerResponse>>(200);

app.MapGet("/api/workers/{id:guid}", async (Guid id, ProjectsDbContext context, ICurrentUser currentUser) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    var worker = await context.Workers
        .AsNoTracking()
        .Include(w => w.Assignments)
        .FirstOrDefaultAsync(w => w.Id == id);

    if (worker == null)
        return Results.NotFound();

    var response = new WorkerResponse(
        worker.Id,
        worker.FullName,
        worker.DocumentNumber,
        worker.Phone,
        worker.Position,
        worker.HourlyRate,
        worker.Assignments.Select(a => new AssignmentDto(a.Id, a.WorkerId, a.ProjectId, a.StartDate, a.EndDate))
    );

    return Results.Ok(response);
})
.RequireAuthorization()
.WithName("GetWorker")
.WithTags("Workers")
.Produces<WorkerResponse>(200)
.Produces(404);

app.MapPost("/api/workers", async (CreateWorkerRequest request, IValidator<CreateWorkerRequest> validator, ProjectsDbContext context, ICurrentUser currentUser) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
        return Results.BadRequest(validationResult.Errors.Select(e => new { Property = e.PropertyName, Error = e.ErrorMessage }));

    // Si se proporciona ProjectId, validar que el proyecto existe y el usuario lo posee
    if (request.ProjectId.HasValue)
    {
        var project = await context.Projects.FirstOrDefaultAsync(p => p.Id == request.ProjectId.Value);
        if (project == null)
            return Results.BadRequest(new { error = "Project not found" });

        if (project.OwnerUserId != currentUser.Id)
            return Results.Forbid();
    }

    try
    {
        // Crear el worker siempre
        var worker = new Worker(request.FullName, request.DocumentNumber, request.Phone, request.Position, request.HourlyRate);
        context.Workers.Add(worker);
        await context.SaveChangesAsync();

        // Solo crear asignación si se proporcionó ProjectId
        var assignments = new List<AssignmentDto>();
        if (request.ProjectId.HasValue)
        {
            var assignment = new Assignment(worker.Id, request.ProjectId.Value, DateOnly.FromDateTime(DateTime.Today));
            context.Assignments.Add(assignment);
            await context.SaveChangesAsync();

            assignments.Add(new AssignmentDto(assignment.Id, assignment.WorkerId, assignment.ProjectId, assignment.StartDate, assignment.EndDate));
        }

        var response = new WorkerResponse(
            worker.Id,
            worker.FullName,
            worker.DocumentNumber,
            worker.Phone,
            worker.Position,
            worker.HourlyRate,
            assignments
        );

        return Results.Created($"/api/workers/{worker.Id}", response);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.RequireAuthorization()
.WithName("CreateWorker")
.WithTags("Workers")
.Accepts<CreateWorkerRequest>("application/json")
.Produces<WorkerResponse>(201)
.Produces(400)
.Produces(403);

app.MapPut("/api/workers/{id:guid}", async (Guid id, UpdateWorkerRequest request, IValidator<UpdateWorkerRequest> validator, ProjectsDbContext context, ICurrentUser currentUser) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
        return Results.BadRequest(validationResult.Errors.Select(e => new { Property = e.PropertyName, Error = e.ErrorMessage }));

    var worker = await context.Workers
        .Include(w => w.Assignments)
        .FirstOrDefaultAsync(w => w.Id == id);

    if (worker == null)
        return Results.NotFound();

    try
    {
        worker.UpdateInfo(request.FullName, request.Phone, request.Position, request.HourlyRate);
        await context.SaveChangesAsync();

        var response = new WorkerResponse(
            worker.Id,
            worker.FullName,
            worker.DocumentNumber,
            worker.Phone,
            worker.Position,
            worker.HourlyRate,
            worker.Assignments.Select(a => new AssignmentDto(a.Id, a.WorkerId, a.ProjectId, a.StartDate, a.EndDate))
        );

        return Results.Ok(response);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.RequireAuthorization()
.WithName("UpdateWorker")
.WithTags("Workers")
.Accepts<UpdateWorkerRequest>("application/json")
.Produces<WorkerResponse>(200)
.Produces(400)
.Produces(404);

app.MapDelete("/api/workers/{id:guid}", async (Guid id, ProjectsDbContext context, ICurrentUser currentUser) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    var worker = await context.Workers.FirstOrDefaultAsync(w => w.Id == id);
    if (worker == null)
        return Results.NotFound();

    context.Workers.Remove(worker);
    await context.SaveChangesAsync();

    return Results.NoContent();
})
.RequireAuthorization()
.WithName("DeleteWorker")
.WithTags("Workers")
.Produces(204)
.Produces(404);

// Worker-Project Assignment endpoints
app.MapGet("/api/projects/{projectId:guid}/workers", async (Guid projectId, ProjectsDbContext context, ICurrentUser currentUser) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    // Check if project exists
    var project = await context.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
    if (project == null)
        return Results.NotFound(new { error = "Project not found" });

    // SUPERVISOR and CONTRACTOR can access any project, others only their own projects
    if (currentUser.Role != "SUPERVISOR" && currentUser.Role != "CONTRACTOR" && project.OwnerUserId != currentUser.Id)
        return Results.Forbid();

    var assignments = await context.Assignments
        .AsNoTracking()
        .Include(a => a.Worker)
        .Where(a => a.ProjectId == projectId)
        .ToListAsync();

    var response = assignments.Select(a => new ProjectWorkerResponse(
        a.Worker!.Id,
        a.Worker.FullName,
        a.Worker.DocumentNumber,
        a.Worker.Phone,
        a.Worker.Position,
        a.Worker.HourlyRate,
        a.Id,
        a.StartDate,
        a.EndDate
    ));

    return Results.Ok(response);
})
.RequireAuthorization()
.WithName("GetProjectWorkers")
.WithTags("Workers")
.Produces<IEnumerable<ProjectWorkerResponse>>(200)
.Produces(404)
.Produces(403);

app.MapPost("/api/projects/{projectId:guid}/workers", async (Guid projectId, AssignWorkerRequest request, IValidator<AssignWorkerRequest> validator, ProjectsDbContext context, ICurrentUser currentUser) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
        return Results.BadRequest(validationResult.Errors.Select(e => new { Property = e.PropertyName, Error = e.ErrorMessage }));

    // Check if project exists
    var project = await context.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
    if (project == null)
        return Results.NotFound(new { error = "Project not found" });

    // SUPERVISOR and CONTRACTOR can manage any project, others only their own projects
    if (currentUser.Role != "SUPERVISOR" && currentUser.Role != "CONTRACTOR" && project.OwnerUserId != currentUser.Id)
        return Results.Forbid();

    // Check if worker exists
    var worker = await context.Workers.FirstOrDefaultAsync(w => w.Id == request.WorkerId);
    if (worker == null)
        return Results.BadRequest(new { error = "Worker not found" });

    // Check if worker is already assigned to this project (active assignment)
    var existingAssignment = await context.Assignments
        .FirstOrDefaultAsync(a => a.WorkerId == request.WorkerId && a.ProjectId == projectId && a.EndDate == null);
    
    if (existingAssignment != null)
        return Results.BadRequest(new { error = "Worker is already assigned to this project" });

    try
    {
        var assignment = new Assignment(request.WorkerId, projectId, request.StartDate);
        if (request.EndDate.HasValue)
            assignment.EndAssignment(request.EndDate.Value);

        context.Assignments.Add(assignment);
        await context.SaveChangesAsync();

        var response = new ProjectWorkerResponse(
            worker.Id,
            worker.FullName,
            worker.DocumentNumber,
            worker.Phone,
            worker.Position,
            worker.HourlyRate,
            assignment.Id,
            assignment.StartDate,
            assignment.EndDate
        );

        return Results.Created($"/api/projects/{projectId}/workers/{worker.Id}", response);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.RequireAuthorization()
.WithName("AssignWorkerToProject")
.WithTags("Workers")
.Accepts<AssignWorkerRequest>("application/json")
.Produces<ProjectWorkerResponse>(201)
.Produces(400)
.Produces(404)
.Produces(403);

app.MapDelete("/api/projects/{projectId:guid}/workers/{workerId:guid}", async (Guid projectId, Guid workerId, ProjectsDbContext context, ICurrentUser currentUser) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    // Check if project exists
    var project = await context.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
    if (project == null)
        return Results.NotFound(new { error = "Project not found" });

    // SUPERVISOR and CONTRACTOR can manage any project, others only their own projects
    if (currentUser.Role != "SUPERVISOR" && currentUser.Role != "CONTRACTOR" && project.OwnerUserId != currentUser.Id)
        return Results.Forbid();

    // Find assignment for this project and worker
    var assignment = await context.Assignments
        .FirstOrDefaultAsync(a => a.WorkerId == workerId && a.ProjectId == projectId);

    if (assignment == null)
        return Results.NotFound(new { error = "Assignment not found" });

    // Remove the assignment completely
    context.Assignments.Remove(assignment);
    await context.SaveChangesAsync();

    return Results.NoContent();
})
.RequireAuthorization()
.WithName("RemoveWorkerFromProject")
.WithTags("Workers")
.Produces(204)
.Produces(404)
.Produces(403);

app.MapGet("/api/workers/{id:guid}/assignments", async (Guid id, ProjectsDbContext context, ICurrentUser currentUser) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    // Check if worker exists
    var worker = await context.Workers.FirstOrDefaultAsync(w => w.Id == id);
    if (worker == null)
        return Results.NotFound(new { error = "Worker not found" });

    // Check if user has access to this worker (owns any project the worker is assigned to)
    var hasAccess = await context.Assignments
        .AnyAsync(a => a.WorkerId == id && 
                      context.Projects.Any(p => p.Id == a.ProjectId && p.OwnerUserId == currentUser.Id));

    if (!hasAccess)
        return Results.Forbid();

    var assignments = await context.Assignments
        .AsNoTracking()
        .Where(a => a.WorkerId == id)
        .Join(context.Projects,
              assignment => assignment.ProjectId,
              project => project.Id,
              (assignment, project) => new { assignment, project })
        .Where(x => x.project.OwnerUserId == currentUser.Id)
        .Select(x => new WorkerAssignmentResponse(
            x.assignment.Id,
            x.project.Id,
            x.project.Name,
            x.assignment.StartDate,
            x.assignment.EndDate
        ))
        .ToListAsync();

    return Results.Ok(assignments);
})
.RequireAuthorization()
.WithName("GetWorkerAssignments")
.WithTags("Workers")
.Produces<IEnumerable<WorkerAssignmentResponse>>(200)
.Produces(404)
.Produces(403);

// Assignments endpoints
app.MapGet("/api/assignments", async (ProjectsDbContext context, ICurrentUser currentUser, int page = 1, int pageSize = 10) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    var query = context.Assignments
        .AsNoTracking()
        .Include(a => a.Worker)
        .Where(a => context.Projects.Any(p => p.Id == a.ProjectId && p.OwnerUserId == currentUser.Id));

    var assignments = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    var response = assignments.Select(a => new AssignmentResponse(
        a.Id,
        a.WorkerId,
        a.ProjectId,
        "Project Name", // TODO: Include project name
        a.StartDate,
        a.EndDate
    ));

    return Results.Ok(response);
})
.RequireAuthorization()
.WithName("GetAssignments")
.WithTags("Assignments")
.Produces<IEnumerable<AssignmentResponse>>(200);

app.MapPost("/api/assignments", async (CreateAssignmentRequest request, IValidator<CreateAssignmentRequest> validator, ProjectsDbContext context, ICurrentUser currentUser) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
        return Results.BadRequest(validationResult.Errors.Select(e => new { Property = e.PropertyName, Error = e.ErrorMessage }));

    // Check if user owns the project
    var project = await context.Projects.FirstOrDefaultAsync(p => p.Id == request.ProjectId);
    if (project == null)
        return Results.BadRequest(new { error = "Project not found" });

    if (project.OwnerUserId != currentUser.Id)
        return Results.Forbid();

    // Check if worker exists
    var worker = await context.Workers.FirstOrDefaultAsync(w => w.Id == request.WorkerId);
    if (worker == null)
        return Results.BadRequest(new { error = "Worker not found" });

    try
    {
        var assignment = new Assignment(request.WorkerId, request.ProjectId, request.StartDate);
        context.Assignments.Add(assignment);
        await context.SaveChangesAsync();

        var response = new AssignmentResponse(
            assignment.Id,
            assignment.WorkerId,
            assignment.ProjectId,
            project.Name,
            assignment.StartDate,
            assignment.EndDate
        );

        return Results.Created($"/api/assignments/{assignment.Id}", response);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error creating assignment: {ex.Message}");
    }
})
.RequireAuthorization()
.WithName("CreateAssignment")
.WithTags("Assignments")
.Accepts<CreateAssignmentRequest>("application/json")
.Produces<AssignmentResponse>(201)
.Produces(400)
.Produces(403);

app.MapPatch("/api/assignments/{id:guid}/end", async (Guid id, EndAssignmentRequest request, ProjectsDbContext context, ICurrentUser currentUser) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    var assignment = await context.Assignments
        .Include(a => a.Worker)
        .FirstOrDefaultAsync(a => a.Id == id);

    if (assignment == null)
        return Results.NotFound();

    // Check if user owns the project
    var hasAccess = await context.Projects
        .AnyAsync(p => p.Id == assignment.ProjectId && p.OwnerUserId == currentUser.Id);

    if (!hasAccess)
        return Results.Forbid();

    try
    {
        assignment.EndAssignment(request.EndDate);
        await context.SaveChangesAsync();

        return Results.Ok(new { message = "Assignment ended successfully" });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.RequireAuthorization()
.WithName("EndAssignment")
.WithTags("Assignments")
.Accepts<EndAssignmentRequest>("application/json")
.Produces(200)
.Produces(400)
.Produces(404)
.Produces(403);

// Attendances endpoints
app.MapGet("/api/attendances", async (ProjectsDbContext context, ICurrentUser currentUser, Guid? projectId = null, DateOnly? day = null, int page = 1, int pageSize = 10) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    var query = context.Attendances
        .AsNoTracking()
        .Include(a => a.Worker)
        .Where(a => context.Projects.Any(p => p.Id == a.ProjectId && p.OwnerUserId == currentUser.Id));

    if (projectId.HasValue)
        query = query.Where(a => a.ProjectId == projectId);

    if (day.HasValue)
        query = query.Where(a => a.Day == day);

    var attendances = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    var response = attendances.Select(a => new AttendanceResponse(
        a.Id,
        a.WorkerId,
        "Worker Name", // TODO: Include worker name
        a.ProjectId,
        "Project Name", // TODO: Include project name
        a.Day,
        a.CheckIn,
        a.CheckOut,
        a.Status.ToString(),
        a.Notes
    ));

    return Results.Ok(response);
})
.RequireAuthorization()
.WithName("GetAttendances")
.WithTags("Attendances")
.Produces<IEnumerable<AttendanceResponse>>(200);

app.MapPost("/api/attendances", async (CreateAttendanceRequest request, IValidator<CreateAttendanceRequest> validator, ProjectsDbContext context, ICurrentUser currentUser) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
        return Results.BadRequest(validationResult.Errors.Select(e => new { Property = e.PropertyName, Error = e.ErrorMessage }));

    // Check if user owns the project
    var project = await context.Projects.FirstOrDefaultAsync(p => p.Id == request.ProjectId);
    if (project == null)
        return Results.BadRequest(new { error = "Project not found" });

    if (project.OwnerUserId != currentUser.Id)
        return Results.Forbid();

    // Check if worker exists
    var worker = await context.Workers.FirstOrDefaultAsync(w => w.Id == request.WorkerId);
    if (worker == null)
        return Results.BadRequest(new { error = "Worker not found" });

    try
    {
        if (!Enum.TryParse<Engitrack.Workers.Domain.Enums.AttendanceStatus>(request.Status, out var status))
            return Results.BadRequest(new { error = "Invalid status" });

        var attendance = new Attendance(request.WorkerId, request.ProjectId, request.Day, status, request.Notes);
        context.Attendances.Add(attendance);
        await context.SaveChangesAsync();

        var response = new AttendanceResponse(
            attendance.Id,
            attendance.WorkerId,
            worker.FullName,
            attendance.ProjectId,
            project.Name,
            attendance.Day,
            attendance.CheckIn,
            attendance.CheckOut,
            attendance.Status.ToString(),
            attendance.Notes
        );

        return Results.Created($"/api/attendances/{attendance.Id}", response);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error creating attendance: {ex.Message}");
    }
})
.RequireAuthorization()
.WithName("CreateAttendance")
.WithTags("Attendances")
.Accepts<CreateAttendanceRequest>("application/json")
.Produces<AttendanceResponse>(201)
.Produces(400)
.Produces(403);

// Auth endpoints
app.MapPost("/auth/register", async (RegisterRequest request, ProjectsDbContext context, JwtHelper jwtHelper) =>
{
    try
    {
        // Check if user already exists
        if (await context.Users.AnyAsync(u => u.Email == request.Email))
            return Results.BadRequest(new { error = "User with this email already exists" });

        // Validate role
        if (!Enum.TryParse<Role>(request.Role, true, out var role))
            return Results.BadRequest(new { error = "Invalid role" });

        // Hash password
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        // Create user
        var user = new User(request.Email, request.FullName, request.Phone ?? "000-000-0000", role, passwordHash);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Generate token
        var token = jwtHelper.GenerateToken(user.Id, user.Email, user.Role.ToString());

        return Results.Ok(new AuthResponse(user.Id, user.Email, user.Role.ToString(), token));
    }
    catch (Exception ex)
    {
        return Results.Problem($"Registration error: {ex.Message}");
    }
})
.WithName("Register")
.WithTags("Auth")
.Accepts<RegisterRequest>("application/json")
.Produces<AuthResponse>(200)
.Produces(400);

app.MapPost("/auth/login", async (LoginRequest request, ProjectsDbContext context, JwtHelper jwtHelper) =>
{
    try
    {
        // Find user
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null)
            return Results.Unauthorized();

        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Results.Unauthorized();

        // Generate token
        var token = jwtHelper.GenerateToken(user.Id, user.Email, user.Role.ToString());

        return Results.Ok(new AuthResponse(user.Id, user.Email, user.Role.ToString(), token));
    }
    catch (Exception ex)
    {
        return Results.Problem($"Login error: {ex.Message}");
    }
})
.WithName("Login")
.WithTags("Auth")
.Accepts<LoginRequest>("application/json")
.Produces<AuthResponse>(200)
.Produces(401);

// Password Reset endpoints
app.MapPost("/auth/forgot-password", async (ForgotPasswordRequest request, ProjectsDbContext context, IEmailService emailService) =>
{
    try
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        
        // Always return success message to avoid revealing if email exists
        if (user == null)
            return Results.Ok(new ForgotPasswordResponse("Si el correo existe, se enviará un código de verificación."));

        // Generate 6-digit code
        var code = new Random().Next(100000, 999999).ToString();
        
        // Set token with 15 minutes expiry
        user.SetPasswordResetToken(code, DateTime.UtcNow.AddMinutes(15));
        await context.SaveChangesAsync();

        // Send email
        await emailService.SendPasswordResetEmailAsync(user.Email, code);

        return Results.Ok(new ForgotPasswordResponse("Si el correo existe, se enviará un código de verificación."));
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error al procesar la solicitud: {ex.Message}");
    }
})
.WithName("ForgotPassword")
.WithTags("Auth")
.Accepts<ForgotPasswordRequest>("application/json")
.Produces<ForgotPasswordResponse>(200);

app.MapPost("/auth/verify-reset-code", async (VerifyResetCodeRequest request, ProjectsDbContext context) =>
{
    var user = await context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
    
    if (user == null || !user.IsPasswordResetTokenValid(request.Code))
        return Results.Ok(new VerifyResetCodeResponse(false));

    return Results.Ok(new VerifyResetCodeResponse(true));
})
.WithName("VerifyResetCode")
.WithTags("Auth")
.Accepts<VerifyResetCodeRequest>("application/json")
.Produces<VerifyResetCodeResponse>(200);

app.MapPost("/auth/reset-password", async (ResetPasswordRequest request, ProjectsDbContext context) =>
{
    var user = await context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
    
    if (user == null || !user.IsPasswordResetTokenValid(request.Code))
        return Results.BadRequest(new { error = "Código inválido o expirado" });

    // Validate new password
    if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
        return Results.BadRequest(new { error = "La contraseña debe tener al menos 6 caracteres" });

    // Update password and clear reset token
    user.SetPassword(BCrypt.Net.BCrypt.HashPassword(request.NewPassword));
    user.ClearPasswordResetToken();
    await context.SaveChangesAsync();

    return Results.Ok(new { message = "Contraseña actualizada exitosamente" });
})
.WithName("ResetPassword")
.WithTags("Auth")
.Accepts<ResetPasswordRequest>("application/json")
.Produces(200)
.Produces(400);

// User profile endpoints
app.MapGet("/api/users/profile", async (ProjectsDbContext context, ClaimsPrincipal user) =>
{
    var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    
    var userProfile = await context.Users
        .AsNoTracking()
        .FirstOrDefaultAsync(u => u.Id == userId);
    
    if (userProfile == null)
        return Results.NotFound("User not found");
    
    var profileResponse = new UserProfileResponse(
        userProfile.Id,
        userProfile.Email,
        userProfile.FullName,
        userProfile.Phone,
        userProfile.Role.ToString()
    );
    
    return Results.Ok(profileResponse);
})
.RequireAuthorization()
.WithName("GetUserProfile")
.WithTags("Users")
.Produces<UserProfileResponse>(200)
.Produces(404);

app.MapGet("/api/users/profile/stats", async (
    ProjectsDbContext context, 
    ClaimsPrincipal user,
    CancellationToken cancellationToken) =>
{
    var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    
    // Count projects owned by the user
    var projectsCount = await context.Projects
        .AsNoTracking()
        .Where(p => p.OwnerUserId == userId)
        .CountAsync(cancellationToken);
    
    // Count all tasks in projects owned by the user
    var tasksCount = await context.ProjectTasks
        .AsNoTracking()
        .Where(t => context.Projects.Any(p => p.Id == t.ProjectId && p.OwnerUserId == userId))
        .CountAsync(cancellationToken);
    
    // Count completed tasks (DONE status) in projects owned by the user
    var completedTasksCount = await context.ProjectTasks
        .AsNoTracking()
        .Where(t => context.Projects.Any(p => p.Id == t.ProjectId && p.OwnerUserId == userId) 
                    && t.Status == Engitrack.Projects.Domain.Enums.TaskStatus.DONE)
        .CountAsync(cancellationToken);
    
    return Results.Ok(new UserStatsResponse(
        projectsCount,
        tasksCount,
        completedTasksCount
    ));
})
.RequireAuthorization()
.WithName("GetUserProfileStats")
.WithTags("Users")
.Produces<UserStatsResponse>(200);

app.MapGet("/api/users/{id:guid}", async (Guid id, ProjectsDbContext context, ClaimsPrincipal user) =>
{
    var currentUserId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    var currentUserRole = user.FindFirst(ClaimTypes.Role)!.Value;
    
    // Only SUPERVISOR and CONTRACTOR can see other users, or users can see themselves
    if (currentUserRole != "SUPERVISOR" && currentUserRole != "CONTRACTOR" && currentUserId != id)
        return Results.Forbid();
    
    var targetUser = await context.Users
        .AsNoTracking()
        .FirstOrDefaultAsync(u => u.Id == id);
    
    if (targetUser == null)
        return Results.NotFound("User not found");
    
    var userResponse = new UserProfileResponse(
        targetUser.Id,
        targetUser.Email,
        targetUser.FullName,
        targetUser.Phone,
        targetUser.Role.ToString()
    );
    
    return Results.Ok(userResponse);
})
.RequireAuthorization()
.WithName("GetUserById")
.WithTags("Users")
.Produces<UserProfileResponse>(200)
.Produces(404)
.Produces(403);

app.MapGet("/api/users", async (ProjectsDbContext context, ClaimsPrincipal user, int page = 1, int pageSize = 20) =>
{
    var currentUserRole = user.FindFirst(ClaimTypes.Role)!.Value;
    
    // Only SUPERVISOR and CONTRACTOR can list all users
    if (currentUserRole != "SUPERVISOR" && currentUserRole != "CONTRACTOR")
        return Results.Forbid();
    
    var users = await context.Users
        .AsNoTracking()
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(u => new UserProfileResponse(
            u.Id,
            u.Email,
            u.FullName,
            u.Phone,
            u.Role.ToString()
        ))
        .ToListAsync();
    
    return Results.Ok(users);
})
.RequireAuthorization()
.WithName("GetUsers")
.WithTags("Users")
.Produces<List<UserProfileResponse>>(200)
.Produces(403);

app.MapPatch("/api/users/profile", async (UpdateUserProfileRequest request, ProjectsDbContext context, ClaimsPrincipal user, IValidator<UpdateUserProfileRequest> validator) =>
{
    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
        return Results.BadRequest(validationResult.Errors.Select(e => new { Property = e.PropertyName, Error = e.ErrorMessage }));

    var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    
    var userProfile = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
    if (userProfile == null)
        return Results.NotFound("User not found");
    
    try
    {
        // Update profile information
        userProfile.UpdateProfile(request.FullName, request.Phone);
        await context.SaveChangesAsync();
        
        var profileResponse = new UserProfileResponse(
            userProfile.Id,
            userProfile.Email,
            userProfile.FullName,
            userProfile.Phone,
            userProfile.Role.ToString()
        );
        
        return Results.Ok(profileResponse);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.RequireAuthorization()
.WithName("UpdateUserProfile")
.WithTags("Users")
.Accepts<UpdateUserProfileRequest>("application/json")
.Produces<UserProfileResponse>(200)
.Produces(400)
.Produces(404);

// ===== INCIDENTS ENDPOINTS =====

app.MapGet("/api/projects/{projectId:guid}/incidents", async (
    Guid projectId,
    ProjectsDbContext context,
    ClaimsPrincipal user) =>
{
    var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    
    // Verify project ownership
    var project = await context.Projects
        .AsNoTracking()
        .FirstOrDefaultAsync(p => p.Id == projectId && p.OwnerUserId == userId);
    
    if (project == null)
        return Results.NotFound("Project not found or access denied");

    var incidents = await context.Incidents
        .AsNoTracking()
        .Where(i => i.ProjectId == projectId)
        .Select(i => new IncidentDto(
            i.Id,
            i.ProjectId,
            i.Title,
            i.Description,
            i.Severity.ToString(),
            i.Status.ToString(),
            i.ReportedBy,
            i.ReportedAt,
            i.AssignedTo,
            i.ResolvedAt
        ))
        .ToListAsync();

    return Results.Ok(incidents);
})
.RequireAuthorization()
.WithName("GetProjectIncidents")
.WithTags("Incidents")
.Produces<List<IncidentDto>>(200)
.Produces(404);

app.MapPost("/api/projects/{projectId:guid}/incidents", async (
    Guid projectId,
    CreateIncidentRequest request,
    ProjectsDbContext context,
    ClaimsPrincipal user,
    IValidator<CreateIncidentRequest> validator) =>
{
    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
        return Results.BadRequest(validationResult.ToDictionary());

    var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    
    // Verify project ownership
    var project = await context.Projects
        .AsNoTracking()
        .FirstOrDefaultAsync(p => p.Id == projectId && p.OwnerUserId == userId);
    
    if (project == null)
        return Results.NotFound("Project not found or access denied");

    var incident = new Incident
    {
        ProjectId = projectId,
        Title = request.Title,
        Description = request.Description,
        Severity = Enum.Parse<IncidentSeverity>(request.Severity),
        Status = IncidentStatus.OPEN,
        ReportedBy = request.ReportedBy,
        ReportedAt = DateTime.UtcNow
    };

    context.Incidents.Add(incident);
    await context.SaveChangesAsync();

    var incidentDto = new IncidentDto(
        incident.Id,
        incident.ProjectId,
        incident.Title,
        incident.Description,
        incident.Severity.ToString(),
        incident.Status.ToString(),
        incident.ReportedBy,
        incident.ReportedAt,
        incident.AssignedTo,
        incident.ResolvedAt
    );

    return Results.Created($"/api/projects/{projectId}/incidents/{incident.Id}", incidentDto);
})
.RequireAuthorization()
.WithName("CreateIncident")
.WithTags("Incidents")
.Accepts<CreateIncidentRequest>("application/json")
.Produces<IncidentDto>(201)
.Produces(400)
.Produces(404);

app.MapPatch("/api/projects/{projectId:guid}/incidents/{incidentId:guid}", async (
    Guid projectId,
    Guid incidentId,
    UpdateIncidentRequest request,
    ProjectsDbContext context,
    ClaimsPrincipal user,
    IValidator<UpdateIncidentRequest> validator) =>
{
    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
        return Results.BadRequest(validationResult.ToDictionary());

    var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    
    // Verify project ownership
    var project = await context.Projects
        .AsNoTracking()
        .FirstOrDefaultAsync(p => p.Id == projectId && p.OwnerUserId == userId);
    
    if (project == null)
        return Results.NotFound("Project not found or access denied");

    var incident = await context.Incidents
        .FirstOrDefaultAsync(i => i.Id == incidentId && i.ProjectId == projectId);
    
    if (incident == null)
        return Results.NotFound("Incident not found");

    // Update fields
    if (!string.IsNullOrEmpty(request.Title))
        incident.Title = request.Title;
    
    if (!string.IsNullOrEmpty(request.Description))
        incident.Description = request.Description;
    
    if (!string.IsNullOrEmpty(request.Severity))
        incident.Severity = Enum.Parse<IncidentSeverity>(request.Severity);
    
    if (request.AssignedTo.HasValue)
        incident.AssignedTo = request.AssignedTo;
    
    if (!string.IsNullOrEmpty(request.Status))
    {
        var newStatus = Enum.Parse<IncidentStatus>(request.Status);
        incident.Status = newStatus;
        
        if (newStatus == IncidentStatus.RESOLVED)
            incident.ResolvedAt = DateTime.UtcNow;
        else if (newStatus == IncidentStatus.OPEN || newStatus == IncidentStatus.IN_PROGRESS)
            incident.ResolvedAt = null;
    }

    await context.SaveChangesAsync();

    var incidentDto = new IncidentDto(
        incident.Id,
        incident.ProjectId,
        incident.Title,
        incident.Description,
        incident.Severity.ToString(),
        incident.Status.ToString(),
        incident.ReportedBy,
        incident.ReportedAt,
        incident.AssignedTo,
        incident.ResolvedAt
    );

    return Results.Ok(incidentDto);
})
.RequireAuthorization()
.WithName("UpdateIncident")
.WithTags("Incidents")
.Accepts<UpdateIncidentRequest>("application/json")
.Produces<IncidentDto>(200)
.Produces(400)
.Produces(404);

// ===== MACHINERY ENDPOINTS =====

app.MapGet("/api/projects/{projectId:guid}/machinery", async (
    Guid projectId,
    ProjectsDbContext context,
    ClaimsPrincipal user) =>
{
    var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    
    // Verify project ownership
    var project = await context.Projects
        .AsNoTracking()
        .FirstOrDefaultAsync(p => p.Id == projectId && p.OwnerUserId == userId);
    
    if (project == null)
        return Results.NotFound("Project not found or access denied");

    var machines = await context.Machines
        .AsNoTracking()
        .Where(m => m.ProjectId == projectId)
        .Select(m => new MachineDto(
            m.Id,
            m.ProjectId,
            m.Name,
            m.SerialNumber,
            m.Model,
            m.Status.ToString(),
            m.LastMaintenanceDate,
            m.NextMaintenanceDate,
            m.HourlyRate
        ))
        .ToListAsync();

    return Results.Ok(machines);
})
.RequireAuthorization()
.WithName("GetProjectMachinery")
.WithTags("Machinery")
.Produces<List<MachineDto>>(200)
.Produces(404);

app.MapPost("/api/projects/{projectId:guid}/machinery", async (
    Guid projectId,
    CreateMachineRequest request,
    ProjectsDbContext context,
    ClaimsPrincipal user,
    IValidator<CreateMachineRequest> validator) =>
{
    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
        return Results.BadRequest(validationResult.ToDictionary());

    var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    
    // Verify project ownership
    var project = await context.Projects
        .AsNoTracking()
        .FirstOrDefaultAsync(p => p.Id == projectId && p.OwnerUserId == userId);
    
    if (project == null)
        return Results.NotFound("Project not found or access denied");

    // Check if serial number already exists
    var existingMachine = await context.Machines
        .AsNoTracking()
        .FirstOrDefaultAsync(m => m.SerialNumber == request.SerialNumber);
    
    if (existingMachine != null)
        return Results.Conflict("A machine with this serial number already exists");

    var machine = new Machine
    {
        ProjectId = projectId,
        Name = request.Name,
        SerialNumber = request.SerialNumber,
        Model = request.Model ?? string.Empty,
        Status = Enum.Parse<MachineStatus>(request.Status),
        HourlyRate = request.HourlyRate
    };

    context.Machines.Add(machine);
    await context.SaveChangesAsync();

    var machineDto = new MachineDto(
        machine.Id,
        machine.ProjectId,
        machine.Name,
        machine.SerialNumber,
        machine.Model,
        machine.Status.ToString(),
        machine.LastMaintenanceDate,
        machine.NextMaintenanceDate,
        machine.HourlyRate
    );

    return Results.Created($"/api/projects/{projectId}/machinery/{machine.Id}", machineDto);
})
.RequireAuthorization()
.WithName("CreateMachine")
.WithTags("Machinery")
.Accepts<CreateMachineRequest>("application/json")
.Produces<MachineDto>(201)
.Produces(400)
.Produces(404)
.Produces(409);

app.MapPatch("/api/projects/{projectId:guid}/machinery/{machineId:guid}", async (
    Guid projectId,
    Guid machineId,
    UpdateMachineRequest request,
    ProjectsDbContext context,
    ClaimsPrincipal user,
    IValidator<UpdateMachineRequest> validator) =>
{
    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
        return Results.BadRequest(validationResult.ToDictionary());

    var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    
    // Verify project ownership
    var project = await context.Projects
        .AsNoTracking()
        .FirstOrDefaultAsync(p => p.Id == projectId && p.OwnerUserId == userId);
    
    if (project == null)
        return Results.NotFound("Project not found or access denied");

    var machine = await context.Machines
        .FirstOrDefaultAsync(m => m.Id == machineId && m.ProjectId == projectId);
    
    if (machine == null)
        return Results.NotFound("Machine not found");

    // Update fields
    if (!string.IsNullOrEmpty(request.Name))
        machine.Name = request.Name;
    
    if (!string.IsNullOrEmpty(request.Model))
        machine.Model = request.Model;
    
    if (!string.IsNullOrEmpty(request.Status))
        machine.Status = Enum.Parse<MachineStatus>(request.Status);
    
    if (request.LastMaintenanceDate.HasValue)
        machine.LastMaintenanceDate = request.LastMaintenanceDate;
    
    if (request.NextMaintenanceDate.HasValue)
        machine.NextMaintenanceDate = request.NextMaintenanceDate;
    
    if (request.HourlyRate.HasValue)
        machine.HourlyRate = request.HourlyRate;

    await context.SaveChangesAsync();

    var machineDto = new MachineDto(
        machine.Id,
        machine.ProjectId,
        machine.Name,
        machine.SerialNumber,
        machine.Model,
        machine.Status.ToString(),
        machine.LastMaintenanceDate,
        machine.NextMaintenanceDate,
        machine.HourlyRate
    );

    return Results.Ok(machineDto);
})
.RequireAuthorization()
.WithName("UpdateMachine")
.WithTags("Machinery")
.Accepts<UpdateMachineRequest>("application/json")
.Produces<MachineDto>(200)
.Produces(400)
.Produces(404);

app.Run();

// DTO for Stored Procedure Result
public class SpTransactionResult
{
    public string Result { get; set; } = string.Empty;
    public decimal NewStock { get; set; }
    public decimal PreviousStock { get; set; }
    public decimal TransactionQuantity { get; set; }
    public string TransactionType { get; set; } = string.Empty;
}
