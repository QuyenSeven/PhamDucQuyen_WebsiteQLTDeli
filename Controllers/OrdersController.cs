using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using QLyNhaHangTDeli.Models;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;
using System.Data.Entity.Infrastructure;
using QLyNhaHangTDeli.Services;
using QLyNhaHangTDeli;
using QLyNhaHangTDeli.Services.Interfaces;
using System.Windows.Forms;
using System.IO;
using QLyNhaHangTDeli.Models.ModelView;
using System.Threading.Tasks;
using System.Configuration;
using QLyNhaHangTDeli.Helpers;
using System.Text.RegularExpressions;


namespace QLyNhaHangTDeli.Controllers
{
    public class OrdersController : Controller
    {
        private TDeliDB db = new TDeliDB();

        private readonly IVnPayService _vnPayService;

        public OrdersController(IVnPayService vnPayService)
        {
            _vnPayService =  vnPayService; // Nếu không dùng DI, khởi tạo trực tiếp
        }

        private string GenerateRandomID(int length)
        {
          
                string randomPart = RandomIDGenerator.Generate(length);
                return "ORDER" + randomPart;
        }

        private void UpdateOrderTotal(string orderId)
        {
            var order = db.Orders.Find(orderId);
            if (order != null)
            {
                var orderDetails = db.OrderDetails.Include("Product").Where(od => od.OrderID == orderId).ToList();
                var totalPrice = orderDetails.Sum(od => (decimal?)(od.Quantity * od.Product.Price)) ?? 0;
                order.Total = totalPrice;
                order.TotalAmount = totalPrice + (order.ShipPrice ?? 0) - (order.Discount ?? 0); // Bạn có thể trừ discount tại đây nếu muốn

                db.SaveChanges();
            }
        }

        public ActionResult OrderList(string orderId, DateTime? date, string status, string sortOrder, int page = 1)
        {

            DateTime thirtyMinutesAgo = DateTime.Now.AddMinutes(-30);

            // Kiểm tra và xoá các đơn hàng chưa thanh toán quá 30 phút với CustomerId = "QUEST001"

            var expiredOrders = db.Orders
                .Where(o => o.CustomerID == "QUEST001" &&
                           (
                               (o.OrderStatus == "Chưa thanh toán" && o.CreatedAt < thirtyMinutesAgo)
                               
                           ))
                .ToList();

            // Xoá các đơn hàng này
            if (expiredOrders.Any())
            {
                db.Orders.RemoveRange(expiredOrders);
                db.SaveChanges();
            }

            var orders = db.Orders.Where(o => o.OrderStatus != "Chưa thanh toán").AsQueryable();

            // Lọc theo Mã đơn
            if (!string.IsNullOrEmpty(orderId))
            {
                orders = orders.Where(o => o.OrderID.Contains(orderId));
            }

            // Lọc theo Ngày (so sánh phần ngày bằng cách tạo một khoảng từ 00:00 đến 23:59)
            if (date.HasValue)
            {
                DateTime startOfDay = date.Value.Date; // 00:00
                DateTime endOfDay = startOfDay.AddDays(1).AddTicks(-1); // 23:59:59.999

                orders = orders.Where(o => o.CreatedAt >= startOfDay && o.CreatedAt <= endOfDay);
            }

            // Lọc theo Trạng thái
            if (!string.IsNullOrEmpty(status))
            {
                orders = orders.Where(o => o.OrderStatus == status);
            }

            // Sắp xếp theo Tổng
            if (sortOrder == "desc")
            {
                orders = orders.OrderByDescending(o => o.TotalAmount);
            }
            else
            {
                orders = orders.OrderBy(o => o.TotalAmount);
            }

            // Phân trang
            int pageSize = 12;
            var pagination = new PaginationModel
            {
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)orders.Count() / pageSize)
            };

            var pagedOrders = orders.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.Pagination = pagination;

            if (Request.IsAjaxRequest())
            {
                return PartialView("_OrderListPartial", pagedOrders);
            }

            return View(pagedOrders);
        }


        [HttpPost]
        public ActionResult UpdateOrderStatus(string id, string newStatus)
        {
            var order = db.Orders.Find(id);
            if (order == null)
                return HttpNotFound();

            order.OrderStatus = newStatus;
            order.UpdatedAt = DateTime.Now;

            db.SaveChanges();

            return Json(new { success = true });
        }



        public JsonResult CheckAutoUpdateOrders()
        {
            var now = DateTime.Now;
            var updatedOrders = new List<object>();

            var orders = db.Orders
                .Where(o => o.OrderStatus == "Đang vận chuyển" && o.UpdatedAt != null)
                .ToList();

            foreach (var order in orders)
            {
                if ((now - order.UpdatedAt.Value).TotalMinutes >= 90)
                {
                    order.OrderStatus = "Đã thanh toán";
                    order.UpdatedAt = now;
                    updatedOrders.Add(new { order.OrderID, order.OrderStatus });
                }
            }

            if (updatedOrders.Any())
                db.SaveChanges();

            return Json(new { success = true, updatedOrders }, JsonRequestBehavior.AllowGet);
        }



        // GET: Orders
        public ActionResult OrdersView()
        {
           
            return View();
        }

        public JsonResult GetProductCount(string orderId)
        {
            var order = db.Orders.Include("OrderDetails").FirstOrDefault(o => o.OrderID == orderId);
            if (order == null)
                return Json(new { success = false, message = "Đơn hàng không tồn tại" }, JsonRequestBehavior.AllowGet);

            int count = order.OrderDetails?.Count ?? 0;
            decimal total =order.Total ?? 0;

            return Json(new { success = true, count , total}, JsonRequestBehavior.AllowGet);
        }

        public ActionResult CreateOrGetOrder()
        {
           
                // Tìm đơn hàng mới nhất chưa thanh toán, không có UserId & TableId
                var existingOrder = db.Orders
                    .Where(o => o.CustomerID == null && o.TableID == null && o.OrderStatus == "Chưa thanh toán")
                    .OrderByDescending(o => o.CreatedAt)
                    .FirstOrDefault();

                if (existingOrder != null)
                {
                    return Json(new { orderId = existingOrder.OrderID }, JsonRequestBehavior.AllowGet);
                }

                string randomOrderId;
                do
                {
                    randomOrderId = GenerateRandomID(6);
                } while (db.Orders.Any(o => o.OrderID == randomOrderId));


            // Nếu không có thì tạo mới
            var newOrder = new Order
                {
                    OrderID = randomOrderId,
                    OrderStatus = "Chưa thanh toán",
                    Payment = "COD",
                    CreatedAt = DateTime.Now
                };

                db.Orders.Add(newOrder);
                db.SaveChanges();

                return Json(new { orderId = newOrder.OrderID }, JsonRequestBehavior.AllowGet);
           
        }
        [HttpPost]
        public ActionResult DeleteOrderIfUnpaid(string orderId)
        {
            // Tìm đơn hàng theo OrderID
            var existingOrder = db.Orders
                                  .FirstOrDefault(o => o.OrderID == orderId && o.OrderStatus == "Chưa thanh toán");

            if (existingOrder != null)
            {
                // Xóa đơn hàng chưa thanh toán
                db.Orders.Remove(existingOrder);
                db.SaveChanges();
                return Json(new { success = true, message = "Đơn hàng đã được xóa." });
            }

            return Json(new { success = false, message = "Không tìm thấy đơn hàng hoặc đơn hàng đã thanh toán." });
        }


        public ActionResult CreateOrGetOrderForCus(string customerId)
        {
            if(customerId == "QUEST001")
            {
                string orderId;
                bool orderIdExists;
                do
                {
                    orderId = GenerateRandomID(6);
                    orderIdExists = db.Orders.Any(o => o.OrderID == orderId);  // Kiểm tra trùng lặp
                } while (orderIdExists);  // Tiếp tục cho đến khi tìm thấy mã duy nhất


                var newOrder = new Order
                {
                    OrderID = orderId, // có thể null
                    CustomerID = customerId,
                    CreatedAt = DateTime.Now,
                    OrderStatus = "Chưa thanh toán",
                    Payment = "COD"
                };

                db.Orders.Add(newOrder);
                db.SaveChanges();
                return Json(new { orderId = newOrder.OrderID }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                // Tìm đơn hàng mới nhất chưa thanh toán, không có UserId & TableId
                var existingOrder = db.Orders
                    .Where(o => o.CustomerID == customerId && o.TableID == null && o.OrderStatus == "Chưa thanh toán")
                    .OrderByDescending(o => o.CreatedAt)
                    .FirstOrDefault();

                if (existingOrder != null)
                {
                    return Json(new { orderId = existingOrder.OrderID }, JsonRequestBehavior.AllowGet);
                }

                string randomOrderId;
                do
                {
                    randomOrderId = GenerateRandomID(6);
                } while (db.Orders.Any(o => o.OrderID == randomOrderId));


                // Nếu không có thì tạo mới
                var newOrder = new Order
                {
                    OrderID = randomOrderId,
                    CustomerID = customerId,
                    OrderStatus = "Chưa thanh toán",
                    Payment = "Chuyển khoản",
                    CreatedAt = DateTime.Now
                };

                try
                {
                    db.Orders.Add(newOrder);
                    
                    
                    db.SaveChanges();
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException ex)
                {
                    foreach (var eve in ex.EntityValidationErrors)
                    {
                        foreach (var ve in eve.ValidationErrors)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"Entity: {eve.Entry.Entity.GetType().Name}, Property: {ve.PropertyName}, Error: {ve.ErrorMessage}");
                        }
                    }

                    // Gợi ý thêm thông báo rõ ràng hơn cho JSON
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ. Vui lòng kiểm tra chi tiết trong Debug log." }, JsonRequestBehavior.AllowGet);
                }
                catch (Exception ex)
                {
                    var innerMessage = ex.InnerException?.InnerException?.Message
                                       ?? ex.InnerException?.Message
                                       ?? ex.Message;

                    System.Diagnostics.Debug.WriteLine("Lỗi khi SaveChanges: " + innerMessage);

                    return Json(new { success = false, message = "Lỗi khi lưu CSDL: " + innerMessage }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { orderId = newOrder.OrderID }, JsonRequestBehavior.AllowGet);
            }

           

           

        }

        [HttpPost]
        public JsonResult AddProductToOrder(string orderId, string productId)
        {
            var orderExists = db.Orders.Any(o => o.OrderID == orderId);
            if (!orderExists)
            {
                System.Diagnostics.Debug.WriteLine("OrderID không tồn tại trong bảng Orders: " + orderId);
                return Json(new { success = false, message = "Đơn hàng không tồn tại. Vui lòng thử lại." });
            }

            var existingItem = db.OrderDetails
                .FirstOrDefault(od => od.OrderID == orderId && od.ProductID == productId);

            if (existingItem != null)
            {
                return Json(new { success = false, message = "Sản phẩm đã tồn tại trong đơn hàng." });
            }

            string newId;
            do
            {
                newId = GenerateRandomID(6);
            } while (db.OrderDetails.Any(od => od.OrderDetailID == newId));


            var newItem = new OrderDetail
            {
                OrderDetailID = newId,
                OrderID = orderId,
                ProductID = productId,
                Quantity = 1
            };
           

            db.OrderDetails.Add(newItem);
            db.SaveChanges();
            UpdateOrderTotal(orderId);

            return Json(new { success = true, message = "Đã thêm sản phẩm vào đơn hàng." });
        }

        public ActionResult OrderDetails(string id)
        {
            var order = db.Orders.Include("OrderDetails.Product").Include("Customer").FirstOrDefault(o => o.OrderID == id);
            if (order == null)
            {
                return HttpNotFound();
            }
            return PartialView("_OrderDetailsPartial", order);
        }


        public ActionResult GetOrderDetail(string orderId)
        {

            var order = db.Orders.Include("OrderDetails").FirstOrDefault(o => o.OrderID == orderId);

            if (order == null)
            {
                return Json(new { success = false, message = "Đơn hàng không tồn tại." }, JsonRequestBehavior.AllowGet);
            }

            if (order.OrderDetails == null || !order.OrderDetails.Any())
            {
                return Json(new { success = false, message = "Chưa có sản phẩm nào trong hóa đơn này." }, JsonRequestBehavior.AllowGet);
            }
            var details = db.OrderDetails
                .Include("Product")
                .Where(od => od.OrderID == orderId)
                .ToList();
            UpdateOrderTotal(orderId);
            if (AuthHelper.GetUserType().ToString() == "User")
            {

                return PartialView("GetOrderDetail", details);
            }

            return PartialView("GetCusOrderDetail", details);
        }

        [HttpPost]
        public JsonResult ClearOrderDetails(string orderId)
        {
            var orderDetails = db.OrderDetails.Where(od => od.OrderID == orderId).ToList();

            if (orderDetails.Any())
            {
                db.OrderDetails.RemoveRange(orderDetails);
                db.SaveChanges();
                UpdateOrderTotal(orderId);
                return Json(new { success = true });
            }

            return Json(new { success = false });
        }

        [HttpPost]
        public JsonResult IncreaseQuantity(string id, string orderId)
        {
            var detail = db.OrderDetails.Find(id);
           

            if (detail != null)
            {

                if (detail.Quantity >= 50)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Số lượng tối đa là 50."
                    });
                }
                detail.Quantity++;
                db.SaveChanges();
                UpdateOrderTotal(orderId);

                var orderEntity = db.Orders.Find(orderId);
                db.Entry(orderEntity).Reload();

                return Json(new
                {
                    success = true,
                    id,
                    newQuantity = detail.Quantity,
                    totalQuantity = db.OrderDetails.Where(o => o.OrderID == detail.OrderID).Sum(x => x.Quantity),
                    totalPrice = orderEntity.Total,
                    finalPrice = orderEntity.TotalAmount
                    
                });
            }

            return Json(new { success = false });
        }

        [HttpPost]
        public JsonResult DecreaseQuantity(string id, string orderId)
        {
            var detail = db.OrderDetails.Find(id);
            if (detail != null && detail.Quantity > 1)
            {
                detail.Quantity--;
                db.SaveChanges();
                UpdateOrderTotal(orderId);

                var orderEntity = db.Orders.Find(orderId);
                db.Entry(orderEntity).Reload();

                return Json(new
                {
                    success = true,
                    id,
                    newQuantity = detail.Quantity,
                    totalQuantity = db.OrderDetails.Where(o => o.OrderID == detail.OrderID).Sum(x => x.Quantity),
                    totalPrice = orderEntity.Total,
                    finalPrice = orderEntity.TotalAmount
                });
            }

            return Json(new { success = false, message = "Số lượng tối thiểu là 1" });
        }
        [HttpPost]
        public JsonResult DeleteOrderDetail(string id)
        {
            var existingOrder = db.Orders
                  .Where(o => o.CustomerID == null && o.TableID == null && o.OrderStatus == "Chưa thanh toán")
                  .OrderByDescending(o => o.CreatedAt)
                  .FirstOrDefault();
            var orderDetail = db.OrderDetails.Find(id);
            if (orderDetail != null)
            {
                string orderId = orderDetail.OrderID ?? "";
                db.OrderDetails.Remove(orderDetail);
                db.SaveChanges();
                UpdateOrderTotal(orderId);

                var orderEntity = db.Orders.Find(orderId);
                db.Entry(orderEntity).Reload();
                var orderDetails = db.OrderDetails.Where(o => o.OrderID == orderId);

                int totalQuantity = orderDetails.Sum(x => (int?)x.Quantity) ?? 0;
                // Áp dụng giảm giá nếu có

                return Json(new
                {
                    success = true,
                    orderId,
                    totalQuantity,
                    totalPrice = orderEntity.Total,
                    finalPrice = orderEntity.TotalAmount
                });
            }

            return Json(new { success = false, message = "Không tìm thấy sản phẩm." });
        }

        [HttpPost]
        public JsonResult DeleteOrder(string id)
        {
            var order = db.Orders.FirstOrDefault(o => o.OrderID == id && o.OrderStatus == "Đã đặt đơn");
            if (order != null)
            {
                db.Orders.Remove(order);
                db.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        public JsonResult GetReceipt(string orderId)
        {
            try
            {
                var order = db.Orders.FirstOrDefault(o => o.OrderID == orderId);
                if (order == null)
                {
                    return Json(new { success = false, message = "Đơn hàng không tồn tại."  }, JsonRequestBehavior.AllowGet);
                }

                // Lấy các chi tiết đơn hàng
                var orderDetails = db.OrderDetails
                    .Where(od => od.OrderID == orderId)
                    .Select(od => new
                    {
                        od.Product.ProductName,
                        od.Quantity,
                        od.Product.Price ,
                        TotalPrice = od.Quantity * od.Product.Price
                    }).ToList();

                decimal totalAmount = db.OrderDetails.Where(o => o.OrderID == orderId)
                                            .Sum(x => x.Quantity * x.Product.Price);
                decimal finalPrice = (totalAmount - (order.Discount ?? 0));

              

                return Json(new
                {
                    success = true,
                    orderId = order.OrderID,
                    createdAt = order.CreatedAt.HasValue ? order.CreatedAt.Value.ToString("dd/MM/yyyy HH:mm") : "N/A"  ,
                    totalQuantity = db.OrderDetails.Where(o => o.OrderID == orderId).Sum(x => x.Quantity),
                    totalPrice = totalAmount,
                    finalPrice = finalPrice,
                    discount = order.Discount ?? 0, // Nếu có chiết khấu
                    orderDetails = orderDetails
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

            public ActionResult GetPayment(string orderId)
            {
            // Giả sử bạn có dữ liệu thanh toán mà bạn muốn hiển thị


            var order = db.Orders.FirstOrDefault(o => o.OrderID == orderId);
            if (order == null)
            {
                return Json(new { success = false, message = "Đơn hàng không tồn tại." }, JsonRequestBehavior.AllowGet);
            }

            
            // Trả về PartialView với model chứa dữ liệu thanh toán
            return PartialView("_Payment", order);
            }


        // VNPay
        [HttpGet]
        public ActionResult CreatePaymentUrlVnpay(string orderId, string address, string fullName, string email, string phone, string discountCode)
        {
            var order = db.Orders.FirstOrDefault(o => o.OrderID == orderId);
            if (order == null)
            {
                return Json(new { success = false, message = "Đơn hàng không tồn tại" });
            }
            string orderDescription = $"thanh toan don hang {order.OrderID}";
            if (order.CustomerID == "QUEST001")
            {
                
                // Nếu có địa chỉ thì thêm vào mô tả, ngược lại chỉ mô tả đơn hàng
                if (!string.IsNullOrWhiteSpace(address) && !string.IsNullOrEmpty(fullName) && !string.IsNullOrEmpty(phone))
                {
                    order.ODDescription = " ho ten " + fullName + " email " + email + " sdt " + phone + " dia chi " + address;
                }
            }
            else
            {
                if(!string.IsNullOrEmpty(discountCode))
                {
                    order.ODDescription += $" Đơn hàng với ma giam gia {discountCode}";
                }
            }

            var model = new PaymentInformationModel
            {
                Amount = Convert.ToDouble(order.TotalAmount.GetValueOrDefault(0)),
               
                OrderDescription = orderDescription,
              
               
                OrderType = "other",
                Name = "Khach hang" ,
                OrderId = order.OrderID
            };

            var paymentUrl = _vnPayService.CreatePaymentUrl(model, HttpContext);
            if (string.IsNullOrEmpty(paymentUrl))
            {
                return Json(new { success = false, message = "Không thể tạo URL thanh toán." }, JsonRequestBehavior.AllowGet);
            }

            //Console.WriteLine("Payment URL: " + paymentUrl);

            return Json(new { success = true, paymentUrl }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult PaymentCallbackVnpay()
        {
            var response = _vnPayService.PaymentExecute(Request.QueryString);
            var model = new JsonView();

            if (response.Success)
            {
                var order = db.Orders.FirstOrDefault(o => o.OrderID == response.OrderId);

                if (order != null)
                {
                    if (response.VnPayResponseCode == "00")
                    {
                        string responseCode = response.OrderDescription;

                        string code = "";

                        if (!string.IsNullOrEmpty(responseCode))
                        {
                            // Biến tất cả về chữ thường để tránh lệ thuộc chữ hoa
                            string lowerResponse = responseCode.ToLower();

                            // Tìm cụm "giam gia"
                            string keyword = "giam gia";
                            int index = lowerResponse.LastIndexOf(keyword);
                            if (index >= 0)
                            {
                                string afterText = responseCode.Substring(index + keyword.Length).Trim();

                                // Lấy từ đầu tiên (bao gồm chữ + số) sau "giam gia"
                                code = new string(afterText
                                    .TakeWhile(c => !char.IsWhiteSpace(c) && c != ',' && c != '.')
                                    .ToArray());

                                Console.WriteLine("==> Mã sau 'giam gia': " + code);
                            }
                        }
                        decimal discountPrice = 0;
                        var discount = db.DiscountCodes.SingleOrDefault(x => x.Code == code);

                        if (discount != null)
                        {
                            if (discount.DiscountType == "Phần trăm")
                            {
                                discountPrice = (order.Total * discount.DiscountAmount ?? 0) / 100;
                            }
                            else if (discount.DiscountType == "Cố định")
                            {
                                discountPrice = discount.DiscountAmount;
                            }

                            // Gán giảm giá vào đơn hàng
                            order.Discount = discountPrice;

                            if(discount.UsedCount == null)
                            {
                                discount.UsedCount = 1;
                            }
                            else
                            {

                                // Tăng số lượt đã dùng của mã giảm giá
                                discount.UsedCount += 1;
                            }

                            // Tạo bản ghi AppliedDiscount mới
                            var appliedDiscount = new AppliedDiscount
                            {
                                AppliedDiscountID = RandomIDGenerator.Generate(8), // hoặc bạn có cách tạo ID riêng
                                OrderID = order.OrderID,
                                DiscountCodeID = discount.DiscountCodeID,
                                CustomerID = order.CustomerID,
                                DiscountAmount = discountPrice,
                                AppliedAt = DateTime.Now
                            };

                            db.AppliedDiscounts.Add(appliedDiscount);
                        }
                        else
                        {
                            order.Discount = 0;
                        }

                        // Cập nhật các trường khác của order
                        order.OrderStatus = "Đã đặt đơn";
                        order.PaymentTransactionId = response.PaymentId;
                        order.VnPayOrderId = response.OrderId;
                        order.VnPayResponseCode = response.VnPayResponseCode;
                        order.OrderDescription = responseCode;
                        order.Payment = "Chuyển khoản";
                        order.UpdatedAt = DateTime.Now;



                        db.SaveChanges();

                        model.Success = "true";
                        model.OrderID = order.OrderID;
                        model.Message = "Thanh toán thành công!";
                    }
                    else
                    {
                        model.Success = "false";
                        model.Message = " Thanh toán thất bại";
                    }
                }
                else
                {
                    model.Success = "false";
                    model.Message = "Không tìm thấy đơn hàng.";
                }
            }
            else
            {
                model.Success = "false";
                model.Message = "Thanh toán thất bại. Vui lòng thử lại.";
            }

            return View("PaymentCallbackResult", model);  // Trả về view với model
        }



        [HttpPost]
        public JsonResult MarkAsPaid(string orderId, bool isCOD, string address, string fullName, string email, string phone, string discountCode)
        {
            try
            {
                var order = db.Orders.FirstOrDefault(o => o.OrderID == orderId);
                if (order == null)
                {
                    return Json(new { success = false, message = "Đơn hàng không tồn tại." });
                }

                if (order.CustomerID != null)
                {
                    // Kiểm tra foreign key CustomerID có tồn tại
                    var customer = db.Customers.FirstOrDefault(c => c.CustomerID == order.CustomerID);
                    if (customer == null)
                    {
                        return Json(new { success = false, message = "Khách hàng không tồn tại: " + order.CustomerID });
                    }
                    if(order.CustomerID == "QUEST001")
                    {
                        order.ODDescription = " ho ten " + fullName +" email " + email+ " sdt " + phone + " dia chi " + address;
                    }
                    decimal discountPrice = 0;
                    var discount = db.DiscountCodes.SingleOrDefault(x => x.Code == discountCode);

                    if (discount != null)
                    {
                        if (discount.DiscountType == "Phần trăm")
                        {
                            discountPrice = (order.Total * discount.DiscountAmount ?? 0) / 100;
                        }
                        else if (discount.DiscountType == "Cố định")
                        {
                            discountPrice = discount.DiscountAmount;
                        }

                        // Gán giảm giá vào đơn hàng
                        order.Discount = discountPrice;

                        if(discount.UsedCount == null)
                        {
                            discount.UsedCount = 1;
                        }
                        else
                        {

                            // Tăng số lượt đã dùng của mã giảm giá
                            discount.UsedCount += 1;
                        }

                        // Tạo bản ghi AppliedDiscount mới
                        var appliedDiscount = new AppliedDiscount
                        {
                            AppliedDiscountID = RandomIDGenerator.Generate(8), // hoặc bạn có cách tạo ID riêng
                            OrderID = order.OrderID,
                            DiscountCodeID = discount.DiscountCodeID,
                            CustomerID = order.CustomerID,
                            DiscountAmount = discountPrice,
                            AppliedAt = DateTime.Now
                        };

                        db.AppliedDiscounts.Add(appliedDiscount);
                    }
                    else
                    {
                        order.Discount = 0;
                    }
                    order.OrderStatus = "Đã đặt đơn";
                    order.Payment = isCOD ? "COD": "Chuyển khoản";
                }
                else  
                {
                   
                    order.OrderStatus = "Đã thanh toán";
                    order.Payment = "COD";
                }

                order.UpdatedAt = DateTime.Now;
                //Console.WriteLine($"OrderID: {order.OrderID}, Payment: {order.Payment}, CustomerID: {order.CustomerID}");

                db.SaveChanges();

                return Json(new { success = true, orderId}, JsonRequestBehavior.AllowGet);
            }
            catch (DbUpdateException ex)
            {
                string message = ex.InnerException?.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = "Lỗi DbUpdate: " + message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult DeleteCurrentOrder(string orderId)
        {
            var order = db.Orders.Find(orderId);
            if (order != null)
            {
                // Xóa các bản ghi trong AppliedDiscounts liên quan đến OrderID
                var appliedDiscounts = db.AppliedDiscounts.Where(ad => ad.OrderID == orderId).ToList();
                db.AppliedDiscounts.RemoveRange(appliedDiscounts);

                // Xóa đơn hàng
                db.Orders.Remove(order);

                // Lưu thay đổi vào cơ sở dữ liệu
                db.SaveChanges();
            }
            return Json(new { success = true });
        }


        [HttpPost]
        public JsonResult Apply(string code, string orderId, string customerId)
        {
            var discount = db.DiscountCodes.FirstOrDefault(d => d.Code == code && d.IsActive == true);
            if (discount == null)
            {
                return Json(new { success = false, message = "Mã không hợp lệ hoặc đã hết hạn." });
            }
            if (discount.IsPublic == false)
            {
                var customerDiscount = db.CustomerDiscountCodes
                                          .FirstOrDefault(cdc => cdc.DiscountCodeID == discount.DiscountCodeID && cdc.CustomerID == customerId);
                if (customerDiscount == null)
                {
                    return Json(new { success = false, message = "Mã giảm giá này không được áp dụng cho bạn." });
                }
            }
            if(discount.UsageLimit != null && discount.UsedCount == discount.UsageLimit)
            {
                return Json(new { success = false, message = "Mã giảm giá đã hết lượt sử dụng." });
            }

            var order = db.Orders.Include("OrderDetails.Product").FirstOrDefault(o => o.OrderID == orderId);
            if (order == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng." });
            }

            // Kiểm tra xem mã giảm giá đã được áp dụng cho khách hàng này chưa (đã lưu trong AppliedDiscounts)
            var existingDiscount = db.AppliedDiscounts
                                     .FirstOrDefault(ad => ad.DiscountCodeID == discount.DiscountCodeID && ad.CustomerID == customerId );
            if (existingDiscount != null)
            {
                return Json(new { success = false, message = "Mã giảm giá đã được sử dụng rồi." });
            }

            // Tính tổng giá trị đơn hàng
            var total = order.OrderDetails.Sum(od => od.Quantity * od.Product.Price);

            if (total < discount.MinimumOrderValue)
            {
                return Json(new { success = false, message = "Đơn hàng không đủ điều kiện áp dụng mã giảm giá." });
            }
            


                decimal Amount;
            if (discount.DiscountType == "Phần trăm")
            {
                Amount = total * (discount.DiscountAmount / 100);
            }
            else
            {
                Amount = discount.DiscountAmount;
            }
            if (Amount > discount.MaxDiscountAmount && discount.MaxDiscountAmount > 0)
            {
                Amount = discount.MaxDiscountAmount ?? 0;
            }

            var final = (total + (order.ShipPrice ?? 0)) - Amount;
            if (final < 0) final = 0;

            // Trả về client thông tin giảm giá, KHÔNG lưu gì vào DB
            return Json(new
            {
                success = true,
                totalPrice = total,
                finalPrice = final,
                shipPrice = order.ShipPrice ?? 0,
                discountAmount = Amount,
                discountCode = discount.Code
            });
        }


        public string RenderPartialViewToString(string viewName, object model)
        {
            var controllerContext = ControllerContext;
            var tempData = controllerContext.Controller.TempData;
            var viewData = new ViewDataDictionary(controllerContext.Controller.ViewData)
            {
                Model = model
            };

            using (var sw = new StringWriter())
            {
                var viewEngineResult = ViewEngines.Engines.FindView(controllerContext, viewName, null);
                var viewContext = new ViewContext(controllerContext, viewEngineResult.View, viewData, tempData, sw);
                viewEngineResult.View.Render(viewContext, sw);
                return sw.GetStringBuilder().ToString();
            }
        }

        public ActionResult CheckOrCreateOrder(string tableId)
        {

            string id;
            do
            {
                id = GenerateRandomID(6);
            }
            while (db.Orders.Any(o => o.OrderID == id));
            var order = db.Orders
                          .FirstOrDefault(o => o.TableID == tableId && o.OrderStatus == "Chưa thanh toán");

           
            if (order == null)
            {
                order = new Order
                {
                    OrderID = id,

                    TableID = tableId,
                    CreatedAt = DateTime.Now,
                    OrderStatus = "Chưa thanh toán" ,
                    Payment = "COD",

                    
                };

                db.Orders.Add(order);
                db.SaveChanges();
            }

           
            var table = db.Tables.SingleOrDefault(t => t.TableID == tableId);

            // Lấy chi tiết đơn hàng (bao gồm thông tin sản phẩm, tổng tiền,... nếu cần)
            var orderDetails = db.OrderDetails
                                 .Where(od => od.OrderID == order.OrderID)
                                 .ToList();

            if (order.TotalAmount > 0)
            {
                table.Status = "Đang sử dụng";
               
            }
            else
            {
                table.Status = "Trống";
            }
            db.SaveChanges();
            var viewModel = new OrderDetailViewModel
            {
                Order = order,
                OrderDetails = orderDetails
            };

            return PartialView("_OrderTableDetails", viewModel);
        }

        public ActionResult LoadOrderDetails(string orderId)
        {
            var order = db.Orders.Find(orderId);
            if (order == null) return HttpNotFound();

            var tableId = order.TableID;
            var table = db.Tables.SingleOrDefault(t => t.TableID == tableId);

            var details = db.OrderDetails.Where(o => o.OrderID == orderId).ToList();

            if (details.Count > 0)
            {
                table.Status = "Đang sử dụng";
                db.SaveChanges();
            }
                var vm = new OrderDetailViewModel
            {
                Order = order,
                OrderDetails = details
            };

            return PartialView("_OrderTableDetails", vm);
        }

        public ActionResult GetInforCus()
        {
            // Giả sử bạn lấy customerId từ Session
            var customerId = AuthHelper.GetUserID();

            if (string.IsNullOrEmpty(customerId))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Không có CustomerID.");
            }

            var customer = db.Customers.FirstOrDefault(c => c.CustomerID == customerId);
            if (customer == null)
            {
                return HttpNotFound("Không tìm thấy thông tin khách hàng.");
            }
            var apiKey = ConfigurationManager.AppSettings["ORSApiKey"];
            ViewBag.ORSApiKey = apiKey;

            return PartialView("_GetInforCus", customer);
        }

        [HttpPost]
        public async Task<JsonResult> CalculateDistance(double latitude, double longitude, string orderId)
        {
            try
            {
                

                var order = db.Orders
                    .FirstOrDefault(o => o.OrderID == orderId);

                // Gọi hàm lấy khoảng cách từ ShippingService (phiên bản OSRM)
                double distance = await ShippingService.GetDistanceFromCoordinatesAsync(latitude, longitude);

                if (distance <= 0)
                {
                    return Json(new { success = false, message = "Không thể tìm thấy địa chỉ nhận hàng" });
                }

                if (distance > 100)
                {
                    return Json(new { success = false, message = "Khu vực không hỗ trợ giao hàng" });
                }

                double fee = ShippingService.CalculateShippingFee(distance);

                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng." });
                }

                order.ShipPrice = decimal.Parse(Math.Round(fee, 0).ToString());
                db.SaveChanges();

                UpdateOrderTotal(order.OrderID);

                return Json(new
                {
                    success = true,
                    fee = order.ShipPrice,
                    distance,
                    finalPrice = order.TotalAmount
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi tính phí vận chuyển: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult SendOrderStatusEmail(string orderId)
        {


            var order = db.Orders.Include("Customer").FirstOrDefault(o => o.OrderID == orderId);
            if (order == null || order.Customer == null)
                return Json(new { success = false, message = "Lỗi đơn hàng hoặc khách hàng trống" });
            string toEmail = "";
            if (order.CustomerID == "QUEST001")
            {
                // Mẫu regex để lấy địa chỉ email sau từ khóa "email "
                var match = Regex.Match(order.ODDescription, @"email\s+([^\s]+)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    toEmail = match.Groups[1].Value;
                }
            }
            else
            {
                toEmail = order.Customer.Email;
            }
           
            string subject = "Xác nhận đơn hàng từ TDeli - Cảm ơn bạn đã thanh toán!";
            string body = $@"";
            if(order.OrderStatus == "Đã đặt đơn")
            {
                 body = $@"
                <p>Xin chào {order.Customer.FullName},</p>
                <p>Chúng tôi xin xác nhận rằng đơn hàng <strong>{order.OrderID}</strong> của bạn đã được thanh toán thành công vào lúc {order.UpdatedAt:HH:mm dd/MM/yyyy}.</p>
                <ul>
                    <li><strong>Trạng thái đơn hàng:</strong> {order.OrderStatus}</li>
                    <li><strong>Tổng tiền:</strong> {order.TotalAmount:N0} VNĐ</li>
                    <li><strong>Ngày đặt hàng:</strong> {order.UpdatedAt:dd/MM/yyyy}</li>
                </ul>
                <p>Chúng tôi sẽ tiến hành xử lý và chuẩn bị đơn hàng của bạn trong thời gian sớm nhất. Nếu có thắc mắc hoặc góp ý vui lòng reply lại mail này.</p>
                <p>Cảm ơn bạn đã lựa chọn TDeli!</p>
                <p>Trân trọng,<br/>TDeli Team</p>
            ";
                EmailService.SendEmail(toEmail, subject, body); // Tự viết hoặc dùng thư viện hỗ trợ
            }
            else if(order.OrderStatus == "Đang vận chuyển")
            {
                body = $@"
                    <p>Xin chào {order.Customer.FullName},</p>
                    <p>Chúng tôi rất vui thông báo rằng đơn hàng <strong>{order.OrderID}</strong> của bạn đã được đóng gói và đang trên đường vận chuyển. Vui lòng chú ý điện thoại của bạn.</p>
                    <ul>
                        <li><strong>Trạng thái đơn hàng:</strong> {order.OrderStatus}</li>
                        <li><strong>Tổng tiền:</strong> {order.TotalAmount:N0} VNĐ</li>
                        <li><strong>Ngày đặt hàng:</strong> {order.UpdatedAt:dd/MM/yyyy}</li>
                    </ul>
                    <p>Bạn sẽ sớm nhận được đơn hàng trong thời gian tới. Cảm ơn bạn đã tin tưởng và sử dụng dịch vụ của TDeli. Nếu có thắc mắc hoặc góp ý vui lòng reply lại mail này.</p>
                    <p>Trân trọng,<br/>TDeli Team</p>
                ";

                EmailService.SendEmail(toEmail, subject, body);
            }
            else if(order.OrderStatus == "Đã thanh toán")
            {
                body = $@"
                        <p>Xin chào {order.Customer.FullName},</p>
                        <p>Chúng tôi xin thông báo rằng đơn hàng <strong>{order.OrderID}</strong> của bạn đã được giao thành công.</p>
                        <ul>
                            <li><strong>Tổng tiền:</strong> {order.TotalAmount:N0} VNĐ</li>
                            <li><strong>Ngày giao:</strong> {DateTime.Now:dd/MM/yyyy HH:mm}</li>
                        </ul>
                        <p>Cảm ơn bạn đã lựa chọn TDeli.</p>
                        <p>Nếu bạn hài lòng với dịch vụ, hãy để lại đánh giá trong các bài viết để chúng tôi phục vụ bạn tốt hơn!</p>
                        <p>Trân trọng,<br/>TDeli Team</p>
                    ";

                EmailService.SendEmail(toEmail, subject, body);
            }

            return Json(new { success = true});
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
