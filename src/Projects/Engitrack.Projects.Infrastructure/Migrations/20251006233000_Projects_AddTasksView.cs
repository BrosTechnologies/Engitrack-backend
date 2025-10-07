using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Engitrack.Projects.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Projects_AddTasksView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE VIEW projects.vw_TasksWithUsers AS
SELECT 
    pt.TaskId,
    pt.TaskName,
    pt.Description as TaskDescription,
    pt.Status as TaskStatus,
    pt.Priority,
    pt.DueDate,
    pt.CreatedAt as TaskCreatedAt,
    pt.ProjectId,
    p.ProjectName,
    p.Status as ProjectStatus,
    pt.AssignedToUserId,
    u.Name as AssignedUserName,
    u.Email as AssignedUserEmail,
    u.Role as AssignedUserRole,
    p.OwnerUserId,
    owner.Name as OwnerName,
    owner.Email as OwnerEmail
FROM projects.ProjectTasks pt
    INNER JOIN projects.Projects p ON pt.ProjectId = p.ProjectId
    LEFT JOIN projects.Users u ON pt.AssignedToUserId = u.UserId
    LEFT JOIN projects.Users owner ON p.OwnerUserId = owner.UserId;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS projects.vw_TasksWithUsers");
        }
    }
}