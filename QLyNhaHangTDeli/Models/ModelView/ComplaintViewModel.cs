using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace QLyNhaHangTDeli.Models
{
    public class ComplaintViewModel
    {
        [Required]
        public string Subject { get; set; }

        [Required]
        public string Description { get; set; }

        public string Email { get; set; }

        public string OrderID { get; set; }
    }
}