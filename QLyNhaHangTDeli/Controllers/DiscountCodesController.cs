using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using QLyNhaHangTDeli.Models;
using QLyNhaHangTDeli.Services;

using System.Data.Entity.Infrastructure;
using QLyNhaHangTDeli.Models.ModelView;
using System.Data.Entity.Validation;  // Thêm dòng này để sử dụng DbUpdateException

namespace QLyNhaHangTDeli.Controllers
{
    public class DiscountCodesController : Controller
    {
        private TDeliDB db = new TDeliDB();

        public ActionResult DiscountCodeManager()
        {
            return View(db.DiscountCodes.ToList());
        }

        // GET: DiscountCode/Create
        [HttpPost]
        public ActionResult SaveDiscount(DiscountCode discountCode)
        {
            try
            {
                // Kiểm tra model có hợp lệ không
                if (ModelState.IsValid)
                {

                    
                    // Nếu discountCodeID không tồn tại (mới tạo), gán CreatedAt và IsActive
                    if (string.IsNullOrEmpty(discountCode.DiscountCodeID))
                    {
                        var isDuplicate = db.DiscountCodes.Any(d => d.Code == discountCode.Code);
                        if (isDuplicate)
                        {
                            return Json(new { success = false, message = "Mã giảm giá đã tồn tại." });
                        }
                        // Tạo ID ngẫu nhiên cho DiscountCodeID
                        discountCode.DiscountCodeID = RandomIDGenerator.Generate(8); // Tạo mã ngẫu nhiên với độ dài 8 ký tự
                        discountCode.CreatedAt = DateTime.Now; // Gán thời gian tạo 
                        discountCode.UpdatedAt = discountCode.UpdatedAt;
                        discountCode.IsActive = discountCode.IsActive ?? true; // Nếu không có, mặc định là true
                        discountCode.UsageLimit = discountCode.UsageLimit ?? null;
                        discountCode.MinimumOrderValue = discountCode.MinimumOrderValue ?? 0;
                        discountCode.MaxDiscountAmount = discountCode.MaxDiscountAmount ?? 0;
                    }
                    else
                    {
                        discountCode.UpdatedAt = DateTime.Now; // Cập nhật thời gian khi sửa mã giảm giá
                        discountCode.IsActive = discountCode.IsActive ?? true; // Nếu không có, mặc định là true
                        discountCode.UsageLimit = discountCode.UsageLimit ?? null;
                        discountCode.MinimumOrderValue = discountCode.MinimumOrderValue ?? 0;
                        discountCode.MaxDiscountAmount = discountCode.MaxDiscountAmount ?? 0;
                    }

                    // Thêm hoặc cập nhật discountCode vào database
                    db.DiscountCodes.AddOrUpdate(discountCode);

                    // Lưu thay đổi vào cơ sở dữ liệu
                    try
                    {
                        db.SaveChanges(); // Hoặc thao tác với cơ sở dữ liệu của bạn
                    }
                    catch (DbUpdateException ex)
                    {
                        var innerException = ex.InnerException?.Message ?? "No inner exception";
                        var errorDetails = ex.Message;
                        Console.WriteLine($"Lỗi cập nhật cơ sở dữ liệu: {errorDetails}, Inner: {innerException}");
                    }

                    return Json(new { success = true, message = "Thêm thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Thiếu dữ liệu" });
                }
            }
            catch (Exception ex)
            {
                // Log lỗi nếu cần thiết
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult ToggleIsActive(string id, bool isActive)
        {
            try
            {
                var discount = db.DiscountCodes.FirstOrDefault(d => d.DiscountCodeID == id);
                if (discount == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy mã giảm giá." });
                }

                discount.IsActive = isActive;
                db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult DeleteConfirmed(string id)
        {
            try
            {
                var item = db.DiscountCodes.Find(id);
                if (item == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy mã giảm giá." });
                }

                db.DiscountCodes.Remove(item);
                db.SaveChanges();

                return Json(new { success = true, message = "Xoá thành công." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        public ActionResult GetAssignedCustomers(string discountCodeId)
        {
            var assignedCustomers = db.CustomerDiscountCodes
                .Where(c => c.DiscountCodeID == discountCodeId)
                .Include(c => c.Customer)
                .ToList();

            return PartialView("_AssignedCustomersPartial", assignedCustomers);
        }

        public ActionResult ListCouponsPartial()
        {
            // Mã công khai
            var publicCoupons = db.DiscountCodes
                                  .Where(dc => dc.IsPublic == true && dc.IsActive == true)
                                  .ToList();

            // Mã riêng tư nhưng có liên kết với bảng CustomerDiscountCode
            var privateLinkedCoupons = db.DiscountCodes
                                         .Where(dc => dc.IsPublic == false && dc.IsActive == true && dc.CustomerDiscountCodes.Any())
                                         .ToList();

            var model = publicCoupons.Concat(privateLinkedCoupons).ToList();
            return PartialView("_ListCouponsPartial", model);
        }

        [HttpPost]
        public JsonResult RemoveAssignment(string id)
        {
            try
            {
                var item = db.CustomerDiscountCodes.Find(id);
                if (item == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy gán mã." });
                }

                db.CustomerDiscountCodes.Remove(item);
                db.SaveChanges();

                return Json(new { success = true, message = "Đã gỡ mã khỏi khách hàng." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        public ActionResult AssignedList(string customerId)
        {
            var codes = db.DiscountCodes
               .Where(dc => dc.IsPublic == false && dc.IsActive == true)
               .Select(dc => new DiscountCodeViewModel
               {
                   CustomerID = customerId,
                   Code = dc.Code,
                   DiscountAmount = dc.DiscountAmount,
                   DiscountType = dc.DiscountType,
                   IsAssigned = db.CustomerDiscountCodes
                                   .Any(cdc => cdc.CustomerID == customerId && cdc.DiscountCodeID == dc.DiscountCodeID)
               })
               .ToList();


            return PartialView("_CustomerDiscountList", codes);
        }

        [HttpPost]
        public JsonResult AssignToCustomer(string customerId, string code)
        {
            var discount = db.DiscountCodes.FirstOrDefault(d => d.Code == code && d.IsActive == true);
            if (discount == null)
                return Json(new { success = false, message = "Mã không hợp lệ" });

            if (db.CustomerDiscountCodes.Any(c => c.CustomerID == customerId && c.DiscountCodeID == discount.DiscountCodeID))
                return Json(new { success = false, message = "Khách hàng đã có mã này" });

            var assignment = new CustomerDiscountCode
            {
                CustomerDiscountID = RandomIDGenerator.Generate(8),
                CustomerID = customerId,
                DiscountCodeID = discount.DiscountCodeID,
                AssignedAt = DateTime.Now
            };
            db.CustomerDiscountCodes.Add(assignment);
            try
            {
                db.SaveChanges();
            }
            catch (DbEntityValidationException ex)
            {
                foreach (var validationErrors in ex.EntityValidationErrors)
                {
                    foreach (var validationError in validationErrors.ValidationErrors)
                    {
                        // In ra các lỗi xác thực chi tiết
                        Console.WriteLine("Property: " + validationError.PropertyName + " Error: " + validationError.ErrorMessage);
                    }
                }
                throw; // Ném lại lỗi để tiếp tục xử lý
            }

            return Json(new { success = true, message = "Gán mã thành công" });
        }

        [HttpPost]
        public JsonResult RemoveFromCustomer(string customerId, string code)
        {
            var discount = db.DiscountCodes.FirstOrDefault(d => d.Code == code);
            if (discount == null)
                return Json(new { success = false, message = "Mã không hợp lệ" });

            var existing = db.CustomerDiscountCodes
                .FirstOrDefault(c => c.CustomerID == customerId && c.DiscountCodeID == discount.DiscountCodeID);
            if (existing == null)
                return Json(new { success = false, message = "Khách hàng chưa có mã này" });

            db.CustomerDiscountCodes.Remove(existing);
            db.SaveChanges();

            return Json(new { success = true, message = "Gỡ mã thành công" });
        }

        public JsonResult Search(string query, string customerId)
        {
            var now = DateTime.Now;
            var publicCoupons = db.DiscountCodes
                        .Where(dc => dc.IsPublic == true && dc.IsActive == true)
                        .ToList();

            var privateLinkedCoupons = db.DiscountCodes
                                         .Where(dc => dc.IsPublic == false && dc.IsActive == true &&
                                                      dc.CustomerDiscountCodes.Any(cdc => cdc.CustomerID == customerId))
                                         .ToList();

            var codes = publicCoupons.Concat(privateLinkedCoupons)
                .Where(dc => string.IsNullOrEmpty(query) || dc.Code.Contains(query))
                .OrderBy(dc => dc.ExpiryDate)
                .Select(dc => dc.Code) // Chỉ lấy Code string
                .Distinct()
                .Take(5) // chỉ lấy 5 gợi ý nếu không nhập gì
                .ToList();

            return Json(new { success = true, codes = codes }, JsonRequestBehavior.AllowGet);
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
