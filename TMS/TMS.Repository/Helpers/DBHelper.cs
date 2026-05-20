using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;

namespace TMS.Repository.Helpers
{
    internal class DBHelper : IDBHelper
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DBHelper> _logger;
        public DBHelper(IConfiguration configuration, ILogger<DBHelper> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }
        private string GetConnectionString()
        {
            return _configuration["ConnectionStrings:DefaultConnection"];
        }
        public DataSet ExecuteProc(string procedure, params SqlParameter[] sqlParameters)
        {
            DataSet ds = new();
            using (SqlConnection connection = new(GetConnectionString()))
            {
                using SqlCommand command = new(procedure, connection);
                command.CommandType = CommandType.StoredProcedure;
                if (sqlParameters != null && sqlParameters.Length > 0)
                {
                    command.Parameters.AddRange(sqlParameters);
                }
                using SqlDataAdapter dataAdapter = new(command);
                try
                {
                    dataAdapter.Fill(ds);
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error, "ExecuteProc", ex);
                }
            }
            return ds;
        }

        public int ExecuteProcNonQuery(string procedure, params SqlParameter[] sqlParameters)
        {
            int result = -1;
            using (SqlConnection connection = new(GetConnectionString()))
            {
                using SqlCommand command = new(procedure, connection);
                command.CommandType = CommandType.StoredProcedure;
                if (sqlParameters != null && sqlParameters.Length > 0)
                {
                    command.Parameters.AddRange(sqlParameters);
                }
                try
                {
                    connection.Open();
                    result = command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error, "ExecuteProcNonQuery", ex);
                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                        connection.Close();
                }
            }
            return result;
        }
    }
}
