using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Quarantine_Management.Function;
using Quarantine_Management.Models;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Globalization;

namespace Quarantine_Management.Controllers
{
    public class UserController : Controller
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

        public IActionResult NewRequest()
        {
            if (HttpContext.Session.GetString("roles") == "User")
            {
                var db = new DatabaseAccessLayer();

                var requestData = db.GetRequestID();

                if (requestData != null && requestData.Count > 0)
                {
                    ViewBag.req_id = requestData[0].req_id;
                }
                else
                {
                    ViewBag.req_id = "No ID Available";
                }
                return View();
            }
            else
            {
                return RedirectToAction("SignOut", "Login");
            }
        }
        public IActionResult Dashboard()
        {
            if (HttpContext.Session.GetString("roles") == "User")
            {
                return View();
            }
            else
            {
                return RedirectToAction("SignOut", "Login");
            }
        }  
        public IActionResult OverdueTracking()
        {
            if (HttpContext.Session.GetString("roles") == "User")
            {
                return View();
            }
            else
            {
                return RedirectToAction("SignOut", "Login");
            }
        }  
        public IActionResult AfterAnalysis()
        {
            if (HttpContext.Session.GetString("roles") == "User")
            {
                return View();
            }
            else
            {
                return RedirectToAction("SignOut", "Login");
            }
        }  
        public IActionResult DeclinedTracking()
        {
            if (HttpContext.Session.GetString("roles") == "User")
            {
                return View();
            }
            else
            {
                return RedirectToAction("SignOut", "Login");
            }
        }  
        public IActionResult WaitingApproval()
        {
            if (HttpContext.Session.GetString("roles") == "User")
            {
                return View();
            }
            else
            {
                return RedirectToAction("SignOut", "Login");
            }
        }   
        public IActionResult FinishTracking()
        {
            if (HttpContext.Session.GetString("roles") == "User")
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
            if (HttpContext.Session.GetString("roles") == "User")
            {
                return View();
            }
            else
            {
                return RedirectToAction("SignOut", "Login");
            }
        }
        public IActionResult WaitingAnalysis()
        {
            if (HttpContext.Session.GetString("roles") == "User")
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
            if (HttpContext.Session.GetString("roles") == "User")
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
        
        public IActionResult UnderAnalysis()
        {
            if (HttpContext.Session.GetString("roles") == "User")
            {
                return View();
            }
            else
            {
                return RedirectToAction("SignOut", "Login");
            }
        }

        [HttpGet]
        public IActionResult GetBoxType(string cell)
        {
            var db = new DatabaseAccessLayer();
            List<SelectModel> data = db.GetBoxTypeFilter(cell);

            return Json(new { items = data });
        } 
        
        [HttpGet]
        public IActionResult GetReferenceFilter(string cell)
        {
            var db = new DatabaseAccessLayer();
            List<SelectModel> data = db.GetReferenceFilter(cell);

            return Json(new { items = data });
        }
        
        [HttpGet]
        public IActionResult GetRemarkFilter(string cell)
        {
            var db = new DatabaseAccessLayer();
            List<SelectModel> data = db.GetRemarkFilter(cell);

            return Json(new { items = data });
        }   
      
        [HttpGet]
        public IActionResult GetSourceIssueFilter(string cell)
        {
            var db = new DatabaseAccessLayer();
            List<SelectModel> data = db.GetSourceIssueFilter(cell);

            return Json(new { items = data });
        }
        
        [HttpGet]
        public IActionResult GetIssueCategory(string cell)
        {
            var db = new DatabaseAccessLayer();
            List<SelectModel> data = db.GetIssueCategory(cell);

            return Json(new { items = data });
        } 
        
        [HttpGet]
        public IActionResult GetSourceSloc(string cell)
        {
            var db = new DatabaseAccessLayer();
            List<SelectModel> data = db.GetSourceSlocFilter(cell);

            return Json(new { items = data });
        }  
        
        [HttpGet]
        public IActionResult GetDestinationSloc(string cell, string sector)
        {
            var db = new DatabaseAccessLayer();
            List<SelectModel> data = db.GetDestinationSlocFilter(cell, sector);

            return Json(new { items = data });
        }


        [HttpGet]
        public IActionResult GetPicFilter(string cell)
        {
            var db = new DatabaseAccessLayer();
            List<SelectModel> data = db.GetPicFilter(cell);

            return Json(new { items = data });
        }
        [HttpGet]
        public IActionResult GetDispositionFilter(string cell)
        {
            var db = new DatabaseAccessLayer();
            List<SelectModel> data = db.GetDispositionFilter(cell);

            return Json(new { items = data });
        }
        [HttpGet]
        public IActionResult GetRackFilter(string cell)
        {
            var db = new DatabaseAccessLayer();
            List<SelectModel> data = db.GetrackFilter(cell);

            return Json(new { items = data });
        }
        [HttpGet]
        public IActionResult GetRowFilter(string cell)
        {
            var db = new DatabaseAccessLayer();
            List<SelectModel> data = db.GetRowFilter(cell);

            return Json(new { items = data });
        }

        [HttpGet]
        public IActionResult GetColumnFilter(string cell)
        {
            var db = new DatabaseAccessLayer();
            List<SelectModel> data = db.GetColumnFilter(cell);

            return Json(new { items = data });
        }

        [HttpGet]
        public IActionResult GetRequestID()
        {
            var db = new DatabaseAccessLayer();
            List<RequestTrackingModel> data = db.GetRequestID();

            return Json(new { items = data });
        }

        [HttpPost]
        public async Task<IActionResult> CreateRequest(string req_id, string sesa_id, string reference, string box_type, string quantity, string rack, string row, string column, string pic, string max_aging, string remark, string ppap, string source_issue, string issue_category, string issue_detail, string disposition, string sloc, string dest_sloc, IFormFile file)
        {
            var db = new DatabaseAccessLayer();
            int rowsAffected = await db.CreateRequest(req_id, sesa_id, reference, box_type, quantity, rack, row, column, pic, max_aging, remark, ppap, source_issue, issue_category, issue_detail, disposition,sloc, dest_sloc, file);

            var result = new { success = rowsAffected > 0, affectedRows = rowsAffected };
            Console.WriteLine($"Controller returning: success={result.success}, rows={result.affectedRows}");
            return Json(result);
        }


        [HttpPost]
        public IActionResult GetFilterDate()
        {
            var db = new DatabaseAccessLayer();
            List<DateDataModel> data = db.GetFilterDate();

            return Json(data);
        }

        [HttpGet]
        public IActionResult GetWaitingApprovalData(string date_from, string date_to)
        {
            var db = new DatabaseAccessLayer();
            List<RequestModel> data = db.GetWaitingApprovalData(date_from, date_to);

            return PartialView("_TableWaitingApproval", data);
        }

        [HttpGet]
        public IActionResult GetDetailData(string id_req)
        {
            var db = new DatabaseAccessLayer();
            List<RequestModel> dataList = db.GetDetailDataUser(id_req);

            RequestModel? data = dataList.FirstOrDefault();

            return PartialView("_TableWaitingApprovalDetail", data);
        }
          
        [HttpGet]
        public IActionResult GetDetailDataOverdue(string id_req)
        {
            var db = new DatabaseAccessLayer();
            List<RequestModel> dataList = db.GetDetailDataOverdue(id_req);

            RequestModel? data = dataList.FirstOrDefault();

            return PartialView("_TableOverdueDetail", data);
        }

        [HttpGet]
        public IActionResult GetImages(string id_req)
        {
            var db = new DatabaseAccessLayer();
            List<RequestModel> data = db.GetImages(id_req);

            // Ensure we return a proper result even if no records found
            return Json(new { items = data });
        }
        [HttpGet]
        public IActionResult GetDetailDataEdit(string id_req)
        {
            var db = new DatabaseAccessLayer();
            List<RequestModel> data = db.GetDetailEdit(id_req);
            return Json(data);
        } 
        
        [HttpGet]
        public IActionResult GetDetailDeclinedDataEdit(string id_req)
        {
            var db = new DatabaseAccessLayer();
            List<RequestModel> data = db.GetDetailDeclinedDataEdit(id_req);
            return Json(data);
        }

        [HttpPost]
        public async Task<IActionResult> EditDataRequest(string req_id, string sesa_id, string reference, string box_type, string quantity, string rack, string row, string column, string pic, string max_aging, string remark, string ppap, string source_issue, string issue_category, string issue_detail, string disposition, string sloc, string dest_sloc, IFormFile file)
        {
            var db = new DatabaseAccessLayer();
            int rowsAffected = await db.EditDataRequest(req_id, sesa_id, reference, box_type, quantity, rack, row, column, pic, max_aging, remark, ppap, source_issue, issue_category, issue_detail, disposition, sloc, dest_sloc, file);

            var result = new { success = rowsAffected > 0, affectedRows = rowsAffected };
            Console.WriteLine($"Controller returning: success={result.success}, rows={result.affectedRows}");
            return Json(result);
        }
        [HttpPost]
        public IActionResult DeleteDataRequest(string id_req)
        {
            var db = new DatabaseAccessLayer();
            List<RequestModel> data = db.DeleteDataRequest(id_req);
            return Json(new { success = data.Count > 0 });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateDeclined(string req_id, string sesa_id, string reference, string box_type, string quantity, string rack, string row, string column, string pic, string max_aging, string remark, string ppap, string source_issue, string issue_category, string issue_detail, string disposition, string sloc, string dest_sloc, IFormFile file)
        {
            var db = new DatabaseAccessLayer();
            int rowsAffected = await db.UpdateDeclined(req_id, sesa_id, reference, box_type, quantity, rack, row, column, pic, max_aging, remark, ppap, source_issue, issue_category, issue_detail, disposition, sloc, dest_sloc, file);

            var result = new { success = rowsAffected > 0, affectedRows = rowsAffected };
            Console.WriteLine($"Controller returning: success={result.success}, rows={result.affectedRows}");
            return Json(result);
        }
        [HttpGet]
        public IActionResult CountAllwaitingApproval()
        {
            var db = new DatabaseAccessLayer();
            int count = db.GetCountAllwaitingApproval();

            return Json(new { count = count });
        }
        
        [HttpGet]
        public IActionResult CountdeclinedData()
        {
            var db = new DatabaseAccessLayer();
            int count = db.GetCountdeclinedData();

            return Json(new { count = count });
        } 
        
        [HttpGet]
        public IActionResult CountWaitingAnalysis()
        {
            var db = new DatabaseAccessLayer();
            int count = db.GetCountWaitingAnalysis();

            return Json(new { count = count });
        }
        
        [HttpGet]
        public IActionResult CountUnderAnalysis()
        {
            var db = new DatabaseAccessLayer();
            int count = db.GetCountUnderAnalysis();

            return Json(new { count = count });
        }
          
        [HttpGet]
        public IActionResult CountAfterAnalysis()
        {
            var db = new DatabaseAccessLayer();
            int count = db.GetCountAfterAnalysis();

            return Json(new { count = count });
        }
          
        [HttpGet]
        public IActionResult CountFinishAnalysis()
        {
            var db = new DatabaseAccessLayer();
            int count = db.GetCountFinishAnalysis();

            return Json(new { count = count });
        } 

        [HttpGet]
        public IActionResult CountoverdueAnalysis()
        {
            var db = new DatabaseAccessLayer();
            int count = db.GetCountoverdueAnalysis();

            return Json(new { count = count });
        }

        [HttpGet]
        public IActionResult GetDeclinedData(string date_from, string date_to)
        {
            var db = new DatabaseAccessLayer();
            List<RequestModel> data = db.GetDeclinedData(date_from, date_to);

            return PartialView("_TableDeclinedData", data);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateDataDeclined(string req_id, string sesa_id, string reference,
      string box_type, string quantity, string rack, string row, string column, string pic,
      string max_aging, string remark, string ppap, string source_issue, string issue_category,
      string issue_detail, string disposition, string sloc, string dest_sloc, IFormFile file,
      string updated_coment)
        {
            try
            {
                // Log received parameters for debugging
                Console.WriteLine($"Received parameters:");
                Console.WriteLine($"req_id: {req_id}");
                Console.WriteLine($"sesa_id: {sesa_id}");
                Console.WriteLine($"updated_coment: {updated_coment}");
                Console.WriteLine($"sloc: {sloc}");
                Console.WriteLine($"dest_sloc: {dest_sloc}");

                // Validate required parameters
                if (string.IsNullOrEmpty(req_id))
                {
                    return Json(new { success = false, message = "Request ID is required" });
                }

                if (string.IsNullOrEmpty(updated_coment))
                {
                    return Json(new { success = false, message = "Updated comment is required" });
                }

                var db = new DatabaseAccessLayer();
                int rowsAffected = await db.UpdateDataDeclined(req_id, sesa_id, reference, box_type,
                    quantity, rack, row, column, pic, max_aging, remark, ppap, source_issue,
                    issue_category, issue_detail, disposition, sloc, dest_sloc, file, updated_coment);

                var result = new { success = rowsAffected > 0, affectedRows = rowsAffected };
                Console.WriteLine($"Controller returning: success={result.success}, rows={result.affectedRows}");
                return Json(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateDataDeclined: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
        [HttpGet]
        public IActionResult GetWaitingAnalysisData(string date_from, string date_to)
        {
            var db = new DatabaseAccessLayer();
            List<RequestModel> data = db.GetWaitingAnalysisData(date_from, date_to);

            return PartialView("_TableWaitingAnalysisData", data);
        }

        [HttpGet]
        public IActionResult GetDetailDataWaitingAnalysis(string id_req)
        {
            try
            {
                var db = new DatabaseAccessLayer();
                List<RequestModel> data = db.GetDetailDataWaitingAnalysis(id_req);

                if (data == null || data.Count == 0)
                {
                    return PartialView("_TableWaitingAnalysisDataDetail", new RequestModel());
                }

                return PartialView("_TableWaitingAnalysisDataDetail", data.First());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetDetailDataWaitingAnalysis: {ex.Message}");

                return Content($"An error occurred: {ex.Message}");
            }
        }

        [HttpGet]
        public IActionResult GetUnderAnalysisData(string date_from, string date_to)
        {
            var db = new DatabaseAccessLayer();
            List<RequestModel> data = db.GetUnderAnalysisData(date_from, date_to);

            return PartialView("_TableUnderAnalysisData", data);
        }
        [HttpGet]
        public IActionResult GetDetailDataUnderAnalysis(string id_req)
        {
            try
            {
                var db = new DatabaseAccessLayer();
                List<RequestModel> data = db.GetDetailDataUnderAnalysis(id_req);

                if (data == null || data.Count == 0)
                {
                    return PartialView("_TableUnderAnalysisDataDetail", new RequestModel());
                }

                return PartialView("_TableUnderAnalysisDataDetail", data.First());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetDetailDataWaitingAnalysis: {ex.Message}");

                return Content($"An error occurred: {ex.Message}");
            }
        }
        
        [HttpGet]
        public IActionResult GetDetailDataUnderAnalysisUpdate(string id_req)
        {
            try
            {
                var db = new DatabaseAccessLayer();
                List<RequestModel> data = db.GetDetailDataUnderAnalysisUpdate(id_req);

                if (data == null || data.Count == 0)
                {
                    return PartialView("_TableUnderAnalysisDataDetailUpdate", new RequestModel());
                }

                return PartialView("_TableUnderAnalysisDataDetailUpdate", data.First());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetDetailDataWaitingAnalysis: {ex.Message}");

                return Content($"An error occurred: {ex.Message}");
            }
        }
        [HttpGet]
        public IActionResult GetFinalStatus(string cell)
        {
            var db = new DatabaseAccessLayer();
            List<SelectModel> data = db.GetFinalStatus(cell);
            return Json(new { items = data });
        }

        [HttpGet]
        public IActionResult GetSlocFinal(string cell)
        {
            var db = new DatabaseAccessLayer();
            List<SelectModel> data = db.GetSlocFinal(cell);
            return Json(new { items = data });
        }

        [HttpGet]
        public IActionResult GetFinalStatusBySloc(string slocId)
        {
            var db = new DatabaseAccessLayer();
            List<SelectModel> data = db.GetFinalStatusBySloc(slocId);
            return Json(new { items = data });
        }

        [HttpPost]
        public IActionResult UpdateRequest(string id_req, string disposition, string final_status, string sloc, string result)
        {
            try
            {
                var db = new DatabaseAccessLayer();
                int rowsAffected = db.UpdateRequest(id_req, disposition, final_status, sloc, result);
                return Json(new { success = rowsAffected > 0 });
            }
            catch (Exception ex)
            {
                
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetAfterAnalysisData(string date_from, string date_to)
        {
            var db = new DatabaseAccessLayer();
            List<RequestModel> data = db.GetAfterAnalysisData(date_from, date_to);

            return PartialView("_TableAfterAnalysisData", data);
        }
        [HttpGet]
        public IActionResult GetDetailDataAfterAnalysis(string id_req)
        {
            try
            {
                var db = new DatabaseAccessLayer();
                List<RequestModel> data = db.GetDetailDataAfterAnalysis(id_req);

                if (data == null || data.Count == 0)
                {
                    return PartialView("_TableAfterAnalysisDataDetail", new RequestModel());
                }

                return PartialView("_TableAfterAnalysisDataDetail", data.First());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetDetailDataWaitingAnalysis: {ex.Message}");

                return Content($"An error occurred: {ex.Message}");
            }
        }

        [HttpGet]
        public IActionResult GetFinishAnalysisData(string date_from, string date_to)
        {
            var db = new DatabaseAccessLayer();
            List<RequestModel> data = db.GetFinishAnalysisData(date_from, date_to);

            return PartialView("_TableFinishAnalysisData", data);
        }

        [HttpGet]
        public IActionResult GetDetailDataFinihsAnalysis(string id_req)
        {
            try
            {
                var db = new DatabaseAccessLayer();
                List<RequestModel> data = db.GetDetailDataFinihsAnalysis(id_req);

                if (data == null || data.Count == 0)
                {
                    return PartialView("_TableFinishAnalysisDataDetail", new RequestModel());
                }

                return PartialView("_TableFinishAnalysisDataDetail", data.First());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetDetailDataWaitingAnalysis: {ex.Message}");

                return Content($"An error occurred: {ex.Message}");
            }
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
        public IActionResult DownloadWaitingApproval(string date_from, string date_to)
        {
            using (XLWorkbook wb = new XLWorkbook())
            {
                DateTime currentDateTime = DateTime.Now;
                string formattedDateTime = currentDateTime.ToString("yyyyMMddHHmmss");
                wb.Worksheets.Add(this.GetDataWaitingApprovalDownload(date_from, date_to).Tables[0]);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                               "SEMB QUARANTINE MANAGEMENT - Waiting Approval Data" + formattedDateTime + ".xlsx");
                }
            }
        }

        private DataSet GetDataWaitingApprovalDownload(string date_from, string date_to)
        {
            string query = "Select * from tbl_tracking_QAINP where status = 'Waiting Approval'";

            if (!string.IsNullOrEmpty(date_from) && !string.IsNullOrEmpty(date_to))
            {
                query += " and request_date between @date_from and @date_to";
            }

            DataSet ds = new DataSet();

            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {
                    cmd.CommandType = CommandType.Text;

                    if (!string.IsNullOrEmpty(date_from) && !string.IsNullOrEmpty(date_to))
                    {
                        cmd.Parameters.AddWithValue("@date_from", date_from);
                        cmd.Parameters.AddWithValue("@date_to", date_to);
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
        public IActionResult DownlodWaitingAnalysis(string date_from, string date_to)
        {
            using (XLWorkbook wb = new XLWorkbook())
            {
                DateTime currentDateTime = DateTime.Now;
                string formattedDateTime = currentDateTime.ToString("yyyyMMddHHmmss");
                wb.Worksheets.Add(this.GetDataWaitingAnalysisDownload(date_from, date_to).Tables[0]);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                               "SEMB QUARANTINE MANAGEMENT - Waiting Analysis Data" + formattedDateTime + ".xlsx");
                }
            }
        }

        private DataSet GetDataWaitingAnalysisDownload(string date_from, string date_to)
        {
            string query = "Select * from tbl_tracking_QAINP where status = 'Waiting Analysis'";

            if (!string.IsNullOrEmpty(date_from) && !string.IsNullOrEmpty(date_to))
            {
                query += " and request_date between @date_from and @date_to";
            }

            DataSet ds = new DataSet();

            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {
                    cmd.CommandType = CommandType.Text;

                    if (!string.IsNullOrEmpty(date_from) && !string.IsNullOrEmpty(date_to))
                    {
                        cmd.Parameters.AddWithValue("@date_from", date_from);
                        cmd.Parameters.AddWithValue("@date_to", date_to);
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
        public IActionResult DownlodUnderAnalysis(string date_from, string date_to)
        {
            using (XLWorkbook wb = new XLWorkbook())
            {
                DateTime currentDateTime = DateTime.Now;
                string formattedDateTime = currentDateTime.ToString("yyyyMMddHHmmss");
                wb.Worksheets.Add(this.GetDataUnderAnalysisDownload(date_from, date_to).Tables[0]);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                               "SEMB QUARANTINE MANAGEMENT - Under Analysis Data" + formattedDateTime + ".xlsx");
                }
            }
        }

        private DataSet GetDataUnderAnalysisDownload(string date_from, string date_to)
        {
            string query = "Select * from tbl_tracking_QAINP where status = 'Under Analysis'";

            if (!string.IsNullOrEmpty(date_from) && !string.IsNullOrEmpty(date_to))
            {
                query += " and request_date between @date_from and @date_to";
            }

            DataSet ds = new DataSet();

            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {
                    cmd.CommandType = CommandType.Text;

                    if (!string.IsNullOrEmpty(date_from) && !string.IsNullOrEmpty(date_to))
                    {
                        cmd.Parameters.AddWithValue("@date_from", date_from);
                        cmd.Parameters.AddWithValue("@date_to", date_to);
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
        public IActionResult DownlodAfterAnalysis(string date_from, string date_to)
        {
            using (XLWorkbook wb = new XLWorkbook())
            {
                DateTime currentDateTime = DateTime.Now;
                string formattedDateTime = currentDateTime.ToString("yyyyMMddHHmmss");
                wb.Worksheets.Add(this.GetDatAfterAnalysisDownload(date_from, date_to).Tables[0]);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                               "SEMB QUARANTINE MANAGEMENT - After Analysis Data" + formattedDateTime + ".xlsx");
                }
            }
        }

        private DataSet GetDatAfterAnalysisDownload(string date_from, string date_to)
        {
            string query = "Select * from tbl_tracking_QAINP where status = 'After Analysis'";

            if (!string.IsNullOrEmpty(date_from) && !string.IsNullOrEmpty(date_to))
            {
                query += " and request_date between @date_from and @date_to";
            }

            DataSet ds = new DataSet();

            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {
                    cmd.CommandType = CommandType.Text;

                    if (!string.IsNullOrEmpty(date_from) && !string.IsNullOrEmpty(date_to))
                    {
                        cmd.Parameters.AddWithValue("@date_from", date_from);
                        cmd.Parameters.AddWithValue("@date_to", date_to);
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
        public IActionResult DownlodFinishData(string date_from, string date_to)
        {
            using (XLWorkbook wb = new XLWorkbook())
            {
                DateTime currentDateTime = DateTime.Now;
                string formattedDateTime = currentDateTime.ToString("yyyyMMddHHmmss");
                wb.Worksheets.Add(this.GetDataFinishAnalysisDownload(date_from, date_to).Tables[0]);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                               "SEMB QUARANTINE MANAGEMENT - Finish Analysis Data" + formattedDateTime + ".xlsx");
                }
            }
        }

        private DataSet GetDataFinishAnalysisDownload(string date_from, string date_to)
        {
            string query = "Select * from tbl_tracking_QAINP where status = 'Finish'";

            if (!string.IsNullOrEmpty(date_from) && !string.IsNullOrEmpty(date_to))
            {
                query += " and request_date between @date_from and @date_to";
            }

            DataSet ds = new DataSet();

            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {
                    cmd.CommandType = CommandType.Text;

                    if (!string.IsNullOrEmpty(date_from) && !string.IsNullOrEmpty(date_to))
                    {
                        cmd.Parameters.AddWithValue("@date_from", date_from);
                        cmd.Parameters.AddWithValue("@date_to", date_to);
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
        public IActionResult DownloadOverdue(string date_from, string date_to)
        {
            using (XLWorkbook wb = new XLWorkbook())
            {
                DateTime currentDateTime = DateTime.Now;
                string formattedDateTime = currentDateTime.ToString("yyyyMMddHHmmss");
                wb.Worksheets.Add(this.GetDataOverdueDownload(date_from, date_to).Tables[0]);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                               "SEMB QUARANTINE MANAGEMENT - Overdue Data" + formattedDateTime + ".xlsx");
                }
            }
        }

        private DataSet GetDataOverdueDownload(string date_from, string date_to)
        {
            string query = "Select * from tbl_tracking_QAINP where status = 'Overdue'";

            if (!string.IsNullOrEmpty(date_from) && !string.IsNullOrEmpty(date_to))
            {
                query += " and request_date between @date_from and @date_to";
            }

            DataSet ds = new DataSet();

            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {
                    cmd.CommandType = CommandType.Text;

                    if (!string.IsNullOrEmpty(date_from) && !string.IsNullOrEmpty(date_to))
                    {
                        cmd.Parameters.AddWithValue("@date_from", date_from);
                        cmd.Parameters.AddWithValue("@date_to", date_to);
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
        public IActionResult GetOverDueData(string date_from, string date_to)
        {
            var db = new DatabaseAccessLayer();
            List<RequestModel> data = db.GetOverDueData(date_from, date_to);

            return PartialView("_TableOverdue", data);
        }
        [HttpPost]
        public async Task<IActionResult> UploadNewRequest(IFormFile myExcelData)
        {
            if (myExcelData == null || myExcelData.Length == 0)
            {
                return Json(new { success = false, message = "Please upload an Excel file (.xlsx)." });
            }

            try
            {
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/upload");
                string fileName = DateTime.Now.ToString("yyyyMMddHHmmss") + ".xlsx";
                filePath = Path.Combine(filePath, fileName);

                using (Stream fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await myExcelData.CopyToAsync(fileStream);
                }

                using (XLWorkbook xLWorkbook = new XLWorkbook(filePath))
                {
                    var worksheet = xLWorkbook.Worksheets.Worksheet(1);
                    int row = 2;
                    int rowsAffected = 0;
                    List<string> importedBOXs = new List<string>();

                    while (!string.IsNullOrEmpty(worksheet.Cell(row, 1).GetString()))
                    {
                        string referenceValue = worksheet.Cell(row, 1).GetString();
                        string box_typeValue = worksheet.Cell(row, 2).GetString();
                        string quantityValue = worksheet.Cell(row, 3).GetString();
                        string picValue = worksheet.Cell(row, 4).GetString();
                        string max_agingValueStr = worksheet.Cell(row, 5).GetString();
                        string rackValue = worksheet.Cell(row, 6).GetString();
                        string rack_rowValue = worksheet.Cell(row, 7).GetString();
                        string rack_columnValue = worksheet.Cell(row, 8).GetString();
                        string remarkValue = worksheet.Cell(row, 9).GetString();
                        string ppapValue = worksheet.Cell(row, 10).GetString();
                        string source_issueValue = worksheet.Cell(row, 11).GetString();
                        string issue_categoryValue = worksheet.Cell(row, 12).GetString();
                        string issue_detailValue = worksheet.Cell(row, 13).GetString();
                        string source_slocValue = worksheet.Cell(row, 14).GetString();
                        string dest_slocValue = worksheet.Cell(row, 15).GetString();

                        if (DateTime.TryParse(max_agingValueStr, out DateTime max_agingValue))
                        {
                            TimeSpan difference = max_agingValue - DateTime.Now;
                            if (difference.TotalDays < 30)
                            {
                                string req_id;
                                using (SqlConnection conn = new SqlConnection(DbConnection()))
                                {
                                    SqlCommand cmd = new SqlCommand("CREATE_REQUEST_ID", conn)
                                    {
                                        CommandType = CommandType.StoredProcedure
                                    };
                                    conn.Open();
                                    req_id = (string)cmd.ExecuteScalar();
                                    conn.Close();
                                }

                                using (SqlConnection conn = new SqlConnection(DbConnection()))
                                {
                                    SqlCommand cmd = new SqlCommand("UPLOAD_NEW_REQUEST", conn)
                                    {
                                        CommandType = CommandType.StoredProcedure
                                    };
                                    cmd.Parameters.AddWithValue("@req_id", req_id);
                                    cmd.Parameters.AddWithValue("@reference", referenceValue);
                                    cmd.Parameters.AddWithValue("@box_type", box_typeValue);
                                    cmd.Parameters.AddWithValue("@quantity", quantityValue);
                                    cmd.Parameters.AddWithValue("@pic", picValue);
                                    cmd.Parameters.AddWithValue("@max_aging", max_agingValue);
                                    cmd.Parameters.AddWithValue("@rack", rackValue);
                                    cmd.Parameters.AddWithValue("@rack_row", rack_rowValue);
                                    cmd.Parameters.AddWithValue("@rack_column", rack_columnValue);
                                    cmd.Parameters.AddWithValue("@remark", remarkValue);
                                    cmd.Parameters.AddWithValue("@ppap", ppapValue);
                                    cmd.Parameters.AddWithValue("@source_issue", source_issueValue);
                                    cmd.Parameters.AddWithValue("@issue_category", issue_categoryValue);
                                    cmd.Parameters.AddWithValue("@issue_detail", issue_detailValue);
                                    cmd.Parameters.AddWithValue("@source_sloc", source_slocValue);
                                    cmd.Parameters.AddWithValue("@dest_sloc", dest_slocValue);
                                    cmd.Parameters.AddWithValue("@requestor", HttpContext.Session.GetString("sesa_id"));
                                    conn.Open();
                                    rowsAffected = cmd.ExecuteNonQuery();
                                    conn.Close();
                                }

                                importedBOXs.Add(referenceValue);
                            }
                        }
                        else
                        {
                            // Log or handle invalid date format if needed
                        }

                        row++;
                    }

                    if (importedBOXs.Count == 0)
                    {
                        return Json(new { success = false, message = "Failed to upload data. Please double check the format and completeness of the data according to the template." });
                    }

                    return Json(new { success = true, message = "Imported " + importedBOXs.Count + " BoXs." });
                }
            }
            catch (Exception ex)
            {
                // Log the exception if logging is setup
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }
    

}
}
