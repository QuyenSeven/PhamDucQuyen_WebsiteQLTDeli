using QLyNhaHangTDeli.Models;
using QLyNhaHangTDeli.Services;
using System;
using System.Drawing.Printing;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace QLyNhaHangTDeli.Controllers
{
    public class PostsController : Controller
    {
        private readonly PostService _postService;
        private readonly RenderService _renderService;
        TDeliDB db = new TDeliDB();

        public PostsController()
        {
            _postService = new PostService();
            _renderService = new RenderService();
        }

        public ActionResult PostView(string search = "", string type = "", int page = 1)
        {
            var result = _postService.GetPagedPosts(search, type, page, 6);

            // Truyền mô hình phân trang và thông tin tìm kiếm đến View
            ViewData["PaginationModel"] = result.paginationModel;
            ViewBag.Search = search;
            ViewBag.Type = type;

            return View(result.posts);
        }

        public ActionResult PostPageAjax(int page = 1, string search = "", string type = "")
        {
            var result = _postService.GetPagedPosts(search, type, page, 6);

            // Render các Partial View cho bài viết và phân trang
            var postsHtml = RenderService.RenderRazorViewToString(ControllerContext, "_PostPartial", result.posts);
            var paginationHtml = RenderService.RenderRazorViewToString(ControllerContext, "_PaginationPartial", result.paginationModel);

            // Trả về kết quả HTML dưới dạng JSON
            return Json(new { html = postsHtml, pagination = paginationHtml }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult PostManager()
        {
            return View();
        }

        [HttpGet]
        public ActionResult GetPostsByCategory(string postType)
        {
            var posts = _postService.GetPostsByCategory(postType);
            var listpost = posts.Select(p => new
            {
                p.PostID,
                p.Title,
                p.Summary,
                p.Content,
                p.PostType,
                p.ImageUrl,
                UpdatedAt = p.UpdatedAt?.ToString("MMM dd, yyyy") ?? "",
                p.AverageRating
            });

            return Json(listpost, JsonRequestBehavior.AllowGet);

        }

        [HttpGet]
        public ActionResult GetAllPosts()
        {
            var posts = _postService.GetAllPosts();
            var listpost = posts.Select(p => new
            {
               
                p.PostID,
                p.Title,
                p.Summary,
                p.Content,
                p.PostType,
                p.ImageUrl,
                UpdatedAt = p.UpdatedAt?.ToString("MMM dd, yyyy") ?? "",
                p.AverageRating,
            });
            
            return Json(listpost, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetPostCountsByCategory()
        {
            var counts = _postService.GetPostCountsByCategory();
            return Json(counts, JsonRequestBehavior.AllowGet);
        }

        public ActionResult DetailsPost(string id)
        {
            var post = _postService.GetPostById(id);
            if (post == null)
                return HttpNotFound();


            // Trả về View với thông tin bài viết và các phần nội dung đã tách
            var viewModel = new Post
            {
                PostID = post.PostID,
                Title = post.Title,
                PostType = post.PostType,
                ImageUrl = post.ImageUrl,
                Summary = post.Summary,
                UpdatedAt = post.UpdatedAt,
                Content = post.Content  ,
                AverageRating = post.AverageRating
            };

            return View(viewModel);
        }

        public ActionResult Details(string id)
        {
            var post = _postService.GetPostById(id);
            if (post == null)
                return HttpNotFound();

          
            // Trả về View với thông tin bài viết và các phần nội dung đã tách
            var viewModel = new Post
            {
                PostID = post.PostID,
                Title = post.Title,
                PostType = post.PostType,
                ImageUrl = post.ImageUrl,
                Summary = post.Summary,
                UpdatedAt = post.UpdatedAt,
                Content = post.Content  ,
                AverageRating = post.AverageRating
            };

            return View(viewModel);
        }


        public ActionResult Create()
        {
            return PartialView("_CreatePost");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Create(Post model, HttpPostedFileBase imageFile)
        {
            if (string.IsNullOrEmpty(model.Title))
            {
                return Json(new { success = false, message = "Tiêu đề không được để trống!" });
            }

            if (string.IsNullOrEmpty(model.Summary))
            {
                return Json(new { success = false, message = "Tóm tắt không được để trống!" });
            }

            if (string.IsNullOrEmpty(model.Content))
            {
                return Json(new { success = false, message = "Nội dung không được để trống!" });
            }

            if (imageFile == null || imageFile.ContentLength == 0)
            {
                return Json(new { success = false, message = "Vui lòng chọn ảnh!" });
            }
            if (string.IsNullOrEmpty(model.PostType) || model.PostType == "PleaseSelect")
            {
                return Json(new { success = false, message = "Vui lòng chọn đề tài!" });
            }


            var result = _postService.CreatePost(model, imageFile, AuthHelper.GetUserID());
            return Json(new { success = result });
        }

        public ActionResult Edit(string id)
        {
            var post = _postService.GetPostForEdit(id);
            if (post == null)
                return HttpNotFound();
            return PartialView("_PostsEdit", post);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Edit(Post model, HttpPostedFileBase imageFile)
        {
            if (string.IsNullOrEmpty(model.Title))
            {
                return Json(new { success = false, message = "Tiêu đề không được để trống!" });
            }

            if (string.IsNullOrEmpty(model.Summary))
            {
                return Json(new { success = false, message = "Tóm tắt không được để trống!" });
            }

            if (string.IsNullOrEmpty(model.Content))
            {
                return Json(new { success = false, message = "Nội dung không được để trống!" });
            }
            var result = _postService.UpdatePost(model, imageFile);
            return Json(new { success = result });
        }

        [HttpPost]
        public JsonResult Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return Json(new { success = false, message = "ID bài viết không hợp lệ!" });
            }

            var result = _postService.DeletePost(id);

            if (!result)
            {
                return Json(new { success = false, message = "Không thể xóa bài viết, vui lòng thử lại!" });
            }
            
            return Json(new { success = result });
        }
    }
}
