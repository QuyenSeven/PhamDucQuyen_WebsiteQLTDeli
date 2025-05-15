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
                return new string(Enumerable.Repeat(chars, length)
                    .Select(s => s[random.Next(s.Length)]).ToArray());
            }
        }
    }
