using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace xAudit.Infrastructure.Driver
{
    public class SqlServerDriver
    {
        private string _SourceCon;
        private SqlConnection _SourceConnection;
        public string SourceDB => _SourceConnection.Database;
        public SqlServerDriver(string sourceCon)
        {
            _SourceCon = sourceCon;
            _SourceConnection = new SqlConnection(_SourceCon);
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
        public Task<DataTable> GetDataTableAsync(string procedure, IDataParameter[] parameters, CommandType commandType = CommandType.StoredProcedure, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.CommandText = procedure;
                    cmd.CommandType = commandType;
                    if (parameters != null && parameters.Length > 0)
                        cmd.Parameters.AddRange(parameters);
                    cmd.Connection = _SourceConnection;
                    using (SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(cmd))
                    {
                        //await OpenAsync(cancellationToken).ConfigureAwait(false);
                        DataTable dt = new DataTable();
                        sqlDataAdapter.Fill(dt);
                        return Task.FromResult(dt);
                    }
                }
            }
            finally
            {
                Close();
            }
        }

        public async Task<int> ExecuteNonQueryAsync(string procedure, IDataParameter[] parameters, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.CommandText = procedure;
                    cmd.CommandType = CommandType.StoredProcedure;
                    if (parameters != null && parameters.Length > 0)

                    {
                        cmd.Parameters.AddRange(parameters);
                    }
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
        public async Task<int> ExecuteTextAsync(string query, IDataParameter[] parameters, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    await OpenAsync(cancellationToken).ConfigureAwait(false);
                    cmd.CommandText = query;
                    cmd.CommandType = CommandType.Text;
                    if (parameters != null && parameters.Length > 0)
                        cmd.Parameters.AddRange(parameters);
                    cmd.Connection = _SourceConnection;
                    return await cmd.ExecuteNonQueryAsync(cancellationToken);
                }
            }
            finally
            {
                Close();
            }
        }
        public async Task ExecuteScriptAsync(string script, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var batches = Regex.Split(script, @"\bGO\b").Where(x=>!string.IsNullOrWhiteSpace(x));
                using (SqlCommand cmd = new SqlCommand())
                {
                    await OpenAsync(cancellationToken).ConfigureAwait(false);
                    cmd.CommandType = CommandType.Text;
                    cmd.Connection = _SourceConnection;
                    foreach (var batch in batches)
                    {

                        cmd.CommandText = batch;
                        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                Close();
            }
        }
        public async Task<object> ExecuteScalarAsync(string procedure, IDataParameter[] parameters, CancellationToken cancellationToken = default(CancellationToken))
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
                    return await cmd.ExecuteScalarAsync(cancellationToken);
                }
            }
            finally
            {
                Close();
            }
        }

        public async Task<object> ExecuteTextScalarAsync(string query, IDataParameter[] parameters, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.CommandText = query;
                    cmd.CommandType = CommandType.Text;
                    if (parameters != null && parameters.Length > 0)
                    {
                        cmd.Parameters.AddRange(parameters);
                    }
                    cmd.Connection = _SourceConnection;
                    await OpenAsync(cancellationToken);
                    return await cmd.ExecuteScalarAsync(cancellationToken);
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
