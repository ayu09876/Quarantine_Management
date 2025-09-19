using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Quarantine_Management.Function;
using Quarantine_Management.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using DocumentFormat.OpenXml.Drawing;

namespace Quarantine_Management.Controllers
{
    public class AdminController : Controller
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
            if (HttpContext.Session.GetString("roles") == "Admin")
            {
                return View();
            }
            else
            {
                return RedirectToAction("SignOut", "Login");
            }
        } 
        
        public IActionResult StorageLocation()
        {
            if (HttpContext.Session.GetString("roles") == "Admin")
            {
                return View();
            }
            else
            {
                return RedirectToAction("SignOut", "Login");
            }
        }    
        
        public IActionResult Reference()
        {
            if (HttpContext.Session.GetString("roles") == "Admin")
            {
                return View();
            }
            else
            {
                return RedirectToAction("SignOut", "Login");
            }
        }
        public IActionResult Issue()
        {
            if (HttpContext.Session.GetString("roles") == "Admin")
            {
                return View();
            }
            else
            {
                return RedirectToAction("SignOut", "Login");
            }
        }
        public IActionResult Disposition()
        {
            if (HttpContext.Session.GetString("roles") == "Admin")
            {
                return View();
            }
            else
            {
                return RedirectToAction("SignOut", "Login");
            }
        }
        public IActionResult Remark()
        {
            if (HttpContext.Session.GetString("roles") == "Admin")
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
            if (HttpContext.Session.GetString("roles") == "Admin")
            {
                return View();
            }
            else
            {
                return RedirectToAction("SignOut", "Login");
            }
        }
        public IActionResult UserManagement()
        {
            if (HttpContext.Session.GetString("roles") == "Admin")
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
            if (HttpContext.Session.GetString("roles") == "Admin")
            {
                return View();
            }
            else
            {
                return RedirectToAction("SignOut", "Login");
            }
        }
        
        public IActionResult ColumnMasterData()
        {
            if (HttpContext.Session.GetString("roles") == "Admin")
            {
                return View();
            }
            else
            {
                return RedirectToAction("SignOut", "Login");
            }
        }
        
        public IActionResult RackMasterData()
        {
            if (HttpContext.Session.GetString("roles") == "Admin")
            {
                return View();
            }
            else
            {
                return RedirectToAction("SignOut", "Login");
            }
        }
        
        public IActionResult RowMasterData()
        {
            if (HttpContext.Session.GetString("roles") == "Admin")
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
            if (HttpContext.Session.GetString("roles") == "Admin")
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

        public IActionResult Status()
        {
            if (HttpContext.Session.GetString("roles") == "Admin")
            {
                return View();
            }
            else
            {
                return RedirectToAction("SignOut", "Login");
            }
        }
          
        public IActionResult ApproverMaster()
        {
            if (HttpContext.Session.GetString("roles") == "Admin")
            {
                return View();
            }
            else
            {
                return RedirectToAction("SignOut", "Login");
            }
        }  
        public IActionResult ApprovalRouting()
        {
            if (HttpContext.Session.GetString("roles") == "Admin")
            {
                return View();
            }
            else
            {
                return RedirectToAction("SignOut", "Login");
            }
        }

        [HttpPost]
        public IActionResult GetFilterDate()
        {
            var db = new DatabaseAccessLayer();
            List<DateDataModel> data = db.GetFilterDate();

            return Json(data);
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
        [HttpGet]
        public IActionResult GetAllDataHistory(string date_from, string date_to)
        {
            var db = new DatabaseAccessLayer();
            List<RequestModel> data = db.GetAllDataHistory(date_from, date_to);

            return PartialView("_TableRequestHistory", data);
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
        [HttpGet]
        public IActionResult GetDetailPendingApprovalUpdate(string id_req)
        {
            var db = new DatabaseAccessLayer();
            RequestModel data = db.GetDetailPendingApprovalUpdate(id_req);
            return PartialView("_TableRequestDetail", data);
        }
        [HttpGet]
        public IActionResult GetAllDataUser(string name, string roles)
        {
            try
            {
                var db = new DatabaseAccessLayer();
                List<LoginModel> dataList = db.GetAllDataUser(name, roles);
                return PartialView("_TableUser", dataList);
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error in GetAllDataUser: {ex.Message}");
                // Return a more graceful error response
                return StatusCode(500, "An error occurred while retrieving user data.");
            }
        }
        [HttpGet]
        public IActionResult GetNameFilter(string cell)
        {
            var db = new DatabaseAccessLayer();
            List<SelectModel> data = db.GetNameFilter(cell);

            return Json(new { items = data });
        }  
        
        [HttpGet]
        public IActionResult GetRoleFilter(string cell)
        {
            var db = new DatabaseAccessLayer();
            List<SelectModel> data = db.GetRoleFilter(cell);

            return Json(new { items = data });
        }  
        [HttpGet]
        public IActionResult GetLevelFilter(string cell)
        {
            var db = new DatabaseAccessLayer();
            List<SelectModel> data = db.GetLevelFilter(cell);

            return Json(new { items = data });
        }
        [HttpPost]
        public IActionResult AddNewUser(string sesa_id, string name, string email, string password, string role, string level)
        {
            var db = new DatabaseAccessLayer();
            List<LoginModel> data = db.AddNewUserDataList(sesa_id, name, email, password, role, level);

            return Json(new { items = data });
        }

        [HttpGet]
        public IActionResult GetUserDataDetail(string id_user)
        {
            var db = new DatabaseAccessLayer();
            List<LoginModel> data = db.GetUserDataDetail(id_user);
            return Json(new { items = data });
        }

        [HttpPost]
        public IActionResult UpdateDataUser(string id_user, string sesa_id, string name, string email, string role, string level)
        {
            var db = new DatabaseAccessLayer();
            bool success = db.UpdateDataUser(id_user, sesa_id, name, email, role, level);

            return Json(new { success = success, message = success ? "Data updated successfully" : "Failed to update data" });
        }
        [HttpPost]
        public IActionResult DeleteDataUser(int id_user)
        {
            var db = new DatabaseAccessLayer();
            List<LoginModel> data = db.DeleteUserDataList(id_user);
            return Json(new { success = data.Count > 0 });
        }
        [HttpGet] // Ubah dari HttpPost ke HttpGet karena JavaScript Anda menggunakan window.location.href
        public IActionResult DownloadDataUser(string name, string role)
        {
            using (XLWorkbook wb = new XLWorkbook())
            {
                DateTime currentDateTime = DateTime.Now;
                string formattedDateTime = currentDateTime.ToString("yyyyMMddHHmmss");
                wb.Worksheets.Add(this.GetUserDataDetail(name, role).Tables[0]);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                               "SEMB QUARANTINE MANAGEMENT - Data User" + formattedDateTime + ".xlsx");
                }
            }
        }

        private DataSet GetUserDataDetail(string name, string role)
        {
            string query = "GET_DATA_USER_QAS";
            DataSet ds = new DataSet();

            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@usr_name", name);
                    cmd.Parameters.AddWithValue("@roles", role);
                    cmd.Connection = conn;
                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        sda.Fill(ds);
                    }
                }
            }
            return ds;
        }

        [HttpGet]
        public IActionResult GetAllDataSloc(string sloc)
        {
            var db = new DatabaseAccessLayer();
            List<SlocModel> dataList = db.GetAllDataSloc(sloc);
            return PartialView("_TableSloc", dataList);
        }

        [HttpPost]
        public IActionResult AddNewSloc(string sloc, string sloc_detail, string description, string plant)
        {
            var db = new DatabaseAccessLayer();
            List<SlocModel> data = db.AddNewSloc(sloc, sloc_detail, description, plant);
            return Json(new { items = data });
        }

        [HttpPost]
        public IActionResult UpdateDataSloc(string id, string sloc, string sloc_detail, string description, string plant)
        {
            var db = new DatabaseAccessLayer();
            bool success = db.UpdateDataSloc(id, sloc, sloc_detail, description, plant);

            return Json(new { success = success, message = success ? "Data updated successfully" : "Failed to update data" });
        }
        [HttpGet]
        public IActionResult GetAllDataSlocDetail(string id)
        {
            var db = new DatabaseAccessLayer();
            List<SlocModel> data = db.GetAllDataSlocDetail(id);
            return Json(new { items = data });
        }

        [HttpPost]
        public IActionResult DeleteDataSloc(string id)
        {
            var db = new DatabaseAccessLayer();
            List<LoginModel> data = db.DeleteDataSloc(id);
            return Json(new { success = data.Count > 0 });
        }
        [HttpGet]
        public IActionResult GetSlocFilter(string cell)
        {
            var db = new DatabaseAccessLayer();
            List<SelectModel> data = db.GetSlocFilter(cell);

            return Json(new { items = data });
        }

        [HttpGet] // Ubah dari HttpPost ke HttpGet karena JavaScript Anda menggunakan window.location.href
        public IActionResult DownloadDataSloc(string sloc)
        {
            using (XLWorkbook wb = new XLWorkbook())
            {
                DateTime currentDateTime = DateTime.Now;
                string formattedDateTime = currentDateTime.ToString("yyyyMMddHHmmss");
                wb.Worksheets.Add(this.GetSlocDataDetail(sloc).Tables[0]);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                               "SEMB QUARANTINE MANAGEMENT - Data Sloc" + formattedDateTime + ".xlsx");
                }
            }
        }
        private DataSet GetSlocDataDetail(string sloc)
        {
            string query;
            if (string.IsNullOrEmpty(sloc))
            {
                query = "SELECT * FROM mst_sloc_QAS ORDER BY sloc DESC";
            }
            else
            {
                query = "SELECT * FROM mst_sloc_QAS WHERE sloc LIKE @sloc ORDER BY sloc DESC";
            }

            DataSet ds = new DataSet();

            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {
                    cmd.CommandType = CommandType.Text;
                    if (!string.IsNullOrEmpty(sloc))
                    {
                        cmd.Parameters.AddWithValue("@sloc", "%" + sloc + "%");
                    }
                    cmd.Connection = conn;
                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        sda.Fill(ds);
                    }
                }
            }
            return ds;
        }
        [HttpGet]
        public IActionResult GetreferenceFilter(string cell)
        {
            var db = new DatabaseAccessLayer();
            List<SelectModel> data = db.GetreferenceFilter(cell);

            return Json(new { items = data });
        }

        [HttpGet]
        public IActionResult GetAllDataReference(string reference)
        {
            var db = new DatabaseAccessLayer();
            List<ReferenceModel> dataList = db.GetAllDataReference(reference);
            return PartialView("_TableReference", dataList);
        }
        [HttpPost]
        public IActionResult AddNewReference(string reference)
        {
            var db = new DatabaseAccessLayer();
            string sesa_id = HttpContext.Session.GetString("sesa_id") ?? "";
            List<ReferenceModel> data = db.AddNewReference(reference, sesa_id);
            return Json(new { items = data });
        }
        [HttpGet]
        public IActionResult GetAllDataReferenceDetail(string id)
        {
            var db = new DatabaseAccessLayer();
            List<ReferenceModel> data = db.GetAllDataReferenceDetail(id);
            return Json(new { items = data });
        }
        [HttpPost]
        public IActionResult UpdateDataReference(string id, string reference)
        {
            var db = new DatabaseAccessLayer();
            string sesa_id = HttpContext.Session.GetString("sesa_id") ?? "";
            bool success = db.UpdateDataReference(id, reference, sesa_id);

            return Json(new { success = success, message = success ? "Data updated successfully" : "Failed to update data" });
        }
        [HttpPost]
        public IActionResult DeleteDataReference(string id)
        {
            var db = new DatabaseAccessLayer();
            List<ReferenceModel> data = db.DeleteDataReference(id);
            return Json(new { success = data.Count > 0 });
        }
        [HttpGet] 
        public IActionResult DownloadDataReference(string reference)
        {
            using (XLWorkbook wb = new XLWorkbook())
            {
                DateTime currentDateTime = DateTime.Now;
                string formattedDateTime = currentDateTime.ToString("yyyyMMddHHmmss");
                wb.Worksheets.Add(this.GetReferenceDataDetail(reference).Tables[0]);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                               "SEMB QUARANTINE MANAGEMENT - Data Reference" + formattedDateTime + ".xlsx");
                }
            }
        }
        private DataSet GetReferenceDataDetail(string reference)
        {
            string query;
            if (string.IsNullOrEmpty(reference))
            {
                query = "SELECT * FROM mst_reference_QAS ORDER BY reference DESC";
            }
            else
            {
                query = "SELECT * FROM mst_reference_QAS WHERE reference LIKE @reference ORDER BY reference DESC";
            }

            DataSet ds = new DataSet();

            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {
                    cmd.CommandType = CommandType.Text;
                    if (!string.IsNullOrEmpty(reference))
                    {
                        cmd.Parameters.AddWithValue("@reference", "%" + reference + "%");
                    }
                    cmd.Connection = conn;
                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        sda.Fill(ds);
                    }
                }
            }
            return ds;
        }
        [HttpGet]
        public IActionResult GetStatusFilter(string cell)
        {
            var db = new DatabaseAccessLayer();
            List<SelectModel> data = db.GetStatusFilter(cell);

            return Json(new { items = data });
        }
        [HttpGet]
        public IActionResult GetAllDataStatus(string status)
        {
            var db = new DatabaseAccessLayer();
            List<StatusModel> dataList = db.GetAllDataStatus(status);
            return PartialView("_TableStatus", dataList);
        }
        [HttpPost]
        public IActionResult AddNewStatus(string status)
        {
            var db = new DatabaseAccessLayer();
            string sesa_id = HttpContext.Session.GetString("sesa_id") ?? "";
            List<StatusModel> data = db.AddNewStatus(status, sesa_id);
            return Json(new { items = data });
        }
        [HttpGet]
        public IActionResult GetAllDatastatusDetail(string id)
        {
            var db = new DatabaseAccessLayer();
            List<StatusModel> data = db.GetAllDatastatusDetail(id);
            return Json(new { items = data });
        }
        [HttpPost]
        public IActionResult UpdateDataStatus(string id, string status)
        {
            var db = new DatabaseAccessLayer();
            string sesa_id = HttpContext.Session.GetString("sesa_id") ?? "";
            bool success = db.UpdateDataStatus(id, status, sesa_id);

            return Json(new { success = success, message = success ? "Data updated successfully" : "Failed to update data" });
        }
        [HttpPost]
        public IActionResult DeleteDataStatus(string id)
        {
            var db = new DatabaseAccessLayer();
            List<StatusModel> data = db.DeleteDataStatus(id);
            return Json(new { success = data.Count > 0 });
        }
        [HttpGet] 
        public IActionResult DownloadDataStatus(string status)
        {
            using (XLWorkbook wb = new XLWorkbook())
            {
                DateTime currentDateTime = DateTime.Now;
                string formattedDateTime = currentDateTime.ToString("yyyyMMddHHmmss");
                wb.Worksheets.Add(this.GetStatusDataDetail(status).Tables[0]);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                               "SEMB QUARANTINE MANAGEMENT - Data Status" + formattedDateTime + ".xlsx");
                }
            }
        }
        private DataSet GetStatusDataDetail(string status)
        {
            string query;
            if (string.IsNullOrEmpty(status))
            {
                query = "SELECT * FROM mst_status ORDER BY status DESC";
            }
            else
            {
                query = "SELECT * FROM mst_status WHERE status LIKE @status ORDER BY status DESC";
            }

            DataSet ds = new DataSet();

            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {
                    cmd.CommandType = CommandType.Text;
                    if (!string.IsNullOrEmpty(status))
                    {
                        cmd.Parameters.AddWithValue("@status", "%" + status + "%");
                    }
                    cmd.Connection = conn;
                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        sda.Fill(ds);
                    }
                }
            }
            return ds;
        }
        [HttpGet]
        public IActionResult GetIssueSourceFilter(string cell)
        {
            var db = new DatabaseAccessLayer();
            List<SelectModel> data = db.GetIssueSourceFilter(cell);

            return Json(new { items = data });
        }
        [HttpGet]
        public IActionResult GetAllDataIssue(string issue_source)
        {
            var db = new DatabaseAccessLayer();
            List<IssueModel> dataList = db.GetAllDataIssue(issue_source);
            return PartialView("_TableIssue", dataList);
        }
        [HttpPost]
        public IActionResult AddNewIssue(string issue_source, string issue_category)
        {
            var db = new DatabaseAccessLayer();
            string sesa_id = HttpContext.Session.GetString("sesa_id") ?? "";
            List<IssueModel> data = db.AddNewIssue(issue_source, issue_category,sesa_id);
            return Json(new { items = data });
        }
        [HttpGet]
        public IActionResult GetAllDataIssueDetail(string id)
        {
            var db = new DatabaseAccessLayer();
            List<IssueModel> data = db.GetAllDataIssueDetail(id);
            return Json(new { items = data });
        }
        [HttpPost]
        public IActionResult UpdateDataIssue(string id, string issue_source, string issue_category)
        {
            var db = new DatabaseAccessLayer();
            string sesa_id = HttpContext.Session.GetString("sesa_id") ?? "";
            bool success = db.UpdateDataIssue(id, issue_source, issue_category, sesa_id);

            return Json(new { success = success, message = success ? "Data updated successfully" : "Failed to update data" });
        }
        [HttpPost]
        public IActionResult DeleteDataIssue(string id)
        {
            var db = new DatabaseAccessLayer();
            List<IssueModel> data = db.DeleteDataIssue(id);
            return Json(new { success = data.Count > 0 });
        }
        [HttpGet] 
        public IActionResult DownloadDataIssue(string issue_source)
        {
            using (XLWorkbook wb = new XLWorkbook())
            {
                DateTime currentDateTime = DateTime.Now;
                string formattedDateTime = currentDateTime.ToString("yyyyMMddHHmmss");
                wb.Worksheets.Add(this.GetIssueDataDownload(issue_source).Tables[0]);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                               "SEMB QUARANTINE MANAGEMENT - Data Issue" + formattedDateTime + ".xlsx");
                }
            }
        }
        private DataSet GetIssueDataDownload(string issue_source)
        {
            string query;
            if (string.IsNullOrEmpty(issue_source))
            {
                query = "SELECT * FROM mst_issue ORDER BY issue_source DESC";
            }
            else
            {
                query = "SELECT * FROM mst_issue WHERE issue_source LIKE @issue_source ORDER BY issue_source DESC";
            }

            DataSet ds = new DataSet();

            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {
                    cmd.CommandType = CommandType.Text;
                    if (!string.IsNullOrEmpty(issue_source))
                    {
                        cmd.Parameters.AddWithValue("@issue_source", "%" + issue_source + "%");
                    }
                    cmd.Connection = conn;
                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        sda.Fill(ds);
                    }
                }
            }
            return ds;
        }

        [HttpGet]
        public IActionResult GetDispositionFilter(string cell)
        {
            var db = new DatabaseAccessLayer();
            List<SelectModel> data = db.GetDispositionFilter(cell);

            return Json(new { items = data });
        }
        [HttpGet]
        public IActionResult GetAllDataDisposition(string disposition)
   {
            var db = new DatabaseAccessLayer();
            List<DispositionModel> dataList = db.GetAllDataDisposition(disposition);
            return PartialView("_TableDisposition", dataList);
        }
        [HttpPost]
        public IActionResult AddNewDisposition(string disposition)
        {
            var db = new DatabaseAccessLayer();
            string sesa_id = HttpContext.Session.GetString("sesa_id") ?? "";
            List<DispositionModel> data = db.AddNewDisposition(disposition, sesa_id);
            return Json(new { items = data });
        }
        [HttpGet]
        public IActionResult GetAllDataDispositionDetail(string id)
        {
            var db = new DatabaseAccessLayer();
            List<DispositionModel> data = db.GetAllDataDispositionDetail(id);
            return Json(new { items = data });
        }
        [HttpPost]
        public IActionResult UpdateDataDisposition(string id, string disposition)
        {
            var db = new DatabaseAccessLayer();
            string sesa_id = HttpContext.Session.GetString("sesa_id") ?? "";
            bool success = db.UpdateDataDisposition(id, disposition, sesa_id);

            return Json(new { success = success, message = success ? "Data updated successfully" : "Failed to update data" });
        }
        [HttpPost]
        public IActionResult DeleteDataDisposition(string id)
        {
            var db = new DatabaseAccessLayer();
            List<DispositionModel> data = db.DeleteDataDisposition(id);
            return Json(new { success = data.Count > 0 });
        }
        [HttpGet]
        public IActionResult DownloadDataDisposition(string disposition)
        {
            using (XLWorkbook wb = new XLWorkbook())
            {
                DateTime currentDateTime = DateTime.Now;
                string formattedDateTime = currentDateTime.ToString("yyyyMMddHHmmss");
                wb.Worksheets.Add(this.GetIssueDownload(disposition).Tables[0]);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                               "SEMB QUARANTINE MANAGEMENT - Data Disposition" + formattedDateTime + ".xlsx");
                }
            }
        }
        private DataSet GetIssueDownload(string disposition)
        {
            string query;
            if (string.IsNullOrEmpty(disposition))
            {
                query = "SELECT * FROM mst_disposition_QAS ORDER BY disposition DESC";
            }
            else
            {
                query = "SELECT * FROM mst_disposition_QAS WHERE disposition LIKE @disposition ORDER BY disposition DESC";
            }

            DataSet ds = new DataSet();

            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {
                    cmd.CommandType = CommandType.Text;
                    if (!string.IsNullOrEmpty(disposition))
                    {
                        cmd.Parameters.AddWithValue("@disposition", "%" + disposition + "%");
                    }
                    cmd.Connection = conn;
                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        sda.Fill(ds);
                    }
                }
            }
            return ds;
        }
        [HttpGet]
        public IActionResult GetRemarkFilter(string cell)
        {
            var db = new DatabaseAccessLayer();
            List<SelectModel> data = db.GetRemarkFilter(cell);

            return Json(new { items = data });
        }
        [HttpGet]
        public IActionResult GetAllDataRemark(string remark)
        {
            var db = new DatabaseAccessLayer();
            List<RemarkModel> dataList = db.GetAllDataRemark(remark);
            return PartialView("_TableRemark", dataList);
        }
        [HttpPost]
        public IActionResult AddNewRemark(string remark)
        {
            var db = new DatabaseAccessLayer();
            string sesa_id = HttpContext.Session.GetString("sesa_id") ?? "";
            List<RemarkModel> data = db.AddNewRemark(remark, sesa_id);
            return Json(new { items = data });
        }
        [HttpGet]
        public IActionResult GetAllDataRemarkDetail(string id)
        {
            var db = new DatabaseAccessLayer();
            List<RemarkModel> data = db.GetAllDataRemarkDetail(id);
            return Json(new { items = data });
        }
        [HttpPost]
        public IActionResult UpdateDataRemark(string id, string remark)
        {
            var db = new DatabaseAccessLayer();
            string sesa_id = HttpContext.Session.GetString("sesa_id") ?? "";
            bool success = db.UpdateDataRemark(id, remark, sesa_id);

            return Json(new { success = success, message = success ? "Data updated successfully" : "Failed to update data" });
        }
        [HttpPost]
        public IActionResult DeleteDataRemark(string id)
        {
            var db = new DatabaseAccessLayer();
            List<RemarkModel> data = db.DeleteDataRemark(id);
            return Json(new { success = data.Count > 0 });
        }
        [HttpGet]
        public IActionResult DownloadDataRemark(string remark)
        {
            using (XLWorkbook wb = new XLWorkbook())
            {
                DateTime currentDateTime = DateTime.Now;
                string formattedDateTime = currentDateTime.ToString("yyyyMMddHHmmss");
                wb.Worksheets.Add(this.GetRemarkDownload(remark).Tables[0]);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                               "SEMB QUARANTINE MANAGEMENT - Data Remark" + formattedDateTime + ".xlsx");
                }
            }
        }
        private DataSet GetRemarkDownload(string remark)
        {
            string query;
            if (string.IsNullOrEmpty(remark))
            {
                query = "SELECT * FROM mst_remark_QAS ORDER BY remark DESC";
            }
            else
            {
                query = "SELECT * FROM mst_remark_QAS WHERE remark LIKE @remark ORDER BY remark DESC";
            }

            DataSet ds = new DataSet();

            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {
                    cmd.CommandType = CommandType.Text;
                    if (!string.IsNullOrEmpty(remark))
                    {
                        cmd.Parameters.AddWithValue("@remark", "%" + remark + "%");
                    }
                    cmd.Connection = conn;
                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        sda.Fill(ds);
                    }
                }
            }
            return ds;
        }
        [HttpGet]
        public IActionResult GetRackFilter(string cell)
        {
            var db = new DatabaseAccessLayer();
            List<SelectModel> data = db.GetrackFilter(cell);

            return Json(new { items = data });
        }
        [HttpGet]
        public IActionResult GetAllDataRack(string rack)
        {
            var db = new DatabaseAccessLayer();
            List<RackModel> dataList = db.GetAllDataRack(rack);
            return PartialView("_TableRack", dataList);
        }
        [HttpPost]
        public IActionResult AddNewRack(string rack)
        {
            var db = new DatabaseAccessLayer();
            string sesa_id = HttpContext.Session.GetString("sesa_id") ?? "";
            List<RackModel> data = db.AddNewRack(rack, sesa_id);
            return Json(new { items = data });
        }
        [HttpGet]
        public IActionResult GetAllDataRackDetail(string id)
        {
            var db = new DatabaseAccessLayer();
            List<RackModel> data = db.GetAllDataRackDetail(id);
            return Json(new { items = data });
        }
        [HttpPost]
        public IActionResult UpdateDataRack(string id, string rack)
        {
            var db = new DatabaseAccessLayer();
            string sesa_id = HttpContext.Session.GetString("sesa_id") ?? "";
            bool success = db.UpdateDataRack(id, rack, sesa_id);

            return Json(new { success = success, message = success ? "Data updated successfully" : "Failed to update data" });
        }
        [HttpPost]
        public IActionResult DeleteDataRack(string id)
        {
            var db = new DatabaseAccessLayer();
            List<RackModel> data = db.DeleteDataRack(id);
            return Json(new { success = data.Count > 0 });
        }
        [HttpGet]
        public IActionResult DownloadDataRack(string rack)
        {
            using (XLWorkbook wb = new XLWorkbook())
            {
                DateTime currentDateTime = DateTime.Now;
                string formattedDateTime = currentDateTime.ToString("yyyyMMddHHmmss");
                wb.Worksheets.Add(this.GetRackDownload(rack).Tables[0]);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                               "SEMB QUARANTINE MANAGEMENT - Data Rack" + formattedDateTime + ".xlsx");
                }
            }
        }
        private DataSet GetRackDownload(string rack)
        {
            string query;
            if (string.IsNullOrEmpty(rack))
            {
                query = "SELECT * FROM mst_rack_QAS ORDER BY rack DESC";
            }
            else
            {
                query = "SELECT * FROM mst_rack_QAS WHERE rack LIKE @rack ORDER BY rack DESC";
            }

            DataSet ds = new DataSet();

            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {
                    cmd.CommandType = CommandType.Text;
                    if (!string.IsNullOrEmpty(rack))
                    {
                        cmd.Parameters.AddWithValue("@rack", "%" + rack + "%");
                    }
                    cmd.Connection = conn;
                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        sda.Fill(ds);
                    }
                }
            }
            return ds;
        }
        [HttpGet]
        public IActionResult GetColumnFilter(string cell)
        {
            var db = new DatabaseAccessLayer();
            List<SelectModel> data = db.GetColumnFilter(cell);

            return Json(new { items = data });
        }
        [HttpGet]
        public IActionResult GetAllDataColumn(string column)
        {
            var db = new DatabaseAccessLayer();
            List<ColumnModel> dataList = db.GetAllDataColumn(column);
            return PartialView("_TableColumn", dataList);
        }
        [HttpPost]
        public IActionResult AddNewColumn(string column)
        {
            var db = new DatabaseAccessLayer();
            string sesa_id = HttpContext.Session.GetString("sesa_id") ?? "";
            List<ColumnModel> data = db.AddNewColumn(column, sesa_id);
            return Json(new { items = data });
        }
        [HttpGet]
        public IActionResult GetAllDataColumnDetail(string id)
        {
            var db = new DatabaseAccessLayer();
            List<ColumnModel> data = db.GetAllDataColumnDetail(id);
            return Json(new { items = data });
        }
        [HttpPost]
        public IActionResult UpdateDataColumn(string id, string column)
        {
            // Added logging to diagnose parameter passing
            Console.WriteLine($"Controller received: id={id}, column={column}");

            var db = new DatabaseAccessLayer();
            string sesa_id = HttpContext.Session.GetString("sesa_id") ?? "";
            bool success = db.UpdateDataColumn(id, column, sesa_id);
            return Json(new { success = success, message = success ? "Data updated successfully" : "Failed to update data" });
        }
        [HttpPost]
        public IActionResult DeleteDataColumn(string id)
        {
            var db = new DatabaseAccessLayer();
            List<ColumnModel> data = db.DeleteDataColumn(id);
            return Json(new { success = data.Count > 0 });
        }
        [HttpGet]
        public IActionResult DownloadDataColumn(string column)
        {
            using (XLWorkbook wb = new XLWorkbook())
            {
                DateTime currentDateTime = DateTime.Now;
                string formattedDateTime = currentDateTime.ToString("yyyyMMddHHmmss");
                wb.Worksheets.Add(this.GetColumDownload(column).Tables[0]);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                               "SEMB QUARANTINE MANAGEMENT - Data Column" + formattedDateTime + ".xlsx");
                }
            }
        }
        private DataSet GetColumDownload(string column)
        {
            string query;
            if (string.IsNullOrEmpty(column))
            {
                query = "SELECT * FROM mst_rack_column_QAS ORDER BY rack_column DESC";
            }
            else
            {
                query = "SELECT * FROM mst_rack_column_QAS WHERE rack_column LIKE @rack_column ORDER BY rack_column DESC";
            }

            DataSet ds = new DataSet();

            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {
                    cmd.CommandType = CommandType.Text;
                    if (!string.IsNullOrEmpty(column))
                    {
                        cmd.Parameters.AddWithValue("@rack_column", "%" + column + "%");
                    }
                    cmd.Connection = conn;
                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        sda.Fill(ds);
                    }
                }
            }
            return ds;
        }
        [HttpGet]
        public IActionResult GetRowFilter(string cell)
        {
            var db = new DatabaseAccessLayer();
            List<SelectModel> data = db.GetRowFilter(cell);

            return Json(new { items = data });
        }
        [HttpGet]
        public IActionResult GetAllDataRow(string row)
        {
            var db = new DatabaseAccessLayer();
            List<RowModel> dataList = db.GetAllDataRow(row);
            return PartialView("_TableRow", dataList);
        }
        [HttpPost]
        public IActionResult AddNewRow(string row)
        {
            var db = new DatabaseAccessLayer();
            string sesa_id = HttpContext.Session.GetString("sesa_id") ?? "";
            List<RowModel> data = db.AddNewRow(row, sesa_id);
            return Json(new { items = data });
        }
        [HttpGet]
        public IActionResult GetAllDataRowDetail(string id)
        {
            var db = new DatabaseAccessLayer();
            List<RowModel> data = db.GetAllDataRowDetail(id);
            return Json(new { items = data });
        }
        [HttpPost]
        public IActionResult UpdateDataRow(string id, string row)
        {
            var db = new DatabaseAccessLayer();
            string sesa_id = HttpContext.Session.GetString("sesa_id") ?? "";
            bool success = db.UpdateDataRow(id, row, sesa_id);

            return Json(new { success = success, message = success ? "Data updated successfully" : "Failed to update data" });
        }
        [HttpPost]
        public IActionResult DeleteDataRow(string id)
        {
            var db = new DatabaseAccessLayer();
            List<RowModel> data = db.DeleteDataRow(id);
            return Json(new { success = data.Count > 0 });
        }
        [HttpGet]
        public IActionResult DownloadDataRow(string row)
        {
            using (XLWorkbook wb = new XLWorkbook())
            {
                DateTime currentDateTime = DateTime.Now;
                string formattedDateTime = currentDateTime.ToString("yyyyMMddHHmmss");
                wb.Worksheets.Add(this.GetRowDownload(row).Tables[0]);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                               "SEMB QUARANTINE MANAGEMENT - Data Column" + formattedDateTime + ".xlsx");
                }
            }
        }
        private DataSet GetRowDownload(string row)
        {
            string query;
            if (string.IsNullOrEmpty(row))
            {
                query = "SELECT * FROM mst_rack_row_QAS ORDER BY rack_row DESC";
            }
            else
            {
                query = "SELECT * FROM mst_rack_row_QAS WHERE rack_row LIKE @rack_row ORDER BY rack_row DESC";
            }

            DataSet ds = new DataSet();

            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {
                    cmd.CommandType = CommandType.Text;
                    if (!string.IsNullOrEmpty(row))
                    {
                        cmd.Parameters.AddWithValue("@rack_row", "%" + row + "%");
                    }
                    cmd.Connection = conn;
                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        sda.Fill(ds);
                    }
                }
            }
            return ds;
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
        public IActionResult CountRequestHistoryAdmin()
        {
            var db = new DatabaseAccessLayer();
            int count = db.GetCountRequestHistory();

            return Json(new { count = count });
        }

        [HttpGet]
        public IActionResult GetAllMasterDataApprover()
        {
            var db = new DatabaseAccessLayer();
           List<ApproverModel> data = db.GetAllMasterDataApprover();

            return PartialView("_TableApproverMaster", data);
        }

        [HttpGet]
        public IActionResult GetSESA_Approver(string family)
        {
            var db = new DatabaseAccessLayer();
            List<SelectModel> data = db.GetSESA_Approver(family);
            return Json(new { items = data });
        }

        [HttpGet]
        public IActionResult GetRouteLevel(string family)
        {
            var db = new DatabaseAccessLayer();
            List<SelectModel> data = db.GetRouteLevel(family);
            return Json(new { items = data });
        }

        [HttpPost]
        public IActionResult AddNewApprover(string usr_sesa, string route_level, string route_flow)
        {
            var db = new DatabaseAccessLayer();
            string modify = HttpContext.Session.GetString("sesa_id") ?? "";
            List<ApproverModel> data = db.AddNewApprover(usr_sesa, route_level, route_flow, modify);

            return Json(new { items = data });
        }

        [HttpPost]
        public IActionResult DeleteDataApproverData(string id)
        {
            var db = new DatabaseAccessLayer();
            List<ApproverModel> data = db.DeleteDataApproverData(id);
            return Json(new { success = data.Count > 0 });
        }

        [HttpPost]
        public IActionResult UpdateDataApprover(int id, string usr_sesa, string route_lvl, string route_flow)
        {
            try
            {
                var db = new DatabaseAccessLayer();
                string modify = HttpContext.Session.GetString("sesa_id") ?? "";
                bool success = db.UpdateDataApprover(id, usr_sesa, route_lvl, route_flow, modify);

                if (success)
                {
                    return Json(new { success = true, message = "Data updated successfully" });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to update data" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet] // Ubah dari HttpPost ke HttpGet karena JavaScript Anda menggunakan window.location.href
        public IActionResult DownloadDataApprover()
        {
            using (XLWorkbook wb = new XLWorkbook())
            {
                DateTime currentDateTime = DateTime.Now;
                string formattedDateTime = currentDateTime.ToString("yyyyMMddHHmmss");
                wb.Worksheets.Add(this.GetDataApprover().Tables[0]);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                               "SEMB QUARANTINE MANAGEMENT - Data Approver" + formattedDateTime + ".xlsx");
                }
            }
        }
        private DataSet GetDataApprover()
        {
            string query = "select * from mst_approvers_QAS";

            DataSet ds = new DataSet();

            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Connection = conn;
                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        sda.Fill(ds);
                    }
                }
            }
            return ds;
        }
        [HttpGet]
        public IActionResult GetAllMasterDataApprovalFlow()
        {
            var db = new DatabaseAccessLayer();
           List<ApproverModel> data = db.GetAllMasterDataApprovalFlow();

            return PartialView("_TableApprovalFlow", data);
        }

        [HttpPost]
        public IActionResult AddNewApprovalFlow(string route_level, string route_desc)
        {
            var db = new DatabaseAccessLayer();
            string modify = HttpContext.Session.GetString("sesa_id") ?? "";
            List<ApproverModel> data = db.AddNewApprovalFlow( route_level, route_desc, modify);

            return Json(new { items = data });
        }

        [HttpPost]
        public IActionResult DeleteDataApprovalData(string route_flow)
        {
            var db = new DatabaseAccessLayer();
            List<ApproverModel> data = db.DeleteDataApprovalData(route_flow);
            return Json(new { success = data.Count > 0 });
        }

        [HttpPost]
        public IActionResult UpdateDataApprovalFlow(string route_flow, string route_lvl, string route_desc)
        {
            var db = new DatabaseAccessLayer();
            string modify = HttpContext.Session.GetString("sesa_id") ?? "";
            List<ApproverModel> data = db.UpdateDataApprovalFlow(route_flow, route_lvl, route_desc, modify);

            return Json(new { success = data.Count > 0, items = data, message = data.Count > 0 ? "Data updated successfully" : "Failed to update data" });
        }


        [HttpGet] // Ubah dari HttpPost ke HttpGet karena JavaScript Anda menggunakan window.location.href
        public IActionResult DownloadDataApprovalFlow()
        {
            using (XLWorkbook wb = new XLWorkbook())
            {
                DateTime currentDateTime = DateTime.Now;
                string formattedDateTime = currentDateTime.ToString("yyyyMMddHHmmss");
                wb.Worksheets.Add(this.GetDataApprovalFlow().Tables[0]);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                               "SEMB QUARANTINE MANAGEMENT - Data Approval Flow" + formattedDateTime + ".xlsx");
                }
            }
        }
        private DataSet GetDataApprovalFlow()
        {
            string query = "select * from mst_route_QAS";

            DataSet ds = new DataSet();

            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {
                    cmd.CommandType = CommandType.Text;
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
