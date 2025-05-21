namespace QLyNhaHangTDeli.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class CustomerDiscountCode
    {
        [Key]
        [StringLength(50)]
        public string CustomerDiscountID { get; set; }

        [Required]
        [StringLength(50)]
        public string CustomerID { get; set; }

        [Required]
        [StringLength(50)]
        public string DiscountCodeID { get; set; }

        public DateTime? AssignedAt { get; set; }

        public virtual DiscountCode DiscountCode { get; set; }

        public virtual Customer Customer { get; set; }
    }
}
