using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using xAudit.Contracts;
using xAudit.Infrastructure.Driver;

namespace xAudit.CDC
{
    public class ArchiverWithCDC : IArchiver
    {
        private SqlServerDriver _sqlServerDriver = null;
        public ArchiverWithCDC(SqlServerDriver sqlServerDriver)
        {
            _sqlServerDriver = sqlServerDriver;
        }
        public async Task<HashSet<string>> Archive(HashSet<string> tables,string instanceName, bool keepVersions = false)
        {
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
                new SqlParameter("@TABLES",dt),
            };

            var result = await _sqlServerDriver.ExecuteNonQueryAsync(instanceName + ".ARCHIVE", parameters);
            return tables;
        }
    }
}
