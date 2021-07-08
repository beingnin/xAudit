--CREATE DATE:        2021-06-02
--AUTHOR:             AKSHAYA SAKTHIVEL

CREATE PROCEDURE XAUDIT.INSERT_NEW_VERSION 
(
	@VERSION VARCHAR(20),
	@MACHINE VARCHAR(100),
	@MAJOR INT,
	@MINOR INT,
	@PATCH INT,
	@PROCESSID INT,
	@TOTALTABLES INT,
	@TRACKSCHEMACHANGES BIT,
	@ENABLEPARTITION BIT,
	@KEEPVERSIONSFORPARTITION BIT
) AS
BEGIN			
			UPDATE XAUDIT.META SET [ISCURRENTVERSION]=0;

			INSERT INTO XAUDIT.META
			VALUES 
			(
				@VERSION,
				@MACHINE,
				@MAJOR,
				@MINOR,
				@PATCH,
				@PROCESSID,
				@TOTALTABLES,
				1,
				GETUTCDATE(),
				@TRACKSCHEMACHANGES,
				@ENABLEPARTITION,
				@KEEPVERSIONSFORPARTITION
				
			);
END
GO

CREATE PROCEDURE XAUDIT.FIND_CURRENT_VERSION AS
BEGIN
	SELECT TOP 1 [VERSION] FROM XAUDIT.META WHERE [ISCURRENTVERSION]=1 ORDER BY [INSTALLEDDATEUTC] DESC;
END
GO

CREATE PROCEDURE XAUDIT.ENABLE_CDC_ON_DB
AS
BEGIN
	IF NOT EXISTS (SELECT 1 FROM SYS.DATABASES WHERE IS_CDC_ENABLED=1 AND [NAME] =(SELECT DB_NAME()) )
	BEGIN
		EXEC SYS.SP_CDC_ENABLE_DB
	END
END
GO

CREATE PROCEDURE XAUDIT.ENABLE_CDC_ON_DB_RECREATE
AS
BEGIN
	IF NOT EXISTS (SELECT * FROM SYS.DATABASES WHERE IS_CDC_ENABLED=1 AND [NAME] = (SELECT DB_NAME()) )
	BEGIN
		EXEC SYS.SP_CDC_ENABLE_DB
	END
	ELSE BEGIN
		EXEC SYS.SP_CDC_DISABLE_DB
		EXEC SYS.SP_CDC_ENABLE_DB
	END
END
GO

CREATE PROCEDURE XAUDIT.GET_TRACKED_TABLES AS
BEGIN	
	SELECT  
	-- C.TABLE_SCHEMA
	--,C.TABLE_NAME
	 TBL.CAPTURE_INSTANCE
	,C.COLUMN_NAME
	,C.DATA_TYPE
	,C.CHARACTER_MAXIMUM_LENGTH
	,C.CHARACTER_OCTET_LENGTH
	,C.NUMERIC_PRECISION
	,C.NUMERIC_SCALE
	,C.DATETIME_PRECISION
	,C.COLLATION_NAME INTO #TCDC FROM (
	SELECT [NAME],SCHEMA_NAME([SCHEMA_ID])[SCHEMA],CT.CAPTURE_INSTANCE FROM SYS.TABLES T 
	JOIN [CDC].[CHANGE_TABLES] CT ON CT.[OBJECT_ID] = T.[OBJECT_ID] WHERE CAPTURE_INSTANCE LIKE 'xAudit_%') TBL
	JOIN INFORMATION_SCHEMA.COLUMNS C ON  TBL.[NAME] = C.TABLE_NAME AND TBL.[SCHEMA]=C.TABLE_SCHEMA
	WHERE C.COLUMN_NAME NOT LIKE '__$%'	--to skip columns maintained by cdc 

	
	;WITH TSOURCE AS 
	(
		SELECT 
		-- C.TABLE_SCHEMA
		--,C.TABLE_NAME
		 TBL.CAPTURE_INSTANCE
		,C.COLUMN_NAME
		,C.DATA_TYPE
		,C.CHARACTER_MAXIMUM_LENGTH
		,C.CHARACTER_OCTET_LENGTH
		,C.NUMERIC_PRECISION
		,C.NUMERIC_SCALE
		,C.DATETIME_PRECISION
		,C.COLLATION_NAME
		FROM (
		SELECT [NAME],SCHEMA_NAME([SCHEMA_ID])[SCHEMA],CT.CAPTURE_INSTANCE FROM SYS.TABLES T 
		JOIN [CDC].[CHANGE_TABLES] CT ON CT.SOURCE_OBJECT_ID = T.[OBJECT_ID] WHERE CAPTURE_INSTANCE LIKE 'xAudit_%') TBL
		JOIN INFORMATION_SCHEMA.COLUMNS C ON  TBL.[NAME] = C.TABLE_NAME AND TBL.[SCHEMA]=C.TABLE_SCHEMA
	),
	COMMON AS
	(
		SELECT * FROM TSOURCE
		INTERSECT 
		SELECT * FROM #TCDC
	),
	DIFFERENCES AS
	(
		SELECT *,'+' [CHANGE] FROM (SELECT * FROM TSOURCE EXCEPT SELECT * FROM COMMON) A
		UNION ALL
		SELECT *,'-' [CHANGE] FROM (SELECT * FROM #TCDC EXCEPT SELECT * FROM COMMON) B
	)
	SELECT * FROM DIFFERENCES
	SELECT DISTINCT CAPTURE_INSTANCE FROM #TCDC
END
GO

CREATE TYPE xAudit.UDT_TABLES AS TABLE
(
	[SL] INT IDENTITY(1,1) PRIMARY KEY NOT NULL,
	[SCHEMA] NVARCHAR(500) NOT NULL ,
	[TABLE] NVARCHAR(500) NOT NULL
)
GO

CREATE PROCEDURE XAUDIT.ENABLE_TABLES 
(
	@TABLES XAUDIT.UDT_TABLES READONLY,
	@INSTANCEPREFIX NVARCHAR(500)
)AS
BEGIN	
	DECLARE @CURSOR INT=(SELECT COUNT(0) FROM @TABLES)
	DECLARE @SCHEMA_NAME NVARCHAR(500)
	DECLARE @TABLE_NAME NVARCHAR(500)
	DECLARE @INSTANCE_NAME NVARCHAR(500)
	WHILE @CURSOR > 0
	BEGIN
		SET @SCHEMA_NAME =(SELECT [SCHEMA] FROM @TABLES WHERE SL = @CURSOR)
		SET @TABLE_NAME = (SELECT [TABLE] FROM @TABLES WHERE SL = @CURSOR)
		SET @INSTANCE_NAME =@INSTANCEPREFIX+'_'+@SCHEMA_NAME+'_'+@TABLE_NAME
	
		IF NOT EXISTS(SELECT 1 FROM CDC.CHANGE_TABLES WHERE CAPTURE_INSTANCE=@INSTANCE_NAME)
		BEGIN
		EXEC SYS.SP_CDC_ENABLE_TABLE
			@SOURCE_SCHEMA = @SCHEMA_NAME,
			@SOURCE_NAME   = @TABLE_NAME,
			@ROLE_NAME     = NULL,
			@CAPTURE_INSTANCE = @INSTANCE_NAME
		END
	END
END
GO

CREATE PROCEDURE XAUDIT.DISABLE_TABLES 
(
	@TABLES XAUDIT.UDT_TABLES READONLY,
	@INSTANCEPREFIX NVARCHAR(500)
)AS
BEGIN	
	DECLARE @CURSOR INT=(SELECT COUNT(0) FROM @TABLES)
	DECLARE @SCHEMA_NAME NVARCHAR(500)
	DECLARE @TABLE_NAME NVARCHAR(500)
	DECLARE @INSTANCE_NAME NVARCHAR(500)
	WHILE @CURSOR > 0
	BEGIN
		SET @SCHEMA_NAME =(SELECT [SCHEMA] FROM @TABLES WHERE SL = @CURSOR)
		SET @TABLE_NAME = (SELECT [TABLE] FROM @TABLES WHERE SL = @CURSOR)
		SET @INSTANCE_NAME =@INSTANCEPREFIX+'_'+@SCHEMA_NAME+'_'+@TABLE_NAME
	
		IF EXISTS(SELECT 1 FROM CDC.CHANGE_TABLES WHERE CAPTURE_INSTANCE=@INSTANCE_NAME)
		BEGIN
		EXEC SYS.SP_CDC_DISABLE_TABLE
			@SOURCE_SCHEMA = @SCHEMA_NAME,
			@SOURCE_NAME   = @TABLE_NAME,
			@CAPTURE_INSTANCE = @INSTANCE_NAME
		END
	END
END
GO

CREATE PROCEDURE XAUDIT.REENABLE_TABLES 
(
	@TABLES XAUDIT.UDT_TABLES READONLY,
	@INSTANCEPREFIX NVARCHAR(500)
)AS
BEGIN	
	DECLARE @CURSOR INT=(SELECT COUNT(0) FROM @TABLES)
	DECLARE @SCHEMA_NAME NVARCHAR(500)
	DECLARE @TABLE_NAME NVARCHAR(500)
	DECLARE @INSTANCE_NAME NVARCHAR(500)
	WHILE @CURSOR > 0
	BEGIN
		SET @SCHEMA_NAME =(SELECT [SCHEMA] FROM @TABLES WHERE SL = @CURSOR)
		SET @TABLE_NAME = (SELECT [TABLE] FROM @TABLES WHERE SL = @CURSOR)
		SET @INSTANCE_NAME =@INSTANCEPREFIX+'_'+@SCHEMA_NAME+'_'+@TABLE_NAME

		IF EXISTS(SELECT 1 FROM CDC.CHANGE_TABLES WHERE CAPTURE_INSTANCE=@INSTANCE_NAME)
		BEGIN
		EXEC SYS.SP_CDC_DISABLE_TABLE
			@SOURCE_SCHEMA = @SCHEMA_NAME,
			@SOURCE_NAME   = @TABLE_NAME,
			@CAPTURE_INSTANCE = @INSTANCE_NAME
		EXEC SYS.SP_CDC_ENABLE_TABLE
			@SOURCE_SCHEMA = @SCHEMA_NAME,
			@SOURCE_NAME   = @TABLE_NAME,
			@ROLE_NAME     = NULL,
			@CAPTURE_INSTANCE = @INSTANCE_NAME
		END
	END
END
GO