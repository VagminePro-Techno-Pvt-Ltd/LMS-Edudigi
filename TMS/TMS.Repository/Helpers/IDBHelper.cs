using Microsoft.Data.SqlClient;
using System.Data;

namespace TMS.Repository.Helpers
{
    internal interface IDBHelper
    {
        DataSet ExecuteProc(string procedure, params SqlParameter[] sqlParameters);
        int ExecuteProcNonQuery(string procedure, params SqlParameter[] sqlParameters);
    }


}
