using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using xAudit.CDC.Exceptions;
using xAudit.Infrastructure.Driver;

namespace xAudit.CDC
{
    internal class InstallerWithCDC
    {
        Version _currentVersion = default(Version);
        private SqlServerDriver _sqlServerDriver;
        public InstallerWithCDC(Version version, SqlServerDriver sqlServerDriver)
        {
            _currentVersion = version;
            _sqlServerDriver = sqlServerDriver;
        }
        public async Task InstallAsync(string DbSchema)
        {
            Console.WriteLine("Fresh installation started...");
            Console.WriteLine("Checking for sql server agent...");
            await this.IsAgentRunning();
            using (var transaction = new TransactionScope(TransactionScopeOption.Required,
                                                          new TransactionOptions() { IsolationLevel = IsolationLevel.ReadCommitted },
                                                          TransactionScopeAsyncFlowOption.Enabled))
            {
                string path = Path.Combine(Environment.CurrentDirectory, "Scripts", "meta.sql");
                StringBuilder query = new StringBuilder(File.ReadAllText(path, Encoding.UTF8));
                query = query.Replace("xAudit", DbSchema);
                await _sqlServerDriver.ExecuteTextAsync(query.ToString(), null);
                transaction.Complete();
            }

        }

        public async Task UpgradeAsync(string DbSchema,CDCReplicatorOptions option)
        {
            Console.WriteLine("Installing version "+_currentVersion+"...");
            await this.IsAgentRunning();
            string versionScriptPath = Path.Combine(Environment.CurrentDirectory, "Scripts", "Versions");
            var cleanupScriptPath = Path.Combine(Environment.CurrentDirectory, "Scripts", "cleanup.sql");
            if (File.Exists(versionScriptPath + "\\" + _currentVersion + ".sql"))
                versionScriptPath = versionScriptPath + "\\" + _currentVersion + ".sql";
            else
            {
                Version previousVersion = _currentVersion.FindImmediatePrevious(Directory.GetFiles(versionScriptPath).Select(x => (Version)Path.GetFileNameWithoutExtension(x)).ToArray());
                versionScriptPath = versionScriptPath + "\\" + previousVersion + ".sql";
                Console.WriteLine("script  not found for current version. Instead executing previous version "+previousVersion);
            }


            using (var transaction = new TransactionScope(TransactionScopeOption.Required,
                                                      new TransactionOptions() { IsolationLevel = IsolationLevel.ReadCommitted },
                                                      TransactionScopeAsyncFlowOption.Enabled))
            {
                //execute cleanup query. Delete all SP, UDF etc belongs to current version
                StringBuilder query = new StringBuilder(File.ReadAllText(cleanupScriptPath, Encoding.UTF8));
                query = query.Replace("xAudit", DbSchema);
                await _sqlServerDriver.ExecuteTextAsync(query.ToString(), null);

                //execute current version scripts. Create all SP, UDF etc belongs to current version
                query = query.Clear();
                query = query.Append(File.ReadAllText(versionScriptPath, Encoding.UTF8)).Replace("xAudit", DbSchema);
                await _sqlServerDriver.ExecuteScriptAsync(query.ToString());

                await this.AddVersion(option, DbSchema);
                transaction.Complete();
            }
        }

        private async Task IsAgentRunning()
        {
            string query = @"SELECT dss.[status] FROM   sys.dm_server_services dss
                                            WHERE  dss.[servicename] LIKE N'SQL Server Agent (%';";

            var status = await _sqlServerDriver.ExecuteTextScalarAsync(query, null);
            if( Convert.ToInt32(status) != 4)
            {
                throw new SqlSeverAgentNotFoundException("xAudit needs Sql Sever Agent to be running");
            }
        }
        private async Task AddVersion(CDCReplicatorOptions option, string dbSchemaName)
        {
            DbParameter[] parameters = new SqlParameter[]
            {
                 new SqlParameter("@VERSION",this._currentVersion.ToString()),
                 new SqlParameter("@MACHINE",Environment.MachineName),
                 new SqlParameter("@MAJOR",this._currentVersion.Major),
                 new SqlParameter("@MINOR",this._currentVersion.Minor),
                 new SqlParameter("@PATCH",this._currentVersion.Patch),
                 new SqlParameter("@PROCESSID",Process.GetCurrentProcess().Id),
                 new SqlParameter("@TOTALTABLES",option.Tables==null?(object)DBNull.Value:option.Tables.Count),
                 new SqlParameter("@TRACKSCHEMACHANGES",option.TrackSchemaChanges),
                 new SqlParameter("@ENABLEPARTITION",option.EnablePartition),
                 new SqlParameter("@KEEPVERSIONSFORPARTITION",option.KeepVersionsForPartition)
            };
            await _sqlServerDriver.ExecuteNonQueryAsync(dbSchemaName + ".INSERT_NEW_VERSION", parameters);
            Console.WriteLine("version " + this._currentVersion + " installed");
        }
        public Task UninstallAsync()
        {
            return null;
        }
    }
}
