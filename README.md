# xAudit  - automatic audit system

xAudit is the easiest way to automatically track data changes in RDBMS database for audit purposes. All you need to do is to tell xAudit which all tables you need to monitor for changes. The xAudit system will give you a replicator object in which you can start listening for DML changes on configured tables.

The biggest advantage of using xAudit is that it always has an eye on the table schema and do the necessary steps to include the change(s) in the replicated data, if it sees the schema had changed, which seems to be a conundrum for many projects

xAudit is currently available for SQL Server 2016 or above. It is available as a nuget package and can be integrated with any app types(console, web & desktop) in both .net framework and .net core projects

# How it works?

xAudit uses Change Data Capture or simply CDC as the back-end replicator implementation. This facility is natively available from SQL Server 2016. This is the reason why xAudit cannot be used along with any version before 2016. Even though we can use CDC for replication purposes, there are many other problems CDC cannot solve on it's own such as a change of schema in source table. CDC is not able to automatically handle a data structure change by default. But xAudit can. xAudit will track any structural change in data and will recreate CDC instance for that table without losing already replicated data.

> CDC is just one of the planned implementations of xAudit's replication mechanism. In a later point more techniques will be added so that more databases can be supported

# ⚙️Install
#### Using Package Manager Console(Visual Studio)
```
Install-Package Beingnin.xAudit
```
#### Using Dotnet CLI
```
dotnet add package Beingnin.xAudit
```
