using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;

namespace QLyNhaHangTDeli.Services
{
    public static class AuthHelper
    {
        public static bool IsAuthenticated => HttpContext.Current.User?.Identity?.IsAuthenticated ?? false;

        public static string GetUserID()
        {
            if (!IsAuthenticated)
                return "QUEST001"; // Trả về customerID mặc định khi không có người dùng

            var ticket = GetAuthTicket();
            return ticket?.Name; // CustomerID hoặc UserID
        }

        public static string GetUserData()
        {
            var ticket = GetAuthTicket();
            return ticket?.UserData;
        }

        public static string GetUserType()
        {
            if (!IsAuthenticated)
                return "Customer";
            var data = GetUserData();
            return data?.Split('|')[0];
        }

        public static string GetFullName()
        {
            var data = GetUserData();
            return data?.Split('|')[1];
        }

        public static string GetEmail()
        {
            var data = GetUserData();
            return data?.Split('|')[2];
        }

        public static string GetOther()
        {
            var data = GetUserData();
            return data?.Split('|')[3];
        }

        private static FormsAuthenticationTicket GetAuthTicket()
        {
            var authCookie = HttpContext.Current.Request.Cookies[FormsAuthentication.FormsCookieName];
            if (authCookie == null) return null;

            try
            {
                var decrypted = FormsAuthentication.Decrypt(authCookie.Value);
                return decrypted;
            }
            catch
            {
                return null;
            }
        }
    }
}