using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using QLyNhaHangTDeli.Models;
using QLyNhaHangTDeli.Services;

namespace Tdelitea.Controllers
{
    public class TablesController : Controller
    {
        private TDeliDB db = new TDeliDB();

        // GET: Tables
        public ActionResult Index(string statusFilter, int page = 1)
        {
            // Lọc theo trạng thái (nếu có)
            var query = db.Tables.AsQueryable();

            if (!string.IsNullOrEmpty(statusFilter))
            {
                query = query.Where(t => t.Status == statusFilter);
            }

            // Sắp xếp theo tên bàn và phân trang
            int pageSize = 10; // Số lượng bàn mỗi trang
            int totalTables = query.Count();
            int totalPages = (int)Math.Ceiling((double)totalTables / pageSize);

            var tables = query
                            .OrderBy(t => t.TableName)
                            .Skip((page - 1) * pageSize)
                            .Take(pageSize)
                            .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.StatusFilter = statusFilter; // Giữ lại bộ lọc trạng thái cho giao diện

            return PartialView("_TableList", tables);
        }


        // GET: Tables/Details/5
        public ActionResult Create()
        {
            return PartialView("_Create"); // Trả về view partial để load vào modal
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(QLyNhaHangTDeli.Models.Table table)
        {
            if (ModelState.IsValid)
            {
                string id;
                do {
                    id = RandomIDGenerator.Generate(6);
                }
                while (db.Tables.Any(t => t.TableID == id)); // Kiểm tra trùng lặp ID
                table.TableID = id;
                if(table.Status != "Trống")
                {
                    table.Status = "Trống";
                }
                db.Tables.Add(table);
                db.SaveChanges();
                return Json(new { success = true });
            }

            // Nếu lỗi, trả lại form với lỗi
            return PartialView("_Create", table);
        }

        public ActionResult Edit(string id)
        {
            var table = db.Tables.Find(id);
            if (table == null)
            {
                return HttpNotFound();
            }

            return PartialView("_EditTablePartial", table); // tạo PartialView
        }

        [HttpPost]
        public ActionResult Edit(QLyNhaHangTDeli.Models.Table model)
        {
            if (ModelState.IsValid)
            {
                var table = db.Tables.Find(model.TableID);
                if (table != null)
                {
                    table.TableName = model.TableName;
                    table.Status = model.Status;
                    db.SaveChanges();
                    return Json(new { success = true, message = "Cập nhật thành công!" });
                }
                return Json(new { success = false, message = "Không tìm thấy bàn!" });
            }

            return Json(new { success = false, message = "Dữ liệu không hợp lệ!" });
        }

        [HttpPost]
        public JsonResult Delete(string id)
        {
            var table = db.Tables.Find(id);
            if (table == null)
            {
                return Json(new { success = false, message = "Không tìm thấy bàn." });
            }

            var orders = db.Orders.Where(o => o.TableID == id).ToList();
            db.Orders.RemoveRange(orders);
            db.Tables.Remove(table);
            db.SaveChanges();
            return Json(new { success = true, message = "Xoá bàn thành công." });
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
