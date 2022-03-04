using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Transactions;
using xAudit.CDC.Extensions;
using xAudit.CDC.Helpers;
using xAudit.Contracts;
using xAudit.Infrastructure.Driver;

namespace xAudit.CDC
{
    public class ReplicatorUsingCDC : IReplicator
    {

        private string _sourceCon = null;
        private string _partitionCon = null;
        private SqlServerDriver _sqlServerDriver = null;
        private static Lazy<ReplicatorUsingCDC> _instance = new Lazy<ReplicatorUsingCDC>(() => new ReplicatorUsingCDC());
        private CDCReplicatorOptions _options = null;
        public Version CurrentVersion => "1.0.16";
        private ReplicatorUsingCDC()
        {
        }
        public static ReplicatorUsingCDC GetInstance(CDCReplicatorOptions options, string sourceCon, string partitionCon)
        {
            _instance.Value._sourceCon = sourceCon;
            _instance.Value._partitionCon = partitionCon;
            _instance.Value._options = options;
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
            Console.WriteLine("Tool status : " + action);
            switch (action)
            {
                case WhatNext.NoUpdate:
                    break;
                case WhatNext.Install:
                    await RunInstallationLogic();
                    break;
                case WhatNext.Upgrade:
                    await RunUpgradationLogic();
                    break;
                case WhatNext.Downgrade:
                    break;
                default:
                    break;
            }

            await this.EnableOndB(this._options.InstanceName);
            _ = await this.CheckAndApplyOnTables(this._options.InstanceName, _options);
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
        private async Task RunInstallationLogic()
        {

            var installer = new InstallerWithCDC(this.CurrentVersion, this._sqlServerDriver);
            await installer.InstallAsync(this._options.InstanceName, this._options.DataDirectory);
            await installer.UpgradeAsync(this._options.InstanceName, this._options);

        }
        private Task RunUpgradationLogic()
        {
            var installer = new InstallerWithCDC(this.CurrentVersion, this._sqlServerDriver);
            return installer.UpgradeAsync(this._options.InstanceName, this._options);
        }
        private async Task<bool> EnableOndB(string DbSchema)
        {
            await _sqlServerDriver.ExecuteNonQueryAsync(DbSchema + ".Enable_CDC_On_DB", null);
            return true;
        }
        /// <summary>
        /// Will try to enable cdc on the given tables if not already enabled
        /// </summary>
        /// <param name="tables">The list of tables under appropriate schema names</param>
        /// <returns>Return the list of tables which got failed to be enabled for cdc</returns>
        private async Task<AuditTableCollection> CheckAndApplyOnTables(string dbSchemaName, CDCReplicatorOptions option)
        {
            var ds = await _sqlServerDriver.GetDataSetAsync(dbSchemaName + ".GET_TRACKED_TABLES@2", null);
            if (ds == null)
                return option.Tables;

            HashSet<string> changedTables = new HashSet<string>();
            HashSet<string> activeTables = new HashSet<string>();
            HashSet<string> inputTables = AuditTableCollectionHelper.ToHashSet(option.Tables);
            if (ds.Tables[0] != null && ds.Tables[0].Rows.Count > 0)
            {
                Console.WriteLine("\nThe following active tables have changed since the last run");
                Console.WriteLine("---------------------------------------------------------");
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    var schema = Convert.ToString(row["SCHEMA"]);
                    var table = Convert.ToString(row["TABLE"]);
                    var tableName = schema + "." + table;
                    if (option.TrackSchemaChanges)
                    {
                        changedTables.Add(tableName);
                    }
                    log(tableName, Convert.ToString(row["COLUMN"]), Convert.ToString(row["CHANGE"]));
                }

                if (!option.TrackSchemaChanges)
                {
                    Console.WriteLine("Warning! Tracking schema changes are disabled for this run");
                }
            }
            if (ds.Tables[1] != null)
            {
                foreach (DataRow row in ds.Tables[1].Rows)
                {
                    var schema = Convert.ToString(row["SOURCE_SCHEMA"]);
                    var table = Convert.ToString(row["SOURCE_TABLE"]);
                    var tableName = schema + "." + table;
                    activeTables.Add(tableName);
                }
            }

            this.SegregateTables(inputTables, activeTables, changedTables, out HashSet<string> recreate, out HashSet<string> disable, out HashSet<string> enable);
            await this.Enable(enable, option.InstanceName, option.ForceMerge);
            await this.Disable(disable, option.InstanceName);
            await this.Reenable(recreate, option.InstanceName, option.ForceMerge);

            return option.Tables;

            //local functions

            void log(string instance, string column, string change)
            {
                switch (change)
                {
                    case "-":
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    case "+":
                        Console.ForegroundColor = ConsoleColor.Green;
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                };
                Console.Write($"[{change}]");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"{instance}--> {column}");
                Console.ResetColor();
            }
        }
        private async Task<WhatNext> WhatToDoNextAsync()
        {
            string query = string.Format(@"IF 
                                         (SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{0}' AND TABLE_NAME = 'Meta') > 0
                                         SELECT  1 AS IsExists, (SELECT TOP(1) [Version] FROM {0}.Meta WHERE IsCurrentVersion = 1) AS [Version]
                                         ELSE SELECT 0 AS IsExists, NULL AS [Version]", _options.InstanceName);
            var dt = await _sqlServerDriver.GetDataTableAsync(query, null, System.Data.CommandType.Text);
            var exists = Convert.ToBoolean(dt.Rows[0]["IsExists"]);
            Version version = Convert.ToString(dt.Rows[0]["Version"]);

            if (!exists)
                return WhatNext.Install;
            if (version == default(Version))
                return WhatNext.Install;
            if (version == this.CurrentVersion)
                return WhatNext.NoUpdate;
            if (version < this.CurrentVersion)
                return WhatNext.Upgrade;
            if (version > this.CurrentVersion)
                return WhatNext.Downgrade;
            else
                return WhatNext.NoUpdate;
        }

        private void SegregateTables(HashSet<string> inputTables, HashSet<string> activeTables, HashSet<string> changedTables, out HashSet<string> recreate, out HashSet<string> disable, out HashSet<string> enable)
        {
            recreate = new HashSet<string>(inputTables);
            disable = new HashSet<string>(activeTables);
            enable = new HashSet<string>(inputTables);
            recreate.IntersectWith(changedTables);
            disable.ExceptWith(inputTables);
            enable.ExceptWith(activeTables);
        }

        private async Task<int> Enable(HashSet<string> tables, string instance, bool forceMerge)
        {
            if (tables == null || tables.Count ==0)
                return 0;

            var dt = new DataTable();

            DataColumn sl = new DataColumn("sl", typeof(int));
            sl.AutoIncrement = true;
            sl.AutoIncrementSeed = sl.AutoIncrementStep = 1;
            dt.Columns.Add(sl);
            dt.Columns.Add("schema", typeof(string));
            dt.Columns.Add("table", typeof(string));
            foreach (var t in tables)
            {
                var detail = t.Split('.');
                DataRow r = dt.NewRow();
                r["schema"] = detail[0];
                r["table"] = detail[1];
                dt.Rows.Add(r);
            }
            IDbDataParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@tables",dt),
                new SqlParameter("@instancePrefix",instance),
                new SqlParameter("@FORCEMERGE",forceMerge)
            };
            Console.WriteLine("\nThe following tables have been added to the audit collection since the last run");
            foreach (var t in tables)
            {
                Console.WriteLine(t);
            }

            using (var transaction = new TransactionScope(TransactionScopeOption.Required,
                                                     new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted },
                                                     TransactionScopeAsyncFlowOption.Enabled))
            {
                var result = await _sqlServerDriver.ExecuteNonQueryAsync(instance + ".ENABLE_TABLES", parameters);
                transaction.Complete();
                return result;
            }

        }
        private async Task<int> Reenable(HashSet<string> tables, string instance, bool forceMerge)
        {
            if (tables == null || tables.Count == 0)
                return 0;

            var dt = new DataTable();

            DataColumn sl = new DataColumn("sl", typeof(int));
            sl.AutoIncrement = true;
            sl.AutoIncrementSeed = sl.AutoIncrementStep = 1;
            dt.Columns.Add(sl);
            dt.Columns.Add("schema", typeof(string));
            dt.Columns.Add("table", typeof(string));
            foreach (var t in tables)
            {
                var detail = t.Split('.');
                DataRow r = dt.NewRow();
                r["schema"] = detail[0];
                r["table"] = detail[1];
                dt.Rows.Add(r);
            }
            IDbDataParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@tables",dt),
                new SqlParameter("@instancePrefix",instance),
                new SqlParameter("@FORCEMERGE",forceMerge)
            };
            using (var transaction = new TransactionScope(TransactionScopeOption.Required,
                                                    new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted },
                                                    TransactionScopeAsyncFlowOption.Enabled))
            {
                var result = await _sqlServerDriver.ExecuteNonQueryAsync(instance + ".REENABLE_TABLES", parameters);
                transaction.Complete();
                return result;
            }
        }
        private async Task<int> Disable(HashSet<string> tables, string instance)
        {
            if (tables == null || tables.Count == 0)
                return 0;

            var dt = new DataTable();

            DataColumn sl = new DataColumn("sl", typeof(int));
            sl.AutoIncrement = true;
            sl.AutoIncrementSeed = sl.AutoIncrementStep = 1;
            dt.Columns.Add(sl);
            dt.Columns.Add("schema", typeof(string));
            dt.Columns.Add("table", typeof(string));
            foreach (var t in tables)
            {
                var detail = t.Split('.');
                DataRow r = dt.NewRow();
                r["schema"] = detail[0];
                r["table"] = detail[1];
                dt.Rows.Add(r);
            }
            IDbDataParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@tables",dt),
                new SqlParameter("@instancePrefix",instance)
            };
            Console.WriteLine("\nThe following tables have been removed from the audit collection since the last run");
            foreach (var t in tables)
            {
                Console.WriteLine(t);
            }
            using (var transaction = new TransactionScope(TransactionScopeOption.Required,
                                                    new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted },
                                                    TransactionScopeAsyncFlowOption.Enabled))
            {
                var result = await _sqlServerDriver.ExecuteNonQueryAsync(instance + ".DISABLE_TABLES", parameters);
                transaction.Complete();
                return result;
            }
        }
        #endregion
    }
}
