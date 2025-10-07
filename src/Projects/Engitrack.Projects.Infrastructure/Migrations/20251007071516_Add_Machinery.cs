using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Engitrack.Projects.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_Machinery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "machinery");

            migrationBuilder.CreateTable(
                name: "Machines",
                schema: "machinery",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LastMaintenanceDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextMaintenanceDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HourlyRate = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Machines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Machines_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "projects",
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Machines_ProjectId",
                schema: "machinery",
                table: "Machines",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Machines_ProjectId_Status",
                schema: "machinery",
                table: "Machines",
                columns: new[] { "ProjectId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Machines_SerialNumber",
                schema: "machinery",
                table: "Machines",
                column: "SerialNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Machines_Status",
                schema: "machinery",
                table: "Machines",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Machines",
                schema: "machinery");
        }
    }
}
