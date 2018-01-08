using DBAccess.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DBAccess
{

    public interface IDbEngineAdapter
    {
        void BeginTransaction();
        void Commit();
        DataSet Excute(string sql, CommandType commandType, SqlParameter[] parameters);
        int ExcuteNonQuery(string sql, CommandType commandType, SqlParameter[] parameters);
        IDataReader ExcuteReader(string sql, CommandType commandType, SqlParameter[] parameters);
    }



    /// <summary>
    /// 用來建立與SQL連線的元件工具
    /// </summary>
    public class DbEngineAdapter : IDbEngineAdapter
    {
        // 讀取 xml 並建立工廠
        private static SQLAccessFactory SQLFactory = new SQLAccessFactory(
            Path.Combine(Path.GetDirectoryName(Assembly.GetAssembly(typeof(DbEngineAdapter)).CodeBase),"DbDrivers.xml")
            );

        private IDbDriver _driver;  // 連線的元件 (SQLSERVER or ORACLE or SQLLITE...)
        private IDbTransaction _tran; // Transaction

        /// <summary>
        /// 建立SQL Server 連線DB
        /// </summary>
        /// <param name="connectString">連線位置</param>
        /// <param name="timeOut">逾時時間(預設120秒)</param>
        /// <param name="driver">連線的元件(預設SQLSERVER秒)</param>
        /// <returns></returns>
        public DbEngineAdapter(string connectString, int timeOut = 120, string driver = "SQLSERVER")
        {
            _driver = (IDbDriver)SQLFactory.CreateDriver(driver, "singleton");
            _driver.TimeOut = timeOut;
            _driver.ConnectString = connectString;
        }


        public void BeginTransaction()
        {
            _driver.Open();
            _driver.Command.Transaction = _tran = _driver.Connection.BeginTransaction();
        }
        public void Commit()
        {
            _tran.Commit();
            _tran.Dispose();
            ClearAndCloseDriver();
            _tran = null;
        }

        /// <summary>
        /// 使用 SqlDataAdapter 存取資料庫
        /// </summary>
        public DataSet Excute(string sql, CommandType commandType, SqlParameter[] parameters)
        {
            DataSet ds;
            var conn = _driver.Connection;
            _driver.Command.CommandText = sql;
            _driver.Command.Connection = conn;
            _driver.Command.CommandType = commandType;
            _driver.Command.CommandTimeout = _driver.TimeOut;

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    _driver.Command.Parameters.Add(param);
                };
            }
            try
            {
                OpenOrCloseDriver("open");
                ds = _driver.Excute();
            }
            catch (Exception ex)
            {
                if (IsOnTransaction())
                {
                    _tran.Rollback();
                    _tran = null;
                }

                throw new Exception($" 執行 {sql} 發生錯誤 | 錯誤原因: " + Environment.NewLine + ex);
            }
            finally
            {
                OpenOrCloseDriver("close");
            }
            return ds;
        }

        /// <summary>
        /// 使用 ExecuteNonQuery()存取資料庫
        /// </summary>
        public int ExcuteNonQuery(string sql, CommandType commandType, SqlParameter[] parameters)
        {

            int count;
            var conn = _driver.Connection;
            _driver.Command.CommandText = sql;
            _driver.Command.Connection = conn;
            _driver.Command.CommandType = commandType;
            _driver.Command.CommandTimeout = _driver.TimeOut;

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    _driver.Command.Parameters.Add(param);
                };
            }
            try
            {
                OpenOrCloseDriver("open");
                count = _driver.ExcuteNonQuery();
            }
            catch (Exception ex)
            {
                if (IsOnTransaction())
                {
                    _tran.Rollback();
                    _tran = null;
                }

                throw new Exception($" 執行 {sql} 發生錯誤 | 錯誤原因: " + Environment.NewLine + ex);
            }
            finally
            {
                OpenOrCloseDriver("close");
            }

            return count;
        }

        /// <summary>
        /// 使用 ExcuteReader()存取資料庫
        /// 注意: 要手動使用 dr.Close(); 關閉原先的Connection
        /// </summary>
        public IDataReader ExcuteReader(string sql, CommandType commandType, SqlParameter[] parameters)
        {
            IDataReader reader;
            var conn = _driver.Connection;
            _driver.Command.CommandText = sql;
            _driver.Command.Connection = conn;
            _driver.Command.CommandType = commandType;
            _driver.Command.CommandTimeout = _driver.TimeOut;

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    _driver.Command.Parameters.Add(param);
                };
            }
            try
            {
                OpenOrCloseDriver("open");
                reader = _driver.ExcuteReader();
            }
            catch (Exception ex)
            {
                if (IsOnTransaction())
                {
                    _tran.Rollback();
                    _tran = null;
                }

                throw new Exception($" 執行 {sql} 發生錯誤 | 錯誤原因: " + Environment.NewLine + ex);
            }
            finally
            {
                // DataReader 物件在這裡不Close() 要手動使用
                // => dr.Close() 才會連同connection一同關閉
            }

            return reader;
        }



        private void OpenOrCloseDriver(string method = "open")
        {
            if (!IsOnTransaction())
            {
                if (method == "open")
                    _driver.Open();
                else if (method == "close")
                {
                    ClearAndCloseDriver();
                }
            }
        }
        private void ClearAndCloseDriver()
        {
            _driver.Command.Parameters.Clear();
            _driver.Command.Dispose();
            _driver.Command = null;
            _driver.Close();
        }
        private bool IsOnTransaction()
        {
            if (_tran == null)
            {
                return false;
            }
            return true;
        }
    }


}
