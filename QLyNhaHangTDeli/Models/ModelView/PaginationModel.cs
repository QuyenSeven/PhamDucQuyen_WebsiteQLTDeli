using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QLyNhaHangTDeli.Models
{
    public class PaginationModel
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}