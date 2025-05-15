namespace QLyNhaHangTDeli.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class Order
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Order()
        {
            AppliedDiscounts = new HashSet<AppliedDiscount>();
            Complaints = new HashSet<Complaint>();
            OrderDetails = new HashSet<OrderDetail>();
        }

        [StringLength(50)]
        public string OrderID { get; set; }

        [StringLength(50)]
        public string CustomerID { get; set; }

        [StringLength(50)]
        public string TableID { get; set; }

        [StringLength(20)]
        public string OrderStatus { get; set; }

        public decimal? Total { get; set; }

        public decimal? Discount { get; set; }

        public decimal? TotalAmount { get; set; }

        [Required]
        [StringLength(50)]
        public string Payment { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        [StringLength(100)]
        public string PaymentTransactionId { get; set; }

        [StringLength(100)]
        public string VnPayOrderId { get; set; }

        [StringLength(10)]
        public string VnPayResponseCode { get; set; }

        [StringLength(500)]
        public string OrderDescription { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<AppliedDiscount> AppliedDiscounts { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Complaint> Complaints { get; set; }

        public virtual Customer Customer { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<OrderDetail> OrderDetails { get; set; }

        public virtual Table Table { get; set; }
    }
}
