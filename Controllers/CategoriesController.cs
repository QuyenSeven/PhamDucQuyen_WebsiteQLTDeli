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
    public class CategoriesController : Controller
    {
        private TDeliDB db = new TDeliDB();

        private string GenerateRandomID(int length)
        {
            string randomPart = RandomIDGenerator.Generate(length);
            return "CATE" + randomPart;
        }

        // GET: Categories
        [HttpGet]
        public ActionResult GetCategories()
        {
            try
            {
                var categories = db.Categories
                    .Select(c => new
                    {
                        c.CategoryID,
                        c.CategoryName,
                        ProductCount = db.Products.Count(p => p.CategoryID == c.CategoryID)
                    })
                    .ToList();

                return Json(categories, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi API GetCategories: " + ex.Message);
                return Json(new { error = true, message = "Không load được loại sản phẩm" });
            }
        }

        [HttpGet]
        public JsonResult GetAllCategories()
        {
            var categories = db.Categories
                .Select(c => new { c.CategoryID, c.CategoryName })
                .ToList();

            return Json(categories, JsonRequestBehavior.AllowGet);
        }


        // GET: Categories/Create
        public ActionResult Create()
        {
            return PartialView("_CreateCategory");
        }

        // POST: Categories/Create

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Category category)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra xem danh mục đã tồn tại chưa
                bool isCategoryExist = db.Categories.Any(c => c.CategoryName.Trim().ToLower() == category.CategoryName.Trim().ToLower());

                if (isCategoryExist)
                {
                    return Json(new { success = false, message = "Danh mục này đã tồn tại." });
                }

                // Sinh mã CategoryID không trùng
                string newId;
                do
                {
                    newId = GenerateRandomID(6);
                } while (db.Categories.Any(c => c.CategoryID == newId));

                category.CategoryID = newId;

                // Nếu chưa tồn tại, thêm mới vào cơ sở dữ liệu
                db.Categories.Add(category);
                db.SaveChanges();

                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Lỗi dữ liệu." });
        }

        [HttpPost]
        // [ValidateAntiForgeryToken]
        public JsonResult DeleteCategory(string id)
        {
            try
            {
                var category = db.Categories.Find(id);
                if (category == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy danh mục." });
                }

                // Kiểm tra xem có sản phẩm nào thuộc danh mục này không
                bool hasProducts = db.Products.Any(p => p.CategoryID == id);
                if (hasProducts)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Vẫn còn sản phẩm thuộc danh mục này. Không thể xóa!"
                    });
                }

                db.Categories.Remove(category);
                db.SaveChanges();

                return Json(new { success = true, message = "Đã xóa danh mục thành công." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
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
