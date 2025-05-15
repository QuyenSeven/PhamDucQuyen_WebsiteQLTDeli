using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using QLyNhaHangTDeli.Models;
using QLyNhaHangTDeli.Services;
using System.Diagnostics;

namespace QLyNhaHangTDeli.Controllers
{
    public class HomeController : Controller
    {
        private TDeliDB db = new TDeliDB();



        public ActionResult Index()
        {
            if (!AuthHelper.IsAuthenticated)
            {
                return View();
            }

            var type = AuthHelper.GetUserType();
            var id = AuthHelper.GetUserID().ToString();

            if (type == "User")
            {

                // Lấy thông tin người dùng từ session
                string fullName = AuthHelper.GetFullName().ToString();
                string role =AuthHelper.GetOther().ToString();

                ViewBag.FullName = fullName; // Hiển thị tên người dùng

                // Kiểm tra và chuyển hướng theo vai trò (role)
                if (role == "Quản lý")
                {
                    return RedirectToAction("ManagerIndex"); // Chuyển hướng đến trang dành cho Quản lý
                }
                else if (role == "Nhân viên" || role == "Thủ kho" || role == "CSKH")
                {
                    return RedirectToAction("EmployeeIndex"); // Chuyển hướng đến trang dành cho Nhân viên
                }
            }
            return View(); // Trang mặc định cho khách (không có vai trò)
        }


        private decimal CalculateRevenueChange(decimal currentRevenue, decimal previousRevenue)
        {
            if (previousRevenue == 0) return currentRevenue > 0 ? 100 : 0; // Nếu không có doanh thu ngày hôm qua, trả về 100% nếu có doanh thu hôm nay
            return (currentRevenue - previousRevenue) / previousRevenue * 100;
        }


        public ActionResult ManagerIndex()
        {
            if (!AuthHelper.IsAuthenticated)
            {
                return RedirectToAction("Login", "Users");
            }
            // Xác định thời điểm bắt đầu và kết thúc của ngày hôm nay
            var today = DateTime.Today;
            var startOfDay = today; // 00:00:00 của ngày hôm nay
            var endOfDay = today.AddDays(1).AddTicks(-1); // 23:59:59.9999999 của ngày hôm nay

            // Lọc và tính tổng giá trị đơn hàng trong ngày hôm nay
            var dailyRevenue = db.Orders
                                 .Where(o => o.UpdatedAt.HasValue &&
                                             o.OrderStatus == "Đã thanh toán" &&
                                             o.UpdatedAt.Value >= startOfDay &&
                                             o.UpdatedAt.Value <= endOfDay)
                                 .Sum(o => o.TotalAmount) ?? 0;

            // Xác định thời điểm bắt đầu và kết thúc của ngày hôm qua
            var yesterday = today.AddDays(-1);
            var startOfYesterday = yesterday; // 00:00:00 của ngày hôm qua
            var endOfYesterday = yesterday.AddDays(1).AddTicks(-1); // 23:59:59.9999999 của ngày hôm qua

            // Lọc và tính tổng giá trị đơn hàng trong ngày hôm qua
            var prevDailyRevenue = db.Orders
                                     .Where(o => o.UpdatedAt.HasValue &&
                                                 o.OrderStatus == "Đã thanh toán" &&
                                                 o.UpdatedAt.Value >= startOfYesterday &&
                                                 o.UpdatedAt.Value <= endOfYesterday)
                                     .Sum(o => o.TotalAmount) ?? 0;

            // Tính toán sự thay đổi doanh thu so với ngày hôm qua
            var dailyRevenueChange = CalculateRevenueChange(dailyRevenue, prevDailyRevenue);

            // Gửi dữ liệu vào ViewBag
            ViewBag.DailyRevenue = dailyRevenue.ToString("N0");
            ViewBag.DailyRevenueChange = dailyRevenueChange.ToString("N0");

            // Xác định ngày đầu tiên và ngày cuối cùng của tuần hiện tại
            // Giả sử tuần bắt đầu vào thứ Hai
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek + 1); // Thứ Hai của tuần hiện tại
            var endOfWeek = startOfWeek.AddDays(7).AddTicks(-1); // Chủ Nhật của tuần hiện tại, 23:59:59.9999999

            // Lọc và tính tổng giá trị đơn hàng trong tuần hiện tại
            var weeklyRevenue = db.Orders
                                  .Where(o => o.UpdatedAt.HasValue &&
                                              o.OrderStatus == "Đã thanh toán" &&
                                              o.UpdatedAt.Value >= startOfWeek &&
                                              o.UpdatedAt.Value <= endOfWeek)
                                  .Sum(o => o.TotalAmount) ?? 0;

            // Xác định ngày đầu tiên và ngày cuối cùng của tuần trước
            var startOfLastWeek = startOfWeek.AddDays(-7); // Thứ Hai của tuần trước
            var endOfLastWeek = startOfWeek.AddTicks(-1); // Chủ Nhật của tuần trước, 23:59:59.9999999

            // Lọc và tính tổng giá trị đơn hàng trong tuần trước
            var prevWeeklyRevenue = db.Orders
                                      .Where(o => o.UpdatedAt.HasValue &&
                                                  o.OrderStatus == "Đã thanh toán" &&
                                                  o.UpdatedAt.Value >= startOfLastWeek &&
                                                  o.UpdatedAt.Value <= endOfLastWeek)
                                      .Sum(o => o.TotalAmount) ?? 0;

            // Tính toán sự thay đổi doanh thu so với tuần trước
            var weeklyRevenueChange = CalculateRevenueChange(weeklyRevenue, prevWeeklyRevenue);

            // Gửi dữ liệu vào ViewBag
            ViewBag.WeeklyRevenue = weeklyRevenue.ToString("N0"); // Định dạng số có dấu phân cách
            ViewBag.WeeklyRevenueChange = weeklyRevenueChange.ToString("N0"); // Định dạng số có dấu phân cách


            var startOfMonth = new DateTime(today.Year, today.Month, 1); // Ngày đầu tiên của tháng
            var endOfMonth = startOfMonth.AddMonths(1).AddTicks(-1); // Ngày cuối cùng của tháng

            // Lọc và tính tổng giá trị đơn hàng trong tháng hiện tại
            var monthlyRevenue = db.Orders
                                   .Where(o => o.UpdatedAt.HasValue &&
                                               o.OrderStatus == "Đã thanh toán" &&
                                               o.UpdatedAt.Value >= startOfMonth &&
                                               o.UpdatedAt.Value <= endOfMonth)
                                   .Sum(o => o.TotalAmount) ?? 0;

            // Tính khoảng thời gian của tháng trước
            var startOfLastMonth = startOfMonth.AddMonths(-1); // Ngày đầu tiên của tháng trước
            var endOfLastMonth = startOfMonth.AddTicks(-1); // Ngày cuối cùng của tháng trước

            // Lọc và tính tổng giá trị đơn hàng trong tháng trước
            var prevMonthlyRevenue = db.Orders
                                       .Where(o => o.UpdatedAt.HasValue &&
                                                   o.OrderStatus == "Đã thanh toán" &&
                                                   o.UpdatedAt.Value >= startOfLastMonth &&
                                                   o.UpdatedAt.Value <= endOfLastMonth)
                                       .Sum(o => o.TotalAmount) ?? 0;

            // Tính toán sự thay đổi doanh thu so với tháng trước
            var monthlyRevenueChange = CalculateRevenueChange(monthlyRevenue, prevMonthlyRevenue);

            // Gửi dữ liệu vào ViewBag
            ViewBag.MonthlyRevenue = monthlyRevenue.ToString("N0"); // Định dạng số có dấu phân cách
            ViewBag.MonthlyRevenueChange = monthlyRevenueChange.ToString("N0"); // Định dạng số có dấu phân cách


            // Xác định ngày đầu tiên và ngày cuối cùng của năm hiện tại
            var startOfYear = new DateTime(today.Year, 1, 1); // Ngày đầu tiên của năm
            var endOfYear = new DateTime(today.Year + 1, 1, 1).AddTicks(-1); // Ngày cuối cùng của năm (31/12, 23:59:59.9999999)

            // Lọc và tính tổng giá trị đơn hàng trong năm hiện tại
            var yearlyRevenue = db.Orders
                                  .Where(o => o.UpdatedAt.HasValue &&
                                              o.OrderStatus == "Đã thanh toán" &&
                                              o.UpdatedAt.Value >= startOfYear &&
                                              o.UpdatedAt.Value <= endOfYear)
                                  .Sum(o => o.TotalAmount) ?? 0;

            // Xác định ngày đầu tiên và ngày cuối cùng của năm trước
            var startOfLastYear = new DateTime(today.Year - 1, 1, 1); // Ngày đầu tiên của năm trước
            var endOfLastYear = new DateTime(today.Year, 1, 1).AddTicks(-1); // Ngày cuối cùng của năm trước (31/12, 23:59:59.9999999)

            // Lọc và tính tổng giá trị đơn hàng trong năm trước
            var prevYearlyRevenue = db.Orders
                                      .Where(o => o.UpdatedAt.HasValue &&
                                                  o.OrderStatus == "Đã thanh toán" &&
                                                  o.UpdatedAt.Value >= startOfLastYear &&
                                                  o.UpdatedAt.Value <= endOfLastYear)
                                      .Sum(o => o.TotalAmount) ?? 0;

            // Tính toán sự thay đổi doanh thu so với năm trước
            var yearlyRevenueChange = CalculateRevenueChange(yearlyRevenue, prevYearlyRevenue);

            // Gửi dữ liệu vào ViewBag
            ViewBag.YearlyRevenue = yearlyRevenue.ToString("N0"); // Định dạng số có dấu phân cách
            ViewBag.YearlyRevenueChange = yearlyRevenueChange.ToString("N0");

            // Lợi nhuận theo từng năm
            var yearlyProfitRaw = db.Orders
                .Where(o => o.UpdatedAt.HasValue && o.OrderStatus == "Đã thanh toán") // Chỉ lấy các đơn hàng đã thanh toán và có ngày cập nhật
                .GroupBy(o => o.UpdatedAt.Value.Year) // Nhóm theo năm
                .Select(g => new
                {
                    Year = g.Key, // Năm
                    Profit = g.Sum(o => o.TotalAmount) // Tổng lợi nhuận trong năm
                })
                .OrderBy(g => g.Year) // Sắp xếp theo năm
                .ToList();

            // Format chuỗi sau khi lấy dữ liệu từ DB
            var yearlyProfit = yearlyProfitRaw.Select(y => new
            {
                Year = $"{y.Year}", // Chuyển năm thành chuỗi
                Profit = y.Profit // Lợi nhuận
            }).ToList();


            // Xu hướng lợi nhuận theo từng ngày trong tháng hiện tại

            var dailyProfit = db.Orders
               .Where(o => o.UpdatedAt.HasValue &&
                           o.UpdatedAt.Value.Year == today.Year &&
                           o.UpdatedAt.Value.Month == today.Month &&
                           o.OrderStatus == "Đã thanh toán")
               .GroupBy(o => o.UpdatedAt.Value.Day) // Nhóm theo ngày
               .Select(g => new
               {
                   Day = g.Key, // Để kiểu int thay vì string
                   Profit = g.Sum(o => o.TotalAmount) // Tổng lợi nhuận trong ngày
               })
               .OrderBy(g => g.Day) // Sắp xếp theo số nguyên
               .ToList();

                    // Nếu cần chuyển Day thành chuỗi để hiển thị, làm như sau:
                    var formattedDailyProfit = dailyProfit.Select(d => new
                    {
                        Day = d.Day.ToString(), // Chuyển sang chuỗi
                        Profit = d.Profit
                    }).ToList();

            // Phân bổ loại sản phẩm theo tháng hiện tại
            var productDistribution = db.OrderDetails
                .Where(od => od.Order.UpdatedAt.HasValue && od.Order.UpdatedAt.Value.Year == today.Year && od.Order.UpdatedAt.Value.Month == today.Month)
                .GroupBy(od => od.Product.Category.CategoryName)
                .Select(g => new
                {
                    Category = g.Key,
                    Count = g.Sum(od => od.Quantity)
                })
                .ToList();


            // Lấy số lượng hóa đơn theo tháng
            var monthlyInvoiceCounts = Enumerable.Range(1, 12).Select(month =>
                db.Orders.Count(o => o.CreatedAt.HasValue &&
                                     o.CreatedAt.Value.Month == month &&
                                     o.CreatedAt.Value.Year == DateTime.Now.Year)
            ).ToList();


            // Truyền dữ liệu qua ViewBag
            ViewBag.MonthlyInvoiceData = monthlyInvoiceCounts;
            // Lấy số lượng đơn khiếu nại đã xử lý và tổng số đơn
            var complaintStats = db.Complaints
                .GroupBy(c => 1)
                .Select(g => new
                {
                    ResolvedComplaints = g.Count(c => c.Status == "Đã xử lý"),
                    TotalComplaints = g.Count()
                })
                .FirstOrDefault();

            ViewBag.Resolved = complaintStats?.ResolvedComplaints ?? 0;
            ViewBag.Total = complaintStats?.TotalComplaints ?? 0;
            ViewBag.ProgressPercent = complaintStats != null && complaintStats.TotalComplaints > 0
                ? Math.Round((double)complaintStats.ResolvedComplaints / complaintStats.TotalComplaints * 100, 2)
                : 0;


            var colors = new[] { "rgba(255, 99, 132, 0.6)", "rgba(54, 162, 235, 0.6)", "rgba(255, 206, 86, 0.6)", "rgba(75, 192, 192, 0.6)", "rgba(153, 102, 255, 0.6)", "rgba(255, 159, 64, 0.6)" };

            // Gán ViewBag.MonthlyProfit với mỗi năm một màu khác nhau
            ViewBag.MonthlyProfit = JsonConvert.SerializeObject(new
            {
                labels = yearlyProfit.Select(m => m.Year).ToArray(),
                datasets = yearlyProfit.Select((m, index) => new
                {
                    label = $"Lợi nhuận năm {m.Year}",
                    data = new[] { m.Profit },
                    backgroundColor = colors[index % colors.Length] // Lặp lại màu nếu số năm nhiều hơn số màu
                }).ToArray()
            });

            ViewBag.DailyProfit = JsonConvert.SerializeObject(new
            {
                labels = dailyProfit.Select(d => d.Day).ToArray(),
                        datasets = new[]
               {
                new {
                    label = "Lợi nhuận theo ngày",
                    data = dailyProfit.Select(d => d.Profit).ToArray(),
                    borderColor = "rgba(75, 192, 192, 1)", // Màu xanh lá cây đậm cho viền
                    backgroundColor = "rgba(75, 192, 192, 0.2)", // Màu xanh lá cây nhạt cho nền
                    fill = true
                }
            }
            });

            ViewBag.ProductPieChart = JsonConvert.SerializeObject(new
            {
                labels = productDistribution.Select(p => p.Category).ToArray(),
                datasets = new[]
                {
                new {
                    data = productDistribution.Select(p => p.Count).ToArray(),
                    backgroundColor = new[] {
                        "#36A2EB", // Xanh dương nhạt
                        "#4BC0C0", // Xanh ngọc
                        "#9966FF", // Tím nhạt
                        "#4CAF50", // Xanh lá cây
                        "#1E88E5", // Xanh dương đậm
                        "#8E44AD", // Tím đậm
                        "#3498DB", // Xanh biển
                        "#2ECC71"  // Xanh lục
                    }
                }
            }
            });

            return View();
            
        }

        public ActionResult EmployeeIndex()
        {
            if (!AuthHelper.IsAuthenticated)
            {
                return RedirectToAction("Login", "Users");
            }
            ViewBag.Categories = db.Categories.ToList();
           


            return View(); // Đảm bảo có Views/Home/EmployeeIndex.cshtml
        }

        public ActionResult ProductsListHome(string keyword = "", string category = "", int page = 1, int pageSize = 12)
        {
            var query = db.Products.AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(p => p.ProductName.Contains(keyword));

            if (!string.IsNullOrEmpty(category))
                query = query.Where(p => p.CategoryID == category); // assuming CategoryID is string or int

            var totalItems = query.Count();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var products = query.OrderBy(p => p.ProductName)
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize)
                                .ToList();
            if (products == null || !products.Any())
            {
                Console.WriteLine("Products list is null or empty");
            }
           
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
           


            // Truyền đúng dữ liệu vào view
            return PartialView("_ProductsList", products);   // Gửi danh sách sản phẩm vào view
        }

        public ActionResult GetRandomProducts()
        {
            var products = db.Products
                .Where(p => p.Status == "Còn hàng" && p.Category.CategoryName != "#Sản phẩm khác")
                .OrderBy(x => Guid.NewGuid())
                .Take(6)
                .ToList(); // <-- Chuyển thành List trong bộ nhớ

            return PartialView("_RandomProducts", products);
        }

        public ActionResult OtherProducts()
        {
            var products = db.Products
                .Where(p => p.Category.CategoryName == "#Sản phẩm khác")
                .ToList();

            return PartialView("_OtherProductsSlider", products);
        }


        public ActionResult RandomPosts(int count = 5)
        {
            var randomPosts = db.Posts
                .Where(p => !string.IsNullOrEmpty(p.ImageUrl))
                .OrderBy(r => Guid.NewGuid())
                .Take(count)
                .ToList();

            return PartialView("_RandomPosts", randomPosts);
        }


        [HttpGet]
        public JsonResult SearchSuggestions(string query)
        {
            var productSuggestions = db.Products
                .Where(p => p.ProductName.Contains(query))
                .Select(p => new
                {
                    Label = p.ProductName,
                    Type = "product",
                    Id = p.ProductID
                })
                .Take(5)
                .ToList();

            var postSuggestions = db.Posts
                .Where(p => p.Title.Contains(query))
                .Select(p => new
                {
                    Label = p.Title,
                    Type = "post",
                    Id = p.PostID
                })
                .Take(5)
                .ToList();

            var suggestions = productSuggestions.Concat(postSuggestions).ToList();

            return Json(new {  suggestions }, JsonRequestBehavior.AllowGet);
        }


    }

}