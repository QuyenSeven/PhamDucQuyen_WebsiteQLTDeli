namespace QLyNhaHangTDeli.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class DiscountCode
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public DiscountCode()
        {
            AppliedDiscounts = new HashSet<AppliedDiscount>();
            CustomerDiscountCodes = new HashSet<CustomerDiscountCode>();
        }

        [StringLength(50)]
        public string DiscountCodeID { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; }

        public decimal DiscountAmount { get; set; }

        [Required]
        [StringLength(20)]
        public string DiscountType { get; set; }

        public DateTime ExpiryDate { get; set; }

        public decimal? MinimumOrderValue { get; set; }

        public bool? IsPublic { get; set; }

        public int? UsageLimit { get; set; }

        public int? UsedCount { get; set; }

        public decimal? MaxDiscountAmount { get; set; }

        public bool? IsActive { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<AppliedDiscount> AppliedDiscounts { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<CustomerDiscountCode> CustomerDiscountCodes { get; set; }
    }
}
