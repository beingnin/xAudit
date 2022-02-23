--CREATE DATE:        2021-06-02
--AUTHOR:             AKSHAYA SAKTHIVEL

CREATE PROCEDURE xAudit.INSERT_NEW_VERSION 
(
	@VERSION VARCHAR(20),
	@MACHINE VARCHAR(100),
	@INSTANCENAME VARCHAR(100),
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
			UPDATE xAudit.META SET [ISCURRENTVERSION]=0;

			INSERT INTO xAudit.META
			VALUES 
			(
				@VERSION,
				@MACHINE,
				@INSTANCENAME,
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

CREATE PROCEDURE xAudit.FIND_CURRENT_VERSION AS
BEGIN
	SELECT TOP 1 [VERSION] FROM xAudit.META WHERE [ISCURRENTVERSION]=1 ORDER BY [INSTALLEDDATEUTC] DESC;
END
GO

CREATE PROCEDURE xAudit.ENABLE_CDC_ON_DB
AS
BEGIN
	IF NOT EXISTS (SELECT 1 FROM SYS.DATABASES WHERE IS_CDC_ENABLED=1 AND [NAME] =(SELECT DB_NAME()) )
	BEGIN
		EXEC SYS.SP_CDC_ENABLE_DB;	
		
	END
END
GO

CREATE PROCEDURE xAudit.ENABLE_CDC_ON_DB_RECREATE
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

CREATE PROCEDURE xAudit.GET_TRACKED_TABLES AS
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

CREATE PROCEDURE [xAudit].[GET_TRACKED_TABLES@2] AS
BEGIN
SELECT SOURCE_SCHEMA,SOURCE_TABLE,COPY_SCHEMA,COPY_TABLE INTO #RUNNING FROM [XAUDIT].[META_TABLES]

;WITH SOURCES AS (
	SELECT R.*,MT.[NAME] AS COLUMN_NAME,MT.SYSTEM_TYPE_NAME AS DATATYPE FROM #RUNNING R 
	CROSS APPLY SYS.DM_EXEC_DESCRIBE_FIRST_RESULT_SET('SELECT TOP 1 * FROM ['+R.SOURCE_SCHEMA+'].['+R.SOURCE_TABLE+']',NULL,0) AS MT
),
TARGETS AS (
	SELECT R.*,MT.[NAME] AS COLUMN_NAME,MT.SYSTEM_TYPE_NAME AS DATATYPE FROM #RUNNING R 
	CROSS APPLY SYS.DM_EXEC_DESCRIBE_FIRST_RESULT_SET('SELECT TOP 1 * FROM ['+R.COPY_SCHEMA+'].['+R.COPY_TABLE+']',NULL,0) AS MT WHERE MT.[NAME] NOT LIKE '__$%'
),
COMMON AS 
(
	SELECT * FROM SOURCES
	INTERSECT 
	SELECT * FROM TARGETS
),
DIFFERENCES AS
(
	SELECT A.SOURCE_SCHEMA[ASCHEMA],A.SOURCE_TABLE[ANAME],A.COLUMN_NAME[ACOLUMN],A.DATATYPE[ATYPE],B.SOURCE_SCHEMA[BSCHEMA],B.SOURCE_TABLE[BNAME],B.COLUMN_NAME[BCOLUMN],B.DATATYPE[BTYPE] FROM (SELECT * FROM SOURCES EXCEPT SELECT * FROM COMMON)A 
	FULL JOIN (SELECT * FROM (SELECT * FROM TARGETS EXCEPT SELECT * FROM COMMON) C) B 
	ON A.SOURCE_SCHEMA = B.SOURCE_SCHEMA AND A.SOURCE_TABLE=B.SOURCE_TABLE AND A.COLUMN_NAME = B.COLUMN_NAME

),
RESULT AS 
(
	SELECT ISNULL(ASCHEMA,BSCHEMA) AS [SCHEMA]
	,ISNULL(ANAME,BNAME) AS [TABLE]
	,ISNULL(ACOLUMN,BCOLUMN) AS [COLUMN]
	,ISNULL(ATYPE,BTYPE) AS [TYPE]
	,CASE WHEN ASCHEMA IS NULL THEN '-' WHEN BSCHEMA IS NULL THEN '+' ELSE BTYPE +' -> '+ATYPE END AS [CHANGE]

	FROM DIFFERENCES
)
SELECT * FROM RESULT
SELECT * FROM #RUNNING

END
GO

CREATE TYPE xAudit.UDT_TABLES AS TABLE
(
	[SL] INT  PRIMARY KEY NOT NULL,
	[SCHEMA] NVARCHAR(500) NOT NULL ,
	[TABLE] NVARCHAR(500) NOT NULL
)
GO

CREATE FUNCTION xAudit.GET_VERSION(@SOURCE_SCHEMA NVARCHAR(500),@SOURCE_TABLE NVARCHAR(500)) RETURNS SMALLINT
AS
BEGIN
	DECLARE @VERSION SMALLINT =(SELECT [CURRENT_SOURCE_VERSION] FROM xAudit.META_TABLES WHERE [SOURCE_SCHEMA]=@SOURCE_SCHEMA AND [SOURCE_TABLE] =@SOURCE_TABLE)
	RETURN @VERSION
		

END
GO

CREATE PROCEDURE [xAudit].[UPGRADE_TABLE_VERSION]
(
	@COPYTABLE SYSNAME
) AS
BEGIN

 UPDATE [xAudit].[META_TABLES] SET CURRENT_SOURCE_VERSION = CURRENT_SOURCE_VERSION + 1, IS_RUNNING =1
 WHERE  @COPYTABLE = '['+COPY_SCHEMA+'].['+COPY_TABLE+']'
	
END
GO

CREATE PROCEDURE [xAudit].[MARK_TABLE_AS_STOPPED]
(
	@SOURCE_SCHEMA SYSNAME,
	@SOURCE_TABLE SYSNAME
) AS
BEGIN

 UPDATE [xAudit].[META_TABLES] SET IS_RUNNING = 0 WHERE SOURCE_SCHEMA =@SOURCE_SCHEMA AND SOURCE_TABLE = @SOURCE_TABLE
	
END
GO

CREATE PROCEDURE [xAudit].[INSERT_NEW_TABLE_VERSION]
(
	@SOURCE_SCHEMA NVARCHAR(500),
	@SOURCE_TABLE NVARCHAR(500),
	@INSTANCE NVARCHAR(500)
) AS
BEGIN

IF NOT EXISTS (SELECT 1 FROM [xAudit].[META_TABLES] WHERE SOURCE_SCHEMA=@SOURCE_SCHEMA AND SOURCE_TABLE=@SOURCE_TABLE)
	BEGIN
		INSERT INTO [xAudit].[META_TABLES]
		           ([SOURCE_SCHEMA]
		           ,[SOURCE_TABLE]
		           ,[CDC_SCHEMA]
		           ,[CDC_TABLE]
				   ,[COPY_SCHEMA]
		           ,[COPY_TABLE]
		           ,[CURRENT_SOURCE_VERSION]
		           ,[IS_RUNNING])
		     VALUES
		           (@SOURCE_SCHEMA
				   ,@SOURCE_TABLE
				   ,'CDC'
				   ,@INSTANCE+'_CT'
				   ,'xAudit'
				   ,@SOURCE_SCHEMA+'_'+@SOURCE_TABLE
				   ,1
				   ,1)	
	
	END
	ELSE BEGIN
		UPDATE [xAudit].[META_TABLES] SET IS_RUNNING = 1 WHERE SOURCE_SCHEMA=@SOURCE_SCHEMA AND SOURCE_TABLE=@SOURCE_TABLE
	END
END
GO

CREATE PROCEDURE [xAudit].[MERGE@2]( @SOURCETABLE SYSNAME, @TARGETTABLE SYSNAME, @FORCE BIT)
AS BEGIN

	DECLARE @CHANGES TABLE(SL INT IDENTITY(1,1),SCOL SYSNAME NULL,ACOL SYSNAME NULL,STYPE SYSNAME NULL, ATYPE SYSNAME NULL,QUERY NVARCHAR(MAX))
	
	;WITH TSOURCE AS (
		SELECT
		 C.[NAME] AS COLUMN_NAME
		,C.SYSTEM_TYPE_NAME AS DATA_TYPE
		 FROM SYS.DM_EXEC_DESCRIBE_FIRST_RESULT_SET('SELECT TOP 1 * FROM '+@SOURCETABLE,NULL,0) C --WHERE C.[NAME] NOT LIKE '__$%'
	),
	TARCHIVE AS (
			SELECT 
			 C.[NAME] AS COLUMN_NAME
			,C.SYSTEM_TYPE_NAME AS DATA_TYPE 
			 FROM SYS.DM_EXEC_DESCRIBE_FIRST_RESULT_SET('SELECT  TOP 1 * FROM '+@TARGETTABLE,NULL,0) C --WHERE C.[NAME] NOT LIKE '__$%'
	),
	COMMON AS
	(
		SELECT * FROM TARCHIVE
		INTERSECT 
		SELECT * FROM TSOURCE
	),
	DIFFERENCES AS
	(
		SELECT A.COLUMN_NAME[SCOL],B.COLUMN_NAME[ACOL],A.DATA_TYPE[STYPE],B.DATA_TYPE[ATYPE] FROM 
		(SELECT * FROM TSOURCE EXCEPT SELECT * FROM COMMON)A
		FULL JOIN (SELECT * FROM (SELECT * FROM TARCHIVE EXCEPT SELECT * FROM COMMON)C)B
		ON A.COLUMN_NAME=B.COLUMN_NAME
		
	),
	TODO AS
	(
		SELECT *,
		CASE WHEN SCOL IS NULL THEN 'ALTER TABLE '+@TARGETTABLE+' DROP COLUMN ['+ACOL+']'
			 WHEN ACOL IS NULL THEN 'ALTER TABLE '+@TARGETTABLE+' ADD ['+SCOL+'] '+STYPE
			 WHEN SCOL=ACOL AND STYPE <> ATYPE THEN 'ALTER TABLE '+@TARGETTABLE+' ALTER COLUMN ['+SCOL+'] '+STYPE
			 ELSE '' END AS QUERY
		FROM DIFFERENCES 
	)
	
	INSERT INTO @CHANGES SELECT * FROM TODO
	SELECT * FROM @CHANGES

	--APPLYING CHANGES

	DECLARE @CURSOR INT= (SELECT COUNT(0) FROM @CHANGES)
	IF(@CURSOR > 0) EXEC xAudit.UPGRADE_TABLE_VERSION @COPYTABLE=@TARGETTABLE
	WHILE @CURSOR>0
	BEGIN
		DECLARE @Q NVARCHAR(MAX) = (SELECT QUERY FROM @CHANGES WHERE SL = @CURSOR)
		BEGIN TRY
			PRINT(@Q)
			EXEC(@Q)
		END TRY
		BEGIN CATCH
			DECLARE @SEVERITY INT = (SELECT ERROR_SEVERITY ( ))
			PRINT @SEVERITY
			IF @FORCE = 1 AND @SEVERITY = 16
				BEGIN
					--	SET NULL FOR NON COMPATIBLE ROWS
					DECLARE @COL SYSNAME = (SELECT SCOL FROM @CHANGES WHERE SL =@CURSOR)
					DECLARE @TYP SYSNAME = (SELECT STYPE FROM @CHANGES WHERE SL =@CURSOR)
					PRINT('UPDATE '+@TARGETTABLE+' SET '+@COL+' = NULL WHERE TRY_CAST( '+@COL+' AS '+@TYP+') IS NULL')
					EXEC('UPDATE '+@TARGETTABLE+' SET '+@COL+' = NULL WHERE TRY_CAST( '+@COL+' AS '+@TYP+') IS NULL')
					PRINT(@Q)
					EXEC(@Q)
				END
			ELSE
				THROW
		END CATCH
		SET @CURSOR = @CURSOR - 1
		
	END
END
GO
		
CREATE PROCEDURE [xAudit].[SETUP_COPY_TABLE](@CDCTABLE SYSNAME, @COPYTABLESCHEMA SYSNAME, @COPYTABLENAME SYSNAME, @FORCEMERGE BIT)
AS BEGIN

	DECLARE @COPYTABLE SYSNAME = '[xAudit].['+@COPYTABLESCHEMA+'_'+@COPYTABLENAME+']';
	--	CREATE COPY TABLE
	IF NOT EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE [TABLE_NAME] = @COPYTABLESCHEMA+'_'+ @COPYTABLENAME AND [TABLE_SCHEMA]='xAudit')
		BEGIN
			EXEC('SELECT * INTO '+@COPYTABLE+' FROM '+@CDCTABLE+' WHERE 1<>1')
		END
	
	--	COMPARE AND MERGE CDC TO COPY TABLE		
	EXEC [xAudit].[MERGE@2] @CDCTABLE, @COPYTABLE,@FORCEMERGE

	--	CREATE COPY TRIGGER
	DECLARE @COLUMNS TABLE([SL] INT IDENTITY(1,1),[NAME] SYSNAME NOT NULL);
	DECLARE @COMBINED NVARCHAR(MAX) = '';
	DECLARE @COMBINEDVALUES NVARCHAR(MAX) = '';
	INSERT INTO @COLUMNS SELECT [NAME] FROM SYS.DM_EXEC_DESCRIBE_FIRST_RESULT_SET('SELECT * FROM '+@CDCTABLE+' WHERE 1<>1',NULL,0)

	DECLARE @CURSOR INT = (SELECT COUNT(0) FROM @COLUMNS)
	WHILE(@CURSOR > 0)
	BEGIN
		SET @COMBINED = @COMBINED + ',' + (SELECT '['+[NAME]+']' FROM @COLUMNS WHERE SL = @CURSOR);
		SET @COMBINEDVALUES = @COMBINEDVALUES + ',' + (SELECT 'INSERTED.['+[NAME]+']' FROM @COLUMNS WHERE SL = @CURSOR);
		SET @CURSOR = @CURSOR - 1;
	END
	SET @COMBINED = (SELECT RIGHT(@COMBINED,LEN(@COMBINED)-1))
	SET @COMBINEDVALUES = (SELECT RIGHT(@COMBINEDVALUES,LEN(@COMBINEDVALUES)-1))
	DECLARE @TRIGGERNAME SYSNAME = '[cdc].[COPYTRIGGER'+@COPYTABLESCHEMA+'_'+@COPYTABLENAME+']';
	IF EXISTS (SELECT * FROM sys.objects WHERE [name] = @TRIGGERNAME AND [type] = 'TR')
	BEGIN
      EXEC ('DROP TRIGGER '+@TRIGGERNAME);
	END;
	EXEC('
	CREATE TRIGGER '+@TRIGGERNAME+' ON '+@CDCTABLE+'
	FOR INSERT AS
	BEGIN
		INSERT INTO '+@COPYTABLE+' ('+@COMBINED+') SELECT '+@COMBINEDVALUES+' FROM INSERTED;
	END');

END
GO

CREATE PROCEDURE [xAudit].[ENABLE_TABLES] 
(
	@TABLES xAudit.UDT_TABLES READONLY,
	@INSTANCEPREFIX NVARCHAR(500),
	@FORCEMERGE BIT
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
			@CAPTURE_INSTANCE = @INSTANCE_NAME,
			@FILEGROUP_NAME = 'X_AUDIT_HISTORY_FG'

		EXEC xAudit.INSERT_NEW_TABLE_VERSION
			@SOURCE_SCHEMA=@SCHEMA_NAME,
			@SOURCE_TABLE =@TABLE_NAME,
			@INSTANCE =@INSTANCE_NAME
		
		IF EXISTS(SELECT 1 FROM SYS.check_constraints WHERE [NAME]='CHECK_VERSION_CONSTRAINT_'+@INSTANCE_NAME)
		BEGIN
			EXEC('ALTER TABLE [CDC].['+@INSTANCE_NAME+'_CT] DROP CONSTRAINT CHECK_VERSION_CONSTRAINT_'+@INSTANCE_NAME)
		END

		DECLARE @V SMALLINT=(SELECT [xAudit].GET_VERSION(@SCHEMA_NAME,@TABLE_NAME))
		EXEC('ALTER TABLE [CDC].['+@INSTANCE_NAME+'_CT]
			ADD [__$version] SMALLINT NOT NULL DEFAULT ('+@V+') 
			CONSTRAINT CHECK_VERSION_CONSTRAINT_'+@INSTANCE_NAME+' CHECK(__$VERSION IS NOT NULL AND __$VERSION ='+@V+')')

		EXEC('CREATE UNIQUE CLUSTERED INDEX ['+@INSTANCE_NAME+'_CT_CLUSTERED_IDX] 
			ON [CDC].['+@INSTANCE_NAME+'_CT]
			(
				[__$start_lsn] ASC,
				[__$seqval] ASC,
				[__$operation] ASC,
				[__$version] ASC
			)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = ON, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)');
		
		--CREATE COPY TABLE
		DECLARE @CDCTABLE SYSNAME ='[CDC].['+@INSTANCE_NAME+'_CT]';
		EXEC [xAudit].[SETUP_COPY_TABLE] @CDCTABLE, @SCHEMA_NAME,@TABLE_NAME,@FORCEMERGE

		END	

		SET @CURSOR=@CURSOR-1;
	END
END
GO

CREATE PROCEDURE [xAudit].[DISABLE_TABLES] 
(
	@TABLES xAudit.UDT_TABLES READONLY,
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
		EXEC [xAudit].[MARK_TABLE_AS_STOPPED]
			@SOURCE_SCHEMA=@SCHEMA_NAME,
			@SOURCE_TABLE =@TABLE_NAME
		END
		SET @CURSOR=@CURSOR-1;
	END
END
GO

CREATE PROCEDURE [xAudit].[REENABLE_TABLES] 
(
	@TABLES xAudit.UDT_TABLES READONLY,
	@INSTANCEPREFIX NVARCHAR(500),
	@FORCEMERGE BIT
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
			@CAPTURE_INSTANCE = @INSTANCE_NAME,
			@FILEGROUP_NAME = 'X_AUDIT_HISTORY_FG'
		END
		IF EXISTS(SELECT 1 FROM SYS.check_constraints WHERE [NAME]='CHECK_VERSION_CONSTRAINT_'+@INSTANCE_NAME)
		BEGIN
			EXEC('ALTER TABLE [CDC].['+@INSTANCE_NAME+'_CT] DROP CONSTRAINT CHECK_VERSION_CONSTRAINT_'+@INSTANCE_NAME)
		END

		DECLARE @V SMALLINT=(SELECT xAudit.GET_VERSION(@SCHEMA_NAME,@TABLE_NAME))
		EXEC('ALTER TABLE [CDC].['+@INSTANCE_NAME+'_CT]
			ADD [__$version] SMALLINT NOT NULL DEFAULT ('+@V+') 
			CONSTRAINT CHECK_VERSION_CONSTRAINT_'+@INSTANCE_NAME+' CHECK(__$VERSION IS NOT NULL AND __$VERSION ='+@V+')');

		EXEC('CREATE UNIQUE CLUSTERED INDEX ['+@INSTANCE_NAME+'_CT_CLUSTERED_IDX] 
			ON [CDC].['+@INSTANCE_NAME+'_CT]
			(
				[__$start_lsn] ASC,
				[__$seqval] ASC,
				[__$operation] ASC,
				[__$version] ASC
			)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = ON, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)');
		
		--CREATE COPY TABLE
		DECLARE @CDCTABLE SYSNAME ='[CDC].['+@INSTANCE_NAME+'_CT]';
		EXEC [xAudit].[SETUP_COPY_TABLE] @CDCTABLE, @SCHEMA_NAME,@TABLE_NAME,@FORCEMERGE

		SET @CURSOR=@CURSOR-1;
	END
END
GO






CREATE PROCEDURE [xAudit].[ARCHIVE]
(
	@TABLES xAudit.UDT_TABLES READONLY,
	@FORCE BIT = NULL
) AS
BEGIN
	--	CHECK IF PARTITION FUNCTION IS AVAILABLE
	--	CHECK IF PARTITION FUNCTION IS AVAILABLE
	DECLARE @BOUNDARY INT = (SELECT FANOUT FROM sys.partition_functions WHERE [NAME] = 'xAudit_HISTORY_PARTITION_FUNCTION')-1;
	IF (@BOUNDARY IS NULL)
	BEGIN
		CREATE PARTITION FUNCTION xAudit_HISTORY_PARTITION_FUNCTION(SMALLINT)
		AS RANGE LEFT FOR VALUES(1);
		SET @BOUNDARY=1;
	END
	--	ADD SCHEME IF NOT EXISTS
	IF NOT EXISTS(SELECT 1 FROM sys.partition_schemes WHERE [NAME]='xAudit_HISTORY_PARTITION_SCHEME')
	BEGIN
		CREATE PARTITION SCHEME xAudit_HISTORY_PARTITION_SCHEME 
		AS PARTITION xAudit_HISTORY_PARTITION_FUNCTION ALL TO ([X_AUDIT_HISTORY_FG])
	END
	DECLARE @CURSOR INT =(SELECT COUNT(0) FROM @TABLES);
	WHILE (@CURSOR <> 0)
	BEGIN
		
		DECLARE @SOURCE_SCHEMA NVARCHAR(500) = (SELECT	[SCHEMA] FROM @TABLES WHERE SL =@CURSOR);
		DECLARE @SOURCE_TABLE NVARCHAR(500) = (SELECT	[TABLE] FROM @TABLES WHERE SL =@CURSOR);
		DECLARE @VERSION SMALLINT =(SELECT CURRENT_SOURCE_VERSION FROM xAudit.META_TABLES WHERE SOURCE_SCHEMA= @SOURCE_SCHEMA AND	SOURCE_TABLE=@SOURCE_TABLE);

		
		--	ADD NEW PARTITION IF NOT AVAILABLE
		IF(@BOUNDARY<@VERSION)
		BEGIN
			ALTER PARTITION FUNCTION xAudit_HISTORY_PARTITION_FUNCTION()
			SPLIT RANGE(@VERSION)
		END

		--	CLONE SOURCE TABLE
		IF NOT EXISTS (SELECT 1 FROM SYS.TABLES WHERE [NAME]=@SOURCE_SCHEMA+'_'+@SOURCE_TABLE+'_ARCHIVED' AND [SCHEMA_ID]=SCHEMA_ID('xAudit'))
		BEGIN
			--	CLONE ENTIRE DATA STRUCTURE TO ARCHIVE
			EXEC('SELECT * INTO [xAudit].'+@SOURCE_SCHEMA+'_'+@SOURCE_TABLE+'_ARCHIVED FROM [CDC].xAudit_'+@SOURCE_SCHEMA+'_'+@SOURCE_TABLE+'_CT WHERE 1<>1' );

			-- ADD CLUSTERED INDEX WITH FILEGROUP

			EXEC('CREATE UNIQUE CLUSTERED INDEX [xAudit_'+@SOURCE_SCHEMA+'_'+@SOURCE_TABLE+'_ARCHIVED_CLUSTERED_IDX] 
			ON [xAudit].'+@SOURCE_SCHEMA+'_'+@SOURCE_TABLE+'_ARCHIVED
			(
				[__$start_lsn] ASC,
				[__$seqval] ASC,
				[__$operation] ASC,
				[__$version] ASC
			)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON xAudit_HISTORY_PARTITION_SCHEME([__$version])');

		END
		ELSE BEGIN --TABLE EXISTS
			--	PREPARE TABLE WITH NEW SCHEMA CHANGES IF ANY
			EXEC [xAudit].[MERGE@2] @SOURCE_SCHEMA, @SOURCE_TABLE, @FORCE
		END
		--	SWITCH DATA TO PARTITION
		
		DECLARE @V VARCHAR(25)= (SELECT CONVERT(VARCHAR(25),@VERSION));
		EXEC('ALTER TABLE [CDC].xAudit_'+@SOURCE_SCHEMA+'_'+@SOURCE_TABLE+'_CT
			  SWITCH TO [xAudit].'+@SOURCE_SCHEMA+'_'+@SOURCE_TABLE+'_ARCHIVED  PARTITION '+@V);

		UPDATE [xAudit].[META_TABLES] SET [CURRENT_SOURCE_VERSION]=@VERSION+1,
		[ARCHIVE_SCHEMA]= 'xAudit',
		[ARCHIVE_TABLE] = @SOURCE_SCHEMA+'_'+@SOURCE_TABLE+'_ARCHIVED'
		WHERE SOURCE_SCHEMA=@SOURCE_SCHEMA AND SOURCE_TABLE=@SOURCE_TABLE

		SET @CURSOR=@CURSOR-1;
	END	--	LOOP ENDS HERE
END

