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
    /// <summary>
    /// 用來建立與SQL連線的元件工具
    /// </summary>
    public class DbEngineAdapter : IDbEngineAdapter
    {
        // 讀取 xml 並建立工廠
        static SQLAccessFactory SQLFactory = new SQLAccessFactory(
            Path.Combine(Path.GetDirectoryName(Assembly.GetAssembly(typeof(DbEngineAdapter)).CodeBase),"DbDrivers.xml")
            );


        #region 內建靜態 Factory
        /// <summary>
        /// 建立SQL Server 連線DB
        /// </summary>
        /// <param name="connectString">連線位置</param>
        /// <param name="timeOut">逾時時間(預設120秒)</param>
        /// <returns></returns>
        public static IDbEngineAdapter CreateSQLServerDB(string connectString ,int timeOut = 120)
        {
            var driver = (IDbDriver)SQLFactory.CreateDriver("SQLSERVER", "singleton");
            driver.TimeOut = timeOut;
            driver.ConnectString = connectString;
            return new DbEngineAdapter(driver);
        }


        #endregion



        private IDbDriver _driver;  // 連線的元件 (SQLSERVER or ORACLE or SQLLITE...)
        private DbEngineAdapter(IDbDriver driver)
        {
            this._driver = driver;
        }


        /// <summary>
        /// 使用 SqlDataAdapter 存取資料庫
        /// </summary>
        public DataSet Excute(string sql, CommandType commandType,params SqlParameter[] parameters)
        {
            DataSet ds;
            try
            {
                ds = this._driver.ExcuteSql_UseAdapter(sql, commandType, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($" 執行 {sql} 發生錯誤 | 錯誤原因: " + Environment.NewLine + ex);
            }


            return ds;
        }





        /// <summary>
        /// 使用 ExecuteNonQuery()存取資料庫
        /// </summary>
        public int ExcuteNonQuery(string sql, CommandType commandType, params SqlParameter[] parameters)
        {

            int count;
            try
            {
                count = this._driver.ExcuteSql_NonQuery(sql, commandType, parameters);

            }
            catch (Exception ex)
            {
                throw new Exception($" 執行 {sql} 發生錯誤 | 錯誤原因: " + Environment.NewLine + ex);
            }


            return count;
        }

        /// <summary>
        /// 使用 ExcuteReader()存取資料庫
        /// 注意: 要手動使用 dr.Close(); 關閉原先的Connection
        /// </summary>
        public IDataReader ExcuteReader(string sql, CommandType commandType, params SqlParameter[] parameters)
        {
            IDataReader reader;
            try
            {
                reader = this._driver.ExcuteReader(sql, commandType, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($" 執行 {sql} 發生錯誤 | 錯誤原因: " + Environment.NewLine + ex);
            }

            return reader;
        }



    }






}
