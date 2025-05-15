using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace QLyNhaHangTDeli.Models
{
    public class RegisterViewModel
    {
        [StringLength(50)]
        public string UserID { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required]
        [StringLength(255)]
        public string ContactInfo { get; set; }

        [Required]
        [StringLength(255)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [StringLength(255)]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp!")]
        public string ConfirmPassword { get; set; }

        public bool IsEmployee { get; set; } = false; // Tick checkbox này để biết đang đăng ký Nhân viên

        // Tuỳ chọn thêm
        public DateTime? CreatedAt { get; set; } = DateTime.Now;
    }
}