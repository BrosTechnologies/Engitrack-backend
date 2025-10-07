using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Engitrack.Projects.Infrastructure.Persistence;
using Engitrack.Projects.Application.Projects.Dtos;
using Engitrack.Projects.Application.Projects.Validators;
using Engitrack.Inventory.Application.Dtos;
using Engitrack.Inventory.Application.Validators;
using Engitrack.Workers.Application.Dtos;
using Engitrack.Workers.Application.Validators;
using Engitrack.Api.Security;
using Engitrack.Api.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Engitrack.Projects.Domain.Entities;
using Engitrack.Projects.Domain.Enums;
using Engitrack.Workers.Domain.Entities;
using BCrypt.Net;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<JwtHelper>();

builder.Services.AddDbContext<ProjectsDbContext>(options =>
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
builder.Services.AddScoped<IValidator<RegisterTransactionRequest>, RegisterTransactionRequestValidator>();
builder.Services.AddScoped<IValidator<CreateWorkerRequest>, CreateWorkerRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateWorkerRequest>, UpdateWorkerRequestValidator>();
builder.Services.AddScoped<IValidator<CreateAssignmentRequest>, CreateAssignmentRequestValidator>();
builder.Services.AddScoped<IValidator<CreateAttendanceRequest>, CreateAttendanceRequestValidator>();

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
app.UseAuthentication();
app.UseAuthorization();

// Basic endpoints
app.MapGet("/api/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }))
   .WithName("HealthCheck")
   .WithTags("System");

// Projects endpoints (authenticated)
app.MapGet("/api/projects", async (ProjectsDbContext context, ICurrentUser currentUser, string? status = null, string? q = null, int page = 1, int pageSize = 10) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    var query = context.Projects
        .AsNoTracking()
        .Include(p => p.Tasks)
        .Where(p => p.OwnerUserId == currentUser.Id);

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
        p.OwnerUserId,
        p.Tasks.Select(t => new ProjectTaskDto(t.Id, t.Title, t.Status.ToString(), t.DueDate))
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

    if (project.OwnerUserId != currentUser.Id)
        return Results.Forbid();

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
    var project = new Project(request.Name, request.StartDate, owner, request.Budget);

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
        project.OwnerUserId,
        project.Tasks.Select(t => new ProjectTaskDto(t.Id, t.Title, t.Status.ToString(), t.DueDate))
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

    if (project.OwnerUserId != currentUser.Id)
        return Results.Forbid();

    // Create task directly without loading the project into context
    var task = new ProjectTask(id, request.Title, request.DueDate);
    context.ProjectTasks.Add(task);
    
    try
    {
        await context.SaveChangesAsync();
        var response = new ProjectTaskDto(task.Id, task.Title, task.Status.ToString(), task.DueDate);
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

    if (project.OwnerUserId != currentUser.Id)
        return Results.Forbid();

    var task = project.Tasks.FirstOrDefault(t => t.Id == taskId);
    if (task == null)
        return Results.NotFound("Task not found");

    // Update task status using domain method
    if (Enum.TryParse<Engitrack.Projects.Domain.Enums.TaskStatus>(request.Status, out var status))
    {
        task.UpdateStatus(status);
        await context.SaveChangesAsync();
        
        var response = new ProjectTaskDto(task.Id, task.Title, task.Status.ToString(), task.DueDate);
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

    if (project.OwnerUserId != currentUser.Id)
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

app.MapPatch("/api/projects/{id:guid}/complete", async (Guid id, ProjectsDbContext context, ICurrentUser currentUser) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    var project = await context.Projects
        .Include(p => p.Tasks)
        .FirstOrDefaultAsync(p => p.Id == id);

    if (project == null)
        return Results.NotFound();

    if (project.OwnerUserId != currentUser.Id)
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
            project.OwnerUserId,
            project.Tasks.Select(t => new ProjectTaskDto(t.Id, t.Title, t.Status.ToString(), t.DueDate))
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

    if (project.OwnerUserId != currentUser.Id)
        return Results.Forbid();

    // Validate EndDate >= StartDate
    if (request.EndDate.HasValue && request.EndDate < project.StartDate)
        return Results.BadRequest(new { error = "EndDate must be >= StartDate" });

    // Update project
    project.UpdateDetails(request.Name, request.Budget);
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
        project.OwnerUserId,
        project.Tasks.Select(t => new ProjectTaskDto(t.Id, t.Title, t.Status.ToString(), t.DueDate))
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

// Inventory endpoints
app.MapPost("/api/inventory/transactions", async (RegisterTransactionRequest request, IValidator<RegisterTransactionRequest> validator, ProjectsDbContext context, ICurrentUser currentUser) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    // Validate request
    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
        return Results.BadRequest(validationResult.Errors.Select(e => new { Property = e.PropertyName, Error = e.ErrorMessage }));

    try
    {
        // Call stored procedure
        var parameters = new[]
        {
            new SqlParameter("@MaterialId", request.MaterialId),
            new SqlParameter("@TxType", request.TxType),
            new SqlParameter("@Quantity", request.Quantity),
            new SqlParameter("@SupplierId", (object?)request.SupplierId ?? DBNull.Value),
            new SqlParameter("@Notes", (object?)request.Notes ?? DBNull.Value),
            new SqlParameter("@ActorUserId", currentUser.Id)
        };

        await context.Database.ExecuteSqlRawAsync(
            "EXEC inventory.usp_RegisterTransaction @MaterialId, @TxType, @Quantity, @SupplierId, @Notes, @ActorUserId",
            parameters);

        return Results.Ok(new { message = "Transaction registered successfully" });
    }
    catch (SqlException ex) when (ex.Number == 51000 || ex.Number == 51001 || ex.Number == 51002 || ex.Number == 51003)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error registering transaction: {ex.Message}");
    }
})
.RequireAuthorization()
.WithName("RegisterTransaction")
.WithTags("Inventory")
.Accepts<RegisterTransactionRequest>("application/json")
.Produces(200)
.Produces(400)
.Produces(401)
.Produces(500);

// Workers endpoints
app.MapGet("/api/workers", async (ProjectsDbContext context, ICurrentUser currentUser, int page = 1, int pageSize = 10) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    var query = context.Workers
        .AsNoTracking()
        .Include(w => w.Assignments)
        .Where(w => w.Assignments.Any(a => context.Projects.Any(p => p.Id == a.ProjectId && p.OwnerUserId == currentUser.Id)));

    var workers = await query
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

    // Check if user owns any project this worker is assigned to
    var hasAccess = await context.Assignments
        .AnyAsync(a => a.WorkerId == id && 
                      context.Projects.Any(p => p.Id == a.ProjectId && p.OwnerUserId == currentUser.Id));

    if (!hasAccess)
        return Results.Forbid();

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
.Produces(404)
.Produces(403);

app.MapPost("/api/workers", async (CreateWorkerRequest request, IValidator<CreateWorkerRequest> validator, ProjectsDbContext context, ICurrentUser currentUser) =>
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

    try
    {
        var worker = new Worker(request.FullName, request.DocumentNumber, request.Phone, request.Position, request.HourlyRate);
        context.Workers.Add(worker);
        await context.SaveChangesAsync();

        // Create assignment
        var assignment = new Assignment(worker.Id, request.ProjectId, DateOnly.FromDateTime(DateTime.Today));
        context.Assignments.Add(assignment);
        await context.SaveChangesAsync();

        var response = new WorkerResponse(
            worker.Id,
            worker.FullName,
            worker.DocumentNumber,
            worker.Phone,
            worker.Position,
            worker.HourlyRate,
            new[] { new AssignmentDto(assignment.Id, assignment.WorkerId, assignment.ProjectId, assignment.StartDate, assignment.EndDate) }
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

    // Check if user owns any project this worker is assigned to
    var hasAccess = await context.Assignments
        .AnyAsync(a => a.WorkerId == id && 
                      context.Projects.Any(p => p.Id == a.ProjectId && p.OwnerUserId == currentUser.Id));

    if (!hasAccess)
        return Results.Forbid();

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
.Produces(404)
.Produces(403);

app.MapDelete("/api/workers/{id:guid}", async (Guid id, ProjectsDbContext context, ICurrentUser currentUser) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    var worker = await context.Workers.FirstOrDefaultAsync(w => w.Id == id);
    if (worker == null)
        return Results.NotFound();

    // Check if user owns any project this worker is assigned to
    var hasAccess = await context.Assignments
        .AnyAsync(a => a.WorkerId == id && 
                      context.Projects.Any(p => p.Id == a.ProjectId && p.OwnerUserId == currentUser.Id));

    if (!hasAccess)
        return Results.Forbid();

    context.Workers.Remove(worker);
    await context.SaveChangesAsync();

    return Results.NoContent();
})
.RequireAuthorization()
.WithName("DeleteWorker")
.WithTags("Workers")
.Produces(204)
.Produces(404)
.Produces(403);

// Auth endpoints
app.MapPost("/auth/register", async (RegisterRequest request, ProjectsDbContext context, JwtHelper jwtHelper) =>
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
    var user = new User(request.Email, request.FullName, "000-000-0000", role, passwordHash);
    context.Users.Add(user);
    await context.SaveChangesAsync();

    // Generate token
    var token = jwtHelper.GenerateToken(user.Id, user.Email, user.Role.ToString());

    return Results.Ok(new AuthResponse(user.Id, user.Email, user.Role.ToString(), token));
})
.WithName("Register")
.WithTags("Auth")
.Accepts<RegisterRequest>("application/json")
.Produces<AuthResponse>(200)
.Produces(400);

app.MapPost("/auth/login", async (LoginRequest request, ProjectsDbContext context, JwtHelper jwtHelper) =>
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
})
.WithName("Login")
.WithTags("Auth")
.Accepts<LoginRequest>("application/json")
.Produces<AuthResponse>(200)
.Produces(401);

app.Run();
