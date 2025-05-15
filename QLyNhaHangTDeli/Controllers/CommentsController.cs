using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using QLyNhaHangTDeli.Models;
using QLyNhaHangTDeli.Models.ModelView;
using QLyNhaHangTDeli.Services;

namespace QLyNhaHangTDeli.Controllers
{
    public class CommentsController : Controller
    {
        private TDeliDB db = new TDeliDB();

        // GET: Comments
        public ActionResult Get5Comments(string postId)
        {
            var comments = db.Comments
                .Where(c => c.PostID == postId)
                .OrderByDescending(c => c.CreatedAt)
                .Take(5)
                .ToList();

            var result = new List<CommentViewModel>();

            foreach (var c in comments)
            {
                var viewModel = new CommentViewModel
                {
                    CommentID = c.CommentID,
                    Content = c.Content,
                    Rating = c.Rating, // ✅ lấy rating
                    CreatedAt = c.CreatedAt ?? DateTime.Now,
                };

                if (c.UserType == "User")
                {
                    var user = db.Users.Find(c.UserID);
                    viewModel.DisplayName = user?.FullName;
                    viewModel.AvatarUrl = null;
                }
                else if (c.UserType == "Customer")
                {
                    var customer = db.Customers.Find(c.UserID);
                    viewModel.DisplayName = customer?.FullName;
                    viewModel.AvatarUrl = customer?.ProfilePicture;
                }

                result.Add(viewModel);
            }

            return PartialView("_CommentPartial", result);
        }

        // GET: Comments
        public ActionResult GetAllComments(string postId)
        {
            var comments = db.Comments
                .Where(c => c.PostID == postId)
                .OrderByDescending(c => c.CreatedAt)
                .ToList();

            var result = new List<CommentViewModel>();

            foreach (var c in comments)
            {
                var viewModel = new CommentViewModel
                {
                    CommentID = c.CommentID,
                    Content = c.Content,
                    Rating = c.Rating, // ✅ lấy rating
                    CreatedAt = c.CreatedAt ?? DateTime.Now,
                };

                if (c.UserType == "User")
                {
                    var user = db.Users.Find(c.UserID);
                    viewModel.DisplayName = user?.FullName;
                    viewModel.AvatarUrl = null;
                }
                else if (c.UserType == "Customer")
                {
                    var customer = db.Customers.Find(c.UserID);
                    viewModel.DisplayName = customer?.FullName;
                    viewModel.AvatarUrl = customer?.ProfilePicture;
                }

                result.Add(viewModel);
            }

            return PartialView("_CommentPartial", result);
        }

        // POST: Add Comment
        [HttpPost]
        public ActionResult AddComment(string postId, string content, int? rating)
        {
            if (!AuthHelper.IsAuthenticated)
                return new HttpStatusCodeResult(401); // Chưa đăng nhập
            if (rating.HasValue && (rating < 1 || rating > 5))
                rating = null; // Hoặc return lỗi, tùy ý
            var comment = new Comment
            {
                CommentID = Guid.NewGuid().ToString(),
                PostID = postId,
                UserID = AuthHelper.GetUserID(),
                UserType = AuthHelper.GetUserType(), // "User" hoặc "Customer"
                Content = content,
                CreatedAt = DateTime.Now ,
                Rating = rating
            };

            db.Comments.Add(comment);
            db.SaveChanges();
            // ✅ Tính lại rating trung bình của bài viết
            var ratingComments = db.Comments
                .Where(c => c.PostID == postId && c.Rating.HasValue)
                .ToList();

            double? averageRating = null;
            if (ratingComments.Any())
            {
                averageRating = Math.Round(ratingComments.Average(c => c.Rating.Value), 1);
            }

            // ✅ Cập nhật lại trường AverageRating trong bảng Posts
            var post = db.Posts.FirstOrDefault(p => p.PostID == postId);
            if (post != null)
            {
                post.AverageRating = averageRating;
                db.SaveChanges(); // Lưu thay đổi
            }

            return Json(new { success = true });
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
