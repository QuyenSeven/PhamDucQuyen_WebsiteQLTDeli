using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using QLyNhaHangTDeli.Models;
using QLyNhaHangTDeli.Services;

namespace Tdelitea.Controllers
{
    public class ProductsController : Controller
    {
        private TDeliDB db = new TDeliDB();

        private string GenerateRandomID(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var id = new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
            return $"PRD-{id}";
        }

        // GET: Products
        public ActionResult ProductsView()
        {
            

            return View();
        }

        [HttpGet]
        public ActionResult GetProductsByCategory(string categoryId)
        {
                    var products = db.Products
               .Where(p => p.CategoryID == categoryId)
               .Select(p => new
               {
                   ProductID = p.ProductID,
                   ProductName = p.ProductName,
                   Price = p.Price,
                   Status = p.Status,
                   ImageURL = p.ImageURL // Không sử dụng Url.Content ở đây
               })
               .ToList();

            

            // Sau khi lấy sản phẩm, thêm đường dẫn hoàn chỉnh cho hình ảnh
            var productList = products.Select(p => new
            {
                ProductID = p.ProductID,
                ProductName = p.ProductName,
                Price = p.Price.ToString("N0"),
                Status = p.Status,
                // Kết hợp đường dẫn thư mục với tên ảnh để tạo đường dẫn ảnh đầy đủ
                ImageUrl = ImageHelper.GetImageUrl(p.ImageURL, new UrlHelper(Request.RequestContext))
            }).ToList();


            return Json(productList, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult GetAllProducts()
        {
            var products = db.Products
               .Select(p => new
               {
                   ProductID = p.ProductID,
                   ProductName = p.ProductName,
                   Price = p.Price,
                   Status = p.Status,
                   ImageURL = p.ImageURL,
                   CategoryName = p.Category.CategoryName // Không sử dụng Url.Content ở đây
               })
               .ToList();



            // Sau khi lấy sản phẩm, thêm đường dẫn hoàn chỉnh cho hình ảnh
            var productList = products.Select(p => new
            {
                ProductID = p.ProductID,
                ProductName = p.ProductName,
                Price = p.Price.ToString("N0"),
                Status = p.Status,
                CategoryName = p.CategoryName,
                // Kết hợp đường dẫn thư mục với tên ảnh để tạo đường dẫn ảnh đầy đủ
                ImageUrl = ImageHelper.GetImageUrl(p.ImageURL, new UrlHelper(Request.RequestContext))
            }).ToList();

            return Json(productList, JsonRequestBehavior.AllowGet);
        }
        public ActionResult GetAllMenu(int page = 1, int pageSize = 8, string view = "grid", string keyword = "", string categoryId = "all")
        {
            var query = db.Products.Where(p => p.Status == "Còn hàng");

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(p => p.ProductName.Contains(keyword));
            }

            if (!string.IsNullOrEmpty(categoryId) && categoryId != "all")
            {
                if (categoryId == "others")
                {
                    query = query.Where(p => p.CategoryID == null);
                }
                else
                {
                    query = query.Where(p => p.CategoryID.ToString() == categoryId);
                }
            }

            var totalItems = query.Count();

            var products = query
                .OrderBy(p => p.ProductName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList() // <-- THỰC HIỆN TRUY VẤN TẠI ĐÂY
                .Select(p => new
                {
                    p.ProductID,
                    p.ProductName,
                    p.Price,
                    p.Status,
                    ImageUrl = ImageHelper.GetImageUrl(p.ImageURL, new UrlHelper(Request.RequestContext)) // xử lý sau khi truy vấn xong
                })
                .ToList();

            return Json(new { data = products, totalItems = totalItems }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ProductDetail(string id)
        {
            var product = db.Products.FirstOrDefault(p => p.ProductID == id);
            if (product == null)
                return HttpNotFound();

            return PartialView("_ProductDetailPartial", product);
        }


        // GET: Products/Create
        public ActionResult Create()
        {
            
            return PartialView("_CreateProduct");
        }

        // POST: Products/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Create(Product model, HttpPostedFileBase imageFile)
        {
            if (ModelState.IsValid)
            {
                if(model.CategoryID == null)
                {
                    return Json(new { success = false, message = "Vui lòng chọn loại sản phẩm" });
                }

                try
                {
                    // Xử lý file ảnh
                    if (imageFile != null && imageFile.ContentLength > 0)
                    {
                        var fileName = Path.GetFileName(imageFile.FileName);
                        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(imageFile.FileName);
                        var path = Path.Combine(Server.MapPath("~/wwwroot/Images/"), fileName);
                        imageFile.SaveAs(path);
                        model.ImageURL = fileNameWithoutExtension;
                    }

                    // Sinh mã ProductID không trùng
                    string randomProductId;
                    do
                    {
                        randomProductId = GenerateRandomID(6);
                    } while (db.Products.Any(p => p.ProductID == randomProductId));

                    model.ProductID = randomProductId;

                    // Lưu sản phẩm vào database
                    db.Products.Add(model);
                    db.SaveChanges();

                    return Json(new { success = true });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message });
                }
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors)
                                   .Select(e => e.ErrorMessage)
                                   .ToList();

            return Json(new { success = false, message = string.Join(", ", errors) });
        }

        // GET: Products/Edit/5
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var product = db.Products.FirstOrDefault(p => p.ProductID == id);
            if (product == null)
            {
                return HttpNotFound();
            }

            var imageUrl = ImageHelper.GetImageUrl(product.ImageURL, new UrlHelper(Request.RequestContext));
            product.ImageURL = imageUrl;
            var categories = db.Categories.ToList();

            ViewBag.Categories = new SelectList(categories, "CategoryID", "CategoryName", product.CategoryID);

            return PartialView("_ProductEdit", product);
        }


        // POST: Products/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Edit(Product model, HttpPostedFileBase imageFile)
        {
            if (ModelState.IsValid)
            {
                try
                {

                    // Giữ nguyên ImageURL cũ nếu không thay đổi ảnh
                     var productToUpdate = db.Products.SingleOrDefault(p => p.ProductID == model.ProductID);
                    if (productToUpdate != null)
                    {
                        // Cập nhật các thông tin của sản phẩm
                        productToUpdate.ProductName = model.ProductName;
                        productToUpdate.Price = model.Price;
                        productToUpdate.CategoryID = model.CategoryID;
                        productToUpdate.Status = model.Status;
                        productToUpdate.ProductDes = model.ProductDes;


                        // Kiểm tra nếu có ảnh cũ và cần xóa
                        if (!string.IsNullOrEmpty(model.ImageURL))
                        {
                            // Các đuôi ảnh phổ biến
                            var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };

                            // Kiểm tra từng đuôi ảnh
                            foreach (var ext in imageExtensions)
                            {
                                var oldImagePath = Path.Combine(Server.MapPath("~/wwwroot/Images/"), model.ImageURL + ext);
                                if (System.IO.File.Exists(oldImagePath))
                                {
                                    System.IO.File.Delete(oldImagePath); // Xóa ảnh cũ nếu tồn tại
                                    break; // Dừng lại khi tìm thấy và xóa ảnh
                                }
                            }
                        }

                        // Nếu có ảnh mới được tải lên
                        if (imageFile != null && imageFile.ContentLength > 0)
                        {
                            var fileName = Path.GetFileName(imageFile.FileName);
                            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(imageFile.FileName);
                            var path = Path.Combine(Server.MapPath("~/wwwroot/Images/"), fileName);
                            imageFile.SaveAs(path);
                            model.ImageURL = fileNameWithoutExtension; // Cập nhật ImageURL với tên ảnh mới
                        }
                        // Nếu không có ảnh mới, giữ nguyên ảnh cũ
                        else
                        {
                            if (string.IsNullOrEmpty(model.ImageURL))
                            {
                                model.ImageURL = productToUpdate.ImageURL; // Giữ nguyên ảnh cũ
                            }
                        }
                        // Không thay đổi ImageURL nếu không có ảnh mới
                        

                        // Cập nhật ImageURL
                        productToUpdate.ImageURL = model.ImageURL;

                        // Lưu thay đổi vào cơ sở dữ liệu
                        db.SaveChanges();
                        return Json(new { success = true });
                    }

                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message });
                }
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors)
                                   .Select(e => e.ErrorMessage)
                                   .ToList();

            return Json(new { success = false, message = string.Join(", ", errors) });
        }



        // POST: Products/Delete/5
        [HttpPost]
        public JsonResult Delete(string id)
        {
            try
            {
                var productToDelete = db.Products.SingleOrDefault(p => p.ProductID == id);
                if (productToDelete != null)
                {
                    db.Products.Remove(productToDelete);
                    db.SaveChanges();
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        public ActionResult MenuView()
        {

            ViewBag.IsLogin = AuthHelper.GetUserType() != null;
            return View();
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
