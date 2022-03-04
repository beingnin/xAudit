using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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
        private Assembly _assembly = Assembly.GetExecutingAssembly();
        public InstallerWithCDC(Version version, SqlServerDriver sqlServerDriver)
        {
            _currentVersion = version;
            _sqlServerDriver = sqlServerDriver;
        }
        public async Task InstallAsync(string DbSchema, string dataFileDirectory)
        {
            Console.WriteLine("Fresh installation started...");
            Console.WriteLine("Checking for sql server agent...");
            await this.IsAgentRunning();

            //cannot use transaction since alter table commands are used multiple times in this script
            StringBuilder query = new StringBuilder();
            using (Stream stream = _assembly.GetManifestResourceStream("xAudit.CDC.Scripts.meta.sql"))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    query.Append(await reader.ReadToEndAsync());
                }
            }
            query = query.Replace("xAudit", DbSchema)
                         .Replace("#DBNAME#", _sqlServerDriver.DbName)
                         .Replace("#DATAFILEPATH#", string.IsNullOrWhiteSpace(dataFileDirectory) ? string.Empty : dataFileDirectory);
            await _sqlServerDriver.ExecuteTextAsync(query.ToString(), null);

        }

        public Task CheckInstance()
        {
            throw new Exception("Schema already exisits. Please uninstall the current instance before proceeding");
        }

        public async Task UpgradeAsync(string DbSchema, CDCReplicatorOptions option)
        {
            Console.WriteLine("Installing version " + _currentVersion + "...");
            await this.IsAgentRunning();
            string[] filenames = _assembly.GetManifestResourceNames();
            string versionScriptPath = null;

            if (filenames.Contains($"xAudit.CDC.Scripts.Versions.{_currentVersion}.sql"))
            {
                versionScriptPath = $"xAudit.CDC.Scripts.Versions.{_currentVersion}.sql";
            }
            else

            {
                var regex = new Regex(@"\d+\.\d+\.\d+");
                var allVersions = filenames.Where(x => x.StartsWith("xAudit.CDC.Scripts.Versions")).Select(x => (Version)regex.Match(x).Value).ToArray();
                Version previousVersion = _currentVersion.FindImmediatePrevious(allVersions);
                versionScriptPath = $"xAudit.CDC.Scripts.Versions.{previousVersion}.sql";
                Console.WriteLine("script  not found for current version. Instead executing script from previous version " + previousVersion);
            }

            using (var transaction = new TransactionScope(TransactionScopeOption.Required,
                                                      new TransactionOptions() { IsolationLevel = IsolationLevel.ReadCommitted },
                                                      TransactionScopeAsyncFlowOption.Enabled))
            {
                //execute cleanup query. Delete all SP, UDF etc belongs to current version
                StringBuilder query = new StringBuilder();
                using (Stream stream = _assembly.GetManifestResourceStream("xAudit.CDC.Scripts.cleanup.sql"))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        query.Append(await reader.ReadToEndAsync());
                    }
                }
                query = query.Replace("xAudit", DbSchema);
                await _sqlServerDriver.ExecuteTextAsync(query.ToString(), null);

                //execute current version scripts. Create all SP, UDF etc belongs to current version
                query = query.Clear();
                using (Stream stream = _assembly.GetManifestResourceStream(versionScriptPath))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        query.Append(await reader.ReadToEndAsync());
                    }
                }
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
            if (Convert.ToInt32(status) != 4)
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
                 new SqlParameter("@INSTANCENAME",option.InstanceName),
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
