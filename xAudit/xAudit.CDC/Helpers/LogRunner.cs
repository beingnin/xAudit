using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
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

        public void Persist()
        {
            //save run in db 
        }
    }
}
