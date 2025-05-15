using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QLyNhaHangTDeli.Models
{
    public class UserCustomerViewModel
    {
        public User User { get; set; }
        public Customer Customer { get; set; }  
        public List<User> Users { get; set; }
        public List<Customer> Customers { get; set; }

    }
}