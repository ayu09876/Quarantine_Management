using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Quarantine_Management.Function;
using Quarantine_Management.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Quarantine_Management.Controllers
{
    public class CSQMController : Controller
    {
        private string DbConnection()
        {
            var dbAccess = new DatabaseAccessLayer();
            string dbString = dbAccess.ConnectionString;
            return dbString;
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Dashboard()
        {
            if (HttpContext.Session.GetString("roles") == "CSQM")
            {
                return View();
            }
            else
            {
                return RedirectToAction("SignOut", "Login");
            }
        }   
        
        public IActionResult MonitoringHistory()
        {
            if (HttpContext.Session.GetString("roles") == "CSQM")
            {
                return View();
            }
            else
            {
                return RedirectToAction("SignOut", "Login");
            }
        }  
        public IActionResult MyProfile()
        {
            if (HttpContext.Session.GetString("roles") == "CSQM")
            {
                return View();
            }
            else
            {
                return RedirectToAction("SignOut", "Login");
            }
        }
        public IActionResult ChangePassword()
        {
            if (HttpContext.Session.GetString("roles") == "CSQM")
            {
                var db = new DatabaseAccessLayer();

                var requestData = db.GetSESAID(HttpContext.Session.GetString("id") ?? "");

                if (requestData != null && requestData.Count > 0)
                {
                    ViewBag.sesa_id = requestData[0].sesa_id;
                }
                else
                {
                    ViewBag.sesa_id = "No SESA ID Available";
                }
                return View();
            }
            else
            {
                return RedirectToAction("SignOut", "Login");
            }
        }
        public IActionResult PendingApproval()
        {
            if (HttpContext.Session.GetString("roles") == "CSQM")
            {
                return View();
            }
            else
            {
                return RedirectToAction("SignOut", "Login");
            }
        }
        [HttpGet]
        public DashboardModel GetChartFinalStatus()
        {
            var db = new DatabaseAccessLayer();
            DashboardModel data = db.GetChartFinalStatus();

            return data;
        }
        [HttpGet]
        public IActionResult GetChartFinalStatusRequestDetail(string finalStatus)
        {
            var db = new DatabaseAccessLayer();
            List<RequestModel> data = db.GetChartFinalStatusRequestDetail(finalStatus);

            return PartialView("_TableChartFinalStatusDetail", data);
        }

        [HttpGet]
        public List<DashboardModel> GetChartPartNumber()
        {
            var db = new DatabaseAccessLayer();
            List<DashboardModel> data = db.GetChartPartNumber();
            return data;
        }


        [HttpGet]
        public IActionResult GetChartPartDetail(string reference)
        {
            var db = new DatabaseAccessLayer();
            List<RequestModel> data = db.GetChartPartDetail(reference);

            return PartialView("_TableChartDetail", data);
        }

        [HttpGet]
        public List<DashboardModel> GetChartSourceIssue()
        {
            var db = new DatabaseAccessLayer();
            List<DashboardModel> data = db.GetChartSourceIssue();
            return data;
        }

        [HttpGet]
        public IActionResult GetChartSourceIssueDetail(string source_issue)
        {
            var db = new DatabaseAccessLayer();
            List<RequestModel> data = db.GetChartSourceIssueDetail(source_issue);

            return PartialView("_TableChartDetail", data);
        }
        [HttpGet]
        public List<DashboardModel> GetChartRequestor()
        {
            var db = new DatabaseAccessLayer();
            List<DashboardModel> data = db.GetChartRequestor();
            return data;
        }

        [HttpGet]
        public IActionResult GetChartRequestDetail(string requestor)
        {
            var db = new DatabaseAccessLayer();
            List<RequestModel> data = db.GetChartRequestDetail(requestor);

            return PartialView("_TableChartDetail", data);
        }

        [HttpGet]
        public IActionResult GetChartRequestList()
        {
            var db = new DatabaseAccessLayer();
            List<RequestModel> data = db.GetChartRequestList();

            return PartialView("_TableChartDetail", data);
        }

        [HttpGet]
        public IActionResult GetChartPartList()
        {
            var db = new DatabaseAccessLayer();
            List<RequestModel> data = db.GetChartPartList();

            return PartialView("_TablePartDetail", data);
        }

        [HttpPost]
        public IActionResult GetFilterDate()
        {
            var db = new DatabaseAccessLayer();
            List<DateDataModel> data = db.GetFilterDate();

            return Json(data);
        }
        [HttpGet]
        public IActionResult GetPendingApprovalData(string date_from, string date_to)
        {
            var db = new DatabaseAccessLayer();
            string csqm = HttpContext.Session.GetString("sesa_id") ?? "";
            List<RequestModel> data = db.GetPendingApprovalData(date_from, date_to, csqm);

            return PartialView("_TablePendingApproval", data);
        }

        [HttpPost]
        public IActionResult ApproveRequest(string id_req, string status, string verify_coment)
        {
            //if (HttpContext.Session.GetString("level") == "csqm")
            //{
            string approver = HttpContext.Session.GetString("sesa_id") ?? "";
            var db = new DatabaseAccessLayer();
            int rowsAffected = db.ApproveRequest(id_req, status, verify_coment, approver);
            return Json(rowsAffected);
           
        }
        [HttpGet]
        public IActionResult GetDetailPendingApprovalUpdate(string id_req)
        {
            var db = new DatabaseAccessLayer();
            RequestModel data = db.GetDetailPendingApprovalUpdate(id_req);
            return PartialView("_TableRequestDetail", data);
        }

        [HttpGet]
        public IActionResult GetDetailUpdatedDeclined(string id_req)
        {
            var db = new DatabaseAccessLayer();
            RequestModel data = db.GetDetailUpdatedDeclined(id_req);
            return PartialView("_TableRequestDetail", data);
        }

        [HttpPost]
        public IActionResult ChangePassword(string usr_sesa, string usr_id, string usr_password)
        {
            Console.WriteLine($"Received data: usr_id={usr_id}, usr_password={usr_password}");
            var db = new DatabaseAccessLayer();
            bool status = db.ChangePassword(usr_id, usr_password);

            return Json(status);
        }
        [HttpGet]
        public IActionResult GetAllDataHistory(string date_from, string date_to)
        {
            var db = new DatabaseAccessLayer();
            List<RequestModel> data = db.GetAllDataHistory(date_from, date_to);

            return PartialView("_TableRequestHistory", data);
        }
        [HttpGet]
        public IActionResult CountPendingApproval()
        {
            var db = new DatabaseAccessLayer();
            string usr_sesa = HttpContext.Session.GetString("sesa_id") ?? "";
            int count = db.GetCountPendingApproval(usr_sesa);

            return Json(new { count = count });
        }

        [HttpGet]
        public IActionResult CountRequestHistory()
        {
            var db = new DatabaseAccessLayer();
            int count = db.GetCountRequestHistory();

            return Json(new { count = count });
        }
        [HttpGet]
        public IActionResult DownlodRequesHistory(string date_from, string date_to)
        {
            using (XLWorkbook wb = new XLWorkbook())
            {
                DateTime currentDateTime = DateTime.Now;
                string formattedDateTime = currentDateTime.ToString("yyyyMMddHHmmss");
                wb.Worksheets.Add(this.GetDataDownloadHistory(date_from, date_to).Tables[0]);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                               "SEMB QUARANTINE MANAGEMENT - REQUEST HISTORY" + formattedDateTime + ".xlsx");
                }
            }
        }

        private DataSet GetDataDownloadHistory(string date_from, string date_to)
        {
            string query = "GET_REQUEST_HISTORY";
            DataSet ds = new DataSet();

            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@date_from", date_from);
                    cmd.Parameters.AddWithValue("@date_to", date_to);
                    cmd.Connection = conn;
                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        sda.Fill(ds);
                    }
                }
            }
            return ds;
        }
    }
}
