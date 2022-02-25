using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Threading.Tasks;
using xAudit.CDC.Shared;
using xAudit.Infrastructure.Driver;

namespace xAudit.CDC.Helpers
{
    public class LogRunner
    {
        private static Run _run = null;
        private SqlServerDriver _sqlServerDriver;
        public LogRunner(string sourceCon)
        {
            _sqlServerDriver = new SqlServerDriver(sourceCon);
        }
        public void CreateRun(Run run)
        {
            if (_run != null)
                throw new Exception("Already initiated a run");
            _run = run;
        }

        public void Log(Log log)
        {
            Console.WriteLine(log.Message);
            if (_run.Logs == null)
                _run.Logs = new List<Log>();
            _run.Logs.Add(log);
        }

        public async Task<int> Persist(Run run)
        {
            DataTable runTables = new DataTable();
            runTables.Columns.Add("Schema", typeof(string));
            runTables.Columns.Add("Name", typeof(string));
            runTables.Columns.Add("FullName", typeof(string));
            runTables.Columns.Add("Action", typeof(RunAction));
            if (run.RunTables != null && run.RunTables.Count != 0)
            {
                foreach (var table in run.RunTables)
                {
                    runTables.Rows.Add(
                        table.Schema,
                        table.Name,
                        table.FullName,
                        table.Action);
                }
            }

            DataTable logs = new DataTable();
            logs.Columns.Add("Message", typeof(string));
            logs.Columns.Add("Type", typeof(MessageType));
            logs.Columns.Add("Exception", typeof(string));
            logs.Columns.Add("StackTrace", typeof(string));
            if (run.Logs != null && run.Logs.Count != 0)
            {
                foreach(var log in run.Logs)
                {
                    logs.Rows.Add(
                        log.Message,
                        log.Type,
                        log.Exception,
                        log.StackTrace);
                }
            }

            IDbDataParameter[] dbDataParameter =
            {
                new SqlParameter("@TrackSchemaChanges",run.TrackSchemaChanges),
                new SqlParameter("@ForceMerge",run.ForceMerge),
                new SqlParameter("@InstanceName",run.InstanceName),
                new SqlParameter("@MachineName",run.MachineName),
                new SqlParameter("@RunTables",runTables),
                new SqlParameter("@Logs",logs)
            };
            return await _sqlServerDriver.ExecuteNonQueryAsync(run.InstanceName + ".INSERT_NEW_Run", dbDataParameter);
        }
    }
}
