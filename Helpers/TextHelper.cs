using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;

namespace QLyNhaHangTDeli.Helpers
{
    public static class TextHelper
    {
        /// <summary>
        /// Loại bỏ dấu tiếng Việt khỏi chuỗi.
        /// </summary>
        public static string RemoveVietnameseSigns(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            input = input.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (char c in input)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Loại bỏ dấu và thay dấu cách bằng dấu gạch ngang (tùy chọn).
        /// </summary>
        public static string Slugify(string input)
        {
            return RemoveVietnameseSigns(input)
                    .ToLower()
                    .Replace(" ", "-")
                    .Replace(",", "-")
                    .Replace(".", "-")
                    .Replace(":", "-")
                    .Replace(";", "-");
        }
    }
}