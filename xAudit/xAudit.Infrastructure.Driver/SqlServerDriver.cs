using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace xAudit.Infrastructure.Driver
{
    public class SqlServerDriver
    {
        private string _SourceCon;
        private SqlConnection _SourceConnection => new SqlConnection(_SourceCon);
        public string SourceDB => _SourceConnection.Database;
        public SqlServerDriver(string sourceCon)
        {
            _SourceCon = sourceCon;
        }

        public async Task<DataSet> GetDataSetAsync(string procedure, IDataParameter[] parameters, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.CommandText = procedure;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddRange(parameters);
                    cmd.Connection = _SourceConnection;
                    using (SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(cmd))
                    {
                        await OpenAsync(cancellationToken);
                        DataSet ds = null;
                        sqlDataAdapter.Fill(ds);
                        return ds;
                    }
                }
            }
            finally
            {
                Close();
            }
        }
        public async Task<DataTable> GetDataTableAsync(string procedure, IDataParameter[] parameters, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.CommandText = procedure;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddRange(parameters);
                    cmd.Connection = _SourceConnection;
                    using (SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(cmd))
                    {
                        await OpenAsync(cancellationToken);
                        DataTable dt = null;
                        sqlDataAdapter.Fill(dt);
                        return dt;
                    }
                }
            }
            finally
            {
                Close();
            }
        }

        public async Task<int> ExecuteNonQuery(string procedure, IDataParameter[] parameters, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.CommandText = procedure;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddRange(parameters);
                    cmd.Connection = _SourceConnection;
                    await OpenAsync(cancellationToken);
                    return await cmd.ExecuteNonQueryAsync(cancellationToken);
                }
            }
            finally
            {
                Close();
            }
        }

        private async Task OpenAsync(CancellationToken cancellationToken)
        {
            if (_SourceConnection.State == ConnectionState.Closed)
                await _SourceConnection.OpenAsync(cancellationToken);
        }
        private void Open()
        {
            if (_SourceConnection.State == ConnectionState.Closed)
                _SourceConnection.Open();
        }
        private void Close()
        {
            if (_SourceConnection.State != ConnectionState.Closed)
                _SourceConnection.Close();
        }

    }
}
