# ⚡️ xAudit - What?

xAudit is the easiest way to automatically track data changes in RDBMS database. All you need to do is to tell xAudit which all tables you need to monitor for changes. The xAudit system will give you a replicator object in which you can start listening for DML changes on configured tables. 

The biggest advantage of using xAudit is that it always has an eye on the table schema and do the necessary steps to include the change(s) in the replicated data, if it sees the schema had changed, which seems to be a conundrum for many projects

xAudit is currently avalibale for SQL Server 2016 or above. It is available as a nuget package and can be integrated with any app types(console, web & desktop) in both .net framework and .net core projects

# ⚡️ xAudit - How?

xAudit uses Change Data Capturem or simply CDC as the backend replicator's implementation. This facility is natively available from SQL Server 2016. This is the reason why xAudit cannot be used along qith any version before 2016. Eventhough we can use CDC for replication purposes, there are many other problems CDC cannot solve on it's own such as a change in the schema of source table. CDC is not able to automatically handle a data structure change by default. But xAudit can. xAudit will track any structural change in data and will reacreate CDC instance for that table without losing already replicated data.

