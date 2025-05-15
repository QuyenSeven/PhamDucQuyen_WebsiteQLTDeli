namespace QLyNhaHangTDeli.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Inventory")]
    public partial class Inventory
    {
        [StringLength(50)]
        public string InventoryID { get; set; }

        [StringLength(50)]
        public string ProductID { get; set; }

        [StringLength(50)]
        public string Supplier { get; set; }

        public int Quantity { get; set; }

        public decimal InPrice { get; set; }

        public DateTime? LastUpdated { get; set; }

        public virtual Product Product { get; set; }
    }
}
