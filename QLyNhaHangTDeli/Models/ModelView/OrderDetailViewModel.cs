using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QLyNhaHangTDeli.Models.ModelView
{
    public class OrderDetailViewModel
    {
        public Order Order { get; set; }
        public List<OrderDetail> OrderDetails { get; set; }
    }
}