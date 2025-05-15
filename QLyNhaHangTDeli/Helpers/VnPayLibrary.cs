using QLyNhaHangTDeli.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Globalization;
using System.Collections.Specialized;

namespace QLyNhaHangTDeli.Helpers
{
    public class VnPayLibrary
    {
        private readonly SortedList<string, string> _requestData = new SortedList<string, string>(new VnPayCompare());
        private readonly SortedList<string, string> _responseData = new SortedList<string, string>(new VnPayCompare());

        public PaymentResponseModel GetFullResponseData(NameValueCollection queryString, string hashSecret)
        {
            var vnPay = new VnPayLibrary();

            // Duyệt qua tất cả các tham số trong QueryString và thêm vào vnPay nếu chúng có tiền tố vnp_
            foreach (var key in queryString.AllKeys)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    vnPay.AddResponseData(key, queryString[key]);
                }
            }

            // Lấy các thông tin trả về từ VnPay
            var orderId = vnPay.GetResponseData("vnp_TxnRef").ToString();
            var vnPayTranId = Convert.ToInt64(vnPay.GetResponseData("vnp_TransactionNo"));
            var vnpResponseCode = vnPay.GetResponseData("vnp_ResponseCode");
            var vnpSecureHash = queryString["vnp_SecureHash"]; // Lấy hash từ query string
            var orderInfo = vnPay.GetResponseData("vnp_OrderInfo");

            // Kiểm tra chữ ký hợp lệ
            var checkSignature = vnPay.ValidateSignature(vnpSecureHash, hashSecret);
            if (!checkSignature)
            {
                return new PaymentResponseModel()
                {
                    Success = false
                };
            }

            // Trả về kết quả nếu không có lỗi
            return new PaymentResponseModel()
            {
                Success = true,
                PaymentMethod = "VnPay",
                OrderDescription = orderInfo,
                OrderId = orderId.ToString(),
                PaymentId = vnPayTranId.ToString(),
                TransactionId = vnPayTranId.ToString(),
                Token = vnpSecureHash,
                VnPayResponseCode = vnpResponseCode
            };
        }


        public string GetIpAddress(HttpContextBase context)
        {
            string ipAddress = "127.0.0.1"; // Mặc định là localhost

            //try
            //{
            //    // Kiểm tra nếu có header X-Forwarded-For (có thể xuất hiện khi có proxy/ngược)
            //    var forwardedFor = context.Request.Headers["X-Forwarded-For"];
            //    if (!string.IsNullOrEmpty(forwardedFor))
            //    {
            //        // Trả về IP đầu tiên trong chuỗi (IP thực tế của client)
            //        ipAddress = forwardedFor.Split(',')[0].Trim();
            //    }
            //    else
            //    {
            //        // Lấy trực tiếp IP từ kết nối của client
            //        var remoteIpAddress = context.Request.UserHostAddress;
            //        if (!string.IsNullOrEmpty(remoteIpAddress))
            //        {
            //            ipAddress = remoteIpAddress;
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{
            //    // Log lỗi hoặc xử lý trường hợp ngoại lệ nếu cần
            //    ipAddress = "Error: " + ex.Message;
            //}

            return ipAddress;
        }




        public void AddRequestData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _requestData[key] = value;
            }
        }

        // Thêm dữ liệu vào _responseData nếu value không null hoặc rỗng
        public void AddResponseData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _responseData[key] = value;
            }
        }

        public string GetResponseData(string key)
        {
            return _responseData.TryGetValue(key, out var retValue) ? retValue : string.Empty;
        }


        public string CreateRequestUrl(string baseUrl, string vnpHashSecret)
        {
            var data = new StringBuilder();

            // Lặp qua các tham số đã được thêm vào _requestData
            foreach (var kv in _requestData.Where(kv => !string.IsNullOrEmpty(kv.Value)))
            {
                data.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
            }

            var querystring = data.ToString();

            baseUrl += "?" + querystring;

            // Loại bỏ dấu '&' cuối cùng nếu có
            var signData = querystring;
            if (signData.Length > 0)
            {
                signData = signData.Remove(signData.Length - 1, 1);
            }

            // Tính toán chữ ký HMAC-SHA512
            var vnpSecureHash = HmacSha512(vnpHashSecret, signData);
            baseUrl += "vnp_SecureHash=" + vnpSecureHash;

            return baseUrl;
        }

        public bool ValidateSignature(string inputHash, string secretKey)
        {
            // Lấy dữ liệu phản hồi
            var rspRaw = GetResponseData();

            // Tính toán chữ ký HMAC-SHA512 từ dữ liệu phản hồi
            var myChecksum = HmacSha512(secretKey, rspRaw);

            // So sánh chữ ký tính toán với chữ ký nhận được từ phản hồi
            return myChecksum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
        }


        private string HmacSha512(string key, string inputData)
        {
            var hash = new StringBuilder();
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                var hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }

            return hash.ToString();
        }

        private string GetResponseData()
        {
            var data = new StringBuilder();

            // Remove unnecessary keys
            if (_responseData.ContainsKey("vnp_SecureHashType"))
            {
                _responseData.Remove("vnp_SecureHashType");
            }

            if (_responseData.ContainsKey("vnp_SecureHash"))
            {
                _responseData.Remove("vnp_SecureHash");
            }

            // Loop through response data and append it to StringBuilder
            foreach (KeyValuePair<string, string> kv in _responseData.Where(kv => !string.IsNullOrEmpty(kv.Value)))
            {
                string key = kv.Key;
                string value = kv.Value;
                data.Append(HttpUtility.UrlEncode(key) + "=" + HttpUtility.UrlEncode(value) + "&");
            }
            // Remove the last '&' character if the StringBuilder has content
            if (data.Length > 0)
            {
                data.Remove(data.Length - 1, 1);
            }

            return data.ToString();
        }
    
    }
}

public class VnPayCompare : IComparer<string>
{
    public int Compare(string x, string y)
    {
        if (x == y) return 0;
        if (x == null) return -1;
        if (y == null) return 1;
        var vnpCompare = CompareInfo.GetCompareInfo("en-US");
        return vnpCompare.Compare(x, y, CompareOptions.Ordinal);
    }
}