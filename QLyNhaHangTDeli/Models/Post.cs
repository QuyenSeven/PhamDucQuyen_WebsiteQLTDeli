namespace QLyNhaHangTDeli.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class Post
    {
        [StringLength(50)]
        public string PostID { get; set; }

        [Required]
        [StringLength(255)]
        public string Title { get; set; }

        [Required]
        [StringLength(500)]
        public string Summary { get; set; }

        [Required]
        public string Content { get; set; }

        [StringLength(255)]
        public string ImageUrl { get; set; }

        [StringLength(50)]
        public string PostType { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        [StringLength(50)]
        public string UserID { get; set; }

        public double? AverageRating { get; set; }

        public virtual User User { get; set; }
    }
}
