using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Engitrack.Projects.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Workers_AddNavigationProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_Workers_WorkerId",
                schema: "workers",
                table: "Attendances",
                column: "WorkerId",
                principalSchema: "workers",
                principalTable: "Workers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_Workers_WorkerId",
                schema: "workers",
                table: "Attendances");
        }
    }
}
