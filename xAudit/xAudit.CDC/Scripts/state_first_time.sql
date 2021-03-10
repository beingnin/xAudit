
--	Begin tran 
--	commit
--	rollback


IF NOT EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.SCHEMATA WHERE [SCHEMA_NAME]='xAudit')
BEGIN
	EXEC('CREATE SCHEMA xAudit');
END
GO

IF NOT EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA='xAudit' AND TABLE_NAME='Meta')
BEGIN
	
	CREATE TABLE xAudit.Meta
	(
		[Version] VARCHAR(20) NOT NULL,
		[Machine] VARCHAR(100) NOT NULL,
		[ProcessID] INT NOT NULL,
		[TotalTables] INT DEFAULT 0,
		[IsCurrentVersion] BIT DEFAULT 0,
		[CreatedDateUTC] DATETIME NOT NULL,
	);
END;
GO

CREATE PROCEDURE xAudit.Insert_New_Version 
(
	@version varchar(20),
	@machine varchar(100),
	@processId int,
	@totalTables int
) AS
BEGIN
	BEGIN TRAN
		BEGIN TRY
			
			UPDATE xAudit.Meta SET [IsCurrentVersion]=0;

			INSERT INTO xAudit.Meta
			VALUES 
			(
				@version,
				@machine,
				@processId,
				@totalTables,
				1,
				GETUTCDATE()
			);
			COMMIT
		END TRY
		BEGIN CATCH
			ROLLBACK
		END CATCH
END
GO

CREATE PROCEDURE xAudit.Find_Current_Version AS
BEGIN
	SELECT TOP 1 [Version] FROM xAudit.Meta WHERE [IsCurrentVersion]=1 ORDER BY [CreatedDateUTC] DESC;
END
GO

CREATE PROCEDURE xAudit.Enable_CDC_On_DB
(
	@db varchar(100)
)AS
BEGIN
	IF NOT EXISTS (SELECT * FROM sys.databases WHERE is_cdc_enabled=1 and [name] = @db )
	BEGIN
		EXEC sys.sp_cdc_enable_db
	END
END
GO

CREATE PROCEDURE xAudit.Enable_CDC_On_DB_Recreate
(
	@db varchar(100)
)AS
BEGIN
	IF NOT EXISTS (SELECT * FROM sys.databases WHERE is_cdc_enabled=1 and [name] = @db )
	BEGIN
		EXEC sys.sp_cdc_enable_db
	END
	ELSE BEGIN
		EXEC sys.sp_cdc_disable_db
		EXEC sys.sp_cdc_enable_db
	END
END
GO
