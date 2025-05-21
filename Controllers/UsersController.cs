using QLyNhaHangTDeli.Models;
using QLyNhaHangTDeli.Services;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.UI.WebControls;
using static System.Net.WebRequestMethods;



namespace QLyNhaHangTDeli.Controllers
{
    public class UsersController : Controller
    {
        private TDeliDB db = new TDeliDB();


        public string GenerateOtp()
        {
            var random = new Random();
            var otp = random.Next(100000, 999999); // Mã OTP 6 chữ số
            //var otp = 100000; // Mã OTP 6 chữ số
            return otp.ToString();
        }

        private string GenerateUserID(int length)
        {


                string randomPart = RandomIDGenerator.Generate(length);
                return "USER" + randomPart;

        }
           private string GenerateCusID(int length)
        {


                string randomPart = RandomIDGenerator.Generate(length);
                return "CUS" + randomPart;

        }

        // GET: Register (Trang đăng ký)
        public ActionResult Register(bool? isEmployee)
        {
            var model = new RegisterViewModel
            {
                IsEmployee = isEmployee ?? false  // Gán giá trị mặc định nếu isEmployee là null
            };
            return View(model);
        }

        // POST: Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ ." });
            }

            if (model.IsEmployee)
            {
                try
                {
                    // Kiểm tra xem số điện thoại đã tồn tại chưa
                    if (db.Users.Any(u => u.ContactInfo == model.ContactInfo))
                    {
                        return Json(new { success = false, message = "Email đã tồn tại." });
                    }
                    if (model.Password != model.ConfirmPassword)
                    {
                        return Json(new { success = false, message = "Mật khẩu xác nhận không khớp!" });
                    }
                    if (model.Password.Length < 6)
                    {
                        return Json(new { success = false, message = "Mật khẩu phải có ít nhất 6 ký tự." });
                    }

                    // Nếu ContactInfo là số điện thoại thì kiểm tra độ dài
                    if (model.ContactInfo.All(char.IsDigit))
                    {
                        if (model.ContactInfo.Length != 10)
                        {
                            return Json(new { success = false, message = "Số điện thoại phải gồm đúng 10 chữ số." });
                        }
                    }

                    if (!IsEmail(model.ContactInfo))
                    {
                        return Json(new { success = false, message = "Email không hợp lệ" });
                    }

                    do
                    {

                        model.UserID = GenerateUserID(6);
                    }
                    while (db.Users.Any(u => u.UserID ==  model.UserID));

                    var user = new User
                    {    
                        UserID = model.UserID,
                        FullName = model.FullName,
                        ContactInfo = model.ContactInfo,
                        Role = "Nhân viên",
                        Status = "Đợi phê duyệt",
                        // Mã hóa mật khẩu với BCrypt
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),

                        CreatedAt = DateTime.Now
                    };
                    db.Users.Add(user);
                    db.SaveChanges();

                    return Json(new { success = true });
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException ex)
                {
                    foreach (var validationErrors in ex.EntityValidationErrors)
                    {
                        foreach (var validationError in validationErrors.ValidationErrors)
                        {
                            System.Diagnostics.Debug.WriteLine($"Lỗi tại trường {validationError.PropertyName}: {validationError.ErrorMessage}");
                            ModelState.AddModelError(validationError.PropertyName, validationError.ErrorMessage);
                        }
                    }
                    return Json(new { success = false });
                }

            }
            else
            {
                try
                {

                    if (!IsEmail(model.ContactInfo))
                    {
                        return Json(new { success = false, message = "Email không hợp lệ" });
                    }

                    if (db.Customers.Any(u => u.Email == model.ContactInfo))
                    {
                        return Json(new { success = false, message = "Email đã tồn tại." });
                    }
                    if (model.Password != model.ConfirmPassword)
                    {
                        return Json(new { success = false, message = "Mật khẩu xác nhận không khớp!" });
                    }
                    if (model.Password.Length < 6)
                    {
                        return Json(new { success = false, message = "Mật khẩu phải có ít nhất 6 ký tự." });
                    }
                    do
                    {

                        model.UserID = GenerateCusID(6);
                    }
                    while (db.Customers.Any(u => u.CustomerID == model.UserID));


                    var customer = new Customer
                    {
                        CustomerID = model.UserID,
                        FullName = model.FullName,
                        Email = model.ContactInfo,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                        CreatedAt = DateTime.Now
                    };
                    db.Customers.Add(customer);
                    db.SaveChanges();
                    var otp = GenerateOtp();
                    EmailService.SendEmail(customer.Email, "OTP Đăng nhập", $"Mã OTP: {otp}");
                    Session["otp"] = otp;
                    Session["otptype"] = "register";
                    Session["otpexpiry"] = DateTime.UtcNow.AddMinutes(5);
                    string userData = $"Customer|{customer.FullName}|{customer.Email}|{customer.ProfilePicture}";

                    // Tạo ticket xác thực
                    var ticket = new FormsAuthenticationTicket(
                        1,
                        customer.CustomerID, // User.Identity.Name
                        DateTime.Now,
                        DateTime.Now.AddDays(30), // persistent trong 30 ngày
                        true,
                        userData,
                        FormsAuthentication.FormsCookiePath
                    );

                    string encrypted = FormsAuthentication.Encrypt(ticket);
                    var authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encrypted)
                    {
                        HttpOnly = true,
                        Expires = DateTime.Now.AddDays(30)
                    };
                    Response.Cookies.Add(authCookie);


                    return Json(new { success = true });
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException ex)
                {
                    foreach (var validationErrors in ex.EntityValidationErrors)
                    {
                        foreach (var validationError in validationErrors.ValidationErrors)
                        {
                            System.Diagnostics.Debug.WriteLine($"Lỗi tại trường {validationError.PropertyName}: {validationError.ErrorMessage}");
                            ModelState.AddModelError(validationError.PropertyName, validationError.ErrorMessage);
                        }
                    }
                    return Json(new { success = false });
                    // Đăng ký cho bảng Customers

                }

                
            }

        }

        // GET: Login (Trang đăng nhập)
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
            }

            if (model.IsEmployee)
            {
                // Đăng nhập nhân viên
                var user = db.Users.FirstOrDefault(u => u.ContactInfo == model.ContactInfor);
                if (user == null)
                    return Json(new { success = false, message = "Email không tồn tại." });

                if (!BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                    return Json(new { success = false, message = "Mật khẩu không đúng." });

                if (user.Status != "Hoạt động")
                    return Json(new { success = false, message = "Liên hệ quản lý để kích hoạt tài khoản." });




                //Session["UserID"] = user.UserID;
                //Session["FullName"] = user.FullName;
                //Session["Role"] = user.Role;
                string userData = $"User|{user.FullName}|{user.ContactInfo}|{user.Role}";

                var ticket = new FormsAuthenticationTicket(
                    1,
                    user.UserID,
                    DateTime.Now,
                    DateTime.Now.AddDays(30),
                    true,
                    userData,
                    FormsAuthentication.FormsCookiePath
                );

                string encrypted = FormsAuthentication.Encrypt(ticket);
                var authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encrypted)
                {
                    HttpOnly = true,
                    Expires = DateTime.Now.AddDays(30)
                };
                Response.Cookies.Add(authCookie);

                return Json(new { success = true });
            }
            else
            {
                // Đăng nhập khách hàng
                var customer = db.Customers.FirstOrDefault(c => c.Email == model.ContactInfor);
                if (customer == null)
                    return Json(new { success = false, message = "Email khách hàng không tồn tại." });

                if (!BCrypt.Net.BCrypt.Verify(model.Password, customer.PasswordHash)) // Hoặc dùng hash nếu đã mã hóa
                    return Json(new { success = false, message = "Mật khẩu không đúng." });


                   var otp = GenerateOtp();
                    EmailService.SendEmail(customer.Email, "OTP Đăng nhập", $"Mã OTP: {otp}");


                string userData = $"Customer|{customer.FullName}|{customer.Email}|{customer.ProfilePicture}";

                var ticket = new FormsAuthenticationTicket(
                    1,
                    customer.CustomerID,
                    DateTime.Now,
                    DateTime.Now.AddDays(30),
                    true,
                    userData,
                    FormsAuthentication.FormsCookiePath
                );

                string encrypted = FormsAuthentication.Encrypt(ticket);
                var authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encrypted)
                {
                    HttpOnly = true,
                    Expires = DateTime.Now.AddDays(30)
                };
                Response.Cookies.Add(authCookie);
                Session["otp"] = otp;
                Session["otptype"] = "login";
                Session["otpexpiry"] = DateTime.UtcNow.AddMinutes(5);

                return Json(new { success = true });
            }
        }


        // Đăng xuất
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            //Session.Clear(); 
                return RedirectToAction("Index", "Home");
        }

        public ActionResult NhanVien()
        {
            if (!AuthHelper.IsAuthenticated)
                return RedirectToAction("Login", "Users");

            var userId = AuthHelper.GetUserID().ToString();

            // Kiểm tra quyền (Role) của người dùng


            var roles = db.Users
                 .Where(u => u.Role != "Quản lý")
                 .Select(u => u.Role)
                 .Distinct()
                 .ToList();

            ViewBag.Roles = roles;
            var model = new UserCustomerViewModel
            {
                Users = db.Users.Where(u => u.UserID != userId).ToList(),
                Customers = db.Customers.ToList()
            };
            ViewBag.AllOrders = db.Orders.Where(o => o.OrderStatus == "Đã thanh toán")
            .Select(o => new {
                o.CustomerID,
                o.TotalAmount,
                o.CreatedAt
            })
            .AsEnumerable() // chuyển sang LINQ to Objects
            .Select(o => new {
                o.CustomerID,
                o.TotalAmount,
                CreatedAt = o.CreatedAt?.ToString("yyyy-MM-dd")
            })
            .ToList();

            return View(model);
        }

        // GET: Create (Trang thêm thông tin người dùng)
        public ActionResult Create()
        {
            return PartialView("_CreateEmployee");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(User user)
        {
            if (user == null)
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ!" });
            }

            try
            {
                // Kiểm tra số điện thoại đã tồn tại
                if (db.Users.Any(u => u.ContactInfo == user.ContactInfo))
                {
                    return Json(new { success = false, message = "Email đã tồn tại!" });
                }

                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Dữ liệu nhập vào không hợp lệ!" });
                }

                do
                {

                    user.UserID = GenerateUserID(6);
                }
                while (db.Users.Any(u => u.UserID == user.UserID));
                // Hash mật khẩu
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
                user.CreatedAt = DateTime.Now;

                db.Users.Add(user);
                db.SaveChanges();

                return Json(new { success = true, message = "Thêm thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        // Chấp nhận tài khoản
        [HttpPost]
        public JsonResult ApproveUser(string id)
        {
            var user = db.Users.Find(id);
            if (user == null)
            {
                return Json(new { success = false, message = "User không tồn tại!" });
            }

            user.Status = "Hoạt động";
            db.SaveChanges();

            return Json(new { success = true, message = "Cập nhật thành công!" });
        }



        public ActionResult Details(string id)
        {
            var customer = db.Customers.FirstOrDefault(c => c.CustomerID == id);
            if (customer == null) return HttpNotFound();

            return PartialView("_CustomerDetails", customer);
        }


        // GET: Edit (Lấy dữ liệu người dùng để hiển thị trong modal)
        public ActionResult Edit(string id)
        {

            var user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return PartialView("_EditEmployee", user);
        }

        // POST: Edit (Cập nhật Role & Status bằng AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Edit(User model)
        {
            try
            {
                // Kiểm tra ModelState có hợp lệ không
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Dữ liệu nhập vào không hợp lệ!" });   
                    
                }

                // Kiểm tra xem user có tồn tại không
                var user = db.Users.Find(model.UserID);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng!" });
                }

                // Cập nhật dữ liệu người dùng
                user.Role = model.Role;
                user.Status = model.Status;
                db.SaveChanges();

                return Json(new { success = true, message = "Cập nhật thành công!", updatedUser = user });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi server: " + ex.Message });
            }
        }


        [HttpPost]
        public JsonResult UpdateAccount(HttpPostedFileBase image, string fullName, string email = null, string phone = null, string address = null, string contactInfo = null)
        {
           
           

            try
            {
               

                // Nếu là user
                if (contactInfo != null)
                {
                    var userId = AuthHelper.GetUserID().ToString();
                    var user = db.Users.Find(userId);
                    // Cập nhật user
                    user.FullName = fullName;
                    user.ContactInfo = contactInfo;
                    // Lấy cookie hiện tại
                    var authCookie = Request.Cookies[FormsAuthentication.FormsCookieName];
                    if (authCookie != null)
                    {
                        // Xóa cookie cũ nếu có
                        var oldCookie = new HttpCookie(FormsAuthentication.FormsCookieName)
                        {
                            Expires = DateTime.Now.AddDays(-1) // Đặt ngày hết hạn trong quá khứ để xóa cookie
                        };
                        Response.Cookies.Add(oldCookie); // Thêm vào response để xóa cookie

                        // Giải mã ticket từ cookie cũ
                        var ticket = FormsAuthentication.Decrypt(authCookie.Value);
                        var userData = ticket.UserData.Split('|');

                        // Cập nhật thông tin người dùng
                        userData[1] = fullName; // Cập nhật FullName
                        userData[2] = contactInfo; // Cập nhật ContactInfo

                        // Tạo lại ticket với dữ liệu đã thay đổi
                        var newUserData = string.Join("|", userData);
                        var newTicket = new FormsAuthenticationTicket(
                            1,
                            ticket.Name,
                            DateTime.Now,
                            DateTime.Now.AddDays(30),
                            true,
                            newUserData,
                            FormsAuthentication.FormsCookiePath
                        );

                        // Mã hóa lại ticket
                        string encrypted = FormsAuthentication.Encrypt(newTicket);

                        // Tạo và thêm cookie mới
                        var newAuthCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encrypted)
                        {
                            HttpOnly = true,
                            Expires = DateTime.Now.AddDays(30) // Đặt thời gian hết hạn đúng
                        };

                        Response.Cookies.Add(newAuthCookie); // Thêm cookie mới
                    }
                }
                else 
                 {
                    var cusID = AuthHelper.GetUserID().ToString();

                    var cus = db.Customers.Find(cusID);
                    if (image != null && image.ContentLength > 0)
                    {
                        var fileName = Path.GetFileName(image.FileName);
                        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(image.FileName);
                        var path = Path.Combine(Server.MapPath("~/wwwroot/Images/"), fileName);
                        image.SaveAs(path);
                        cus.ProfilePicture = fileNameWithoutExtension;
                    }
                    // Cập nhật customer
                    cus.FullName = fullName;
                     cus.Email = email;
                     cus.Phone = phone ?? "";
                     cus.Address = address ?? "";

                    // Lấy cookie hiện tại
                    var authCookie = Request.Cookies[FormsAuthentication.FormsCookieName];
                    if (authCookie != null)
                    {
                        // Xóa cookie cũ nếu có
                        var oldCookie = new HttpCookie(FormsAuthentication.FormsCookieName)
                        {
                            Expires = DateTime.Now.AddDays(-1) // Đặt ngày hết hạn trong quá khứ để xóa cookie
                        };
                        Response.Cookies.Add(oldCookie); // Thêm vào response để xóa cookie

                        // Giải mã ticket từ cookie cũ
                        var ticket = FormsAuthentication.Decrypt(authCookie.Value);
                        var userData = ticket.UserData.Split('|');

                        // Cập nhật thông tin người dùng
                        userData[1] = fullName; // Cập nhật FullName
                        userData[2] = contactInfo; // Cập nhật ContactInfo
                        userData[3] = cus.ProfilePicture;

                        // Tạo lại ticket với dữ liệu đã thay đổi
                        var newUserData = string.Join("|", userData);
                        var newTicket = new FormsAuthenticationTicket(
                            1,
                            ticket.Name,
                            DateTime.Now,
                            DateTime.Now.AddDays(30),
                            true,
                            newUserData,
                            FormsAuthentication.FormsCookiePath
                        );

                        // Mã hóa lại ticket
                        string encrypted = FormsAuthentication.Encrypt(newTicket);

                        // Tạo và thêm cookie mới
                        var newAuthCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encrypted)
                        {
                            HttpOnly = true,
                            Expires = DateTime.Now.AddDays(30) // Đặt thời gian hết hạn đúng
                        };

                        Response.Cookies.Add(newAuthCookie); // Thêm cookie mới
                    }

                }    
                // Lưu vào DB...
                db.SaveChanges();

               


                return Json(new { success = true, message = "Cập nhật thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

      



        // GET: Delete (Trang xác nhận xoá người dùng)
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public JsonResult DeleteConfirmed(string id)
        {
            var user = db.Users.Find(id);
            if (user != null)
            {
                db.Users.Remove(user);
                db.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "User không tồn tại!" });
        }



        // GET: Search (Trang tìm kiếm người dùng)
        public ActionResult Search(string keyword, string type)
        {
            if (type == "user")
            {

                var userId = AuthHelper.GetUserID().ToString();
                var users = db.Users.Where(u => u.UserID != userId).ToList();
                var roles = db.Users
                .Where(u => u.Role != "Quản lý")
                .Select(u => u.Role)
                .Distinct()
                .ToList();

                ViewBag.Roles = roles;
                var model = new UserCustomerViewModel
                {
                    Users = users.Where(u => u.FullName.Contains(keyword) ||
                    u.ContactInfo.Contains(keyword)).ToList()
                };
               
               

                return PartialView("_EmployeeTable", model);
            }
            else if (type == "customer")
            {

                var model = new UserCustomerViewModel
                {
                    Customers = db.Customers
                    .Where(c => c.FullName.Contains(keyword) || c.Email.Contains(keyword))
                    .ToList()
                };

                ViewBag.AllOrders = db.Orders.Where(o => o.OrderStatus == "Đã thanh toán")
               .Select(o => new {
                   o.CustomerID,
                   o.TotalAmount,
                   o.CreatedAt
               })
               .AsEnumerable() // chuyển sang LINQ to Objects
               .Select(o => new {
                   o.CustomerID,
                   o.TotalAmount,
                   CreatedAt = o.CreatedAt?.ToString("yyyy-MM-dd")
               })
               .ToList();

                return PartialView("_CustomerTable", model); // View partial riêng cho customer
            }

            return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        }

        public ActionResult FilterByStatus(string status)
        {
            var users = db.Users.AsQueryable(); // Truy vấn tất cả nhân viên

            var availableStatuses = db.Users.Select(u => u.Status).Distinct().ToList();
            if (!string.IsNullOrEmpty(status) && availableStatuses.Contains(status))
            {
                users = users.Where(u => u.Status == status);
            }

            return PartialView("_EmployeeTable", users.ToList());
        }

        // Quên mật khẩu
        public ActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public JsonResult ForgotPassword(ForgotResetPasswordViewModel model)
        {
            var otp = 100000.ToString();
            var user = db.Users.FirstOrDefault(u => u.ContactInfo == model.ContactInfo.Trim());
            var customer = db.Customers.FirstOrDefault(u => u.Email == model.ContactInfo.Trim());
            
            var  contactInfo = model.ContactInfo;
            if (user != null)
            {
            

                if (IsEmail(model.ContactInfo))
                {
                    otp = GenerateOtp();
                    EmailService.SendEmail(user.ContactInfo, "OTP Đăng nhập", $"Mã OTP: {otp}");

                }
                Session["OTP"] = otp;
                Session["OTPExpiry"] = DateTime.UtcNow.AddMinutes(5);
                Session["OTPType"] = "forgot"; // Đánh dấu là OTP quên mật khẩu

                

                return Json(new { success = true });
                
            }
            else

               
                if(customer != null)
                { 
                    if (IsEmail(model.ContactInfo))
                    {
                        otp = GenerateOtp();
                        EmailService.SendEmail(customer.Email, "OTP Đăng nhập", $"Mã OTP: {otp}");

                    }
                    Session["OTP"] = otp;
                    Session["OTPExpiry"] = DateTime.UtcNow.AddMinutes(5);
                    Session["OTPType"] = "forgot"; // Đánh dấu là OTP quên mật khẩu
                    return Json(new { success = true });
                }
            

            return Json(new { success = false, message = "Người dùng không tồn tại." });
        }

        public ActionResult VerifyOTP()
        {
            return PartialView("_VerifyOtpModal");
        }

        [HttpPost]
        public JsonResult VerifyOTP(ForgotResetPasswordViewModel model)
        {
            var otp = Session["OTP"] as string;
            var expiry = Session["OTPExpiry"] as DateTime?;
            var otpType = Session["OTPType"] as string;

            if (otp == null || expiry == null || expiry < DateTime.UtcNow)
            {
                return Json(new { success = false, message = "OTP đã hết hạn." });
            }

            if (otp != model.OTP)
            {
                return Json(new { success = false, message = "Mã OTP không đúng!" });
            }

            // Nếu là OTP đăng nhập -> Chuyển đến Dashboard
            if (otpType == "login")
            {
                return Json(new { success = true, redirectUrl = "/Home/Index" });
            }
            // Nếu là OTP quên mật khẩu -> Chuyển đến Reset Password
            else if (otpType == "forgot")
            {
                return Json(new { success = true, redirectUrl = "/Users/ResetPassword" });
            }
            else if(otpType == "register")
            {
                return Json(new { success = true, redirectUrl = "/Home/Index" });
            }

            return Json(new { success = false, message = "Lỗi xác minh OTP!" });
        }

        public ActionResult ResetPassword()
        {
            return View();
        }

        [HttpPost]
        public JsonResult ResetPassword(ForgotResetPasswordViewModel model)
        {
            var user = db.Users.FirstOrDefault(u => u.ContactInfo == model.ContactInfo);
            if (user != null)
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                db.SaveChanges();

                return Json(new { success = true, message = "Mật khẩu đã được đặt lại, vui lòng đăng nhập!" });
            }
            else
            {
                var customer = db.Customers.FirstOrDefault(u => u.Email == model.ContactInfo);
                if(customer != null)
                {
                    customer.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                    db.SaveChanges();
                    return Json(new { success = true, message = "Mật khẩu đã được đặt lại, vui lòng đăng nhập!" });
                }
            }
            return Json(new { success = false, message = "Tài khoản không tồn tại." });

           
        }

        [HttpPost]
        public JsonResult ChangePassword(string currentPassword, string newPassword)
        {
            if (!AuthHelper.IsAuthenticated)
            {
                return Json(new { success = false, message = "Chưa đăng nhập" });
            }

            var type = AuthHelper.GetUserType();
            var id = AuthHelper.GetUserID().ToString();

            if (type == "User")
            {
                
                var user = db.Users.FirstOrDefault(u => u.UserID == id);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy tài khoản" });
                }

                if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash)) // Nếu có mã hóa thì dùng BCrypt / SHA1...
                {
                    return Json(new { success = false, message = "Mật khẩu hiện tại không đúng" });
                }

                // Cập nhật mật khẩu mới
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
               
            }
            else
            {
                
                var cus = db.Customers.FirstOrDefault(c => c.CustomerID == id);

                if (cus == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy tài khoản" });
                }

                if (!BCrypt.Net.BCrypt.Verify(currentPassword, cus.PasswordHash)) // Nếu có mã hóa thì dùng BCrypt / SHA1...
                {
                    return Json(new { success = false, message = "Mật khẩu hiện tại không đúng" });
                }

                // Cập nhật mật khẩu mới
                cus.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

            }


            db.SaveChanges();

            return Json(new { success = true });
        }


        // Hàm kiểm tra có phải email hay không
        public bool IsEmail(string input)
        {
            return Regex.IsMatch(input,
                @"^(?!\.)(""([^""\r\\]|\\[""\r\\])*""|" +
                @"([-a-zA-Z0-9!#\$%&'\*\+/=\?\^_`{\|}~](\.?[-a-zA-Z0-9!#\$%&'\*\+/=\?\^_`{\|}~])*){1,64})@" +
                @"([a-zA-Z0-9]([-a-zA-Z0-9]*[a-zA-Z0-9])?\.)+" +
                @"[a-zA-Z]{2,}$",
                RegexOptions.IgnoreCase);
        }

        public ActionResult UserInfor()
        {
            if (!AuthHelper.IsAuthenticated)
            {
                return Json(new { success = false, message = "Chưa đăng nhập" });
            }

            var type = AuthHelper.GetUserType();
            var id = AuthHelper.GetUserID().ToString();

            var model = new UserCustomerViewModel();

            // Nếu cusID có giá trị và userId null
            if (type == "Customer")
            {
                var customer = db.Customers.FirstOrDefault(c => c.CustomerID == id);
                if (customer != null)
                {
                    model.Customer = customer;
                }
            }
            // Nếu userId có giá trị và cusID null
            else if (type == "User")
            {
                var user = db.Users.FirstOrDefault(u => u.UserID == id);
                if (user != null)
                {
                    model.User = user;
                }
            }
            // Nếu cả hai đều có giá trị (tùy theo yêu cầu thì có thể xử lý thêm)
           

            return View(model);
        }


    }
}
