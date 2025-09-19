using Microsoft.AspNetCore.Mvc;
using Quarantine_Management.Function;
using Quarantine_Management.Models;
using Microsoft.Data.SqlClient;

namespace Quarantine_Management.Controllers
{
    public class LoginController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public new IActionResult SignOut()
        {
            if (HttpContext.Session != null)
            {
                HttpContext.Session.Clear();
            }
            return View("Index");
        }

        private string DbConnection()
        {
            var dbAccess = new DatabaseAccessLayer();
            string dbString = dbAccess.ConnectionString;
            return dbString;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(LoginModel user)
        {
            var hashpassword = new Authentication();

            //if (ModelState.IsValid)
            //{
            List<LoginModel> userInfo = new List<LoginModel>();
            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                string passwordHash = hashpassword.MD5Hash(user.password ?? string.Empty);
                string query = "SELECT * FROM mst_users_QAS WHERE usr_sesa = '" + user.sesa_id + "' AND usr_password = '" + passwordHash + "' ";
                string update_loginID_query = @"UPDATE mst_users_QAS SET usr_loginid= (REPLACE(convert(varchar, getdate(),112),'/','') + replace(convert(varchar, getdate(),108),':','')) 
                                                    WHERE usr_sesa = '" + user.sesa_id + "' ";

                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    ViewData["Message"] = "HAS DATA";
                    while (reader.Read())
                    {
                        var loginUser = new LoginModel();
                        loginUser.id = reader["usr_id"].ToString() ?? string.Empty;
                        loginUser.name = reader["usr_name"].ToString() ?? string.Empty;
                        loginUser.sesa_id = reader["usr_sesa"].ToString() ?? string.Empty;
                        loginUser.roles = reader["roles"].ToString() ?? string.Empty;
                        loginUser.email = reader["usr_email"].ToString() ?? string.Empty;
                        loginUser.level = reader["usr_level"].ToString() ?? string.Empty;
                        userInfo.Add(loginUser);
                        HttpContext.Session.SetString("id", loginUser.id.ToString());
                        HttpContext.Session.SetString("name", loginUser.name);
                        HttpContext.Session.SetString("roles", loginUser.roles);
                        HttpContext.Session.SetString("sesa_id", loginUser.sesa_id);
                        HttpContext.Session.SetString("email", loginUser.email);
                        HttpContext.Session.SetString("level", loginUser.level);
                    }

                    //LastLoginTime(user.sesa_id);

                    if (HttpContext.Session.GetString("roles") == "User") 
                    {
                        int rowsAffected = 0;
                        SqlCommand command = new SqlCommand(update_loginID_query, conn);
                        rowsAffected = command.ExecuteNonQuery();

                        return RedirectToAction("Dashboard", "User");
                    }

                    if (HttpContext.Session.GetString("roles") == "CSQM")
                    {
                        return RedirectToAction("PendingApproval", "CSQM");
                    }

                    if (HttpContext.Session.GetString("roles") == "Admin")
                    {
                        return RedirectToAction("Dashboard", "Admin");
                    }


                }
                else
                {
                    ViewData["Message"] = "User and Password not Registered !";
                }

                conn.Close();

            }
            //}

            return View("Index");
        }
    }
}
