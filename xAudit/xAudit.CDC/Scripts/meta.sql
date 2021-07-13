



IF NOT EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.SCHEMATA WHERE [SCHEMA_NAME]='xAudit')
BEGIN
	EXEC('CREATE SCHEMA xAudit');
END


IF NOT EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA='xAudit' AND TABLE_NAME='Meta')
BEGIN
	
	CREATE TABLE xAudit.Meta
	(
		[Version] VARCHAR(20) NOT NULL,
		[Machine] VARCHAR(100) NOT NULL,
		[INSTANCENAME] VARCHAR(100) NOT NULL,
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
	);
END

