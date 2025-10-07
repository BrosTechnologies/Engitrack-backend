using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Engitrack.Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialInventoryCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "inventory");

            migrationBuilder.CreateTable(
                name: "Materials",
                schema: "inventory",
                columns: table => new
                {
                    MaterialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Stock = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false, defaultValue: 0m),
                    MinNivel = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Materials", x => x.MaterialId);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                schema: "inventory",
                columns: table => new
                {
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Ruc = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.SupplierId);
                });

            migrationBuilder.CreateTable(
                name: "InventoryTransactions",
                schema: "inventory",
                columns: table => new
                {
                    TxId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    MaterialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TxType = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    TxDate = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryTransactions", x => x.TxId);
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalSchema: "inventory",
                        principalTable: "Materials",
                        principalColumn: "MaterialId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "inventory",
                        principalTable: "Suppliers",
                        principalColumn: "SupplierId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_MaterialId",
                schema: "inventory",
                table: "InventoryTransactions",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_MaterialId_TxDate",
                schema: "inventory",
                table: "InventoryTransactions",
                columns: new[] { "MaterialId", "TxDate" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_SupplierId",
                schema: "inventory",
                table: "InventoryTransactions",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_TxDate",
                schema: "inventory",
                table: "InventoryTransactions",
                column: "TxDate");

            migrationBuilder.CreateIndex(
                name: "IX_Materials_ProjectId",
                schema: "inventory",
                table: "Materials",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Materials_ProjectId_Name_Unique",
                schema: "inventory",
                table: "Materials",
                columns: new[] { "ProjectId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Materials_ProjectId_Status",
                schema: "inventory",
                table: "Materials",
                columns: new[] { "ProjectId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Materials_Status",
                schema: "inventory",
                table: "Materials",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Name",
                schema: "inventory",
                table: "Suppliers",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Ruc",
                schema: "inventory",
                table: "Suppliers",
                column: "Ruc",
                unique: true,
                filter: "[Ruc] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryTransactions",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "Materials",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "Suppliers",
                schema: "inventory");
        }
    }
}
