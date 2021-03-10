using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using xAudit.Contracts;
using xAudit.Infrastructure.Driver;

namespace xAudit.CDC
{
    public class ReplicatorUsingCDC : IReplicator
    {
        private enum WhatNext { NoUpdate,Install, Upgrade,Downgrade}
        private const string _SCHEMA = "xAudit";
        private string _sourceCon = null;
        private string _partitionCon = null;
        private SqlServerDriver _sqlServerDriver = null;
        private IDictionary<string, string> _tables = null;
        private static Lazy<ReplicatorUsingCDC> _instance = new Lazy<ReplicatorUsingCDC>(() => new ReplicatorUsingCDC());

        public string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString();
        private ReplicatorUsingCDC()
        {
            Console.WriteLine("object created");
        }
        public static ReplicatorUsingCDC GetInstance(IDictionary<string, string> tables, string sourceCon, string partitionCon)
        {
            _instance.Value._sourceCon = sourceCon;
            _instance.Value._tables = tables;
            _instance.Value._partitionCon = partitionCon;
            _instance.Value._sqlServerDriver = new SqlServerDriver(_instance.Value._sourceCon);

            return _instance.Value;
        }
        public void Partition(string schema, string table, string version)
        {
            throw new NotImplementedException();
        }

        public Task PartitionAsync(string schema, string table, string version)
        {
            throw new NotImplementedException();
        }

        public void Start()
        {
            throw new NotImplementedException();
        }

        public async Task StartAsync()
        {
            var action = await WhatToDoNextAsync();
        }

        public void Stop(bool backupBeforeDisabling = true, bool cleanSource = true)
        {
            throw new NotImplementedException();
        }

        public Task StopAsync(bool cleanup)
        {
            throw new NotImplementedException();
        }


        #region private-methods

        private Task ExecuteInitialScriptsAsync()
        {
            return null;
        }
        private Task ExecuteVersionUpdateScriptsAsync()
        {
            return null;
        }
        private async Task<WhatNext> WhatToDoNextAsync()
        {
            string query = string.Format(@"IF 
                                         (SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{0}' AND TABLE_NAME = 'Meta') > 0
                                         SELECT  1 AS IsExists, (SELECT TOP(1) [Version] FROM xAudit.Meta WHERE IsCurrentVersion = 1) AS [Version]
                                         ELSE SELECT 0 AS IsExists, NULL AS [Version]", _SCHEMA);
            var dt = await _sqlServerDriver.GetDataTableAsync(query, null,System.Data.CommandType.Text);
            var exists = Convert.ToBoolean(dt.Rows[0]["IsExists"]);
            var version = Convert.ToString(dt.Rows[0]["Version"]);

            if (!exists)
                return WhatNext.Install;
            if (string.IsNullOrWhiteSpace(version))
                return WhatNext.Install;
            else
                return WhatNext.Upgrade;
        }

        #endregion
    }
}
