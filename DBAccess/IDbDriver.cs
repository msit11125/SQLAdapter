using System.Data;
using System.Data.SqlClient;

namespace DBAccess
{
    public interface IDbDriver
    {
        string ConnectString { get; set; }
        int TimeOut { get; set; }
        IDbConnection Connection { get; set; }
        IDbCommand Command { get; set; }

        //開啟/關閉 Connection
        void Open() ;
        void Close();
        /// <summary>
        /// 使用 DataAdapter 存取資料庫
        /// </summary>
        /// <returns>DataSet</returns>
        DataSet Excute();
        /// <summary>
        /// 使用 ExcuteNonQuery 存取資料庫
        /// </summary>
        /// <returns>int</returns>
        int ExcuteNonQuery();
        /// <summary>
        /// 執行使用 ExcuteReader 存取資料庫，
        /// 注意: 要手動使用 dr.Close(); 關閉原先的Connection
        /// </summary>
        /// <returns>IDataReader</returns>
        IDataReader ExcuteReader();

    }
}