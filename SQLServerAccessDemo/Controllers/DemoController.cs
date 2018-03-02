using DBAccess;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SQLServerAccessDemo.Controllers
{
    public class DemoController : Controller
    {
        // 一頁 Take 筆數
        private static readonly int pageRow = 30; 
        // 預設連線位置 (按鈕 [取得資料庫] 可變動)
        private string connectString = ConfigurationManager.AppSettings["connectionString"];

        public ActionResult Index()
        {
            ViewBag.connectString = this.connectString;
            return View();
        }

        /// <summary>
        /// 查詢SQL結果頁面
        /// </summary>
        [HttpGet]
        public ActionResult Result(int? page, int? totalPage)
        {
            if (Session["tempTable"] != null)
            {
                DataTable dt = (DataTable)Session["tempTable"];

                IEnumerable<DataRow> enumerableDt = new List<DataRow>();
                if (dt != null)
                {
                    //DataSet 做分頁動作
                    if (dt.Rows.Count > 0)
                    {
                        enumerableDt = dt.AsEnumerable()
                        .Skip(((page ?? 1) - 1) * pageRow)
                        .Take(pageRow);
                    }
                    if (totalPage == null)
                        totalPage = (dt.Rows.Count % pageRow == 0) ? (int)(dt.Rows.Count / pageRow) : (int)(dt.Rows.Count / pageRow) + 1; //總頁數

                    ViewBag.TotalPage = totalPage;
                    ViewBag.TotalRow = dt.Rows.Count;
                    ViewBag.NowPage = page?? 1; // 現在的頁數 null:為第一頁

                }
                DataTable tableNew = dt;
                if (enumerableDt.Any()) // 先確認有DataRow 否則會Exception
                    tableNew = enumerableDt.CopyToDataTable();


                return View(tableNew);
            }

            return View();
        }


        /// <summary>
        /// 查詢SQL Submit送出到此
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Result(PostResource resource)
        {
            // *** 步驟(1) 建立SQL Server 的 Adapter
            DbEngineAdapter db = new DbEngineAdapter(connectString,resource.TimeOut);
            // *** 步驟(2-1) 建立SQL ParameterBuilder 
            var builder = new SQLParameterBuilder();

            if (resource.SqlParameterResourceList != null)
            {
                // 迴圈 加入SQL參數
                foreach (var parameter in resource.SqlParameterResourceList)
                {
                    string pName = parameter?.Name.FirstOrDefault() == '@' ? parameter?.Name : '@' + parameter?.Name;

                    if (pName == null)
                    {
                        // Model State Set Error ...
                        return View();
                    }

                    // *** 步驟(2-2) Add Input 或 Output 參數
                    if (parameter.Direction.ToLower() == "input")
                        builder.Add_Input_Parameter(pName, parameter.Value, parameter.SqlDbType);
                    else if (parameter.Direction.ToLower() == "output")
                        if (parameter.Scale == 0 && parameter.Precision == 0)
                            builder.Add_Output_Parameter(pName, parameter.SqlDbType, parameter.Size);// 無精確度
                        else
                            builder.Add_Output_Parameter(pName, parameter.SqlDbType, parameter.Precision, parameter.Scale, parameter.Size);// 有精確度
                }
            }
            // *** 步驟(2-3) 將builder 轉成 SqlParameter 陣列
            SqlParameter[] pArrar = builder.ToArray();

            // *** 步驟(3) 執行 SQL 敘述 => 丟入 Sql 或 預存程序名稱 、 CommandType 、 SqlParameter[]
            db.OpenConn(); //開啟連線
            var ds = db.Excute(
                resource.Sql,
                resource.CommandType,
                pArrar
            );
            db.CloseConn(); //關閉連線

            // *** 步驟(4) (非必要) 可將SqlParameter轉Class
            //                     或是DataSet內的Table轉List<>

            // IList<YourClass> list = bag.DataSet.Tables[0].ToList<YourClass>();
            // YourClass para = bag.OutputParameters.ToClass<YourClass>();
            // .....

            // 先清除舊的 Session
            Session["tempParameters"] = null;
            Session["tempTable"] = null;

            DataTable dt = null;
            if (ds.Tables.Count > 0)
            {
                dt = ds.Tables[0];
            }

            if (pArrar.Any(p => p.Direction == ParameterDirection.Output))
            {
                Session["tempParameters"] = pArrar;
            }

            Session["tempTable"] = dt;

            DataTable tableNew = dt;
            //DataSet 做分頁動作
            if (tableNew != null)
            {
                if (tableNew.Rows.Count > 0)
                {
                    tableNew = dt.AsEnumerable()
                       .Skip(0)
                       .Take(pageRow)
                       .CopyToDataTable();
                }

                ViewBag.TotalPage = (dt.Rows.Count % pageRow == 0) ? (int)(dt.Rows.Count / pageRow) : (int)(dt.Rows.Count / pageRow) + 1; //總頁數
                ViewBag.TotalRow = dt.Rows.Count;
            }



            return View(tableNew);

        }



        /// <summary>
        /// 取得DB列表
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult GetDataBaseList()
        {
            // *** 步驟(1) 建立SQL Server 的 Adapter
            DbEngineAdapter db = new DbEngineAdapter(connectString);
            // *** 步驟(3) 執行 SQL 敘述 => 丟入 Sql 或 預存程序名稱 、 CommandType 、 SqlParameter[]
            db.OpenConn();
            var ds = db.Excute(
                @"
                  SELECT name FROM master.dbo.sysdatabases 
                  WHERE name not in ('master','tempdb','model','msdb')
                ",  /* 依照系統DB做排除 'master','tempdb','model','msdb' */
                CommandType.Text,
                null
            );
            db.CloseConn();

            var nameList = ds.Tables[0].ToList<Names>();

            return Json(nameList);
        }


        /// <summary>
        /// 取得Table、View、SP列表
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult GetTableViewAndSPList(string dbName)
        {
            // *** 步驟(1) 建立SQL Server 的 Adapter
            DbEngineAdapter db = new DbEngineAdapter(connectString);
            // *** 步驟(3) 執行 SQL 敘述 => 丟入 Sql 或 預存程序名稱 、 CommandType 、 SqlParameter[]
            var dsTable = db.Excute(
                $@"SELECT (TABLE_NAME) AS name FROM
                               {dbName}.INFORMATION_SCHEMA.TABLES
                               WHERE TABLE_TYPE = 'BASE TABLE'
                               ORDER BY TABLE_NAME",
                CommandType.Text,
                null
            );

            db.OpenConn();
            var dsView = db.Excute(
                $"SELECT (TABLE_NAME) AS name FROM {dbName}.INFORMATION_SCHEMA.VIEWS ORDER BY TABLE_NAME",
                CommandType.Text,
                null
            );


            var dsSP = db.Excute(
                $@"
                    SELECT (Routine_Name) as Name
                    FROM {dbName}.INFORMATION_SCHEMA.ROUTINES
                    WHERE ROUTINE_TYPE = 'PROCEDURE' AND Left(Routine_Name, 3) NOT IN ('sp_', 'xp_', 'ms_')
                    ORDER BY SPECIFIC_NAME
                ",
                CommandType.Text,
                null
            );
            db.CloseConn();


            // *** 步驟(4) 轉成List
            var viewList = dsView.Tables[0].ToList<Names>();
            var spList = dsSP.Tables[0].ToList<Names>();
            var tableList = dsTable.Tables[0].ToList<Names>();


            return Json(new ViewAndSP()
            {
                Tables = tableList,
                Views = viewList,
                SPs = spList
            });
        }


        /// <summary>
        /// 取得SP參數
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult GetSpNeedParameters(string dbName, string spName)
        {
            // *** 步驟(1) 建立SQL Server 的 Adapter
            DbEngineAdapter db = new DbEngineAdapter(connectString);
            // *** 步驟(2-1) 建立SQL ParameterBuilder 
            var builder = new SQLParameterBuilder();
            builder.Add_Input_Parameter("@spName", spName, SqlDbType.VarChar);

            // *** 步驟(2-2) Add Input 或 Output 參數
            SqlParameter[] pArray = builder.ToArray();

            // *** 步驟(3) 執行 SQL 敘述 => 丟入 Sql 或 預存程序名稱 、 CommandType 、 SqlParameter[]
            db.OpenConn();
            var ds = db.Excute(
                   $@" 
                    SELECT 
                        PARAMETER_NAME AS Parameter,
                        DATA_TYPE AS Type,
                        (case when PARAMETER_MODE='IN' then 0 when PARAMETER_MODE='INOUT' then 1 end) AS OutPut,   
                        ISNULL(CHARACTER_MAXIMUM_LENGTH,0) AS Size,
                        ISNULL(NUMERIC_PRECISION,0) AS Precision,
                        ISNULL(NUMERIC_SCALE,0) AS Scale
                    FROM {dbName}.INFORMATION_SCHEMA.PARAMETERS
                    WHERE SPECIFIC_NAME = @spName
                    order by OutPut,Parameter
                    ",
                   CommandType.Text,
                   pArray
               );
            db.CloseConn();

            // *** 步驟(4) 轉成List
            var pList = ds.Tables[0].ToList<ParameterAndType>();

            return Json(pList);
        }


        /// <summary>
        /// 查詢SP或View的Code
        /// </summary>
        /// <param name="Type"> P: StoreProcedure  V: View </param>
        /// <returns></returns>
        public ActionResult SPOrViewCodes(string dbName, string spOrViewName, char type)
        {
            // *** 步驟(1) 建立SQL Server 的 Adapter
            DbEngineAdapter db = new DbEngineAdapter(connectString);

            // *** 步驟(2-1) 建立SQL ParameterBuilder 
            var builder = new SQLParameterBuilder();
            // *** 步驟(2-2) Add Input 或 Output 參數
            builder.Add_Input_Parameter("@name", spOrViewName, SqlDbType.VarChar);
            builder.Add_Input_Parameter("@type", type, SqlDbType.VarChar);
            // *** 步驟(2-3) 將builder 轉成 SqlParameter 陣列
            SqlParameter[] pArray = builder.ToArray();
            // *** 步驟(3) 執行 SQL 敘述 => 丟入 Sql 或 預存程序名稱 、 CommandType 、 SqlParameter[]
            DataSet ds = null;
            db.OpenConn();
            switch (type)
            {
                case 'P': //SP
                    ds = db.Excute(
                        $@" 
                             SELECT ROUTINE_DEFINITION AS definition
                             FROM {dbName}.INFORMATION_SCHEMA.ROUTINES
                             WHERE SPECIFIC_NAME = @name
                         ",
                        CommandType.Text,
                         pArray
                    );

                    break;
                case 'V': //SP
                    ds = db.Excute(
                        $@" 
                             SELECT VIEW_DEFINITION AS definition
                             FROM {dbName}.INFORMATION_SCHEMA.VIEWS
                             WHERE TABLE_NAME = @name
                         ",
                        CommandType.Text,
                         pArray
                    );
                    break;
            }
            db.CloseConn();

            string codes = "<span style='color:red'>查無資料</span>";
            if (ds != null && ds.Tables[0].Rows.Count > 0)
            {
                codes = ds.Tables[0].Rows[0]["definition"].ToString().Replace("\n", "<br />");
            }
            ViewBag.Code = codes;
            return View();
        }



        /// <summary>
        /// 例外 Handler 回傳錯誤Json
        /// </summary>
        /// <param name="filterContext"></param>
        protected override void OnException(ExceptionContext filterContext)
        {
            if (filterContext.Exception != null)
            {
                filterContext.HttpContext.Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;
                filterContext.HttpContext.Response.TrySkipIisCustomErrors = true; // <= 解決IIS回傳Html
                filterContext.Result = new JsonResult()
                {
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                    Data = new
                    {
                        Error = filterContext.Exception.Message
                    }
                };
                filterContext.ExceptionHandled = true;
            }

        }

    }







    #region Resource資源 (Model)

    public class PostResource
    {
        public string Sql { get; set; }
        public CommandType CommandType { get; set; }
        public int TimeOut { get; set; }
        public List<SqlParameterResource> SqlParameterResourceList { get; set; }
    }
    public class SqlParameterResource
    {
        public string Direction { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public SqlDbType SqlDbType { get; set; }
        public byte Precision { get; set; }
        public byte Scale { get; set; }
        public int Size { get; set; }

    }



    public class ParameterAndType
    {
        public string Parameter { get; set; }
        public string Type { get; set; }
        public int OutPut { get; set; }
        public int Size { get; set; }
        public int Precision { get; set; }
        public int Scale { get; set; }

    }
    public class ViewAndSP
    {
        public List<Names> Tables { get; set; }
        public List<Names> Views { get; set; }
        public List<Names> SPs { get; set; }

    }

    public class Names
    {
        public string Name { get; set; }
    }
    #endregion
}