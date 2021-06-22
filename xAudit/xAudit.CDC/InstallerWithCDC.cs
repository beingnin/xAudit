using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using xAudit.Infrastructure.Driver;

namespace xAudit.CDC
{
    internal class InstallerWithCDC
    {
        Version _version = default(Version);
        private SqlServerDriver _sqlServerDriver;
        public InstallerWithCDC(Version version, SqlServerDriver sqlServerDriver)
        {
            _version = version;
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
        public Task UnInstallAsync()
        {
            return null;
        }
    }
}
