using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QLyNhaHangTDeli.Models.ModelView
{
    public class DiscountCodeViewModel
    {
        public string CustomerID { get; set; }
        public string Code { get; set; }
        public decimal DiscountAmount { get; set; }
        public string DiscountType { get; set; }
        public bool IsAssigned { get; set; }
    }
}