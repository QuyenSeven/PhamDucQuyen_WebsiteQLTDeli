namespace QLyNhaHangTDeli.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class Complaint
    {
        [StringLength(50)]
        public string ComplaintID { get; set; }

        [StringLength(50)]
        public string Email { get; set; }

        [StringLength(50)]
        public string CustomerID { get; set; }

        [StringLength(50)]
        public string OrderID { get; set; }

        [Required]
        [StringLength(255)]
        public string Subject { get; set; }

        [Required]
        public string Description { get; set; }

        [StringLength(20)]
        public string Status { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? ResolvedAt { get; set; }

        public string AdminComment { get; set; }

        public virtual Customer Customer { get; set; }

        public virtual Order Order { get; set; }
    }
}
