using System;
using System.IO;
using System.Web;
using System.Web.Mvc;

namespace QLyNhaHangTDeli.Services
{
    public static class ImageHelper
    {
        private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };

        public static string GetImageUrl(string imageName, UrlHelper urlHelper)
        {
            if (string.IsNullOrEmpty(imageName))
            {
                return urlHelper.Content("~/wwwroot/Images/no-image.jpg");
            }

            string imagePath = HttpContext.Current.Server.MapPath("~/wwwroot/Images/");

            foreach (var ext in ImageExtensions)
            {
                string fullPath = Path.Combine(imagePath, imageName + ext);
                if (File.Exists(fullPath))
                {
                    return urlHelper.Content("~/wwwroot/Images/" + imageName + ext);
                }
            }

            return urlHelper.Content("~/wwwroot/Images/no-image.jpg");
        }

        public static string SaveImage(HttpPostedFileBase imageFile, string folder = "~/wwwroot/Images/")
        {
            if (imageFile == null || imageFile.ContentLength == 0) return null;

            var fileName = Path.GetFileName(imageFile.FileName);
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);

            var uniqueName = fileNameWithoutExt + "_" + Guid.NewGuid().ToString("N").Substring(0, 6) + extension;

            var path = Path.Combine(HttpContext.Current.Server.MapPath(folder), uniqueName);
            imageFile.SaveAs(path);

            return Path.GetFileNameWithoutExtension(uniqueName); // Trả tên không có đuôi
        }

        public static void DeleteImage(string imageName, string folder = "~/wwwroot/Images/")
        {
            if (string.IsNullOrEmpty(imageName)) return;

            var basePath = HttpContext.Current.Server.MapPath(folder);

            foreach (var ext in ImageExtensions)
            {
                string fullPath = Path.Combine(basePath, imageName + ext);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
        }
    }
}
