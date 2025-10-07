using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Engitrack.Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreateInventoryStoredProcedure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE PROCEDURE [inventory].[usp_RegisterTransaction]
                    @MaterialId UNIQUEIDENTIFIER,
                    @ProjectId UNIQUEIDENTIFIER,
                    @TxType NVARCHAR(16),
                    @Quantity DECIMAL(18,3),
                    @SupplierId UNIQUEIDENTIFIER = NULL,
                    @Notes NVARCHAR(400) = NULL,
                    @OwnerUserId UNIQUEIDENTIFIER
                AS
                BEGIN
                    SET NOCOUNT ON;
                    
                    BEGIN TRY
                        BEGIN TRANSACTION;
                        
                        -- Validate material exists and belongs to user's project
                        IF NOT EXISTS (
                            SELECT 1 FROM [inventory].[Materials] m
                            INNER JOIN [projects].[Projects] p ON m.ProjectId = p.Id
                            WHERE m.Id = @MaterialId 
                            AND m.ProjectId = @ProjectId
                            AND p.OwnerUserId = @OwnerUserId
                            AND m.Status = 'Active'
                        )
                        BEGIN
                            RAISERROR('Material not found or access denied', 16, 1);
                            RETURN;
                        END
                        
                        -- Get current stock
                        DECLARE @CurrentStock DECIMAL(18,3);
                        SELECT @CurrentStock = Stock FROM [inventory].[Materials] WHERE Id = @MaterialId;
                        
                        -- Validate stock for USAGE transactions
                        IF @TxType = 'USAGE' AND @CurrentStock < @Quantity
                        BEGIN
                            DECLARE @ErrorMsg NVARCHAR(200) = 
                                'Insufficient stock. Current: ' + CAST(@CurrentStock AS NVARCHAR(20)) + 
                                ', Requested: ' + CAST(@Quantity AS NVARCHAR(20));
                            RAISERROR(@ErrorMsg, 16, 1);
                            RETURN;
                        END
                        
                        -- Validate TxType
                        IF @TxType NOT IN ('ENTRY', 'USAGE', 'ADJUSTMENT')
                        BEGIN
                            RAISERROR('Invalid transaction type. Must be ENTRY, USAGE, or ADJUSTMENT', 16, 1);
                            RETURN;
                        END
                        
                        -- Calculate new stock based on transaction type
                        DECLARE @NewStock DECIMAL(18,3);
                        IF @TxType = 'ENTRY'
                            SET @NewStock = @CurrentStock + @Quantity;
                        ELSE IF @TxType = 'USAGE'
                            SET @NewStock = @CurrentStock - @Quantity;
                        ELSE IF @TxType = 'ADJUSTMENT'
                            SET @NewStock = @Quantity;  -- Direct stock adjustment
                        
                        -- Validate new stock >= 0
                        IF @NewStock < 0
                        BEGIN
                            RAISERROR('Stock cannot be negative', 16, 1);
                            RETURN;
                        END
                        
                        -- Insert transaction record using correct column names
                        INSERT INTO [inventory].[InventoryTransactions] 
                        (TxId, MaterialId, TxType, Quantity, SupplierId, Notes, TxDate)
                        VALUES 
                        (NEWID(), @MaterialId, @TxType, @Quantity, @SupplierId, @Notes, GETUTCDATE());
                        
                        -- Update material stock
                        UPDATE [inventory].[Materials] 
                        SET Stock = @NewStock, UpdatedAt = GETUTCDATE()
                        WHERE Id = @MaterialId;
                        
                        COMMIT TRANSACTION;
                        
                        -- Return success result
                        SELECT 
                            'SUCCESS' as Result,
                            @NewStock as NewStock,
                            @CurrentStock as PreviousStock,
                            @Quantity as TransactionQuantity,
                            @TxType as TransactionType;
                            
                    END TRY
                    BEGIN CATCH
                        IF @@TRANCOUNT > 0
                            ROLLBACK TRANSACTION;
                            
                        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
                        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
                        DECLARE @ErrorState INT = ERROR_STATE();
                        
                        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
                    END CATCH
                END;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [inventory].[usp_RegisterTransaction];");
        }
    }
}
