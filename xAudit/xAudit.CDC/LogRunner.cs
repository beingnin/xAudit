using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using System.Threading.Tasks;
using xAudit.CDC.Shared;
using xAudit.Infrastructure.Driver;

namespace xAudit.CDC
{
    public class LogRunner
    {
        private SqlServerDriver _sqlServerDriver;
        public LogRunner(SqlServerDriver sqlServerDriver)
        {
            _sqlServerDriver = sqlServerDriver;
        }
        public async Task<int> CreateRun(Run run)
        {
            DbParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Archive",run.Archive),
                new SqlParameter("@TrackSchemaChanges",run.TrackSchemaChanges),
                new SqlParameter("@InstanceName",run.InstanceName),
                new SqlParameter("@MachineName",run.MachineName),
            };
            int runId=await _sqlServerDriver.ExecuteNonQueryAsync(run.InstanceName + ".INSERT_New_Run", parameters);
            return runId;
        }

        public async Task<int> Log(Log log)
        {
            DbParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@RunId",log.Run.Id),
                new SqlParameter("@Message",log.Message),
                new SqlParameter("@Type",log.Type),
                new SqlParameter("@Exception",log.Exception),
                new SqlParameter("@StackTrace",log.StackTrace)
            };
            int logId = await _sqlServerDriver.ExecuteNonQueryAsync(log.Run.InstanceName + ".INSERT_New_Log", parameters);
            return logId;
        }
    }
}
