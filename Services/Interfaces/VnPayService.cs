using Microsoft.Extensions.Configuration;
using QLyNhaHangTDeli.Helpers;
using QLyNhaHangTDeli.Models;
using QLyNhaHangTDeli.Services.Interfaces;
using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Web;
using System.Web.Mvc;

namespace QLyNhaHangTDeli.Services
{
    public class VnPayService : IVnPayService
    {
        public string CreatePaymentUrl(PaymentInformationModel model, HttpContextBase context)
        {
            var timeZoneById = TimeZoneInfo.FindSystemTimeZoneById(ConfigurationManager.AppSettings["TimeZoneId"]);
            var timeNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneById);
            var tick = model.OrderId; // Mã giao dịch duy nhất

            var pay = new VnPayLibrary();
            var urlCallBack = ConfigurationManager.AppSettings["vnp_PaymentBackReturnUrl"];

            // Thêm các tham số vào request
            pay.AddRequestData("vnp_Version", ConfigurationManager.AppSettings["vnp_Version"]);
            pay.AddRequestData("vnp_Command", ConfigurationManager.AppSettings["vnp_Command"]);
            pay.AddRequestData("vnp_TmnCode", ConfigurationManager.AppSettings["vnp_TmnCode"]);
            pay.AddRequestData("vnp_Amount", (model.Amount * 100).ToString()); // Số tiền cần thanh toán (đơn vị VND)
            pay.AddRequestData("vnp_CreateDate", timeNow.ToString("yyyyMMddHHmmss"));
            pay.AddRequestData("vnp_CurrCode", ConfigurationManager.AppSettings["vnp_CurrCode"]);
            pay.AddRequestData("vnp_IpAddr", pay.GetIpAddress(context)); // Lấy địa chỉ IP người dùng
            pay.AddRequestData("vnp_Locale", ConfigurationManager.AppSettings["vnp_Locale"]);
            pay.AddRequestData("vnp_OrderInfo", $"{model.Name} {model.OrderDescription}");
            pay.AddRequestData("vnp_OrderType", model.OrderType);
            pay.AddRequestData("vnp_ReturnUrl", urlCallBack); // Địa chỉ callback sau khi thanh toán
            pay.AddRequestData("vnp_TxnRef", tick); // Số tham chiếu giao dịch (unqiue)

            // Tạo URL thanh toán
            var paymentUrl = pay.CreateRequestUrl(ConfigurationManager.AppSettings["vnp_BaseUrl"], ConfigurationManager.AppSettings["vnp_HashSecret"]);

            return paymentUrl; // Trả về URL thanh toán cho người dùng
        }

        public PaymentResponseModel PaymentExecute(NameValueCollection collections)
        {
            var pay = new VnPayLibrary();

            // Lấy dữ liệu phản hồi từ VNPAY
            var response = pay.GetFullResponseData(collections, ConfigurationManager.AppSettings["vnp_HashSecret"]);

            if (!response.Success)
            {
                // Xử lý lỗi nếu chữ ký không hợp lệ
                return new PaymentResponseModel()
                {
                    Success = false,
                    
                };
            }

            // Trả về thông tin giao dịch khi thành công
            return response;
        }


    }
}
