using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBAccess
{
    /// <summary>
    /// 建立連線DB方式介面
    /// </summary>
    public interface IDbDriver
    {

        /// <summary>
        /// 連線位置
        /// </summary>
        string ConnectString { get; set; }

        /// <summary>
        /// 逾時時間
        /// </summary>
        int TimeOut { get; set; }


        /// <summary>
        /// 執行使用 SqlDataAdapter
        /// </summary>
        /// <param name="sql">sql敘述</param>
        /// <param name="type">CommandType類型</param>
        /// <param name="parameters">陣列參數</param>
        /// <returns>DataSet</returns>
        DataSet ExcuteSql_UseAdapter(string sql, CommandType type, IEnumerable<SqlParameter> parameters);
        /// <summary>
        /// 執行使用 ExecuteNonQuery
        /// </summary>
        /// <param name="sql">sql敘述</param>
        /// <param name="type">CommandType類型</param>
        /// <param name="parameters">陣列參數</param>
        /// <returns>int</returns>
        int ExcuteSql_NonQuery(string sql, CommandType type, IEnumerable<SqlParameter> parameters);
        /// <summary>
        /// 執行使用 ExecuteReader
        /// </summary>
        /// <param name="sql">sql敘述</param>
        /// <param name="type">CommandType類型</param>
        /// <param name="parameters">陣列參數</param>
        /// <returns>IDataReader</returns>
        IDataReader ExcuteReader(string sql, CommandType type, IEnumerable<SqlParameter> parameters);
    }
}
