using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
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

        public async Task UpgradeAsync(string DbSchema)
        {
            string versionScriptPath = Path.Combine(Environment.CurrentDirectory, "Scripts", "Versions");
            var cleanupScriptPath = Path.Combine(Environment.CurrentDirectory, "Scripts", "cleanup.sql");
            if (File.Exists(versionScriptPath + "\\" + _currentVersion + ".sql"))
                versionScriptPath = versionScriptPath + "\\" + _currentVersion + ".sql";
            else
            {
                Version previousVersion = _currentVersion.FindImmediatePrevious(Directory.GetFiles(versionScriptPath).Select(x => (Version)Path.GetFileNameWithoutExtension(x)).ToArray());
                versionScriptPath = versionScriptPath + "\\" + previousVersion + ".sql";
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
                await _sqlServerDriver.ExecuteTextAsync(query.ToString(), null);
                transaction.Complete();
            }
        }

        public async Task<bool> IsAgentRunning()
        {
            string query = @"SELECT dss.[status] FROM   sys.dm_server_services dss
                                            WHERE  dss.[servicename] LIKE N'SQL Server Agent (%';";

            var status = await _sqlServerDriver.ExecuteTextScalarAsync(query, null);
            return Convert.ToInt32(status) == 4;
        }
        public async Task<bool> EnableCDC()
        {
            await _sqlServerDriver.ExecuteNonQueryAsync("sys.sp_cdc_enable_db", null);
            return true;
        }
        public Task UninstallAsync()
        {
            return null;
        }
    }
}
