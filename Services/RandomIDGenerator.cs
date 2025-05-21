using System;
using System.Linq;

namespace QLyNhaHangTDeli.Services
{
    public static class RandomIDGenerator
    {
        private const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        public static string Generate(int length = 6)
        {
            var random = new Random();
            string randomPart = new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            string year = DateTime.Now.Year.ToString(); // Lấy năm hiện tại (ví dụ: "2025")
            return year + randomPart; // Ví dụ: "2025A1B2C3"
        }
    }
}
