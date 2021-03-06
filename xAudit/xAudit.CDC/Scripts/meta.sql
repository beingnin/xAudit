


IF NOT EXISTS (SELECT 1 FROM SYS.FILEGROUPS WHERE [NAME]= 'X_AUDIT_HISTORY_FG' AND [TYPE]='FG')
BEGIN
	EXEC('ALTER DATABASE #DBNAME# ADD FILEGROUP X_AUDIT_HISTORY_FG');	
END

IF NOT EXISTS (SELECT 1 FROM SYS.DATABASE_FILES F JOIN SYS.FILEGROUPS FG ON FG.DATA_SPACE_ID = F.DATA_SPACE_ID WHERE FG.[NAME]='X_AUDIT_HISTORY_FG' AND F.[NAME]='X_AUDIT_HISTORY_DATA_FILE')
BEGIN
	EXEC('ALTER DATABASE #DBNAME# 
		  ADD FILE 
		  (
		      NAME = X_AUDIT_HISTORY_DATA_FILE,
		      FILENAME = ''#DATAFILEPATH#'',
		      SIZE = 5MB,
		      MAXSIZE = 100MB,
		      FILEGROWTH = 5MB
		  ) TO FILEGROUP X_AUDIT_HISTORY_FG'
		);
END

IF NOT EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.SCHEMATA WHERE [SCHEMA_NAME]='xAudit')
BEGIN
	EXEC('CREATE SCHEMA xAudit');
END


IF NOT EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA='xAudit' AND TABLE_NAME='META')
BEGIN
	
	CREATE TABLE xAudit.META
	(
		[Version] VARCHAR(20) NOT NULL,
		[Machine] VARCHAR(100) NOT NULL,
		[Instancename] VARCHAR(100) NOT NULL,
		[Major] INT NOT NULL,
		[Minor] INT NOT NULL,
		[Patch] INT NOT NULL,
		[ProcessID] INT NOT NULL,
		[TotalTables] INT DEFAULT 0,
		[IsCurrentVersion] BIT DEFAULT 0,
		[InstalledDateUTC] DATETIME NOT NULL,
		[TrackSchemaChanges] BIT DEFAULT 0,
		[EnablePartition] BIT DEFAULT 0,
		[KeepVersionsForPartition] BIT DEFAULT 0
	) ON [X_AUDIT_HISTORY_FG];
END

IF NOT EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA='xAudit' AND TABLE_NAME='META_TABLES')
BEGIN

	CREATE TABLE xAudit.META_TABLES
	(
		[SOURCE_SCHEMA] NVARCHAR(500) NOT NULL,
		[SOURCE_TABLE] NVARCHAR(500) NOT NULL,
		[TARGET_SCHEMA] NVARCHAR(500) NOT NULL,
		[TARGET_TABLE] NVARCHAR(500) NOT NULL,
		[ARCHIVE_SCHEMA] NVARCHAR(500) NULL,
		[ARCHIVE_TABLE] NVARCHAR(500)  NULL,
		[CURRENT_SOURCE_VERSION] SMALLINT NOT NULL,
		[IS_RUNNING] BIT NOT NULL
	)ON [X_AUDIT_HISTORY_FG]
END
IF NOT EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA='xAudit' AND TABLE_NAME='DDL_CHANGES')
BEGIN
	CREATE TABLE [XAUDIT].[DDL_CHANGES]
	(
		[SOURCE_SCHEMA] SYSNAME NOT NULL,
		[SOURCE_TABLE] SYSNAME NOT NULL,
		DDL_COMMAND NVARCHAR(MAX) NOT NULL,
		MERGED BIT DEFAULT 0 NOT NULL,
		[VERSION] SMALLINT NOT NULL,
		CHANGE_DATE DATETIME2 NOT NULL
	)ON [X_AUDIT_HISTORY_FG]
END