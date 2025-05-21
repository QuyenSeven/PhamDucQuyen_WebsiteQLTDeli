using QLyNhaHangTDeli.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using System.Web.Mvc;
using System.Web.Services.Description;
using System.Text.RegularExpressions;

namespace QLyNhaHangTDeli.Services
{
    public class PostService
    {
        private string GeneratePostID(int length)
        {

            string randomPart = RandomIDGenerator.Generate(length);
            return "POST" + randomPart;
        }

        private readonly TDeliDB db = new TDeliDB();

        public (List<Post> posts, PaginationModel paginationModel) GetPagedPosts(string search, string type, int page, int pageSize)
        {
            var query = db.Posts.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                // Lọc theo tiêu đề bài viết
                query = query.Where(p => p.Title.Contains(search));
            }

            if (!string.IsNullOrEmpty(type))
            {
                // Lọc theo loại bài viết
                query = query.Where(p => p.PostType == type);
            }

            // Tổng số bài viết tìm được
            int totalPosts = query.Count();

            // Lấy các bài viết theo trang
            var posts = query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)  // Bỏ qua các bài viết trước đó
                .Take(pageSize)               // Lấy số bài viết theo kích thước trang
                .ToList();

            var pagination = new PaginationModel
            {
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)totalPosts / pageSize)  // Tính số trang
            };

            return (posts, pagination);
        }



        public List<Post> GetAllPosts()
        {
            var post = db.Posts
                .Select(p => new
                {
                    PostID = p.PostID,
                    PostTitle = p.Title,
                    Summary = p.Summary,
                    Content = p.Content,
                    Type = p.PostType,
                    ImageURL = p.ImageUrl,
                    Date = p.UpdatedAt,
                    AverageRating = p.AverageRating
                })
                .ToList();

            var urlHelper = new UrlHelper(HttpContext.Current.Request.RequestContext);

            // Mapping the anonymous type to Post objects
            var postList = post.Select(p => new  Post
            {
                PostID = p.PostID,
                Title = p.PostTitle,
                Summary = p.Summary,
                Content = p.Content,
                PostType = p.Type,
                ImageUrl = ImageHelper.GetImageUrl(p.ImageURL, urlHelper),
                UpdatedAt = p.Date,
                AverageRating = p.AverageRating,
            }).ToList();


            return postList;
        }


        public List<Post> GetPostsByCategory(string postType)
        {
            var post = db.Posts 
                .Where( p => p.PostType == postType )
                .Select(p => new
                {
                    PostID = p.PostID,
                    PostTitle = p.Title,
                    Summary = p.Summary,
                    Content = p.Content,
                    Type = p.PostType,
                    ImageURL = p.ImageUrl,
                    Date = p.UpdatedAt,
                    AverageRating = p.AverageRating
                })
                .ToList();

            var urlHelper = new UrlHelper(HttpContext.Current.Request.RequestContext);
           

            // Mapping the anonymous type to Post objects
            var postList = post.Select(p => new Post
            {
                PostID = p.PostID,
                Title = p.PostTitle,
                Summary = p.Summary,
                Content = p.Content,
                PostType = p.Type,
                ImageUrl = ImageHelper.GetImageUrl(p.ImageURL, urlHelper) ,
                UpdatedAt = p.Date ,
                AverageRating = p.AverageRating
            }).ToList();

            return postList;
        }

        public Dictionary<string, int> GetPostCountsByCategory()
        {
            var counts =  db.Posts
                .GroupBy(p => p.PostType)
                .ToDictionary(g => g.Key, g => g.Count());
            var total = db.Posts.Count();

            // Thêm tổng "all"
            counts.Add( "all",  total );
            return counts;
        }

        public Post GetPostById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            return db.Posts.FirstOrDefault(p => p.PostID.ToLower() == id.ToLower());
        }

        public bool CreatePost(Post model, HttpPostedFileBase imageFile, string authorId)
        {
            model.PostID = GeneratePostID(6);
            model.UserID = authorId;
            model.CreatedAt = DateTime.Now;
            model.UpdatedAt = DateTime.Now;

            if (imageFile != null)
            {
                var savedName = ImageHelper.SaveImage(imageFile, "~/wwwroot/Images/");
                model.ImageUrl = savedName; // không có đuôi .jpg
            }

            db.Posts.Add(model);
            db.SaveChanges();
            return true;
        }

        public Post GetPostForEdit(string id)
        {
            var post =  db.Posts.Find(id);
            var urlHelper = new UrlHelper(HttpContext.Current.Request.RequestContext);
            var ImageURL = ImageHelper.GetImageUrl(post.ImageUrl, urlHelper);
            post.ImageUrl = ImageURL;

            return post;
        }

        public bool UpdatePost(Post model, HttpPostedFileBase imageFile)
        {
            var existing = db.Posts.Find(model.PostID);
            if (existing == null) return false;

            existing.Title = model.Title;
            existing.Content = model.Content;
            existing.PostType = model.PostType;
            existing.UpdatedAt = DateTime.Now;

            if (imageFile != null)
            {
                if (!string.IsNullOrEmpty(existing.ImageUrl))
                {
                    ImageHelper.DeleteImage(existing.ImageUrl, "~/wwwroot/Images/");
                }

                var savedName = ImageHelper.SaveImage(imageFile, "~/wwwroot/Images/");
                existing.ImageUrl = savedName;
            }

            db.Entry(existing).State = EntityState.Modified;
            db.SaveChanges();
            return true;
        }

        public bool DeletePost(string id)
        {
            var post = db.Posts.Find(id);
            if (post == null) return false;

            if (!string.IsNullOrEmpty(post.ImageUrl))
            {
                ImageHelper.DeleteImage(post.ImageUrl, "~/wwwroot/Images/");
            }

            db.Posts.Remove(post);
            db.SaveChanges();
            return true;
        }
    }
}
