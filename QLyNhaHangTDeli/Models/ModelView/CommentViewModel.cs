using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QLyNhaHangTDeli.Models.ModelView
{
    public class CommentViewModel
    {
        public string CommentID { get; set; }
        public string Content { get; set; }
        public int? Rating { get; set; }
        public DateTime CreatedAt { get; set; }
        public string DisplayName { get; set; }
        public string AvatarUrl { get; set; }
    }
}