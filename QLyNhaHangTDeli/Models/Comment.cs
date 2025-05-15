namespace QLyNhaHangTDeli.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class Comment
    {
        [StringLength(50)]
        public string CommentID { get; set; }

        [Required]
        [StringLength(50)]
        public string PostID { get; set; }

        [Required]
        [StringLength(50)]
        public string UserID { get; set; }

        [Required]
        [StringLength(20)]
        public string UserType { get; set; }

        public int? Rating { get; set; }

        [Required]
        [StringLength(1000)]
        public string Content { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}
