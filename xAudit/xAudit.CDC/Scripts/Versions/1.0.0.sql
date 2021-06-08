




--Procedure:          xAudit.Insert_New_Version 
--Create Date:        2021-06-02
--Author:             Akshaya Sakthivel
--Description:        This procedure inserts the newer of the already existing application.

CREATE PROCEDURE xAudit.Insert_New_Version 
(
	@version VARCHAR(20),
	@machine VARCHAR(100),
	@major INT,
	@minor INT,
	@patch INT,
	@processId INT,
	@totalTables INT
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
				@major,
				@minor,
				@patch,
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

--Procedure:          xAudit.Find_Current_Version
--Create Date:        2021-06-02
--Author:             Akshaya Sakthivel
--Description:        This procedure get the current version of all the installed versions.
CREATE PROCEDURE xAudit.Find_Current_Version AS
BEGIN
	SELECT TOP 1 [Version] FROM xAudit.Meta WHERE [IsCurrentVersion]=1 ORDER BY [CreatedDateUTC] DESC;
END
GO

CREATE PROCEDURE xAudit.Enable_CDC_On_DB
(
	@db VARCHAR(100)
)AS
BEGIN
	IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE is_cdc_enabled=1 and [name] = @db )
	BEGIN
		EXEC sys.sp_cdc_enable_db
	END
END
GO

CREATE PROCEDURE xAudit.Enable_CDC_On_DB_Recreate
(
	@db VARCHAR(100)
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
