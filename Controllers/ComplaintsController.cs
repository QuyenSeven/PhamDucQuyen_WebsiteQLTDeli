using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using QLyNhaHangTDeli.Models;
using QLyNhaHangTDeli.Services;

namespace QLyNhaHangTDeli.Controllers
{
    public class ComplaintsController : Controller
    {
        private TDeliDB db = new TDeliDB();

        private string GenerateCPLTID(int length)
        {

            string randomPart = RandomIDGenerator.Generate(length);
            return "SERVICES" + randomPart;
        }


        public ActionResult ComplaintList(string subject, DateTime? date, string status, string sortOrder, int page = 1)
        {
            DateTime threeMonthsAgo = DateTime.Now.AddMonths(-3);

            // Lấy danh sách khiếu nại cần xoá:
            // 1. Rỗng Subject hoặc Description
            // 2. Đã xử lý và quá 3 tháng
            var expiredComplaints = db.Complaints
                .Where(c =>
                    string.IsNullOrEmpty(c.Subject) ||
                    string.IsNullOrEmpty(c.Description) ||
                    (c.Status == "Đã xử lý" && c.CreatedAt < threeMonthsAgo)
                )
                .ToList();

            // Xoá nếu có
            if (expiredComplaints.Any())
            {
                db.Complaints.RemoveRange(expiredComplaints);
                db.SaveChanges();
            }

            var complaints = db.Complaints
                            .OrderBy(c => c.Status == "Đã xử lý")          // "Chưa xử lý" sẽ lên trước
                            .ThenByDescending(c => c.CreatedAt)            // Mới nhất trước
                            .AsQueryable();

            // Lọc theo CustomerID
            if (!string.IsNullOrEmpty(subject))
            {
                complaints = complaints.Where(c => c.Subject.Contains(subject));
            }

            // Lọc theo ngày
            if (date.HasValue)
            {
                DateTime startOfDay = date.Value.Date;
                DateTime endOfDay = startOfDay.AddDays(1).AddTicks(-1);
                complaints = complaints.Where(c => c.CreatedAt >= startOfDay && c.CreatedAt <= endOfDay);
            }

            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(status))
            {
                complaints = complaints.Where(c => c.Status == status);
            }

            // Sắp xếp (tuỳ chọn: theo ngày tạo hoặc trạng thái)
            switch (sortOrder)
            {
                case "date_desc":
                    complaints = complaints.OrderByDescending(c => c.CreatedAt);
                    break;
                case "date_asc":
                    complaints = complaints.OrderBy(c => c.CreatedAt);
                    break;
                default:
                    complaints = complaints.OrderByDescending(c => c.CreatedAt); // Mặc định
                    break;
            }

            // Phân trang
            int pageSize = 10;
            var pagination = new PaginationModel
            {
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)complaints.Count() / pageSize)
            };

            var pagedComplaints = complaints.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.Pagination = pagination;

            if (Request.IsAjaxRequest())
            {
                return PartialView("_ComplaintListPartial", pagedComplaints);
            }

            return View(pagedComplaints);
        }

        public ActionResult ComplaintDetail(string id)
        {
            var complaint = db.Complaints
                .Include(c => c.Customer)
                .Include(c => c.Order)
                .FirstOrDefault(c => c.ComplaintID == id);

            if (complaint == null)
            {
                return HttpNotFound();
            }

            return PartialView("_ComplaintDetailPartial", complaint);
        }


        // GET: Complaints
        public ActionResult ComPlaintForm()
        {
          
            var userId = QLyNhaHangTDeli.Services.AuthHelper.GetUserID();

            List<SelectListItem> orders = new List<SelectListItem>();

            if (userId != "QUEST001")
            {
                var listOrders = db.Orders
                    .Where(o => o.CustomerID == userId && o.OrderStatus == "Đã đặt đơn")
                    .Select(o => new SelectListItem
                    {
                        Value = o.OrderID,
                        Text = o.OrderID
                    })
                    .ToList();

                orders = listOrders;
            }

            ViewBag.OrderList = orders;
            ViewBag.UserID = userId;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SubmitComplaint(ComplaintViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("ComPlaintForm", model);
            }

            var type = Services.AuthHelper.GetUserType();
            var userId = Services.AuthHelper.GetUserID();

            var cus = db.Customers.SingleOrDefault(x => x.CustomerID == userId);

            var complaint = new Complaint
            {
                ComplaintID = GenerateCPLTID(6),
                Subject = model.Subject,
                Description = model.Description,
                CreatedAt = DateTime.Now,
                Status = "Chưa xử lý"
            };
            complaint.CustomerID = userId;

            if (userId != "QUEST001")
            {
               
                complaint.OrderID = model.OrderID;
                complaint.Email = cus.Email;
            }
            else
            {
               
                complaint.Email = model.Email;
            }

            db.Complaints.Add(complaint);
            db.SaveChanges();

            TempData["Success"] = "Gửi khiếu nại thành công!";
            return RedirectToAction("ComPlaintForm");
        }

        [HttpPost]
        public JsonResult SendAdminReply(string complaintId, string adminComment, string email)
        {
            var complaint = db.Complaints.FirstOrDefault(c => c.ComplaintID == complaintId);
            if (complaint == null)
            {
                return Json(new { success = false, message = "Không tìm thấy khiếu nại." });
            }

            try
            {
                // Cập nhật phản hồi
                complaint.AdminComment = adminComment;
                complaint.Status = "Đã xử lý";
                complaint.ResolvedAt = DateTime.Now;
                db.SaveChanges();

                // Gửi email đến khách hàng
                string subject = "Phản hồi khiếu nại từ hệ thống TDeli";
                string body = $@"
                <div style='font-family: Arial, sans-serif; font-size: 14px; color: #333;'>
                    <p>Xin chào quý khách,</p>

                    <p>Chúng tôi xin gửi lời <strong>xin lỗi chân thành</strong> về sự bất tiện mà quý khách đã gặp phải.</p>

                    <p>Chúng tôi rất <strong>trân trọng</strong> những phản hồi từ quý khách. Sau khi xem xét khiếu nại, chúng tôi đã xử lý và đưa ra giải pháp như sau:</p>

                    <blockquote style='border-left: 4px solid #ccc; margin: 10px 0; padding-left: 10px; color: #555;'>
                        {adminComment}
                    </blockquote>

                    <p>Rất mong nhận được sự thông cảm từ quý khách.</p>

                    <p>Một lần nữa, xin cảm ơn quý khách đã góp ý để giúp TDeli ngày càng hoàn thiện hơn.</p>

                    <p>Trân trọng,<br/>
                    <strong>TDeli Tea</strong></p>
                </div>";


                EmailService.SendEmail(email, subject, body); // Hàm gửi email tuỳ bạn triển khai

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
