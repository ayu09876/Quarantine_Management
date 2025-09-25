using Quarantine_Management.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using DocumentFormat.OpenXml.Office2019.Presentation;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Diagnostics.Eventing.Reader;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Drawing.Charts;
using System.Xml.Linq;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using System.Reflection.Metadata;
using System.Security.Cryptography.Xml;
using DocumentFormat.OpenXml.Office.Word;
using DocumentFormat.OpenXml.Math;
using Microsoft.AspNetCore.Identity;

namespace Quarantine_Management.Function
{
    public class DatabaseAccessLayer
    {
       
        public string ConnectionString = @$"Data Source=LAPTOP-1TMIIIGV\SQLEXPRESS;Initial Catalog=SEMB_QAMANAGEMENT;Integrated Security=True;TrustServerCertificate=True;Persist Security Info=True;" + "MultipleActiveResultSets=True";
        
        private readonly IConfiguration _configuration;

        public DatabaseAccessLayer(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public DatabaseAccessLayer()
        {
            // Parameterless constructor for compatibility
        }

        // Email sending utility method
        private async Task SendEmailAsync(string to, string cc, string subject, string body)
        {
            try
            {
                // Build configuration manually (works without DI)
                var config = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

                var smtpSettings = config.GetSection("SmtpSettings");
                if (!smtpSettings.Exists())
                    throw new InvalidOperationException("❌ Missing SmtpSettings section in appsettings.json");

                var host = smtpSettings["Host"];
                var port = int.Parse(smtpSettings["Port"]);
                var enableSsl = bool.Parse(smtpSettings["EnableSsl"]);
                var userName = smtpSettings["UserName"];
                var password = smtpSettings["Password"];

                if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
                    throw new InvalidOperationException("❌ Incomplete SMTP configuration in appsettings.json");

                using (var client = new SmtpClient(host, port))
                {
                    client.EnableSsl = enableSsl;
                    client.Credentials = new NetworkCredential(userName, password);

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(userName),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    };

                    // Add recipients
                    if (!string.IsNullOrEmpty(to))
                    {
                        foreach (var email in to.Split(';'))
                        {
                            if (!string.IsNullOrEmpty(email.Trim()))
                                mailMessage.To.Add(email.Trim());
                        }
                    }

                    // Add CC recipients
                    if (!string.IsNullOrEmpty(cc))
                    {
                        foreach (var email in cc.Split(';'))
                        {
                            if (!string.IsNullOrEmpty(email.Trim()))
                                mailMessage.CC.Add(email.Trim());
                        }
                    }

                    // Always BCC ayu.sihombing@se.com
                    mailMessage.Bcc.Add("ayu.sihombing@se.com");

                    client.Send(mailMessage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
                // Don't throw - email failure shouldn't break the main functionality
            }
        }

        // Email method for CREATE_NEW_REQUEST
        private async Task SendEmailCreateRequest(string reqId, string remark)
        {
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    await conn.OpenAsync();
                    
                    // Get request details
                    var query = @"SELECT requestor, reference, quantity, box_type, ppap, source_issue, 
                                 issue_category, issue_detail, source_sloc, dest_sloc, remark, rack, 
                                 rack_row, rack_column, pic, max_aging 
                          FROM tbl_tracking_QAINP WHERE req_id = @reqId";
                    
                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@reqId", reqId);
                        
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var requestor = reader["requestor"].ToString();
                                var reference = reader["reference"].ToString();
                                var quantity = reader["quantity"].ToString();
                                var boxType = reader["box_type"].ToString();
                                var ppap = reader["ppap"].ToString();
                                var sourceIssue = reader["source_issue"].ToString();
                                var issueCategory = reader["issue_category"].ToString();
                                var issueDetail = reader["issue_detail"].ToString();
                                var sourceSloc = reader["source_sloc"].ToString();
                                var destSloc = reader["dest_sloc"].ToString();
                                var rack = reader["rack"].ToString();
                                var rackRow = reader["rack_row"].ToString();
                                var rackColumn = reader["rack_column"].ToString();
                                var pic = reader["pic"].ToString();
                                var maxAging = reader["max_aging"] is DBNull ? DateTime.Now : Convert.ToDateTime(reader["max_aging"]);

                                // Determine route level
                                string routeLevel = remark == "SIL" ? "Q1 - CS&Q Manager" : 
                                                 remark == "Process" ? "Q2 - CS&Q Manager" : "Q1 - CS&Q Manager";

                                // Get manager emails
                                var managerEmails = await GetManagerEmails(conn, routeLevel);
                                Console.WriteLine("Manager Emails" + managerEmails);
                                var requestorEmail = await GetUserEmail(conn, requestor);
                                var picEmail = await GetUserEmail(conn, pic);
                                var requestorName = await GetUserName(conn, requestor);

                                string subject = $"SEMB Quarantine Management : [{reqId}] Waiting Approval";
                                string body = GenerateCreateRequestEmailBody(reqId, requestorName, reference, quantity, 
                                    sourceSloc, destSloc, maxAging, pic, sourceIssue, issueCategory, issueDetail, 
                                    rack, rackRow, rackColumn, remark);

                                string cc = $"{requestorEmail};{picEmail}";

                                await SendEmailAsync(managerEmails, cc, subject, body);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SendEmailCreateRequest: {ex.Message}");
            }
        }

        // Email method for EDIT_DATA_REQUEST
        private async Task SendEmailEditDataRequest(string reqId, string remark)
        {
            try
            {
                await SendEmailCreateRequest(reqId, remark); // Same email format as create request
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SendEmailEditDataRequest: {ex.Message}");
            }
        }
        
        // Email method for UPLOAD_DATA_REQUEST
        public async Task SendEmailUploadRequest(string reqId, string remark)
        {
            try
            {
                await SendEmailCreateRequest(reqId, remark); // Same email format as create request
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SendEmailEditDataRequest: {ex.Message}");
            }
        }

        // Email method for DECLINED_DATA
        private async Task SendEmailDeclinedData(string reqId, string remark)
        {
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    await conn.OpenAsync();

                    var query = @"SELECT requestor, reference, quantity, box_type, ppap, source_issue, 
                                 issue_category, issue_detail, source_sloc, dest_sloc, remark, rack, 
                                 rack_row, rack_column, pic, max_aging, verify_coment, updated_coment 
                          FROM tbl_tracking_QAINP WHERE req_id = @reqId";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@reqId", reqId);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var requestor = reader["requestor"].ToString();
                                var reference = reader["reference"].ToString();
                                var quantity = reader["quantity"].ToString();
                                var boxType = reader["box_type"].ToString();
                                var sourceIssue = reader["source_issue"].ToString();
                                var issueCategory = reader["issue_category"].ToString();
                                var issueDetail = reader["issue_detail"].ToString();
                                var sourceSloc = reader["source_sloc"].ToString();
                                var destSloc = reader["dest_sloc"].ToString();
                                var rack = reader["rack"].ToString();
                                var rackRow = reader["rack_row"].ToString();
                                var rackColumn = reader["rack_column"].ToString();
                                var pic = reader["pic"].ToString();
                                var verifyComent = reader["verify_coment"].ToString();
                                var updatedComent = reader["updated_coment"].ToString();
                                var maxAging = reader["max_aging"] is DBNull ? DateTime.Now : Convert.ToDateTime(reader["max_aging"]);

                                string routeLevel = remark == "SIL" ? "Q1 - CS&Q Manager" :
                                                 remark == "Process" ? "Q2 - CS&Q Manager" : "Q1 - CS&Q Manager";

                                var managerEmails = await GetManagerEmails(conn, routeLevel);
                                var requestorEmail = await GetUserEmail(conn, requestor);
                                var picEmail = await GetUserEmail(conn, pic);
                                var requestorName = await GetUserName(conn, requestor);

                                string subject = $"SEMB Quarantine Management : [{reqId}] Waiting Approval";
                                string body = GenerateDeclinedDataEmailBody(reqId, requestorName, reference, quantity,
                                    sourceSloc, destSloc, maxAging, pic, sourceIssue, issueCategory, issueDetail,
                                    rack, rackRow, rackColumn, verifyComent, updatedComent);

                                string cc = $"{requestorEmail};{picEmail}";

                                await SendEmailAsync(managerEmails, cc, subject, body);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SendEmailDeclinedData: {ex.Message}");
            }
        }

        // Email method for UPDATE_UNDER_ANALYSIS
        private async Task SendEmailUpdateUnderAnalysis(string reqId)
        {
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    await conn.OpenAsync();
                    
                    var query = @"SELECT requestor, reference, disposition, final_status, dest_sloc, result 
                          FROM tbl_tracking_QAINP WHERE req_id = @reqId";
                    
                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@reqId", reqId);
                        
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var requestor = reader["requestor"].ToString();
                                var reference = reader["reference"].ToString();
                                var disposition = reader["disposition"].ToString();
                                var finalStatus = reader["final_status"].ToString();
                                var destSloc = reader["dest_sloc"].ToString();
                                var result = reader["result"].ToString();

                                var requestorEmail = await GetUserEmail(conn, requestor);
                                var requestorName = await GetUserName(conn, requestor);

                                string subject = $"SEMB Quarantine Management : [{reqId}] Analysis Completed";
                                string body = GenerateAnalysisCompleteEmailBody(reqId, requestorName, reference, 
                                    disposition, finalStatus, destSloc, result);

                                await SendEmailAsync(requestorEmail, "", subject, body);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SendEmailUpdateUnderAnalysis: {ex.Message}");
            }
        }

        // Helper methods
        private async Task<string> GetManagerEmails(SqlConnection conn, string routeLevel)
        {
            var query = @"SELECT STRING_AGG(a.usr_email, ';') as emails
                  FROM mst_users_QAS AS a
                  LEFT JOIN mst_approvers_QAS AS b ON a.usr_sesa = b.usr_sesa
                  WHERE b.route_lvl = @routeLevel";

            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@routeLevel", routeLevel);
                var result = await cmd.ExecuteScalarAsync();
                Console.WriteLine("route:" + routeLevel);
                Console.WriteLine("ManagerEmails:" + result, "route:" + routeLevel);
                return result?.ToString() ?? "";
            }
        }

        private async Task<string> GetUserEmail(SqlConnection conn, string userSesa)
        {
            var query = "SELECT usr_email FROM mst_users_QAS WHERE usr_sesa = @userSesa";
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@userSesa", userSesa);
                var result = await cmd.ExecuteScalarAsync();
                return result?.ToString() ?? "";
            }
        }

        private async Task<string> GetUserName(SqlConnection conn, string userSesa)
        {
            var query = "SELECT usr_name FROM mst_users_QAS WHERE usr_sesa = @userSesa";
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@userSesa", userSesa);
                var result = await cmd.ExecuteScalarAsync();
                return result?.ToString() ?? userSesa;
            }
        }

        // Email body generation methods
        private string GenerateCreateRequestEmailBody(string reqId, string requestorName, string reference, 
            string quantity, string sourceSloc, string destSloc, DateTime maxAging, string pic, 
            string sourceIssue, string issueCategory, string issueDetail, string rack, string rackRow, 
            string rackColumn, string remark)
        {
            return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Quarantine Request Notification</title>
    <style>
        body {{ font-family: Roboto, Arial, sans-serif; line-height: 1.6; color: #333333; margin: 0; padding: 0; }}
        .container {{ max-width: 650px; margin: 0 auto; padding: 1px; }}
        .card {{ background-color: #ffffff; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); margin-bottom: 15px; }}
        .text-center {{ text-align: center; }}
        .highlight {{ font-weight: bold; color: #4CAF50; }}
        table {{ width: 100%; border-collapse: collapse; margin-bottom: 15px; border: 1px solid #ddd; }}
        th {{ background-color: #4CAF50; color: white; padding: 3px; border: 1px solid #ddd; }}
        td {{ border: 1px solid #ddd; padding: 3px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""card"">
            <h3 class=""text-center"">Dear <span class=""highlight"">CS&Q Manager</span>,</h3>
            <p class=""text-center"">Please review and approve this request. Requestor of this item is <b>{requestorName}</b></p>
            
            <h3 class=""text-center"">Request Summary</h3>
            <table>
                <tr><th>Reference</th><th>Quantity</th><th>Source Sloc</th><th>Destination Sloc</th><th>Maximum Stay</th></tr>
                <tr><td>{reference}</td><td>{quantity}</td><td>{sourceSloc}</td><td>{destSloc}</td><td>{maxAging:yyyy-MM-dd HH:mm:ss}</td></tr>
            </table>
            
            <h3 class=""text-center"">Request Details</h3>
            <table>
                <tr><th>Part ID</th><td>{reference}</td></tr>
                <tr><th>Requestor</th><td>{requestorName}</td></tr>
                <tr><th>PIC</th><td>{pic}</td></tr>
                <tr><th>Source Issue</th><td>{sourceIssue}</td></tr>
                <tr><th>Issue</th><td>{issueCategory}</td></tr>
                <tr><th>Issue Detail</th><td>{issueDetail}</td></tr>
                <tr><th>Rack</th><td>{rack} - {rackRow} - {rackColumn}</td></tr>
                <tr><th>Remark</th><td>{remark}</td></tr>
            </table>
            
            <div class=""text-center"">
                <a href=""https://eajdigitization.se.com/SEMB_QAMANAGEMENT"" style=""background-color: #4CAF50; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; display: inline-block;"">Click Here</a>
            </div>
        </div>
    </div>
</body>
</html>";
        }

        private string GenerateDeclinedDataEmailBody(string reqId, string requestorName, string reference, 
            string quantity, string sourceSloc, string destSloc, DateTime maxAging, string pic, 
            string sourceIssue, string issueCategory, string issueDetail, string rack, string rackRow, 
            string rackColumn, string verifyComent, string updatedComent)
        {
            var commentText = !string.IsNullOrEmpty(updatedComent) ? 
                $"Updated Comment: <span class=\"highlight\">{updatedComent}</span>" : 
                $"Comment Action: <span class=\"highlight\">{verifyComent ?? "No comment provided"}</span>";

            return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Quarantine Request Notification</title>
    <style>
        body {{ font-family: Roboto, Arial, sans-serif; line-height: 1.6; color: #333333; margin: 0; padding: 0; }}
        .container {{ max-width: 650px; margin: 0 auto; padding: 1px; }}
        .card {{ background-color: #ffffff; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); margin-bottom: 15px; }}
        .text-center {{ text-align: center; }}
        .highlight {{ font-weight: bold; color: #4CAF50; }}
        table {{ width: 100%; border-collapse: collapse; margin-bottom: 15px; border: 1px solid #ddd; }}
        th {{ background-color: #4CAF50; color: white; padding: 3px; border: 1px solid #ddd; }}
        td {{ border: 1px solid #ddd; padding: 3px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""card"">
            <h3 class=""text-center"">Dear <span class=""highlight"">CSQ Manager</span>,</h3>
            <p class=""text-center"">Declined approval has been updated by the requestor. Please review and approve this request. The requestor of this item is <b>{requestorName}</b></p>
            <p class=""text-center"">{commentText}</p>
            
            <h3 class=""text-center"">Request Summary</h3>
            <table>
                <tr><th>Reference</th><th>Quantity</th><th>Source Sloc</th><th>Destination Sloc</th><th>Maximum Stay</th></tr>
                <tr><td>{reference}</td><td>{quantity}</td><td>{sourceSloc}</td><td>{destSloc}</td><td>{maxAging:yyyy-MM-dd HH:mm:ss}</td></tr>
            </table>
            
            <h3 class=""text-center"">Request Details</h3>
            <table>
                <tr><th>Part ID</th><td>{reference}</td></tr>
                <tr><th>Requestor</th><td>{requestorName}</td></tr>
                <tr><th>PIC</th><td>{pic}</td></tr>
                <tr><th>Source Issue</th><td>{sourceIssue}</td></tr>
                <tr><th>Issue</th><td>{issueCategory}</td></tr>
                <tr><th>Issue Detail</th><td>{issueDetail}</td></tr>
                <tr><th>Rack</th><td>{rack} - {rackRow} - {rackColumn}</td></tr>
            </table>
            
            <div class=""text-center"">
                <a href=""https://eajdigitization.se.com/SEMB_PEM_QUARANTINE"" style=""background-color: #4CAF50; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; display: inline-block;"">Click Here</a>
            </div>
        </div>
    </div>
</body>
</html>";
        }

        private string GenerateAnalysisCompleteEmailBody(string reqId, string requestorName, string reference, 
            string disposition, string finalStatus, string destSloc, string result)
        {
            return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Analysis Complete Notification</title>
    <style>
        body {{ font-family: Roboto, Arial, sans-serif; line-height: 1.6; color: #333333; margin: 0; padding: 0; }}
        .container {{ max-width: 650px; margin: 0 auto; padding: 20px; }}
        .card {{ background-color: #ffffff; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); padding: 20px; }}
        .highlight {{ font-weight: bold; color: #4CAF50; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""card"">
            <h3>Dear <span class=""highlight"">{requestorName}</span>,</h3>
            <p>Your quarantine request analysis has been completed.</p>
            
            <h4>Request Details:</h4>
            <p><strong>Request ID:</strong> {reqId}</p>
            <p><strong>Reference:</strong> {reference}</p>
            <p><strong>Disposition:</strong> {disposition}</p>
            <p><strong>Final Status:</strong> {finalStatus}</p>
            <p><strong>Destination SLOC:</strong> {destSloc}</p>
            <p><strong>Result:</strong> {result}</p>
            
            <p>Thank you for using SEMB Quarantine Management System.</p>
        </div>
    </div>
</body>
</html>";
        }
        public List<SelectModel> GetIssueFilter(string cell)
        {
            List<SelectModel> data = new List<SelectModel>();
            string query = "SELECT Distinct issue_category FROM tbl_tracking_QAINP WHERE issue_category LIKE @cell ORDER BY issue_category DESC";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@cell", "%" + cell + "%");
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new SelectModel
                            {
                                Text = reader["issue_category"].ToString() ?? string.Empty,
                                Id = reader["issue_category"].ToString() ?? string.Empty
                            };
                            data.Add(data_list);
                        }
                    }
                }
            }

            return data;
        }
        public List<SelectModel> GetPICFilter(string cell)
        {
            List<SelectModel> data = new List<SelectModel>();
            string query = "SELECT Distinct pic FROM tbl_tracking_QAINP WHERE pic LIKE @cell  ORDER BY pic DESC";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@cell", "%" + cell + "%");
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new SelectModel
                            {
                                Text = reader["pic"].ToString() ?? string.Empty,
                                Id = reader["pic"].ToString() ?? string.Empty
                            };
                            data.Add(data_list);
                        }
                    }
                }
            }

            return data;
        }
        public List<SelectModel> GetIssueFilterWaitinApproval(string cell)
        {
            List<SelectModel> data = new List<SelectModel>();
            string query = "SELECT Distinct issue_category FROM tbl_tracking_QAINP WHERE issue_category LIKE @cell and status ='Waiting Approval' ORDER BY issue_category DESC";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@cell", "%" + cell + "%");
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new SelectModel
                            {
                                Text = reader["issue_category"].ToString() ?? string.Empty,
                                Id = reader["issue_category"].ToString() ?? string.Empty
                            };
                            data.Add(data_list);
                        }
                    }
                }
            }

            return data;
        }
        public List<SelectModel> GetPICFilterWaitinApproval(string cell)
        {
            List<SelectModel> data = new List<SelectModel>();
            string query = "SELECT Distinct pic FROM tbl_tracking_QAINP WHERE pic LIKE @cell and status = 'Waiting Approval' ORDER BY pic DESC";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@cell", "%" + cell + "%");
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new SelectModel
                            {
                                Text = reader["pic"].ToString() ?? string.Empty,
                                Id = reader["pic"].ToString() ?? string.Empty
                            };
                            data.Add(data_list);
                        }
                    }
                }
            }

            return data;
        }
        public List<SelectModel> GetIssueFilterDeclined(string cell)
        {
            List<SelectModel> data = new List<SelectModel>();
            string query = "SELECT Distinct issue_category FROM tbl_tracking_QAINP WHERE issue_category LIKE @cell and status ='Declined' ORDER BY issue_category DESC";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@cell", "%" + cell + "%");
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new SelectModel
                            {
                                Text = reader["issue_category"].ToString() ?? string.Empty,
                                Id = reader["issue_category"].ToString() ?? string.Empty
                            };
                            data.Add(data_list);
                        }
                    }
                }
            }

            return data;
        }
        public List<SelectModel> GetPICFilterDeclined(string cell)
        {
            List<SelectModel> data = new List<SelectModel>();
            string query = "SELECT Distinct pic FROM tbl_tracking_QAINP WHERE pic LIKE @cell and status = 'Declined' ORDER BY pic DESC";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@cell", "%" + cell + "%");
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new SelectModel
                            {
                                Text = reader["pic"].ToString() ?? string.Empty,
                                Id = reader["pic"].ToString() ?? string.Empty
                            };
                            data.Add(data_list);
                        }
                    }
                }
            }

            return data;
        }
        
        public List<SelectModel> GetIssueFilterWaitingAnalysis(string cell)
        {
            List<SelectModel> data = new List<SelectModel>();
            string query = "SELECT Distinct issue_category FROM tbl_tracking_QAINP WHERE issue_category LIKE @cell and status ='Waiting Analysis' ORDER BY issue_category DESC";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@cell", "%" + cell + "%");
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new SelectModel
                            {
                                Text = reader["issue_category"].ToString() ?? string.Empty,
                                Id = reader["issue_category"].ToString() ?? string.Empty
                            };
                            data.Add(data_list);
                        }
                    }
                }
            }

            return data;
        }
        public List<SelectModel> GetPICFilterWaitingAnalyis(string cell)
        {
            List<SelectModel> data = new List<SelectModel>();
            string query = "SELECT Distinct pic FROM tbl_tracking_QAINP WHERE pic LIKE @cell and status = 'Waiting Analysis' ORDER BY pic DESC";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@cell", "%" + cell + "%");
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new SelectModel
                            {
                                Text = reader["pic"].ToString() ?? string.Empty,
                                Id = reader["pic"].ToString() ?? string.Empty
                            };
                            data.Add(data_list);
                        }
                    }
                }
            }

            return data;
        }
        
        public List<SelectModel> GetIssueFilterUnderAnalysis(string cell)
        {
            List<SelectModel> data = new List<SelectModel>();
            string query = "SELECT Distinct issue_category FROM tbl_tracking_QAINP WHERE issue_category LIKE @cell and status ='Under Analysis' ORDER BY issue_category DESC";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@cell", "%" + cell + "%");
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new SelectModel
                            {
                                Text = reader["issue_category"].ToString() ?? string.Empty,
                                Id = reader["issue_category"].ToString() ?? string.Empty
                            };
                            data.Add(data_list);
                        }
                    }
                }
            }

            return data;
        }
        public List<SelectModel> GetPICFilterUnderAnalyis(string cell)
        {
            List<SelectModel> data = new List<SelectModel>();
            string query = "SELECT Distinct pic FROM tbl_tracking_QAINP WHERE pic LIKE @cell and status = 'Under Analysis' ORDER BY pic DESC";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@cell", "%" + cell + "%");
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new SelectModel
                            {
                                Text = reader["pic"].ToString() ?? string.Empty,
                                Id = reader["pic"].ToString() ?? string.Empty
                            };
                            data.Add(data_list);
                        }
                    }
                }
            }

            return data;
        }
        
        public List<SelectModel> GetIssueFilterAfterAnalysis(string cell)
        {
            List<SelectModel> data = new List<SelectModel>();
            string query = "SELECT Distinct issue_category FROM tbl_tracking_QAINP WHERE issue_category LIKE @cell and status ='After Analysis' ORDER BY issue_category DESC";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@cell", "%" + cell + "%");
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new SelectModel
                            {
                                Text = reader["issue_category"].ToString() ?? string.Empty,
                                Id = reader["issue_category"].ToString() ?? string.Empty
                            };
                            data.Add(data_list);
                        }
                    }
                }
            }

            return data;
        }
        public List<SelectModel> GetPICFilterAfterAnalyis(string cell)
        {
            List<SelectModel> data = new List<SelectModel>();
            string query = "SELECT Distinct pic FROM tbl_tracking_QAINP WHERE pic LIKE @cell and status = 'After Analysis' ORDER BY pic DESC";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@cell", "%" + cell + "%");
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new SelectModel
                            {
                                Text = reader["pic"].ToString() ?? string.Empty,
                                Id = reader["pic"].ToString() ?? string.Empty
                            };
                            data.Add(data_list);
                        }
                    }
                }
            }

            return data;
        }
        
        public List<SelectModel> GetIssueFilterFinishAnalysis(string cell)
        {
            List<SelectModel> data = new List<SelectModel>();
            string query = "SELECT Distinct issue_category FROM tbl_tracking_QAINP WHERE issue_category LIKE @cell and status ='Finish Analysis' ORDER BY issue_category DESC";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@cell", "%" + cell + "%");
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new SelectModel
                            {
                                Text = reader["issue_category"].ToString() ?? string.Empty,
                                Id = reader["issue_category"].ToString() ?? string.Empty
                            };
                            data.Add(data_list);
                        }
                    }
                }
            }

            return data;
        }
        public List<SelectModel> GetPICFilterFinishAnalyis(string cell)
        {
            List<SelectModel> data = new List<SelectModel>();
            string query = "SELECT Distinct pic FROM tbl_tracking_QAINP WHERE pic LIKE @cell and status = 'Finish Analysis' ORDER BY pic DESC";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@cell", "%" + cell + "%");
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new SelectModel
                            {
                                Text = reader["pic"].ToString() ?? string.Empty,
                                Id = reader["pic"].ToString() ?? string.Empty
                            };
                            data.Add(data_list);
                        }
                    }
                }
            }

            return data;
        }

        public List<SelectModel> GetBoxTypeFilter(string cell)
        {
            List<SelectModel> data = new List<SelectModel>();
            string query = "SELECT id, box_id FROM mst_box WHERE box_id LIKE @cell ORDER BY box_id DESC";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@cell", "%" + cell + "%");
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new SelectModel
                            {
                                Text = reader["box_id"].ToString() ?? string.Empty,
                                Id = reader["id"].ToString() ?? string.Empty
                            };
                            data.Add(data_list);
                        }
                    }
                }
            }

            return data;
        }  
        public List<SelectModel> GetReferenceFilter(string cell)
        {
            List<SelectModel> data = new List<SelectModel>();
            string query = "SELECT id, reference FROM mst_reference_QAS WHERE reference LIKE @cell ORDER BY reference DESC";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@cell", "%" + cell + "%");
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new SelectModel
                            {
                                Text = reader["reference"].ToString() ?? string.Empty,
                                Id = reader["id"].ToString() ?? string.Empty
                            };
                            data.Add(data_list);
                        }
                    }
                }
            }

            return data;
        }  
        public List<SelectModel> GetRemarkFilter(string cell)
        {
            List<SelectModel> data = new List<SelectModel>();
            string query = "SELECT id, remark FROM mst_remark_QAS WHERE remark LIKE @cell ORDER BY remark DESC";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@cell", "%" + cell + "%");
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new SelectModel
                            {
                                Text = reader["remark"].ToString() ?? string.Empty,
                                Id = reader["id"].ToString() ?? string.Empty
                            };
                            data.Add(data_list);
                        }
                    }
                }
            }

            return data;
        } 
        public List<SelectModel> GetSourceIssueFilter(string cell)
        {
            List<SelectModel> data = new List<SelectModel>();
            string query = "SELECT id, issue_source FROM mst_issue WHERE issue_source LIKE @cell ORDER BY issue_source DESC";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@cell", "%" + cell + "%");
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new SelectModel
                            {
                                Text = reader["issue_source"].ToString() ?? string.Empty,
                                Id = reader["id"].ToString() ?? string.Empty
                            };
                            data.Add(data_list);
                        }
                    }
                }
            }

            return data;
        }
        public List<SelectModel> GetIssueCategory(string cell)
        {
            List<SelectModel> data = new List<SelectModel>();
            string query = "SELECT id, issue_category FROM mst_issue WHERE issue_category LIKE @cell ORDER BY issue_category DESC";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@cell", "%" + cell + "%");
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new SelectModel
                            {
                                Text = reader["issue_category"].ToString() ?? string.Empty,
                                Id = reader["id"].ToString() ?? string.Empty
                            };
                            data.Add(data_list);
                        }
                    }
                }
            }

            return data;
        }   
        public List<SelectModel> GetSourceSloc(string cell)
        {
            List<SelectModel> data = new List<SelectModel>();
            string query = "SELECT id, sloc FROM mst_issue WHERE issue_category LIKE @cell ORDER BY issue_category DESC";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@cell", "%" + cell + "%");
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new SelectModel
                            {
                                Text = reader["issue_category"].ToString() ?? string.Empty,
                                Id = reader["id"].ToString() ?? string.Empty
                            };
                            data.Add(data_list);
                        }
                    }
                }
            }

            return data;
        }
        public List<SelectModel> GetSourceSlocFilter(string cell)
        {
            List<SelectModel> data = new List<SelectModel>();
            string query = "SELECT id, sloc, sloc_detail FROM mst_sloc_QAS WHERE sloc LIKE '%" + cell + "%' ORDER BY sloc_detail DESC";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@cell", "%" + cell + "%");
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new SelectModel
                            {
                                Text = reader["sloc"].ToString() + " - " + reader["sloc_detail"].ToString(),
                                Id = reader["id"].ToString() ?? string.Empty
                            };
                            data.Add(data_list);
                        }
                    }
                }
            }

            return data;
        }
        public List<SelectModel> GetDestinationSlocFilter(string cell, string sector)
        {
            List<SelectModel> data = new List<SelectModel>();
            string query = "SELECT id, sloc, sloc_detail FROM mst_sloc_QAS WHERE sloc LIKE '%" + cell + "%' AND id != '" + sector + "' ORDER BY sloc_detail DESC";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@cell", "%" + cell + "%");
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new SelectModel
                            {
                                Text = reader["sloc"].ToString() + " - " + reader["sloc_detail"].ToString(),
                                Id = reader["id"].ToString() ?? string.Empty
                            };
                            data.Add(data_list);
                        }
                    }
                }
            }

            return data;
        }
        public List<SelectModel> GetPicFilter(string cell)
        {
            List<SelectModel> data = new List<SelectModel>();
            string query = "SELECT usr_sesa, usr_name FROM mst_users_QAS WHERE usr_name LIKE '%" + cell + "%' ORDER BY usr_name DESC";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@cell", "%" + cell + "%");
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new SelectModel
                            {
                                Text = reader["usr_name"].ToString() ?? string.Empty,
                                Id = reader["usr_sesa"].ToString() ?? string.Empty
                            };
                            data.Add(data_list);
                        }
                    }
                }
            }

            return data;
        }
        public List<SelectModel> GetDispositionFilter(string cell)
        {
            List<SelectModel> data = new List<SelectModel>();
            string query = "SELECT id, disposition FROM mst_disposition_QAS WHERE disposition LIKE '%" + cell + "%' ORDER BY disposition DESC";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@cell", "%" + cell + "%");
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new SelectModel
                            {
                                Text = reader["disposition"].ToString() ?? string.Empty,
                                Id = reader["id"].ToString() ?? string.Empty
                            };
                            data.Add(data_list);
                        }
                    }
                }
            }

            return data;
        }

        public List<SelectModel> GetrackFilter(string cell)
        {
            List<SelectModel> data = new List<SelectModel>();
            string query = "SELECT id, rack FROM mst_rack_QAS WHERE rack LIKE '%" + cell + "%' ORDER BY rack DESC";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@cell", "%" + cell + "%");
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new SelectModel
                            {
                                Text = reader["rack"].ToString() ?? string.Empty,
                                Id = reader["id"].ToString() ?? string.Empty
                            };
                            data.Add(data_list);
                        }
                    }
                }
            }

            return data;
        }
        public List<SelectModel> GetRowFilter(string cell)
        {
            List<SelectModel> data = new List<SelectModel>();
            string query = "SELECT id, rack_row FROM mst_rack_row_QAS WHERE rack_row LIKE '%" + cell + "%' ORDER BY rack_row DESC";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@cell", "%" + cell + "%");
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new SelectModel
                            {
                                Text = reader["rack_row"].ToString() ?? string.Empty,
                                Id = reader["id"].ToString() ?? string.Empty
                            };
                            data.Add(data_list);
                        }
                    }
                }
            }

            return data;
        }
        public List<SelectModel> GetColumnFilter(string cell)
        {
            List<SelectModel> data = new List<SelectModel>();
            string query = "SELECT id, rack_column FROM mst_rack_column_QAS WHERE rack_column LIKE '%" + cell + "%' ORDER BY rack_column DESC";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@cell", "%" + cell + "%");
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new SelectModel
                            {
                                Text = reader["rack_column"].ToString() ?? string.Empty,
                                Id = reader["id"].ToString() ?? string.Empty
                            };
                            data.Add(data_list);
                        }
                    }
                }
            }

            return data;
        }

        public List<RequestTrackingModel> GetRequestID()
        {
            List<RequestTrackingModel> dataList = new List<RequestTrackingModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("CREATE_REQUEST_ID", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    using SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            RequestTrackingModel row = new RequestTrackingModel();
                            row.req_id = reader["req_id"].ToString() ?? string.Empty;
                            dataList.Add(row);
                        }
                    }
                }
                conn.Close();
            }
            return dataList;
        }
        public List<LoginModel> GetSESAID(string id)
        {
            List<LoginModel> dataList = new List<LoginModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT usr_sesa, usr_id from mst_users_QAS where usr_id = @id", conn))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@id", id);
                    using SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            LoginModel row = new LoginModel();
                            row.sesa_id = reader["usr_sesa"].ToString() ?? string.Empty;
                            dataList.Add(row);
                        }
                    }
                }
                conn.Close();
            }
            return dataList;
        }
        public async Task<int> CreateRequest(string req_id, string sesa_id, string reference, string box_type, string quantity, string rack, string row, string column, string pic, string max_aging, string remark, string ppap, string source_issue, string issue_category, string issue_detail, string disposition, string sloc, string dest_sloc, IFormFile file)
        {
            int rowsAffected = 0;
            string? fileName = null;

            if (file != null && file.Length > 0)
            {
                fileName = Path.GetFileName(file.FileName);
                var filePath = Path.Combine("wwwroot", "images", "upload", fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
            }

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                string query = "CREATE_NEW_REQUEST";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@req_id", req_id);
                cmd.Parameters.AddWithValue("@requestor", sesa_id);
                cmd.Parameters.AddWithValue("@reference", reference);
                cmd.Parameters.AddWithValue("@box_type", box_type);
                cmd.Parameters.AddWithValue("@quantity", quantity);
                cmd.Parameters.AddWithValue("@rack", rack);
                cmd.Parameters.AddWithValue("@rack_row", row);
                cmd.Parameters.AddWithValue("@rack_column", column);
                cmd.Parameters.AddWithValue("@pic", pic);
                cmd.Parameters.AddWithValue("@max_aging", max_aging);
                cmd.Parameters.AddWithValue("@remark", remark);
                cmd.Parameters.AddWithValue("@ppap", ppap ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@source_issue", source_issue);
                cmd.Parameters.AddWithValue("@issue", issue_category);
                cmd.Parameters.AddWithValue("@issue_detail", issue_detail);
                cmd.Parameters.AddWithValue("@disposition", disposition);
                cmd.Parameters.AddWithValue("@source_sloc", sloc);
                cmd.Parameters.AddWithValue("@dest_sloc", dest_sloc);
                cmd.Parameters.AddWithValue("@picture", fileName ?? (object)DBNull.Value);

                conn.Open();
                rowsAffected = await cmd.ExecuteNonQueryAsync();
                conn.Close();
            }

            // Send email after successful creation
            if (rowsAffected > 0 || rowsAffected == -1)
            {
                await SendEmailCreateRequest(req_id, remark);
            }

            // Jika rowsAffected adalah -1, kita bisa mengembalikan 1 untuk menunjukkan bahwa insert berhasil
            return rowsAffected == -1 ? 1 : rowsAffected;
        }
        public List<DateDataModel> GetFilterDate()
        {
            List<DateDataModel> data = new List<DateDataModel>();
            string query = "GET_FILTER_DATE";
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Connection = conn;
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new DateDataModel();
                            data_list.Date_From = Convert.ToDateTime(reader["Date_From"]).ToString("yyyy-MM-dd");
                            data_list.Date_To = Convert.ToDateTime(reader["Date_To"]).ToString("yyyy-MM-dd");
                            data.Add(data_list);
                        }
                    }
                    conn.Close();
                }
            }
            return data;
        }
        public List<RequestModel> GetPendingApprovalData(string date_from, string date_to, string csqm)
        {
            List<RequestModel> dataList = new List<RequestModel>();  // Create a list to return
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "GET_PENDING_APPROVAL_DATA";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Handle date parameters properly
                    if (!string.IsNullOrEmpty(date_from))
                        cmd.Parameters.AddWithValue("@date_from", Convert.ToDateTime(date_from));
                    else
                        cmd.Parameters.AddWithValue("@date_from", DBNull.Value);

                    if (!string.IsNullOrEmpty(date_to))
                        cmd.Parameters.AddWithValue("@date_to", Convert.ToDateTime(date_to));
                    else
                        cmd.Parameters.AddWithValue("@date_to", DBNull.Value);

                      if (!string.IsNullOrEmpty(csqm))
                        cmd.Parameters.AddWithValue("@user_sesa", csqm);
                    else
                        cmd.Parameters.AddWithValue("@user_sesa", DBNull.Value);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            RequestModel data = new RequestModel();  // Create a new model for each row
                            data.id_req = reader["id_req"].ToString() ?? string.Empty;
                            data.req_id = reader["req_id"].ToString() ?? string.Empty;
                            data.requestor = reader["requestor"].ToString() ?? string.Empty;
                            data.reference = reader["reference"].ToString() ?? string.Empty;
                            data.rack = reader["rack"].ToString() ?? string.Empty;
                            data.row = reader["rack_row"].ToString() ?? string.Empty;
                            data.column = reader["rack_column"].ToString() ?? string.Empty;
                            data.box_type = reader["box_type"].ToString() ?? string.Empty;
                            data.quantity = reader["quantity"].ToString() ?? string.Empty;
                            data.remark = reader["remark"].ToString() ?? string.Empty;
                            data.ppap = reader["ppap"].ToString() ?? string.Empty;
                            data.request_date = reader["request_date"] as DateTime? != null ?
                                ((DateTime)reader["request_date"]).ToString("d MMM yyyy HH:mm:ss") : "NA";

                            data.record_date = reader["record_date"] as DateTime? != null ?
                                ((DateTime)reader["record_date"]).ToString("d MMM yyyy HH:mm:ss") : "NA";
                            data.last_update = reader["last_update"] as DateTime? != null ?
                                ((DateTime)reader["last_update"]).ToString("d MMM yyyy HH:mm:ss") : "NA";

                            data.source_issue = reader["source_issue"].ToString() ?? string.Empty;
                            data.issue_category = reader["issue_category"].ToString() ?? string.Empty;
                            data.max_aging = reader["max_aging"] as DateTime? != null ?
                                ((DateTime)reader["max_aging"]).ToString("ddd d MMM yyyy") : "NA";
                            data.issue_detail = reader["issue_detail"].ToString() ?? string.Empty;
                            data.source_sloc = reader["source_sloc"].ToString() ?? string.Empty;
                            data.dest_sloc = reader["dest_sloc"].ToString() ?? string.Empty;
                            data.source_sloc_detail = reader["source_detail"].ToString() ?? string.Empty;
                            data.dest_sloc_detail = reader["dest_detail"].ToString() ?? string.Empty;
                            data.source_sloc_id = reader["source_id"].ToString() ?? string.Empty;
                            data.dest_sloc_id = reader["dest_id"].ToString() ?? string.Empty;
                            data.disposition = reader["disposition"].ToString() ?? string.Empty;
                            data.pic = reader["pic"].ToString() ?? string.Empty;
                            data.picture = reader["picture"].ToString() ?? string.Empty;
                            data.status = reader["status"].ToString() ?? string.Empty;
                            data.updated_coment = reader["updated_coment"].ToString() ?? string.Empty;
                            dataList.Add(data); 
                        }
                    }
                }
                conn.Close();
            }
            return dataList; 
        }
        
        public List<RequestModel> GetWaitingApprovalData(string date_from, string date_to)
        {
            List<RequestModel> dataList = new List<RequestModel>();  // Create a list to return
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "GET_WAITING_APPROVAL_DATA";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Handle date parameters properly
                    if (!string.IsNullOrEmpty(date_from))
                        cmd.Parameters.AddWithValue("@date_from", Convert.ToDateTime(date_from));
                    else
                        cmd.Parameters.AddWithValue("@date_from", DBNull.Value);

                    if (!string.IsNullOrEmpty(date_to))
                        cmd.Parameters.AddWithValue("@date_to", Convert.ToDateTime(date_to));
                    else
                        cmd.Parameters.AddWithValue("@date_to", DBNull.Value);
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            RequestModel data = new RequestModel();  // Create a new model for each row
                            data.id_req = reader["id_req"].ToString() ?? string.Empty;
                            data.req_id = reader["req_id"].ToString() ?? string.Empty;
                            data.requestor = reader["requestor"].ToString() ?? string.Empty;
                            data.reference = reader["reference"].ToString() ?? string.Empty;
                            data.rack = reader["rack"].ToString() ?? string.Empty;
                            data.row = reader["rack_row"].ToString() ?? string.Empty;
                            data.column = reader["rack_column"].ToString() ?? string.Empty;
                            data.box_type = reader["box_type"].ToString() ?? string.Empty;
                            data.quantity = reader["quantity"].ToString() ?? string.Empty;
                            data.remark = reader["remark"].ToString() ?? string.Empty;
                            data.ppap = reader["ppap"].ToString() ?? string.Empty;
                            data.request_date = reader["request_date"] as DateTime? != null ?
                                ((DateTime)reader["request_date"]).ToString("d MMM yyyy HH:mm:ss") : "NA";

                            data.record_date = reader["record_date"] as DateTime? != null ?
                                ((DateTime)reader["record_date"]).ToString("d MMM yyyy HH:mm:ss") : "NA";
                            data.last_update = reader["last_update"] as DateTime? != null ?
                                ((DateTime)reader["last_update"]).ToString("d MMM yyyy HH:mm:ss") : "NA";

                            data.source_issue = reader["source_issue"].ToString() ?? string.Empty;
                            data.issue_category = reader["issue_category"].ToString() ?? string.Empty;
                            data.max_aging = reader["max_aging"] as DateTime? != null ?
                                ((DateTime)reader["max_aging"]).ToString("ddd d MMM yyyy") : "NA";
                            data.issue_detail = reader["issue_detail"].ToString() ?? string.Empty;
                            data.source_sloc = reader["source_sloc"].ToString() ?? string.Empty;
                            data.dest_sloc = reader["dest_sloc"].ToString() ?? string.Empty;
                            data.source_sloc_detail = reader["source_detail"].ToString() ?? string.Empty;
                            data.dest_sloc_detail = reader["dest_detail"].ToString() ?? string.Empty;
                            data.source_sloc_id = reader["source_id"].ToString() ?? string.Empty;
                            data.dest_sloc_id = reader["dest_id"].ToString() ?? string.Empty;
                            data.disposition = reader["disposition"].ToString() ?? string.Empty;
                            data.pic = reader["pic"].ToString() ?? string.Empty;
                            data.picture = reader["picture"].ToString() ?? string.Empty;
                            data.status = reader["status"].ToString() ?? string.Empty;
                            dataList.Add(data); 
                        }
                    }
                }
                conn.Close();
            }
            return dataList; 
        }

        public List<RequestModel> GetDetailDataUser(string id_req)
        {
            List<RequestModel> dataList = new List<RequestModel>();  // Create a list to return

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "GET_WAITING_APPROVAL_DATA_DETAIL";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@id_req", id_req);
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            RequestModel data = new RequestModel();  // Create a new model for each row
                            data.id_req = reader["id_req"].ToString() ?? string.Empty;
                            data.req_id = reader["req_id"].ToString() ?? string.Empty;
                            data.requestor = reader["requestor"].ToString() ?? string.Empty;
                            data.reference = reader["reference"].ToString() ?? string.Empty;
                            data.quantity = reader["quantity"].ToString() ?? string.Empty;
                            data.box_type = reader["box_type"].ToString() ?? string.Empty;
                            data.remark = reader["remark"].ToString() ?? string.Empty;
                            data.request_date = reader["record_date"] as DateTime? != null ? ((DateTime)reader["record_date"]).ToString("d MMM yyyy HH:mm:ss") : "NA";
                            data.source_issue = reader["source_issue"].ToString() ?? string.Empty;
                            data.issue_category = reader["issue_category"].ToString() ?? string.Empty;
                            data.max_aging = reader["max_aging"] as DateTime? != null ? ((DateTime)reader["max_aging"]).ToString("ddd d MMM yyyy") : "NA";
                            data.issue_detail = reader["issue_detail"].ToString() ?? string.Empty;
                            data.dest_sloc = reader["dest_sloc"].ToString() + " - " + reader["dest_sloc_detail"].ToString();
                            data.source_sloc = reader["source_sloc"].ToString() + " - " + reader["source_sloc_detail"].ToString();
                            data.rack = reader["rack"].ToString() + " - " + reader["rack_row"].ToString()+ " - " + reader["rack_column"].ToString();
                            data.disposition = reader["disposition"].ToString() ?? string.Empty;
                            data.pic = reader["pic"].ToString() ?? string.Empty;
                            data.status = reader["status"].ToString() ?? string.Empty;
                            data.ppap = reader["ppap"].ToString() ?? string.Empty;
                            data.picture = reader["picture"].ToString() ?? string.Empty;


                            dataList.Add(data);  // Add each model to the list
                        }
                    }
                }
                conn.Close();
            }
            return dataList;  // Return the list
        } 
        public List<RequestModel> GetDetailDataOverdue(string id_req)
        {
            List<RequestModel> dataList = new List<RequestModel>();  // Create a list to return

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "GET_OVERDUE_DATA_DETAIL";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@id_req", id_req);
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            RequestModel data = new RequestModel();  // Create a new model for each row
                            data.id_req = reader["id_req"].ToString() ?? string.Empty;
                            data.req_id = reader["req_id"].ToString() ?? string.Empty;
                            data.requestor = reader["requestor"].ToString() ?? string.Empty;
                            data.reference = reader["reference"].ToString() ?? string.Empty;
                            data.quantity = reader["quantity"].ToString() ?? string.Empty;
                            data.box_type = reader["box_type"].ToString() ?? string.Empty;
                            data.remark = reader["remark"].ToString() ?? string.Empty;
                            data.request_date = reader["record_date"] as DateTime? != null ? ((DateTime)reader["record_date"]).ToString("d MMM yyyy HH:mm:ss") : "NA";
                            data.source_issue = reader["source_issue"].ToString() ?? string.Empty;
                            data.issue_category = reader["issue_category"].ToString() ?? string.Empty;
                            data.max_aging = reader["max_aging"] as DateTime? != null ? ((DateTime)reader["max_aging"]).ToString("ddd d MMM yyyy") : "NA";
                            data.issue_detail = reader["issue_detail"].ToString() ?? string.Empty;
                            data.dest_sloc = reader["dest_sloc"].ToString() + " - " + reader["dest_sloc_detail"].ToString();
                            data.source_sloc = reader["source_sloc"].ToString() + " - " + reader["source_sloc_detail"].ToString();
                            data.rack = reader["rack"].ToString() + " - " + reader["rack_row"].ToString()+ " - " + reader["rack_column"].ToString();
                            data.disposition = reader["disposition"].ToString() ?? string.Empty;
                            data.pic = reader["pic"].ToString() ?? string.Empty;
                            data.ppap = reader["ppap"].ToString() ?? string.Empty;
                            data.status = reader["status"].ToString() ?? string.Empty;
                            data.picture = reader["picture"].ToString() ?? string.Empty;


                            dataList.Add(data);  // Add each model to the list
                        }
                    }
                }
                conn.Close();
            }
            return dataList;  // Return the list
        }
        public List<RequestModel> GetImages(string id_req)
        {
            List<RequestModel> images = new List<RequestModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                string query = "SELECT picture FROM tbl_tracking_QAINP WHERE id_req = @id_req";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id_req", id_req);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                RequestModel row = new RequestModel();
                                row.picture = reader["picture"].ToString() ?? string.Empty;
                                images.Add(row);
                            }
                        }
                    }
                }
                conn.Close();
            }
            return images;
        }
        public List<RequestModel> GetDetailEdit(string id_req)
        {
            List<RequestModel> dataList = new List<RequestModel>();  // Create a list to return

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "GET_WAITING_APPROVAL_DATA_DETAIL";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@id_req", id_req);
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            RequestModel data = new RequestModel();  // Create a new model for each row
                            data.id_req = reader["id_req"].ToString() ?? string.Empty;
                            data.req_id = reader["req_id"].ToString() ?? string.Empty;
                            data.requestor = reader["requestor"].ToString() ?? string.Empty;
                            data.reference = reader["reference"].ToString() ?? string.Empty;
                            data.quantity = reader["quantity"].ToString() ?? string.Empty;
                            data.box_type = reader["box_type"].ToString() ?? string.Empty;
                            data.remark = reader["remark"].ToString() ?? string.Empty;
                            data.request_date = reader["record_date"] as DateTime? != null ? ((DateTime)reader["record_date"]).ToString("d MMM yyyy HH:mm:ss") : "NA";
                            data.source_issue = reader["source_issue"].ToString() ?? string.Empty;
                            data.issue_category = reader["issue_category"].ToString() ?? string.Empty;
                            data.max_aging = reader["max_aging"] as DateTime? != null ? ((DateTime)reader["max_aging"]).ToString("ddd d MMM yyyy") : "NA";
                            data.issue_detail = reader["issue_detail"].ToString() ?? string.Empty;
                            data.dest_sloc = reader["dest_sloc"].ToString() + " - " + reader["dest_sloc_detail"].ToString();
                            data.source_sloc = reader["source_sloc"].ToString() + " - " + reader["source_sloc_detail"].ToString();
                            data.rack = reader["rack"].ToString() ?? string.Empty;
                            data.ppap = reader["ppap"].ToString() ?? string.Empty;
                            data.row = reader["rack_row"].ToString() ?? string.Empty;
                            data.column = reader["rack_column"].ToString() ?? string.Empty;
                            data.disposition = reader["disposition"].ToString() ?? string.Empty;
                            data.pic = reader["pic"].ToString() ?? string.Empty;
                            data.status = reader["status"].ToString() ?? string.Empty;
                            data.picture = reader["picture"].ToString() ?? string.Empty;


                            dataList.Add(data);  // Add each model to the list
                        }
                    }
                }
                conn.Close();
            }
            return dataList;  // Return the list
        }
        public List<RequestModel> GetDetailDeclinedDataEdit(string id_req)
        {
            List<RequestModel> dataList = new List<RequestModel>();  // Create a list to return

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "GET_DECLINED_DATA_DETAIL";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@id_req", id_req);
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            RequestModel data = new RequestModel();  // Create a new model for each row
                            data.id_req = reader["id_req"].ToString() ?? string.Empty;
                            data.req_id = reader["req_id"].ToString() ?? string.Empty;
                            data.requestor = reader["requestor"].ToString() ?? string.Empty;
                            data.reference = reader["reference"].ToString() ?? string.Empty;
                            data.quantity = reader["quantity"].ToString() ?? string.Empty;
                            data.box_type = reader["box_type"].ToString() ?? string.Empty;
                            data.remark = reader["remark"].ToString() ?? string.Empty;
                            data.request_date = reader["record_date"] as DateTime? != null ? ((DateTime)reader["record_date"]).ToString("d MMM yyyy HH:mm:ss") : "NA";
                            data.source_issue = reader["source_issue"].ToString() ?? string.Empty;
                            data.issue_category = reader["issue_category"].ToString() ?? string.Empty;
                            data.max_aging = reader["max_aging"] as DateTime? != null ? ((DateTime)reader["max_aging"]).ToString("ddd d MMM yyyy") : "NA";
                            data.issue_detail = reader["issue_detail"].ToString() ?? string.Empty;
                            data.dest_sloc = reader["dest_sloc"].ToString() + " - " + reader["dest_sloc_detail"].ToString();
                            data.source_sloc = reader["source_sloc"].ToString() + " - " + reader["source_sloc_detail"].ToString();
                            data.rack = reader["rack"].ToString() ?? string.Empty;
                            data.row = reader["rack_row"].ToString() ?? string.Empty;
                            data.column = reader["rack_column"].ToString() ?? string.Empty;
                            data.disposition = reader["disposition"].ToString() ?? string.Empty;
                            data.ppap = reader["ppap"].ToString() ?? string.Empty;
                            data.pic = reader["pic"].ToString() ?? string.Empty;
                            data.status = reader["status"].ToString() ?? string.Empty;
                            data.picture = reader["picture"].ToString() ?? string.Empty;
                            data.verify_coment = reader["verify_coment"].ToString() ?? string.Empty;
                            data.updated_coment = reader["updated_coment"].ToString() ?? string.Empty;


                            dataList.Add(data);  // Add each model to the list
                        }
                    }
                }
                conn.Close();
            }
            return dataList;  // Return the list
        }
        public async Task<int> EditDataRequest(string req_id, string sesa_id, string reference, string box_type, string quantity, string rack, string row, string column, string pic, string max_aging, string remark, string ppap, string source_issue, string issue_category, string issue_detail, string disposition, string sloc, string dest_sloc, IFormFile file)
        {
            int rowsAffected = 0;
            string? fileName = null;

            try
            {
                // File handling logic
                if (file != null && file.Length > 0)
                {
                    fileName = Path.GetFileName(file.FileName);
                    var filePath = Path.Combine("wwwroot", "images", "upload", fileName);

                    // Ensure directory exists
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? string.Empty);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                }

                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    string query = "EDIT_DATA_REQUEST";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Parameters in the exact order they appear in the stored procedure
                        cmd.Parameters.AddWithValue("@req_id", req_id);
                        cmd.Parameters.AddWithValue("@requestor", sesa_id);
                        cmd.Parameters.AddWithValue("@reference", reference);
                        cmd.Parameters.AddWithValue("@quantity", quantity);
                        cmd.Parameters.AddWithValue("@box_type", box_type);
                        cmd.Parameters.AddWithValue("@ppap", ppap ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@source_issue", source_issue);
                        cmd.Parameters.AddWithValue("@issue", issue_category);
                        cmd.Parameters.AddWithValue("@issue_detail", issue_detail);
                        cmd.Parameters.AddWithValue("@source_sloc", sloc);
                        cmd.Parameters.AddWithValue("@dest_sloc", dest_sloc);
                        cmd.Parameters.AddWithValue("@remark", remark);
                        cmd.Parameters.AddWithValue("@rack", rack);
                        cmd.Parameters.AddWithValue("@rack_row", row);
                        cmd.Parameters.AddWithValue("@rack_column", column);
                        cmd.Parameters.AddWithValue("@max_aging", max_aging);
                        cmd.Parameters.AddWithValue("@picture", fileName ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@pic", pic);
                        cmd.Parameters.AddWithValue("@disposition", disposition);

                        await conn.OpenAsync();
                        rowsAffected = await cmd.ExecuteNonQueryAsync();
                    }
                }

                // Send email after successful edit
                if (rowsAffected > 0 || rowsAffected == -1)
                {
                    await SendEmailEditDataRequest(req_id, remark);
                }

                return rowsAffected == -1 ? 1 : rowsAffected;
            }
            catch (Exception ex)
            {
                // Add logging here
                Console.WriteLine($"Error in EditDataRequest: {ex.Message}");
                throw; // Re-throw to handle at caller level or add more specific handling
            }
        }
        public List<RequestModel> DeleteDataRequest(string id_req)
        {
            List<RequestModel> data = new List<RequestModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "Delete from tbl_tracking_QAINP WHERE id_req = @id_req";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id_req", id_req);

                    conn.Open();

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        data.Add(new RequestModel
                        {
                            id_req = id_req
                        });
                    }
                }
                conn.Close();
            }
            return data;
        }

        public async Task<int> UpdateDeclined(string req_id, string sesa_id, string reference, string box_type, string quantity, string rack, string row, string column, string pic, string max_aging, string remark, string ppap, string source_issue, string issue_category, string issue_detail, string disposition, string sloc, string dest_sloc, IFormFile file)
        {
            int rowsAffected = 0;
            string? fileName = null;

            try
            {
                // File handling logic
                if (file != null && file.Length > 0)
                {
                    fileName = Path.GetFileName(file.FileName);
                    var filePath = Path.Combine("wwwroot", "images", "upload", fileName);

                    // Ensure directory exists
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? string.Empty);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                }

                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    string query = "UPDATE_DECLINED_REQUEST";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Parameters in the exact order they appear in the stored procedure
                        cmd.Parameters.AddWithValue("@req_id", req_id);
                        cmd.Parameters.AddWithValue("@requestor", sesa_id);
                        cmd.Parameters.AddWithValue("@reference", reference);
                        cmd.Parameters.AddWithValue("@quantity", quantity);
                        cmd.Parameters.AddWithValue("@box_type", box_type);
                        cmd.Parameters.AddWithValue("@ppap", ppap ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@source_issue", source_issue);
                        cmd.Parameters.AddWithValue("@issue", issue_category);
                        cmd.Parameters.AddWithValue("@issue_detail", issue_detail);
                        cmd.Parameters.AddWithValue("@source_sloc", sloc);
                        cmd.Parameters.AddWithValue("@dest_sloc", dest_sloc);
                        cmd.Parameters.AddWithValue("@remark", remark);
                        cmd.Parameters.AddWithValue("@rack", rack);
                        cmd.Parameters.AddWithValue("@rack_row", row);
                        cmd.Parameters.AddWithValue("@rack_column", column);
                        cmd.Parameters.AddWithValue("@max_aging", max_aging);
                        cmd.Parameters.AddWithValue("@picture", fileName ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@pic", pic);
                        cmd.Parameters.AddWithValue("@disposition", disposition);

                        await conn.OpenAsync();
                        rowsAffected = await cmd.ExecuteNonQueryAsync();
                    }
                }

                return rowsAffected == -1 ? 1 : rowsAffected;
            }
            catch (Exception ex)
            {
                // Add logging here
                Console.WriteLine($"Error in EditDataRequest: {ex.Message}");
                throw; // Re-throw to handle at caller level or add more specific handling
            }
        }
        public int GetCountAllwaitingApproval()
        {
            int count = 0;
            string query = @"SELECT COUNT(*) AS count 
                  FROM tbl_tracking_QAINP
                  WHERE status = 'Waiting Approval'";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    count = (int)cmd.ExecuteScalar();
                    conn.Close();
                }
            }

            return count;
        }
        
        public int GetCountdeclinedData()
        {
            int count = 0;
            string query = @"SELECT COUNT(*) AS count 
                  FROM tbl_tracking_QAINP
                  WHERE status = 'Declined'";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    count = (int)cmd.ExecuteScalar();
                    conn.Close();
                }
            }

            return count;
        }
        
        public int GetCountWaitingAnalysis()
        {
            int count = 0;
            string query = @"SELECT COUNT(*) AS count 
                  FROM tbl_tracking_QAINP
                  WHERE status = 'Waiting Analysis'";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    count = (int)cmd.ExecuteScalar();
                    conn.Close();
                }
            }

            return count;
        }
        public int GetCountUnderAnalysis()
        {
            int count = 0;
            string query = @"SELECT COUNT(*) AS count 
                  FROM tbl_tracking_QAINP
                  WHERE status = 'Under Analysis' and sap_status = 'Block by SAP'";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    count = (int)cmd.ExecuteScalar();
                    conn.Close();
                }
            }

            return count;
        }
        public int GetCountAfterAnalysis()
        {
            int count = 0;
            string query = @"SELECT COUNT(*) AS count 
                  FROM tbl_tracking_QAINP
                  WHERE status = 'After Analysis'";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    count = (int)cmd.ExecuteScalar();
                    conn.Close();
                }
            }

            return count;
        }
        public int GetCountFinishAnalysis()
        {
            int count = 0;
            string query = @"SELECT COUNT(*) AS count 
                  FROM tbl_tracking_QAINP
                  WHERE status = 'Finish Analysis' and sap_status = 'Unblock by SAP'";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    count = (int)cmd.ExecuteScalar();
                    conn.Close();
                }
            }

            return count;
        }
        
        public int GetCountoverdueAnalysis()
        {
            int count = 0;
            string query = @"SELECT COUNT(*) AS count 
                  FROM tbl_tracking_QAINP
                  WHERE status = 'Overdue'";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    count = (int)cmd.ExecuteScalar();
                    conn.Close();
                }
            }

            return count;
        }

        public int GetCountPendingApproval(string usr_sesa)
        {
            int count = 0;

            try
            {
                // First, get the user's route level
                string userRouteLvl = string.Empty;
                string routeLvlQuery = "SELECT TOP 1 route_lvl FROM mst_approvers_QAS WHERE usr_sesa = @usr_sesa";

                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    using (SqlCommand routeCmd = new SqlCommand(routeLvlQuery, conn))
                    {
                        routeCmd.Parameters.AddWithValue("@usr_sesa", usr_sesa);
                        conn.Open();
                        var result = routeCmd.ExecuteScalar();
                        if (result != null)
                        {
                            userRouteLvl = result.ToString() ?? string.Empty;
                        }
                        conn.Close();
                    }
                }

                // Build the filter based on route level, matching the stored procedure logic
                string filter = "status = 'Waiting Approval'";

                if (userRouteLvl == "Q1 - CS&Q Manager")
                {
                    filter += " AND remark = 'SIL'";
                }
                else if (userRouteLvl == "Q2 - CS&Q Manager")
                {
                    filter += " AND remark = 'Process'";
                }

                // Build the count query
                string query = $@"
            SELECT COUNT(*) AS count 
            FROM tbl_tracking_QAINP WITH (NOLOCK)
            WHERE {filter}";

                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        conn.Open();
                        count = (int)cmd.ExecuteScalar();
                        conn.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception or handle it according to your application's error handling
                Console.WriteLine($"Error in GetCountPendingApproval: {ex.Message}");
                // Optionally rethrow or return a default value
                // throw;
            }

            return count;
        }

        public int GetCountRequestHistory()
        {
            int count = 0;
            string query = @"SELECT COUNT(*) AS count 
                  FROM tbl_tracking_QAINP";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    count = (int)cmd.ExecuteScalar();
                    conn.Close();
                }
            }

            return count;
        }

        public List<RequestModel> GetDeclinedData(string date_from, string date_to)
        {
            List<RequestModel> dataList = new List<RequestModel>();  // Create a list to return

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "GET_DECLINED_DATA";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@date_from", date_from);
                    cmd.Parameters.AddWithValue("@date_to", date_to);
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            RequestModel data = new RequestModel();  // Create a new model for each row
                            data.id_req = reader["id_req"].ToString() ?? string.Empty;
                            data.req_id = reader["req_id"].ToString() ?? string.Empty;
                            data.requestor = reader["requestor"].ToString() ?? string.Empty;
                            data.reference = reader["reference"].ToString() ?? string.Empty;
                            data.rack = reader["rack"].ToString() ?? string.Empty;
                            data.row = reader["rack_row"].ToString() ?? string.Empty;
                            data.column = reader["rack_column"].ToString() ?? string.Empty;
                            data.box_type = reader["box_type"].ToString() ?? string.Empty;
                            data.quantity = reader["quantity"].ToString() ?? string.Empty;
                            data.remark = reader["remark"].ToString() ?? string.Empty;
                            data.ppap = reader["ppap"].ToString() ?? string.Empty;
                            data.request_date = reader["record_date"] as DateTime? != null ? ((DateTime)reader["record_date"]).ToString("d MMM yyyy HH:mm:ss") : "NA";
                            data.source_issue = reader["source_issue"].ToString() ?? string.Empty;
                            data.issue_category = reader["issue_category"].ToString() ?? string.Empty;
                            data.max_aging = reader["max_aging"] as DateTime? != null ? ((DateTime)reader["max_aging"]).ToString("ddd d MMM yyyy") : "NA";
                            data.issue_detail = reader["issue_detail"].ToString() ?? string.Empty;
                            data.source_sloc = reader["source_sloc"].ToString() ?? string.Empty;
                            data.dest_sloc = reader["dest_sloc"].ToString() ?? string.Empty;
                            data.source_sloc_detail = reader["source_detail"].ToString() ?? string.Empty;
                            data.dest_sloc_detail = reader["dest_detail"].ToString() ?? string.Empty;
                            data.source_sloc_id = reader["source_id"].ToString() ?? string.Empty;
                            data.dest_sloc_id = reader["dest_id"].ToString() ?? string.Empty;
                            data.disposition = reader["disposition"].ToString() ?? string.Empty;
                            data.pic = reader["pic"].ToString() ?? string.Empty;
                            data.picture = reader["picture"].ToString() ?? string.Empty;
                            data.status = reader["status"].ToString() ?? string.Empty;

                            dataList.Add(data);  // Add each model to the list
                        }
                    }
                }
                conn.Close();
            }
            return dataList;  // Return the list
        }
        public async Task<int> UpdateDataDeclined(string req_id, string sesa_id, string reference,
     string box_type, string quantity, string rack, string row, string column, string pic,
     string max_aging, string remark, string ppap, string source_issue, string issue_category,
     string issue_detail, string disposition, string sloc, string dest_sloc, IFormFile file,
     string updated_coment)
        {
            int rowsAffected = 0;
            string? fileName = null;

            try
            {
                // Handle file upload
                if (file != null && file.Length > 0)
                {
                    fileName = Path.GetFileName(file.FileName);
                    var uploadPath = Path.Combine("wwwroot", "images", "upload");

                    // Create directory if it doesn't exist
                    if (!Directory.Exists(uploadPath))
                    {
                        Directory.CreateDirectory(uploadPath);
                    }

                    var filePath = Path.Combine(uploadPath, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                }

                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    string query = "UPDATE_DECLINED_REQUEST";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add parameters with proper null handling
                    cmd.Parameters.AddWithValue("@req_id", req_id ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@requestor", sesa_id ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@reference", reference ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@box_type", box_type ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@quantity", quantity ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@rack", rack ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@rack_row", row ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@rack_column", column ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@pic", pic ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@max_aging", max_aging ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@remark", remark ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ppap", ppap ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@source_issue", source_issue ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@issue", issue_category ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@issue_detail", issue_detail ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@disposition", disposition ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@source_sloc", sloc ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@dest_sloc", dest_sloc ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@picture", fileName ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@updated_coment", updated_coment ?? (object)DBNull.Value);

                    conn.Open();
                    rowsAffected = await cmd.ExecuteNonQueryAsync();
                    conn.Close();
                }

                // Send email after successful update
                if (rowsAffected > 0 || rowsAffected == -1)
                {
                    await SendEmailDeclinedData(req_id, remark);
                }

                // If rowsAffected is -1, return 1 to indicate successful execution
                return rowsAffected == -1 ? 1 : rowsAffected;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateDataDeclined DAL: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                throw; // Re-throw to let controller handle the error
            }
        }
        public List<RequestModel> GetWaitingAnalysisData(string date_from, string date_to)
        {
            List<RequestModel> dataList = new List<RequestModel>();  // Create a list to return

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "GET_WAITING_ANALYSIS";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@date_from", date_from);
                    cmd.Parameters.AddWithValue("@date_to", date_to);
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            RequestModel data = new RequestModel();  // Create a new model for each row
                            data.id_req = reader["id_req"].ToString() ?? string.Empty;
                            data.req_id = reader["req_id"].ToString() ?? string.Empty;
                            data.requestor = reader["requestor"].ToString() ?? string.Empty;
                            data.reference = reader["reference"].ToString() ?? string.Empty;
                            data.rack = reader["rack"].ToString() ?? string.Empty;
                            data.row = reader["rack_row"].ToString() ?? string.Empty;
                            data.column = reader["rack_column"].ToString() ?? string.Empty;
                            data.box_type = reader["box_type"].ToString() ?? string.Empty;
                            data.quantity = reader["quantity"].ToString() ?? string.Empty;
                            data.remark = reader["remark"].ToString() ?? string.Empty;
                            data.request_date = reader["record_date"] as DateTime? != null ? ((DateTime)reader["record_date"]).ToString("d MMM yyyy HH:mm:ss") : "NA";
                            data.source_issue = reader["source_issue"].ToString() ?? string.Empty;
                            data.issue_category = reader["issue_category"].ToString() ?? string.Empty;
                            data.max_aging = reader["max_aging"] as DateTime? != null ? ((DateTime)reader["max_aging"]).ToString("ddd d MMM yyyy") : "NA";
                            data.issue_detail = reader["issue_detail"].ToString() ?? string.Empty;
                            data.source_sloc = reader["source_sloc"].ToString() ?? string.Empty;
                            data.dest_sloc = reader["dest_sloc"].ToString() ?? string.Empty;
                            data.disposition = reader["disposition"].ToString() ?? string.Empty;
                            data.pic = reader["pic"].ToString() ?? string.Empty;
                            data.picture = reader["picture"].ToString() ?? string.Empty;
                            data.status = reader["status"].ToString() ?? string.Empty;

                            dataList.Add(data);  // Add each model to the list
                        }
                    }
                }
                conn.Close();
            }
            return dataList;  // Return the list
        }
        public List<RequestModel> GetDetailDataWaitingAnalysis(string id_req)
        {
            List<RequestModel> dataList = new List<RequestModel>();  // Create a list to return

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "GET_WAITING_ANALYSIS_DETAIL";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@id_req", id_req);
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            RequestModel data = new RequestModel();  // Create a new model for each row
                            data.id_req = reader["id_req"].ToString() ?? string.Empty;
                            data.req_id = reader["req_id"].ToString() ?? string.Empty;
                            data.requestor = reader["requestor"].ToString() ?? string.Empty;
                            data.reference = reader["reference"].ToString() ?? string.Empty;
                            data.box_type = reader["box_type"].ToString() ?? string.Empty;
                            data.quantity = reader["quantity"].ToString() ?? string.Empty;
                            data.remark = reader["remark"].ToString() ?? string.Empty;
                            data.request_date = reader["record_date"] as DateTime? != null ? ((DateTime)reader["record_date"]).ToString("d MMM yyyy HH:mm:ss") : "NA";
                            data.source_issue = reader["source_issue"].ToString() ?? string.Empty;
                            data.issue_category = reader["issue_category"].ToString() ?? string.Empty;
                            data.max_aging = reader["max_aging"] as DateTime? != null ? ((DateTime)reader["max_aging"]).ToString("ddd d MMM yyyy") : "NA";
                            data.issue_detail = reader["issue_detail"].ToString() ?? string.Empty;
                            data.dest_sloc = reader["dest_sloc"].ToString() + " - " + reader["dest_sloc_detail"].ToString();
                            data.source_sloc = reader["source_sloc"].ToString() + " - " + reader["source_sloc_detail"].ToString();
                            data.rack = reader["rack"].ToString() + " - " + reader["rack_row"].ToString() + " - " + reader["rack_column"].ToString();
                            data.disposition = reader["disposition"].ToString() ?? string.Empty;
                            data.pic = reader["pic"].ToString() ?? string.Empty;
                            data.ppap = reader["ppap"].ToString() ?? string.Empty;
                            data.picture = reader["picture"].ToString() ?? string.Empty;
                            data.status = reader["status"].ToString() ?? string.Empty;
                            data.verify_coment = reader["verify_coment"]?.ToString() ?? string.Empty;
                            data.sap_status = reader["sap_status"]?.ToString() ?? string.Empty;
                            data.final_status = reader["final_status"]?.ToString() ?? string.Empty;

                            dataList.Add(data);  // Add each model to the list
                        }
                    }
                }
                conn.Close();
            }
            return dataList;  // Return the list
        }
        public List<RequestModel> GetUnderAnalysisData(string date_from, string date_to)
        {
            List<RequestModel> dataList = new List<RequestModel>();  // Create a list to return

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "GET_UNDER_ANALYSIS";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@date_from", date_from);
                    cmd.Parameters.AddWithValue("@date_to", date_to);
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            RequestModel data = new RequestModel();  // Create a new model for each row
                            data.id_req = reader["id_req"].ToString() ?? string.Empty;
                            data.req_id = reader["req_id"].ToString() ?? string.Empty;
                            data.requestor = reader["requestor"].ToString() ?? string.Empty;
                            data.reference = reader["reference"].ToString() ?? string.Empty;
                            data.rack = reader["rack"].ToString() ?? string.Empty;
                            data.row = reader["rack_row"].ToString() ?? string.Empty;
                            data.column = reader["rack_column"].ToString() ?? string.Empty;
                            data.box_type = reader["box_type"].ToString() ?? string.Empty;
                            data.quantity = reader["quantity"].ToString() ?? string.Empty;
                            data.remark = reader["remark"].ToString() ?? string.Empty;
                            data.request_date = reader["record_date"] as DateTime? != null ? ((DateTime)reader["record_date"]).ToString("d MMM yyyy HH:mm:ss") : "NA";
                            data.source_issue = reader["source_issue"].ToString() ?? string.Empty;
                            data.issue_category = reader["issue_category"].ToString() ?? string.Empty;
                            data.max_aging = reader["max_aging"] as DateTime? != null ? ((DateTime)reader["max_aging"]).ToString("ddd d MMM yyyy") : "NA";
                            data.issue_detail = reader["issue_detail"].ToString() ?? string.Empty;
                            data.source_sloc = reader["source_sloc"].ToString() ?? string.Empty;
                            data.dest_sloc = reader["dest_sloc"].ToString() ?? string.Empty;
                            data.dest_sloc_detail = reader["dest_detail"].ToString() ?? string.Empty;
                            data.dest_sloc_id = reader["dest_id"].ToString() ?? string.Empty;
                            data.disposition = reader["disposition"].ToString() ?? string.Empty;
                            data.pic = reader["pic"].ToString() ?? string.Empty;
                            data.picture = reader["picture"].ToString() ?? string.Empty;
                            data.status = reader["status"].ToString() ?? string.Empty;
                            data.verify_coment = reader["verify_coment"].ToString() ?? string.Empty;
                            data.sap_status = reader["sap_status"].ToString() ?? string.Empty;
                            data.final_status = reader["final_status"].ToString() ?? string.Empty;

                            dataList.Add(data);  // Add each model to the list
                        }
                    }
                }
                conn.Close();
            }
            return dataList;  // Return the list
        }
        public List<RequestModel> GetDetailDataUnderAnalysis(string id_req)
        {
            List<RequestModel> dataList = new List<RequestModel>();  // Create a list to return

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "GET_UNDER_ANALYSIS_DETAIL";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@id_req", id_req);
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            RequestModel data = new RequestModel();  // Create a new model for each row
                            data.id_req = reader["id_req"].ToString() ?? string.Empty;
                            data.req_id = reader["req_id"].ToString() ?? string.Empty;
                            data.requestor = reader["requestor"].ToString() ?? string.Empty;
                            data.reference = reader["reference"].ToString() ?? string.Empty;
                            data.box_type = reader["box_type"].ToString() ?? string.Empty;
                            data.quantity = reader["quantity"].ToString() ?? string.Empty;
                            data.remark = reader["remark"].ToString() ?? string.Empty;
                            data.request_date = reader["record_date"] as DateTime? != null ? ((DateTime)reader["record_date"]).ToString("d MMM yyyy HH:mm:ss") : "NA";
                            data.source_issue = reader["source_issue"].ToString() ?? string.Empty;
                            data.issue_category = reader["issue_category"].ToString() ?? string.Empty;
                            data.max_aging = reader["max_aging"] as DateTime? != null ? ((DateTime)reader["max_aging"]).ToString("ddd d MMM yyyy") : "NA";
                            data.issue_detail = reader["issue_detail"].ToString() ?? string.Empty;
                            data.dest_sloc = reader["dest_sloc"].ToString() + " - " + reader["dest_sloc_detail"].ToString();
                            data.source_sloc = reader["source_sloc"].ToString() + " - " + reader["source_sloc_detail"].ToString();
                            data.rack = reader["rack"].ToString() + " - " + reader["rack_row"].ToString() + " - " + reader["rack_column"].ToString();
                            data.disposition = reader["disposition"].ToString() ?? string.Empty;
                            data.pic = reader["pic"].ToString() ?? string.Empty;
                            data.ppap = reader["ppap"].ToString() ?? string.Empty;
                            data.picture = reader["picture"].ToString() ?? string.Empty;
                            data.status = reader["status"].ToString() ?? string.Empty;
                            data.verify_coment = reader["verify_coment"]?.ToString() ?? string.Empty;
                            data.sap_status = reader["sap_status"]?.ToString() ?? string.Empty;
                            data.final_status = reader["final_status"]?.ToString() ?? string.Empty;

                            dataList.Add(data);  // Add each model to the list
                        }
                    }
                }
                conn.Close();
            }
            return dataList;  // Return the list
        }
             
        public List<RequestModel> GetDetailDataUnderAnalysisUpdate(string id_req)
        {
            List<RequestModel> dataList = new List<RequestModel>();  // Create a list to return

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "GET_UNDER_ANALYSIS_DETAIL";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@id_req", id_req);
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            RequestModel data = new RequestModel();  // Create a new model for each row
                            data.id_req = reader["id_req"].ToString() ?? string.Empty;
                            data.req_id = reader["req_id"].ToString() ?? string.Empty;
                            data.requestor = reader["requestor"].ToString() ?? string.Empty;
                            data.reference = reader["reference"].ToString() ?? string.Empty;
                            data.box_type = reader["box_type"].ToString() ?? string.Empty;
                            data.quantity = reader["quantity"].ToString() ?? string.Empty;
                            data.remark = reader["remark"].ToString() ?? string.Empty;
                            data.request_date = reader["record_date"] as DateTime? != null ? ((DateTime)reader["record_date"]).ToString("d MMM yyyy HH:mm:ss") : "NA";
                            data.source_issue = reader["source_issue"].ToString() ?? string.Empty;
                            data.issue_category = reader["issue_category"].ToString() ?? string.Empty;
                            data.max_aging = reader["max_aging"] as DateTime? != null ? ((DateTime)reader["max_aging"]).ToString("ddd d MMM yyyy") : "NA";
                            data.issue_detail = reader["issue_detail"].ToString() ?? string.Empty;
                            data.dest_sloc = reader["dest_sloc"].ToString() + " - " + reader["dest_sloc_detail"].ToString();
                            data.source_sloc = reader["source_sloc"].ToString() + " - " + reader["source_sloc_detail"].ToString();
                            data.rack = reader["rack"].ToString() + " - " + reader["rack_row"].ToString() + " - " + reader["rack_column"].ToString();
                            data.disposition = reader["disposition"].ToString() ?? string.Empty;
                            data.pic = reader["pic"].ToString() ?? string.Empty;
                            data.picture = reader["picture"].ToString() ?? string.Empty;
                            data.status = reader["status"].ToString() ?? string.Empty;
                            data.verify_coment = reader["verify_coment"]?.ToString() ?? string.Empty;
                            data.sap_status = reader["sap_status"]?.ToString() ?? string.Empty;
                            data.final_status = reader["final_status"]?.ToString() ?? string.Empty;

                            dataList.Add(data);  // Add each model to the list
                        }
                    }
                }
                conn.Close();
            }
            return dataList;  // Return the list
        }
        public List<SelectModel> GetSlocFinal(string cell)
        {
            List<SelectModel> data = new List<SelectModel>();
            string query = "SELECT id, sloc, sloc_detail, Description FROM mst_sloc_QAS WHERE sloc LIKE @cell AND Description IS NOT NULL ORDER BY sloc_detail DESC";
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@cell", "%" + cell + "%");
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new SelectModel
                            {
                                Text = reader["sloc"].ToString() + " - " + reader["sloc_detail"].ToString(),
                                Id = reader["id"].ToString() ?? string.Empty
                            };
                            data.Add(data_list);
                        }
                    }
                }
            }
            return data;
        }

        public List<SelectModel> GetFinalStatus(string cell)
        {
            List<SelectModel> data = new List<SelectModel>();
            string query = "SELECT id, sloc, sloc_detail, Description FROM mst_sloc_QAS WHERE sloc LIKE @cell AND Description IS NOT NULL ORDER BY sloc_detail DESC";
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@cell", "%" + cell + "%");
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new SelectModel
                            {
                                Text = reader["Description"].ToString() ?? string.Empty,
                                Id = reader["id"].ToString() ?? string.Empty
                            };
                            data.Add(data_list);
                        }
                    }
                }
            }
            return data;
        }

        public List<SelectModel> GetFinalStatusBySloc(string slocId)
        {
            List<SelectModel> data = new List<SelectModel>();
            // Gunakan ID dari sloc untuk mendapatkan final status yang sesuai
            string query = "SELECT id, Description FROM mst_sloc_QAS WHERE id = @slocId";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@slocId", slocId);
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var description = reader["Description"].ToString();
                            var data_list = new SelectModel
                            {
                                Text = description ?? string.Empty,
                                Id = reader["id"].ToString() ?? string.Empty
                            };
                            data.Add(data_list);
                        }
                    }
                }
            }
            return data;
        }

        public int UpdateRequest(string id_req, string disposition, string final_status, string sloc, string result)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    string query = "UPDATE_UNDER_ANALYSIS";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@id_req", id_req);
                    cmd.Parameters.AddWithValue("@disposition", disposition);
                    cmd.Parameters.AddWithValue("@final_status", final_status);
                    cmd.Parameters.AddWithValue("@dest_sloc", sloc);
                    cmd.Parameters.AddWithValue("@result", result);
                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();
                    conn.Close();

                    // Send email after successful update
                    if (rowsAffected > 0 || rowsAffected == -1)
                    {
                        // Get req_id from id_req
                        string reqId = GetReqIdFromIdReq(id_req);
                        if (!string.IsNullOrEmpty(reqId))
                        {
                            _ = SendEmailUpdateUnderAnalysis(reqId); // Fire and forget
                        }
                    }

                    // Handle -1 return value (which often means success in stored procedures)
                    return rowsAffected == -1 ? 1 : rowsAffected;
                }
            }
            catch (Exception ex)
            {
                // Add logging here
                Console.WriteLine($"Error in UpdateRequest: {ex.Message}");
                throw; // Re-throw to handle at caller level or add more specific handling
            }
        }

        private string GetReqIdFromIdReq(string id_req)
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                var query = "SELECT req_id FROM tbl_tracking_QAINP WHERE id_req = @id_req";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id_req", id_req);
                    conn.Open();
                    var result = cmd.ExecuteScalar();
                    return result?.ToString() ?? "";
                }
            }
        }

        public List<RequestModel> GetAfterAnalysisData(string date_from, string date_to)
        {
            List<RequestModel> dataList = new List<RequestModel>();  // Create a list to return

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "GET_AFTER_ANALYSIS";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@date_from", date_from);
                    cmd.Parameters.AddWithValue("@date_to", date_to);
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            RequestModel data = new RequestModel();  // Create a new model for each row
                            data.id_req = reader["id_req"].ToString() ?? string.Empty;
                            data.req_id = reader["req_id"].ToString() ?? string.Empty;
                            data.requestor = reader["requestor"].ToString() ?? string.Empty;
                            data.reference = reader["reference"].ToString() ?? string.Empty;
                            data.rack = reader["rack"].ToString() ?? string.Empty;
                            data.row = reader["rack_row"].ToString() ?? string.Empty;
                            data.column = reader["rack_column"].ToString() ?? string.Empty;
                            data.box_type = reader["box_type"].ToString() ?? string.Empty;
                            data.quantity = reader["quantity"].ToString() ?? string.Empty;
                            data.remark = reader["remark"].ToString() ?? string.Empty;
                            data.request_date = reader["request_date"] as DateTime? != null ? ((DateTime)reader["request_date"]).ToString("d MMM yyyy HH:mm:ss") : "NA";
                            data.source_issue = reader["source_issue"].ToString() ?? string.Empty;
                            data.issue_category = reader["issue_category"].ToString() ?? string.Empty;
                            data.max_aging = reader["max_aging"] as DateTime? != null ? ((DateTime)reader["max_aging"]).ToString("ddd d MMM yyyy") : "NA";
                            data.issue_detail = reader["issue_detail"].ToString() ?? string.Empty;
                            data.source_sloc = reader["source_sloc"].ToString() ?? string.Empty;
                            data.dest_sloc = reader["dest_sloc"].ToString() ?? string.Empty;
                            data.disposition = reader["disposition"].ToString() ?? string.Empty;
                            data.pic = reader["pic"].ToString() ?? string.Empty;
                            data.picture = reader["picture"].ToString() ?? string.Empty;
                            data.status = reader["status"].ToString() ?? string.Empty;
                            data.verify_coment = reader["verify_coment"].ToString() ?? string.Empty;
                            data.sap_status = reader["sap_status"].ToString() ?? string.Empty;
                            data.final_status = reader["final_status"].ToString() ?? string.Empty;
                            data.finish_date = reader["finish_date"] as DateTime? != null ? ((DateTime)reader["finish_date"]).ToString("d MMM yyyy HH:mm:ss") : "NA";

                            dataList.Add(data);  // Add each model to the list
                        }
                    }
                }
                conn.Close();
            }
            return dataList;  // Return the list
        }
        public List<RequestModel> GetDetailDataAfterAnalysis(string id_req)
        {
            List<RequestModel> dataList = new List<RequestModel>();  // Create a list to return

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "GET_AFTER_ANALYSIS_DETAIL";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@id_req", id_req);
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            RequestModel data = new RequestModel();  // Create a new model for each row
                            data.id_req = reader["id_req"].ToString() ?? string.Empty;
                            data.req_id = reader["req_id"].ToString() ?? string.Empty;
                            data.requestor = reader["requestor"].ToString() ?? string.Empty;
                            data.reference = reader["reference"].ToString() ?? string.Empty;
                            data.box_type = reader["box_type"].ToString() ?? string.Empty;
                            data.quantity = reader["quantity"].ToString() ?? string.Empty;
                            data.remark = reader["remark"].ToString() ?? string.Empty;
                            data.request_date = reader["record_date"] as DateTime? != null ? ((DateTime)reader["record_date"]).ToString("d MMM yyyy HH:mm:ss") : "NA";
                            data.source_issue = reader["source_issue"].ToString() ?? string.Empty;
                            data.issue_category = reader["issue_category"].ToString() ?? string.Empty;
                            data.max_aging = reader["max_aging"] as DateTime? != null ? ((DateTime)reader["max_aging"]).ToString("ddd d MMM yyyy") : "NA";
                            data.issue_detail = reader["issue_detail"].ToString() ?? string.Empty;
                            data.dest_sloc = reader["dest_sloc"].ToString() + " - " + reader["dest_sloc_detail"].ToString();
                            data.source_sloc = reader["source_sloc"].ToString() + " - " + reader["source_sloc_detail"].ToString();
                            data.rack = reader["rack"].ToString() + " - " + reader["rack_row"].ToString() + " - " + reader["rack_column"].ToString();
                            data.disposition = reader["disposition"].ToString() ?? string.Empty;
                            data.ppap = reader["ppap"].ToString() ?? string.Empty;
                            data.pic = reader["pic"].ToString() ?? string.Empty;
                            data.picture = reader["picture"].ToString() ?? string.Empty;
                            data.status = reader["status"].ToString() ?? string.Empty;
                            data.verify_coment = reader["verify_coment"]?.ToString() ?? string.Empty;
                            data.result = reader["result"]?.ToString() ?? string.Empty;
                            data.sap_status = reader["sap_status"]?.ToString() ?? string.Empty;
                            data.final_status = reader["final_status"]?.ToString() ?? string.Empty;

                            dataList.Add(data);  // Add each model to the list
                        }
                    }
                }
                conn.Close();
            }
            return dataList;  // Return the list
        }

        public List<RequestModel> GetFinishAnalysisData(string date_from, string date_to)
        {
            List<RequestModel> dataList = new List<RequestModel>();  // Create a list to return

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "GET_FINISH_ANALYSIS";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@date_from", date_from);
                    cmd.Parameters.AddWithValue("@date_to", date_to);
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            RequestModel data = new RequestModel();  // Create a new model for each row
                            data.id_req = reader["id_req"].ToString() ?? string.Empty;
                            data.req_id = reader["req_id"].ToString() ?? string.Empty;
                            data.requestor = reader["requestor"].ToString() ?? string.Empty;
                            data.reference = reader["reference"].ToString() ?? string.Empty;
                            data.rack = reader["rack"].ToString() ?? string.Empty;
                            data.row = reader["rack_row"].ToString() ?? string.Empty;
                            data.column = reader["rack_column"].ToString() ?? string.Empty;
                            data.box_type = reader["box_type"].ToString() ?? string.Empty;
                            data.quantity = reader["quantity"].ToString() ?? string.Empty;
                            data.remark = reader["remark"].ToString() ?? string.Empty;
                            data.request_date = reader["request_date"] as DateTime? != null ? ((DateTime)reader["request_date"]).ToString("d MMM yyyy HH:mm:ss") : "NA";
                            data.source_issue = reader["source_issue"].ToString() ?? string.Empty;
                            data.issue_category = reader["issue_category"].ToString() ?? string.Empty;
                            data.max_aging = reader["max_aging"] as DateTime? != null ? ((DateTime)reader["max_aging"]).ToString("ddd d MMM yyyy") : "NA";
                            data.issue_detail = reader["issue_detail"].ToString() ?? string.Empty;
                            data.source_sloc = reader["source_sloc"].ToString() ?? string.Empty;
                            data.dest_sloc = reader["dest_sloc"].ToString() ?? string.Empty;
                            data.disposition = reader["disposition"].ToString() ?? string.Empty;
                            data.pic = reader["pic"].ToString() ?? string.Empty;
                            data.picture = reader["picture"].ToString() ?? string.Empty;
                            data.status = reader["status"].ToString() ?? string.Empty;
                            data.verify_coment = reader["verify_coment"].ToString() ?? string.Empty;
                            data.sap_status = reader["sap_status"].ToString() ?? string.Empty;
                            data.final_status = reader["final_status"].ToString() ?? string.Empty;
                            data.finish_date = reader["finish_date"] as DateTime? != null ? ((DateTime)reader["finish_date"]).ToString("d MMM yyyy HH:mm:ss") : "NA";

                            dataList.Add(data);  // Add each model to the list
                        }
                    }
                }
                conn.Close();
            }
            return dataList;  // Return the list
        }

        public List<RequestModel> GetDetailDataFinihsAnalysis(string id_req)
        {
            List<RequestModel> dataList = new List<RequestModel>();  // Create a list to return

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "GET_FINISH_ANALYSIS_DETAIL";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@id_req", id_req);
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            RequestModel data = new RequestModel();  // Create a new model for each row
                            data.id_req = reader["id_req"].ToString() ?? string.Empty;
                            data.req_id = reader["req_id"].ToString() ?? string.Empty;
                            data.requestor = reader["requestor"].ToString() ?? string.Empty;
                            data.reference = reader["reference"].ToString() ?? string.Empty;
                            data.box_type = reader["box_type"].ToString() ?? string.Empty;
                            data.quantity = reader["quantity"].ToString() ?? string.Empty;
                            data.remark = reader["remark"].ToString() ?? string.Empty;
                            data.request_date = reader["record_date"] as DateTime? != null ? ((DateTime)reader["record_date"]).ToString("d MMM yyyy HH:mm:ss") : "NA";
                            data.source_issue = reader["source_issue"].ToString() ?? string.Empty;
                            data.issue_category = reader["issue_category"].ToString() ?? string.Empty;
                            data.max_aging = reader["max_aging"] as DateTime? != null ? ((DateTime)reader["max_aging"]).ToString("ddd d MMM yyyy") : "NA";
                            data.issue_detail = reader["issue_detail"].ToString() ?? string.Empty;
                            data.dest_sloc = reader["dest_sloc"].ToString() + " - " + reader["dest_sloc_detail"].ToString();
                            data.source_sloc = reader["source_sloc"].ToString() + " - " + reader["source_sloc_detail"].ToString();
                            data.rack = reader["rack"].ToString() + " - " + reader["rack_row"].ToString() + " - " + reader["rack_column"].ToString();
                            data.disposition = reader["disposition"].ToString() ?? string.Empty;
                            data.pic = reader["pic"].ToString() ?? string.Empty;
                            data.ppap = reader["ppap"].ToString() ?? string.Empty;
                            data.picture = reader["picture"].ToString() ?? string.Empty;
                            data.status = reader["status"].ToString() ?? string.Empty;
                            data.verify_coment = reader["verify_coment"]?.ToString() ?? string.Empty;
                            data.result = reader["result"]?.ToString() ?? string.Empty;
                            data.sap_status = reader["sap_status"]?.ToString() ?? string.Empty;
                            data.final_status = reader["final_status"]?.ToString() ?? string.Empty;

                            dataList.Add(data);  // Add each model to the list
                        }
                    }
                }
                conn.Close();
            }
            return dataList;  // Return the list
        }

        public bool ChangePassword(string usr_id, string usr_password)
        {
            try
            {
                Authentication hashpassword = new Authentication();
                string hashedPassword = hashpassword.MD5Hash(usr_password);

                LoginModel user = new LoginModel
                {

                    id = usr_id, // Ubah sesuai tipe data yang sesuai dengan kolom di database
                    password = hashedPassword
                };

                // Lakukan operasi update ke database menggunakan model pengguna
                UpdateUserInDatabase(user);

                Console.WriteLine("User Updated Successfully");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }

            return true;
        }

        private void UpdateUserInDatabase(LoginModel user)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(ConnectionString))
                {
                    con.Open();
                    string query = $"UPDATE [SEMB_QAMANAGEMENT].[dbo].[mst_users_QAS] SET usr_password = '{user.password}' " +
                                   $"WHERE usr_id = {user.id}";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                // Handle exception, log, or throw if necessary
            }
        }
        public DashboardModel GetChartFinalStatus()
        {
            DashboardModel data = new DashboardModel();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "GET_CHART_FINAL_STATUS";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            data.total_request = Convert.ToInt32(reader["totalreq"].ToString());
                            data.part_number = reader["part_number"].ToString() ?? string.Empty;
                            data.back_to_production = Convert.ToInt32(reader["back_to_production"].ToString());
                            data.scrap = Convert.ToInt32(reader["scrap"].ToString());
                            data.send_to_blp = Convert.ToInt32(reader["send_to_blp"].ToString());
                            data.send_to_supplier = Convert.ToInt32(reader["send_to_supplier"].ToString());
                        }
                    }
                }
                conn.Close();
            }
            return data;
        }
        public List<RequestModel> GetChartFinalStatusRequestDetail(string finalStatus)
        {
            List<RequestModel> data = new List<RequestModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "GET_CHART_FINAL_STATUS_REQUEST_DETAIL";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@finalStatus", finalStatus);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new RequestModel
                            {
                                id_req = reader["id_req"].ToString() ?? string.Empty,
                                req_id = reader["req_id"].ToString() ?? string.Empty,
                                requestor = reader["req_name"].ToString() ?? string.Empty,
                                reference = reader["reference"].ToString() ?? string.Empty,
                                quantity = reader["quantity"].ToString() ?? string.Empty,
                                rack = reader["rack"].ToString() ?? string.Empty,
                                remark = reader["remark"].ToString() ?? string.Empty,
                                box_type = reader["box_type"].ToString() ?? string.Empty,
                                max_aging = reader["max_aging"].ToString() ?? string.Empty,
                                source_issue = reader["source_issue"].ToString() ?? string.Empty,
                                issue_category = reader["issue_category"].ToString() ?? string.Empty,
                                result = reader["result"]?.ToString() ?? string.Empty.ToString() ?? string.Empty,
                                issue_detail = reader["issue_detail"].ToString() ?? string.Empty,
                                dest_sloc = reader["dest_sloc"].ToString() + " - " + reader["dest_sloc_detail"].ToString(),
                                status = reader["status"].ToString() ?? string.Empty,
                                pic = reader["pic"].ToString() ?? string.Empty,
                                final_status = reader["final_status"].ToString() ?? string.Empty,
                                request_date = reader["record_date"] as DateTime? != null ? ((DateTime)reader["record_date"]).ToString("d MMM yyyy HH:mm:ss") : "NA",
                                finish_date = reader["finish_date"] as DateTime? != null ? ((DateTime)reader["finish_date"]).ToString("d MMM yyyy HH:mm:ss") : "NA"
                            };
                            data.Add(data_list);

                        }
                    }
                }
                conn.Close();
            }
            return data;
        }
        public List<DashboardModel> GetChartPartNumber()
        {
            List<DashboardModel> dataList = new List<DashboardModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "GET_CHART_PART_NUMBER";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data = new DashboardModel
                            {
                                reference = reader["reference"].ToString() ?? string.Empty,
                                total = Convert.ToInt32(reader["total"].ToString() ?? string.Empty)
                            };
                            dataList.Add(data);
                        }
                    }
                }
                conn.Close();
            }
            return dataList;
        }
        public List<RequestModel> GetChartPartDetail(string reference)
        {
            List<RequestModel> data = new List<RequestModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "GET_CHART_PART_NUMBER_DETAIL";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@reference", reference);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new RequestModel
                            {
                                id_req = reader["id_req"].ToString() ?? string.Empty,
                                req_id = reader["req_id"].ToString() ?? string.Empty,
                                requestor = reader["req_name"].ToString() ?? string.Empty,
                                reference = reader["reference"].ToString() ?? string.Empty,
                                quantity = reader["quantity"].ToString() ?? string.Empty,
                                rack = reader["rack"].ToString() ?? string.Empty,
                                remark = reader["remark"].ToString() ?? string.Empty,
                                box_type = reader["box_type"].ToString() ?? string.Empty,
                                max_aging = reader["max_aging"].ToString() ?? string.Empty,
                                source_issue = reader["source_issue"].ToString() ?? string.Empty,
                                issue_category = reader["issue_category"].ToString() ?? string.Empty,
                                result = reader["result"]?.ToString() ?? string.Empty.ToString() ?? string.Empty,
                                issue_detail = reader["issue_detail"].ToString() ?? string.Empty,
                                dest_sloc = reader["dest_sloc"].ToString() + " - " + reader["dest_sloc_detail"].ToString(),
                                status = reader["status"].ToString() ?? string.Empty,
                                final_status = reader["final_status"].ToString() ?? string.Empty,
                                pic = reader["pic"].ToString() ?? string.Empty,
                                request_date = reader["record_date"] as DateTime? != null ? ((DateTime)reader["record_date"]).ToString("d MMM yyyy HH:mm:ss") : "NA",
                                finish_date = reader["finish_date"] as DateTime? != null ? ((DateTime)reader["finish_date"]).ToString("d MMM yyyy HH:mm:ss") : "NA"
                            };
                            data.Add(data_list);

                        }
                    }
                }
                conn.Close();
            }
            return data;
        }
        public List<DashboardModel> GetChartSourceIssue()
        {
            List<DashboardModel> dataList = new List<DashboardModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "GET_CHART_ISSUE_SOURCE";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data = new DashboardModel
                            {
                                source_issue = reader["source_issue"].ToString() ?? string.Empty,
                                total = Convert.ToInt32(reader["total"].ToString())
                            };
                            dataList.Add(data);
                        }
                    }
                }
                conn.Close();
            }
            return dataList;
        }

        public List<RequestModel> GetChartSourceIssueDetail(string source_issue)
        {
            List<RequestModel> data = new List<RequestModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "GET_CHART_SOURCE_ISSUE_DETAIL";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@source_issue", source_issue);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new RequestModel
                            {
                                id_req = reader["id_req"].ToString() ?? string.Empty,
                                req_id = reader["req_id"].ToString() ?? string.Empty,
                                requestor = reader["req_name"].ToString() ?? string.Empty,
                                reference = reader["reference"].ToString() ?? string.Empty,
                                quantity = reader["quantity"].ToString() ?? string.Empty,
                                rack = reader["rack"].ToString() ?? string.Empty,
                                remark = reader["remark"].ToString() ?? string.Empty,
                                box_type = reader["box_type"].ToString() ?? string.Empty,
                                max_aging = reader["max_aging"].ToString() ?? string.Empty,
                                source_issue = reader["source_issue"].ToString() ?? string.Empty,
                                issue_category = reader["issue_category"].ToString() ?? string.Empty,
                                result = reader["result"]?.ToString() ?? string.Empty.ToString() ?? string.Empty,
                                issue_detail = reader["issue_detail"].ToString() ?? string.Empty,
                                dest_sloc = reader["dest_sloc"].ToString() + " - " + reader["dest_sloc_detail"].ToString(),
                                status = reader["status"].ToString() ?? string.Empty,
                                final_status = reader["final_status"].ToString() ?? string.Empty,
                                pic = reader["pic"].ToString() ?? string.Empty,
                                request_date = reader["record_date"] as DateTime? != null ? ((DateTime)reader["record_date"]).ToString("d MMM yyyy HH:mm:ss") : "NA",
                                finish_date = reader["finish_date"] as DateTime? != null ? ((DateTime)reader["finish_date"]).ToString("d MMM yyyy HH:mm:ss") : "NA"
                            };
                            data.Add(data_list);

                        }
                    }
                }
                conn.Close();
            }
            return data;
        }

        public List<DashboardModel> GetChartRequestor()
        {
            List<DashboardModel> dataList = new List<DashboardModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "GET_CHART_REQUESTOR";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data = new DashboardModel
                            {
                                requestor = reader["requestor"].ToString() ?? string.Empty,
                                total = Convert.ToInt32(reader["total"].ToString())
                            };
                            dataList.Add(data);
                        }
                    }
                }
                conn.Close();
            }
            return dataList;
        }

        public List<RequestModel> GetChartRequestDetail(string requestor)
        {
            List<RequestModel> data = new List<RequestModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "GET_CHART_REQUESTOR_DETAIL";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@requestor", requestor);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new RequestModel
                            {
                                id_req = reader["id_req"].ToString() ?? string.Empty,
                                req_id = reader["req_id"].ToString() ?? string.Empty,
                                requestor = reader["req_name"].ToString() ?? string.Empty,
                                reference = reader["reference"].ToString() ?? string.Empty,
                                quantity = reader["quantity"].ToString() ?? string.Empty,
                                rack = reader["rack"].ToString() ?? string.Empty,
                                remark = reader["remark"].ToString() ?? string.Empty,
                                box_type = reader["box_type"].ToString() ?? string.Empty,
                                max_aging = reader["max_aging"].ToString() ?? string.Empty,
                                source_issue = reader["source_issue"].ToString() ?? string.Empty,
                                issue_category = reader["issue_category"].ToString() ?? string.Empty,
                                result = reader["result"]?.ToString() ?? string.Empty.ToString() ?? string.Empty,
                                issue_detail = reader["issue_detail"].ToString() ?? string.Empty,
                                dest_sloc = reader["dest_sloc"].ToString() + " - " + reader["dest_sloc_detail"].ToString(),
                                status = reader["status"].ToString() ?? string.Empty,
                                final_status = reader["final_status"].ToString() ?? string.Empty,
                                pic = reader["pic"].ToString() ?? string.Empty,
                                request_date = reader["record_date"] as DateTime? != null ? ((DateTime)reader["record_date"]).ToString("d MMM yyyy HH:mm:ss") : "NA",
                                finish_date = reader["finish_date"] as DateTime? != null ? ((DateTime)reader["finish_date"]).ToString("d MMM yyyy HH:mm:ss") : "NA"
                            };
                            data.Add(data_list);

                        }
                    }
                }
                conn.Close();
            }
            return data;
        }

        public List<RequestModel> GetChartRequestDetail()
        {
            List<RequestModel> data = new List<RequestModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "GET_CHART_REQUEST_LIST";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new RequestModel
                            {
                                id_req = reader["id_req"].ToString() ?? string.Empty,
                                req_id = reader["req_id"].ToString() ?? string.Empty,
                                requestor = reader["req_name"].ToString() ?? string.Empty,
                                reference = reader["reference"].ToString() ?? string.Empty,
                                quantity = reader["quantity"].ToString() ?? string.Empty,
                                rack = reader["rack"].ToString() ?? string.Empty,
                                remark = reader["remark"].ToString() ?? string.Empty,
                                box_type = reader["box_type"].ToString() ?? string.Empty,
                                max_aging = reader["max_aging"].ToString() ?? string.Empty,
                                source_issue = reader["source_issue"].ToString() ?? string.Empty,
                                issue_category = reader["issue_category"].ToString() ?? string.Empty,
                                result = reader["result"]?.ToString() ?? string.Empty.ToString() ?? string.Empty,
                                issue_detail = reader["issue_detail"].ToString() ?? string.Empty,
                                dest_sloc = reader["dest_sloc"].ToString() + " - " + reader["dest_sloc_detail"].ToString(),
                                status = reader["status"].ToString() ?? string.Empty,
                                final_status = reader["final_status"].ToString() ?? string.Empty,
                                pic = reader["pic"].ToString() ?? string.Empty,
                                request_date = reader["record_date"] as DateTime? != null ? ((DateTime)reader["record_date"]).ToString("d MMM yyyy HH:mm:ss") : "NA",
                                finish_date = reader["finish_date"] as DateTime? != null ? ((DateTime)reader["finish_date"]).ToString("d MMM yyyy HH:mm:ss") : "NA"
                            };
                            data.Add(data_list);

                        }
                    }
                }
                conn.Close();
            }
            return data;
        }

        public List<RequestModel> GetChartRequestList()
        {
            List<RequestModel> data = new List<RequestModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "GET_CHART_REQUEST_LIST";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new RequestModel
                            {
                                id_req = reader["id_req"].ToString() ?? string.Empty,
                                req_id = reader["req_id"].ToString() ?? string.Empty,
                                requestor = reader["req_name"].ToString() ?? string.Empty,
                                reference = reader["reference"].ToString() ?? string.Empty,
                                quantity = reader["quantity"].ToString() ?? string.Empty,
                                rack = reader["rack"].ToString() ?? string.Empty,
                                remark = reader["remark"].ToString() ?? string.Empty,
                                box_type = reader["box_type"].ToString() ?? string.Empty,
                                max_aging = reader["max_aging"].ToString() ?? string.Empty,
                                source_issue = reader["source_issue"].ToString() ?? string.Empty,
                                issue_category = reader["issue_category"].ToString() ?? string.Empty,
                                result = reader["result"].ToString() ?? string.Empty,
                                issue_detail = reader["issue_detail"].ToString() ?? string.Empty,
                                dest_sloc = reader["dest_sloc"].ToString() + " - " + reader["dest_sloc_detail"].ToString(),
                                status = reader["status"].ToString() ?? string.Empty,
                                final_status = reader["final_status"].ToString() ?? string.Empty,
                                pic = reader["pic"].ToString() ?? string.Empty,
                                request_date = reader["record_date"] as DateTime? != null ? ((DateTime)reader["record_date"]).ToString("d MMM yyyy HH:mm:ss") : "NA",
                                finish_date = reader["finish_date"] as DateTime? != null ? ((DateTime)reader["finish_date"]).ToString("d MMM yyyy HH:mm:ss") : "NA"
                            };
                            data.Add(data_list);

                        }
                    }
                }
                conn.Close();
            }
            return data;
        }
        public List<RequestModel> GetChartPartList()
        {
            List<RequestModel> data = new List<RequestModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "GET_CHART_PART_LIST";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new RequestModel
                            {
                                reference = reader["reference"].ToString() ?? string.Empty,
                                quantity = reader["quantity"].ToString() ?? string.Empty,
                                rack = reader["rack"].ToString() ?? string.Empty,

                            };
                            data.Add(data_list);

                        }
                    }
                }
                conn.Close();
            }
            return data;
        }

        public int ApproveRequest(string id_req, string status, string verify_coment, string approver)
        {
            int rowsAffected = 0;

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                string query = "APPROVE_REQUEST";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@id_req", id_req);
                cmd.Parameters.AddWithValue("@status", status);
                cmd.Parameters.AddWithValue("@verify_coment", verify_coment);
                cmd.Parameters.AddWithValue("@approver", approver);
                conn.Open();
                rowsAffected = cmd.ExecuteNonQuery();
                conn.Close();
            }
            return rowsAffected;
        }

        public RequestModel GetDetailPendingApprovalUpdate(string id_req)
        {
            RequestModel data = new RequestModel();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "GET_WAITING_APPROVAL_DATA_DETAIL";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@id_req", id_req);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            data.id_req = reader["id_req"].ToString() ?? string.Empty;
                            data.req_id = reader["req_id"].ToString() ?? string.Empty;
                            data.requestor = reader["requestor"].ToString() ?? string.Empty;
                            data.reference = reader["reference"].ToString() ?? string.Empty;
                            data.quantity = reader["quantity"].ToString() ?? string.Empty;
                            data.rack = reader["rack"].ToString() + " - " + reader["rack_row"].ToString() + " - " + reader["rack_column"].ToString();
                            data.remark = reader["remark"].ToString() ?? string.Empty;
                            data.box_type = reader["box_type"].ToString() ?? string.Empty;
                            data.max_aging = reader["max_aging"].ToString() ?? string.Empty;
                            data.source_issue = reader["source_issue"].ToString() ?? string.Empty;
                            data.issue_category = reader["issue_category"].ToString() ?? string.Empty;
                            data.issue_detail = reader["issue_detail"].ToString() ?? string.Empty;
                            data.source_sloc = reader["source_sloc"].ToString() + " - " + reader["source_sloc_detail"].ToString();
                            data.dest_sloc = reader["dest_sloc"].ToString() + " - " + reader["dest_sloc_detail"].ToString();
                            data.status = reader["status"].ToString() ?? string.Empty;
                            data.disposition = reader["disposition"].ToString() ?? string.Empty;
                            data.pic = reader["pic"].ToString() ?? string.Empty;
                            data.ppap = reader["ppap"].ToString() ?? string.Empty;
                            data.request_date = reader["request_date"] as DateTime? != null ? ((DateTime)reader["record_date"]).ToString("d MMM yyyy HH:mm:ss") : "NA";
                        }
                    }
                }
                conn.Close();
            }
            return data;
        }
        
        public RequestModel GetDetailUpdatedDeclined(string id_req)
        {
            RequestModel data = new RequestModel();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "GET_WAITING_APPROVAL_DATA_DETAIL";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@id_req", id_req);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            data.id_req = reader["id_req"].ToString() ?? string.Empty;
                            data.req_id = reader["req_id"].ToString() ?? string.Empty;
                            data.requestor = reader["requestor"].ToString() ?? string.Empty;
                            data.reference = reader["reference"].ToString() ?? string.Empty;
                            data.quantity = reader["quantity"].ToString() ?? string.Empty;
                            data.rack = reader["rack"].ToString() + " - " + reader["rack_row"].ToString() + " - " + reader["rack_column"].ToString();
                            data.remark = reader["remark"].ToString() ?? string.Empty;
                            data.box_type = reader["box_type"].ToString() ?? string.Empty;
                            data.max_aging = reader["max_aging"].ToString() ?? string.Empty;
                            data.source_issue = reader["source_issue"].ToString() ?? string.Empty;
                            data.issue_category = reader["issue_category"].ToString() ?? string.Empty;
                            data.issue_detail = reader["issue_detail"].ToString() ?? string.Empty;
                            data.source_sloc = reader["source_sloc"].ToString() + " - " + reader["source_sloc_detail"].ToString();
                            data.dest_sloc = reader["dest_sloc"].ToString() + " - " + reader["dest_sloc_detail"].ToString();
                            data.status = reader["status"].ToString() ?? string.Empty;
                            data.disposition = reader["disposition"].ToString() ?? string.Empty;
                            data.pic = reader["pic"].ToString() ?? string.Empty;
                            data.ppap = reader["ppap"].ToString() ?? string.Empty;
                            data.updated_coment = reader["updated_coment"].ToString() ?? string.Empty;
                            data.request_date = reader["request_date"] as DateTime? != null ? ((DateTime)reader["record_date"]).ToString("d MMM yyyy HH:mm:ss") : "NA";
                        }
                    }
                }
                conn.Close();
            }
            return data;
        }

        public List<RequestModel> GetAllDataHistory(string date_from, string date_to)
        {
            List<RequestModel> dataList = new List<RequestModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "GET_REQUEST_HISTORY";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@date_from", date_from);
                    cmd.Parameters.AddWithValue("@date_to", date_to);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            RequestModel data = new RequestModel
                            {
                                id_req = reader["id_req"].ToString() ?? string.Empty,
                                req_id = reader["req_id"].ToString() ?? string.Empty,
                                requestor = reader["requestor"].ToString() ?? string.Empty,
                                reference = reader["reference"].ToString() ?? string.Empty,
                                quantity = reader["quantity"].ToString() ?? string.Empty,
                                rack = reader["rack"].ToString() + " - " + reader["rack_row"].ToString() + " - " + reader["rack_column"].ToString(),
                                remark = reader["remark"].ToString() ?? string.Empty,
                                box_type = reader["box_type"].ToString() ?? string.Empty,
                                max_aging = reader["max_aging"].ToString() ?? string.Empty,
                                source_issue = reader["source_issue"].ToString() ?? string.Empty,
                                issue_category = reader["issue_category"].ToString() ?? string.Empty,
                                issue_detail = reader["issue_detail"].ToString() ?? string.Empty,
                                source_sloc = reader["source_sloc"].ToString() + " - " + reader["source_sloc_detail"].ToString(),
                                dest_sloc = reader["dest_sloc"].ToString() + " - " + reader["dest_sloc_detail"].ToString(),
                                status = reader["status"].ToString() ?? string.Empty,
                                disposition = reader["disposition"].ToString() ?? string.Empty,
                                pic = reader["pic"].ToString() ?? string.Empty,
                                request_date = reader["request_date"] as DateTime? != null ? ((DateTime)reader["record_date"]).ToString("d MMM yyyy HH:mm:ss") : "NA"
                            };
                            dataList.Add(data);
                        }
                    }
                }
                conn.Close();
            }
            return dataList;
        }

        public List<LoginModel> GetAllDataUser(string name, string roles)
        {
            List<LoginModel> dataList = new List<LoginModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "GET_DATA_USER_QAS";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@usr_name", name ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@roles", roles ?? (object)DBNull.Value);
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            LoginModel data = new LoginModel
                            {
                                id = reader["usr_id"].ToString() ?? string.Empty,
                                sesa_id = reader["usr_sesa"].ToString() ?? string.Empty,
                                name = reader["usr_name"].ToString() ?? string.Empty,
                                email = reader["usr_email"].ToString() ?? string.Empty,
                                roles = reader["roles"].ToString() ?? string.Empty,
                                level = reader["access"].ToString() ?? string.Empty,
                                record_date = reader["usr_record_date"] as DateTime? != null
                                    ? ((DateTime)reader["usr_record_date"]).ToString("d MMM yyyy HH:mm:ss")
                                    : "NA"
                            };
                            dataList.Add(data);
                        }
                    }
                }
                conn.Close();
            }
            return dataList;
        }
        public List<SelectModel> GetNameFilter(string cell)
        {
            List<SelectModel> data = new List<SelectModel>();
            string query = "SELECT DISTINCT usr_id, usr_name FROM mst_users_QAS WHERE usr_name LIKE @cell ORDER BY usr_name DESC";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@cell", "%" + cell + "%");
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new SelectModel
                            {
                                Text = reader["usr_name"].ToString() ?? string.Empty,
                                Id = reader["usr_id"].ToString() ?? string.Empty
                            };
                            data.Add(data_list);
                        }
                    }
                }
            }

            return data;
        }
        
        public List<SelectModel> GetRoleFilter(string cell)
        {
            List<SelectModel> data = new List<SelectModel>();
            string query = "SELECT DISTINCT roles FROM mst_users_QAS WHERE roles LIKE @cell ORDER BY roles DESC";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@cell", "%" + cell + "%");
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new SelectModel
                            {
                                Text = reader["roles"].ToString() ?? string.Empty,
                                Id = reader["roles"].ToString() ?? string.Empty
                            };
                            data.Add(data_list);
                        }
                    }
                }
            }

            return data;
        }
        public List<SelectModel> GetLevelFilter(string cell)
        {
            var data = new List<SelectModel>();
            var query = @"
        SELECT DISTINCT a.usr_level, b.access 
        FROM mst_users_QAS AS a 
        LEFT JOIN mst_access AS b ON a.usr_level = b.id 
        WHERE usr_level LIKE @cell 
        ORDER BY usr_level DESC";

            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@cell", "%" + cell + "%");
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var dataList = new SelectModel
                        {
                            Text = reader["access"].ToString() ?? string.Empty,
                            Id = reader["usr_level"].ToString() ?? string.Empty
                        };
                        data.Add(dataList);
                    }
                }
            }

            return data;
        }

        public List<LoginModel> AddNewUserDataList(string sesa_id, string name, string email, string password, string role, string level)
        {
            if (string.IsNullOrEmpty(password))
            {
                password = "123";
            }
            var hashpassword = new Authentication();
            string passwordHash = hashpassword.MD5Hash(password);
            List<LoginModel> data = new List<LoginModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                try
                {
                    // Check if the user already exists
                    string checkQuery = "SELECT COUNT(*) FROM mst_users_QAS WHERE usr_sesa = @usr_sesa";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@usr_sesa", sesa_id);
                        conn.Open();
                        int userExists = (int)checkCmd.ExecuteScalar();
                        conn.Close();

                        if (userExists > 0)
                        {
                            // User already exists, return an empty list or handle as needed
                            Console.WriteLine("User  with sesa_id already exists.");
                            return data;
                        }
                    }

                    // Insert new user data
                    string query = "INSERT INTO mst_users_QAS (usr_sesa, usr_name, usr_email, usr_password, roles, usr_level, usr_record_date) VALUES (@usr_sesa, @usr_name, @usr_email, @usr_password, @roles, @usr_level, GETDATE())";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.Parameters.AddWithValue("@usr_sesa", sesa_id);
                        cmd.Parameters.AddWithValue("@usr_name", name);
                        cmd.Parameters.AddWithValue("@usr_email", email);
                        cmd.Parameters.AddWithValue("@usr_password", passwordHash);
                        cmd.Parameters.AddWithValue("@roles", role);
                        cmd.Parameters.AddWithValue("@usr_level", level);

                        conn.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();
                        conn.Close();

                        if (rowsAffected > 0)
                        {
                            // Retrieve the newly added user data
                            string selectQuery = "SELECT usr_sesa, usr_name, usr_email, usr_password, roles, usr_level, usr_record_date FROM mst_users_QAS WHERE usr_sesa = @usr_sesa";
                            using (SqlCommand selectCmd = new SqlCommand(selectQuery, conn))
                            {
                                selectCmd.Parameters.AddWithValue("@usr_sesa", sesa_id);
                                conn.Open();
                                using (SqlDataReader reader = selectCmd.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        LoginModel user = new LoginModel
                                        {
                                            sesa_id = reader["usr_sesa"].ToString() ?? string.Empty,
                                            name = reader["usr_name"].ToString() ?? string.Empty,
                                            email = reader["usr_email"].ToString() ?? string.Empty,
                                            password = reader["usr_password"].ToString() ?? string.Empty,
                                            roles = reader["roles"].ToString() ?? string.Empty,
                                            level = reader["usr_level"].ToString() ?? string.Empty, // Ensure usr_level is included in the SELECT statement
                                            record_date = reader["usr_record_date"].ToString() ?? string.Empty
                                        };
                                        data.Add(user);
                                    }
                                }
                                conn.Close();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Handle exception (e.g., log the error)
                    Console.WriteLine("An error occurred: " + ex.Message);
                }
            }
            return data;
        }
        public List<LoginModel> GetUserDataDetail(string id_user)
        {
            List<LoginModel> dataList = new List<LoginModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "SELECT * from mst_users_QAS where usr_id = @usr_id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@usr_id", id_user ?? (object)DBNull.Value);
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            LoginModel data = new LoginModel
                            {
                                id = reader["usr_id"].ToString() ?? string.Empty,
                                sesa_id = reader["usr_sesa"].ToString() ?? string.Empty,
                                name = reader["usr_name"].ToString() ?? string.Empty,
                                email = reader["usr_email"].ToString() ?? string.Empty,
                                roles = reader["roles"].ToString() ?? string.Empty,
                                record_date = reader["usr_record_date"] as DateTime? != null
                                    ? ((DateTime)reader["usr_record_date"]).ToString("d MMM yyyy HH:mm:ss")
                                    : "NA"
                            };
                            dataList.Add(data);
                        }
                    }
                }
                conn.Close();
            }
            return dataList;
        }
        public bool UpdateDataUser(string id_user, string sesa_id, string name, string email, string role, string level)
        {
            bool success = false;

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                string updateQuery = @"
            UPDATE mst_users_QAS 
            SET 
                usr_sesa = @usr_sesa, 
                usr_name = @usr_name, 
                usr_email = @usr_email, 
                roles = @roles, 
                usr_level = @usr_level, 
                usr_record_date = GETDATE() 
            WHERE usr_id = @usr_id";

                using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@usr_id", id_user);
                    cmd.Parameters.AddWithValue("@usr_sesa", sesa_id);
                    cmd.Parameters.AddWithValue("@usr_name", name);
                    cmd.Parameters.AddWithValue("@usr_email", email);
                    cmd.Parameters.AddWithValue("@roles", role);
                    cmd.Parameters.AddWithValue("@usr_level", level);

                    try
                    {
                        conn.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();
                        success = rowsAffected > 0;
                    }
                    catch (Exception ex)
                    {
                        // Sebaiknya gunakan logging daripada Console.WriteLine di aplikasi produksi
                        Console.WriteLine("Terjadi kesalahan saat memperbarui data user: " + ex.Message);
                    }
                }
            }

            return success;
        }

        public List<LoginModel> DeleteUserDataList(int id_user)
        {
            List<LoginModel> data = new List<LoginModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "Delete from mst_users_QAS WHERE usr_id = @usr_id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@usr_id", id_user);

                    conn.Open();

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        data.Add(new LoginModel
                        {
                            id = id_user.ToString()
                        });
                    }
                }
                conn.Close();
            }
            return data;
        }
        public List<SlocModel> GetAllDataSloc(string sloc)
        {
            List<SlocModel> dataList = new List<SlocModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                // Adjust the query to handle null sloc
                var query = "SELECT * FROM mst_sloc_QAS";
                if (!string.IsNullOrEmpty(sloc))
                {
                    query += " WHERE sloc LIKE @sloc";
                }
                query += " ORDER BY sloc DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.Text;
                    if (!string.IsNullOrEmpty(sloc))
                    {
                        cmd.Parameters.AddWithValue("@sloc", "%" + sloc + "%");
                    }
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            SlocModel data = new SlocModel
                            {
                                id = reader["id"].ToString() ?? string.Empty,
                                sloc = reader["sloc"].ToString() ?? string.Empty,
                                sloc_detail = reader["sloc_detail"].ToString() ?? string.Empty,
                                description = reader["Description"].ToString() ?? string.Empty,
                                plant = reader["plant"].ToString() ?? string.Empty,
                            };
                            dataList.Add(data);
                        }
                    }
                }
                conn.Close();
            }
            return dataList;
        }


        public List<SlocModel> AddNewSloc(string sloc, string sloc_detail, string description, string plant)
        {
            List<SlocModel> data = new List<SlocModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                try
                {
                    string query = "INSERT INTO mst_sloc_QAS (sloc, sloc_detail, description, plant) VALUES (@sloc, @sloc_detail, @Description, @plant)";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.Parameters.AddWithValue("@sloc", sloc);
                        cmd.Parameters.AddWithValue("@sloc_detail", sloc_detail);
                        cmd.Parameters.AddWithValue("@Description", description);
                        cmd.Parameters.AddWithValue("@plant", plant);
                        conn.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();
                        conn.Close();
                        if (rowsAffected > 0)
                        {
                            // Retrieve the newly added SLOC data
                            string selectQuery = "SELECT sloc, sloc_detail, description, plant FROM mst_sloc_QAS WHERE sloc = @sloc";
                            using (SqlCommand selectCmd = new SqlCommand(selectQuery, conn))
                            {
                                selectCmd.Parameters.AddWithValue("@sloc", sloc);
                                conn.Open();
                                using (SqlDataReader reader = selectCmd.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        SlocModel slocData = new SlocModel
                                        {
                                            sloc = reader["sloc"].ToString() ?? string.Empty,
                                            sloc_detail = reader["sloc_detail"].ToString() ?? string.Empty,
                                            description = reader["Description"].ToString() ?? string.Empty,
                                            plant = reader["plant"].ToString() ?? string.Empty
                                        };
                                        data.Add(slocData);
                                    }
                                }
                                conn.Close();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Handle exception (e.g., log the error)
                    Console.WriteLine("An error occurred: " + ex.Message);
                }
            }
            return data;
        }
        public bool UpdateDataSloc(string id, string sloc, string sloc_detail, string description, string plant)
        {
            bool success = false;
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                try
                {
                    string insertQuery = "UPDATE mst_sloc_QAS set sloc = @sloc, sloc_detail = @sloc_detail, Description = @Description, plant= @plant where id = @id";
                    using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@sloc", sloc);
                        cmd.Parameters.AddWithValue("@sloc_detail", sloc_detail);
                        cmd.Parameters.AddWithValue("@Description", description);
                        cmd.Parameters.AddWithValue("@plant", plant);
                        conn.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();
                        conn.Close();
                        success = (rowsAffected > 0);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occurred: " + ex.Message);
                    success = false;
                }
            }
            return success;
        }

        public List<SlocModel> GetAllDataSlocDetail(string id)
        {
            List<SlocModel> data = new List<SlocModel>();
            string query = "SELECT * FROM mst_sloc_QAS where id = @id";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new SlocModel
                            {
                                id = reader["id"].ToString() ?? string.Empty,
                                sloc = reader["sloc"].ToString() ?? string.Empty,
                                sloc_detail = reader["sloc_detail"].ToString() ?? string.Empty,
                                description = reader["Description"].ToString() ?? string.Empty,
                                plant = reader["plant"].ToString() ?? string.Empty,
                            };
                            data.Add(data_list);
                        }
                    }
                }
            }

            return data;
        }
        public List<LoginModel> DeleteDataSloc(string id)
        {
            List<LoginModel> data = new List<LoginModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "Delete from mst_sloc_QAS WHERE id = @id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);

                    conn.Open();

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        data.Add(new LoginModel
                        {
                            id = id.ToString()
                        });
                    }
                }
                conn.Close();
            }
            return data;
        }
        public List<SelectModel> GetSlocFilter(string cell)
        {
            List<SelectModel> data = new List<SelectModel>();
            string query = "SELECT DISTINCT id, sloc FROM mst_sloc_QAS WHERE sloc LIKE @cell ORDER BY sloc DESC";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@cell", "%" + cell + "%");
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new SelectModel
                            {
                                Text = reader["sloc"].ToString() ?? string.Empty,
                                Id = reader["id"].ToString() ?? string.Empty
                            };
                            data.Add(data_list);
                        }
                    }
                }
            }

            return data;
        }

        public List<SelectModel> GetreferenceFilter(string cell)
        {
            List<SelectModel> data = new List<SelectModel>();
            string query = "SELECT DISTINCT id, reference FROM mst_reference_QAS WHERE reference LIKE @cell ORDER BY reference DESC";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@cell", "%" + cell + "%");
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new SelectModel
                            {
                                Text = reader["reference"].ToString() ?? string.Empty,
                                Id = reader["id"].ToString() ?? string.Empty
                            };
                            data.Add(data_list);
                        }
                    }
                }
            }

            return data;
        }
        public List<ReferenceModel> GetAllDataReference(string reference)
        {
            List<ReferenceModel> dataList = new List<ReferenceModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "SELECT * FROM mst_reference_QAS";
                if (!string.IsNullOrEmpty(reference))
                {
                    query += " WHERE reference LIKE @reference";
                }
                query += " ORDER BY id DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.Text;
                    if (!string.IsNullOrEmpty(reference))
                    {
                        cmd.Parameters.AddWithValue("@reference", "%" + reference + "%");
                    }
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ReferenceModel data = new ReferenceModel
                            {
                                id = reader["id"].ToString() ?? string.Empty,
                                reference = reader["reference"].ToString() ?? string.Empty,
                                record_date = reader["record_date"] as DateTime? != null
                                    ? ((DateTime)reader["record_date"]).ToString("d MMM yyyy HH:mm:ss")
                                    : "NA",
                                modify_by = reader["usr_sesa"].ToString() ?? string.Empty
                            };
                            dataList.Add(data);
                        }
                    }
                }
                conn.Close();
            }
            return dataList;
        }

        public List<ReferenceModel> AddNewReference(string reference, string sesa_id)
        {
            List<ReferenceModel> data = new List<ReferenceModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                try
                {
                    string query = "INSERT INTO mst_reference_QAS (reference, record_date, usr_sesa) VALUES (@reference, GETDATE(), @usr_sesa)";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.Parameters.AddWithValue("@reference", reference);
                        cmd.Parameters.AddWithValue("@usr_sesa", sesa_id);
                        conn.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();
                        conn.Close();
                        if (rowsAffected > 0)
                        {
                            // Retrieve the newly added SLOC data
                            string selectQuery = "SELECT reference, record_date, usr_sesa FROM mst_reference_QAS WHERE reference = @reference";
                            using (SqlCommand selectCmd = new SqlCommand(selectQuery, conn))
                            {
                                selectCmd.Parameters.AddWithValue("@reference", reference);
                                conn.Open();
                                using (SqlDataReader reader = selectCmd.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        ReferenceModel referenceData = new ReferenceModel
                                        {
                                            reference = reader["reference"].ToString() ?? string.Empty,
                                            record_date = reader["record_date"].ToString() ?? string.Empty,
                                            modify_by = reader["usr_sesa"].ToString() ?? string.Empty
                                        };
                                        data.Add(referenceData);
                                    }
                                }
                                conn.Close();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Handle exception (e.g., log the error)
                    Console.WriteLine("An error occurred: " + ex.Message);
                }
            }
            return data;
        }
        public List<ReferenceModel> GetAllDataReferenceDetail(string id)
        {
            List<ReferenceModel> data = new List<ReferenceModel>();
            string query = "SELECT * FROM mst_reference_QAS where id = @id";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new ReferenceModel
                            {
                                id = reader["id"].ToString() ?? string.Empty,
                                reference = reader["reference"].ToString() ?? string.Empty,
                                record_date = reader["record_date"].ToString() ?? string.Empty,
                                modify_by = reader["usr_sesa"].ToString() ?? string.Empty
                            };
                            data.Add(data_list);
                        }
                    }
                }
            }

            return data;
        }
        public bool UpdateDataReference(string id, string reference, string sesa_id)
        {
            bool success = false;
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                try
                {
                    string insertQuery = "UPDATE mst_reference_QAS set reference = @reference, record_date = GETDATE(), usr_sesa = @usr_sesa where id = @id";
                    using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@reference", reference);
                        cmd.Parameters.AddWithValue("@usr_sesa", sesa_id);
                        conn.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();
                        conn.Close();
                        success = (rowsAffected > 0);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occurred: " + ex.Message);
                    success = false;
                }
            }
            return success;
        }
        public List<ReferenceModel> DeleteDataReference(string id)
        {
            List<ReferenceModel> data = new List<ReferenceModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "Delete from mst_reference_QAS WHERE id = @id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);

                    conn.Open();

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        data.Add(new ReferenceModel
                        {
                            id = id.ToString()
                        });
                    }
                }
                conn.Close();
            }
            return data;
        }
        public List<SelectModel> GetStatusFilter(string cell)
        {
            List<SelectModel> data = new List<SelectModel>();
            string query = "SELECT DISTINCT id, status FROM mst_status WHERE status LIKE @cell ORDER BY status DESC";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@cell", "%" + cell + "%");
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new SelectModel
                            {
                                Text = reader["status"].ToString() ?? string.Empty,
                                Id = reader["id"].ToString() ?? string.Empty
                            };
                            data.Add(data_list);
                        }
                    }
                }
            }

            return data;
        }
        public List<StatusModel> GetAllDataStatus(string status)
        {
            List<StatusModel> dataList = new List<StatusModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "SELECT * FROM mst_status";
                if (!string.IsNullOrEmpty(status))
                {
                    query += " WHERE status LIKE @status";
                }
                query += " ORDER BY status DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.Text;
                    if (!string.IsNullOrEmpty(status))
                    {
                        cmd.Parameters.AddWithValue("@status", "%" + status + "%");
                    }
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            StatusModel data = new StatusModel
                            {
                                id = reader["id"].ToString() ?? string.Empty,
                                status = reader["status"].ToString() ?? string.Empty,
                                record_date = reader["record_date"] as DateTime? != null
                                    ? ((DateTime)reader["record_date"]).ToString("d MMM yyyy HH:mm:ss")
                                    : "NA",
                                modify_by = reader["usr_sesa"].ToString() ?? string.Empty
                            };
                            dataList.Add(data);
                        }
                    }
                }
                conn.Close();
            }
            return dataList;
        }

        public List<StatusModel> AddNewStatus(string status, string sesa_id)
        {
            List<StatusModel> data = new List<StatusModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                try
                {
                    string query = "INSERT INTO mst_status (status, record_date, usr_sesa) VALUES (@status, GETDATE(), @usr_sesa)";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.Parameters.AddWithValue("@status", status);
                        cmd.Parameters.AddWithValue("@usr_sesa", sesa_id);
                        conn.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();
                        conn.Close();
                        if (rowsAffected > 0)
                        {
                            // Retrieve the newly added SLOC data
                            string selectQuery = "SELECT status, record_date, usr_sesa FROM mst_status WHERE status = @status";
                            using (SqlCommand selectCmd = new SqlCommand(selectQuery, conn))
                            {
                                selectCmd.Parameters.AddWithValue("@status", status);
                                conn.Open();
                                using (SqlDataReader reader = selectCmd.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        StatusModel statusData = new StatusModel
                                        {
                                            status = reader["status"].ToString() ?? string.Empty,
                                            record_date = reader["record_date"].ToString() ?? string.Empty,
                                            modify_by = reader["usr_sesa"].ToString() ?? string.Empty
                                        };
                                        data.Add(statusData);
                                    }
                                }
                                conn.Close();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Handle exception (e.g., log the error)
                    Console.WriteLine("An error occurred: " + ex.Message);
                }
            }
            return data;
        }
        public List<StatusModel> GetAllDatastatusDetail(string id)
        {
            List<StatusModel> data = new List<StatusModel>();
            string query = "SELECT * FROM mst_status where id = @id";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new StatusModel
                            {
                                id = reader["id"].ToString() ?? string.Empty,
                                status = reader["status"].ToString() ?? string.Empty,
                                record_date = reader["record_date"].ToString() ?? string.Empty,
                                modify_by = reader["usr_sesa"].ToString() ?? string.Empty
                            };
                            data.Add(data_list);
                        }
                    }
                }
            }

            return data;
        }
        public bool UpdateDataStatus(string id, string status, string sesa_id)
        {
            bool success = false;
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                try
                {
                    string insertQuery = "UPDATE mst_status set status = @status, record_date = GETDATE(), usr_sesa = @usr_sesa where id = @id";
                    using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@status", status);
                        cmd.Parameters.AddWithValue("@usr_sesa", sesa_id);
                        conn.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();
                        conn.Close();
                        success = (rowsAffected > 0);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occurred: " + ex.Message);
                    success = false;
                }
            }
            return success;
        }
        public List<StatusModel> DeleteDataStatus(string id)
        {
            List<StatusModel> data = new List<StatusModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "Delete from mst_status WHERE id = @id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);

                    conn.Open();

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        data.Add(new StatusModel
                        {
                            id = id.ToString()
                        });
                    }
                }
                conn.Close();
            }
            return data;
        }

        
        public List<SelectModel> GetIssueSourceFilter(string cell)
        {
            List<SelectModel> data = new List<SelectModel>();
            string query = "SELECT DISTINCT id, issue_source FROM mst_issue WHERE issue_source LIKE @cell ORDER BY issue_source DESC";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@cell", "%" + cell + "%");
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new SelectModel
                            {
                                Text = reader["issue_source"].ToString() ?? string.Empty,
                                Id = reader["id"].ToString() ?? string.Empty
                            };
                            data.Add(data_list);
                        }
                    }
                }
            }

            return data;
        }
        public List<IssueModel> GetAllDataIssue(string issue_source)
        {
            List<IssueModel> dataList = new List<IssueModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "SELECT * FROM mst_issue";
                if (!string.IsNullOrEmpty(issue_source))
                {
                    query += " WHERE issue_source LIKE @issue_source";
                }
                query += " ORDER BY issue_source DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.Text;
                    if (!string.IsNullOrEmpty(issue_source))
                    {
                        cmd.Parameters.AddWithValue("@issue_source", "%" + issue_source + "%");
                    }
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            IssueModel data = new IssueModel
                            {
                                id = reader["id"].ToString() ?? string.Empty,
                                issue_source = reader["issue_source"].ToString() ?? string.Empty,
                                issue_category = reader["issue_category"].ToString() ?? string.Empty,
                                record_date = reader["record_date"] as DateTime? != null
                                    ? ((DateTime)reader["record_date"]).ToString("d MMM yyyy HH:mm:ss")
                                    : "NA",
                                modify_by = reader["usr_sesa"].ToString() ?? string.Empty
                            };
                            dataList.Add(data);
                        }
                    }
                }
                conn.Close();
            }
            return dataList;
        }

        public List<IssueModel> AddNewIssue(string issue_source, string issue_category, string sesa_id)
        {
            List<IssueModel> data = new List<IssueModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                try
                {
                    string query = "INSERT INTO mst_issue (issue_source, issue_category, record_date, usr_sesa) VALUES (@issue_source, @issue_category, GETDATE(), @usr_sesa)";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.Parameters.AddWithValue("@issue_source", issue_source);
                        cmd.Parameters.AddWithValue("@issue_category", issue_category);
                        cmd.Parameters.AddWithValue("@usr_sesa", sesa_id);
                        conn.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();
                        conn.Close();
                        if (rowsAffected > 0)
                        {
                            // Retrieve the newly added SLOC data
                            string selectQuery = "SELECT issue_source, issue_category, record_date, usr_sesa FROM mst_issue WHERE issue_source = @issue_source";
                            using (SqlCommand selectCmd = new SqlCommand(selectQuery, conn))
                            {
                                selectCmd.Parameters.AddWithValue("@issue_source", issue_source);
                                conn.Open();
                                using (SqlDataReader reader = selectCmd.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        IssueModel issueData = new IssueModel
                                        {
                                            issue_source = reader["issue_source"].ToString() ?? string.Empty,
                                            issue_category = reader["issue_category"].ToString() ?? string.Empty,
                                            record_date = reader["record_date"].ToString() ?? string.Empty,
                                            modify_by = reader["usr_sesa"].ToString() ?? string.Empty
                                        };
                                        data.Add(issueData);
                                    }
                                }
                                conn.Close();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Handle exception (e.g., log the error)
                    Console.WriteLine("An error occurred: " + ex.Message);
                }
            }
            return data;
        }
        public List<IssueModel> GetAllDataIssueDetail(string id)
        {
            List<IssueModel> data = new List<IssueModel>();
            string query = "SELECT * FROM mst_issue where id = @id";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new IssueModel
                            {
                                id = reader["id"].ToString() ?? string.Empty,
                                issue_source = reader["issue_source"].ToString() ?? string.Empty,
                                issue_category = reader["issue_category"].ToString() ?? string.Empty,
                                record_date = reader["record_date"].ToString() ?? string.Empty,
                                modify_by = reader["usr_sesa"].ToString() ?? string.Empty
                            };
                            data.Add(data_list);
                        }
                    }
                }
            }

            return data;
        }
        public bool UpdateDataIssue(string id, string issue_source, string issue_category, string sesa_id)
        {
            bool success = false;
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                try
                {
                    string insertQuery = "UPDATE mst_issue set issue_source = @issue_source, issue_category =@issue_category, record_date = GETDATE(), usr_sesa = @usr_sesa where id = @id";
                    using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.Parameters.AddWithValue("@id", id); 
                        cmd.Parameters.AddWithValue("@issue_source", issue_source);
                        cmd.Parameters.AddWithValue("@issue_category", issue_category);
                        cmd.Parameters.AddWithValue("@usr_sesa", sesa_id);
                        conn.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();
                        conn.Close();
                        success = (rowsAffected > 0);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occurred: " + ex.Message);
                    success = false;
                }
            }
            return success;
        }
        public List<IssueModel> DeleteDataIssue(string id)
        {
            List<IssueModel> data = new List<IssueModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "Delete from mst_issue WHERE id = @id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);

                    conn.Open();

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        data.Add(new IssueModel
                        {
                            id = id.ToString()
                        });
                    }
                }
                conn.Close();
            }
            return data;
        }

        public List<DispositionModel> GetAllDataDisposition(string disposition)
        {
            List<DispositionModel> dataList = new List<DispositionModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "SELECT * FROM mst_disposition_QAS";
                if (!string.IsNullOrEmpty(disposition))
                {
                    query += " WHERE disposition LIKE @disposition";
                }
                query += " ORDER BY disposition DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.Text;
                    if (!string.IsNullOrEmpty(disposition))
                    {
                        cmd.Parameters.AddWithValue("@disposition", "%" + disposition + "%");
                    }
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            DispositionModel data = new DispositionModel
                            {
                                id = reader["id"].ToString() ?? string.Empty,
                                disposition = reader["disposition"].ToString() ?? string.Empty,
                                record_date = reader["record_date"] as DateTime? != null
                                    ? ((DateTime)reader["record_date"]).ToString("d MMM yyyy HH:mm:ss")
                                    : "NA",
                                modify_by = reader["usr_sesa"].ToString() ?? string.Empty
                            };
                            dataList.Add(data);
                        }
                    }
                }
                conn.Close();
            }
            return dataList;
        }

        public List<DispositionModel> AddNewDisposition(string disposition, string sesa_id)
        {
            List<DispositionModel> data = new List<DispositionModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                try
                {
                    string query = "INSERT INTO mst_disposition_QAS (disposition, record_date, usr_sesa) VALUES (@disposition, GETDATE(), @usr_sesa)";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.Parameters.AddWithValue("@disposition", disposition);
                        cmd.Parameters.AddWithValue("@usr_sesa", sesa_id);
                        conn.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();
                        conn.Close();
                        if (rowsAffected > 0)
                        {
                            // Retrieve the newly added SLOC data
                            string selectQuery = "SELECT disposition, record_date, usr_sesa FROM mst_INP_disposition WHERE disposition = @disposition";
                            using (SqlCommand selectCmd = new SqlCommand(selectQuery, conn))
                            {
                                selectCmd.Parameters.AddWithValue("@disposition", disposition);
                                conn.Open();
                                using (SqlDataReader reader = selectCmd.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        DispositionModel dispositionData = new DispositionModel
                                        {
                                            disposition = reader["disposition"].ToString() ?? string.Empty,
                                            record_date = reader["record_date"].ToString() ?? string.Empty,
                                            modify_by = reader["usr_sesa"].ToString() ?? string.Empty
                                        };
                                        data.Add(dispositionData);
                                    }
                                }
                                conn.Close();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Handle exception (e.g., log the error)
                    Console.WriteLine("An error occurred: " + ex.Message);
                }
            }
            return data;
        }
        public List<DispositionModel> GetAllDataDispositionDetail(string id)
        {
            List<DispositionModel> data = new List<DispositionModel>();
            string query = "SELECT * FROM mst_disposition_QAS where id = @id";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new DispositionModel
                            {
                                id = reader["id"].ToString() ?? string.Empty,
                                disposition = reader["disposition"].ToString() ?? string.Empty,
                                record_date = reader["record_date"].ToString() ?? string.Empty,
                                modify_by = reader["usr_sesa"].ToString() ?? string.Empty
                            };
                            data.Add(data_list);
                        }
                    }
                }
            }

            return data;
        }
        public bool UpdateDataDisposition(string id, string disposition, string sesa_id)
        {
            bool success = false;
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                try
                {
                    string insertQuery = "UPDATE mst_disposition_QAS set disposition = @disposition, record_date = GETDATE(), usr_sesa = @usr_sesa where id = @id";
                    using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@disposition", disposition);
                        cmd.Parameters.AddWithValue("@usr_sesa", sesa_id);
                        conn.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();
                        conn.Close();
                        success = (rowsAffected > 0);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occurred: " + ex.Message);
                    success = false;
                }
            }
            return success;
        }
        public List<DispositionModel> DeleteDataDisposition(string id)
        {
            List<DispositionModel> data = new List<DispositionModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "Delete from mst_disposition_QAS WHERE id = @id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);

                    conn.Open();

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        data.Add(new DispositionModel
                        {
                            id = id.ToString()
                        });
                    }
                }
                conn.Close();
            }
            return data;
        }
        
        public List<RemarkModel> GetAllDataRemark(string remark)
        {
            List<RemarkModel> dataList = new List<RemarkModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "SELECT * FROM mst_remark_QAS";
                if (!string.IsNullOrEmpty(remark))
                {
                    query += " WHERE remark LIKE @remark";
                }
                query += " ORDER BY remark DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.Text;
                    if (!string.IsNullOrEmpty(remark))
                    {
                        cmd.Parameters.AddWithValue("@remark", "%" + remark + "%");
                    }
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            RemarkModel data = new RemarkModel
                            {
                                id = reader["id"].ToString() ?? string.Empty,
                                remark = reader["remark"].ToString() ?? string.Empty,
                                record_date = reader["record_date"] as DateTime? != null
                                    ? ((DateTime)reader["record_date"]).ToString("d MMM yyyy HH:mm:ss")
                                    : "NA",
                                modify_by = reader["usr_sesa"].ToString() ?? string.Empty
                            };
                            dataList.Add(data);
                        }
                    }
                }
                conn.Close();
            }
            return dataList;
        }

        public List<RemarkModel> AddNewRemark(string remark, string sesa_id)
        {
            List<RemarkModel> data = new List<RemarkModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                try
                {
                    string query = "INSERT INTO mst_remark_QAS (remark, record_date, usr_sesa) VALUES (@remark, GETDATE(), @usr_sesa)";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.Parameters.AddWithValue("@remark", remark);
                        cmd.Parameters.AddWithValue("@usr_sesa", sesa_id);
                        conn.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();
                        conn.Close();
                        if (rowsAffected > 0)
                        {
                            // Retrieve the newly added SLOC data
                            string selectQuery = "SELECT remark, record_date, usr_sesa FROM mst_remark_QAS WHERE remark = @remark";
                            using (SqlCommand selectCmd = new SqlCommand(selectQuery, conn))
                            {
                                selectCmd.Parameters.AddWithValue("@remark", remark);
                                conn.Open();
                                using (SqlDataReader reader = selectCmd.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        RemarkModel remarkData = new RemarkModel
                                        {
                                            remark = reader["remark"].ToString() ?? string.Empty,
                                            record_date = reader["record_date"].ToString() ?? string.Empty,
                                            modify_by = reader["usr_sesa"].ToString() ?? string.Empty
                                        };
                                        data.Add(remarkData);
                                    }
                                }
                                conn.Close();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Handle exception (e.g., log the error)
                    Console.WriteLine("An error occurred: " + ex.Message);
                }
            }
            return data;
        }
        public List<RemarkModel> GetAllDataRemarkDetail(string id)
        {
            List<RemarkModel> data = new List<RemarkModel>();
            string query = "SELECT * FROM mst_remark_QAS where id = @id";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new RemarkModel
                            {
                                id = reader["id"].ToString() ?? string.Empty,
                                remark = reader["remark"].ToString() ?? string.Empty,
                                record_date = reader["record_date"].ToString() ?? string.Empty,
                                modify_by = reader["usr_sesa"].ToString() ?? string.Empty
                            };
                            data.Add(data_list);
                        }
                    }
                }
            }

            return data;
        }
        public bool UpdateDataRemark(string id, string remark, string sesa_id)
        {
            bool success = false;
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                try
                {
                    string insertQuery = "UPDATE mst_remark_QAS set remark = @remark, record_date = GETDATE(), usr_sesa = @usr_sesa where id = @id";
                    using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@remark", remark);
                        cmd.Parameters.AddWithValue("@usr_sesa", sesa_id);
                        conn.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();
                        conn.Close();
                        success = (rowsAffected > 0);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occurred: " + ex.Message);
                    success = false;
                }
            }
            return success;
        }
        public List<RemarkModel> DeleteDataRemark(string id)
        {
            List<RemarkModel> data = new List<RemarkModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "Delete from mst_remark_QAS WHERE id = @id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);

                    conn.Open();

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        data.Add(new RemarkModel
                        {
                            id = id.ToString()
                        });
                    }
                }
                conn.Close();
            }
            return data;
        }
        
        public List<RackModel> GetAllDataRack(string rack)
        {
            List<RackModel> dataList = new List<RackModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "SELECT * FROM mst_rack_QAS";
                if (!string.IsNullOrEmpty(rack))
                {
                    query += " WHERE rack LIKE @rack";
                }
                query += " ORDER BY rack DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.Text;
                    if (!string.IsNullOrEmpty(rack))
                    {
                        cmd.Parameters.AddWithValue("@rack", "%" + rack + "%");
                    }
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            RackModel data = new RackModel
                            {
                                id = reader["id"].ToString() ?? string.Empty,
                                rack = reader["rack"].ToString() ?? string.Empty,
                                record_date = reader["record_date"] as DateTime? != null
                                    ? ((DateTime)reader["record_date"]).ToString("d MMM yyyy HH:mm:ss")
                                    : "NA",
                                modify_by = reader["usr_sesa"].ToString() ?? string.Empty
                            };
                            dataList.Add(data);
                        }
                    }
                }
                conn.Close();
            }
            return dataList;
        }

        public List<RackModel> AddNewRack(string rack, string sesa_id)
        {
            List<RackModel> data = new List<RackModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                try
                {
                    string query = "INSERT INTO mst_rack_QAS (rack, record_date, usr_sesa) VALUES (@rack, GETDATE(), @usr_sesa)";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.Parameters.AddWithValue("@rack", rack);
                        cmd.Parameters.AddWithValue("@usr_sesa", sesa_id);
                        conn.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();
                        conn.Close();
                        if (rowsAffected > 0)
                        {
                            // Retrieve the newly added SLOC data
                            string selectQuery = "SELECT rack, record_date, usr_sesa FROM mst_rack_QAS WHERE rack = @rack";
                            using (SqlCommand selectCmd = new SqlCommand(selectQuery, conn))
                            {
                                selectCmd.Parameters.AddWithValue("@rack", rack);
                                conn.Open();
                                using (SqlDataReader reader = selectCmd.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        RackModel rackData = new RackModel
                                        {
                                            rack = reader["rack"].ToString() ?? string.Empty,
                                            record_date = reader["record_date"].ToString() ?? string.Empty,
                                            modify_by = reader["usr_sesa"].ToString() ?? string.Empty
                                        };
                                        data.Add(rackData);
                                    }
                                }
                                conn.Close();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Handle exception (e.g., log the error)
                    Console.WriteLine("An error occurred: " + ex.Message);
                }
            }
            return data;
        }
        public List<RackModel> GetAllDataRackDetail(string id)
        {
            List<RackModel> data = new List<RackModel>();
            string query = "SELECT * FROM mst_rack_QAS where id = @id";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new RackModel
                            {
                                id = reader["id"].ToString() ?? string.Empty,
                                rack = reader["rack"].ToString() ?? string.Empty,
                                record_date = reader["record_date"].ToString() ?? string.Empty,
                                modify_by = reader["usr_sesa"].ToString() ?? string.Empty
                            };
                            data.Add(data_list);
                        }
                    }
                }
            }

            return data;
        }
        public bool UpdateDataRack(string id, string rack, string sesa_id)
        {
            bool success = false;
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                try
                {
                    string insertQuery = "UPDATE mst_rack_QAS set rack = @rack, record_date = GETDATE(), usr_sesa = @usr_sesa where id = @id";
                    using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@rack", rack);
                        cmd.Parameters.AddWithValue("@usr_sesa", sesa_id);
                        conn.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();
                        conn.Close();
                        success = (rowsAffected > 0);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occurred: " + ex.Message);
                    success = false;
                }
            }
            return success;
        }
        public List<RackModel> DeleteDataRack(string id)
        {
            List<RackModel> data = new List<RackModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "Delete from mst_rack_QAS WHERE id = @id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);

                    conn.Open();

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        data.Add(new RackModel
                        {
                            id = id.ToString()
                        });
                    }
                }
                conn.Close();
            }
            return data;
        }
        public List<ColumnModel> GetAllDataColumn(string column)
        {
            List<ColumnModel> dataList = new List<ColumnModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "SELECT * FROM mst_rack_column_QAS";
                if (!string.IsNullOrEmpty(column))
                {
                    query += " WHERE rack_column LIKE @rack_column";
                }
                query += " ORDER BY rack_column DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.Text;
                    if (!string.IsNullOrEmpty(column))
                    {
                        cmd.Parameters.AddWithValue("@rack_column", "%" + column + "%");
                    }
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ColumnModel data = new ColumnModel
                            {
                                id = reader["id"].ToString() ?? string.Empty,
                                column = reader["rack_column"].ToString() ?? string.Empty,
                                record_date = reader["record_date"] as DateTime? != null
                                    ? ((DateTime)reader["record_date"]).ToString("d MMM yyyy HH:mm:ss")
                                    : "NA",
                                modify_by = reader["usr_sesa"].ToString() ?? string.Empty
                            };
                            dataList.Add(data);
                        }
                    }
                }
                conn.Close();
            }
            return dataList;
        }

        public List<ColumnModel> AddNewColumn(string column, string sesa_id)
        {
            List<ColumnModel> data = new List<ColumnModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                try
                {
                    string query = "INSERT INTO mst_rack_column_QAS (rack_column, record_date, usr_sesa) VALUES (@rack_column, GETDATE(), @usr_sesa)";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.Parameters.AddWithValue("@rack_column", column);
                        cmd.Parameters.AddWithValue("@usr_sesa", sesa_id);
                        conn.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();
                        conn.Close();
                        if (rowsAffected > 0)
                        {
                            // Retrieve the newly added SLOC data
                            string selectQuery = "SELECT rack_column, record_date, usr_sesa FROM mst_rack_column_QAS WHERE rack_column = @rack_column";
                            using (SqlCommand selectCmd = new SqlCommand(selectQuery, conn))
                            {
                                selectCmd.Parameters.AddWithValue("@rack_column", column);
                                conn.Open();
                                using (SqlDataReader reader = selectCmd.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        ColumnModel rackData = new ColumnModel
                                        {
                                            column = reader["rack_column"].ToString() ?? string.Empty,
                                            record_date = reader["record_date"].ToString() ?? string.Empty,
                                            modify_by = reader["usr_sesa"].ToString() ?? string.Empty
                                        };
                                        data.Add(rackData);
                                    }
                                }
                                conn.Close();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Handle exception (e.g., log the error)
                    Console.WriteLine("An error occurred: " + ex.Message);
                }
            }
            return data;
        }
        public List<ColumnModel> GetAllDataColumnDetail(string id)
        {
            List<ColumnModel> data = new List<ColumnModel>();
            string query = "SELECT * FROM mst_rack_column_QAS where id = @id";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new ColumnModel
                            {
                                id = reader["id"].ToString() ?? string.Empty,
                                column = reader["rack_column"].ToString() ?? string.Empty,
                                record_date = reader["record_date"].ToString() ?? string.Empty,
                                modify_by = reader["usr_sesa"].ToString() ?? string.Empty
                            };
                            data.Add(data_list);
                        }
                    }
                }
            }

            return data;
        }
        public bool UpdateDataColumn(string id, string rack_column, string usr_sesa)
        {
            bool success = false;

            // Added parameter validation
            if (string.IsNullOrEmpty(id))
            {
                Console.WriteLine("Error: id parameter is null or empty");
                return false;
            }

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                try
                {
                    string updateQuery = "UPDATE mst_rack_column_QAS SET rack_column = @rack_column, record_date = GETDATE(), usr_sesa = @usr_sesa WHERE id = @id";
                    using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                    {
                        cmd.CommandType = CommandType.Text;

                        // Debug parameter values
                        Console.WriteLine($"SQL Parameters: id={id}, rack_column={rack_column}, usr_sesa={usr_sesa}");

                        // Fix parameter sizing to match database column types
                        cmd.Parameters.Add("@id", SqlDbType.VarChar, 50).Value = id;
                        cmd.Parameters.Add("@rack_column", SqlDbType.VarChar, 12).Value = rack_column;
                        cmd.Parameters.Add("@usr_sesa", SqlDbType.VarChar, 50).Value = usr_sesa;

                        conn.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();
                        conn.Close();

                        Console.WriteLine($"Update query affected {rowsAffected} rows");
                        success = (rowsAffected > 0);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occurred: " + ex.Message);
                    success = false;
                }
            }
            return success;
        }
        public List<ColumnModel> DeleteDataColumn(string id)
        {
            List<ColumnModel> data = new List<ColumnModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "Delete from mst_rack_column_QAS WHERE id = @id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);

                    conn.Open();

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        data.Add(new ColumnModel
                        {
                            id = id.ToString()
                        });
                    }
                }
                conn.Close();
            }
            return data;
        }
        
        public List<RowModel> GetAllDataRow(string row)
        {
            List<RowModel> dataList = new List<RowModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "SELECT * FROM mst_rack_row_QAS";
                if (!string.IsNullOrEmpty(row))
                {
                    query += " WHERE rack_row LIKE @rack_row";
                }
                query += " ORDER BY rack_row DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.Text;
                    if (!string.IsNullOrEmpty(row))
                    {
                        cmd.Parameters.AddWithValue("@rack_row", "%" + row + "%");
                    }
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            RowModel data = new RowModel
                            {
                                id = reader["id"].ToString() ?? string.Empty,
                                row = reader["rack_row"].ToString() ?? string.Empty,
                                record_date = reader["record_date"] as DateTime? != null
                                    ? ((DateTime)reader["record_date"]).ToString("d MMM yyyy HH:mm:ss")
                                    : "NA",
                                modify_by = reader["usr_sesa"].ToString() ?? string.Empty
                            };
                            dataList.Add(data);
                        }
                    }
                }
                conn.Close();
            }
            return dataList;
        }

        public List<RowModel> AddNewRow(string row, string sesa_id)
        {
            List<RowModel> data = new List<RowModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                try
                {
                    string query = "INSERT INTO mst_rack_row_QAS (rack_row, record_date, usr_sesa) VALUES (@rack_row, GETDATE(), @usr_sesa)";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.Parameters.AddWithValue("@rack_row", row);
                        cmd.Parameters.AddWithValue("@usr_sesa", sesa_id);
                        conn.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();
                        conn.Close();
                        if (rowsAffected > 0)
                        {
                            // Retrieve the newly added SLOC data
                            string selectQuery = "SELECT rack_row, record_date, usr_sesa FROM mst_rack_row_QAS WHERE rack_row = @rack_row";
                            using (SqlCommand selectCmd = new SqlCommand(selectQuery, conn))
                            {
                                selectCmd.Parameters.AddWithValue("@rack_row", row);
                                conn.Open();
                                using (SqlDataReader reader = selectCmd.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        RowModel rackData = new RowModel
                                        {
                                            row = reader["rack_row"].ToString() ?? string.Empty,
                                            record_date = reader["record_date"].ToString() ?? string.Empty,
                                            modify_by = reader["usr_sesa"].ToString() ?? string.Empty
                                        };
                                        data.Add(rackData);
                                    }
                                }
                                conn.Close();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Handle exception (e.g., log the error)
                    Console.WriteLine("An error occurred: " + ex.Message);
                }
            }
            return data;
        }
        public List<RowModel> GetAllDataRowDetail(string id)
        {
            List<RowModel> data = new List<RowModel>();
            string query = "SELECT * FROM mst_rack_row_QAS where id = @id";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new RowModel
                            {
                                id = reader["id"].ToString() ?? string.Empty,
                                row = reader["rack_row"].ToString() ?? string.Empty,
                                record_date = reader["record_date"].ToString() ?? string.Empty,
                                modify_by = reader["usr_sesa"].ToString() ?? string.Empty
                            };
                            data.Add(data_list);
                        }
                    }
                }
            }

            return data;
        }
        public bool UpdateDataRow(string id, string row, string sesa_id)
        {
            bool success = false;
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                try
                {
                    string insertQuery = "UPDATE mst_rack_row_QAS set rack_row = @rack_row, record_date = GETDATE(), usr_sesa = @usr_sesa where id = @id";
                    using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@rack_row", row);
                        cmd.Parameters.AddWithValue("@usr_sesa", sesa_id);
                        conn.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();
                        conn.Close();
                        success = (rowsAffected > 0);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occurred: " + ex.Message);
                    success = false;
                }
            }
            return success;
        }
        public List<RowModel> DeleteDataRow(string id)
        {
            List<RowModel> data = new List<RowModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "Delete from mst_rack_row_QAS WHERE id = @id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);

                    conn.Open();

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        data.Add(new RowModel
                        {
                            id = id.ToString()
                        });
                    }
                }
                conn.Close();
            }
            return data;
        }
        public List<RequestModel> GetOverDueData(string date_from, string date_to)
        {
            List<RequestModel> dataList = new List<RequestModel>();  // Create a list to return

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "GET_OVERDUE_DATA";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@date_from", date_from);
                    cmd.Parameters.AddWithValue("@date_to", date_to);
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            RequestModel data = new RequestModel();  // Create a new model for each row
                            data.id_req = reader["id_req"].ToString() ?? string.Empty;
                            data.req_id = reader["req_id"].ToString() ?? string.Empty;
                            data.requestor = reader["requestor"].ToString() ?? string.Empty;
                            data.reference = reader["reference"].ToString() ?? string.Empty;
                            data.rack = reader["rack"].ToString() ?? string.Empty;
                            data.row = reader["rack_row"].ToString() ?? string.Empty;
                            data.column = reader["rack_column"].ToString() ?? string.Empty;
                            data.box_type = reader["box_type"].ToString() ?? string.Empty;
                            data.quantity = reader["quantity"].ToString() ?? string.Empty;
                            data.remark = reader["remark"].ToString() ?? string.Empty;
                            data.request_date = reader["record_date"] as DateTime? != null ? ((DateTime)reader["record_date"]).ToString("d MMM yyyy HH:mm:ss") : "NA";
                            data.source_issue = reader["source_issue"].ToString() ?? string.Empty;
                            data.issue_category = reader["issue_category"].ToString() ?? string.Empty;
                            data.max_aging = reader["max_aging"] as DateTime? != null ? ((DateTime)reader["max_aging"]).ToString("ddd d MMM yyyy") : "NA";
                            data.issue_detail = reader["issue_detail"].ToString() ?? string.Empty;
                            data.source_sloc = reader["source_sloc"].ToString() ?? string.Empty;
                            data.dest_sloc = reader["dest_sloc"].ToString() ?? string.Empty;
                            data.source_sloc_detail = reader["source_detail"].ToString() ?? string.Empty;
                            data.dest_sloc_detail = reader["dest_detail"].ToString() ?? string.Empty;
                            data.source_sloc_id = reader["source_id"].ToString() ?? string.Empty;
                            data.dest_sloc_id = reader["dest_id"].ToString() ?? string.Empty;
                            data.disposition = reader["disposition"].ToString() ?? string.Empty;
                            data.pic = reader["pic"].ToString() ?? string.Empty;
                            data.picture = reader["picture"].ToString() ?? string.Empty;
                            data.status = reader["status"].ToString() ?? string.Empty;

                            dataList.Add(data);  // Add each model to the list
                        }
                    }
                }
                conn.Close();
            }
            return dataList;  // Return the list
        }

        public List<ApproverModel> GetAllMasterDataApprover()
        {
            List<ApproverModel> dataList = new List<ApproverModel>();  // Create a list to return
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = @"SELECT a.id, a.route_flow, a.approvers_modify, a.record_date_up, 
                             a.usr_sesa, r.route_desc, a.route_lvl, u.usr_name AS user_name,
                             u2.usr_name AS modifier_name
                      FROM mst_approvers_QAS a
                      LEFT OUTER JOIN mst_route_QAS r ON r.route_lvl = a.route_lvl
                      LEFT OUTER JOIN mst_users_QAS u ON u.usr_sesa = a.usr_sesa
                      LEFT OUTER JOIN mst_users_QAS u2 ON u2.usr_sesa = a.approvers_modify 
                      ORDER BY a.route_lvl DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.Text;
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ApproverModel data = new ApproverModel();
                            data.id = Convert.ToInt32(reader["id"].ToString() ?? string.Empty);
                            data.route_flow = Convert.ToInt32(reader["route_flow"].ToString() ?? string.Empty);
                            data.modify = reader["approvers_modify"].ToString() ?? string.Empty;
                            data.modifier_name = reader["modifier_name"] == DBNull.Value ? "NA" : reader["modifier_name"].ToString() ?? string.Empty;
                            data.record_date_up = reader["record_date_up"] as DateTime? != null ? ((DateTime)reader["record_date_up"]).ToString("ddd d MMM yyyy") : "NA";
                            data.usr_sesa = reader["usr_sesa"].ToString() ?? string.Empty;
                            data.usr_name = reader["user_name"] == DBNull.Value ? "NA" : reader["user_name"].ToString() ?? string.Empty;
                            data.route_desc = reader["route_desc"].ToString() ?? string.Empty;
                            data.route_lvl = reader["route_lvl"].ToString() ?? string.Empty;
                            dataList.Add(data);
                        }
                    }
                }
                conn.Close();
            }
            return dataList;
        }

        public List<SelectModel> GetSESA_Approver(string family)
        {
            List<SelectModel> data = new List<SelectModel>();
            string query = "SELECT usr_id, usr_sesa FROM mst_users_QAS WHERE usr_sesa LIKE @family AND roles = 'CSQM' ORDER BY usr_sesa DESC";
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@family", "%" + family + "%");
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new SelectModel
                            {
                                Text = reader["usr_sesa"].ToString() ?? string.Empty,
                                Id = reader["usr_id"].ToString() ?? string.Empty
                            };
                            data.Add(data_list);
                        }
                    }
                }
            }
            return data;
        }
        
        public List<SelectModel> GetRouteLevel(string family)
        {
            List<SelectModel> data = new List<SelectModel>();
            string query = "SELECT DISTINCT route_lvl, route_flow, route_desc FROM mst_route_QAS WHERE route_lvl LIKE '" + family +"%' ORDER BY route_lvl DESC";
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@family", "%" + family + "%");
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data_list = new SelectModel
                            {
                                Text = reader["route_lvl"].ToString() + " - " +
                            reader["route_desc"].ToString(),
                                Id = reader["route_flow"].ToString() ?? string.Empty
                            };
                            data.Add(data_list);
                        }
                    }
                }
            }
            return data;
        }
        public List<ApproverModel> AddNewApprover(string usr_sesa, string route_level, string route_flow, string modify)
        {
            List<ApproverModel> data = new List<ApproverModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                string query = "INSERT INTO mst_approvers_QAS(route_lvl, usr_sesa, record_date, record_date_up, route_flow, approvers_modify) VALUES(@route_lvl, @usr_sesa, GETDATE(), GETDATE(), @route_flow, @modify)";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@route_lvl", route_level);
                    cmd.Parameters.AddWithValue("@usr_sesa", usr_sesa);
                    cmd.Parameters.AddWithValue("@route_flow", route_flow);
                    cmd.Parameters.AddWithValue("@modify", modify);
                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();
                    conn.Close();
                    if (rowsAffected > 0)
                    {
                        // Retrieve the newly added approver data from the correct table (mst_approvers_QAS)
                        string selectQuery = "SELECT route_lvl, usr_sesa, route_flow, approvers_modify FROM mst_approvers_QAS WHERE usr_sesa = @usr_sesa AND route_lvl = @route_lvl";
                        using (SqlCommand selectCmd = new SqlCommand(selectQuery, conn))
                        {
                            selectCmd.Parameters.AddWithValue("@usr_sesa", usr_sesa);
                            selectCmd.Parameters.AddWithValue("@route_lvl", route_level);
                            conn.Open();
                            using (SqlDataReader reader = selectCmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    ApproverModel approver = new ApproverModel
                                    {
                                        usr_sesa = reader["usr_sesa"].ToString() ?? string.Empty,
                                        route_lvl = reader["route_lvl"].ToString() ?? string.Empty,
                                        route_flow = Convert.ToInt32(reader["route_flow"].ToString()),
                                        modify = reader["approvers_modify"].ToString() ?? string.Empty
                                    };
                                    data.Add(approver);
                                }
                            }
                            conn.Close();
                        }
                    }
                }
            }
            return data;
        }
        public List<ApproverModel> DeleteDataApproverData(string id)
        {
            List<ApproverModel> data = new List<ApproverModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "Delete from mst_approvers_QAS WHERE id = @id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        // Safely convert string to int
                        if (int.TryParse(id, out int idValue))
                        {
                            data.Add(new ApproverModel
                            {
                                id = idValue
                            });
                        }
                    }
                }
                conn.Close();
            }
            return data;
        }

        public bool UpdateDataApprover(int id, string usr_sesa, string route_lvl, string route_flow, string modify)
        {
            bool success = false;
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                string query = "UPDATE mst_approvers_QAS SET route_lvl = @route_lvl, usr_sesa = @usr_sesa, route_flow = @route_flow, record_date_up = GETDATE(), approvers_modify = @modify WHERE id = @id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@route_lvl", route_lvl);
                    cmd.Parameters.AddWithValue("@usr_sesa", usr_sesa);
                    cmd.Parameters.AddWithValue("@route_flow", route_flow);
                    cmd.Parameters.AddWithValue("@modify", modify);

                    try
                    {
                        conn.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();
                        success = rowsAffected > 0;
                    }
                    catch (Exception ex)
                    {
                        // Log exception if needed
                        Console.WriteLine("Error updating approver: " + ex.Message);
                        success = false;
                    }
                    finally
                    {
                        if (conn.State == ConnectionState.Open)
                            conn.Close();
                    }
                }
            }
            return success;
        }
        public List<ApproverModel> GetAllMasterDataApprovalFlow()
        {
            List<ApproverModel> dataList = new List<ApproverModel>();  // Create a list to return
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = @"SELECT route_flow, route_lvl, route_desc, record_date, record_date_up, route_modify , b.usr_name From mst_route_QAS left join mst_users_QAS as b on usr_sesa = route_modify ORDER BY route_flow DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.Text;
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ApproverModel data = new ApproverModel();
                            data.route_flow = Convert.ToInt32(reader["route_flow"].ToString());
                            data.modify = reader["route_modify"].ToString() ?? string.Empty;
                            data.record_date_up = reader["record_date_up"] as DateTime? != null ? ((DateTime)reader["record_date_up"]).ToString("ddd d MMM yyyy") : "NA";
                            data.record_date = reader["record_date"] as DateTime? != null ? ((DateTime)reader["record_date"]).ToString("ddd d MMM yyyy") : "NA";
                            data.route_desc = reader["route_desc"].ToString() ?? string.Empty;
                            data.route_lvl = reader["route_lvl"].ToString() ?? string.Empty;
                            dataList.Add(data);
                        }
                    }
                }
                conn.Close();
            }
            return dataList;
        }

        public List<ApproverModel> AddNewApprovalFlow(string route_level, string route_desc, string modify)
        {
            List<ApproverModel> data = new List<ApproverModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                string query = @"INSERT INTO mst_route_QAS (route_lvl, route_desc, record_date,
 record_date_up, route_modify ) VALUES(@route_lvl, @route_desc, GETDATE(), GETDATE(), @modify)";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@route_lvl", route_level);
                    cmd.Parameters.AddWithValue("@route_desc", route_desc);
                    cmd.Parameters.AddWithValue("@modify", modify);
                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();
                    conn.Close();
                    if (rowsAffected > 0)
                    {
                        // Retrieve the newly added approver data from the correct table (mst_approvers_QAS)
                        string selectQuery = "SELECT route_lvl, route_desc, route_flow, route_modify FROM mst_route_QAS WHERE route_lvl = @route_lvl AND route_desc = @route_desc";
                        using (SqlCommand selectCmd = new SqlCommand(selectQuery, conn))
                        {
                            selectCmd.Parameters.AddWithValue("@route_desc", route_desc);
                            selectCmd.Parameters.AddWithValue("@route_lvl", route_level);
                            conn.Open();
                            using (SqlDataReader reader = selectCmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    ApproverModel approver = new ApproverModel
                                    {
                                        route_lvl = reader["route_lvl"].ToString() ?? string.Empty,
                                        route_desc = reader["route_desc"].ToString() ?? string.Empty,
                                        route_flow = Convert.ToInt32(reader["route_flow"].ToString()),
                                        modify = reader["route_modify"].ToString() ?? string.Empty
                                    };
                                    data.Add(approver);
                                }
                            }
                            conn.Close();
                        }
                    }
                }
            }
            return data;
        }
        public List<ApproverModel> DeleteDataApprovalData(string route_flow)
        {
            List<ApproverModel> data = new List<ApproverModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var query = "Delete from mst_route_QAS WHERE route_flow = @route_flow";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@route_flow", route_flow);
                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        // Safely convert string to int
                        if (int.TryParse(route_flow, out int idValue))
                        {
                            data.Add(new ApproverModel
                            {
                                route_flow = idValue
                            });
                        }
                    }
                }
                conn.Close();
            }
            return data;
        }

        public List<ApproverModel> UpdateDataApprovalFlow(string route_flow, string route_lvl, string route_desc, string modify)
        {
            List<ApproverModel> data = new List<ApproverModel>();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                string query = "UPDATE mst_route_QAS set route_lvl = @route_lvl, route_desc = @route_desc, record_date = GETDATE(), record_date_up = GETDATE(), route_modify = @modify where   route_flow = @route_flow";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@route_flow", route_flow);
                    cmd.Parameters.AddWithValue("@route_lvl", route_lvl);
                    cmd.Parameters.AddWithValue("@route_desc", route_desc);
                    cmd.Parameters.AddWithValue("@modify", modify);
                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();
                    conn.Close();
                    if (rowsAffected > 0)
                    {
                        // Retrieve the newly added approver data from the correct table (mst_approvers_QAS)
                        string selectQuery = "SELECT route_lvl, route_flow, route_desc,route_modify FROM mst_route_QAS WHERE route_lvl = @route_lvl AND route_desc = @route_desc";
                        using (SqlCommand selectCmd = new SqlCommand(selectQuery, conn))
                        {
                            selectCmd.Parameters.AddWithValue("@route_desc", route_desc);
                            selectCmd.Parameters.AddWithValue("@route_lvl", route_lvl);
                            conn.Open();
                            using (SqlDataReader reader = selectCmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    ApproverModel approver = new ApproverModel
                                    {
                                        route_desc = reader["route_desc"].ToString() ?? string.Empty,
                                        route_lvl = reader["route_lvl"].ToString() ?? string.Empty,
                                        route_flow = Convert.ToInt32(reader["route_flow"].ToString()),
                                        modify = reader["route_modify"].ToString() ?? string.Empty
                                    };
                                    data.Add(approver);
                                }
                            }
                            conn.Close();
                        }
                    }
                }
            }
            return data;
        }
    }

}