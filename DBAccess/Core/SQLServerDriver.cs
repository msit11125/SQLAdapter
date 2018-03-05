using DBAccess.Core;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace DBAccess.Core
{
    /// <summary>
    /// 啟用SQLServer連線工具
    /// </summary>
    internal sealed class SQLServerDriver : DbAbstractDriver, IDbDriver
    {

        #region -------- 封裝資料 -----------

        private IDbConnection _connection = null;
        private IDbCommand _command = null;

        #endregion


        public override IDbConnection Connection
        {
            get
            {
                //初始化_connection
                _connection = _connection ?? new SqlConnection(ConnectString);
                return _connection;
            }
            set
            {
                _connection = value;
            }
        }
        public override IDbCommand Command
        {
            get
            {
                //初始化_command
                _command = _command ?? new SqlCommand();
                return _command;
            }
            set
            {
                _command = value;
            }
        }



        public void Open()
        {
            this.Connection.Open();
        }
        public void Dipose()
        {
            this.Connection.Close();
        }



        /// <summary>
        /// 執行使用 SqlDataAdapter
        /// </summary>
        /// <returns>DataSet</returns>
        public DataSet Excute()
        {
            DataSet ds;
            using (SqlDataAdapter sda = new SqlDataAdapter((SqlCommand)Command))
            {
                sda.Fill(ds = new DataSet());
            }
            return ds;
        }

        /// <summary>
        /// 執行使用 ExecuteNonQuery
        /// </summary>
        /// <returns>int</returns>
        public int ExcuteNonQuery()
        {
            return Command.ExecuteNonQuery();
        }

        /// <summary>
        /// 執行使用 ExecuteReader
        /// </summary>
        /// <returns>IDataReader</returns>
        public IDataReader ExcuteReader()
        {
            return Command.ExecuteReader();
        }

    }





}