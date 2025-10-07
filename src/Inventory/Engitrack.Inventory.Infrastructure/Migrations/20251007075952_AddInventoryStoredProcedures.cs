using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Engitrack.Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryStoredProcedures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE PROCEDURE [inventory].[usp_RegisterTransaction]
                    @MaterialId UNIQUEIDENTIFIER,
                    @ProjectId UNIQUEIDENTIFIER,
                    @TransactionType NVARCHAR(50),
                    @Quantity DECIMAL(18,3),
                    @Reference NVARCHAR(255) = NULL,
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
                        
                        -- Validate stock for withdrawals
                        IF @TransactionType = 'Withdrawal' AND @CurrentStock < @Quantity
                        BEGIN
                            RAISERROR('Insufficient stock. Current: %s, Requested: %s', 16, 1, 
                                CAST(@CurrentStock AS NVARCHAR), CAST(@Quantity AS NVARCHAR));
                            RETURN;
                        END
                        
                        -- Calculate new stock
                        DECLARE @NewStock DECIMAL(18,3);
                        IF @TransactionType = 'Addition'
                            SET @NewStock = @CurrentStock + @Quantity;
                        ELSE IF @TransactionType = 'Withdrawal'
                            SET @NewStock = @CurrentStock - @Quantity;
                        ELSE IF @TransactionType = 'Adjustment'
                            SET @NewStock = @Quantity;
                        ELSE
                        BEGIN
                            RAISERROR('Invalid transaction type', 16, 1);
                            RETURN;
                        END
                        
                        -- Validate new stock >= 0
                        IF @NewStock < 0
                        BEGIN
                            RAISERROR('Stock cannot be negative', 16, 1);
                            RETURN;
                        END
                        
                        -- Insert transaction record
                        INSERT INTO [inventory].[InventoryTransactions] 
                        (Id, MaterialId, TransactionType, Quantity, PreviousStock, NewStock, Reference, CreatedAt)
                        VALUES 
                        (NEWID(), @MaterialId, @TransactionType, @Quantity, @CurrentStock, @NewStock, @Reference, GETUTCDATE());
                        
                        -- Update material stock
                        UPDATE [inventory].[Materials] 
                        SET Stock = @NewStock, UpdatedAt = GETUTCDATE()
                        WHERE Id = @MaterialId;
                        
                        COMMIT TRANSACTION;
                        
                        -- Return success result
                        SELECT 
                            'SUCCESS' as Result,
                            @NewStock as NewStock,
                            @CurrentStock as PreviousStock;
                            
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
