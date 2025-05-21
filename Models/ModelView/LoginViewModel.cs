using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace QLyNhaHangTDeli.Models
{
    public class LoginViewModel
    {
        [Required]
        public string ContactInfor { get; set; }
        [Required]
        public string Password { get; set; }
        public bool IsEmployee { get; set; }
    }
}