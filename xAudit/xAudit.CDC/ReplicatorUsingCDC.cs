using System;
using System.Threading.Tasks;
using System.Linq;
using xAudit.Contracts;
using xAudit.Infrastructure.Driver;
using System.Collections.Generic;
using System.Data;
using xAudit.CDC.Extensions;
using xAudit.CDC.Helpers;

namespace xAudit.CDC
{
    public class ReplicatorUsingCDC : IReplicator
    {

        private string _sourceCon = null;
        private string _partitionCon = null;
        private SqlServerDriver _sqlServerDriver = null;
        private static Lazy<ReplicatorUsingCDC> _instance = new Lazy<ReplicatorUsingCDC>(() => new ReplicatorUsingCDC());
        private CDCReplicatorOptions _options = null;
        public Version CurrentVersion => "1.1.1";
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
            Console.WriteLine("status :" + action);
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

            await this.EnableCDC(this._options.InstanceName);
            var failedList = await this.EnableCDC(this._options.InstanceName, _options);
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
            await installer.InstallAsync(this._options.InstanceName);
            await installer.UpgradeAsync(this._options.InstanceName, this._options);

        }
        private Task RunUpgradationLogic()
        {
            var installer = new InstallerWithCDC(this.CurrentVersion, this._sqlServerDriver);
            return installer.UpgradeAsync(this._options.InstanceName, this._options);
        }
        private async Task<bool> EnableCDC(string DbSchema)
        {
            await _sqlServerDriver.ExecuteNonQueryAsync(DbSchema + ".Enable_CDC_On_DB", null);
            return true;
        }
        /// <summary>
        /// Will try to enable cdc on the given tables if not already enabled
        /// </summary>
        /// <param name="tables">The list of tables under appropriate schema names</param>
        /// <returns>Return the list of tables which got failed to be enabled for cdc</returns>
        private async Task<AuditTableCollection> EnableCDC(string dbSchemaName, CDCReplicatorOptions option)
        {
            var ds = await _sqlServerDriver.GetDataSetAsync(dbSchemaName + ".GET_TRACKED_TABLES", null);
            if (ds == null)
                return option.Tables;

            HashSet<string> recreatableInstances = null;
            HashSet<string> activeInstances = null;
            HashSet<string> inputTables = AuditTableCollectionHelper.ToHashSet(option.Tables);
            if (option.TrackSchemaChanges)
            {
                if (ds.Tables[0] != null)
                {
                    recreatableInstances = new HashSet<string>();
                    Console.WriteLine("\nThe following tables have changed since the last run");
                    Console.WriteLine("---------------------------------------------------------");
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        var ins = Convert.ToString(row["CAPTURE_INSTANCE"]).SplitInstance();
                        var tableName = ins.Item1 + "." + ins.Item2;
                        recreatableInstances.Add(tableName);
                        log(tableName, Convert.ToString(row["COLUMN_NAME"]), Convert.ToChar(row["CHANGE"]));
                    }
                }
            }
            if(ds.Tables[1]!=null)
            {
                activeInstances = new HashSet<string>();
                foreach (DataRow row in ds.Tables[1].Rows)
                {
                    var ins = Convert.ToString(row["CAPTURE_INSTANCE"]).SplitInstance();
                    var tableName = ins.Item1 + "." + ins.Item2;
                    activeInstances.Add(tableName);
                }
            }


            return option.Tables;

            //local functions

            void log(string instance, string column, char change)
            {
                Console.ForegroundColor = change == '-' ? ConsoleColor.Red : ConsoleColor.Green;
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
                                         SELECT  1 AS IsExists, (SELECT TOP(1) [Version] FROM xAudit.Meta WHERE IsCurrentVersion = 1) AS [Version]
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

        #endregion
    }
}
