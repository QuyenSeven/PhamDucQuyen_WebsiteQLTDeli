namespace QLyNhaHangTDeli.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class AppliedDiscount
    {
        [StringLength(50)]
        public string AppliedDiscountID { get; set; }

        [Required]
        [StringLength(50)]
        public string OrderID { get; set; }

        [Required]
        [StringLength(50)]
        public string DiscountCodeID { get; set; }

        [Required]
        [StringLength(50)]
        public string CustomerID { get; set; }

        public decimal DiscountAmount { get; set; }

        public DateTime? AppliedAt { get; set; }

        public virtual Customer Customer { get; set; }

        public virtual DiscountCode DiscountCode { get; set; }

        public virtual Order Order { get; set; }
    }
}
