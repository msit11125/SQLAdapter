using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBAccess
{
    /// <summary>
    /// 建立連線DB方式介面
    /// </summary>
    public abstract class DbAbstractDriver
    {

        /// <summary>
        /// 連線位置
        /// </summary>
        public virtual string ConnectString { get; set; }
        /// <summary>
        /// 逾時時間
        /// </summary>
        public virtual int TimeOut { get; set; }
        public abstract IDbConnection Connection { get; set; }
        public abstract IDbCommand Command { get; set; }


    }
}
