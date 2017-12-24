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
    internal sealed class SQLServerDriver : IDbDriver
    {
        /// <summary>
        /// 連線位置
        /// </summary>
        public string ConnectString { get; set; }

        /// <summary>
        /// 逾時時間 (秒)
        /// </summary>
        public int TimeOut { get; set; }


        #region -------- 封裝資料 -----------

        private SqlConnection _connection = null;
        public SqlConnection Connection
        {
            get
            {
                //初始化Connection
                if (_connection == null)
                {
                    _connection = new SqlConnection(ConnectString);
                }
                return _connection; 
            }
            set
            {
                _connection = value;
            }
        }


        //開啟 Connection
        private void Open() { _connection.Open(); }
        //關閉 Connection
        private void Close() { _connection.Close(); }

        #endregion


        /// <summary>
        /// 執行使用 SqlDataAdapter
        /// </summary>
        /// <param name="sql">sql敘述</param>
        /// <param name="type">CommandType類型</param>
        /// <param name="parameters">陣列參數</param>
        /// <returns>DataSet</returns>
        public DataSet ExcuteSql_UseAdapter(string sql, CommandType type, IEnumerable<SqlParameter> parameters)
        {
            DataSet ds;
            SqlCommand command = null;
            try
            {
                command = new SqlCommand(sql, Connection);
                command.CommandType = type;
                command.CommandTimeout = TimeOut;

                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        command.Parameters.Add(param);
                    };
                }
                Open();

                using (SqlDataAdapter sda = new SqlDataAdapter(command))
                {
                    sda.Fill(ds = new DataSet());
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
            finally
            {
                command.Parameters.Clear();
                command.Dispose();
                Close();
            }

            return ds;
        }

        /// <summary>
        /// 執行使用 ExecuteNonQuery
        /// </summary>
        /// <param name="sql">sql敘述</param>
        /// <param name="type">CommandType類型</param>
        /// <param name="parameters">陣列參數</param>
        /// <returns>int</returns>
        public int ExcuteSql_NonQuery(string sql, CommandType type, IEnumerable<SqlParameter> parameters)
        {
            var i = 0;
            SqlCommand command = null;
            try
            {
                command = new SqlCommand(sql, Connection);
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        command.Parameters.Add(param);
                    };
                }

                command.CommandType = type;
                command.CommandTimeout = TimeOut;

                Open();

                i = command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
            finally
            {
                command.Parameters.Clear();
                command.Dispose();
                Close();
            }

            return i;
        }

        /// <summary>
        /// 執行使用 ExecuteReader
        /// </summary>
        /// <param name="sql">sql敘述</param>
        /// <param name="type">CommandType類型</param>
        /// <param name="parameters">陣列參數</param>
        /// <returns>IDataReader</returns>
        public IDataReader ExcuteReader(string sql, CommandType type, IEnumerable<SqlParameter> parameters)
        {
            SqlCommand command = null;
            IDataReader reader = null;
            try
            {
                command = new SqlCommand(sql, Connection);
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        command.Parameters.Add(param);
                    };
                }

                command.CommandType = type;
                command.CommandTimeout = TimeOut;

                Open();
                reader = command.ExecuteReader();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
            finally
            {
                command.Parameters.Clear();
                command.Dispose();
                // DataReader 物件在這裡不Close() 要手動使用
                // => dr.Close() 才會連同connection一同關閉
            }

            return reader;
        }
    }





}