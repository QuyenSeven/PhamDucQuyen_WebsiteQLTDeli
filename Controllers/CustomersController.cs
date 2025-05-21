using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using QLyNhaHangTDeli.Models;
using QLyNhaHangTDeli.Services;

namespace QLyNhaHangTDeli.Controllers
{
    public class CustomersController : Controller
    {
        private TDeliDB db = new TDeliDB();

        [HttpPost]
        public JsonResult UpdateInfo(string FullName, string Phone, string Address)
        {
            try
            {
                string customerId = AuthHelper.GetUserID(); // Hoặc lấy từ xác thực

                if (string.IsNullOrEmpty(customerId))
                    return Json(new { success = false, message = "Không xác định được người dùng." });

                var customer = db.Customers.Find(customerId);
                if (customer == null)
                    return Json(new { success = false, message = "Không tìm thấy người dùng." });

                customer.FullName = FullName;
                customer.Phone = Phone;
                customer.Address = Address;

                db.SaveChanges();

                return Json(new { success = true, message = "Cập nhật thông tin thành công." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult CreateAccount(string Email, string FullName, string Phone, string Address)
        {
            if (!IsEmail(Email))
            {
                return Json(new { success = false, message = "Email không hợp lệ!" });
            }
            if (db.Customers.Any(c => c.Email == Email))
            {
                return Json(new { success = false, message = "Email đã tồn tại trong hệ thống." });
            }

            var newCustomer = new Customer
            {
                CustomerID = GenerateCustomerID(),
                Email = Email,
                FullName = FullName,
                Phone = Phone,
                Address = Address,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("000000"),
                CreatedAt = DateTime.Now
                // Bạn có thể thêm default password hash nếu cần
            };

            db.Customers.Add(newCustomer);
            db.SaveChanges();


            return Json(new { success = true, message = "Tạo tài khoản thành công." });
        }

        // Hàm tạo mã CustomerID (tuỳ theo logic của bạn)
        private string GenerateCustomerID()
        {
          
            string randomPart = RandomIDGenerator.Generate(6);
            return "CUS" + randomPart;
        }

        public bool IsEmail(string input)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(input);
                return addr.Address == input;
            }
            catch
            {
                return false;
            }
        }

    }





}
