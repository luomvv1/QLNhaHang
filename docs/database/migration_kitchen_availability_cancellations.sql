SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF COL_LENGTH('dbo.FoodVariants', 'IsSoldOut') IS NULL
    ALTER TABLE dbo.FoodVariants ADD IsSoldOut bit NOT NULL CONSTRAINT DF_FoodVariants_IsSoldOut DEFAULT (0);
IF COL_LENGTH('dbo.Orders', 'CancelledBy') IS NULL
    ALTER TABLE dbo.Orders ADD CancelledBy int NULL;
IF COL_LENGTH('dbo.Orders', 'CancellationReason') IS NULL
    ALTER TABLE dbo.Orders ADD CancellationReason nvarchar(500) NULL;
IF COL_LENGTH('dbo.OrderItems', 'CancelledAt') IS NULL
    ALTER TABLE dbo.OrderItems ADD CancelledAt datetime2(0) NULL;
IF COL_LENGTH('dbo.OrderItems', 'CancelledBy') IS NULL
    ALTER TABLE dbo.OrderItems ADD CancelledBy int NULL;
IF COL_LENGTH('dbo.OrderItems', 'CancellationReason') IS NULL
    ALTER TABLE dbo.OrderItems ADD CancellationReason nvarchar(500) NULL;
IF COL_LENGTH('dbo.OrderItems', 'CancelledFromStatus') IS NULL
    ALTER TABLE dbo.OrderItems ADD CancelledFromStatus varchar(20) NULL;
COMMIT TRANSACTION;
