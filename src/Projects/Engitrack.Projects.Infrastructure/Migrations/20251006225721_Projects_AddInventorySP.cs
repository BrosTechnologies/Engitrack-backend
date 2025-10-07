using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Engitrack.Projects.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Projects_AddInventorySP : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE inventory.usp_RegisterTransaction
  @MaterialId uniqueidentifier,
  @TxType nvarchar(16),       -- ENTRY|USAGE|ADJUSTMENT
  @Quantity decimal(18,3),
  @SupplierId uniqueidentifier = NULL,
  @Notes nvarchar(400) = NULL,
  @ActorUserId uniqueidentifier
AS
BEGIN
  SET NOCOUNT ON;
  IF (@Quantity <= 0) THROW 51000, 'Quantity must be > 0', 1;

  DECLARE @ProjectId uniqueidentifier, @Owner uniqueidentifier;
  SELECT @ProjectId = m.ProjectId FROM inventory.Materials m WHERE m.MaterialId=@MaterialId;
  IF @ProjectId IS NULL THROW 51002, 'Material no existe', 1;

  SELECT @Owner = p.OwnerUserId FROM projects.Projects p WHERE p.ProjectId=@ProjectId;
  IF @Owner IS NULL OR @Owner <> @ActorUserId THROW 51003, 'Usuario sin permisos (no es Owner)', 1;

  BEGIN TRAN;
    INSERT INTO inventory.Transactions (TxId, MaterialId, TxType, Quantity, SupplierId, Notes, TxDate)
    VALUES (NEWID(), @MaterialId, @TxType, @Quantity, @SupplierId, @Notes, SYSUTCDATETIME());

    UPDATE m SET m.Stock = m.Stock + CASE @TxType WHEN 'ENTRY' THEN @Quantity WHEN 'USAGE' THEN -@Quantity ELSE @Quantity END
    FROM inventory.Materials m WHERE m.MaterialId=@MaterialId;

    IF EXISTS (SELECT 1 FROM inventory.Materials WHERE MaterialId=@MaterialId AND Stock < 0)
    BEGIN ROLLBACK TRAN; THROW 51001, 'Stock no puede ser negativo', 1; END
  COMMIT TRAN;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS inventory.usp_RegisterTransaction");
        }
    }
}
